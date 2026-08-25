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
    const RULER_SIZE = 24;
    const CANVAS_W = 1200;
    const CANVAS_H = 800;
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
        return {
            x: Math.max(0, Math.min(CANVAS_W, point.x)),
            y: Math.max(0, Math.min(CANVAS_H, point.y)),
        };
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
        els.canvasScaler.style.width = CANVAS_W * zoom + "px";
        els.canvasScaler.style.height = CANVAS_H * zoom + "px";
        els.canvas.style.transform = "scale(" + zoom + ")";

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
                text: "متن جدید",
                x: 80 + offset,
                y: 80 + offset,
                width: 0,
                height: 0,
                manualSize: false,
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

    function labelForBox(box) {
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
            empty.textContent = "هنوز تکست باکسی نیست. «افزودن تکست باکس» را بزنید.";
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
            icon.className = "bi bi-cursor-text text-muted";

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
        const enabled = !!box;
        els.controls.classList.toggle("is-disabled", !enabled);
        els.deleteBtn.disabled = !enabled;
        els.selectionHint.textContent = enabled
            ? "داخل باکس تایپ کنید. نقطه آبی = جابه‌جایی. دستگیره‌های لبه = تغییر عرض و ارتفاع."
            : "یک تکست باکس انتخاب کنید یا جدید بسازید.";

        if (!box) {
            return;
        }

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
        const el = box.el;
        const input = box.input;
        const stack = getFontCssStack(box.fontFamily);
        el.style.setProperty("--box-font", stack);
        el.style.left = box.x + "px";
        el.style.top = box.y + "px";
        el.style.color = box.fill;
        el.style.webkitTextStroke = box.strokeWidth > 0 ? `${box.strokeWidth}px ${box.stroke}` : "0 transparent";
        input.style.fontFamily = stack;
        input.style.fontSize = box.fontSize + "px";
        input.style.letterSpacing = box.letterSpacing + "px";
        input.style.color = box.fill;
        if (input.value !== box.text) {
            input.value = box.text;
        }
        el.classList.toggle("is-selected", box.id === selectedId);
        fitBoxToText(box);
    }

    function createResizeHandle(direction, title) {
        const handle = document.createElement("div");
        handle.className = "design-textbox-resize design-textbox-resize-" + direction;
        handle.dataset.resize = direction;
        handle.title = title;
        return handle;
    }

    function createBoxElement(box) {
        const el = document.createElement("div");
        el.className = "design-textbox";
        el.dataset.boxId = String(box.id);

        const moveHandle = document.createElement("div");
        moveHandle.className = "design-textbox-handle";
        moveHandle.title = "جابه‌جایی";

        const resizeE = createResizeHandle("e", "تغییر عرض");
        const resizeS = createResizeHandle("s", "تغییر ارتفاع");
        const resizeSe = createResizeHandle("se", "تغییر عرض و ارتفاع");

        const input = document.createElement("textarea");
        input.className = "design-textbox-input";
        input.rows = 1;
        input.cols = 1;
        input.value = box.text;
        input.spellcheck = false;
        input.setAttribute("aria-label", "متن تکست باکس");

        el.appendChild(moveHandle);
        el.appendChild(resizeE);
        el.appendChild(resizeS);
        el.appendChild(resizeSe);
        el.appendChild(input);
        els.canvas.appendChild(el);

        box.el = el;
        box.input = input;
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
            if (event.target === input || event.target === moveHandle || event.target.classList.contains("design-textbox-resize")) {
                return;
            }
            beginDrag(box, event);
        });

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
        const padX = 18;
        const padY = 14;
        return {
            width: Math.max(MIN_BOX_WIDTH, Math.max(box.fontSize * 0.8, measured.width) + padX),
            height: Math.max(MIN_BOX_HEIGHT, Math.max(box.fontSize * 1.35, measured.height) + padY),
            measured,
        };
    }

    function fitBoxToText(box) {
        const input = box.input;
        const el = box.el;
        if (!input || !el) {
            return;
        }

        const minSize = getContentMinSize(box);
        if (!box.manualSize) {
            box.width = minSize.width;
            box.height = minSize.height;
        } else {
            // اگر متن بزرگ‌تر شد، کادر را حداقل به اندازه محتوا بزرگ کن
            box.width = Math.max(box.width || MIN_BOX_WIDTH, minSize.width);
            box.height = Math.max(box.height || MIN_BOX_HEIGHT, minSize.height);
        }

        applyBoxSize(box);
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
        els.canvas.querySelectorAll(".design-textbox").forEach(function (node) {
            node.remove();
        });
        if (els.fontFile) {
            els.fontFile.value = "";
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
        box.fontFamily = els.font.value || "Vazir";
        box.fontSize = Number(els.fontSize.value) || 48;
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
        resizeState = {
            box,
            direction,
            startX: event.clientX,
            startY: event.clientY,
            startWidth: box.width || box.el.offsetWidth,
            startHeight: box.height || box.el.offsetHeight,
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

            if (dir === "e" || dir === "se") {
                box.width = Math.max(MIN_BOX_WIDTH, resizeState.startWidth + dx);
            }
            if (dir === "s" || dir === "se") {
                box.height = Math.max(MIN_BOX_HEIGHT, resizeState.startHeight + dy);
            }

            box.manualSize = true;
            applyBoxSize(box);
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
    }

    function onPointerUp() {
        if (panState) {
            panState = null;
            updateStageCursorClass();
            setStatus("بوم جابه‌جا شد.");
            return;
        }
        if (resizeState) {
            resizeState.box.el.classList.remove("is-resizing");
            resizeState = null;
            setStatus("اندازه کادر به‌روز شد.");
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
        const lines = (text || " ").replace(/\r\n/g, "\n").split("\n");
        const lineHeight = Math.ceil(fontSize * 1.35);
        let maxWidth = 0;
        lines.forEach((line) => {
            const base = ctx.measureText(line || " ").width;
            const extra = Math.max(0, (line.length - 1) * letterSpacing);
            maxWidth = Math.max(maxWidth, base + extra);
        });
        return {
            width: Math.ceil(maxWidth),
            height: Math.ceil(Math.max(1, lines.length) * lineHeight),
            lineHeight,
            lines,
        };
    }

    function buildTextMarkup(box) {
        const text = (box.text || " ").replace(/\r\n/g, "\n");
        const measured = measureTextBox(text, box.fontFamily, box.fontSize, box.letterSpacing);
        const boxWidth = box.width || measured.width + 18;
        const anchorX = box.x + boxWidth - 10;
        const localY = box.y + 8;
        const tspans = measured.lines
            .map((line, index) => {
                const y = localY + box.fontSize * 0.85 + index * measured.lineHeight;
                return `<tspan x="${anchorX}" y="${y}">${escapeXml(line.length ? line : " ")}</tspan>`;
            })
            .join("");
        return (
            `  <text style="font-family: ${escapeXml(getFontCssStack(box.fontFamily))};" font-size="${box.fontSize}" ` +
            `fill="${escapeXml(box.fill)}" stroke="${escapeXml(box.stroke)}" stroke-width="${box.strokeWidth}" ` +
            `letter-spacing="${box.letterSpacing}" paint-order="stroke fill" text-anchor="end" ` +
            `direction="rtl" unicode-bidi="plaintext">\n` +
            `    ${tspans}\n` +
            `  </text>`
        );
    }

    function parseRgb(fillValue) {
        if (!fillValue) {
            return null;
        }
        const match = String(fillValue).match(/rgb\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)/i);
        if (!match) {
            return null;
        }
        return {
            r: Number(match[1]),
            g: Number(match[2]),
            b: Number(match[3]),
        };
    }

    function isBackgroundFill(fillValue) {
        const rgb = parseRgb(fillValue);
        if (!rgb) {
            return false;
        }
        return rgb.r + rgb.g + rgb.b >= 720;
    }

    function weldBoxToPaths(box) {
        if (typeof ImageTracer === "undefined") {
            return Promise.reject(new Error("کتابخانه Weld در دسترس نیست."));
        }

        return waitForFonts(box.fontFamily, box.fontSize).then(function () {
            const text = (box.text || " ").replace(/\r\n/g, "\n");
            const scale = 2;
            const pad = Math.ceil(Math.max(12, box.fontSize * 0.3, box.strokeWidth * 2));
            const measured = measureTextBox(text, box.fontFamily, box.fontSize, box.letterSpacing);
            const width = Math.max(1, measured.width + pad * 2);
            const height = Math.max(1, measured.height + pad * 2);
            const canvas = document.createElement("canvas");
            canvas.width = width * scale;
            canvas.height = height * scale;
            const ctx = canvas.getContext("2d");
            ctx.scale(scale, scale);
            ctx.fillStyle = "#ffffff";
            ctx.fillRect(0, 0, width, height);
            ctx.font = `${box.fontSize}px ${getFontCssStack(box.fontFamily)}`;
            ctx.textAlign = "right";
            ctx.textBaseline = "alphabetic";
            ctx.direction = "rtl";
            ctx.fillStyle = box.fill === "#ffffff" ? "#000000" : box.fill;
            ctx.strokeStyle = box.stroke === "#ffffff" ? "#000000" : box.stroke;
            ctx.lineWidth = box.strokeWidth;
            ctx.lineJoin = "round";
            if (ctx.letterSpacing !== undefined) {
                ctx.letterSpacing = `${box.letterSpacing}px`;
            }

            measured.lines.forEach((line, index) => {
                const x = width - pad;
                const y = pad + box.fontSize * 0.85 + index * measured.lineHeight;
                const content = line.length ? line : " ";
                if (box.strokeWidth > 0) {
                    ctx.strokeText(content, x, y);
                }
                ctx.fillText(content, x, y);
            });

            const traced = ImageTracer.imagedataToSVG(
                ctx.getImageData(0, 0, canvas.width, canvas.height),
                {
                    ltres: 0.8,
                    qtres: 0.8,
                    pathomit: 4,
                    colorsampling: 0,
                    numberofcolors: 2,
                    mincolorratio: 0,
                    colorquantcycles: 1,
                    blurradius: 0,
                    blurdelta: 20,
                    strokewidth: 0,
                    linefilter: true,
                    scale: 1,
                    roundcoords: 2,
                    viewbox: true,
                    desc: false,
                }
            );

            const doc = new DOMParser().parseFromString(traced, "image/svg+xml");
            const paths = Array.from(doc.querySelectorAll("path")).filter((path) => {
                return path.getAttribute("d") && !isBackgroundFill(path.getAttribute("fill"));
            });
            if (!paths.length) {
                throw new Error("مسیری از متن جوش‌خورده ساخته نشد.");
            }

            const pathMarkup = paths
                .map((path) => {
                    const d = path.getAttribute("d");
                    return (
                        `    <path d="${d}" fill="${escapeXml(box.fill)}" ` +
                        `stroke="${escapeXml(box.stroke)}" stroke-width="${box.strokeWidth}" fill-rule="evenodd"/>`
                    );
                })
                .join("\n");

            return {
                width: canvas.width,
                height: canvas.height,
                markup:
                    `  <g transform="translate(${box.x}, ${box.y}) scale(${1 / scale})">\n` +
                    `${pathMarkup}\n` +
                    `  </g>`,
            };
        });
    }

    function computeBounds() {
        let maxX = 400;
        let maxY = 300;
        boxes.forEach((box) => {
            const measured = measureTextBox(box.text || " ", box.fontFamily, box.fontSize, box.letterSpacing);
            maxX = Math.max(maxX, box.x + measured.width + 40);
            maxY = Math.max(maxY, box.y + measured.height + 40);
        });
        return {
            width: Math.ceil(maxX),
            height: Math.ceil(maxY),
        };
    }

    function buildExportSvg() {
        const bounds = computeBounds();
        const tasks = boxes.map((box) => {
            if (!box.weld) {
                return Promise.resolve(buildTextMarkup(box));
            }
            return weldBoxToPaths(box).then((result) => result.markup);
        });

        return Promise.all(tasks).then(function (parts) {
            return (
                `<?xml version="1.0" encoding="UTF-8"?>\n` +
                `<svg xmlns="http://www.w3.org/2000/svg" width="${bounds.width}" height="${bounds.height}" ` +
                `viewBox="0 0 ${bounds.width} ${bounds.height}">\n` +
                parts.join("\n") +
                `\n</svg>\n`
            );
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
            setStatus("ابتدا حداقل یک تکست باکس اضافه کنید.");
            return;
        }
        const hasText = boxes.some((box) => (box.text || "").trim());
        if (!hasText) {
            setStatus("حداقل یکی از تکست باکس‌ها باید متن داشته باشد.");
            return;
        }

        els.exportBtn.disabled = true;
        setStatus("در حال ساخت SVG…");
        buildExportSvg()
            .then(function (svg) {
                downloadSvg(svg, els.fileName.value);
                setStatus("فایل SVG دانلود شد.");
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

        const fontFace = new FontFace(familyName, `url(${objectUrl})`);
        setStatus("در حال بارگذاری فونت…");
        fontFace
            .load()
            .then(function (loaded) {
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

    applyZoom(1);
    addTextBox({ text: "طراحی" });
    setStatus("جابه‌جایی بوم: کشیدن روی فضای خالی، دکمه «جابه‌جایی»، Space+کشیدن، یا کلیک وسط ماوس. زوم حول مکان ماوس انجام می‌شود.");
})();
