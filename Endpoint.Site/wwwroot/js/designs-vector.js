/**
 * تبدیل متن به path با opentype.js و جوش اختیاری (Weld) با paper.js
 * خروجی SVG سازگار با ساختار Corel-like
 */
(function (global) {
    "use strict";

    const CSS_PPI = 96;
    const COREL_UNITS_PER_CM = 1000;
    const PX_TO_CM = 2.54 / CSS_PPI;
    const PX_TO_COREL = PX_TO_CM * COREL_UNITS_PER_CM;

    const FONT_URLS = {
        Vazir: "/lib/vazir-font/dist/Vazir-Regular.ttf",
        Vazirmatn:
            "https://cdn.jsdelivr.net/gh/rastikerdar/vazirmatn@v33.003/fonts/ttf/Vazirmatn-Regular.ttf",
        Samim: "https://cdn.jsdelivr.net/gh/rastikerdar/samim-font@v4.0.5/dist/Samim.ttf",
        Sahel: "https://cdn.jsdelivr.net/gh/rastikerdar/sahel-font@v3.4.0/dist/Sahel.ttf",
        Tanha: "https://cdn.jsdelivr.net/gh/rastikerdar/tanha-font@v0.10/dist/Tanha.ttf",
    };

    const fontCache = new Map();
    const customFontBuffers = new Map();
    let paperReady = false;

    function ensureOpentype() {
        if (typeof opentype === "undefined") {
            throw new Error("کتابخانه opentype.js بارگذاری نشده است.");
        }
    }

    function ensurePaper() {
        if (typeof paper === "undefined") {
            throw new Error("کتابخانه paper.js بارگذاری نشده است.");
        }
        if (paperReady) {
            return;
        }
        const canvas = document.createElement("canvas");
        canvas.width = 8;
        canvas.height = 8;
        paper.setup(canvas);
        paper.settings.applyMatrix = true;
        paperReady = true;
    }

    function pxToCorelUnits(px) {
        return Math.round(Number(px) * PX_TO_COREL);
    }

    function normalizeHexColor(color) {
        if (!color) {
            return "#000000";
        }
        const value = String(color).trim();
        if (/^#[0-9a-fA-F]{6}$/.test(value)) {
            return value.toUpperCase();
        }
        if (/^#[0-9a-fA-F]{3}$/.test(value)) {
            return ("#" + value[1] + value[1] + value[2] + value[2] + value[3] + value[3]).toUpperCase();
        }
        return value;
    }

    function compactCorelPathD(d) {
        if (!d) {
            return "";
        }
        return String(d)
            .replace(/,/g, " ")
            .replace(/([a-zA-Z])\s+/g, "$1")
            .replace(/\s+([a-zA-Z])/g, "$1")
            .replace(/(-?\d+\.\d+)/g, function (num) {
                return String(Math.round(Number(num)));
            })
            .replace(/\s+/g, " ")
            .replace(/ ([MLHVCSQTAZ])/gi, "$1")
            .replace(/([MLHVCSQTAZ]) /gi, "$1")
            .trim();
    }

    /**
     * فقط مختصات path را مقیاس می‌دهد (نه پرچم‌های قوس A).
     */
    function scalePathDataToCorel(d) {
        if (!d) {
            return "";
        }
        const tokens = String(d).match(/[a-zA-Z]|[+-]?(?:\d*\.\d+|\d+)(?:[eE][+-]?\d+)?/g);
        if (!tokens) {
            return "";
        }
        let i = 0;
        let command = "";
        const out = [];

        function read() {
            return tokens[i++];
        }

        function scaleNum(raw) {
            return String(pxToCorelUnits(Number(raw)));
        }

        while (i < tokens.length) {
            if (/^[a-zA-Z]$/.test(tokens[i])) {
                command = read();
                out.push(command);
            }
            if (!command) {
                break;
            }
            const cmd = command.toUpperCase();

            if (cmd === "Z") {
                continue;
            }

            if (cmd === "H") {
                while (i < tokens.length && !/^[a-zA-Z]$/.test(tokens[i])) {
                    out.push(scaleNum(read()));
                }
                continue;
            }

            if (cmd === "V") {
                while (i < tokens.length && !/^[a-zA-Z]$/.test(tokens[i])) {
                    out.push(scaleNum(read()));
                }
                continue;
            }

            if (cmd === "A") {
                while (i < tokens.length && !/^[a-zA-Z]$/.test(tokens[i])) {
                    out.push(scaleNum(read())); // rx
                    out.push(scaleNum(read())); // ry
                    out.push(read()); // rotation
                    out.push(read()); // large-arc
                    out.push(read()); // sweep
                    out.push(scaleNum(read())); // x
                    out.push(scaleNum(read())); // y
                }
                continue;
            }

            // M L T Q S C — همه اعداد مختصات‌اند
            while (i < tokens.length && !/^[a-zA-Z]$/.test(tokens[i])) {
                out.push(scaleNum(read()));
            }
        }

        return out.join(" ").replace(/\s+/g, " ").trim();
    }

    function hasArabicScript(text) {
        return /[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF\uFB50-\uFDFF\uFE70-\uFEFF]/.test(text || "");
    }

    /** شکل‌دهی حروف عربی/فارسی بدون معکوس‌کردن */
    function reshapeArabic(text) {
        if (!hasArabicScript(text)) {
            return text;
        }
        if (global.ArabicReshaper && typeof global.ArabicReshaper.convertArabic === "function") {
            return global.ArabicReshaper.convertArabic(text);
        }
        return text;
    }

    /**
     * ترتیب بصری برای چیدمان LTR در opentype.
     * حروف بعد از reshape در ترتیب منطقی‌اند؛ برای رسم چپ→راست معکوس می‌شوند.
     */
    function toVisualRtlRun(text) {
        const shaped = reshapeArabic(text);
        if (!hasArabicScript(text)) {
            return shaped;
        }
        return Array.from(shaped).reverse().join("");
    }

    function registerCustomFont(familyName, arrayBuffer) {
        customFontBuffers.set(familyName, arrayBuffer);
        fontCache.delete(familyName);
    }

    function loadFont(familyName) {
        ensureOpentype();
        if (fontCache.has(familyName)) {
            return Promise.resolve(fontCache.get(familyName));
        }

        if (customFontBuffers.has(familyName)) {
            try {
                const font = opentype.parse(customFontBuffers.get(familyName));
                fontCache.set(familyName, font);
                return Promise.resolve(font);
            } catch (err) {
                return Promise.reject(new Error("پارس فونت سفارشی ناموفق بود."));
            }
        }

        const url = FONT_URLS[familyName];
        if (!url) {
            return Promise.reject(
                new Error(
                    "برای خروجی path، فونت «" +
                        familyName +
                        "» را به‌صورت TTF/OTF آپلود کنید یا یکی از فونت‌های وزیر/وزیرمتن/صمیم/ساحل/تنها را انتخاب کنید."
                )
            );
        }

        return fetch(url)
            .then(function (response) {
                if (!response.ok) {
                    throw new Error("دانلود فونت ناموفق بود (" + response.status + "): " + url);
                }
                return response.arrayBuffer();
            })
            .then(function (buffer) {
                const font = opentype.parse(buffer);
                if (!font || !font.glyphs) {
                    throw new Error("پارس فونت ناموفق بود: " + url);
                }
                fontCache.set(familyName, font);
                return font;
            });
    }

    function safeKerning(font, leftGlyph, rightGlyph) {
        try {
            const value = font.getKerningValue(leftGlyph, rightGlyph);
            return Number.isFinite(value) ? value : 0;
        } catch (e) {
            return 0;
        }
    }

    function otPathToD(otPath) {
        if (!otPath) {
            return "";
        }
        if (typeof otPath.toPathData === "function") {
            try {
                const d = otPath.toPathData(3);
                if (d && String(d).replace(/[\sMZ]/gi, "").length) {
                    return d;
                }
            } catch (e) {
                // ادامه با commands
            }
        }
        const commands = otPath.commands || [];
        if (!commands.length) {
            return "";
        }
        const parts = [];
        for (let i = 0; i < commands.length; i++) {
            const c = commands[i];
            const t = c.type;
            if (t === "M" || t === "L") {
                parts.push(t, c.x, c.y);
            } else if (t === "C") {
                parts.push(t, c.x1, c.y1, c.x2, c.y2, c.x, c.y);
            } else if (t === "Q") {
                parts.push(t, c.x1, c.y1, c.x, c.y);
            } else if (t === "Z") {
                parts.push("Z");
            }
        }
        return parts.join(" ");
    }

    function forEachGlyph(font, text, fontSize, letterSpacing, startX, baselineY, onGlyph) {
        const glyphs = font.stringToGlyphs(text);
        const scale = fontSize / font.unitsPerEm;
        let x = startX;
        for (let i = 0; i < glyphs.length; i++) {
            const glyph = glyphs[i];
            onGlyph(glyph, x, baselineY);
            let adv = (Number(glyph.advanceWidth) || 0) * scale;
            if (i < glyphs.length - 1) {
                adv += safeKerning(font, glyph, glyphs[i + 1]) * scale;
                adv += Number(letterSpacing) || 0;
            }
            x += adv;
        }
        return x - startX;
    }

    function measureGlyphRunWidth(font, text, fontSize, letterSpacing) {
        return forEachGlyph(font, text, fontSize, letterSpacing, 0, 0, function () {});
    }

    /**
     * ساخت path data مستقیم از opentype — بدون paper (موقعیت پایدار).
     */
    function glyphRunToPathData(font, visualText, fontSize, letterSpacing, startX, baselineY) {
        const chunks = [];
        forEachGlyph(font, visualText, fontSize, letterSpacing, startX, baselineY, function (glyph, x, y) {
            const d = otPathToD(glyph.getPath(x, y, fontSize));
            if (d && d.length > 4) {
                chunks.push(d);
            }
        });
        return chunks.join("");
    }

    function bakeItemToPathData(item) {
        if (!item) {
            return "";
        }
        item.applyMatrix = true;
        if (typeof item.applyMatrix === "boolean") {
            // اطمینان از پخت ماتریس در سگمنت‌ها
            item.matrix = item.matrix;
        }
        if (item.pathData) {
            return item.pathData;
        }
        const node = item.exportSVG({ asString: false, precision: 3 });
        if (!node) {
            return "";
        }
        if (node.getAttribute && node.tagName === "path") {
            return node.getAttribute("d") || "";
        }
        if (node.querySelectorAll) {
            return Array.from(node.querySelectorAll("path"))
                .map(function (p) {
                    return p.getAttribute("d") || "";
                })
                .join("");
        }
        return "";
    }

    function approxPathWidth(d) {
        const nums = String(d).match(/-?\d+\.?\d*/g);
        if (!nums || nums.length < 2) {
            return 0;
        }
        let minX = Infinity;
        let maxX = -Infinity;
        for (let i = 0; i + 1 < nums.length; i += 2) {
            const x = Number(nums[i]);
            if (Number.isFinite(x)) {
                minX = Math.min(minX, x);
                maxX = Math.max(maxX, x);
            }
        }
        return Number.isFinite(minX) ? maxX - minX : 0;
    }

    function weldPathData(d) {
        if (!d) {
            return d;
        }
        // برای حروف فارسی، boolean unite اغلب حفره‌ها (مثل م) را خراب می‌کند
        // و قطعات را جابه‌جا می‌نماید. یک path مرکب (چند subpath) برای Corel کافی است.
        // فقط اگر عرض نتیجه غیرعادی بزرگ نشد، unite را می‌پذیریم.
        try {
            ensurePaper();
            paper.project.clear();
            const beforeW = approxPathWidth(d);
            const compound = new paper.CompoundPath(d);
            compound.fillRule = "nonzero";
            const children = compound.children && compound.children.length ? compound.children.slice() : [];
            if (children.length < 2) {
                return d;
            }
            let united = children[0].clone();
            for (let i = 1; i < children.length; i++) {
                const next = united.unite(children[i]);
                if (united.remove) {
                    united.remove();
                }
                united = next;
            }
            const weldedD = bakeItemToPathData(united);
            if (!weldedD) {
                return d;
            }
            const afterW = approxPathWidth(weldedD);
            if (beforeW > 0 && afterW > beforeW * 1.35 + 40) {
                console.warn("Weld unite rejected: inflated bounds");
                return d;
            }
            return weldedD;
        } catch (e) {
            console.warn("Weld boolean skipped:", e);
            return d;
        }
    }

    function textBoxToWeldedPath(box) {
        ensureOpentype();
        const family = box.fontFamily || "Vazir";
        return loadFont(family).then(function (font) {
            const raw = (box.text || "").replace(/\r\n/g, "\n");
            if (!raw.trim()) {
                throw new Error("متن خالی است.");
            }

            const lines = raw.split("\n");
            const lineHeight = box.fontSize * 1.35;
            const padX = 10;
            const padY = 8;

            const visuals = lines.map(function (line) {
                const content = line.length ? line : " ";
                return toVisualRtlRun(content);
            });
            const measuredWidths = visuals.map(function (visual) {
                return measureGlyphRunWidth(font, visual, box.fontSize, box.letterSpacing || 0);
            });
            const maxLineWidth = Math.max.apply(null, measuredWidths.concat([0]));
            // اگر باکس دستی خیلی عریض باشد، متن را به عرض واقعی‌اش راست‌چین می‌کنیم
            // نه اینکه فاصلهٔ جعلی داخل path ایجاد شود
            const contentWidth = maxLineWidth + padX * 2;
            const boxWidth = box.width > 0 ? box.width : contentWidth;
            const rightEdge = box.x + boxWidth - padX;

            const pathChunks = [];
            visuals.forEach(function (visual, index) {
                const lineW = measuredWidths[index] || 0;
                const startX = rightEdge - lineW;
                const baselineY = box.y + padY + box.fontSize * 0.85 + index * lineHeight;
                const d = glyphRunToPathData(
                    font,
                    visual,
                    box.fontSize,
                    box.letterSpacing || 0,
                    startX,
                    baselineY
                );
                if (d) {
                    pathChunks.push(d);
                }
            });

            if (!pathChunks.length) {
                throw new Error("مسیری از فونت ساخته نشد.");
            }

            let d = pathChunks.join("");
            if (box.weld) {
                d = weldPathData(d);
            }

            const scaled = compactCorelPathD(scalePathDataToCorel(d));
            if (!scaled || !String(scaled).replace(/[\sMZ]/gi, "").length) {
                throw new Error("پس از مقیاس‌گذاری، path خالی شد.");
            }

            return {
                paths: [
                    {
                        d: scaled,
                        fill: box.fill,
                        stroke: box.stroke,
                        strokeWidth: box.strokeWidth || 0,
                    },
                ],
            };
        });
    }

    function getCorelStyleRegistry() {
        const fills = new Map();
        const strokes = new Map();
        let fillIndex = 0;
        let strokeIndex = 0;

        return {
            fillClass(color) {
                const hex = normalizeHexColor(color);
                if (!fills.has(hex)) {
                    fills.set(hex, "fil" + fillIndex++);
                }
                return fills.get(hex);
            },
            strokeClass(color, widthPx) {
                const hex = normalizeHexColor(color);
                const width = Math.max(0, Number(widthPx) || 0);
                const key = hex + "|" + width;
                if (!strokes.has(key)) {
                    strokes.set(key, {
                        className: "str" + strokeIndex++,
                        hex: hex,
                        width: width,
                    });
                }
                return strokes.get(key).className;
            },
            buildCss() {
                const lines = [];
                fills.forEach(function (className, hex) {
                    lines.push("    ." + className + " {fill:" + hex + "}");
                });
                strokes.forEach(function (item) {
                    const sw = Math.max(0.01, item.width * PX_TO_COREL);
                    lines.push(
                        "    ." +
                            item.className +
                            " {stroke:" +
                            item.hex +
                            ";stroke-width:" +
                            Math.round(sw * 100) / 100 +
                            "}"
                    );
                });
                return lines.join("\n");
            },
        };
    }

    function buildCorelSvgDocument(boundsPx, bodyLines, styles) {
        const vbW = Math.max(1, pxToCorelUnits(boundsPx.width));
        const vbH = Math.max(1, pxToCorelUnits(boundsPx.height));
        const widthCm = Math.max(0.01, Number((boundsPx.width * PX_TO_CM).toFixed(3)));
        const heightCm = Math.max(0.01, Number((boundsPx.height * PX_TO_CM).toFixed(3)));
        const css = styles.buildCss();

        return (
            '<?xml version="1.0" encoding="UTF-8"?>\n' +
            '<!DOCTYPE svg PUBLIC "-//W3C//DTD SVG 1.1//EN" "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd">\n' +
            "<!-- Creator: TaskPlanner Design (opentype.js + paper.js) -->\n" +
            '<svg xmlns="http://www.w3.org/2000/svg" xml:space="preserve" width="' +
            widthCm +
            'cm" height="' +
            heightCm +
            'cm" version="1.1" ' +
            'style="shape-rendering:geometricPrecision; text-rendering:geometricPrecision; image-rendering:optimizeQuality; fill-rule:nonzero; clip-rule:nonzero" ' +
            'viewBox="0 0 ' +
            vbW +
            " " +
            vbH +
            '"\n' +
            ' xmlns:xlink="http://www.w3.org/1999/xlink">\n' +
            " <defs>\n" +
            '  <style type="text/css">\n' +
            "   <![CDATA[\n" +
            css +
            "\n" +
            "   ]]>\n" +
            "  </style>\n" +
            " </defs>\n" +
            ' <g id="Layer_x0020_1">\n' +
            bodyLines.join("\n") +
            "\n </g>\n" +
            "</svg>\n"
        );
    }

    global.DesignVector = {
        FONT_URLS: FONT_URLS,
        registerCustomFont: registerCustomFont,
        loadFont: loadFont,
        textBoxToWeldedPath: textBoxToWeldedPath,
        getCorelStyleRegistry: getCorelStyleRegistry,
        buildCorelSvgDocument: buildCorelSvgDocument,
        pxToCorelUnits: pxToCorelUnits,
        PX_TO_CM: PX_TO_CM,
        normalizeHexColor: normalizeHexColor,
        hasArabicScript: hasArabicScript,
        toVisualRtlRun: toVisualRtlRun,
    };
})(window);
