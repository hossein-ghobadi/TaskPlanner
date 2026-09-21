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
        convertBtn: document.getElementById("convertToCurvesBtn"),
        breakApartBtn: document.getElementById("breakApartBtn"),
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

    const marqueeEl = document.createElement("div");
    marqueeEl.className = "design-marquee d-none";
    marqueeEl.setAttribute("aria-hidden", "true");
    els.canvas.appendChild(marqueeEl);
    els.marquee = marqueeEl;

    const customFonts = new Map();
    const boxes = [];
    let selectedId = null;
    let selectedIds = [];
    let editingId = null;
    let nextId = 1;
    let suppressPanelSync = false;
    let dragState = null;
    let resizeState = null;
    let panState = null;
    let marqueeState = null;
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
    const STROKE_SLIDER_MIN_MAX = 200;
    const RULER_SIZE = 28;
    const CANVAS_GROW_PAD = 400;
    const spectrumControllers = { fill: null, stroke: null };
    const ZOOM_MIN = 0.01;
    const ZOOM_MAX = 4;
    const ZOOM_STEP = 0.1;
    // استاندارد CSS: 96px = 1in
    const CSS_PPI = 96;
    const PX_PER_METER = CSS_PPI / 0.0254;
    const MAX_CANVAS_M = 10;
    const MAX_CANVAS_PX = Math.round(MAX_CANVAS_M * PX_PER_METER);
    const MIN_CANVAS_W = 1200;
    const MIN_CANVAS_H = 800;
    let canvasW = MIN_CANVAS_W;
    let canvasH = MIN_CANVAS_H;
    const UNIT_DEFS = {
        px: { id: "px", label: "px", pxPerUnit: 1, minor: 10, mid: 50, major: 100, decimals: 0 },
        mm: { id: "mm", label: "mm", pxPerUnit: CSS_PPI / 25.4, minor: 1, mid: 5, major: 10, decimals: 1 },
        cm: { id: "cm", label: "cm", pxPerUnit: CSS_PPI / 2.54, minor: 0.5, mid: 1, major: 5, decimals: 2 },
        m: { id: "m", label: "m", pxPerUnit: CSS_PPI / 0.0254, minor: 0.01, mid: 0.05, major: 0.1, decimals: 3 },
        in: { id: "in", label: "in", pxPerUnit: CSS_PPI, minor: 0.125, mid: 0.25, major: 1, decimals: 2 },
    };

    function getUnitConfig() {
        const key = (els.unitSelect && els.unitSelect.value) || "cm";
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
            x: Math.min(canvasW, Math.max(0, point.x)),
            y: Math.min(canvasH, Math.max(0, point.y)),
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
        const w = Math.min(MAX_CANVAS_PX, Math.max(MIN_CANVAS_W, Math.ceil(nextW)));
        const h = Math.min(MAX_CANVAS_PX, Math.max(MIN_CANVAS_H, Math.ceil(nextH)));
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
        const pct = value * 100;
        if (pct < 10) {
            return toFaDigits(pct.toFixed(1)) + "٪";
        }
        return toFaDigits(Math.round(pct)) + "٪";
    }

    function clampZoom(value) {
        const n = Number(value) || ZOOM_MIN;
        const stepped = n < 0.1 ? Math.round(n * 1000) / 1000 : Math.round(n * 100) / 100;
        return Math.min(ZOOM_MAX, Math.max(ZOOM_MIN, stepped));
    }

    function getFitZoom() {
        if (!els.stage) {
            return 1;
        }
        const pad = 32;
        const viewW = Math.max(160, els.stage.clientWidth - pad);
        const viewH = Math.max(160, els.stage.clientHeight - pad);
        return clampZoom(Math.min(1, viewW / Math.max(1, canvasW), viewH / Math.max(1, canvasH)));
    }

    function fitPageToView() {
        if (!els.canvas || !els.canvasScaler || !els.stage) {
            return;
        }
        zoom = 1;
        els.canvas.style.transform = "scale(1)";
        applyCanvasDomSize();
        updateStageGrid();
        els.stage.scrollLeft = 0;
        els.stage.scrollTop = 0;
        syncZoomUi();
        drawRulers();
        syncAllBoxChrome();
        setStatus("زوم ۱۰۰٪");
    }

    function updateStageGrid() {
        if (!els.stage) {
            return;
        }
        let grid = 24 * zoom;
        while (grid > 0 && grid < 18) {
            grid *= 2;
        }
        while (grid > 72) {
            grid /= 2;
        }
        if (!(grid > 0)) {
            grid = 24;
        }
        els.stage.style.backgroundSize = grid + "px " + grid + "px, " + grid + "px " + grid + "px, auto";
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
        updateStageGrid();

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
        syncAllBoxChrome();
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
        const factor = delta > 0 ? 1.2 : 1 / 1.2;
        applyZoom(zoom * factor, clientX, clientY);
    }

    function niceUnitStep(minUnits) {
        if (!(minUnits > 0) || !Number.isFinite(minUnits)) {
            return 1;
        }
        const pow = Math.pow(10, Math.floor(Math.log10(minUnits)));
        const n = minUnits / pow;
        if (n <= 1) {
            return 1 * pow;
        }
        if (n <= 2) {
            return 2 * pow;
        }
        if (n <= 5) {
            return 5 * pow;
        }
        return 10 * pow;
    }

    function getRulerTickStep(cfg, minPx) {
        const pxPer = cfg.pxPerUnit * zoom;
        if (!(pxPer > 0)) {
            return cfg.major || 1;
        }
        return niceUnitStep((minPx || 12) / pxPer);
    }

    function drawRulers() {
        if (!els.rulerH || !els.rulerV || !els.stage) {
            return;
        }

        const cfg = getUnitConfig();
        const tickStep = getRulerTickStep(cfg, 14);
        const labelStep = getRulerTickStep(cfg, 56);
        const minorPx = tickStep * cfg.pxPerUnit * zoom;
        if (!(minorPx > 0.5)) {
            return;
        }
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

        ctxH.fillStyle = "#f8fafc";
        ctxH.fillRect(0, 0, viewW, RULER_SIZE);
        ctxV.fillStyle = "#f8fafc";
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
            const unitValue = i * tickStep;
            const major = nearlyMultiple(unitValue, labelStep);
            const tick = major ? 14 : nearlyMultiple(unitValue, labelStep / 2) ? 10 : 6;
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
            const unitValue = i * tickStep;
            const major = nearlyMultiple(unitValue, labelStep);
            const tick = major ? 14 : nearlyMultiple(unitValue, labelStep / 2) ? 10 : 6;
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
        exitTextEdit();
        setSelection([], { silent: true });
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
            exitTextEdit();
            setSelection([], { silent: true });
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
        return false;
    }

    function isEmptyCanvasTarget(target) {
        return (
            target === els.stage ||
            target === els.canvas ||
            target === els.canvasScaler ||
            target === els.measureLayer ||
            (target && target.classList && target.classList.contains("design-marquee"))
        );
    }

    function canStartMarquee(event) {
        if (measureMode || panMode || spaceHeld) {
            return false;
        }
        if (event.button !== 0) {
            return false;
        }
        return isEmptyCanvasTarget(event.target);
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
        marqueeState = null;
        updateStageCursorClass();
    }

    function hideMarquee() {
        if (els.marquee) {
            els.marquee.classList.add("d-none");
            els.marquee.style.width = "0px";
            els.marquee.style.height = "0px";
        }
    }

    function updateMarqueeDom(x, y, w, h) {
        if (!els.marquee) {
            return;
        }
        els.marquee.style.left = x + "px";
        els.marquee.style.top = y + "px";
        els.marquee.style.width = Math.max(0, w) + "px";
        els.marquee.style.height = Math.max(0, h) + "px";
    }

    function beginMarquee(event) {
        event.preventDefault();
        const point = getCanvasPoint(event);
        const additive = !!selectionModifier(event);
        marqueeState = {
            startX: point.x,
            startY: point.y,
            additive,
            moved: false,
            startClientX: event.clientX,
            startClientY: event.clientY,
            baseIds: additive ? selectedIds.slice() : [],
        };
        dragState = null;
        resizeState = null;
        if (!additive) {
            setSelection([], { silent: true });
        }
        if (els.marquee) {
            els.marquee.classList.remove("d-none");
            updateMarqueeDom(point.x, point.y, 0, 0);
        }
    }

    function applyMarqueeSelection(point) {
        const rect = {
            x: Math.min(marqueeState.startX, point.x),
            y: Math.min(marqueeState.startY, point.y),
            width: Math.abs(point.x - marqueeState.startX),
            height: Math.abs(point.y - marqueeState.startY),
        };
        updateMarqueeDom(rect.x, rect.y, rect.width, rect.height);
        const hitIds = boxes
            .filter(function (box) {
                return rectsIntersect(rect, getBoxBounds(box));
            })
            .map(function (box) {
                return box.id;
            });
        const ids = marqueeState.baseIds.slice();
        hitIds.forEach(function (id) {
            if (ids.indexOf(id) < 0) {
                ids.push(id);
            }
        });
        setSelection(ids, { silent: true });
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

    function isBoxSelected(id) {
        return selectedIds.indexOf(id) >= 0;
    }

    function getSelectedBoxes() {
        return selectedIds
            .map(function (id) {
                return boxes.find((box) => box.id === id) || null;
            })
            .filter(Boolean);
    }

    function getBoxBounds(box) {
        let w = Number(box.width) || 0;
        let h = Number(box.height) || 0;
        if (box.el) {
            w = Math.max(w, box.el.offsetWidth || 0);
            h = Math.max(h, box.el.offsetHeight || 0);
        }
        return {
            x: box.x || 0,
            y: box.y || 0,
            width: Math.max(1, w),
            height: Math.max(1, h),
        };
    }

    function rectsIntersect(a, b) {
        return a.x < b.x + b.width && a.x + a.width > b.x && a.y < b.y + b.height && a.y + a.height > b.y;
    }

    function selectionModifier(event) {
        if (event.ctrlKey || event.metaKey) {
            return "toggle";
        }
        if (event.shiftKey) {
            return "add";
        }
        return "";
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
            return (box.name || (box.convertedFromText ? "منحنی" : "SVG")).slice(0, 28);
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
            empty.className = "design-box-list-empty";
            empty.textContent = "هنوز لایه‌ای نیست. از نوار بالا «متن» یا «SVG» اضافه کنید.";
            els.boxList.appendChild(empty);
            return;
        }

        boxes.forEach((box, index) => {
            const item = document.createElement("button");
            item.type = "button";
            item.className = "design-box-list-item" + (isBoxSelected(box.id) ? " active" : "");

            const label = document.createElement("span");
            label.textContent = `#${index + 1} — ${labelForBox(box)}`;

            const icon = document.createElement("i");
            icon.className = isSvgItem(box)
                ? box.convertedFromText
                    ? "bi bi-bezier2 text-muted"
                    : "bi bi-filetype-svg text-muted"
                : "bi bi-cursor-text text-muted";

            item.appendChild(label);
            item.appendChild(icon);
            item.addEventListener("click", function (event) {
                const modifier = selectionModifier(event);
                if (modifier === "toggle") {
                    selectBox(box.id, { toggle: true });
                    return;
                }
                if (modifier === "add") {
                    selectRangeTo(box.id);
                    return;
                }
                selectBox(box.id);
            });
            els.boxList.appendChild(item);
        });
    }

    function syncPanelFromSelected() {
        const selectedCount = selectedIds.length;
        const box = getSelected();
        const hasBox = !!box;
        const svgMode = selectedCount > 0 && getSelectedBoxes().every(isSvgItem);
        els.controls.classList.toggle("is-disabled", !hasBox);
        els.controls.classList.toggle("is-svg-mode", svgMode);
        els.deleteBtn.disabled = !hasBox;
        if (els.stage) {
            els.stage.classList.toggle("has-multi-selection", selectedCount > 1);
        }
        if (!box) {
            els.selectionHint.textContent = "یک آیتم انتخاب کنید، تکست بسازید یا SVG آپلود کنید.";
            if (els.breakApartBtn) {
                els.breakApartBtn.disabled = true;
            }
            if (els.convertBtn) {
                els.convertBtn.disabled = true;
            }
            return;
        }
        if (selectedCount > 1) {
            els.selectionHint.textContent =
                toFaDigits(selectedCount) +
                " مورد انتخاب شد. بکشید تا با هم جابه‌جا شوند. Shift/Ctrl+کلیک برای افزودن یا حذف.";
            if (els.breakApartBtn) {
                els.breakApartBtn.disabled = true;
            }
            if (els.convertBtn) {
                els.convertBtn.disabled = true;
            }
            suppressPanelSync = true;
            els.fill.value = normalizeFillHex(box.fill || box.originalFill || "#111827");
            els.stroke.value = normalizeFillHex(box.stroke || box.originalStroke || "#111827");
            setStrokeWidthControls(box.strokeWidth || 0);
            if (!isSvgItem(box)) {
                els.font.value = box.fontFamily;
                els.fontSize.value = String(box.fontSize);
                els.fontSizeValue.textContent = String(box.fontSize);
            }
            syncSpectrumFromInput("fill", els.fill.value);
            syncSpectrumFromInput("stroke", els.stroke.value);
            suppressPanelSync = false;
            return;
        }
        if (els.convertBtn) {
            els.convertBtn.disabled = isSvgItem(box);
        }
        if (svgMode) {
            els.selectionHint.textContent = box.convertedFromText
                ? "شکل انتخاب شد. بکشید تا جابه‌جا شود. با «جدا کردن اجزا» نقطه‌ها را از بدنه جدا کنید."
                : "شکل انتخاب شد. بکشید تا جابه‌جا شود. دستگیره‌ها اندازه را عوض می‌کنند.";
            suppressPanelSync = true;
            els.fill.value = normalizeFillHex(box.fill || box.originalFill || "#111827");
            els.stroke.value = normalizeFillHex(box.stroke || box.originalStroke || "#111827");
            setStrokeWidthControls(box.strokeWidth || 0);
            syncSpectrumFromInput("fill", els.fill.value);
            syncSpectrumFromInput("stroke", els.stroke.value);
            suppressPanelSync = false;
            updateBreakApartButton(box);
            return;
        }

        if (els.breakApartBtn) {
            els.breakApartBtn.disabled = true;
        }

        els.selectionHint.textContent =
            "برای جابه‌جایی بکشید. برای ویرایش متن دوبار کلیک کنید. در حالت ویرایش می‌توانید بخشی از متن را رنگ کنید.";

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
        els.fill.value = getActiveFillForPanel(box);
        els.stroke.value = box.stroke;
        setStrokeWidthControls(box.strokeWidth);
        els.fontSizeValue.textContent = String(box.fontSize);
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

    function forceMulticolorPreviewPaint(box) {
        const preview = box && box.preview;
        if (!preview || preview.classList.contains("d-none")) {
            return;
        }
        // بوم با transform:scale رندر می‌شود؛ بدون این، گاهی فقط کادر بزرگ می‌شود
        // و گلیف‌های preview تا تعامل بعدی (کلیک) در اندازه قبلی می‌مانند.
        preview.style.transform = "translateZ(0)";
        void preview.offsetWidth;
        preview.style.removeProperty("transform");
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

        const fontSizePx = box.fontSize + "px";
        const letterSpacingPx = box.letterSpacing + "px";
        const fills = ensureCharFills(box);
        let html = "";
        let i = 0;
        while (i < text.length) {
            const fill = normalizeFillHex(fills[i] || box.fill);
            let j = i + 1;
            while (j < text.length && normalizeFillHex(fills[j] || box.fill) === fill) {
                j++;
            }
            // font-size صریح روی span — inherit زیر transform بوم گاهی به‌روز نمی‌شود
            html +=
                '<span style="color:' +
                fill +
                ";font-size:" +
                fontSizePx +
                ";letter-spacing:" +
                letterSpacingPx +
                ';line-height:1.35">' +
                escapeHtml(text.slice(i, j)).replace(/\n/g, "<br>") +
                "</span>";
            i = j;
        }
        preview.innerHTML = html || "&nbsp;";
        preview.classList.remove("d-none");
        input.classList.add("is-multicolor");
        input.style.color = "transparent";
        input.style.webkitTextFillColor = "transparent";
        forceMulticolorPreviewPaint(box);
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
        applyOutsideTextStroke(el, box.strokeWidth, box.stroke);
        input.style.fontFamily = stack;
        input.style.fontSize = box.fontSize + "px";
        input.style.letterSpacing = box.letterSpacing + "px";
        input.style.lineHeight = "1.35";
        applyOutsideTextStroke(input, hasMultiFill(box) ? 0 : box.strokeWidth, box.stroke);
        if (input.value !== box.text) {
            input.value = box.text;
        }
        if (box.preview) {
            box.preview.style.fontFamily = stack;
            box.preview.style.fontSize = box.fontSize + "px";
            box.preview.style.letterSpacing = box.letterSpacing + "px";
            applyOutsideTextStroke(box.preview, box.strokeWidth, box.stroke);
        }
        renderTextColorPreview(box);
        if (!hasMultiFill(box)) {
            input.style.color = box.fill;
        }
        el.classList.toggle("is-selected", isBoxSelected(box.id));
        el.classList.toggle("is-editing", box.id === editingId);
        fitBoxToText(box);
        syncBoxChromeSize(box);
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
        el.classList.toggle("is-selected", isBoxSelected(box.id));
        el.classList.toggle(
            "has-outside-stroke",
            !!(box.strokeOverride && Number(box.strokeWidth) > 0)
        );
        syncBoxChromeSize(box);
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

    function isInvisibleFrameRect(el) {
        const tag = svgNodeTag(el);
        if (tag !== "rect") {
            return false;
        }
        if (el.getAttribute("data-tp-frame") === "1") {
            return true;
        }
        const styleFill = extractCssColor(el.getAttribute("style"), "fill");
        const styleStroke = extractCssColor(el.getAttribute("style"), "stroke");
        const fillAttr = el.getAttribute("fill") || styleFill;
        if (!fillAttr) {
            return false;
        }
        const strokeAttr = el.getAttribute("stroke") || styleStroke || "none";
        return isPaintNone(fillAttr) && isPaintNone(strokeAttr);
    }

    function stripInvisibleFrameRects(root) {
        if (!root || !root.querySelectorAll) {
            return;
        }
        Array.from(root.querySelectorAll("rect")).forEach(function (el) {
            if (isInvisibleFrameRect(el)) {
                el.parentNode && el.parentNode.removeChild(el);
            }
        });
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
            if (el.closest("[data-outside-stroke='1']") || isInvisibleFrameRect(el)) {
                continue;
            }
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

    function outsideStrokeWidth(width) {
        return Math.max(0, Number(width) || 0) * 2;
    }

    function applyOutsideTextStroke(target, width, color) {
        if (!target || !target.style) {
            return;
        }
        const w = Math.max(0, Number(width) || 0);
        if (w > 0) {
            // fill روی stroke کشیده می‌شود تا نیمهٔ داخلی پوشانده شود
            target.style.paintOrder = "stroke fill";
            target.style.webkitTextStroke = outsideStrokeWidth(w) + "px " + color;
        } else {
            target.style.paintOrder = "";
            target.style.webkitTextStroke = "0 transparent";
        }
    }

    function svgGraphicKeepOut() {
        return {
            defs: 1,
            style: 1,
            title: 1,
            desc: 1,
            metadata: 1,
            clippath: 1,
            mask: 1,
            filter: 1,
            lineargradient: 1,
            radialgradient: 1,
            pattern: 1,
        };
    }

    function svgNodeTag(node) {
        return String((node && node.tagName) || "")
            .toLowerCase()
            .replace(/^.*:/, "");
    }

    function clearOutsideStrokeLayer(svg) {
        if (!svg || !svg.querySelectorAll) {
            return;
        }
        Array.from(svg.querySelectorAll("[data-outside-stroke='1']")).forEach(function (node) {
            node.remove();
        });
    }

    function stripElementStroke(el) {
        el.setAttribute("stroke", "none");
        el.removeAttribute("stroke-width");
        if (el.style) {
            el.style.stroke = "none";
            el.style.strokeWidth = "0";
        }
    }

    function listSvgGraphicChildren(svg) {
        const keepOut = svgGraphicKeepOut();
        const nodes = [];
        Array.from(svg.childNodes).forEach(function (node) {
            if (node.nodeType !== 1) {
                return;
            }
            if (keepOut[svgNodeTag(node)] || node.getAttribute("data-outside-stroke") === "1") {
                return;
            }
            nodes.push(node);
        });
        return nodes;
    }

    /**
     * حاشیه را پشت fill می‌کشد با ضخامت دو برابر تا فقط بیرون شکل دیده شود.
     * fill روی لایهٔ اصلی، درز اتصال حروف هم‌پوشان را هم می‌پوشاند.
     */
    function applyOutsideStrokeLayer(svg, stroke, strokeWidthUnits) {
        clearOutsideStrokeLayer(svg);
        const shapeSelector = "path, polygon, polyline, rect, circle, ellipse, line, text, tspan, use";
        Array.from(svg.querySelectorAll(shapeSelector)).forEach(function (el) {
            if (el.closest("[data-outside-stroke='1']")) {
                return;
            }
            stripElementStroke(el);
        });
        if (!(strokeWidthUnits > 0)) {
            return;
        }

        const graphicNodes = listSvgGraphicChildren(svg);
        if (!graphicNodes.length) {
            return;
        }

        const ns = "http://www.w3.org/2000/svg";
        const doc = svg.ownerDocument || document;
        const strokeGroup = doc.createElementNS(ns, "g");
        const paintedWidth = String(outsideStrokeWidth(strokeWidthUnits));
        strokeGroup.setAttribute("data-outside-stroke", "1");
        strokeGroup.setAttribute("fill", "none");
        strokeGroup.setAttribute("stroke", stroke);
        strokeGroup.setAttribute("stroke-width", paintedWidth);
        strokeGroup.setAttribute("stroke-linejoin", "round");
        strokeGroup.setAttribute("stroke-linecap", "round");
        strokeGroup.setAttribute("pointer-events", "none");

        graphicNodes.forEach(function (node) {
            strokeGroup.appendChild(node.cloneNode(true));
        });
        stripInvisibleFrameRects(strokeGroup);

        Array.from(strokeGroup.querySelectorAll("*")).forEach(function (el) {
            el.setAttribute("fill", "none");
            el.setAttribute("stroke", stroke);
            el.setAttribute("stroke-width", paintedWidth);
            el.setAttribute("stroke-linejoin", "round");
            el.setAttribute("stroke-linecap", "round");
            el.removeAttribute("class");
            if (el.style) {
                el.style.fill = "none";
                el.style.stroke = stroke;
                el.style.strokeWidth = paintedWidth;
            }
        });

        svg.insertBefore(strokeGroup, graphicNodes[0]);
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
                // stroke روی لایهٔ جداگانه است؛ کلاس‌های اصلی نباید حاشیهٔ داخلی بکشند
                css = css.replace(/stroke\s*:\s*([^;}{]+)/gi, function (full, value) {
                    if (isPaintNone(value) || /^url\(/i.test(value)) {
                        return full;
                    }
                    return "stroke:none";
                });
                if (/stroke-width\s*:/i.test(css)) {
                    css = css.replace(/stroke-width\s*:\s*[^;}{]+/gi, "stroke-width:0");
                }
            }
            styleEl.textContent = css;
        });

        const shapes = svg.querySelectorAll(
            "path, polygon, polyline, rect, circle, ellipse, line, text, tspan"
        );
        Array.from(shapes).forEach(function (el) {
            if (el.closest("[data-outside-stroke='1']") || isInvisibleFrameRect(el)) {
                return;
            }
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
        });

        if (strokeOverride) {
            applyOutsideStrokeLayer(svg, stroke, strokeWidthPx > 0 ? strokeWidthUnits : 0);
        }
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
        const liveSvg = mountSvgInto(box.inner, sourceMarkup, {
            preserveAspectRatio: box.convertedFromText ? "none" : "xMidYMid meet",
        });
        box.liveSvg = liveSvg;
        if (!liveSvg) {
            return;
        }
        stripInvisibleFrameRects(liveSvg);
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

    function syncBoxChromeSize(box) {
        if (!box || !box.el) {
            return;
        }
        const w = Math.max(1, Number(box.width) || box.el.offsetWidth || 1);
        const h = Math.max(1, Number(box.height) || box.el.offsetHeight || 1);
        const visualMin = Math.min(w, h) * zoom;
        const screen = Math.max(16, Math.min(44, visualMin * 0.08));
        const z = Math.max(0.01, zoom);
        const size = screen / z;
        const border = Math.max(1, 2 / z);
        box.el.style.setProperty("--chrome-size", size + "px");
        box.el.style.setProperty("--chrome-nudge", size / 2 + "px");
        box.el.style.setProperty("--chrome-border", border + "px");
    }

    function syncAllBoxChrome() {
        boxes.forEach(syncBoxChromeSize);
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
        syncBoxChromeSize(box);

        moveHandle.addEventListener("mousedown", function (event) {
            beginDrag(box, event, { fromHandle: true });
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
            if (box.input && editingId === box.id && event.target === box.input) {
                return;
            }
            beginDrag(box, event);
        });

        el.addEventListener("dblclick", function (event) {
            if (isSvgItem(box) || !box.input) {
                return;
            }
            event.preventDefault();
            event.stopPropagation();
            enterTextEdit(box);
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
            if (editingId !== box.id) {
                enterTextEdit(box);
                return;
            }
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
            if (editingId !== box.id) {
                return;
            }
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
        if (/m\s*$/i.test(str)) {
            return num * (CSS_PPI / 0.0254);
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
        if (/m\s*$/i.test(str)) {
            return declaredVb.width / (num * 100);
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

    function maxStrokeExtent(root) {
        let max = 0;
        const readWidth = function (el) {
            if (!el || !el.getAttribute) {
                return;
            }
            const attr = parseFloat(el.getAttribute("stroke-width"));
            if (Number.isFinite(attr) && attr > max) {
                max = attr;
            }
            const style = el.getAttribute("style") || "";
            const m = style.match(/stroke-width\s*:\s*([\d.]+)/i);
            if (m) {
                const n = parseFloat(m[1]);
                if (Number.isFinite(n) && n > max) {
                    max = n;
                }
            }
        };
        readWidth(root);
        Array.from(
            root.querySelectorAll("path, polygon, polyline, rect, circle, ellipse, line, text, g, use")
        ).forEach(readWidth);
        return max * 0.5;
    }

    function elementBBoxInSvgUserSpace(el, svg) {
        if (!el || typeof el.getBBox !== "function") {
            return null;
        }
        const b = el.getBBox();
        if (!b || !Number.isFinite(b.x) || !Number.isFinite(b.y)) {
            return null;
        }
        const width = Number.isFinite(b.width) ? b.width : 0;
        const height = Number.isFinite(b.height) ? b.height : 0;
        if (width === 0 && height === 0) {
            return null;
        }
        const ctm = typeof el.getCTM === "function" ? el.getCTM() : null;
        const rootCtm = typeof svg.getScreenCTM === "function" ? svg.getScreenCTM() : null;
        if (!ctm || !rootCtm || typeof svg.createSVGPoint !== "function") {
            return { x: b.x, y: b.y, width: width, height: height };
        }
        let toUser;
        try {
            toUser = rootCtm.inverse().multiply(ctm);
        } catch (e) {
            return { x: b.x, y: b.y, width: width, height: height };
        }
        const corners = [
            [b.x, b.y],
            [b.x + width, b.y],
            [b.x, b.y + height],
            [b.x + width, b.y + height],
        ];
        let minX = Infinity;
        let minY = Infinity;
        let maxX = -Infinity;
        let maxY = -Infinity;
        corners.forEach(function (c) {
            const p = svg.createSVGPoint();
            p.x = c[0];
            p.y = c[1];
            const t = p.matrixTransform(toUser);
            minX = Math.min(minX, t.x);
            minY = Math.min(minY, t.y);
            maxX = Math.max(maxX, t.x);
            maxY = Math.max(maxY, t.y);
        });
        if (!(maxX > minX && maxY > minY)) {
            return null;
        }
        return { x: minX, y: minY, width: maxX - minX, height: maxY - minY };
    }

    function measureSvgContentBox(svg) {
        const host = document.createElement("div");
        host.style.cssText =
            "position:absolute;left:-99999px;top:0;width:800px;height:800px;overflow:visible;opacity:0;pointer-events:none;";
        const clone = document.importNode(svg, true);
        if (!clone.getAttribute("width")) {
            clone.setAttribute("width", "800");
        }
        if (!clone.getAttribute("height")) {
            clone.setAttribute("height", "800");
        }
        host.appendChild(clone);
        document.body.appendChild(host);

        try {
            let box = null;
            if (typeof clone.getBBox === "function") {
                try {
                    const b = clone.getBBox();
                    if (b && b.width > 0.01 && b.height > 0.01) {
                        box = { x: b.x, y: b.y, width: b.width, height: b.height };
                    }
                } catch (e) {
                    box = null;
                }
            }

            if (!box) {
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
                        if (isInvisibleFrameRect(el) || el.closest("[data-outside-stroke='1']")) {
                            return;
                        }
                        const b = elementBBoxInSvgUserSpace(el, clone);
                        if (!b) {
                            return;
                        }
                        found = true;
                        minX = Math.min(minX, b.x);
                        minY = Math.min(minY, b.y);
                        maxX = Math.max(maxX, b.x + b.width);
                        maxY = Math.max(maxY, b.y + b.height);
                    } catch (e) {
                        // نادیده
                    }
                });
                if (found && maxX > minX && maxY > minY) {
                    box = { x: minX, y: minY, width: maxX - minX, height: maxY - minY };
                }
            }

            if (!box) {
                return null;
            }
            const pad = Math.max(2, maxStrokeExtent(clone));
            return {
                x: box.x - pad,
                y: box.y - pad,
                width: box.width + pad * 2,
                height: box.height + pad * 2,
            };
        } catch (e) {
            return null;
        } finally {
            host.remove();
        }
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

    function mountSvgInto(container, markup, options) {
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
        liveSvg.setAttribute(
            "preserveAspectRatio",
            (options && options.preserveAspectRatio) || "xMidYMid meet"
        );
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
        el.className = "design-svgbox" + (box.convertedFromText ? " is-curve" : "");
        el.dataset.boxId = String(box.id);

        const inner = document.createElement("div");
        inner.className = "design-svgbox-inner";

        el.appendChild(inner);
        els.canvas.appendChild(el);

        box.el = el;
        box.input = null;
        box.inner = inner;
        box.liveSvg = null;

        attachCommonBoxChrome(box, el, box.convertedFromText ? "تغییر اندازه شکل" : "تغییر اندازه SVG");
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
        const width = Math.min(MAX_CANVAS_PX, Math.max(8, Number(parsed.widthPx) || 100));
        const height = Math.min(MAX_CANVAS_PX, Math.max(8, Number(parsed.heightPx) || 100));
        const hasOrigin = Number.isFinite(parsed.originX) && Number.isFinite(parsed.originY);
        const x = hasOrigin ? Math.max(0, parsed.originX) : 100 + offset;
        const y = hasOrigin ? Math.max(0, parsed.originY) : 100 + offset;
        const detected = detectSvgPaint(
            new DOMParser().parseFromString(parsed.markup, "image/svg+xml").documentElement
        );

        const box = {
            id: nextId++,
            kind: "svg",
            name: (fileName || "svg").replace(/\.svg$/i, ""),
            x: x,
            y: y,
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
            convertedFromText: !!parsed.fromTaskPlanner,
        };
        boxes.push(box);
        createSvgElement(box);
        ensureCanvasFits(x + width + CANVAS_GROW_PAD, y + height + CANVAS_GROW_PAD);
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
        stripInvisibleFrameRects(svg);
        if (!svg.getAttribute("xmlns")) {
            svg.setAttribute("xmlns", "http://www.w3.org/2000/svg");
        }

        // مقیاس فیزیکی را قبل از نرمال‌سازی از روی محتوا + واحدهای فایل بخوان
        // (نه maxSide دلخواه؛ ابعاد روی بوم = همان cm/mm فایل)
        const declaredVb = readDeclaredViewBox(svg);
        const unitsPerCm = inferUnitsPerCm(svg, declaredVb);
        const sheetWpx = parseSvgLength(svg.getAttribute("width"), 0);
        const sheetHpx = parseSvgLength(svg.getAttribute("height"), 0);
        const restored = restoreTpBoxes(svg);

        let widthPx;
        let heightPx;
        let originX = null;
        let originY = null;
        let viewBox;
        let fromTaskPlanner = false;
        const pxPerCm = CSS_PPI / 2.54;

        if (restored && restored.multi) {
            fromTaskPlanner = true;
            widthPx = sheetWpx > 0 ? sheetWpx : declaredVb ? (declaredVb.width / (unitsPerCm || 1000)) * pxPerCm : 100;
            heightPx = sheetHpx > 0 ? sheetHpx : declaredVb ? (declaredVb.height / (unitsPerCm || 1000)) * pxPerCm : 100;
            originX = 0;
            originY = 0;
            viewBox = declaredVb || { x: 0, y: 0, width: widthPx, height: heightPx };
            svg.setAttribute("preserveAspectRatio", "none");
            svg.setAttribute("overflow", "visible");
        } else if (restored) {
            fromTaskPlanner = true;
            widthPx = restored.width;
            heightPx = restored.height;
            originX = restored.x;
            originY = restored.y;
            viewBox = restored.viewBox;
        } else {
            const contentBox = measureSvgContentBox(svg);
            if (contentBox && contentBox.width > 0 && contentBox.height > 0) {
                if (sheetWpx > 0 && sheetHpx > 0 && declaredVb && declaredVb.width > 0 && declaredVb.height > 0) {
                    widthPx = sheetWpx * (contentBox.width / declaredVb.width);
                    heightPx = sheetHpx * (contentBox.height / declaredVb.height);
                    originX = sheetWpx * ((contentBox.x - declaredVb.x) / declaredVb.width);
                    originY = sheetHpx * ((contentBox.y - declaredVb.y) / declaredVb.height);
                } else if (unitsPerCm == null) {
                    widthPx = contentBox.width;
                    heightPx = contentBox.height;
                    originX = contentBox.x;
                    originY = contentBox.y;
                } else {
                    widthPx = (contentBox.width / unitsPerCm) * pxPerCm;
                    heightPx = (contentBox.height / unitsPerCm) * pxPerCm;
                    originX = (contentBox.x / unitsPerCm) * pxPerCm;
                    originY = (contentBox.y / unitsPerCm) * pxPerCm;
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

            if (widthPx > MAX_CANVAS_PX || heightPx > MAX_CANVAS_PX) {
                const scale = Math.min(MAX_CANVAS_PX / Math.max(1, widthPx), MAX_CANVAS_PX / Math.max(1, heightPx));
                widthPx *= scale;
                heightPx *= scale;
            }

            viewBox = normalizeSvgContentIntoFrame(svg);
            svg.setAttribute("preserveAspectRatio", "xMidYMid meet");
        }

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
            originX: originX,
            originY: originY,
            unitsPerCm: unitsPerCm,
            fromTaskPlanner: fromTaskPlanner,
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
        // padding اینپوت: 6px 10px + border کادر ۱px + حاشیهٔ بیرونی
        const strokePad = Math.max(0, Number(box.strokeWidth) || 0);
        const padX = 20 + 2 + strokePad * 2;
        const padY = 12 + 2 + strokePad * 2;
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
        if (els.strokeWidth) {
            setStrokeWidthControls(box.strokeWidth);
        }
        suppressPanelSync = false;
    }

    function exitTextEdit() {
        if (!editingId) {
            return;
        }
        const box = boxes.find((item) => item.id === editingId);
        editingId = null;
        if (box && box.el) {
            box.el.classList.remove("is-editing");
        }
        const active = document.activeElement;
        if (box && box.input && active === box.input) {
            box.input.blur();
        }
    }

    function enterTextEdit(box) {
        if (!box || isSvgItem(box) || !box.input) {
            return;
        }
        if (editingId && editingId !== box.id) {
            exitTextEdit();
        }
        editingId = box.id;
        selectBox(box.id);
        box.el.classList.add("is-editing");
        box.input.focus();
        try {
            const len = box.input.value.length;
            box.input.setSelectionRange(0, len);
        } catch (err) {
            /* ignore */
        }
        setStatus("در حال ویرایش متن. برای جابه‌جایی، بیرون از متن کلیک کنید و بکشید.");
    }

    function updateSelectionStatus() {
        const count = selectedIds.length;
        if (!count) {
            setStatus("");
            return;
        }
        if (count > 1) {
            setStatus(
                toFaDigits(count) +
                    " مورد انتخاب شد. برای جابه‌جایی بکشید. Shift یا Ctrl+کلیک برای افزودن."
            );
            return;
        }
        const selected = getSelected();
        if (!selected) {
            setStatus("");
            return;
        }
        if (isSvgItem(selected)) {
            setStatus("شکل انتخاب شد. برای جابه‌جایی بکشید.");
            return;
        }
        setStatus("انتخاب شد. برای جابه‌جایی بکشید. برای ویرایش متن دوبار کلیک کنید.");
    }

    function setSelection(ids, options) {
        const unique = [];
        (ids || []).forEach(function (id) {
            if (id == null || unique.indexOf(id) >= 0) {
                return;
            }
            if (boxes.some((box) => box.id === id)) {
                unique.push(id);
            }
        });
        selectedIds = unique;
        selectedId = unique.length ? unique[unique.length - 1] : null;
        if (editingId && editingId !== selectedId) {
            exitTextEdit();
        }
        boxes.forEach((box) => applyBoxStyles(box));
        renderList();
        syncPanelFromSelected();
        if (options && options.silent) {
            return;
        }
        updateSelectionStatus();
    }

    function selectRangeTo(id) {
        const fromIndex = boxes.findIndex((box) => box.id === selectedId);
        const toIndex = boxes.findIndex((box) => box.id === id);
        if (fromIndex < 0 || toIndex < 0) {
            selectBox(id, { add: true });
            return;
        }
        const start = Math.min(fromIndex, toIndex);
        const end = Math.max(fromIndex, toIndex);
        const rangeIds = boxes.slice(start, end + 1).map((box) => box.id);
        const next = selectedIds.slice();
        rangeIds.forEach(function (rid) {
            if (next.indexOf(rid) < 0) {
                next.push(rid);
            }
        });
        setSelection(next.filter((item) => item !== id).concat([id]));
    }

    function selectBox(id, options) {
        options = options || {};
        if (id == null) {
            setSelection([], options);
            return;
        }
        if (options.toggle) {
            if (isBoxSelected(id)) {
                setSelection(
                    selectedIds.filter((item) => item !== id),
                    options
                );
            } else {
                setSelection(selectedIds.concat([id]), options);
            }
            return;
        }
        if (options.add) {
            if (isBoxSelected(id)) {
                setSelection(
                    selectedIds.filter((item) => item !== id).concat([id]),
                    options
                );
            } else {
                setSelection(selectedIds.concat([id]), options);
            }
            return;
        }
        setSelection([id], options);
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
        const items = getSelectedBoxes();
        if (!items.length) {
            return;
        }
        exitTextEdit();
        const idSet = {};
        items.forEach(function (box) {
            idSet[box.id] = true;
            if (box.el) {
                box.el.remove();
            }
        });
        for (let i = boxes.length - 1; i >= 0; i--) {
            if (idSet[boxes[i].id]) {
                boxes.splice(i, 1);
            }
        }
        setSelection([]);
        setStatus(items.length > 1 ? toFaDigits(items.length) + " مورد حذف شد." : "آیتم حذف شد.");
    }

    function clearDesign() {
        exitTextEdit();
        boxes.splice(0, boxes.length);
        selectedIds = [];
        selectedId = null;
        hideMarquee();
        marqueeState = null;
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

    function setStrokeWidthControls(value) {
        const n = Math.max(0, Number(value) || 0);
        const rounded = Math.round(n * 10) / 10;
        if (els.strokeWidth) {
            const max = Math.max(STROKE_SLIDER_MIN_MAX, Math.ceil(rounded));
            els.strokeWidth.max = String(max);
            els.strokeWidth.value = String(rounded);
        }
        if (els.strokeWidthValue) {
            if (els.strokeWidthValue.tagName === "INPUT") {
                els.strokeWidthValue.value = String(rounded);
            } else {
                els.strokeWidthValue.textContent = String(rounded);
            }
        }
        return rounded;
    }

    function readStrokeWidthFromPanel(event) {
        const source = event && event.target;
        if (source === els.strokeWidthValue) {
            return Math.max(0, Number(els.strokeWidthValue.value) || 0);
        }
        return Math.max(0, Number(els.strokeWidth && els.strokeWidth.value) || 0);
    }

    function applyPanelToSelected(event) {
        if (suppressPanelSync) {
            return;
        }
        const items = getSelectedBoxes();
        if (!items.length) {
            return;
        }
        const strokeW = setStrokeWidthControls(readStrokeWidthFromPanel(event));
        items.forEach(function (box) {
            if (isSvgItem(box)) {
                box.stroke = normalizeFillHex(els.stroke.value);
                box.strokeWidth = strokeW;
                box.strokeOverride = true;
                applySvgStyles(box, { repaint: true });
                return;
            }
            box.fontFamily = els.font.value || "Vazir";
            box.fontSize = clampFontSize(Number(els.fontSize.value) || 48);
            box.letterSpacing = 0;
            box.stroke = els.stroke.value;
            box.strokeWidth = strokeW;
            applyBoxStyles(box);
            fitBoxToText(box);
        });
        const primary = getSelected();
        if (primary) {
            if (!isSvgItem(primary)) {
                els.fontSizeValue.textContent = String(primary.fontSize);
            }
            syncSpectrumFromInput("stroke", els.stroke.value);
        }
        renderList();
        setStatus(items.length > 1 ? "استایل روی موارد انتخاب‌شده اعمال شد." : "استایل اعمال شد.");
    }

    function applyFillColorToSelected() {
        if (suppressPanelSync) {
            return;
        }
        const items = getSelectedBoxes();
        if (!items.length) {
            return;
        }
        const multi = items.length > 1;
        items.forEach(function (box) {
            if (isSvgItem(box)) {
                box.fill = normalizeFillHex(els.fill.value);
                box.fillOverride = true;
                applySvgStyles(box, { repaint: true });
                return;
            }
            if (multi) {
                box.fill = normalizeFillHex(els.fill.value);
                box.charFills = null;
                applyBoxStyles(box);
                fitBoxToText(box);
                return;
            }
            applyFillFromPanel(box, els.fill.value);
            applyBoxStyles(box);
            fitBoxToText(box);
        });
        syncSpectrumFromInput("fill", els.fill.value);
        renderList();
        if (multi) {
            setStatus("رنگ پر روی موارد انتخاب‌شده اعمال شد.");
        }
    }

    function applyStrokeColorToSelected() {
        if (suppressPanelSync) {
            return;
        }
        const items = getSelectedBoxes();
        if (!items.length) {
            return;
        }
        items.forEach(function (box) {
            if (isSvgItem(box)) {
                box.stroke = normalizeFillHex(els.stroke.value);
                box.strokeOverride = true;
                if (!(Number(box.strokeWidth) > 0)) {
                    box.strokeWidth = 1;
                }
                applySvgStyles(box, { repaint: true });
                return;
            }
            box.stroke = els.stroke.value;
            applyBoxStyles(box);
        });
        const primary = getSelected();
        if (primary && isSvgItem(primary) && !(Number(primary.strokeWidth) > 0)) {
            primary.strokeWidth = 1;
        }
        if (primary && Number(primary.strokeWidth) > 0) {
            suppressPanelSync = true;
            setStrokeWidthControls(primary.strokeWidth);
            suppressPanelSync = false;
        }
        syncSpectrumFromInput("stroke", els.stroke.value);
        setStatus(items.length > 1 ? "رنگ حاشیه روی موارد انتخاب‌شده اعمال شد." : "رنگ حاشیه اعمال شد.");
    }

    function beginDrag(box, event, options) {
        event.preventDefault();
        event.stopPropagation();
        const modifier = selectionModifier(event);
        const alreadyIn = isBoxSelected(box.id);
        const countBefore = selectedIds.length;

        if (modifier === "toggle") {
            selectBox(box.id, { toggle: true, silent: true });
            if (!isBoxSelected(box.id)) {
                updateSelectionStatus();
                return;
            }
        } else if (modifier === "add") {
            selectBox(box.id, { add: true, silent: true });
        } else if (!alreadyIn) {
            selectBox(box.id);
        } else {
            selectBox(box.id, { add: true, silent: true });
        }

        const point = getCanvasPoint(event);
        const items = getSelectedBoxes().map(function (item) {
            return {
                box: item,
                offsetX: point.x - item.x,
                offsetY: point.y - item.y,
            };
        });
        dragState = {
            box,
            items,
            startClientX: event.clientX,
            startClientY: event.clientY,
            moved: false,
            maybeEdit:
                alreadyIn &&
                countBefore === 1 &&
                selectedIds.length === 1 &&
                !isSvgItem(box) &&
                editingId !== box.id &&
                !(options && options.fromHandle) &&
                !modifier,
        };
        resizeState = null;
        marqueeState = null;
        items.forEach(function (item) {
            item.box.el.classList.add("is-dragging");
        });
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

        if (marqueeState) {
            if (
                !marqueeState.moved &&
                Math.abs(event.clientX - marqueeState.startClientX) +
                    Math.abs(event.clientY - marqueeState.startClientY) >
                    4
            ) {
                marqueeState.moved = true;
            }
            if (marqueeState.moved) {
                applyMarqueeSelection(getCanvasPoint(event));
            }
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
                box.width = Math.min(MAX_CANVAS_PX, Math.max(24, resizeState.startWidth * scale));
                box.height = Math.min(MAX_CANVAS_PX, Math.max(24, resizeState.startHeight * scale));
                if (resizeState.startStrokeWidth > 0) {
                    box.strokeWidth = Math.max(0, Math.round(resizeState.startStrokeWidth * scale * 10) / 10);
                }
                applySvgStyles(box);
                setStrokeWidthControls(box.strokeWidth);
                return;
            }

            box.fontSize = clampFontSize(resizeState.startFontSize * scale);
            box.letterSpacing = 0;
            box.strokeWidth = Math.max(0, Math.round(resizeState.startStrokeWidth * scale * 10) / 10);

            box.el.style.fontSize = box.fontSize + "px";
            box.el.style.letterSpacing = box.letterSpacing + "px";
            if (box.input) {
                box.input.style.fontSize = box.fontSize + "px";
                box.input.style.letterSpacing = box.letterSpacing + "px";
            }
            if (box.preview) {
                box.preview.style.fontSize = box.fontSize + "px";
                box.preview.style.letterSpacing = box.letterSpacing + "px";
                applyOutsideTextStroke(box.preview, box.strokeWidth, box.stroke);
            }
            applyOutsideTextStroke(box.el, box.strokeWidth, box.stroke);
            if (box.input) {
                applyOutsideTextStroke(box.input, hasMultiFill(box) ? 0 : box.strokeWidth, box.stroke);
            }
            renderTextColorPreview(box);
            fitBoxToText(box);
            syncBoxChromeSize(box);
            syncFontSizePanel(box);
            return;
        }

        if (!dragState) {
            return;
        }
        if (
            !dragState.moved &&
            Math.abs(event.clientX - dragState.startClientX) +
                Math.abs(event.clientY - dragState.startClientY) >
                4
        ) {
            dragState.moved = true;
            dragState.maybeEdit = false;
        }
        if (!dragState.moved) {
            return;
        }
        const point = getCanvasPoint(event);
        const items = dragState.items || [];
        let maxRight = 0;
        let maxBottom = 0;
        items.forEach(function (item) {
            const box = item.box;
            box.x = Math.min(
                Math.max(0, canvasW - Math.max(8, box.width || 0)),
                Math.max(0, point.x - item.offsetX)
            );
            box.y = Math.min(
                Math.max(0, canvasH - Math.max(8, box.height || 0)),
                Math.max(0, point.y - item.offsetY)
            );
            box.el.style.left = box.x + "px";
            box.el.style.top = box.y + "px";
            maxRight = Math.max(maxRight, box.x + (box.width || 0) + CANVAS_GROW_PAD);
            maxBottom = Math.max(maxBottom, box.y + (box.height || 0) + CANVAS_GROW_PAD);
        });
        if (maxRight > canvasW - 80 || maxBottom > canvasH - 80) {
            ensureCanvasFits(maxRight, maxBottom);
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
        if (marqueeState) {
            hideMarquee();
            marqueeState = null;
            updateSelectionStatus();
            return;
        }
        if (resizeState) {
            const box = resizeState.box;
            box.el.classList.remove("is-resizing");
            if (isSvgItem(box)) {
                applySvgStyles(box, { resize: true, repaint: !!box.strokeOverride || Number(box.strokeWidth) > 0 });
                setStrokeWidthControls(box.strokeWidth || 0);
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
        const box = dragState.box;
        const shouldEdit = dragState.maybeEdit && !dragState.moved;
        const moved = dragState.moved;
        const items = dragState.items || [{ box: dragState.box }];
        items.forEach(function (item) {
            if (item.box && item.box.el) {
                item.box.el.classList.remove("is-dragging");
            }
        });
        dragState = null;
        if (shouldEdit) {
            enterTextEdit(box);
            return;
        }
        if (moved) {
            setStatus("موقعیت به‌روز شد.");
        }
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

    function textBoxToPaths(box, options) {
        if (!window.DesignVector || typeof DesignVector.textBoxToWeldedPath !== "function") {
            return Promise.reject(new Error("کتابخانه تبدیل به منحنی (opentype/paper) در دسترس نیست."));
        }
        return DesignVector.textBoxToWeldedPath(box, options);
    }

    function usableCurvePaths(result) {
        return ((result && result.paths) || []).filter(function (p) {
            return p && p.d && String(p.d).replace(/[\sMZ]/gi, "").length > 0;
        });
    }

    function buildCurvesSvgFromPaths(width, height, paths) {
        const w = Math.max(1, Number(width) || 1);
        const h = Math.max(1, Number(height) || 1);
        const inner = paths
            .map(function (item) {
                const fill = escapeXml(item.fill || "#000000");
                return (
                    `<path d="${item.d}" fill="${fill}" fill-rule="nonzero" stroke="none"` +
                    (item.role ? ` data-role="${escapeXml(item.role)}"` : "") +
                    `/>`
                );
            })
            .join("");
        return {
            viewBox: { x: 0, y: 0, width: w, height: h },
            markup:
                `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${w} ${h}" overflow="visible">` +
                inner +
                `</svg>`,
            inner: inner,
        };
    }

    function boundsFromPathData(d) {
        const tokens = String(d || "")
            .replace(/,/g, " ")
            .trim()
            .match(/[MmLlHhVvCcSsQqTtAaZz]|-?\d*\.?\d+(?:e[-+]?\d+)?/g);
        if (!tokens || tokens.length < 3) {
            return null;
        }
        let minX = Infinity;
        let minY = Infinity;
        let maxX = -Infinity;
        let maxY = -Infinity;
        let found = false;
        let i = 0;
        let x = 0;
        let y = 0;
        let cmd = "L";
        function add(px, py) {
            if (!Number.isFinite(px) || !Number.isFinite(py)) {
                return;
            }
            found = true;
            minX = Math.min(minX, px);
            minY = Math.min(minY, py);
            maxX = Math.max(maxX, px);
            maxY = Math.max(maxY, py);
        }
        function take() {
            return Number(tokens[i++]);
        }
        while (i < tokens.length) {
            const t = tokens[i];
            if (/^[MmLlHhVvCcSsQqTtAaZz]$/.test(t)) {
                cmd = t;
                i++;
                if (/^[Zz]$/.test(cmd)) {
                    continue;
                }
            }
            const rel = cmd === cmd.toLowerCase();
            const uc = cmd.toUpperCase();
            if (!(i < tokens.length) || !Number.isFinite(Number(tokens[i]))) {
                break;
            }
            if (uc === "M" || uc === "L" || uc === "T") {
                let nx = take();
                let ny = take();
                if (rel) {
                    nx += x;
                    ny += y;
                }
                x = nx;
                y = ny;
                add(x, y);
                if (uc === "M") {
                    cmd = rel ? "l" : "L";
                }
            } else if (uc === "H") {
                let nx = take();
                if (rel) {
                    nx += x;
                }
                x = nx;
                add(x, y);
            } else if (uc === "V") {
                let ny = take();
                if (rel) {
                    ny += y;
                }
                y = ny;
                add(x, y);
            } else if (uc === "C") {
                let x1 = take();
                let y1 = take();
                let x2 = take();
                let y2 = take();
                let nx = take();
                let ny = take();
                if (rel) {
                    x1 += x;
                    y1 += y;
                    x2 += x;
                    y2 += y;
                    nx += x;
                    ny += y;
                }
                add(x1, y1);
                add(x2, y2);
                x = nx;
                y = ny;
                add(x, y);
            } else if (uc === "S" || uc === "Q") {
                let x1 = take();
                let y1 = take();
                let nx = take();
                let ny = take();
                if (rel) {
                    x1 += x;
                    y1 += y;
                    nx += x;
                    ny += y;
                }
                add(x1, y1);
                x = nx;
                y = ny;
                add(x, y);
            } else if (uc === "A") {
                take();
                take();
                take();
                take();
                take();
                let nx = take();
                let ny = take();
                if (rel) {
                    nx += x;
                    ny += y;
                }
                x = nx;
                y = ny;
                add(x, y);
            } else {
                i++;
            }
        }
        if (!found || !(maxX > minX) || !(maxY > minY)) {
            return null;
        }
        return { x: minX, y: minY, width: maxX - minX, height: maxY - minY };
    }

    function unionPathBounds(paths) {
        let minX = Infinity;
        let minY = Infinity;
        let maxX = -Infinity;
        let maxY = -Infinity;
        let found = false;
        (paths || []).forEach(function (item) {
            const b = boundsFromPathData(item && item.d);
            if (!b) {
                return;
            }
            found = true;
            minX = Math.min(minX, b.x);
            minY = Math.min(minY, b.y);
            maxX = Math.max(maxX, b.x + b.width);
            maxY = Math.max(maxY, b.y + b.height);
        });
        if (!found) {
            return null;
        }
        return { x: minX, y: minY, width: maxX - minX, height: maxY - minY };
    }

    function fitCurvesToGlyphBounds(originX, originY, originW, originH, paths, strokeWidth) {
        const parsed = buildCurvesSvgFromPaths(originW, originH, paths);
        let content = unionPathBounds(paths);
        if (!content || !(content.width > 0 && content.height > 0)) {
            try {
                const doc = new DOMParser().parseFromString(parsed.markup, "image/svg+xml");
                const svg = doc.documentElement;
                if (svg && String(svg.tagName).toLowerCase() === "svg") {
                    content = measureSvgContentBox(svg);
                }
            } catch (e) {
                content = null;
            }
        }
        if (!content || !(content.width > 0 && content.height > 0)) {
            return {
                parsed: parsed,
                x: originX,
                y: originY,
                width: originW,
                height: originH,
            };
        }
        const pad = Math.max(2, (Number(strokeWidth) || 0) + 2);
        const frame = {
            x: content.x - pad,
            y: content.y - pad,
            width: content.width + pad * 2,
            height: content.height + pad * 2,
        };
        return {
            parsed: {
                viewBox: frame,
                markup:
                    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="${frame.x} ${frame.y} ${frame.width} ${frame.height}" overflow="visible" preserveAspectRatio="none">` +
                    parsed.inner +
                    `</svg>`,
                inner: parsed.inner,
            },
            x: originX + frame.x,
            y: originY + frame.y,
            width: frame.width,
            height: frame.height,
        };
    }

    function convertSelectedTextToCurves() {
        const box = getSelected();
        if (!box || isSvgItem(box)) {
            return;
        }
        if (!(box.text || "").trim()) {
            setStatus("متن خالی را نمی‌توان به منحنی تبدیل کرد.");
            return;
        }

        if (els.convertBtn) {
            els.convertBtn.disabled = true;
        }
        exitTextEdit();
        setStatus("در حال تبدیل به منحنی…");

        textBoxToPaths(box, { scaleToCorel: false, origin: "box", joiningClusters: true })
            .then(function (result) {
                const paths = usableCurvePaths(result);
                if (!paths.length) {
                    throw new Error("مسیر منحنی ساخته نشد.");
                }

                const width = Math.max(1, box.width || (box.el && box.el.offsetWidth) || 1);
                const height = Math.max(1, box.height || (box.el && box.el.offsetHeight) || 1);
                const strokeWidth = Number(box.strokeWidth) || 0;
                const fitted = fitCurvesToGlyphBounds(box.x, box.y, width, height, paths, strokeWidth);
                const parsed = fitted.parsed;
                const name = (box.text || "").replace(/\s+/g, " ").trim().slice(0, 28) || "منحنی";
                const fill = normalizeFillHex(box.fill || "#111827");
                const stroke = normalizeFillHex(box.stroke || "#111827");

                if (box.el) {
                    box.el.remove();
                }

                box.kind = "svg";
                box.convertedFromText = true;
                box.name = name;
                box.x = fitted.x;
                box.y = fitted.y;
                box.width = fitted.width;
                box.height = fitted.height;
                box.viewBox = parsed.viewBox;
                box.svgMarkup = parsed.markup;
                box.svgInner = parsed.inner;
                box.svgMarkupOriginal = parsed.markup;
                box.svgInnerOriginal = parsed.inner;
                box.unitsPerCm = null;
                box.fill = fill;
                box.stroke = stroke;
                box.strokeWidth = strokeWidth;
                box.originalFill = fill;
                box.originalStroke = stroke;
                box.fillOverride = false;
                box.strokeOverride = strokeWidth > 0;

                delete box.text;
                delete box.fontFamily;
                delete box.fontSize;
                delete box.letterSpacing;
                delete box.charFills;
                delete box.weld;
                delete box.input;
                delete box.preview;
                delete box.handle;
                delete box.weldLayer;
                delete box._selStart;
                delete box._selEnd;

                createSvgElement(box);
                boxes.forEach(function (item) {
                    applyBoxStyles(item);
                });
                renderList();
                syncPanelFromSelected();
                setStatus("متن به منحنی تبدیل شد. برای رنگ جداگانه، «جدا کردن اجزا» را بزنید.");
            })
            .catch(function (err) {
                console.error(err);
                setStatus(err && err.message ? err.message : "تبدیل به منحنی ناموفق بود.");
            })
            .finally(function () {
                if (els.convertBtn) {
                    els.convertBtn.disabled = false;
                }
            });
    }

    function svgShapeSelector() {
        return "path, polygon, polyline, rect, circle, ellipse, line";
    }

    function listSvgShapeElements(svg) {
        if (!svg || !svg.querySelectorAll) {
            return [];
        }
        return Array.from(svg.querySelectorAll(svgShapeSelector())).filter(function (el) {
            if (el.closest("[data-outside-stroke]")) {
                return false;
            }
            return !isInvisibleFrameRect(el);
        });
    }

    function clonePathWithD(el, d, role) {
        const clone = el.cloneNode(true);
        clone.setAttribute("d", d);
        clone.setAttribute("fill-rule", "nonzero");
        if (role) {
            clone.setAttribute("data-role", role);
        }
        return clone;
    }

    function weldPathD(d) {
        if (!d || !window.DesignVector || typeof DesignVector.weldPathData !== "function") {
            return d;
        }
        try {
            return DesignVector.weldPathData(d) || d;
        } catch (e) {
            return d;
        }
    }

    function shapeFillKey(el) {
        return String(
            (el && el.getAttribute && el.getAttribute("fill")) ||
                (el && el.style && el.style.fill) ||
                ""
        )
            .trim()
            .toLowerCase() || "#000000";
    }

    function boundsOverlapPad(a, b, pad) {
        pad = Number(pad) || 0.75;
        return !!(
            a &&
            b &&
            a.x < b.x + b.width + pad &&
            a.x + a.width > b.x - pad &&
            a.y < b.y + b.height + pad &&
            a.y + a.height > b.y - pad
        );
    }

    function weldExplodedParts(parts) {
        const others = [];
        const bodies = [];
        (parts || []).forEach(function (el) {
            const tag = String((el && el.tagName) || "")
                .toLowerCase()
                .replace(/^.*:/, "");
            const role = String((el && el.getAttribute && el.getAttribute("data-role")) || "").toLowerCase();
            if (tag === "path" && role !== "dot") {
                const welded = weldPathD(el.getAttribute("d") || "");
                bodies.push(clonePathWithD(el, welded, role === "body" ? "body" : role || "body"));
                return;
            }
            others.push(el);
        });
        if (bodies.length <= 1) {
            return others.concat(bodies);
        }

        const used = bodies.map(function () {
            return false;
        });
        const weldedBodies = [];
        for (let i = 0; i < bodies.length; i++) {
            if (used[i]) {
                continue;
            }
            const group = [bodies[i]];
            used[i] = true;
            const fill = shapeFillKey(bodies[i]);
            let grown = true;
            while (grown) {
                grown = false;
                let gb = null;
                group.forEach(function (el) {
                    const b = boundsFromPathData(el.getAttribute("d") || "");
                    if (!b) {
                        return;
                    }
                    if (!gb) {
                        gb = { x: b.x, y: b.y, width: b.width, height: b.height };
                        return;
                    }
                    const x2 = Math.max(gb.x + gb.width, b.x + b.width);
                    const y2 = Math.max(gb.y + gb.height, b.y + b.height);
                    gb.x = Math.min(gb.x, b.x);
                    gb.y = Math.min(gb.y, b.y);
                    gb.width = x2 - gb.x;
                    gb.height = y2 - gb.y;
                });
                for (let j = 0; j < bodies.length; j++) {
                    if (used[j] || shapeFillKey(bodies[j]) !== fill) {
                        continue;
                    }
                    if (boundsOverlapPad(gb, boundsFromPathData(bodies[j].getAttribute("d") || ""))) {
                        group.push(bodies[j]);
                        used[j] = true;
                        grown = true;
                    }
                }
            }
            if (group.length === 1) {
                weldedBodies.push(group[0]);
            } else {
                const combined = group
                    .map(function (el) {
                        return el.getAttribute("d") || "";
                    })
                    .join("");
                weldedBodies.push(clonePathWithD(group[0], weldPathD(combined), "body"));
            }
        }
        return others.concat(weldedBodies);
    }

    function explodeShapeElements(svg) {
        const expanded = [];
        const splitter =
            window.DesignVector && typeof DesignVector.splitGlyphContours === "function"
                ? DesignVector.splitGlyphContours
                : null;
        listSvgShapeElements(svg).forEach(function (el) {
            const tag = String(el.tagName || "").toLowerCase().replace(/^.*:/, "");
            if (tag !== "path") {
                expanded.push(el);
                return;
            }
            const role = String(el.getAttribute("data-role") || "").toLowerCase();
            // تبدیل به منحنی قبلاً نقطه را از بدنه جدا کرده.
            // دوباره شکستن کانتور کل کلمه، تکهٔ وصل حروف (مثل ی) را نقطه می‌گیرد و حفره می‌گذارد.
            if (role === "body" || role === "dot") {
                expanded.push(el);
                return;
            }
            const d = el.getAttribute("d") || "";
            const parts = splitter ? splitter(d) : [{ d: d, role: "body" }];
            if (!parts || parts.length <= 1) {
                expanded.push(el);
                return;
            }
            parts.forEach(function (part) {
                if (!part || !part.d) {
                    return;
                }
                const clone = el.cloneNode(true);
                clone.setAttribute("d", part.d);
                clone.setAttribute("fill-rule", "nonzero");
                if (part.role) {
                    clone.setAttribute("data-role", part.role);
                }
                expanded.push(clone);
            });
        });
        return expanded;
    }

    function countSvgShapes(box) {
        if (!box) {
            return 0;
        }
        if (!box.liveSvg && isSvgItem(box)) {
            refreshSvgPaint(box);
        }
        return explodeShapeElements(box.liveSvg).length;
    }

    function updateBreakApartButton(box) {
        if (!els.breakApartBtn) {
            return;
        }
        const count = isSvgItem(box) ? countSvgShapes(box) : 0;
        els.breakApartBtn.disabled = count < 2;
    }

    function ancestorTransformList(el, svg) {
        const transforms = [];
        let node = el;
        while (node && node !== svg) {
            const t = node.getAttribute && node.getAttribute("transform");
            if (t) {
                transforms.unshift(t);
            }
            node = node.parentElement;
        }
        return transforms;
    }

    function measureDetachedShapeBBox(el) {
        if (!el) {
            return null;
        }
        const host = document.createElement("div");
        host.style.cssText =
            "position:absolute;left:-99999px;top:0;width:800px;height:800px;overflow:visible;opacity:0;pointer-events:none;";
        const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
        svg.setAttribute("xmlns", "http://www.w3.org/2000/svg");
        svg.setAttribute("width", "800");
        svg.setAttribute("height", "800");
        svg.style.overflow = "visible";
        const clone = el.cloneNode(true);
        svg.appendChild(clone);
        host.appendChild(svg);
        document.body.appendChild(host);
        try {
            if (typeof clone.getBBox !== "function") {
                return null;
            }
            const b = clone.getBBox();
            if (!b || !(b.width > 0.05) || !(b.height > 0.05)) {
                return null;
            }
            return { x: b.x, y: b.y, width: b.width, height: b.height };
        } catch (e) {
            return null;
        } finally {
            host.remove();
        }
    }

    function shapeBBoxInSvg(svg, el) {
        if (el && String(el.tagName || "").toLowerCase().replace(/^.*:/, "") === "path") {
            const fromD = boundsFromPathData(el.getAttribute("d") || "");
            if (fromD && fromD.width > 0.05 && fromD.height > 0.05) {
                return fromD;
            }
        }
        const inDom = !!(el && el.ownerSVGElement) || !!(svg && el && svg.contains && svg.contains(el));
        if (inDom) {
            const transforms = ancestorTransformList(el, svg);
            if (!transforms.length && typeof el.getBBox === "function") {
                try {
                    const b = el.getBBox();
                    if (b && b.width > 0.2 && b.height > 0.2) {
                        return { x: b.x, y: b.y, width: b.width, height: b.height };
                    }
                } catch (e) {
                    // fallback
                }
            }
        } else {
            const detached = measureDetachedShapeBBox(el);
            if (detached) {
                return detached;
            }
        }
        const svgRect = svg.getBoundingClientRect();
        const elRect = el.getBoundingClientRect();
        if (!(svgRect.width > 0) || !(svgRect.height > 0)) {
            return null;
        }
        const vb =
            svg.viewBox && svg.viewBox.baseVal
                ? svg.viewBox.baseVal
                : { x: 0, y: 0, width: svgRect.width, height: svgRect.height };
        const sx = vb.width / svgRect.width;
        const sy = vb.height / svgRect.height;
        const bbox = {
            x: vb.x + (elRect.left - svgRect.left) * sx,
            y: vb.y + (elRect.top - svgRect.top) * sy,
            width: elRect.width * sx,
            height: elRect.height * sy,
        };
        if (!(bbox.width > 0.2) || !(bbox.height > 0.2)) {
            return null;
        }
        return bbox;
    }

    function extractShapeMarkup(svg, el) {
        const clone = el.cloneNode(true);
        const markup = new XMLSerializer().serializeToString(clone);
        const transforms = ancestorTransformList(el, svg);
        if (!transforms.length) {
            return markup;
        }
        return `<g transform="${transforms.join(" ")}">${markup}</g>`;
    }

    function createPartBoxFromShape(parentBox, svg, el, index, total) {
        const bbox = shapeBBoxInSvg(svg, el);
        if (!bbox) {
            return null;
        }
        const pad = Math.max(bbox.width, bbox.height) * 0.04 + 1;
        const frame = {
            x: bbox.x - pad,
            y: bbox.y - pad,
            width: Math.max(1, bbox.width + pad * 2),
            height: Math.max(1, bbox.height + pad * 2),
        };
        const parentVb = parentBox.viewBox || { x: 0, y: 0, width: parentBox.width, height: parentBox.height };
        const scaleX = parentBox.width / Math.max(0.0001, parentVb.width);
        const scaleY = parentBox.height / Math.max(0.0001, parentVb.height);
        let partW = frame.width * scaleX;
        let partH = frame.height * scaleY;
        const minSide = 8;
        if (partW < minSide || partH < minSide) {
            const grow = minSide / Math.max(0.0001, Math.min(partW, partH));
            partW *= grow;
            partH *= grow;
        }
        const inner = extractShapeMarkup(svg, el);
        const markup =
            `<svg xmlns="http://www.w3.org/2000/svg" viewBox="${frame.x} ${frame.y} ${frame.width} ${frame.height}" overflow="visible">` +
            inner +
            `</svg>`;
        const fillAttr = el.getAttribute("fill") || (el.style && el.style.fill);
        const strokeAttr = el.getAttribute("stroke") || (el.style && el.style.stroke);
        const fill = isPaintNone(fillAttr)
            ? normalizeFillHex(parentBox.fill || "#111827")
            : cssColorToHex(fillAttr);
        const stroke = isPaintNone(strokeAttr)
            ? normalizeFillHex(parentBox.stroke || "#111827")
            : cssColorToHex(strokeAttr);
        const baseName = (parentBox.name || "شکل").replace(/\s+\d+$/, "").replace(/\s+نقطه$/, "");
        const isDot = el.getAttribute("data-role") === "dot";
        return {
            id: nextId++,
            kind: "svg",
            convertedFromText: !!parentBox.convertedFromText,
            name: isDot ? baseName + " نقطه" : total > 1 ? baseName + " " + (index + 1) : baseName,
            x: parentBox.x + (frame.x - parentVb.x) * scaleX,
            y: parentBox.y + (frame.y - parentVb.y) * scaleY,
            width: partW,
            height: partH,
            viewBox: frame,
            svgMarkup: markup,
            svgInner: inner,
            svgMarkupOriginal: markup,
            svgInnerOriginal: inner,
            unitsPerCm: parentBox.unitsPerCm,
            fill: fill,
            stroke: stroke,
            strokeWidth: Number(parentBox.strokeWidth) || 0,
            originalFill: fill,
            originalStroke: stroke,
            fillOverride: !!parentBox.fillOverride,
            strokeOverride: !!parentBox.strokeOverride || (Number(parentBox.strokeWidth) || 0) > 0,
        };
    }

    function breakApartSelected() {
        const box = getSelected();
        if (!box || !isSvgItem(box)) {
            return;
        }
        if (!box.liveSvg) {
            refreshSvgPaint(box);
        }
        const svg = box.liveSvg;
        if (!svg) {
            setStatus("شکل قابل جداسازی نیست.");
            return;
        }
        const shapes = weldExplodedParts(explodeShapeElements(svg));
        if (shapes.length < 2) {
            setStatus("این شکل فقط یک جزء دارد.");
            return;
        }

        if (els.breakApartBtn) {
            els.breakApartBtn.disabled = true;
        }

        const created = [];
        shapes.forEach(function (shape, index) {
            const part = createPartBoxFromShape(box, svg, shape, index, shapes.length);
            if (part) {
                created.push(part);
            }
        });

        if (created.length < 2) {
            setStatus("اجزای قابل جداسازی پیدا نشد.");
            updateBreakApartButton(box);
            return;
        }

        const index = boxes.findIndex(function (item) {
            return item.id === box.id;
        });
        if (box.el) {
            box.el.remove();
        }
        if (index >= 0) {
            boxes.splice(index, 1);
        }

        created.forEach(function (part, i) {
            boxes.splice(Math.max(0, index) + i, 0, part);
            createSvgElement(part);
        });

        selectBox(created[0].id, { silent: true });
        setStatus(created.length + " جزء جدا شد. هر کدام را انتخاب کنید و رنگ یا حاشیه‌اش را عوض کنید.");
    }

    function computeBounds() {
        let maxX = 400;
        let maxY = 300;
        boxes.forEach((box) => {
            const extra = 40 + (Number(box.strokeWidth) || 0);
            if (isSvgItem(box)) {
                maxX = Math.max(maxX, box.x + (box.width || 0) + extra);
                maxY = Math.max(maxY, box.y + (box.height || 0) + extra);
                return;
            }
            const measured = measureTextBox(box.text || " ", box.fontFamily, box.fontSize, box.letterSpacing);
            const w = box.width || measured.width + extra;
            const h = box.height || measured.height + extra;
            maxX = Math.max(maxX, box.x + w + extra);
            maxY = Math.max(maxY, box.y + h + extra);
        });
        return {
            width: Math.ceil(maxX),
            height: Math.ceil(maxY),
        };
    }

    function pxToCorelScale() {
        return (2.54 / CSS_PPI) * 1000;
    }

    function tpBoxAttrString(box, vb) {
        const frame = vb || box.viewBox || { x: 0, y: 0, width: box.width, height: box.height };
        const round = function (n) {
            return Math.round((Number(n) || 0) * 1000) / 1000;
        };
        return (
            ` data-tp-x="${round(box.x)}"` +
            ` data-tp-y="${round(box.y)}"` +
            ` data-tp-w="${round(box.width)}"` +
            ` data-tp-h="${round(box.height)}"` +
            ` data-tp-vb="${round(frame.x)} ${round(frame.y)} ${round(frame.width)} ${round(frame.height)}"`
        );
    }

    function restoreTpBoxes(svg) {
        const groups = Array.from(svg.querySelectorAll("[data-tp-w]")).filter(function (el) {
            return !el.parentElement || !el.parentElement.closest("[data-tp-w]");
        });
        if (!groups.length) {
            return null;
        }
        if (groups.length > 1) {
            return { multi: true };
        }
        const g = groups[0];
        const x = parseFloat(g.getAttribute("data-tp-x"));
        const y = parseFloat(g.getAttribute("data-tp-y"));
        const w = parseFloat(g.getAttribute("data-tp-w"));
        const h = parseFloat(g.getAttribute("data-tp-h"));
        const vbParts = String(g.getAttribute("data-tp-vb") || "")
            .trim()
            .split(/[\s,]+/)
            .map(Number);
        const vb =
            vbParts.length === 4 && vbParts.every(function (n) {
                return Number.isFinite(n);
            })
                ? { x: vbParts[0], y: vbParts[1], width: vbParts[2], height: vbParts[3] }
                : { x: 0, y: 0, width: w, height: h };
        if (!(w > 0 && h > 0 && vb.width > 0 && vb.height > 0)) {
            return null;
        }
        g.removeAttribute("transform");
        svg.setAttribute("viewBox", vb.x + " " + vb.y + " " + vb.width + " " + vb.height);
        svg.setAttribute("preserveAspectRatio", "none");
        svg.setAttribute("overflow", "visible");
        return { x: x, y: y, width: w, height: h, viewBox: vb };
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
            `  <g id="import_${box.id}"${tpBoxAttrString(box, vb)} transform="translate(${round(tx)} ${round(ty)}) scale(${round(sx)} ${round(sy)}) translate(${round(-vb.x)} ${round(-vb.y)})">\n` +
            `   ${box.svgInner}\n` +
            `  </g>`
        );
    }

    function textBoxContentInset() {
        if (window.DesignVector && typeof DesignVector.textBoxContentInset === "function") {
            return DesignVector.textBoxContentInset();
        }
        return { x: 11, y: 7 };
    }

    function buildTextMarkupCorel(box, styles) {
        const text = (box.text || " ").replace(/\r\n/g, "\n");
        const measured = measureTextBox(text, box.fontFamily, box.fontSize, box.letterSpacing);
        const boxWidth = box.width || measured.width + 18;
        const inset = textBoxContentInset();
        const localY0 = box.y + inset.y;
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
                const x = pxToCorelUnits(box.x + boxWidth - inset.x - lineWidth);
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
            return textBoxToPaths(box, { scaleToCorel: false, origin: "box" })
                .then(function (result) {
                    const paths = (result && result.paths) || [];
                    const usable = paths.filter(function (p) {
                        return p && p.d && String(p.d).replace(/[\sMZ]/gi, "").length > 0;
                    });
                    if (!usable.length) {
                        throw new Error("path خالی ساخته شد.");
                    }
                    return { type: "paths", paths: usable, box: box };
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
                const items = (part.paths || []).filter(function (item) {
                    return item && item.d;
                });
                const box = part.box;
                const w = Math.max(1, Number(box && box.width) || 1);
                const h = Math.max(1, Number(box && box.height) || 1);
                const tx = pxToCorelUnits(box ? box.x : 0);
                const ty = pxToCorelUnits(box ? box.y : 0);
                const s = pxToCorelScale();
                const round = function (n) {
                    return Math.round(n * 1000) / 1000;
                };
                const localVb = { x: 0, y: 0, width: w, height: h };
                body.push(
                    `  <g${tpBoxAttrString(box, localVb)} transform="translate(${round(tx)} ${round(ty)}) scale(${round(s)})">`
                );
                const stroked = items.filter(function (item) {
                    return (Number(item.strokeWidth) || 0) > 0;
                });
                if (stroked.length) {
                    body.push(
                        '    <g data-outside-stroke="1" fill="none" stroke-linejoin="round" stroke-linecap="round">'
                    );
                    stroked.forEach(function (item) {
                        const sw = outsideStrokeWidth(item.strokeWidth);
                        body.push(
                            `      <path fill="none" stroke="${escapeXml(item.stroke || "#000000")}" stroke-width="${sw}" fill-rule="nonzero" d="${item.d}"/>`
                        );
                    });
                    body.push("    </g>");
                }
                items.forEach(function (item) {
                    const fillCls = styles.fillClass(item.fill);
                    const fillAttr = ` fill="${escapeXml(item.fill || "#000000")}"`;
                    body.push(
                        `    <path class="${fillCls}"${fillAttr} stroke="none" fill-rule="nonzero" d="${item.d}"/>`
                    );
                });
                body.push("  </g>");
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
        els.fill.addEventListener(evt, applyFillColorToSelected);
        els.stroke.addEventListener(evt, applyStrokeColorToSelected);
        els.strokeWidth.addEventListener(evt, applyPanelToSelected);
        if (els.strokeWidthValue) {
            els.strokeWidthValue.addEventListener(evt, applyPanelToSelected);
        }
    });

    if (els.fontFile) {
        els.fontFile.addEventListener("change", onFontFileChange);
    }

    els.addBtn.addEventListener("click", function () {
        enterTextEdit(addTextBox());
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
    if (els.convertBtn) {
        els.convertBtn.addEventListener("click", convertSelectedTextToCurves);
    }
    if (els.breakApartBtn) {
        els.breakApartBtn.addEventListener("click", breakApartSelected);
    }

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
                fitPageToView();
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
            beginPan(event);
            return;
        }
        if (canStartMarquee(event)) {
            beginMarquee(event);
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
        if ((event.ctrlKey || event.metaKey) && (event.key === "a" || event.key === "A")) {
            const tag = (document.activeElement && document.activeElement.tagName) || "";
            if (tag !== "TEXTAREA" && tag !== "INPUT" && tag !== "SELECT") {
                event.preventDefault();
                setSelection(boxes.map((box) => box.id));
                return;
            }
        }
        if (event.key === "Escape") {
            if (editingId) {
                exitTextEdit();
                setStatus("ویرایش متن تمام شد. برای جابه‌جایی شکل را بکشید.");
                return;
            }
            if (selectedIds.length) {
                setSelection([]);
                return;
            }
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
        if (event.key === "Delete" && getSelected() && document.activeElement && document.activeElement.tagName !== "TEXTAREA") {
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
    updateStageGrid();
    drawRulers();
    addTextBox({ text: "طراحی" });
    setStatus("شکل را انتخاب کنید و بکشید. چندتایی: Shift/Ctrl+کلیک یا کشیدن روی فضای خالی. جابه‌جایی بوم: Space یا کلیک وسط.");
})();
