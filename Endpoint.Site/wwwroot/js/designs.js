(function () {
    "use strict";

    const els = {
        canvas: document.getElementById("designCanvas"),
        canvasScaler: document.getElementById("designCanvasScaler"),
        stage: document.getElementById("designStage"),
        board: document.getElementById("designBoard"),
        rulerH: document.getElementById("designRulerH"),
        rulerV: document.getElementById("designRulerV"),
        measureLayer: document.getElementById("designMeasureLayer"),
        measureLine: document.getElementById("measureLine"),
        measurePointA: document.getElementById("measurePointA"),
        measurePointB: document.getElementById("measurePointB"),
        measureLabel: document.getElementById("measureLabel"),
        measureLabelBg: document.getElementById("measureLabelBg"),
        measureInfo: document.getElementById("designMeasureInfo"),
        guideX: document.getElementById("designGuideX"),
        guideY: document.getElementById("designGuideY"),
        measureBtn: document.getElementById("measureToolBtn"),
        clearMeasureBtn: document.getElementById("clearMeasureBtn"),
        panBtn: document.getElementById("panToolBtn"),
        unitSelect: document.getElementById("designUnitSelect"),
        zoomInBtn: document.getElementById("zoomInBtn"),
        zoomOutBtn: document.getElementById("zoomOutBtn"),
        zoomResetBtn: document.getElementById("zoomResetBtn"),
        zoomLevelLabel: document.getElementById("zoomLevelLabel"),
        zoomInFab: document.getElementById("zoomInFab"),
        zoomOutFab: document.getElementById("zoomOutFab"),
        zoomResetFab: document.getElementById("zoomResetFab"),
        boxList: document.getElementById("designBoxList"),
        controls: document.getElementById("designControls"),
        selectionHint: document.getElementById("selectionHint"),
        font: document.getElementById("designFontSelect"),
        fontFile: document.getElementById("designFontFile"),
        fontSize: document.getElementById("designFontSizeInput"),
        fontSizeValue: document.getElementById("designFontSizeValue"),
        letterSpacing: document.getElementById("designLetterSpacingInput"),
        letterSpacingValue: document.getElementById("designLetterSpacingValue"),
        fill: document.getElementById("designFillColor"),
        stroke: document.getElementById("designStrokeColor"),
        strokeWidth: document.getElementById("designStrokeWidthInput"),
        strokeWidthValue: document.getElementById("designStrokeWidthValue"),
        weld: document.getElementById("designWeldToggle"),
        fileName: document.getElementById("designFileName"),
        status: document.getElementById("designStatus"),
        addBtn: document.getElementById("addTextBoxBtn"),
        uploadSvgBtn: document.getElementById("uploadSvgBtn"),
        svgFile: document.getElementById("designSvgFile"),
        deleteBtn: document.getElementById("deleteTextBoxBtn"),
        exportBtn: document.getElementById("exportSvgBtn"),
        clearBtn: document.getElementById("clearDesignBtn"),
    };

    if (!els.canvas || !els.addBtn) {
        return;
    }

    const customFonts = new Map();
    const boxes = [];
    let selectedId = null;
    let nextId = 1;
    let suppressPanelSync = false;
    let dragState = null;
    let resizeState = null;
    let panState = null;
    let measureMode = false;
    let panMode = false;
    let spaceHeld = false;
    let measurePoint = null;
    let measureResult = null;
    let zoom = 1;
    let lastPointer = { x: null, y: null, overStage: false };
    const MIN_BOX_WIDTH = 48;
    const MIN_BOX_HEIGHT = 36;
    const MIN_FONT_SIZE = 12;
    const MAX_FONT_SIZE = 240;
    const RULER_SIZE = 24;
    const MIN_CANVAS_W = 1200;
    const MIN_CANVAS_H = 800;
    const CANVAS_GROW_PAD = 400;
    let canvasW = MIN_CANVAS_W;
    let canvasH = MIN_CANVAS_H;
    const ZOOM_MIN = 0.25;
    const ZOOM_MAX = 4;
    const ZOOM_STEP = 0.1;
    // استاندارد CSS: 96px = 1in
    const CSS_PPI = 96;
    const UNIT_DEFS = {
        px: { id: "px", label: "px", pxPerUnit: 1, minor: 10, mid: 50, major: 100, decimals: 0 },
        mm: { id: "mm", label: "mm", pxPerUnit: CSS_PPI / 25.4, minor: 1, mid: 5, major: 10, decimals: 1 },
        cm: { id: "cm", label: "cm", pxPerUnit: CSS_PPI / 2.54, minor: 0.5, mid: 1, major: 5, decimals: 2 },
        in: { id: "in", label: "in", pxPerUnit: CSS_PPI, minor: 0.125, mid: 0.25, major: 1, decimals: 2 },
    };

    function getUnitConfig() {
        const key = (els.unitSelect && els.unitSelect.value) || "px";
        return UNIT_DEFS[key] || UNIT_DEFS.px;
    }

    function setStatus(message) {
        if (els.status) {
            els.status.textContent = message || "";
        }
    }

    function toFaDigits(value) {
        return String(value).replace(/\d/g, function (d) {
            return "۰۱۲۳۴۵۶۷۸۹"[Number(d)];
        });
    }

    function pxToUnit(px) {
        const cfg = getUnitConfig();
        return px / cfg.pxPerUnit;
    }

    function formatLength(px) {
        const cfg = getUnitConfig();
        const value = pxToUnit(px);
        const rounded = Number(value.toFixed(cfg.decimals));
        return toFaDigits(rounded) + " " + cfg.label;
    }

    function formatRulerLabel(unitValue) {
        const cfg = getUnitConfig();
        if (cfg.id === "px") {
            return String(Math.round(unitValue));
        }
        if (cfg.id === "in") {
            return String(Number(unitValue.toFixed(cfg.decimals)));
        }
        if (Number.isInteger(unitValue) || Math.abs(unitValue - Math.round(unitValue)) < 1e-6) {
            return String(Math.round(unitValue));
        }
        return String(Number(unitValue.toFixed(cfg.decimals)));
    }

    function nearlyMultiple(value, step) {
        if (!step) {
            return false;
        }
        const ratio = value / step;
        return Math.abs(ratio - Math.round(ratio)) < 1e-6;
    }

    function getCanvasPoint(event) {
        const point = clientToCanvasPoint(event.clientX, event.clientY);
        // فقط کف صفر؛ سقف ثابت نداریم تا بوم بتواند بزرگ شود
        return {
            x: Math.max(0, point.x),
            y: Math.max(0, point.y),
        };
    }

    function applyCanvasDomSize() {
        if (!els.canvas || !els.canvasScaler) {
            return;
        }
        els.canvas.style.width = canvasW + "px";
        els.canvas.style.height = canvasH + "px";
        els.canvasScaler.style.width = canvasW * zoom + "px";
        els.canvasScaler.style.height = canvasH * zoom + "px";
    }

    function setCanvasSize(nextW, nextH) {
        const w = Math.max(MIN_CANVAS_W, Math.ceil(nextW));
        const h = Math.max(MIN_CANVAS_H, Math.ceil(nextH));
        if (w === canvasW && h === canvasH) {
            return false;
        }
        canvasW = w;
        canvasH = h;
        applyCanvasDomSize();
        drawRulers();
        return true;
    }

    function contentBounds() {
        let maxX = MIN_CANVAS_W;
        let maxY = MIN_CANVAS_H;
        boxes.forEach(function (box) {
            const w = box.width || 0;
            const h = box.height || 0;
            maxX = Math.max(maxX, (box.x || 0) + w + CANVAS_GROW_PAD);
            maxY = Math.max(maxY, (box.y || 0) + h + CANVAS_GROW_PAD);
        });
        return { width: maxX, height: maxY };
    }

    function ensureCanvasFits(extraX, extraY) {
        const bounds = contentBounds();
        const needW = Math.max(bounds.width, Number(extraX) || 0);
        const needH = Math.max(bounds.height, Number(extraY) || 0);
        return setCanvasSize(needW, needH);
    }

    function formatZoomLabel(value) {
        return toFaDigits(Math.round(value * 100)) + "٪";
    }

    function clampZoom(value) {
        const stepped = Math.round(value * 100) / 100;
        return Math.min(ZOOM_MAX, Math.max(ZOOM_MIN, stepped));
    }

    function trackPointer(event, overStage) {
        if (overStage) {
            lastPointer.x = event.clientX;
            lastPointer.y = event.clientY;
            lastPointer.overStage = true;
            return;
        }
        lastPointer.overStage = false;
    }

    function getZoomPivotClient() {
        // آخرین مکان ماوس روی بوم (حتی اگر الان روی دکمه زوم باشد)
        if (typeof lastPointer.x === "number" && typeof lastPointer.y === "number") {
            return { x: lastPointer.x, y: lastPointer.y };
        }
        if (!els.stage) {
            return { x: 0, y: 0 };
        }
        const rect = els.stage.getBoundingClientRect();
        return {
            x: rect.left + els.stage.clientWidth / 2,
            y: rect.top + els.stage.clientHeight / 2,
        };
    }

    function clientToCanvasPoint(clientX, clientY) {
        const rect = els.canvas.getBoundingClientRect();
        return {
            x: (clientX - rect.left) / zoom,
            y: (clientY - rect.top) / zoom,
        };
    }

    function applyZoom(nextZoom, clientX, clientY) {
        if (!els.canvas || !els.canvasScaler || !els.stage) {
            return;
        }

        const prev = zoom;
        const next = clampZoom(nextZoom);
        if (next === prev) {
            syncZoomUi();
            return;
        }

        const stage = els.stage;
        let pivotClientX = clientX;
        let pivotClientY = clientY;
        if (typeof pivotClientX !== "number" || typeof pivotClientY !== "number") {
            const pivot = getZoomPivotClient();
            pivotClientX = pivot.x;
            pivotClientY = pivot.y;
        }

        // نقطه بوم دقیقاً زیر نشانگر ماوس (قبل از تغییر زوم)
        const before = clientToCanvasPoint(pivotClientX, pivotClientY);

        zoom = next;
        els.canvas.style.transform = "scale(" + zoom + ")";
        applyCanvasDomSize();

        const grid = 24 * zoom;
        stage.style.backgroundSize = grid + "px " + grid + "px, " + grid + "px " + grid + "px, auto";

        // اجبار به محاسبه layout قبل از تنظیم اسکرول
        void els.canvasScaler.offsetWidth;

        const stageRect = stage.getBoundingClientRect();
        const anchorX = pivotClientX - stageRect.left;
        const anchorY = pivotClientY - stageRect.top;

        // همان نقطه بوم باید دوباره زیر ماوس بیاید
        stage.scrollLeft = before.x * zoom - anchorX;
        stage.scrollTop = before.y * zoom - anchorY;

        // یک فریم بعد دوباره قفل کن (بعضی مرورگرها scroll را دیر اعمال می‌کنند)
        const lockLeft = stage.scrollLeft;
        const lockTop = stage.scrollTop;
        requestAnimationFrame(function () {
            stage.scrollLeft = before.x * zoom - (pivotClientX - stage.getBoundingClientRect().left);
            stage.scrollTop = before.y * zoom - (pivotClientY - stage.getBoundingClientRect().top);
            if (stage.scrollLeft !== lockLeft || stage.scrollTop !== lockTop) {
                drawRulers();
            }
        });

        syncZoomUi();
        drawRulers();
        setStatus("زوم: " + formatZoomLabel(zoom) + " — مرجع مکان ماوس");
    }

    function syncZoomUi() {
        if (els.zoomLevelLabel) {
            els.zoomLevelLabel.textContent = formatZoomLabel(zoom);
        }
        if (els.zoomResetFab) {
            els.zoomResetFab.textContent = formatZoomLabel(zoom);
        }
        if (els.zoomInBtn) {
            els.zoomInBtn.disabled = zoom >= ZOOM_MAX;
        }
        if (els.zoomOutBtn) {
            els.zoomOutBtn.disabled = zoom <= ZOOM_MIN;
        }
        if (els.zoomInFab) {
            els.zoomInFab.disabled = zoom >= ZOOM_MAX;
        }
        if (els.zoomOutFab) {
            els.zoomOutFab.disabled = zoom <= ZOOM_MIN;
        }
    }

    function zoomBy(delta, clientX, clientY) {
        applyZoom(zoom + delta, clientX, clientY);
    }

    function drawRulers() {
        if (!els.rulerH || !els.rulerV || !els.stage) {
            return;
        }

        const cfg = getUnitConfig();
        const minorPx = cfg.minor * cfg.pxPerUnit * zoom;
        const scrollLeft = els.stage.scrollLeft;
        const scrollTop = els.stage.scrollTop;
        const viewW = els.stage.clientWidth;
        const viewH = els.stage.clientHeight;

        const h = els.rulerH;
        const v = els.rulerV;
        const dpr = window.devicePixelRatio || 1;

        h.width = Math.max(1, Math.floor(viewW * dpr));
        h.height = Math.floor(RULER_SIZE * dpr);
        h.style.width = viewW + "px";
        h.style.height = RULER_SIZE + "px";

        v.width = Math.floor(RULER_SIZE * dpr);
        v.height = Math.max(1, Math.floor(viewH * dpr));
        v.style.width = RULER_SIZE + "px";
        v.style.height = viewH + "px";

        const ctxH = h.getContext("2d");
        const ctxV = v.getContext("2d");
        ctxH.setTransform(dpr, 0, 0, dpr, 0, 0);
        ctxV.setTransform(dpr, 0, 0, dpr, 0, 0);

        ctxH.fillStyle = "#f1f5f9";
        ctxH.fillRect(0, 0, viewW, RULER_SIZE);
        ctxV.fillStyle = "#f1f5f9";
        ctxV.fillRect(0, 0, RULER_SIZE, viewH);

        ctxH.strokeStyle = "#94a3b8";
        ctxH.fillStyle = "#64748b";
        ctxH.font = "10px Vazir, Tahoma, sans-serif";
        ctxH.textAlign = "center";
        ctxH.textBaseline = "top";

        const startIndexX = Math.floor(scrollLeft / minorPx);
        const endIndexX = Math.ceil((scrollLeft + viewW) / minorPx) + 1;
        for (let i = startIndexX; i <= endIndexX; i++) {
            const x = i * minorPx;
            const px = x - scrollLeft;
            if (px < -1 || px > viewW + 1) {
                continue;
            }
            const unitValue = i * cfg.minor;
            const major = nearlyMultiple(unitValue, cfg.major);
            const mid = nearlyMultiple(unitValue, cfg.mid);
            const tick = major ? 14 : mid ? 10 : 6;
            ctxH.beginPath();
            ctxH.moveTo(px + 0.5, RULER_SIZE);
            ctxH.lineTo(px + 0.5, RULER_SIZE - tick);
            ctxH.stroke();
            if (major && unitValue >= 0) {
                ctxH.fillText(formatRulerLabel(unitValue), px, 2);
            }
        }

        ctxH.fillStyle = "#475569";
        ctxH.textAlign = "start";
        ctxH.fillText(cfg.label, 4, 2);

        ctxV.strokeStyle = "#94a3b8";
        ctxV.fillStyle = "#64748b";
        ctxV.font = "10px Vazir, Tahoma, sans-serif";
        ctxV.textAlign = "center";

        const startIndexY = Math.floor(scrollTop / minorPx);
        const endIndexY = Math.ceil((scrollTop + viewH) / minorPx) + 1;
        for (let i = startIndexY; i <= endIndexY; i++) {
            const y = i * minorPx;
            const py = y - scrollTop;
            if (py < -1 || py > viewH + 1) {
                continue;
            }
            const unitValue = i * cfg.minor;
            const major = nearlyMultiple(unitValue, cfg.major);
            const mid = nearlyMultiple(unitValue, cfg.mid);
            const tick = major ? 14 : mid ? 10 : 6;
            ctxV.beginPath();
            ctxV.moveTo(RULER_SIZE, py + 0.5);
            ctxV.lineTo(RULER_SIZE - tick, py + 0.5);
            ctxV.stroke();
            if (major && unitValue >= 0) {
                ctxV.save();
                ctxV.translate(10, py);
                ctxV.rotate(-Math.PI / 2);
                ctxV.fillText(formatRulerLabel(unitValue), 0, 0);
                ctxV.restore();
            }
        }
    }

    function showGuides(x, y) {
        if (!els.guideX || !els.guideY) {
            return;
        }
        els.guideX.classList.remove("d-none");
        els.guideY.classList.remove("d-none");
        els.guideX.style.left = x + "px";
        els.guideY.style.top = y + "px";
    }

    function hideGuides() {
        if (els.guideX) {
            els.guideX.classList.add("d-none");
        }
        if (els.guideY) {
            els.guideY.classList.add("d-none");
        }
    }

    function setMeasureVisible(nodes, visible) {
        nodes.forEach(function (node) {
            if (!node) {
                return;
            }
            node.classList.toggle("d-none", !visible);
        });
    }

    function clearMeasure() {
        measurePoint = null;
        measureResult = null;
        setMeasureVisible(
            [els.measureLine, els.measurePointA, els.measurePointB, els.measureLabel, els.measureLabelBg],
            false
        );
        if (els.measureInfo) {
            els.measureInfo.classList.add("d-none");
            els.measureInfo.textContent = "";
        }
        if (els.clearMeasureBtn) {
            els.clearMeasureBtn.classList.add("d-none");
        }
    }

    function refreshMeasureDisplay() {
        if (measureResult) {
            updateMeasureVisual(measureResult.a, measureResult.b, false);
        }
    }

    function onUnitChange() {
        drawRulers();
        refreshMeasureDisplay();
        const cfg = getUnitConfig();
        setStatus("واحد اندازه‌گیری: " + cfg.label);
    }

    function updateMeasureVisual(a, b, commit) {
        const dx = b.x - a.x;
        const dy = b.y - a.y;
        const dist = Math.sqrt(dx * dx + dy * dy);
        const midX = (a.x + b.x) / 2;
        const midY = (a.y + b.y) / 2;
        const label = formatLength(dist);

        els.measureLine.setAttribute("x1", a.x);
        els.measureLine.setAttribute("y1", a.y);
        els.measureLine.setAttribute("x2", b.x);
        els.measureLine.setAttribute("y2", b.y);

        els.measurePointA.setAttribute("cx", a.x);
        els.measurePointA.setAttribute("cy", a.y);
        els.measurePointB.setAttribute("cx", b.x);
        els.measurePointB.setAttribute("cy", b.y);

        els.measureLabel.setAttribute("x", midX);
        els.measureLabel.setAttribute("y", midY - 14);
        els.measureLabel.textContent = label;

        const approxWidth = Math.max(58, label.length * 7.2);
        els.measureLabelBg.setAttribute("x", midX - approxWidth / 2);
        els.measureLabelBg.setAttribute("y", midY - 26);
        els.measureLabelBg.setAttribute("width", approxWidth);
        els.measureLabelBg.setAttribute("height", 20);

        setMeasureVisible(
            [els.measureLine, els.measurePointA, els.measurePointB, els.measureLabel, els.measureLabelBg],
            true
        );

        if (els.measureInfo) {
            els.measureInfo.classList.remove("d-none");
            els.measureInfo.innerHTML =
                `<div><strong>فاصله:</strong> ${formatLength(dist)}</div>` +
                `<div><strong>ΔX:</strong> ${formatLength(Math.abs(dx))}</div>` +
                `<div><strong>ΔY:</strong> ${formatLength(Math.abs(dy))}</div>`;
        }
        if (els.clearMeasureBtn) {
            els.clearMeasureBtn.classList.remove("d-none");
        }

        if (commit) {
            measureResult = { a: { x: a.x, y: a.y }, b: { x: b.x, y: b.y }, dist, dx, dy };
        }
    }

    function updateStageCursorClass() {
        els.stage.classList.toggle("is-measure-mode", measureMode);
        els.stage.classList.toggle("is-pan-mode", panMode && !measureMode);
        els.stage.classList.toggle("is-space-pan", spaceHeld && !measureMode && !panMode);
        els.stage.classList.toggle("is-panning", !!panState);
    }

    function setMeasureMode(enabled) {
        measureMode = !!enabled;
        if (measureMode) {
            panMode = false;
            if (els.panBtn) {
                els.panBtn.classList.remove("active");
                els.panBtn.setAttribute("aria-pressed", "false");
            }
        }
        updateStageCursorClass();
        if (els.measureBtn) {
            els.measureBtn.classList.toggle("active", measureMode);
            els.measureBtn.setAttribute("aria-pressed", measureMode ? "true" : "false");
        }
        if (!measureMode) {
            hideGuides();
            measurePoint = null;
            if (measureResult) {
                updateMeasureVisual(measureResult.a, measureResult.b, false);
            }
            setStatus("حالت اندازه‌گیری خاموش شد.");
            return;
        }
        selectedId = null;
        boxes.forEach((box) => applyBoxStyles(box));
        renderList();
        syncPanelFromSelected();
        setStatus("روی بوم کلیک کنید: نقطه اول، سپس نقطه دوم برای اندازه‌گیری فاصله.");
    }

    function setPanMode(enabled) {
        panMode = !!enabled;
        if (panMode) {
            measureMode = false;
            hideGuides();
            measurePoint = null;
            if (els.measureBtn) {
                els.measureBtn.classList.remove("active");
                els.measureBtn.setAttribute("aria-pressed", "false");
            }
            selectedId = null;
            boxes.forEach((box) => applyBoxStyles(box));
            renderList();
            syncPanelFromSelected();
            setStatus("حالت جابه‌جایی فعال است. بوم را با ماوس بکشید.");
        } else {
            setStatus("حالت جابه‌جایی خاموش شد.");
        }
        if (els.panBtn) {
            els.panBtn.classList.toggle("active", panMode);
            els.panBtn.setAttribute("aria-pressed", panMode ? "true" : "false");
        }
        updateStageCursorClass();
    }

    function canStartPan(event) {
        if (measureMode) {
            return false;
        }
        if (event.button === 1) {
            return true;
        }
        if (event.button !== 0) {
            return false;
        }
        if (panMode || spaceHeld) {
            return true;
        }
        const target = event.target;
        return (
            target === els.stage ||
            target === els.canvas ||
            target === els.canvasScaler ||
            target === els.measureLayer
        );
    }

    function beginPan(event) {
        event.preventDefault();
        panState = {
            startX: event.clientX,
            startY: event.clientY,
            scrollLeft: els.stage.scrollLeft,
            scrollTop: els.stage.scrollTop,
        };
        dragState = null;
        resizeState = null;
        updateStageCursorClass();
    }

    function handleMeasureClick(event) {
        const point = getCanvasPoint(event);
        if (!measurePoint) {
            measurePoint = point;
            els.measurePointA.setAttribute("cx", point.x);
            els.measurePointA.setAttribute("cy", point.y);
            setMeasureVisible([els.measurePointA], true);
            setMeasureVisible([els.measureLine, els.measurePointB, els.measureLabel, els.measureLabelBg], false);
            setStatus("نقطه اول ثبت شد. نقطه دوم را کلیک کنید.");
            return;
        }
        updateMeasureVisual(measurePoint, point, true);
        measurePoint = null;
        setStatus("فاصله اندازه‌گیری شد. برای اندازه جدید دوباره کلیک کنید.");
    }

    function handleMeasureMove(event) {
        if (!measureMode) {
            return;
        }
        const point = getCanvasPoint(event);
        showGuides(point.x, point.y);
        if (measurePoint) {
            updateMeasureVisual(measurePoint, point, false);
        }
    }

    function getFontCssStack(fontFamily) {
        const name = fontFamily || "Vazir";
        return `"${name}", Tahoma, sans-serif`;
    }

    function escapeXml(value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&apos;");
    }

    function sanitizeFileName(name) {
        const cleaned = (name || "design").trim().replace(/[\\/:*?"<>|]+/g, "-");
        return cleaned || "design";
    }

    function waitForFonts(fontFamily, fontSize) {
        if (!document.fonts || !document.fonts.load) {
            return Promise.resolve();
        }
        return document.fonts.load(`${fontSize}px "${fontFamily}"`).catch(function () {
            return undefined;
        });
    }

    function getSelected() {
        return boxes.find((box) => box.id === selectedId) || null;
    }

    function createDefaultBox(partial) {
        const offset = (boxes.length % 8) * 28;
        return Object.assign(
            {
                id: nextId++,
                kind: "text",
                text: "متن جدید",
                x: 80 + offset,
                y: 80 + offset,
                width: 0,
                height: 0,
                fontFamily: "Vazir",
                fontSize: 48,
                letterSpacing: 0,
                fill: "#111827",
                stroke: "#111827",
                strokeWidth: 0,
                weld: false,
            },
            partial || {}
        );
    }

    function isSvgItem(box) {
        return !!(box && box.kind === "svg");
    }

    function labelForBox(box) {
        if (isSvgItem(box)) {
            return (box.name || "SVG").slice(0, 28);
        }
        const trimmed = (box.text || "").replace(/\s+/g, " ").trim();
        return trimmed ? trimmed.slice(0, 28) : "تکست باکس خالی";
    }

    function renderList() {
        if (!els.boxList) {
            return;
        }
        els.boxList.innerHTML = "";
        if (!boxes.length) {
            const empty = document.createElement("div");
            empty.className = "small text-muted";
            empty.textContent = "هنوز آیتمی نیست. تکست باکس اضافه کنید یا SVG آپلود کنید.";
            els.boxList.appendChild(empty);
            return;
        }

        boxes.forEach((box, index) => {
            const item = document.createElement("button");
            item.type = "button";
            item.className = "design-box-list-item" + (box.id === selectedId ? " active" : "");

            const label = document.createElement("span");
            label.textContent = `#${index + 1} — ${labelForBox(box)}`;

            const icon = document.createElement("i");
            icon.className = isSvgItem(box)
                ? "bi bi-filetype-svg text-muted"
                : "bi bi-cursor-text text-muted";

            item.appendChild(label);
            item.appendChild(icon);
            item.addEventListener("click", function () {
                selectBox(box.id);
            });
            els.boxList.appendChild(item);
        });
    }

    function syncPanelFromSelected() {
        const box = getSelected();
        const enabled = !!box && !isSvgItem(box);
        els.controls.classList.toggle("is-disabled", !enabled);
        els.deleteBtn.disabled = !box;
        if (!box) {
            els.selectionHint.textContent = "یک آیتم انتخاب کنید، تکست بسازید یا SVG آپلود کنید.";
            return;
        }
        if (isSvgItem(box)) {
            els.selectionHint.textContent =
                "SVG انتخاب‌شده: نقطه آبی = جابه‌جایی، دستگیره‌ها = تغییر اندازه. تنظیمات فونت روی SVG اعمال نمی‌شود.";
            return;
        }

        els.selectionHint.textContent =
            "داخل باکس تایپ کنید. نقطه آبی = جابه‌جایی. دستگیره‌ها = بزرگ/کوچک کردن فونت.";

        suppressPanelSync = true;
        els.font.value = box.fontFamily;
        if (![...els.font.options].some((opt) => opt.value === box.fontFamily)) {
            const option = document.createElement("option");
            option.value = box.fontFamily;
            option.textContent = box.fontFamily + " (سفارشی)";
            els.font.appendChild(option);
            els.font.value = box.fontFamily;
        }
        els.fontSize.value = String(box.fontSize);
        els.letterSpacing.value = String(box.letterSpacing);
        els.fill.value = box.fill;
        els.stroke.value = box.stroke;
        els.strokeWidth.value = String(box.strokeWidth);
        els.weld.checked = !!box.weld;
        els.fontSizeValue.textContent = String(box.fontSize);
        els.letterSpacingValue.textContent = String(box.letterSpacing);
        els.strokeWidthValue.textContent = String(box.strokeWidth);
        suppressPanelSync = false;
    }

    function applyBoxStyles(box) {
        if (isSvgItem(box)) {
            applySvgStyles(box);
            return;
        }
        const el = box.el;
        const input = box.input;
        if (!el || !input) {
            return;
        }
        const stack = getFontCssStack(box.fontFamily);
        el.style.setProperty("--box-font", stack);
        el.style.left = box.x + "px";
        el.style.top = box.y + "px";
        el.style.color = box.fill;
        el.style.fontSize = box.fontSize + "px";
        el.style.letterSpacing = box.letterSpacing + "px";
        el.style.webkitTextStroke = box.strokeWidth > 0 ? `${box.strokeWidth}px ${box.stroke}` : "0 transparent";
        input.style.fontFamily = stack;
        input.style.fontSize = box.fontSize + "px";
        input.style.letterSpacing = box.letterSpacing + "px";
        input.style.color = box.fill;
        input.style.lineHeight = "1.35";
        if (input.value !== box.text) {
            input.value = box.text;
        }
        el.classList.toggle("is-selected", box.id === selectedId);
        fitBoxToText(box);
    }

    function applySvgStyles(box) {
        const el = box.el;
        if (!el) {
            return;
        }
        el.style.left = box.x + "px";
        el.style.top = box.y + "px";
        el.style.width = box.width + "px";
        el.style.height = box.height + "px";
        el.classList.toggle("is-selected", box.id === selectedId);
        ensureCanvasFits();
    }

    function createResizeHandle(direction, title) {
        const handle = document.createElement("div");
        handle.className = "design-textbox-resize design-textbox-resize-" + direction;
        handle.dataset.resize = direction;
        handle.title = title;
        return handle;
    }

    function attachCommonBoxChrome(box, el, resizeTitle) {
        const moveHandle = document.createElement("div");
        moveHandle.className = "design-textbox-handle";
        moveHandle.title = "جابه‌جایی";

        const resizeE = createResizeHandle("e", resizeTitle);
        const resizeS = createResizeHandle("s", resizeTitle);
        const resizeSe = createResizeHandle("se", resizeTitle);

        el.appendChild(moveHandle);
        el.appendChild(resizeE);
        el.appendChild(resizeS);
        el.appendChild(resizeSe);

        box.handle = moveHandle;

        moveHandle.addEventListener("mousedown", function (event) {
            beginDrag(box, event);
        });

        [resizeE, resizeS, resizeSe].forEach(function (handle) {
            handle.addEventListener("mousedown", function (event) {
                beginResize(box, handle.dataset.resize, event);
            });
        });

        el.addEventListener("mousedown", function (event) {
            if (
                event.target === moveHandle ||
                (event.target.classList && event.target.classList.contains("design-textbox-resize"))
            ) {
                return;
            }
            if (box.input && event.target === box.input) {
                return;
            }
            beginDrag(box, event);
        });
    }

    function createBoxElement(box) {
        const el = document.createElement("div");
        el.className = "design-textbox";
        el.dataset.boxId = String(box.id);

        const input = document.createElement("textarea");
        input.className = "design-textbox-input";
        input.rows = 1;
        input.cols = 1;
        input.value = box.text;
        input.spellcheck = false;
        input.setAttribute("aria-label", "متن تکست باکس");

        el.appendChild(input);
        els.canvas.appendChild(el);

        box.el = el;
        box.input = input;

        attachCommonBoxChrome(box, el, "بزرگ/کوچک کردن فونت");

        input.addEventListener("focus", function () {
            selectBox(box.id);
        });

        input.addEventListener("input", function () {
            box.text = input.value;
            fitBoxToText(box);
            renderList();
            setStatus("متن به‌روز شد.");
        });

        input.addEventListener("mousedown", function (event) {
            event.stopPropagation();
            selectBox(box.id);
        });

        applyBoxStyles(box);
    }

    function parseSvgLength(value, fallback) {
        if (value == null || value === "") {
            return fallback;
        }
        const str = String(value).trim();
        const num = parseFloat(str);
        if (!Number.isFinite(num)) {
            return fallback;
        }
        // تبدیل به پیکسل صفحه (۹۶ppi) — همان مقیاس خط‌کش/بوم
        if (/mm\s*$/i.test(str)) {
            return num * (CSS_PPI / 25.4);
        }
        if (/cm\s*$/i.test(str)) {
            return num * (CSS_PPI / 2.54);
        }
        if (/in\s*$/i.test(str)) {
            return num * CSS_PPI;
        }
        if (/pt\s*$/i.test(str)) {
            return num * (CSS_PPI / 72);
        }
        if (/pc\s*$/i.test(str)) {
            return num * (CSS_PPI / 6);
        }
        // px یا بدون واحد
        return Math.abs(num) > 0 ? Math.abs(num) : fallback;
    }

    function inferUnitsPerCm(svg, declaredVb) {
        const wAttr = svg.getAttribute("width");
        if (!declaredVb || !(declaredVb.width > 0) || !wAttr) {
            return 1000; // پیش‌فرض Corel: ۱۰۰۰ واحد = ۱cm
        }
        const str = String(wAttr).trim();
        const num = parseFloat(str);
        if (!(num > 0)) {
            return 1000;
        }
        if (/cm\s*$/i.test(str)) {
            return declaredVb.width / num;
        }
        if (/mm\s*$/i.test(str)) {
            return declaredVb.width / (num / 10);
        }
        if (/in\s*$/i.test(str)) {
            return declaredVb.width / (num * 2.54);
        }
        // width بر حسب px و viewBox هم‌اندازه → واحد viewBox = px
        if (/px\s*$/i.test(str) || !/[a-z%]/i.test(str.replace(/[\d.\s+-eE]/g, ""))) {
            return null;
        }
        return 1000;
    }

    function svgViewBoxToPagePx(viewBox, unitsPerCm) {
        if (!viewBox) {
            return { width: 100, height: 100 };
        }
        if (unitsPerCm == null) {
            return {
                width: Math.max(1, viewBox.width),
                height: Math.max(1, viewBox.height),
            };
        }
        const pxPerCm = CSS_PPI / 2.54;
        return {
            width: Math.max(1, (viewBox.width / unitsPerCm) * pxPerCm),
            height: Math.max(1, (viewBox.height / unitsPerCm) * pxPerCm),
        };
    }

    function sanitizeSvgRoot(svg) {
        const forbidden = ["script", "foreignObject", "iframe", "embed", "object", "audio", "video", "metadata"];
        forbidden.forEach(function (tag) {
            Array.from(svg.getElementsByTagName(tag)).forEach(function (node) {
                node.parentNode && node.parentNode.removeChild(node);
            });
        });
        const walk = function (node) {
            if (!node || node.nodeType !== 1) {
                return;
            }
            Array.from(node.attributes || []).forEach(function (attr) {
                const name = attr.name.toLowerCase();
                const val = String(attr.value || "");
                if (name.startsWith("on") || /javascript:/i.test(val) || /data:text\/html/i.test(val)) {
                    node.removeAttribute(attr.name);
                }
                if ((name === "href" || name === "xlink:href") && /^\s*https?:/i.test(val)) {
                    node.removeAttribute(attr.name);
                }
            });
            Array.from(node.children || []).forEach(walk);
        };
        walk(svg);
        return svg;
    }

    function measureSvgContentBox(svg) {
        const host = document.createElement("div");
        host.style.cssText =
            "position:absolute;left:-99999px;top:0;width:1px;height:1px;overflow:hidden;opacity:0;pointer-events:none;";
        const clone = document.importNode(svg, true);
        clone.removeAttribute("viewBox");
        clone.removeAttribute("width");
        clone.removeAttribute("height");
        host.appendChild(clone);
        document.body.appendChild(host);

        try {
            let minX = Infinity;
            let minY = Infinity;
            let maxX = -Infinity;
            let maxY = -Infinity;
            let found = false;

            const shapes = clone.querySelectorAll(
                "path, polygon, polyline, rect, circle, ellipse, line, text, use, image"
            );
            shapes.forEach(function (el) {
                try {
                    if (typeof el.getBBox !== "function") {
                        return;
                    }
                    const b = el.getBBox();
                    if (!b || !Number.isFinite(b.x) || !Number.isFinite(b.y)) {
                        return;
                    }
                    const x2 = b.x + (Number.isFinite(b.width) ? b.width : 0);
                    const y2 = b.y + (Number.isFinite(b.height) ? b.height : 0);
                    if (x2 === b.x && y2 === b.y && b.width === 0 && b.height === 0) {
                        return;
                    }
                    found = true;
                    minX = Math.min(minX, b.x);
                    minY = Math.min(minY, b.y);
                    maxX = Math.max(maxX, x2);
                    maxY = Math.max(maxY, y2);
                } catch (e) {
                    // نادیده
                }
            });

            if (found && maxX > minX && maxY > minY) {
                return {
                    x: minX,
                    y: minY,
                    width: maxX - minX,
                    height: maxY - minY,
                };
            }

            if (typeof clone.getBBox === "function") {
                const b = clone.getBBox();
                if (b && b.width > 0 && b.height > 0) {
                    return { x: b.x, y: b.y, width: b.width, height: b.height };
                }
            }
        } catch (e) {
            // ادامه با fallback
        } finally {
            host.remove();
        }
        return null;
    }

    function readDeclaredViewBox(svg) {
        const raw = (svg.getAttribute("viewBox") || svg.getAttribute("viewbox") || "").trim();
        const vb = raw.split(/[\s,]+/).map(Number);
        if (vb.length === 4 && vb.every(function (n) { return Number.isFinite(n); }) && vb[2] > 0 && vb[3] > 0) {
            return { x: vb[0], y: vb[1], width: vb[2], height: vb[3] };
        }
        return null;
    }

    /**
     * محتوا را به مبدأ منتقل می‌کند و viewBox را دقیقاً دور همان محتوا می‌بندد.
     * از ورق بزرگ Corel (مثلاً 320cm) به‌عنوان کادر استفاده نمی‌کنیم.
     */
    function normalizeSvgContentIntoFrame(svg) {
        const content = measureSvgContentBox(svg);

        if (!content || !(content.width > 0 && content.height > 0)) {
            const declared = readDeclaredViewBox(svg);
            if (declared) {
                svg.setAttribute("viewBox", "0 0 " + declared.width + " " + declared.height);
                return { x: 0, y: 0, width: declared.width, height: declared.height };
            }
            const width = parseSvgLength(svg.getAttribute("width"), 100);
            const height = parseSvgLength(svg.getAttribute("height"), 100);
            svg.setAttribute("viewBox", "0 0 " + width + " " + height);
            return { x: 0, y: 0, width: width, height: height };
        }

        // کمی حاشیه برای stroke
        const pad = Math.max(content.width, content.height) * 0.01 + 2;
        const frame = {
            x: 0,
            y: 0,
            width: content.width + pad * 2,
            height: content.height + pad * 2,
        };

        const ns = "http://www.w3.org/2000/svg";
        const wrap = svg.ownerDocument.createElementNS(ns, "g");
        // فقط جابه‌جایی داده به داخل کادر (بدون مقیاس‌کردن به ورق عظیم)
        wrap.setAttribute(
            "transform",
            "translate(" + (pad - content.x) + " " + (pad - content.y) + ")"
        );
        wrap.setAttribute("data-normalized", "1");

        const keepOut = { defs: 1, style: 1, title: 1, desc: 1, metadata: 1 };
        const toMove = [];
        Array.from(svg.childNodes).forEach(function (node) {
            if (node.nodeType === 1) {
                const tag = String(node.tagName || "")
                    .toLowerCase()
                    .replace(/^.*:/, "");
                if (keepOut[tag]) {
                    return;
                }
                toMove.push(node);
            }
        });
        toMove.forEach(function (node) {
            wrap.appendChild(node);
        });
        svg.appendChild(wrap);

        svg.setAttribute("viewBox", "0 0 " + frame.width + " " + frame.height);
        svg.removeAttribute("width");
        svg.removeAttribute("height");
        return frame;
    }

    function extractSvgViewBox(svg) {
        // بعد از normalize معمولاً viewBox از ۰ شروع می‌شود
        const declared = readDeclaredViewBox(svg);
        if (declared) {
            return declared;
        }
        const contentBox = measureSvgContentBox(svg);
        if (contentBox) {
            return { x: 0, y: 0, width: contentBox.width, height: contentBox.height };
        }
        const width = parseSvgLength(svg.getAttribute("width"), 100);
        const height = parseSvgLength(svg.getAttribute("height"), 100);
        return { x: 0, y: 0, width: width, height: height };
    }

    function mountSvgInto(container, markup) {
        container.replaceChildren();
        const doc = new DOMParser().parseFromString(markup, "image/svg+xml");
        if (doc.querySelector("parsererror")) {
            return null;
        }
        const parsed = doc.documentElement;
        if (!parsed || String(parsed.tagName).toLowerCase() !== "svg") {
            return null;
        }
        const liveSvg = document.importNode(parsed, true);
        liveSvg.removeAttribute("width");
        liveSvg.removeAttribute("height");
        liveSvg.setAttribute("width", "100%");
        liveSvg.setAttribute("height", "100%");
        liveSvg.setAttribute("preserveAspectRatio", "xMidYMid meet");
        liveSvg.style.width = "100%";
        liveSvg.style.height = "100%";
        liveSvg.style.display = "block";
        liveSvg.style.maxWidth = "100%";
        liveSvg.style.maxHeight = "100%";
        liveSvg.style.overflow = "visible";
        container.appendChild(liveSvg);
        return liveSvg;
    }

    function createSvgElement(box) {
        const el = document.createElement("div");
        el.className = "design-svgbox";
        el.dataset.boxId = String(box.id);

        const inner = document.createElement("div");
        inner.className = "design-svgbox-inner";
        const liveSvg = mountSvgInto(inner, box.svgMarkup);
        if (!liveSvg) {
            inner.textContent = "SVG قابل نمایش نیست";
            inner.style.color = "#b91c1c";
            inner.style.fontSize = "12px";
            inner.style.padding = "8px";
            inner.style.pointerEvents = "none";
        }

        el.appendChild(inner);
        els.canvas.appendChild(el);

        box.el = el;
        box.input = null;
        box.inner = inner;

        attachCommonBoxChrome(box, el, "تغییر اندازه SVG");
        applySvgStyles(box);
    }

    function addSvgItem(parsed, fileName) {
        const offset = (boxes.length % 8) * 28;
        // اندازه واقعی فایل روی بوم — بدون کوچک/بزرگ‌کردن دلخواه
        const width = Math.max(8, Number(parsed.widthPx) || 100);
        const height = Math.max(8, Number(parsed.heightPx) || 100);

        const box = {
            id: nextId++,
            kind: "svg",
            name: (fileName || "svg").replace(/\.svg$/i, ""),
            x: 100 + offset,
            y: 100 + offset,
            width: width,
            height: height,
            viewBox: parsed.viewBox,
            svgMarkup: parsed.markup,
            svgInner: parsed.inner,
            unitsPerCm: parsed.unitsPerCm,
        };
        boxes.push(box);
        createSvgElement(box);
        selectBox(box.id);
        renderList();
        const cmW = width / (CSS_PPI / 2.54);
        const cmH = height / (CSS_PPI / 2.54);
        setStatus(
            "SVG «" +
                box.name +
                "» اضافه شد — اندازه واقعی: " +
                cmW.toFixed(2) +
                "×" +
                cmH.toFixed(2) +
                " cm"
        );
        return box;
    }

    function parseUploadedSvg(text, fileName) {
        const cleaned = String(text || "")
            .replace(/^\uFEFF/, "")
            .replace(/<\?xml[\s\S]*?\?>/i, "")
            .replace(/<!DOCTYPE[\s\S]*?>/i, "")
            .trim();
        const doc = new DOMParser().parseFromString(cleaned, "image/svg+xml");
        if (doc.querySelector("parsererror")) {
            throw new Error("فایل SVG معتبر نیست.");
        }
        const svg = doc.documentElement;
        if (!svg || String(svg.tagName).toLowerCase() !== "svg") {
            throw new Error("ریشهٔ فایل باید <svg> باشد.");
        }
        sanitizeSvgRoot(svg);
        if (!svg.getAttribute("xmlns")) {
            svg.setAttribute("xmlns", "http://www.w3.org/2000/svg");
        }

        // مقیاس فیزیکی را قبل از نرمال‌سازی از روی محتوا + واحدهای فایل بخوان
        // (نه maxSide دلخواه؛ ابعاد روی بوم = همان cm/mm فایل)
        const declaredVb = readDeclaredViewBox(svg);
        const unitsPerCm = inferUnitsPerCm(svg, declaredVb);
        const sheetWpx = parseSvgLength(svg.getAttribute("width"), 0);
        const sheetHpx = parseSvgLength(svg.getAttribute("height"), 0);
        const contentBox = measureSvgContentBox(svg);

        let widthPx;
        let heightPx;
        const pxPerCm = CSS_PPI / 2.54;
        if (contentBox && contentBox.width > 0 && contentBox.height > 0) {
            if (unitsPerCm == null) {
                // واحد viewBox همان پیکسل صفحه است
                widthPx = contentBox.width;
                heightPx = contentBox.height;
            } else if (sheetWpx > 0 && sheetHpx > 0 && declaredVb && declaredVb.width > 0 && declaredVb.height > 0) {
                // محتوا نسبت به ورق فیزیکی فایل
                widthPx = sheetWpx * (contentBox.width / declaredVb.width);
                heightPx = sheetHpx * (contentBox.height / declaredVb.height);
            } else {
                widthPx = (contentBox.width / unitsPerCm) * pxPerCm;
                heightPx = (contentBox.height / unitsPerCm) * pxPerCm;
            }
        } else if (sheetWpx > 0 && sheetHpx > 0) {
            widthPx = sheetWpx;
            heightPx = sheetHpx;
        } else {
            const sized = svgViewBoxToPagePx(
                declaredVb || { x: 0, y: 0, width: 100, height: 100 },
                unitsPerCm
            );
            widthPx = sized.width;
            heightPx = sized.height;
        }

        // محتوا را به مبدأ منتقل کن؛ واحدهای مختصات عوض نمی‌شوند (فقط جابه‌جایی)
        const viewBox = normalizeSvgContentIntoFrame(svg);
        svg.setAttribute("preserveAspectRatio", "xMidYMid meet");
        svg.removeAttribute("width");
        svg.removeAttribute("height");

        const inner = Array.from(svg.childNodes)
            .map(function (node) {
                return new XMLSerializer().serializeToString(node);
            })
            .join("");
        if (!inner.trim()) {
            throw new Error("محتوای SVG خالی است.");
        }

        return {
            viewBox: viewBox,
            markup: new XMLSerializer().serializeToString(svg),
            inner: inner,
            name: fileName,
            widthPx: widthPx,
            heightPx: heightPx,
            unitsPerCm: unitsPerCm,
        };
    }

    function onSvgFileChange(event) {
        const file = event.target.files && event.target.files[0];
        if (!file) {
            return;
        }
        if (!/\.svg$/i.test(file.name) && file.type !== "image/svg+xml") {
            setStatus("فقط فایل SVG مجاز است.");
            event.target.value = "";
            return;
        }
        setStatus("در حال خواندن SVG…");
        file
            .text()
            .then(function (text) {
                const parsed = parseUploadedSvg(text, file.name);
                addSvgItem(parsed, file.name);
            })
            .catch(function (err) {
                console.error(err);
                setStatus(err && err.message ? err.message : "آپلود SVG ناموفق بود.");
            })
            .finally(function () {
                event.target.value = "";
            });
    }

    function applyBoxSize(box) {
        const el = box.el;
        const input = box.input;
        if (!el || !input) {
            return;
        }
        el.style.width = box.width + "px";
        el.style.height = box.height + "px";
        input.style.width = "100%";
        input.style.height = "100%";
    }

    function getContentMinSize(box) {
        const text = box.text || " ";
        const measured = measureTextBox(text, box.fontFamily, box.fontSize, box.letterSpacing);
        // padding اینپوت: 6px 10px + border کادر ۱px
        const padX = 20 + 2;
        const padY = 12 + 2;
        const lineH = Math.ceil(box.fontSize * 1.35);
        return {
            width: Math.max(MIN_BOX_WIDTH, measured.width + padX),
            height: Math.max(MIN_BOX_HEIGHT, measured.height + padY, lineH + padY),
            measured,
        };
    }

    function fitBoxToText(box) {
        const input = box.input;
        const el = box.el;
        if (!input || !el) {
            return;
        }

        // کادر هویت مستقلی ندارد؛ همیشه دور متن می‌نشیند
        const minSize = getContentMinSize(box);
        box.width = minSize.width;
        box.height = minSize.height;
        applyBoxSize(box);
        // اسکرول ناخواسته بعد از تغییر اندازه
        input.scrollTop = 0;
        input.scrollLeft = 0;
        ensureCanvasFits();
    }

    function clampFontSize(size) {
        return Math.max(MIN_FONT_SIZE, Math.min(MAX_FONT_SIZE, Math.round(size)));
    }

    function syncFontSizePanel(box) {
        if (!box || suppressPanelSync) {
            return;
        }
        suppressPanelSync = true;
        els.fontSize.value = String(box.fontSize);
        els.fontSizeValue.textContent = String(box.fontSize);
        if (els.letterSpacing) {
            els.letterSpacing.value = String(box.letterSpacing);
            els.letterSpacingValue.textContent = String(box.letterSpacing);
        }
        if (els.strokeWidth) {
            els.strokeWidth.value = String(box.strokeWidth);
            els.strokeWidthValue.textContent = String(box.strokeWidth);
        }
        suppressPanelSync = false;
    }

    function selectBox(id) {
        selectedId = id;
        boxes.forEach((box) => applyBoxStyles(box));
        renderList();
        syncPanelFromSelected();
        setStatus(getSelected() ? "تکست باکس انتخاب شد." : "");
    }

    function addTextBox(partial) {
        const box = createDefaultBox(partial);
        boxes.push(box);
        createBoxElement(box);
        selectBox(box.id);
        renderList();
        setStatus("تکست باکس جدید اضافه شد.");
        return box;
    }

    function deleteSelected() {
        const box = getSelected();
        if (!box) {
            return;
        }
        box.el.remove();
        const index = boxes.findIndex((item) => item.id === box.id);
        if (index >= 0) {
            boxes.splice(index, 1);
        }
        selectedId = boxes.length ? boxes[Math.max(0, index - 1)].id : null;
        renderList();
        syncPanelFromSelected();
        boxes.forEach((item) => applyBoxStyles(item));
        setStatus("تکست باکس حذف شد.");
    }

    function clearDesign() {
        boxes.splice(0, boxes.length);
        selectedId = null;
        els.canvas.querySelectorAll(".design-textbox, .design-svgbox").forEach(function (node) {
            node.remove();
        });
        if (els.fontFile) {
            els.fontFile.value = "";
        }
        if (els.svgFile) {
            els.svgFile.value = "";
        }
        els.fileName.value = "design";
        clearMeasure();
        setMeasureMode(false);
        setPanMode(false);
        renderList();
        syncPanelFromSelected();
        setStatus("همه پاک شد.");
    }

    function applyPanelToSelected() {
        if (suppressPanelSync) {
            return;
        }
        const box = getSelected();
        if (!box || isSvgItem(box)) {
            return;
        }
        box.fontFamily = els.font.value || "Vazir";
        box.fontSize = clampFontSize(Number(els.fontSize.value) || 48);
        box.letterSpacing = Number(els.letterSpacing.value) || 0;
        box.fill = els.fill.value;
        box.stroke = els.stroke.value;
        box.strokeWidth = Number(els.strokeWidth.value) || 0;
        box.weld = !!els.weld.checked;
        els.fontSizeValue.textContent = String(box.fontSize);
        els.letterSpacingValue.textContent = String(box.letterSpacing);
        els.strokeWidthValue.textContent = String(box.strokeWidth);
        applyBoxStyles(box);
        fitBoxToText(box);
        renderList();
        waitForFonts(box.fontFamily, box.fontSize).then(function () {
            applyBoxStyles(box);
            setStatus(box.weld ? "Weld برای این باکس فعال است." : "استایل اعمال شد.");
        });
    }

    function beginDrag(box, event) {
        event.preventDefault();
        event.stopPropagation();
        selectBox(box.id);
        const point = getCanvasPoint(event);
        dragState = {
            box,
            offsetX: point.x - box.x,
            offsetY: point.y - box.y,
        };
        resizeState = null;
        box.el.classList.add("is-dragging");
    }

    function beginResize(box, direction, event) {
        event.preventDefault();
        event.stopPropagation();
        selectBox(box.id);
        if (!isSvgItem(box)) {
            fitBoxToText(box);
        }
        resizeState = {
            box,
            direction,
            startX: event.clientX,
            startY: event.clientY,
            startWidth: Math.max(1, box.width || box.el.offsetWidth),
            startHeight: Math.max(1, box.height || box.el.offsetHeight),
            startFontSize: box.fontSize || 48,
            startLetterSpacing: box.letterSpacing || 0,
            startStrokeWidth: box.strokeWidth || 0,
        };
        dragState = null;
        box.el.classList.add("is-resizing");
    }

    function onPointerMove(event) {
        if (panState) {
            const dx = event.clientX - panState.startX;
            const dy = event.clientY - panState.startY;
            els.stage.scrollLeft = panState.scrollLeft - dx;
            els.stage.scrollTop = panState.scrollTop - dy;
            return;
        }

        if (resizeState) {
            const box = resizeState.box;
            const dx = (event.clientX - resizeState.startX) / zoom;
            const dy = (event.clientY - resizeState.startY) / zoom;
            const dir = resizeState.direction;
            const scaleX = (resizeState.startWidth + dx) / resizeState.startWidth;
            const scaleY = (resizeState.startHeight + dy) / resizeState.startHeight;
            let scale = 1;
            if (dir === "e") {
                scale = scaleX;
            } else if (dir === "s") {
                scale = scaleY;
            } else {
                scale = Math.sqrt(Math.max(0.01, scaleX) * Math.max(0.01, scaleY));
            }
            scale = Math.max(0.05, scale);

            if (isSvgItem(box)) {
                box.width = Math.max(24, resizeState.startWidth * scale);
                box.height = Math.max(24, resizeState.startHeight * scale);
                applySvgStyles(box);
                return;
            }

            box.fontSize = clampFontSize(resizeState.startFontSize * scale);
            box.letterSpacing = Math.round(resizeState.startLetterSpacing * scale * 10) / 10;
            box.strokeWidth = Math.max(0, Math.round(resizeState.startStrokeWidth * scale * 10) / 10);

            box.el.style.fontSize = box.fontSize + "px";
            box.el.style.letterSpacing = box.letterSpacing + "px";
            if (box.input) {
                box.input.style.fontSize = box.fontSize + "px";
                box.input.style.letterSpacing = box.letterSpacing + "px";
            }
            if (box.strokeWidth > 0) {
                box.el.style.webkitTextStroke = box.strokeWidth + "px " + box.stroke;
            } else {
                box.el.style.webkitTextStroke = "0 transparent";
            }
            fitBoxToText(box);
            syncFontSizePanel(box);
            return;
        }

        if (!dragState) {
            return;
        }
        const point = getCanvasPoint(event);
        const box = dragState.box;
        box.x = Math.max(0, point.x - dragState.offsetX);
        box.y = Math.max(0, point.y - dragState.offsetY);
        box.el.style.left = box.x + "px";
        box.el.style.top = box.y + "px";

        const right = box.x + (box.width || 0) + CANVAS_GROW_PAD;
        const bottom = box.y + (box.height || 0) + CANVAS_GROW_PAD;
        if (right > canvasW - 80 || bottom > canvasH - 80) {
            ensureCanvasFits(right, bottom);
        }

        if (els.stage) {
            const edge = 48;
            const rect = els.stage.getBoundingClientRect();
            if (event.clientX > rect.right - edge) {
                els.stage.scrollLeft += 24;
            } else if (event.clientX < rect.left + edge + RULER_SIZE) {
                els.stage.scrollLeft -= 24;
            }
            if (event.clientY > rect.bottom - edge) {
                els.stage.scrollTop += 24;
            } else if (event.clientY < rect.top + edge + RULER_SIZE) {
                els.stage.scrollTop -= 24;
            }
        }
    }

    function onPointerUp() {
        if (panState) {
            panState = null;
            updateStageCursorClass();
            setStatus("بوم جابه‌جا شد.");
            return;
        }
        if (resizeState) {
            const box = resizeState.box;
            box.el.classList.remove("is-resizing");
            if (isSvgItem(box)) {
                applySvgStyles(box);
                resizeState = null;
                setStatus("اندازه SVG به‌روز شد.");
                return;
            }
            fitBoxToText(box);
            syncFontSizePanel(box);
            resizeState = null;
            setStatus("اندازه فونت: " + box.fontSize);
            return;
        }
        if (!dragState) {
            return;
        }
        dragState.box.el.classList.remove("is-dragging");
        dragState = null;
        setStatus("موقعیت به‌روز شد.");
    }

    function measureTextBox(text, fontFamily, fontSize, letterSpacing) {
        const canvas = document.createElement("canvas");
        const ctx = canvas.getContext("2d");
        ctx.font = `${fontSize}px ${getFontCssStack(fontFamily)}`;
        if (ctx.letterSpacing !== undefined) {
            ctx.letterSpacing = `${Number(letterSpacing) || 0}px`;
        }
        const lines = (text || " ").replace(/\r\n/g, "\n").split("\n");
        const lineHeight = Math.ceil(fontSize * 1.35);
        let maxWidth = 0;
        const lineWidths = lines.map((line) => {
            const content = line || " ";
            let width = ctx.measureText(content).width;
            // اگر مرورگر letterSpacing کانواس را اعمال نکند
            if (ctx.letterSpacing === undefined && letterSpacing) {
                width += Math.max(0, (content.length - 1) * letterSpacing);
            }
            maxWidth = Math.max(maxWidth, width);
            return width;
        });
        return {
            width: Math.ceil(maxWidth),
            height: Math.ceil(Math.max(1, lines.length) * lineHeight),
            lineHeight,
            lines,
            lineWidths,
        };
    }

    function pxToCorelUnits(px) {
        if (window.DesignVector && typeof DesignVector.pxToCorelUnits === "function") {
            return DesignVector.pxToCorelUnits(px);
        }
        return Math.round(Number(px) * ((2.54 / CSS_PPI) * 1000));
    }

    function getCorelStyleRegistry() {
        if (window.DesignVector && typeof DesignVector.getCorelStyleRegistry === "function") {
            return DesignVector.getCorelStyleRegistry();
        }
        throw new Error("ماژول DesignVector بارگذاری نشده است.");
    }

    function weldBoxToPaths(box) {
        if (!window.DesignVector || typeof DesignVector.textBoxToWeldedPath !== "function") {
            return Promise.reject(new Error("کتابخانه Weld (opentype/paper) در دسترس نیست."));
        }
        return DesignVector.textBoxToWeldedPath(box);
    }

    function computeBounds() {
        let maxX = 400;
        let maxY = 300;
        boxes.forEach((box) => {
            if (isSvgItem(box)) {
                maxX = Math.max(maxX, box.x + (box.width || 0) + 40);
                maxY = Math.max(maxY, box.y + (box.height || 0) + 40);
                return;
            }
            const measured = measureTextBox(box.text || " ", box.fontFamily, box.fontSize, box.letterSpacing);
            const w = box.width || measured.width + 40;
            const h = box.height || measured.height + 40;
            maxX = Math.max(maxX, box.x + w + 40);
            maxY = Math.max(maxY, box.y + h + 40);
        });
        return {
            width: Math.ceil(maxX),
            height: Math.ceil(maxY),
        };
    }

    function buildImportedSvgMarkup(box) {
        const vb = box.viewBox || { x: 0, y: 0, width: box.width, height: box.height };
        const sx = pxToCorelUnits(box.width) / Math.max(0.0001, vb.width);
        const sy = pxToCorelUnits(box.height) / Math.max(0.0001, vb.height);
        const tx = pxToCorelUnits(box.x);
        const ty = pxToCorelUnits(box.y);
        const round = function (n) {
            return Math.round(n * 1000) / 1000;
        };
        return (
            `  <g id="import_${box.id}" transform="translate(${round(tx)} ${round(ty)}) scale(${round(sx)} ${round(sy)}) translate(${round(-vb.x)} ${round(-vb.y)})">\n` +
            `   ${box.svgInner}\n` +
            `  </g>`
        );
    }

    function buildTextMarkupCorel(box, styles) {
        const text = (box.text || " ").replace(/\r\n/g, "\n");
        const measured = measureTextBox(text, box.fontFamily, box.fontSize, box.letterSpacing);
        const boxWidth = box.width || measured.width + 18;
        const localY0 = box.y + 8;
        const fillCls = styles.fillClass(box.fill);
        const hasStroke = (box.strokeWidth || 0) > 0;
        const strokeCls = hasStroke ? styles.strokeClass(box.stroke, box.strokeWidth) : "";
        const className = hasStroke ? fillCls + " " + strokeCls : fillCls;
        const fontSize = pxToCorelUnits(box.fontSize);
        const letterSpacing = pxToCorelUnits(box.letterSpacing);
        // متن منطقی اصلی + RTL (نه Presentation Forms؛ بدون فونت خالی دیده می‌شود)
        const tspans = measured.lines
            .map((line, index) => {
                const content = line.length ? line : " ";
                const y = pxToCorelUnits(localY0 + box.fontSize * 0.85 + index * measured.lineHeight);
                const lineWidth = measured.lineWidths ? measured.lineWidths[index] : measured.width;
                const x = pxToCorelUnits(box.x + boxWidth - 10 - lineWidth);
                return `   <tspan x="${x}" y="${y}">${escapeXml(content)}</tspan>`;
            })
            .join("\n");

        return (
            `  <text class="${className}" style="font-family:'${escapeXml(box.fontFamily || "Vazir")}',Tahoma,sans-serif;font-size:${fontSize};letter-spacing:${letterSpacing}" ` +
            `fill="${escapeXml(box.fill || "#000000")}" text-anchor="start" direction="rtl" unicode-bidi="bidi-override" xml:space="preserve">\n` +
            `${tspans}\n` +
            `  </text>`
        );
    }

    function buildExportSvg() {
        const bounds = computeBounds();
        const styles = getCorelStyleRegistry();

        const tasks = boxes.map((box) => {
            if (isSvgItem(box)) {
                return Promise.resolve({ type: "svg", box: box });
            }
            if (!(box.text || "").trim()) {
                return Promise.resolve({ type: "empty" });
            }
            return weldBoxToPaths(box)
                .then(function (result) {
                    const paths = (result && result.paths) || [];
                    const usable = paths.filter(function (p) {
                        return p && p.d && String(p.d).replace(/[\sMZ]/gi, "").length > 0;
                    });
                    if (!usable.length) {
                        throw new Error("path خالی ساخته شد.");
                    }
                    return { type: "paths", paths: usable };
                })
                .catch(function (err) {
                    console.warn("path export failed, fallback to text:", err);
                    return { type: "text", box: box, error: err };
                });
        });

        return Promise.all(tasks).then(function (parts) {
            const body = [];
            let pathError = null;

            parts.forEach(function (part) {
                if (part.type === "empty") {
                    return;
                }
                if (part.type === "svg") {
                    body.push(buildImportedSvgMarkup(part.box));
                    return;
                }
                if (part.type === "text") {
                    pathError = part.error || pathError;
                    body.push(buildTextMarkupCorel(part.box, styles));
                    return;
                }
                (part.paths || []).forEach(function (item) {
                    if (!item.d) {
                        return;
                    }
                    const fillCls = styles.fillClass(item.fill);
                    const hasStroke = (item.strokeWidth || 0) > 0;
                    const className = hasStroke
                        ? fillCls + " " + styles.strokeClass(item.stroke, item.strokeWidth)
                        : fillCls;
                    const fillAttr = ` fill="${escapeXml(item.fill || "#000000")}"`;
                    const strokeAttr =
                        hasStroke
                            ? ` stroke="${escapeXml(item.stroke || "#000000")}" stroke-width="${Math.max(
                                  1,
                                  pxToCorelUnits(item.strokeWidth)
                              )}"`
                            : ` stroke="none"`;
                    body.push(`  <path class="${className}"${fillAttr}${strokeAttr} fill-rule="nonzero" d="${item.d}"/>`);
                });
            });

            if (!body.length) {
                throw pathError || new Error("محتوایی برای خروجی SVG ساخته نشد.");
            }

            if (window.DesignVector && typeof DesignVector.buildCorelSvgDocument === "function") {
                return DesignVector.buildCorelSvgDocument(bounds, body, styles);
            }

            throw new Error("ماژول DesignVector بارگذاری نشده است.");
        });
    }

    function downloadSvg(svgContent, fileName) {
        const blob = new Blob([svgContent], { type: "image/svg+xml;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = `${sanitizeFileName(fileName)}.svg`;
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);
    }

    function exportSvg() {
        if (!boxes.length) {
            setStatus("ابتدا یک تکست باکس یا SVG اضافه کنید.");
            return;
        }
        const hasContent = boxes.some(function (box) {
            return isSvgItem(box) || (box.text || "").trim();
        });
        if (!hasContent) {
            setStatus("حداقل یک متن یا SVG برای خروجی لازم است.");
            return;
        }

        els.exportBtn.disabled = true;
        setStatus("در حال ساخت SVG…");
        buildExportSvg()
            .then(function (svg) {
                if (!svg || String(svg).length < 80) {
                    throw new Error("خروجی SVG خالی بود.");
                }
                downloadSvg(svg, els.fileName.value);
                setStatus("فایل SVG دانلود شد (" + Math.round(String(svg).length / 1024) + " KB).");
            })
            .catch(function (error) {
                console.error(error);
                setStatus(error && error.message ? error.message : "خروجی SVG ناموفق بود.");
            })
            .finally(function () {
                els.exportBtn.disabled = false;
            });
    }

    function addCustomFontOption(familyName) {
        const exists = Array.from(els.font.options).some((opt) => opt.value === familyName);
        if (!exists) {
            const option = document.createElement("option");
            option.value = familyName;
            option.textContent = familyName + " (سفارشی)";
            els.font.appendChild(option);
        }
        els.font.value = familyName;
    }

    function onFontFileChange(event) {
        const file = event.target.files && event.target.files[0];
        if (!file) {
            return;
        }

        const familyName = file.name.replace(/\.[^.]+$/, "").trim() || "CustomFont";
        const objectUrl = URL.createObjectURL(file);
        const previous = customFonts.get(familyName);
        if (previous) {
            URL.revokeObjectURL(previous);
        }
        customFonts.set(familyName, objectUrl);

        setStatus("در حال بارگذاری فونت…");
        Promise.all([
            file.arrayBuffer(),
            new FontFace(familyName, `url(${objectUrl})`).load(),
        ])
            .then(function (results) {
                const buffer = results[0];
                const loaded = results[1];
                if (window.DesignVector && typeof DesignVector.registerCustomFont === "function") {
                    DesignVector.registerCustomFont(familyName, buffer);
                }
                document.fonts.add(loaded);
                addCustomFontOption(familyName);
                applyPanelToSelected();
                setStatus("فونت «" + familyName + "» اضافه شد.");
            })
            .catch(function (error) {
                console.error(error);
                setStatus("بارگذاری فونت ناموفق بود. فایل TTF/OTF معتبر انتخاب کنید.");
            });
    }

    ["input", "change"].forEach(function (evt) {
        els.font.addEventListener(evt, applyPanelToSelected);
        els.fontSize.addEventListener(evt, applyPanelToSelected);
        els.letterSpacing.addEventListener(evt, applyPanelToSelected);
        els.fill.addEventListener(evt, applyPanelToSelected);
        els.stroke.addEventListener(evt, applyPanelToSelected);
        els.strokeWidth.addEventListener(evt, applyPanelToSelected);
        els.weld.addEventListener(evt, applyPanelToSelected);
    });

    if (els.fontFile) {
        els.fontFile.addEventListener("change", onFontFileChange);
    }

    els.addBtn.addEventListener("click", function () {
        addTextBox();
    });
    if (els.uploadSvgBtn && els.svgFile) {
        els.uploadSvgBtn.addEventListener("click", function () {
            els.svgFile.click();
        });
        els.svgFile.addEventListener("change", onSvgFileChange);
    }
    els.deleteBtn.addEventListener("click", deleteSelected);
    els.clearBtn.addEventListener("click", clearDesign);
    els.exportBtn.addEventListener("click", exportSvg);

    if (els.measureBtn) {
        els.measureBtn.addEventListener("click", function () {
            setMeasureMode(!measureMode);
        });
    }
    if (els.panBtn) {
        els.panBtn.addEventListener("click", function () {
            setPanMode(!panMode);
        });
    }
    if (els.clearMeasureBtn) {
        els.clearMeasureBtn.addEventListener("click", function () {
            clearMeasure();
            setStatus("اندازه‌گیری پاک شد.");
        });
    }
    if (els.unitSelect) {
        els.unitSelect.addEventListener("change", onUnitChange);
    }

    function bindZoomButton(btn, deltaOrReset) {
        if (!btn) {
            return;
        }
        btn.addEventListener("click", function () {
            const pivot = getZoomPivotClient();
            if (deltaOrReset === "reset") {
                applyZoom(1, pivot.x, pivot.y);
                return;
            }
            zoomBy(deltaOrReset, pivot.x, pivot.y);
        });
    }

    bindZoomButton(els.zoomInBtn, ZOOM_STEP);
    bindZoomButton(els.zoomOutBtn, -ZOOM_STEP);
    bindZoomButton(els.zoomResetBtn, "reset");
    bindZoomButton(els.zoomInFab, ZOOM_STEP);
    bindZoomButton(els.zoomOutFab, -ZOOM_STEP);
    bindZoomButton(els.zoomResetFab, "reset");

    els.stage.addEventListener(
        "wheel",
        function (event) {
            if (!(event.ctrlKey || event.metaKey)) {
                return;
            }
            event.preventDefault();
            trackPointer(event, true);
            const delta = event.deltaY < 0 ? ZOOM_STEP : -ZOOM_STEP;
            zoomBy(delta, event.clientX, event.clientY);
        },
        { passive: false }
    );

    els.stage.addEventListener("mousedown", function (event) {
        trackPointer(event, true);
        if (measureMode) {
            if (event.target.closest && event.target.closest(".design-textbox-resize, .design-textbox-handle")) {
                return;
            }
            event.preventDefault();
            handleMeasureClick(event);
            return;
        }

        if (canStartPan(event)) {
            if (
                event.button === 0 &&
                !panMode &&
                !spaceHeld &&
                (event.target === els.stage ||
                    event.target === els.canvas ||
                    event.target === els.canvasScaler ||
                    event.target === els.measureLayer)
            ) {
                selectedId = null;
                boxes.forEach((box) => applyBoxStyles(box));
                renderList();
                syncPanelFromSelected();
            }
            beginPan(event);
            return;
        }
    });

    // جلوگیری از منوی اسکرول وسط
    els.stage.addEventListener("auxclick", function (event) {
        if (event.button === 1) {
            event.preventDefault();
        }
    });
    els.stage.addEventListener("contextmenu", function (event) {
        if (panState) {
            event.preventDefault();
        }
    });

    els.stage.addEventListener("mousemove", function (event) {
        trackPointer(event, true);
        handleMeasureMove(event);
    });

    els.stage.addEventListener("mouseenter", function (event) {
        trackPointer(event, true);
    });

    els.stage.addEventListener("mouseleave", function () {
        trackPointer({ clientX: lastPointer.x, clientY: lastPointer.y }, false);
        if (measureMode) {
            hideGuides();
        }
    });

    els.stage.addEventListener("scroll", drawRulers);
    window.addEventListener("resize", drawRulers);

    window.addEventListener("mousemove", onPointerMove);
    window.addEventListener("mouseup", onPointerUp);

    window.addEventListener("keydown", function (event) {
        if (event.code === "Space" && !event.repeat) {
            const tag = (document.activeElement && document.activeElement.tagName) || "";
            if (tag !== "TEXTAREA" && tag !== "INPUT" && tag !== "SELECT") {
                event.preventDefault();
                spaceHeld = true;
                updateStageCursorClass();
            }
        }
        if ((event.ctrlKey || event.metaKey) && (event.key === "=" || event.key === "+" || event.key === "-" || event.key === "0")) {
            const tag = (document.activeElement && document.activeElement.tagName) || "";
            if (tag === "TEXTAREA" || tag === "INPUT" || tag === "SELECT") {
                return;
            }
            const pivot = getZoomPivotClient();
            if (event.key === "0") {
                event.preventDefault();
                applyZoom(1, pivot.x, pivot.y);
                return;
            }
            if (event.key === "-") {
                event.preventDefault();
                zoomBy(-ZOOM_STEP, pivot.x, pivot.y);
                return;
            }
            event.preventDefault();
            zoomBy(ZOOM_STEP, pivot.x, pivot.y);
            return;
        }
        if (event.key === "Escape") {
            if (panMode) {
                setPanMode(false);
                return;
            }
            if (measureMode) {
                if (measurePoint) {
                    measurePoint = null;
                    if (measureResult) {
                        updateMeasureVisual(measureResult.a, measureResult.b, false);
                    } else {
                        clearMeasure();
                    }
                    setStatus("انتخاب نقطه لغو شد.");
                    return;
                }
                setMeasureMode(false);
                return;
            }
        }
        if (event.key === "Delete" && getSelected() && document.activeElement.tagName !== "TEXTAREA") {
            deleteSelected();
        }
    });

    window.addEventListener("keyup", function (event) {
        if (event.code === "Space") {
            spaceHeld = false;
            updateStageCursorClass();
        }
    });

    applyCanvasDomSize();
    applyZoom(1);
    addTextBox({ text: "طراحی" });
    setStatus("جابه‌جایی بوم: کشیدن روی فضای خالی، دکمه «جابه‌جایی»، Space+کشیدن، یا کلیک وسط ماوس. زوم حول مکان ماوس انجام می‌شود.");
})();
