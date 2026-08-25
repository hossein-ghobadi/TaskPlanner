(function () {
    "use strict";

    const els = {
        canvas: document.getElementById("designCanvas"),
        stage: document.getElementById("designStage"),
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
    const MIN_BOX_WIDTH = 48;
    const MIN_BOX_HEIGHT = 36;

    function setStatus(message) {
        if (els.status) {
            els.status.textContent = message || "";
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
        els.canvas.innerHTML = "";
        if (els.fontFile) {
            els.fontFile.value = "";
        }
        els.fileName.value = "design";
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
        const rect = els.canvas.getBoundingClientRect();
        dragState = {
            box,
            offsetX: event.clientX - rect.left - box.x,
            offsetY: event.clientY - rect.top - box.y,
        };
        resizeState = null;
        box.el.classList.add("is-dragging");
    }

    function beginResize(box, direction, event) {
        event.preventDefault();
        event.stopPropagation();
        selectBox(box.id);
        const rect = els.canvas.getBoundingClientRect();
        resizeState = {
            box,
            direction,
            startX: event.clientX,
            startY: event.clientY,
            startWidth: box.width || box.el.offsetWidth,
            startHeight: box.height || box.el.offsetHeight,
            canvasLeft: rect.left,
            canvasTop: rect.top,
        };
        dragState = null;
        box.el.classList.add("is-resizing");
    }

    function onPointerMove(event) {
        if (resizeState) {
            const box = resizeState.box;
            const dx = event.clientX - resizeState.startX;
            const dy = event.clientY - resizeState.startY;
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
        const rect = els.canvas.getBoundingClientRect();
        const box = dragState.box;
        box.x = Math.max(0, event.clientX - rect.left - dragState.offsetX);
        box.y = Math.max(0, event.clientY - rect.top - dragState.offsetY);
        box.el.style.left = box.x + "px";
        box.el.style.top = box.y + "px";
    }

    function onPointerUp() {
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

    els.stage.addEventListener("mousedown", function (event) {
        if (event.target === els.stage || event.target === els.canvas) {
            selectedId = null;
            boxes.forEach((box) => applyBoxStyles(box));
            renderList();
            syncPanelFromSelected();
        }
    });

    window.addEventListener("mousemove", onPointerMove);
    window.addEventListener("mouseup", onPointerUp);

    window.addEventListener("keydown", function (event) {
        if (event.key === "Delete" && getSelected() && document.activeElement.tagName !== "TEXTAREA") {
            deleteSelected();
        }
    });

    addTextBox({ text: "طراحی" });
    setStatus("برای افزودن متن بیشتر، «افزودن تکست باکس» را بزنید.");
})();
