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
    // باید با .design-textbox-input padding و border کادر یکی باشد
    const TEXT_PAD_X = 10;
    const TEXT_PAD_Y = 6;
    const TEXT_BORDER = 1;

    function textBoxContentInset() {
        return {
            x: TEXT_PAD_X + TEXT_BORDER,
            y: TEXT_PAD_Y + TEXT_BORDER,
        };
    }

    function cssLineBaseline(font, fontSize, padY, lineIndex) {
        const size = Number(fontSize) || 0;
        const upm = Math.max(1, Number(font && font.unitsPerEm) || 1000);
        const ascent = (Number(font && font.ascender) / upm) * size;
        const descent = Math.abs(Number(font && font.descender) / upm) * size;
        const lineHeight = size * 1.35;
        const extra = lineHeight - (ascent + descent);
        const halfLeading = extra / 2;
        return padY + halfLeading + ascent + (Number(lineIndex) || 0) * lineHeight;
    }

    const FONT_URLS = {
        Vazir: "/lib/vazir-font/dist/Vazir-Regular.ttf",
        Vazirmatn: "/lib/vazirmatn/fonts/ttf/Vazirmatn-Regular.ttf",
        Samim: "/lib/samim-font/Samim.ttf",
        Sahel: "/lib/sahel-font/Sahel.ttf",
        Tanha: "/lib/tanha-font/Tanha.ttf",
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

    function lastLetterJoinType(text) {
        const chars = Array.from(String(text || ""));
        for (let i = chars.length - 1; i >= 0; i--) {
            const t = arabicJoinType(chars[i]);
            if (t !== "T") {
                return t;
            }
        }
        return "U";
    }

    /**
     * R = فقط از راست وصل می‌شود (ا د ر و ...)
     * D = از دو طرف وصل می‌شود
     * T = اعراب شفاف
     * U = جدا (فاصله، همزه، لاتین)
     */
    function arabicJoinType(ch) {
        if (!ch) {
            return "U";
        }
        if (/\s/.test(ch)) {
            return "U";
        }
        const code = ch.charCodeAt(0);
        if (
            (code >= 0x064b && code <= 0x065f) ||
            code === 0x0670 ||
            (code >= 0x06d6 && code <= 0x06ed)
        ) {
            return "T";
        }
        if (ch === "ء") {
            return "U";
        }
        if (/[اأإآدذرزوؤةٱژ]/.test(ch)) {
            return "R";
        }
        if (
            (code >= 0x0600 && code <= 0x06ff) ||
            (code >= 0x0750 && code <= 0x077f) ||
            (code >= 0x08a0 && code <= 0x08ff) ||
            (code >= 0xfb50 && code <= 0xfdff) ||
            (code >= 0xfe70 && code <= 0xfeff)
        ) {
            return "D";
        }
        return "U";
    }

    function splitLineIntoClustersAndGaps(text) {
        const s = String(text || "");
        const pieces = [];
        let i = 0;
        while (i < s.length) {
            if (/\s/.test(s.charAt(i))) {
                let count = 0;
                while (i < s.length && /\s/.test(s.charAt(i))) {
                    count++;
                    i++;
                }
                pieces.push({ type: "space", count: count });
                continue;
            }
            const start = i;
            let current = s.charAt(i);
            i++;
            while (i < s.length) {
                const next = s.charAt(i);
                if (/\s/.test(next)) {
                    break;
                }
                const t = arabicJoinType(next);
                if (t === "U") {
                    break;
                }
                if (t === "T") {
                    current += next;
                    i++;
                    continue;
                }
                const prev = lastLetterJoinType(current);
                if (prev === "D" && (t === "D" || t === "R")) {
                    current += next;
                    i++;
                } else {
                    break;
                }
            }
            pieces.push({ type: "cluster", text: current, start: start });
        }
        return pieces;
    }

    function spaceAdvancePx(font, fontSize) {
        try {
            const glyph = font.charToGlyph(" ");
            return ((Number(glyph.advanceWidth) || font.unitsPerEm * 0.25) * fontSize) / font.unitsPerEm;
        } catch (e) {
            return fontSize * 0.25;
        }
    }

    function measureJoiningClusterLineWidth(font, logicalLine, fontSize, letterSpacing) {
        const spacing = Number(letterSpacing) || 0;
        const pieces = splitLineIntoClustersAndGaps(logicalLine);
        const ordered = hasArabicScript(logicalLine) ? pieces.slice().reverse() : pieces;
        let w = 0;
        ordered.forEach(function (piece, idx) {
            if (piece.type === "space") {
                w += spaceAdvancePx(font, fontSize) * piece.count;
                return;
            }
            const visual = toVisualRtlRun(piece.text);
            w += measureGlyphRunWidth(font, visual, fontSize, spacing);
            const next = ordered[idx + 1];
            if (next && next.type !== "space") {
                w += spacing;
            }
        });
        return w;
    }

    function glyphLineToJoiningClusterPaths(
        font,
        logicalLine,
        logicalColors,
        fontSize,
        letterSpacing,
        startX,
        baselineY,
        defaultFill
    ) {
        const fallback = normalizeHexColor(defaultFill || "#000000");
        const spacing = Number(letterSpacing) || 0;
        const pieces = splitLineIntoClustersAndGaps(logicalLine);
        const ordered = hasArabicScript(logicalLine) ? pieces.slice().reverse() : pieces;
        let x = startX;
        const chunks = [];
        ordered.forEach(function (piece, idx) {
            if (piece.type === "space") {
                x += spaceAdvancePx(font, fontSize) * piece.count;
                return;
            }
            const colorSlice = Array.isArray(logicalColors)
                ? logicalColors.slice(piece.start, piece.start + piece.text.length)
                : null;
            const visual = toVisualRtlRunWithColors(piece.text, colorSlice, fallback);
            const fill = normalizeHexColor((visual.colors && visual.colors[0]) || fallback);
            const bodyParts = [];
            const dots = [];
            forEachGlyph(font, visual.text, fontSize, spacing, x, baselineY, function (glyph, gx, gy) {
                const gd = otPathToD(glyph.getPath(gx, gy, fontSize));
                if (!gd || gd.length <= 4) {
                    return;
                }
                splitGlyphContours(gd).forEach(function (part) {
                    if (!part || !part.d) {
                        return;
                    }
                    if (part.role === "dot") {
                        dots.push(part.d);
                    } else {
                        bodyParts.push(part.d);
                    }
                });
            });
            if (bodyParts.length) {
                chunks.push({ d: bodyParts.join(""), fill: fill, role: "body" });
            }
            dots.forEach(function (dotD) {
                chunks.push({ d: dotD, fill: fill, role: "dot" });
            });
            x += measureGlyphRunWidth(font, visual.text, fontSize, spacing);
            const next = ordered[idx + 1];
            if (next && next.type !== "space") {
                x += spacing;
            }
        });
        return chunks;
    }
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

    function boundsFromPathD(d) {
        const nums = String(d || "").match(/-?\d*\.?\d+(?:e[-+]?\d+)?/gi);
        if (!nums || nums.length < 2) {
            return null;
        }
        let minX = Infinity;
        let minY = Infinity;
        let maxX = -Infinity;
        let maxY = -Infinity;
        let found = false;
        for (let i = 0; i + 1 < nums.length; i += 2) {
            const x = Number(nums[i]);
            const y = Number(nums[i + 1]);
            if (!Number.isFinite(x) || !Number.isFinite(y)) {
                continue;
            }
            found = true;
            minX = Math.min(minX, x);
            minY = Math.min(minY, y);
            maxX = Math.max(maxX, x);
            maxY = Math.max(maxY, y);
        }
        if (!found || !(maxX > minX) || !(maxY > minY)) {
            return null;
        }
        return { x: minX, y: minY, width: maxX - minX, height: maxY - minY };
    }

    function splitPathSubpaths(d) {
        const raw = String(d || "").replace(/,/g, " ").trim();
        if (!raw) {
            return [];
        }
        return raw
            .split(/(?=[Mm])/)
            .map(function (part) {
                return part.trim();
            })
            .filter(function (part) {
                return part.length > 2 && /[MLHVCSQTAmlhvcsqta]/.test(part);
            });
    }

    function contourArea(b) {
        return b.width * b.height;
    }

    function isCompactMark(b) {
        const ar = b.width / Math.max(0.0001, b.height);
        return ar >= 0.38 && ar <= 2.6;
    }

    function subpathEndPoints(d) {
        const tokens = String(d || "").replace(/,/g, " ").trim().match(/[MmLlHhVvCcSsQqTtAaZz]|-?\d*\.?\d+(?:e[-+]?\d+)?/g) || [];
        const pts = [];
        let i = 0;
        let x = 0;
        let y = 0;
        let cmd = "L";
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
            function take() {
                return Number(tokens[i++]);
            }
            if (!(i < tokens.length) || !Number.isFinite(Number(tokens[i]))) {
                break;
            }
            if (uc === "M" || uc === "L") {
                let nx = take();
                let ny = take();
                if (rel) {
                    nx += x;
                    ny += y;
                }
                x = nx;
                y = ny;
                pts.push({ x: x, y: y });
                if (uc === "M") {
                    cmd = rel ? "l" : "L";
                }
            } else if (uc === "H") {
                let nx = take();
                if (rel) {
                    nx += x;
                }
                x = nx;
                pts.push({ x: x, y: y });
            } else if (uc === "V") {
                let ny = take();
                if (rel) {
                    ny += y;
                }
                y = ny;
                pts.push({ x: x, y: y });
            } else if (uc === "C") {
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
                pts.push({ x: x, y: y });
            } else if (uc === "S" || uc === "Q") {
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
                pts.push({ x: x, y: y });
            } else if (uc === "T") {
                let nx = take();
                let ny = take();
                if (rel) {
                    nx += x;
                    ny += y;
                }
                x = nx;
                y = ny;
                pts.push({ x: x, y: y });
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
                pts.push({ x: x, y: y });
            } else {
                i++;
            }
        }
        return pts;
    }

    function subpathSignedArea(d) {
        const pts = subpathEndPoints(d);
        if (pts.length < 3) {
            return 0;
        }
        let area = 0;
        for (let i = 0; i < pts.length; i++) {
            const p = pts[i];
            const q = pts[(i + 1) % pts.length];
            area += p.x * q.y - q.x * p.y;
        }
        return area / 2;
    }

    function containsPoint(b, x, y, inset) {
        inset = Number(inset) || 0;
        return (
            x >= b.x + inset &&
            x <= b.x + b.width - inset &&
            y >= b.y + inset &&
            y <= b.y + b.height - inset
        );
    }

    function fullyInsideBounds(inner, outer, pad) {
        pad = Number(pad) || 0;
        return (
            inner.x >= outer.x - pad &&
            inner.y >= outer.y - pad &&
            inner.x + inner.width <= outer.x + outer.width + pad &&
            inner.y + inner.height <= outer.y + outer.height + pad
        );
    }

    function boundsOverlap(a, b, pad) {
        pad = Number(pad) || 0;
        return (
            a.x < b.x + b.width + pad &&
            a.x + a.width > b.x - pad &&
            a.y < b.y + b.height + pad &&
            a.y + a.height > b.y - pad
        );
    }

    /**
     * روی یک گلیف: حفره با بدنه می‌ماند.
     * کانتور کوچک بالا/پایین یا داخل حرف نقطه است؛ تکهٔ وصل کناری بدنه است.
     * اگر چند حرف در یک path باشند، موقعیت نسبت به نزدیک‌ترین حرف سنجیده می‌شود.
     */
    function splitGlyphContours(d) {
        const subs = splitPathSubpaths(d);
        if (!subs.length) {
            return [];
        }
        if (subs.length === 1) {
            return [{ d: subs[0], role: "body" }];
        }

        const items = subs
            .map(function (sd) {
                return {
                    d: sd,
                    bounds: boundsFromPathD(sd),
                    winding: subpathSignedArea(sd),
                };
            })
            .filter(function (item) {
                return item.bounds && item.bounds.width > 0 && item.bounds.height > 0;
            });
        if (!items.length) {
            return [{ d: d, role: "body" }];
        }

        items.sort(function (a, b) {
            return contourArea(b.bounds) - contourArea(a.bounds);
        });
        const host = items[0];
        const maxArea = contourArea(host.bounds);
        const bodySign = host.winding >= 0 ? 1 : -1;

        items.forEach(function (item, i) {
            item.parent = -1;
            const cx = item.bounds.x + item.bounds.width / 2;
            const cy = item.bounds.y + item.bounds.height / 2;
            for (let j = 0; j < i; j++) {
                const parent = items[j];
                if (
                    containsPoint(parent.bounds, cx, cy, 0) ||
                    fullyInsideBounds(item.bounds, parent.bounds, 0.6)
                ) {
                    item.parent = j;
                    break;
                }
            }
            item.oppositeWinding =
                item.winding !== 0 && (item.winding >= 0 ? 1 : -1) !== bodySign;
        });

        const largeBodies = items.filter(function (item) {
            if (item.parent >= 0) {
                const parent = items[item.parent];
                if (contourArea(item.bounds) < contourArea(parent.bounds) * 0.55) {
                    return false;
                }
            }
            return contourArea(item.bounds) >= maxArea * 0.1;
        });
        const multiLetter = largeBodies.length >= 2;

        function nearestLetter(item) {
            if (!multiLetter) {
                return host;
            }
            const cx = item.bounds.x + item.bounds.width / 2;
            const cy = item.bounds.y + item.bounds.height / 2;
            let best = largeBodies[0] || host;
            let bestScore = Infinity;
            largeBodies.forEach(function (cand) {
                if (cand === item) {
                    return;
                }
                if (
                    containsPoint(cand.bounds, cx, cy, 0) ||
                    fullyInsideBounds(item.bounds, cand.bounds, 0.6)
                ) {
                    best = cand;
                    bestScore = -1;
                    return;
                }
                const dx = Math.max(
                    0,
                    Math.max(
                        item.bounds.x - (cand.bounds.x + cand.bounds.width),
                        cand.bounds.x - (item.bounds.x + item.bounds.width)
                    )
                );
                const dy = Math.max(
                    0,
                    Math.max(
                        item.bounds.y - (cand.bounds.y + cand.bounds.height),
                        cand.bounds.y - (item.bounds.y + item.bounds.height)
                    )
                );
                const score = dx + dy * 0.35;
                if (score < bestScore) {
                    best = cand;
                    bestScore = score;
                }
            });
            return best;
        }

        items.forEach(function (item) {
            const area = contourArea(item.bounds);
            const parent = item.parent >= 0 ? items[item.parent] : null;
            const parentArea = parent ? contourArea(parent.bounds) : 0;
            if (parent && area < parentArea * 0.55) {
                const innerNuqta =
                    !item.oppositeWinding &&
                    isCompactMark(item.bounds) &&
                    area < parentArea * 0.085;
                item.kind = innerNuqta ? "dot" : "hole";
                return;
            }
            const ref = nearestLetter(item);
            const refArea = contourArea(ref.bounds);
            const small =
                !item.oppositeWinding &&
                area < refArea * 0.28 &&
                (isCompactMark(item.bounds) || area < refArea * 0.12);
            if (!small) {
                item.kind = "body";
                return;
            }
            const cx = item.bounds.x + item.bounds.width / 2;
            const cy = item.bounds.y + item.bounds.height / 2;
            const top = ref.bounds.y + ref.bounds.height * 0.2;
            const bot = ref.bounds.y + ref.bounds.height * 0.8;
            const px = (cx - ref.bounds.x) / Math.max(0.0001, ref.bounds.width);
            const attachedToLetter =
                boundsOverlap(item.bounds, ref.bounds, Math.max(0.8, ref.bounds.width * 0.04)) &&
                item.bounds.y < ref.bounds.y + ref.bounds.height * 0.92 &&
                item.bounds.y + item.bounds.height > ref.bounds.y + ref.bounds.height * 0.08;
            const besideJoin =
                cy >= top &&
                cy <= bot &&
                (px < 0.18 || px > 0.82 || cx < ref.bounds.x || cx > ref.bounds.x + ref.bounds.width);
            if (attachedToLetter || besideJoin) {
                item.kind = "body";
                return;
            }
            if (cy <= top || cy >= bot) {
                item.kind = "dot";
                return;
            }
            item.kind = "body";
        });

        const bodyParts = [];
        const dots = [];
        items.forEach(function (item) {
            if (item.kind === "dot") {
                dots.push({ d: item.d, role: "dot" });
            } else {
                bodyParts.push(item.d);
            }
        });
        if (!bodyParts.length) {
            return dots.length ? dots : [{ d: d, role: "body" }];
        }
        return [{ d: bodyParts.join(""), role: "body" }].concat(dots);
    }
    function glyphRunToGlyphPaths(font, visualText, colors, fontSize, letterSpacing, startX, baselineY, defaultFill) {
        const chunks = [];
        const fallback = normalizeHexColor(defaultFill || "#000000");
        const fills = Array.isArray(colors) ? colors : [];
        let glyphIndex = 0;
        forEachGlyph(font, visualText, fontSize, letterSpacing, startX, baselineY, function (glyph, x, y) {
            const d = otPathToD(glyph.getPath(x, y, fontSize));
            if (d && d.length > 4) {
                const fill = normalizeHexColor(
                    fills[Math.min(glyphIndex, Math.max(0, fills.length - 1))] || fallback
                );
                splitGlyphContours(d).forEach(function (part) {
                    chunks.push({ d: part.d, fill: fill, role: part.role });
                });
            }
            glyphIndex++;
        });
        return chunks;
    }
    function glyphRunToColoredPathChunks(font, visualText, colors, fontSize, letterSpacing, startX, baselineY, defaultFill) {
        const text = String(visualText || "");
        if (!text.length) {
            return [];
        }
        const fills = Array.isArray(colors) ? colors : [];
        const fallback = normalizeHexColor(defaultFill || "#000000");
        const chunks = [];
        let i = 0;
        let x = startX;
        const spacing = Number(letterSpacing) || 0;

        while (i < text.length) {
            const fill = normalizeHexColor(fills[i] || fallback);
            let j = i + 1;
            while (j < text.length && normalizeHexColor(fills[j] || fallback) === fill) {
                j++;
            }
            const run = text.slice(i, j);
            const d = glyphRunToPathData(font, run, fontSize, spacing, x, baselineY);
            if (d) {
                chunks.push({ d: d, fill: fill });
            }
            x += measureGlyphRunWidth(font, run, fontSize, spacing);
            if (j < text.length) {
                x += spacing;
            }
            i = j;
        }
        return chunks;
    }

    /**
     * رنگ‌های منطقی یک خط را با reshape/معکوس RTL به ترتیب بصری هم‌تراز می‌کند.
     */
    function toVisualRtlRunWithColors(text, logicalColors, defaultFill) {
        const content = text && text.length ? text : " ";
        const fallback = normalizeHexColor(defaultFill || "#000000");
        const colors = [];
        for (let i = 0; i < content.length; i++) {
            colors.push(normalizeHexColor((logicalColors && logicalColors[i]) || fallback));
        }
        const shaped = reshapeArabic(content);
        let visualChars;
        let visualColors;
        if (shaped.length === content.length) {
            visualChars = shaped.split("");
            visualColors = colors.slice();
        } else {
            // طول عوض شده؛ کل خط با رنگ غالب
            visualChars = shaped.split("");
            const counts = {};
            colors.forEach(function (c) {
                counts[c] = (counts[c] || 0) + 1;
            });
            let dominant = fallback;
            let best = 0;
            Object.keys(counts).forEach(function (c) {
                if (counts[c] > best) {
                    best = counts[c];
                    dominant = c;
                }
            });
            visualColors = visualChars.map(function () {
                return dominant;
            });
        }
        if (hasArabicScript(content)) {
            visualChars.reverse();
            visualColors.reverse();
        }
        return {
            text: visualChars.join(""),
            colors: visualColors,
        };
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

    function textBoxToWeldedPath(box, options) {
        ensureOpentype();
        options = options || {};
        const scaleToCorel = options.scaleToCorel !== false;
        const originBox = options.origin === "box";
        const family = box.fontFamily || "Vazir";
        return loadFont(family).then(function (font) {
            const raw = (box.text || "").replace(/\r\n/g, "\n");
            if (!raw.trim()) {
                throw new Error("متن خالی است.");
            }

            const lines = raw.split("\n");
            const inset = textBoxContentInset();
            const padX = inset.x;
            const padY = inset.y;
            const defaultFill = normalizeHexColor(box.fill || "#000000");
            const charFills = Array.isArray(box.charFills) ? box.charFills : null;
            const originX = originBox ? 0 : Number(box.x) || 0;
            const originY = originBox ? 0 : Number(box.y) || 0;

            // رنگ‌های منطقی هر خط (شاخص UTF-16 هم‌تراز با textarea)
            let offset = 0;
            const lineColorSlices = lines.map(function (line) {
                const slice = charFills ? charFills.slice(offset, offset + line.length) : null;
                offset += line.length + 1; // +1 برای \n
                return slice;
            });

            const visuals = lines.map(function (line, index) {
                const content = line.length ? line : " ";
                return toVisualRtlRunWithColors(content, lineColorSlices[index], defaultFill);
            });
            const measuredWidths = lines.map(function (line, index) {
                const content = line.length ? line : " ";
                if (options.joiningClusters) {
                    return measureJoiningClusterLineWidth(
                        font,
                        content,
                        box.fontSize,
                        box.letterSpacing || 0
                    );
                }
                return measureGlyphRunWidth(font, visuals[index].text, box.fontSize, box.letterSpacing || 0);
            });
            const maxLineWidth = Math.max.apply(null, measuredWidths.concat([0]));
            const contentWidth = maxLineWidth + padX * 2;
            const boxWidth = box.width > 0 ? box.width : contentWidth;
            const rightEdge = originX + boxWidth - padX;

            const coloredChunks = [];
            lines.forEach(function (line, index) {
                const visual = visuals[index];
                const lineW = measuredWidths[index] || 0;
                const startX = rightEdge - lineW;
                const baselineY = originY + cssLineBaseline(font, box.fontSize, padY, index);
                const content = line.length ? line : " ";
                const parts = options.joiningClusters
                    ? glyphLineToJoiningClusterPaths(
                          font,
                          content,
                          lineColorSlices[index],
                          box.fontSize,
                          box.letterSpacing || 0,
                          startX,
                          baselineY,
                          defaultFill
                      )
                    : options.separateGlyphs
                      ? glyphRunToGlyphPaths(
                            font,
                            visual.text,
                            visual.colors,
                            box.fontSize,
                            box.letterSpacing || 0,
                            startX,
                            baselineY,
                            defaultFill
                        )
                      : glyphRunToColoredPathChunks(
                            font,
                            visual.text,
                            visual.colors,
                            box.fontSize,
                            box.letterSpacing || 0,
                            startX,
                            baselineY,
                            defaultFill
                        );
                parts.forEach(function (part) {
                    coloredChunks.push(part);
                });
            });

            if (!coloredChunks.length) {
                throw new Error("مسیری از فونت ساخته نشد.");
            }

            const paths = [];
            if (options.separateGlyphs || options.joiningClusters) {
                coloredChunks.forEach(function (part) {
                    if (!part.d) {
                        return;
                    }
                    const outD = scaleToCorel ? compactCorelPathD(scalePathDataToCorel(part.d)) : part.d;
                    if (outD && String(outD).replace(/[\sMZ]/gi, "").length) {
                        paths.push({
                            d: outD,
                            fill: part.fill,
                            stroke: box.stroke,
                            strokeWidth: box.strokeWidth || 0,
                            role: part.role || "body",
                        });
                    }
                });
            } else {
            const byFill = new Map();
            coloredChunks.forEach(function (part) {
                if (!part.d) {
                    return;
                }
                if (!byFill.has(part.fill)) {
                    byFill.set(part.fill, []);
                }
                byFill.get(part.fill).push(part.d);
            });

            byFill.forEach(function (ds, fill) {
                let d = ds.join("");
                if (box.weld) {
                    d = weldPathData(d);
                }
                const outD = scaleToCorel ? compactCorelPathD(scalePathDataToCorel(d)) : d;
                if (outD && String(outD).replace(/[\sMZ]/gi, "").length) {
                    paths.push({
                        d: outD,
                        fill: fill,
                        stroke: box.stroke,
                        strokeWidth: box.strokeWidth || 0,
                    });
                }
            });
            }

            if (!paths.length) {
                throw new Error(scaleToCorel ? "پس از مقیاس‌گذاری، path خالی شد." : "path خالی ساخته شد.");
            }

            return { paths: paths };
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
                    const sw = Math.max(0.01, item.width * 2 * PX_TO_COREL);
                    lines.push(
                        "    ." +
                            item.className +
                            " {stroke:" +
                            item.hex +
                            ";stroke-width:" +
                            Math.round(sw * 100) / 100 +
                            ";paint-order:stroke fill}"
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
        splitGlyphContours: splitGlyphContours,
        textBoxToWeldedPath: textBoxToWeldedPath,
        getCorelStyleRegistry: getCorelStyleRegistry,
        buildCorelSvgDocument: buildCorelSvgDocument,
        pxToCorelUnits: pxToCorelUnits,
        PX_TO_CM: PX_TO_CM,
        textBoxContentInset: textBoxContentInset,
        normalizeHexColor: normalizeHexColor,
        hasArabicScript: hasArabicScript,
        toVisualRtlRun: toVisualRtlRun,
        toVisualRtlRunWithColors: toVisualRtlRunWithColors,
    };
})(window);
