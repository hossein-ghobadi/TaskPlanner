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
        fillSpectrum: document.getElementById("designFillSpectrum"),
        stroke: document.getElementById("designStrokeColor"),
        strokeSpectrum: document.getElementById("designStrokeSpectrum"),
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
    const spectrumControllers = { fill: null, stroke: null };
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
                charFills: null, // آرایه رنگ هر کاراکتر؛ null = یکدست با fill
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
        const hasBox = !!box;
        const svgMode = isSvgItem(box);
        els.controls.classList.toggle("is-disabled", !hasBox);
        els.controls.classList.toggle("is-svg-mode", svgMode);
        els.deleteBtn.disabled = !box;
        if (!box) {
            els.selectionHint.textContent = "یک آیتم انتخاب کنید، تکست بسازید یا SVG آپلود کنید.";
            return;
        }
        if (svgMode) {
            els.selectionHint.textContent =
                "SVG انتخاب‌شده: رنگ پر/حاشیه را از طیف عوض کنید. نقطه آبی = جابه‌جایی، دستگیره‌ها = تغییر اندازه.";
            suppressPanelSync = true;
            els.fill.value = normalizeFillHex(box.fill || box.originalFill || "#111827");
            els.stroke.value = normalizeFillHex(box.stroke || box.originalStroke || "#111827");
            els.strokeWidth.value = String(box.strokeWidth || 0);
            els.strokeWidthValue.textContent = String(box.strokeWidth || 0);
            syncSpectrumFromInput("fill", els.fill.value);
            syncSpectrumFromInput("stroke", els.stroke.value);
            suppressPanelSync = false;
            return;
        }

        els.selectionHint.textContent =
            "بخشی از متن را انتخاب کنید و رنگ پر را عوض کنید؛ بدون انتخاب، رنگ کل باکس عوض می‌شود.";

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
        els.fill.value = getActiveFillForPanel(box);
        els.stroke.value = box.stroke;
        els.strokeWidth.value = String(box.strokeWidth);
        els.weld.checked = !!box.weld;
        els.fontSizeValue.textContent = String(box.fontSize);
        els.letterSpacingValue.textContent = String(box.letterSpacing);
        els.strokeWidthValue.textContent = String(box.strokeWidth);
        syncSpectrumFromInput("fill", els.fill.value);
        syncSpectrumFromInput("stroke", els.stroke.value);
        suppressPanelSync = false;
    }

    function clamp01(n) {
        return Math.max(0, Math.min(1, n));
    }

    function hexToRgb(hex) {
        const value = normalizeFillHex(hex || "#000000").replace("#", "");
        return {
            r: parseInt(value.slice(0, 2), 16),
            g: parseInt(value.slice(2, 4), 16),
            b: parseInt(value.slice(4, 6), 16),
        };
    }

    function rgbToHex(r, g, b) {
        function part(n) {
            return Math.max(0, Math.min(255, Math.round(n)))
                .toString(16)
                .padStart(2, "0");
        }
        return "#" + part(r) + part(g) + part(b);
    }

    function rgbToHsv(r, g, b) {
        r /= 255;
        g /= 255;
        b /= 255;
        const max = Math.max(r, g, b);
        const min = Math.min(r, g, b);
        const d = max - min;
        let h = 0;
        const s = max === 0 ? 0 : d / max;
        const v = max;
        if (d !== 0) {
            switch (max) {
                case r:
                    h = ((g - b) / d + (g < b ? 6 : 0)) / 6;
                    break;
                case g:
                    h = ((b - r) / d + 2) / 6;
                    break;
                default:
                    h = ((r - g) / d + 4) / 6;
                    break;
            }
        }
        return { h: h * 360, s: s, v: v };
    }

    function hsvToRgb(h, s, v) {
        h = ((h % 360) + 360) % 360;
        const c = v * s;
        const x = c * (1 - Math.abs(((h / 60) % 2) - 1));
        const m = v - c;
        let r = 0;
        let g = 0;
        let b = 0;
        if (h < 60) {
            r = c;
            g = x;
        } else if (h < 120) {
            r = x;
            g = c;
        } else if (h < 180) {
            g = c;
            b = x;
        } else if (h < 240) {
            g = x;
            b = c;
        } else if (h < 300) {
            r = x;
            b = c;
        } else {
            r = c;
            b = x;
        }
        return {
            r: (r + m) * 255,
            g: (g + m) * 255,
            b: (b + m) * 255,
        };
    }

    function syncSpectrumFromInput(kind, color) {
        const ctrl = spectrumControllers[kind];
        if (!ctrl) {
            return;
        }
        ctrl.setHex(color, true);
    }

    function buildSpectrumPicker(root, kind, onChange) {
        if (!root) {
            return null;
        }
        root.innerHTML = "";
        root.classList.add("design-spectrum");

        const sv = document.createElement("div");
        sv.className = "design-spectrum-sv";
        sv.tabIndex = 0;
        const white = document.createElement("div");
        white.className = "design-spectrum-sv-white";
        const black = document.createElement("div");
        black.className = "design-spectrum-sv-black";
        const thumb = document.createElement("div");
        thumb.className = "design-spectrum-sv-thumb";
        sv.appendChild(white);
        sv.appendChild(black);
        sv.appendChild(thumb);

        const hue = document.createElement("input");
        hue.type = "range";
        hue.className = "design-spectrum-hue";
        hue.min = "0";
        hue.max = "360";
        hue.step = "1";
        hue.value = "210";
        hue.setAttribute("aria-label", "فام رنگ");

        const rgbWrap = document.createElement("div");
        rgbWrap.className = "design-spectrum-rgb";
        const channels = ["R", "G", "B"].map(function (name) {
            const label = document.createElement("label");
            label.textContent = name;
            const input = document.createElement("input");
            input.type = "number";
            input.min = "0";
            input.max = "255";
            input.step = "1";
            input.value = "0";
            input.setAttribute("aria-label", name);
            label.appendChild(input);
            rgbWrap.appendChild(label);
            return input;
        });

        const preview = document.createElement("div");
        preview.className = "design-spectrum-preview";

        root.appendChild(sv);
        root.appendChild(hue);
        root.appendChild(rgbWrap);
        root.appendChild(preview);

        const state = { h: 210, s: 0.67, v: 0.67, silent: false };

        function emit() {
            const rgb = hsvToRgb(state.h, state.s, state.v);
            const hex = rgbToHex(rgb.r, rgb.g, rgb.b);
            if (!state.silent && typeof onChange === "function") {
                onChange(hex);
            }
        }

        function paint(skipRgbInputs) {
            const rgb = hsvToRgb(state.h, state.s, state.v);
            const hex = rgbToHex(rgb.r, rgb.g, rgb.b);
            const pure = hsvToRgb(state.h, 1, 1);
            sv.style.backgroundColor = rgbToHex(pure.r, pure.g, pure.b);
            thumb.style.left = state.s * 100 + "%";
            thumb.style.top = (1 - state.v) * 100 + "%";
            hue.value = String(Math.round(state.h));
            preview.style.backgroundColor = hex;
            if (!skipRgbInputs) {
                channels[0].value = String(Math.round(rgb.r));
                channels[1].value = String(Math.round(rgb.g));
                channels[2].value = String(Math.round(rgb.b));
            }
            return hex;
        }

        function setHex(hex, silent) {
            const rgb = hexToRgb(hex);
            const hsv = rgbToHsv(rgb.r, rgb.g, rgb.b);
            state.h = hsv.h;
            state.s = hsv.s;
            state.v = hsv.v;
            state.silent = !!silent;
            paint(false);
            state.silent = false;
        }

        function setFromPointer(clientX, clientY) {
            const rect = sv.getBoundingClientRect();
            if (!(rect.width > 0) || !(rect.height > 0)) {
                return;
            }
            const x = clamp01((clientX - rect.left) / rect.width);
            const y = clamp01((clientY - rect.top) / rect.height);
            state.s = x;
            state.v = clamp01(1 - y);
            paint(false);
            emit();
        }

        let dragging = false;
        function onPointerDown(event) {
            event.preventDefault();
            dragging = true;
            setFromPointer(event.clientX, event.clientY);
            if (sv.setPointerCapture && event.pointerId != null) {
                try {
                    sv.setPointerCapture(event.pointerId);
                } catch (e) {
                    // ignore
                }
            }
        }
        function onPointerMove(event) {
            if (!dragging) {
                return;
            }
            event.preventDefault();
            setFromPointer(event.clientX, event.clientY);
        }
        function onPointerUp() {
            dragging = false;
        }

        sv.addEventListener("pointerdown", onPointerDown);
        sv.addEventListener("pointermove", onPointerMove);
        sv.addEventListener("pointerup", onPointerUp);
        sv.addEventListener("pointercancel", onPointerUp);

        hue.addEventListener("input", function () {
            state.h = Number(hue.value) || 0;
            paint(false);
            emit();
        });

        channels.forEach(function (input) {
            input.addEventListener("change", function () {
                const r = Number(channels[0].value) || 0;
                const g = Number(channels[1].value) || 0;
                const b = Number(channels[2].value) || 0;
                const hsv = rgbToHsv(r, g, b);
                state.h = hsv.h;
                state.s = hsv.s;
                state.v = hsv.v;
                paint(true);
                emit();
            });
        });

        // جلوگیری از از دست رفتن selection متن هنگام درگ روی طیف
        root.addEventListener("mousedown", function (event) {
            if (event.target && event.target.closest && event.target.closest("input")) {
                return;
            }
            event.preventDefault();
        });

        paint(false);
        const api = { setHex: setHex, kind: kind };
        spectrumControllers[kind] = api;
        return api;
    }

    function normalizeFillHex(color) {
        if (!color) {
            return "#111827";
        }
        const value = String(color).trim();
        if (/^#[0-9a-fA-F]{6}$/.test(value)) {
            return value.toLowerCase();
        }
        if (/^#[0-9a-fA-F]{3}$/.test(value)) {
            return ("#" + value[1] + value[1] + value[2] + value[2] + value[3] + value[3]).toLowerCase();
        }
        return value;
    }

    function hasMultiFill(box) {
        if (!box || !Array.isArray(box.charFills) || !box.charFills.length) {
            return false;
        }
        const first = normalizeFillHex(box.charFills[0] || box.fill);
        for (let i = 1; i < box.charFills.length; i++) {
            if (normalizeFillHex(box.charFills[i] || box.fill) !== first) {
                return true;
            }
        }
        return false;
    }

    function collapseCharFillsIfUniform(box) {
        if (!box || !Array.isArray(box.charFills)) {
            return;
        }
        if (!box.charFills.length) {
            box.charFills = null;
            return;
        }
        const first = normalizeFillHex(box.charFills[0] || box.fill);
        const allSame = box.charFills.every(function (c) {
            return normalizeFillHex(c || box.fill) === first;
        });
        if (allSame) {
            box.fill = first;
            box.charFills = null;
        }
    }

    function ensureCharFills(box) {
        const text = box.text || "";
        const fallback = normalizeFillHex(box.fill || "#111827");
        if (!Array.isArray(box.charFills) || box.charFills.length !== text.length) {
            const next = [];
            for (let i = 0; i < text.length; i++) {
                next.push(
                    Array.isArray(box.charFills) && box.charFills[i]
                        ? normalizeFillHex(box.charFills[i])
                        : fallback
                );
            }
            box.charFills = next;
        }
        return box.charFills;
    }

    function remapCharFills(oldText, oldFills, newText, defaultFill) {
        if (!Array.isArray(oldFills)) {
            return null;
        }
        const fallback = normalizeFillHex(defaultFill || "#111827");
        const oldT = String(oldText || "");
        const newT = String(newText || "");
        let prefix = 0;
        while (prefix < oldT.length && prefix < newT.length && oldT.charAt(prefix) === newT.charAt(prefix)) {
            prefix++;
        }
        let suffix = 0;
        while (
            suffix < oldT.length - prefix &&
            suffix < newT.length - prefix &&
            oldT.charAt(oldT.length - 1 - suffix) === newT.charAt(newT.length - 1 - suffix)
        ) {
            suffix++;
        }
        const result = [];
        for (let i = 0; i < prefix; i++) {
            result.push(normalizeFillHex(oldFills[i] || fallback));
        }
        for (let i = prefix; i < newT.length - suffix; i++) {
            result.push(fallback);
        }
        for (let i = 0; i < suffix; i++) {
            result.push(normalizeFillHex(oldFills[oldT.length - suffix + i] || fallback));
        }
        if (!result.length) {
            return null;
        }
        const first = result[0];
        if (result.every(function (c) {
            return c === first;
        })) {
            return null;
        }
        return result;
    }

    function getActiveFillForPanel(box) {
        if (!box) {
            return "#111827";
        }
        const range = getSavedTextSelection(box);
        if (range && Array.isArray(box.charFills)) {
            const i = Math.min(range.start, Math.max(0, (box.text || "").length - 1));
            if (i >= 0 && box.charFills[i]) {
                return normalizeFillHex(box.charFills[i]);
            }
        }
        return normalizeFillHex(box.fill || "#111827");
    }

    function getSavedTextSelection(box) {
        if (!box) {
            return null;
        }
        const input = box.input;
        let start = box._selStart;
        let end = box._selEnd;
        if (
            input &&
            document.activeElement === input &&
            typeof input.selectionStart === "number"
        ) {
            start = input.selectionStart;
            end = input.selectionEnd;
        }
        if (typeof start !== "number" || typeof end !== "number" || start === end) {
            return null;
        }
        return {
            start: Math.min(start, end),
            end: Math.max(start, end),
        };
    }

    function rememberTextSelection(box) {
        const input = box && box.input;
        if (!input || typeof input.selectionStart !== "number") {
            return;
        }
        box._selStart = input.selectionStart;
        box._selEnd = input.selectionEnd;
    }

    function escapeHtml(text) {
        return String(text || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function renderTextColorPreview(box) {
        const preview = box.preview;
        const input = box.input;
        if (!preview || !input) {
            return;
        }
        const text = box.text || "";
        const multi = hasMultiFill(box);
        if (!multi) {
            preview.innerHTML = "";
            preview.classList.add("d-none");
            input.classList.remove("is-multicolor");
            input.style.color = box.fill;
            input.style.webkitTextFillColor = "";
            return;
        }

        const fills = ensureCharFills(box);
        let html = "";
        let i = 0;
        while (i < text.length) {
            const fill = normalizeFillHex(fills[i] || box.fill);
            let j = i + 1;
            while (j < text.length && normalizeFillHex(fills[j] || box.fill) === fill) {
                j++;
            }
            html +=
                '<span style="color:' +
                fill +
                '">' +
                escapeHtml(text.slice(i, j)).replace(/\n/g, "<br>") +
                "</span>";
            i = j;
        }
        preview.innerHTML = html || "&nbsp;";
        preview.classList.remove("d-none");
        input.classList.add("is-multicolor");
        input.style.color = "transparent";
        input.style.webkitTextFillColor = "transparent";
    }

    function applyFillFromPanel(box, color) {
        const hex = normalizeFillHex(color);
        const range = getSavedTextSelection(box);
        const textLen = (box.text || "").length;

        if (range && range.start < textLen) {
            const start = Math.max(0, range.start);
            const end = Math.min(textLen, range.end);
            if (end > start) {
                ensureCharFills(box);
                for (let i = start; i < end; i++) {
                    box.charFills[i] = hex;
                }
                collapseCharFillsIfUniform(box);
                box.fill = hex;
                setStatus("رنگ بخش انتخاب‌شده اعمال شد.");
                return;
            }
        }

        box.fill = hex;
        box.charFills = null;
        setStatus("رنگ کل متن اعمال شد.");
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
        input.style.lineHeight = "1.35";
        if (input.value !== box.text) {
            input.value = box.text;
        }
        if (box.preview) {
            box.preview.style.fontFamily = stack;
            box.preview.style.fontSize = box.fontSize + "px";
            box.preview.style.letterSpacing = box.letterSpacing + "px";
            box.preview.style.webkitTextStroke =
                box.strokeWidth > 0 ? `${box.strokeWidth}px ${box.stroke}` : "0 transparent";
        }
        renderTextColorPreview(box);
        if (!hasMultiFill(box)) {
            input.style.color = box.fill;
        }
        el.classList.toggle("is-selected", box.id === selectedId);
        fitBoxToText(box);
    }

    function applySvgStyles(box, options) {
        const el = box.el;
        if (!el) {
            return;
        }
        el.style.left = box.x + "px";
        el.style.top = box.y + "px";
        el.style.width = box.width + "px";
        el.style.height = box.height + "px";
        el.classList.toggle("is-selected", box.id === selectedId);
        const forcePaint = !!(options && options.repaint);
        if (forcePaint || !box.liveSvg) {
            refreshSvgPaint(box);
        } else if (box.strokeOverride && box.fillOverride !== undefined) {
            // ضخامت حاشیه نسبت به اندازه باکس است؛ بعد از resize دوباره محاسبه شود
            if (options && options.resize) {
                refreshSvgPaint(box);
            }
        }
        ensureCanvasFits();
    }

    function isPaintNone(value) {
        const v = String(value || "")
            .trim()
            .toLowerCase();
        return !v || v === "none" || v === "transparent";
    }

    function extractCssColor(cssText, prop) {
        if (!cssText) {
            return null;
        }
        const re = new RegExp("(?:^|[;{]\\s*)" + prop + "\\s*:\\s*([^;}{]+)", "i");
        const m = String(cssText).match(re);
        return m ? m[1].trim() : null;
    }

    function cssColorToHex(color) {
        if (!color) {
            return "#111827";
        }
        const raw = String(color).trim();
        if (/^#[0-9a-fA-F]{3}$|^#[0-9a-fA-F]{6}$/.test(raw)) {
            return normalizeFillHex(raw);
        }
        const rgbMatch = raw.match(/rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)/i);
        if (rgbMatch) {
            return rgbToHex(Number(rgbMatch[1]), Number(rgbMatch[2]), Number(rgbMatch[3]));
        }
        try {
            const canvas = document.createElement("canvas");
            canvas.width = 1;
            canvas.height = 1;
            const ctx = canvas.getContext("2d");
            ctx.fillStyle = "#000000";
            ctx.fillStyle = raw;
            const computed = String(ctx.fillStyle || "");
            const m = computed.match(/rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)/i);
            if (m) {
                return rgbToHex(Number(m[1]), Number(m[2]), Number(m[3]));
            }
            if (/^#[0-9a-fA-F]{6}$/i.test(computed)) {
                return normalizeFillHex(computed);
            }
        } catch (e) {
            // ignore
        }
        return "#111827";
    }

    function detectSvgPaint(svg) {
        const result = {
            fill: "#111827",
            stroke: "#111827",
            strokeWidth: 0,
        };
        if (!svg) {
            return result;
        }

        const styleColors = [];
        Array.from(svg.querySelectorAll("style")).forEach(function (styleEl) {
            const css = styleEl.textContent || "";
            const fillMatches = css.match(/fill\s*:\s*([^;}{]+)/gi) || [];
            fillMatches.forEach(function (part) {
                const color = part.split(":").slice(1).join(":").trim();
                if (!isPaintNone(color) && !/^url\(/i.test(color)) {
                    styleColors.push(color);
                }
            });
            const strokeMatch = css.match(/stroke\s*:\s*([^;}{]+)/i);
            if (strokeMatch && !isPaintNone(strokeMatch[1]) && !/^url\(/i.test(strokeMatch[1])) {
                result.stroke = strokeMatch[1].trim();
            }
        });
        if (styleColors.length) {
            result.fill = styleColors[0];
        }

        const shapes = svg.querySelectorAll("path, polygon, polyline, rect, circle, ellipse, text, tspan");
        for (let i = 0; i < shapes.length; i++) {
            const el = shapes[i];
            const fill =
                el.getAttribute("fill") ||
                extractCssColor(el.getAttribute("style"), "fill") ||
                (el.style && el.style.fill);
            if (result.fill === "#111827" && fill && !isPaintNone(fill) && !/^url\(/i.test(fill)) {
                result.fill = fill;
            }
            const stroke =
                el.getAttribute("stroke") ||
                extractCssColor(el.getAttribute("style"), "stroke") ||
                (el.style && el.style.stroke);
            if (stroke && !isPaintNone(stroke) && !/^url\(/i.test(stroke)) {
                result.stroke = stroke;
                const sw =
                    el.getAttribute("stroke-width") ||
                    extractCssColor(el.getAttribute("style"), "stroke-width");
                const swNum = parseFloat(sw);
                if (Number.isFinite(swNum) && swNum > 0) {
                    result.strokeWidth = swNum;
                }
            }
            if (result.fill !== "#111827") {
                break;
            }
        }

        result.fill = cssColorToHex(result.fill);
        result.stroke = cssColorToHex(result.stroke);
        return result;
    }

    function paintSvgDocument(svg, box) {
        if (!svg || !box) {
            return;
        }
        const fillOverride = !!box.fillOverride;
        const strokeOverride = !!box.strokeOverride;
        if (!fillOverride && !strokeOverride) {
            return;
        }

        const fill = normalizeFillHex(box.fill || "#111827");
        const stroke = normalizeFillHex(box.stroke || "#111827");
        const strokeWidthPx = Math.max(0, Number(box.strokeWidth) || 0);
        const vb = box.viewBox || { width: box.width || 1 };
        const unitScale = box.width > 0 ? (vb.width || box.width) / box.width : 1;
        const strokeWidthUnits = strokeWidthPx * unitScale;

        Array.from(svg.querySelectorAll("style")).forEach(function (styleEl) {
            let css = styleEl.textContent || "";
            if (fillOverride) {
                css = css.replace(/fill\s*:\s*([^;}{]+)/gi, function (full, value) {
                    if (isPaintNone(value) || /^url\(/i.test(value)) {
                        return full;
                    }
                    return "fill:" + fill;
                });
            }
            if (strokeOverride) {
                css = css.replace(/stroke\s*:\s*([^;}{]+)/gi, function (full, value) {
                    if (isPaintNone(value) || /^url\(/i.test(value)) {
                        return full;
                    }
                    return "stroke:" + (strokeWidthPx > 0 ? stroke : "none");
                });
                if (strokeWidthPx > 0) {
                    if (/stroke-width\s*:/i.test(css)) {
                        css = css.replace(/stroke-width\s*:\s*[^;}{]+/gi, "stroke-width:" + strokeWidthUnits);
                    }
                }
            }
            styleEl.textContent = css;
        });

        const shapes = svg.querySelectorAll(
            "path, polygon, polyline, rect, circle, ellipse, line, text, tspan"
        );
        Array.from(shapes).forEach(function (el) {
            if (fillOverride) {
                const fillAttr = el.getAttribute("fill");
                const styleFill = extractCssColor(el.getAttribute("style"), "fill") || (el.style && el.style.fill);
                const current = fillAttr || styleFill;
                if (!isPaintNone(current) && !/^url\(/i.test(String(current || ""))) {
                    // اگر fill نداشت ولی در کلاس رنگ دارد، باز هم ست کن
                    el.setAttribute("fill", fill);
                    if (el.style) {
                        el.style.fill = fill;
                    }
                } else if (!current) {
                    // اشکال بدون fill صریح (رنگ از CSS کلاس) — override کن
                    el.setAttribute("fill", fill);
                    if (el.style) {
                        el.style.fill = fill;
                    }
                }
            }

            if (strokeOverride) {
                if (strokeWidthPx > 0) {
                    el.setAttribute("stroke", stroke);
                    el.setAttribute("stroke-width", String(strokeWidthUnits));
                    if (el.style) {
                        el.style.stroke = stroke;
                        el.style.strokeWidth = String(strokeWidthUnits);
                    }
                } else {
                    el.setAttribute("stroke", "none");
                    if (el.style) {
                        el.style.stroke = "none";
                    }
                }
            }
        });
    }

    function serializeSvgBoxMarkup(svg) {
        if (!svg) {
            return { markup: "", inner: "" };
        }
        const markup = new XMLSerializer().serializeToString(svg);
        const inner = Array.from(svg.childNodes)
            .map(function (node) {
                return new XMLSerializer().serializeToString(node);
            })
            .join("");
        return { markup: markup, inner: inner };
    }

    function refreshSvgPaint(box) {
        if (!isSvgItem(box) || !box.inner) {
            return;
        }
        const sourceMarkup = box.svgMarkupOriginal || box.svgMarkup;
        if (!sourceMarkup) {
            return;
        }
        const liveSvg = mountSvgInto(box.inner, sourceMarkup);
        box.liveSvg = liveSvg;
        if (!liveSvg) {
            return;
        }
        if (box.fillOverride || box.strokeOverride) {
            paintSvgDocument(liveSvg, box);
            const painted = serializeSvgBoxMarkup(liveSvg);
            box.svgMarkup = painted.markup;
            box.svgInner = painted.inner;
        } else {
            box.svgMarkup = box.svgMarkupOriginal || box.svgMarkup;
            box.svgInner = box.svgInnerOriginal || box.svgInner;
        }
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

        const preview = document.createElement("div");
        preview.className = "design-textbox-preview d-none";
        preview.setAttribute("aria-hidden", "true");

        const input = document.createElement("textarea");
        input.className = "design-textbox-input";
        input.rows = 1;
        input.cols = 1;
        input.value = box.text;
        input.spellcheck = false;
        input.setAttribute("aria-label", "متن تکست باکس");

        el.appendChild(preview);
        el.appendChild(input);
        els.canvas.appendChild(el);

        box.el = el;
        box.input = input;
        box.preview = preview;

        attachCommonBoxChrome(box, el, "بزرگ/کوچک کردن فونت");

        input.addEventListener("focus", function () {
            selectBox(box.id);
        });

        input.addEventListener("input", function () {
            const oldText = box.text || "";
            const newText = input.value;
            box.charFills = remapCharFills(oldText, box.charFills, newText, box.fill);
            box.text = newText;
            rememberTextSelection(box);
            renderTextColorPreview(box);
            fitBoxToText(box);
            renderList();
            setStatus("متن به‌روز شد.");
        });

        function syncFillPickerFromSelection() {
            rememberTextSelection(box);
            if (suppressPanelSync || selectedId !== box.id) {
                return;
            }
            suppressPanelSync = true;
            els.fill.value = getActiveFillForPanel(box);
            syncSpectrumFromInput("fill", els.fill.value);
            suppressPanelSync = false;
        }

        input.addEventListener("select", syncFillPickerFromSelection);
        input.addEventListener("keyup", syncFillPickerFromSelection);
        input.addEventListener("mouseup", syncFillPickerFromSelection);
        input.addEventListener("blur", function () {
            rememberTextSelection(box);
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

        el.appendChild(inner);
        els.canvas.appendChild(el);

        box.el = el;
        box.input = null;
        box.inner = inner;
        box.liveSvg = null;

        attachCommonBoxChrome(box, el, "تغییر اندازه SVG");
        applySvgStyles(box);

        if (!box.liveSvg && box.inner && !box.inner.querySelector("svg")) {
            inner.textContent = "SVG قابل نمایش نیست";
            inner.style.color = "#b91c1c";
            inner.style.fontSize = "12px";
            inner.style.padding = "8px";
            inner.style.pointerEvents = "none";
        }
    }

    function addSvgItem(parsed, fileName) {
        const offset = (boxes.length % 8) * 28;
        const width = Math.max(8, Number(parsed.widthPx) || 100);
        const height = Math.max(8, Number(parsed.heightPx) || 100);
        const detected = detectSvgPaint(
            new DOMParser().parseFromString(parsed.markup, "image/svg+xml").documentElement
        );

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
            svgMarkupOriginal: parsed.markup,
            svgInnerOriginal: parsed.inner,
            unitsPerCm: parsed.unitsPerCm,
            fill: detected.fill,
            stroke: detected.stroke,
            strokeWidth: 0,
            originalFill: detected.fill,
            originalStroke: detected.stroke,
            fillOverride: false,
            strokeOverride: false,
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
        if (!box) {
            return;
        }
        if (isSvgItem(box)) {
            box.stroke = normalizeFillHex(els.stroke.value);
            box.strokeWidth = Number(els.strokeWidth.value) || 0;
            box.strokeOverride = true;
            els.strokeWidthValue.textContent = String(box.strokeWidth);
            syncSpectrumFromInput("stroke", box.stroke);
            applySvgStyles(box, { repaint: true });
            setStatus("حاشیه SVG به‌روز شد.");
            return;
        }
        box.fontFamily = els.font.value || "Vazir";
        box.fontSize = clampFontSize(Number(els.fontSize.value) || 48);
        box.letterSpacing = Number(els.letterSpacing.value) || 0;
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

    function applyFillColorToSelected() {
        if (suppressPanelSync) {
            return;
        }
        const box = getSelected();
        if (!box) {
            return;
        }
        if (isSvgItem(box)) {
            box.fill = normalizeFillHex(els.fill.value);
            box.fillOverride = true;
            syncSpectrumFromInput("fill", box.fill);
            applySvgStyles(box, { repaint: true });
            setStatus("رنگ پر SVG اعمال شد.");
            return;
        }
        applyFillFromPanel(box, els.fill.value);
        syncSpectrumFromInput("fill", els.fill.value);
        applyBoxStyles(box);
        fitBoxToText(box);
        renderList();
    }

    function applyStrokeColorToSelected() {
        if (suppressPanelSync) {
            return;
        }
        const box = getSelected();
        if (!box) {
            return;
        }
        if (isSvgItem(box)) {
            box.stroke = normalizeFillHex(els.stroke.value);
            box.strokeOverride = true;
            if (!(Number(box.strokeWidth) > 0)) {
                box.strokeWidth = 1;
                suppressPanelSync = true;
                els.strokeWidth.value = "1";
                els.strokeWidthValue.textContent = "1";
                suppressPanelSync = false;
            }
            syncSpectrumFromInput("stroke", box.stroke);
            applySvgStyles(box, { repaint: true });
            setStatus("رنگ حاشیه SVG اعمال شد.");
            return;
        }
        box.stroke = els.stroke.value;
        syncSpectrumFromInput("stroke", els.stroke.value);
        applyBoxStyles(box);
        setStatus("رنگ حاشیه اعمال شد.");
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
                applySvgStyles(box, { resize: true, repaint: !!box.strokeOverride });
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
        const hasStroke = (box.strokeWidth || 0) > 0;
        const strokeCls = hasStroke ? styles.strokeClass(box.stroke, box.strokeWidth) : "";
        const fontSize = pxToCorelUnits(box.fontSize);
        const letterSpacing = pxToCorelUnits(box.letterSpacing);
        const defaultFill = normalizeFillHex(box.fill || "#000000");
        const charFills = Array.isArray(box.charFills) ? box.charFills : null;

        let offset = 0;
        const tspans = measured.lines
            .map((line, index) => {
                const content = line.length ? line : " ";
                const y = pxToCorelUnits(localY0 + box.fontSize * 0.85 + index * measured.lineHeight);
                const lineWidth = measured.lineWidths ? measured.lineWidths[index] : measured.width;
                const x = pxToCorelUnits(box.x + boxWidth - 10 - lineWidth);
                const lineFills = charFills ? charFills.slice(offset, offset + line.length) : null;
                offset += line.length + 1;

                if (!lineFills || !hasMultiFill({ charFills: lineFills, fill: defaultFill, text: content })) {
                    const fillCls = styles.fillClass(defaultFill);
                    const className = hasStroke ? fillCls + " " + strokeCls : fillCls;
                    return `   <tspan class="${className}" x="${x}" y="${y}" fill="${escapeXml(defaultFill)}">${escapeXml(content)}</tspan>`;
                }

                // بازه‌های رنگی پشت‌سرهم
                let parts = "";
                let i = 0;
                let first = true;
                while (i < content.length) {
                    const fill = normalizeFillHex(lineFills[i] || defaultFill);
                    let j = i + 1;
                    while (j < content.length && normalizeFillHex(lineFills[j] || defaultFill) === fill) {
                        j++;
                    }
                    const fillCls = styles.fillClass(fill);
                    const className = hasStroke ? fillCls + " " + strokeCls : fillCls;
                    const chunk = content.slice(i, j);
                    if (first) {
                        parts += `   <tspan class="${className}" x="${x}" y="${y}" fill="${escapeXml(fill)}">${escapeXml(chunk)}</tspan>`;
                        first = false;
                    } else {
                        parts += `   <tspan class="${className}" fill="${escapeXml(fill)}">${escapeXml(chunk)}</tspan>`;
                    }
                    i = j;
                }
                return parts;
            })
            .join("\n");

        return (
            `  <text style="font-family:'${escapeXml(box.fontFamily || "Vazir")}',Tahoma,sans-serif;font-size:${fontSize};letter-spacing:${letterSpacing}" ` +
            `text-anchor="start" direction="rtl" unicode-bidi="bidi-override" xml:space="preserve">\n` +
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

    buildSpectrumPicker(els.fillSpectrum, "fill", function (hex) {
        if (suppressPanelSync) {
            return;
        }
        els.fill.value = hex;
        applyFillColorToSelected();
    });
    buildSpectrumPicker(els.strokeSpectrum, "stroke", function (hex) {
        if (suppressPanelSync) {
            return;
        }
        els.stroke.value = hex;
        applyStrokeColorToSelected();
    });
    syncSpectrumFromInput("fill", els.fill ? els.fill.value : "#111827");
    syncSpectrumFromInput("stroke", els.stroke ? els.stroke.value : "#111827");

    ["input", "change"].forEach(function (evt) {
        els.font.addEventListener(evt, applyPanelToSelected);
        els.fontSize.addEventListener(evt, applyPanelToSelected);
        els.letterSpacing.addEventListener(evt, applyPanelToSelected);
        els.fill.addEventListener(evt, applyFillColorToSelected);
        els.stroke.addEventListener(evt, applyStrokeColorToSelected);
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
