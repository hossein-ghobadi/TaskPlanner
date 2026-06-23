(function () {
    const STORAGE_KEY = "taskplanner.mindmap.state";
    const canvas = document.getElementById("mindmapCanvas");
    const linesSvg = document.getElementById("mindmapLines");
    const addNodeBtn = document.getElementById("addNodeBtn");
    const exportBtn = document.getElementById("exportBtn");
    const importJsonBtn = document.getElementById("importJsonBtn");
    const importJsonInput = document.getElementById("importJsonInput");
    const exportWordBtn = document.getElementById("exportWordBtn");
    const clearBtn = document.getElementById("clearBtn");
    const selectionHint = document.getElementById("selectionHint");
    const nodeTextInput = document.getElementById("nodeTextInput");
    const nodeColorInput = document.getElementById("nodeColorInput");
    const nodeTextColorInput = document.getElementById("nodeTextColorInput");
    const nodeFontSizeInput = document.getElementById("nodeFontSizeInput");
    const nodeFontSizeValue = document.getElementById("nodeFontSizeValue");
    const stage = document.getElementById("mindmapStage");
    const viewport = document.getElementById("mindmapViewport");
    const zoomInBtn = document.getElementById("mindmapZoomIn");
    const zoomOutBtn = document.getElementById("mindmapZoomOut");
    const zoomResetBtn = document.getElementById("mindmapZoomReset");

    if (!canvas || !linesSvg) {
        return;
    }

    let viewScale = 1;
    let viewPanX = 0;
    let viewPanY = 0;
    /** @type {{ sx: number, sy: number, px: number, py: number } | null} */
    let panDragState = null;
    let spacePanArmed = false;
    const VIEW_SCALE_MIN = 0.25;
    const VIEW_SCALE_MAX = 2.5;

    let state = { nodes: [], links: [] };
    let selectedNodeId = null;
    /** انتخاب چندگانهٔ نودها (برای کادر موس و Ctrl) */
    let selectedNodeIds = new Set();
    /** آخرین نود برای Shift+کلیک (بازه در ترتیب آرایه) */
    let nodeRangeAnchorId = null;
    let suppressNextCanvasClick = false;
    /** @type {{ x0: number, y0: number, x1: number, y1: number, additive: boolean } | null} */
    let marqueeState = null;
    let marqueeEl = null;
    let connectSourceId = null;
    let dragState = null;
    /** @type {{ id: string, sx: number, sy: number, sw: number, sh: number } | null} */
    let resizeState = null;
    /** @type {{ fromId: string, x: number, y: number } | null} */
    let linkDrag = null;
    let linkHoverId = null;
    let editingNodeId = null;
    let skipInlineBlurCommit = false;

    function createId() {
        if (window.crypto && typeof window.crypto.randomUUID === "function") {
            return window.crypto.randomUUID();
        }
        return "node-" + Date.now() + "-" + Math.floor(Math.random() * 100000);
    }

    function clampNodeFontSize(value) {
        const n = Number(value);
        if (!Number.isFinite(n)) {
            return 16;
        }
        return Math.round(Math.max(10, Math.min(32, n)));
    }

    function applyNodeSizing(wrap, fontSize) {
        const size = clampNodeFontSize(fontSize);
        wrap.style.setProperty("--mm-node-size", size + "px");
        wrap.style.fontSize = size + "px";
    }

    function updateNodeFontSizeLabel(size) {
        if (nodeFontSizeValue) {
            nodeFontSizeValue.textContent = String(clampNodeFontSize(size));
        }
    }

    function normalizeNode(raw) {
        const n = raw || {};
        const legacyLabels = Array.isArray(n.childBranchLabels)
            ? n.childBranchLabels.map(String).map((s) => s.trim()).filter(Boolean)
            : [];
        let branchDraft = typeof n.branchDraft === "string" ? n.branchDraft : "";
        if (!branchDraft && legacyLabels.length) {
            branchDraft = legacyLabels.join("\n");
        }
        return {
            id: n.id || createId(),
            text: n.text != null ? String(n.text) : "ایده جدید",
            x: typeof n.x === "number" ? n.x : 0,
            y: typeof n.y === "number" ? n.y : 0,
            w: typeof n.w === "number" && Number.isFinite(n.w) ? Math.max(60, Math.round(n.w)) : null,
            h: typeof n.h === "number" && Number.isFinite(n.h) ? Math.max(34, Math.round(n.h)) : null,
            background: n.background || "#ffffff",
            textColor: n.textColor || "#0f172a",
            fontSize: clampNodeFontSize(n.fontSize),
            accentColor: typeof n.accentColor === "string" ? n.accentColor : "",
            childBranchLabels: legacyLabels,
            branchDraft
        };
    }

    function getMindmapShellFromTarget(el) {
        if (!el || !el.closest) {
            return null;
        }
        const wrap = el.closest(".mindmap-node");
        if (wrap) {
            return wrap;
        }
        const cluster = el.closest(".mindmap-node-cluster");
        return cluster ? cluster.querySelector(".mindmap-node") : null;
    }


    const BRANCH_PALETTE = ["#7c3aed", "#2563eb", "#0891b2", "#dc2626", "#ea580c", "#65a30d", "#db2777", "#6366f1", "#059669"];

    function hasIncomingLink(nodeId) {
        return state.links.some((l) => l.to === nodeId);
    }

    function hasOutgoingLink(nodeId) {
        return state.links.some((l) => l.from === nodeId);
    }

    function parentIdOf(nodeId) {
        const link = state.links.find((l) => l.to === nodeId);
        return link ? link.from : null;
    }

    function buildNodeDepthMap() {
        const depths = new Map();
        const roots = state.nodes.filter((n) => !hasIncomingLink(n.id));
        const queue = roots.map((n) => ({ id: n.id, depth: 0 }));
        const seen = new Set();
        while (queue.length > 0) {
            const item = queue.shift();
            if (!item || seen.has(item.id)) {
                continue;
            }
            seen.add(item.id);
            depths.set(item.id, item.depth);
            state.links
                .filter((l) => l.from === item.id)
                .forEach((l) => {
                    if (!seen.has(l.to)) {
                        queue.push({ id: l.to, depth: item.depth + 1 });
                    }
                });
        }
        state.nodes.forEach((n) => {
            if (!depths.has(n.id)) {
                depths.set(n.id, hasIncomingLink(n.id) ? 2 : 0);
            }
        });
        return depths;
    }

    function siblingOrdinal(nodeId) {
        const pid = parentIdOf(nodeId);
        if (!pid) {
            return 1;
        }
        const siblings = state.links.filter((l) => l.from === pid).map((l) => l.to);
        const idx = siblings.indexOf(nodeId);
        return idx >= 0 ? idx + 1 : 1;
    }

    function hashCode(s) {
        let h = 0;
        for (let i = 0; i < s.length; i++) {
            h = (Math.imul(31, h) + s.charCodeAt(i)) | 0;
        }
        return h;
    }

    function accentForNode(node) {
        if (node.accentColor && /^#[0-9A-Fa-f]{6}$/i.test(node.accentColor)) {
            return node.accentColor;
        }
        const idx = Math.abs(hashCode(node.id)) % BRANCH_PALETTE.length;
        return BRANCH_PALETTE[idx];
    }

    function nextChildAccent(parentId) {
        const n = state.links.filter((l) => l.from === parentId).length;
        return BRANCH_PALETTE[n % BRANCH_PALETTE.length];
    }

    function ensureBranchThemeFor(nodeId) {
        const node = getNode(nodeId);
        if (!node || !hasIncomingLink(nodeId)) {
            return;
        }
        if (!node.accentColor || !/^#[0-9A-Fa-f]{6}$/i.test(node.accentColor)) {
            node.accentColor = accentForNode(node);
        }
        node.background = "#ffffff";
        node.textColor = node.accentColor || accentForNode(node);
    }

    function migrateState() {
        state.nodes = state.nodes.map(normalizeNode);
        state.nodes.forEach((node) => {
            if (hasIncomingLink(node.id)) {
                if (!node.accentColor) {
                    node.accentColor = accentForNode(node);
                }
                node.background = "#ffffff";
                node.textColor = node.accentColor || accentForNode(node);
            }
        });
    }

    function saveState() {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    }

    function loadState() {
        const raw = localStorage.getItem(STORAGE_KEY);
        if (!raw) {
            return;
        }
        try {
            const parsed = JSON.parse(raw);
            const r = hydrateStateFromParsed(parsed);
            if (!r.ok) {
                state = { nodes: [], links: [] };
            }
        } catch {
            state = { nodes: [], links: [] };
        }
    }

    /**
     * ساخت نسخهٔ خام { nodes, links } از JSON بدون تغییر stateٔ فعلی.
     * @returns {{ ok: true, next: { nodes: any[], links: any[] } } | { ok: false, error: string }}
     */
    function buildRawMindmapStateFromParsed(parsed) {
        if (!parsed || typeof parsed !== "object") {
            return { ok: false, error: "فایل JSON معتبر نیست." };
        }
        const nodesIn = Array.isArray(parsed.nodes) ? parsed.nodes : [];
        const linksIn = Array.isArray(parsed.links) ? parsed.links : [];
        const normalizedNodes = nodesIn.map((raw) => normalizeNode(raw));
        const idSet = new Set(normalizedNodes.map((n) => n.id));
        const links = [];
        const seen = new Set();
        for (let i = 0; i < linksIn.length; i++) {
            const l = linksIn[i];
            if (!l || typeof l !== "object") {
                continue;
            }
            const from = l.from != null ? String(l.from) : "";
            const to = l.to != null ? String(l.to) : "";
            if (!from || !to || !idSet.has(from) || !idSet.has(to)) {
                continue;
            }
            const key = from + "\0" + to;
            if (seen.has(key)) {
                continue;
            }
            seen.add(key);
            links.push({ from, to });
        }
        return { ok: true, next: { nodes: normalizedNodes, links } };
    }

    /**
     * اعمال دادهٔ پارس‌شده (مثل localStorage) روی state و migrate.
     * @returns {{ ok: true } | { ok: false, error: string }}
     */
    function hydrateStateFromParsed(parsed) {
        const r = buildRawMindmapStateFromParsed(parsed);
        if (!r.ok) {
            return r;
        }
        state = r.next;
        migrateState();
        return { ok: true };
    }

    /** جایگزینی state پس از تأیید کاربر؛ فرض می‌کند next از buildRawMindmapStateFromParsed معتبر است. */
    function applyImportedSnapshot(next) {
        editingNodeId = null;
        dragState = null;
        cancelLinkDrag();
        clearNodeMultiSelection();
        connectSourceId = null;
        selectedNodeId = null;
        selectedNodeIds.clear();
        nodeRangeAnchorId = null;
        state = next;
        migrateState();
        resetViewTransform();
        saveState();
        syncPanel();
        render();
    }

    function getNode(nodeId) {
        return state.nodes.find((n) => n.id === nodeId) || null;
    }

    function setNodeSelectionFromIds(ids, preferredPrimary) {
        const filtered = ids.map(String).filter((id) => getNode(id));
        selectedNodeIds = new Set(filtered);
        if (selectedNodeIds.size === 0) {
            selectedNodeId = null;
            nodeRangeAnchorId = null;
            return;
        }
        if (preferredPrimary && selectedNodeIds.has(preferredPrimary)) {
            selectedNodeId = preferredPrimary;
        } else {
            selectedNodeId = filtered[0];
        }
    }

    function clearNodeMultiSelection() {
        selectedNodeIds.clear();
        selectedNodeId = null;
        nodeRangeAnchorId = null;
    }

    function syncSingleNodeSelection(nodeId) {
        if (!nodeId) {
            clearNodeMultiSelection();
            return;
        }
        setNodeSelectionFromIds([nodeId], nodeId);
        nodeRangeAnchorId = nodeId;
    }

    function applyViewportTransform() {
        if (viewport) {
            viewport.style.transform = "translate(" + viewPanX + "px," + viewPanY + "px) scale(" + viewScale + ")";
        }
    }

    function resetViewTransform() {
        viewScale = 1;
        viewPanX = 0;
        viewPanY = 0;
        applyViewportTransform();
        scheduleDrawLinks();
    }

    function zoomAtClientPoint(clientX, clientY, factor) {
        if (!stage || !viewport) {
            return;
        }
        const hr = stage.getBoundingClientRect();
        const mx = clientX - hr.left;
        const my = clientY - hr.top;
        const worldX = (mx - viewPanX) / viewScale;
        const worldY = (my - viewPanY) / viewScale;
        let newScale = viewScale * factor;
        newScale = Math.min(VIEW_SCALE_MAX, Math.max(VIEW_SCALE_MIN, newScale));
        viewPanX = mx - worldX * newScale;
        viewPanY = my - worldY * newScale;
        viewScale = newScale;
        applyViewportTransform();
        scheduleDrawLinks();
    }

    function marqueeContentToScreenRect(x0, y0, x1, y1) {
        if (!stage || !viewport) {
            const canvasRect = canvas.getBoundingClientRect();
            return {
                left: Math.min(x0, x1) + canvasRect.left,
                top: Math.min(y0, y1) + canvasRect.top,
                right: Math.max(x0, x1) + canvasRect.left,
                bottom: Math.max(y0, y1) + canvasRect.top
            };
        }
        const hr = stage.getBoundingClientRect();
        const lo = Math.min(x0, x1);
        const hi = Math.max(x0, x1);
        const t = Math.min(y0, y1);
        const b = Math.max(y0, y1);
        return {
            left: hr.left + viewPanX + lo * viewScale,
            top: hr.top + viewPanY + t * viewScale,
            right: hr.left + viewPanX + hi * viewScale,
            bottom: hr.top + viewPanY + b * viewScale
        };
    }

    function getCanvasLocalPoint(clientX, clientY) {
        if (stage && viewport) {
            const hr = stage.getBoundingClientRect();
            return {
                x: (clientX - hr.left - viewPanX) / viewScale,
                y: (clientY - hr.top - viewPanY) / viewScale
            };
        }
        const r = canvas.getBoundingClientRect();
        return { x: clientX - r.left, y: clientY - r.top };
    }

    function ensureMarqueeEl() {
        if (marqueeEl && marqueeEl.parentNode === canvas) {
            return marqueeEl;
        }
        const el = document.createElement("div");
        el.className = "mindmap-marquee";
        el.setAttribute("aria-hidden", "true");
        canvas.appendChild(el);
        marqueeEl = el;
        return el;
    }

    function removeMarqueeEl() {
        if (marqueeEl && marqueeEl.parentNode) {
            marqueeEl.remove();
        }
        marqueeEl = null;
    }

    function updateMarqueeVisual() {
        if (!marqueeState) {
            return;
        }
        const el = ensureMarqueeEl();
        const x = Math.min(marqueeState.x0, marqueeState.x1);
        const y = Math.min(marqueeState.y0, marqueeState.y1);
        const w = Math.abs(marqueeState.x1 - marqueeState.x0);
        const h = Math.abs(marqueeState.y1 - marqueeState.y0);
        el.style.left = x + "px";
        el.style.top = y + "px";
        el.style.width = w + "px";
        el.style.height = h + "px";
    }

    function nodesIntersectingClientRect(rect) {
        const out = [];
        state.nodes.forEach((node) => {
            const cluster = canvas.querySelector('.mindmap-node-cluster[data-node-id="' + node.id + '"]');
            if (!cluster) {
                return;
            }
            const r = cluster.getBoundingClientRect();
            if (r.left < rect.right && r.right > rect.left && r.top < rect.bottom && r.bottom > rect.top) {
                out.push(node.id);
            }
        });
        return out;
    }

    function onMarqueeMove(event) {
        if (!marqueeState) {
            return;
        }
        const p = getCanvasLocalPoint(event.clientX, event.clientY);
        marqueeState.x1 = p.x;
        marqueeState.y1 = p.y;
        updateMarqueeVisual();
    }

    function onMarqueeEnd(event) {
        window.removeEventListener("mousemove", onMarqueeMove);
        window.removeEventListener("mouseup", onMarqueeEnd, true);
        if (!marqueeState) {
            return;
        }
        const x0 = marqueeState.x0;
        const y0 = marqueeState.y0;
        const x1 = marqueeState.x1;
        const y1 = marqueeState.y1;
        const additive = marqueeState.additive;
        marqueeState = null;
        removeMarqueeEl();

        const dx = Math.abs(x1 - x0);
        const dy = Math.abs(y1 - y0);
        if (dx < 4 && dy < 4) {
            if (!additive) {
                clearNodeMultiSelection();
                if (editingNodeId) {
                    commitInlineEdit(editingNodeId);
                } else {
                    applySelectionUi();
                }
            }
            suppressNextCanvasClick = true;
            return;
        }

        const scr = marqueeContentToScreenRect(x0, y0, x1, y1);
        const picked = nodesIntersectingClientRect(scr);

        suppressNextCanvasClick = true;
        if (editingNodeId) {
            commitInlineEdit(editingNodeId);
        }

        if (additive) {
            picked.forEach((id) => selectedNodeIds.add(id));
            if (picked.length) {
                selectedNodeId = picked[picked.length - 1];
            }
        } else {
            setNodeSelectionFromIds(picked, picked.length ? picked[picked.length - 1] : null);
        }
        if (selectedNodeId) {
            nodeRangeAnchorId = selectedNodeId;
        }
        applySelectionUi();
    }

    function startMarquee(event) {
        if (linkDrag) {
            return;
        }
        const p = getCanvasLocalPoint(event.clientX, event.clientY);
        marqueeState = {
            x0: p.x,
            y0: p.y,
            x1: p.x,
            y1: p.y,
            additive: !!(event.ctrlKey || event.metaKey)
        };
        updateMarqueeVisual();
        window.addEventListener("mousemove", onMarqueeMove);
        window.addEventListener("mouseup", onMarqueeEnd, true);
        event.preventDefault();
    }

    function clientPointToSvgUser(svg, clientX, clientY) {
        if (!svg.createSVGPoint) {
            const r = svg.getBoundingClientRect();
            return { x: clientX - r.left, y: clientY - r.top };
        }
        const pt = svg.createSVGPoint();
        pt.x = clientX;
        pt.y = clientY;
        const ctm = svg.getScreenCTM();
        if (!ctm) {
            const r = svg.getBoundingClientRect();
            return { x: clientX - r.left, y: clientY - r.top };
        }
        try {
            return pt.matrixTransform(ctm.inverse());
        } catch {
            const r = svg.getBoundingClientRect();
            return { x: clientX - r.left, y: clientY - r.top };
        }
    }

    function clientRectToSvgUserRect(svg, clientRect) {
        const tl = clientPointToSvgUser(svg, clientRect.left, clientRect.top);
        const tr = clientPointToSvgUser(svg, clientRect.right, clientRect.top);
        const bl = clientPointToSvgUser(svg, clientRect.left, clientRect.bottom);
        const br = clientPointToSvgUser(svg, clientRect.right, clientRect.bottom);
        const xs = [tl.x, tr.x, bl.x, br.x];
        const ys = [tl.y, tr.y, bl.y, br.y];
        return {
            left: Math.min(xs[0], xs[1], xs[2], xs[3]),
            right: Math.max(xs[0], xs[1], xs[2], xs[3]),
            top: Math.min(ys[0], ys[1], ys[2], ys[3]),
            bottom: Math.max(ys[0], ys[1], ys[2], ys[3]),
            width: 0,
            height: 0
        };
    }

    function connectionEndpoints(fromR, toR) {
        const fc = { x: (fromR.left + fromR.right) / 2, y: (fromR.top + fromR.bottom) / 2 };
        const tc = { x: (toR.left + toR.right) / 2, y: (toR.top + toR.bottom) / 2 };
        const dx = tc.x - fc.x;
        const dy = tc.y - fc.y;
        let x1;
        let y1 = fc.y;
        let x2;
        let y2 = tc.y;
        if (Math.abs(dx) >= Math.abs(dy)) {
            if (dx >= 0) {
                x1 = fromR.right;
                x2 = toR.left;
            } else {
                x1 = fromR.left;
                x2 = toR.right;
            }
        } else {
            if (dy >= 0) {
                y1 = fromR.bottom;
                y2 = toR.top;
                x1 = fc.x;
                x2 = tc.x;
            } else {
                y1 = fromR.top;
                y2 = toR.bottom;
                x1 = fc.x;
                x2 = tc.x;
            }
        }
        return { x1, y1, x2, y2 };
    }

    function cubicPath(x1, y1, x2, y2) {
        const dx = x2 - x1;
        const dy = y2 - y1;
        const t = 0.52;
        if (Math.abs(dx) >= Math.abs(dy)) {
            const cx1 = x1 + dx * t;
            const cx2 = x2 - dx * t;
            return `M ${x1} ${y1} C ${cx1} ${y1}, ${cx2} ${y2}, ${x2} ${y2}`;
        }
        const cy1 = y1 + dy * t;
        const cy2 = y2 - dy * t;
        return `M ${x1} ${y1} C ${x1} ${cy1}, ${x2} ${cy2}, ${x2} ${y2}`;
    }

    function linkGradientColors(fromNode, toNode) {
        const accent = accentForNode(toNode);
        return { c0: accent, c1: accent };
    }

    function removeGhostPath() {
        const g = linesSvg.querySelector("#mindmapGhostLink");
        if (g) {
            g.remove();
        }
    }

    function ensureSvgDefs() {
        let defs = linesSvg.querySelector("defs");
        if (!defs) {
            defs = document.createElementNS("http://www.w3.org/2000/svg", "defs");
            linesSvg.insertBefore(defs, linesSvg.firstChild);
        }
        return defs;
    }

    function drawLinksFromDom() {
        const defs = ensureSvgDefs();
        defs.querySelectorAll("linearGradient.mm-link-grad").forEach((g) => g.remove());
        linesSvg.querySelectorAll("path.mindmap-link").forEach((p) => p.remove());

        const depthMap = buildNodeDepthMap();
        const nodesById = {};
        canvas.querySelectorAll(".mindmap-node").forEach((el) => {
            const id = el.dataset.id;
            if (id) {
                nodesById[id] = el;
            }
        });

        state.links.forEach((link, idx) => {
            const fromEl = nodesById[link.from];
            const toEl = nodesById[link.to];
            if (!fromEl || !toEl) {
                return;
            }
            const fromNode = getNode(link.from);
            const toNode = getNode(link.to);
            if (!fromNode || !toNode) {
                return;
            }
            const fromR = clientRectToSvgUserRect(linesSvg, fromEl.getBoundingClientRect());
            const toR = clientRectToSvgUserRect(linesSvg, toEl.getBoundingClientRect());
            fromR.width = fromR.right - fromR.left;
            fromR.height = fromR.bottom - fromR.top;
            toR.width = toR.right - toR.left;
            toR.height = toR.bottom - toR.top;
            const { x1, y1, x2, y2 } = connectionEndpoints(fromR, toR);
            const { c0, c1 } = linkGradientColors(fromNode, toNode);
            const fromDepth = depthMap.get(fromNode.id) ?? 0;
            const toDepth = depthMap.get(toNode.id) ?? 0;
            let strokeW = 4;
            if (fromDepth === 0 && toDepth === 1) {
                strokeW = 6.5;
            } else if (toDepth >= 2) {
                strokeW = 2.25;
            }
            const gradId = "mm-lg-" + idx;
            const lg = document.createElementNS("http://www.w3.org/2000/svg", "linearGradient");
            lg.setAttribute("class", "mm-link-grad");
            lg.setAttribute("id", gradId);
            lg.setAttribute("gradientUnits", "userSpaceOnUse");
            lg.setAttribute("x1", String(x1));
            lg.setAttribute("y1", String(y1));
            lg.setAttribute("x2", String(x2));
            lg.setAttribute("y2", String(y2));
            const stop0 = document.createElementNS("http://www.w3.org/2000/svg", "stop");
            stop0.setAttribute("offset", "0%");
            stop0.setAttribute("stop-color", c0);
            const stop1 = document.createElementNS("http://www.w3.org/2000/svg", "stop");
            stop1.setAttribute("offset", "100%");
            stop1.setAttribute("stop-color", c1);
            lg.appendChild(stop0);
            lg.appendChild(stop1);
            defs.appendChild(lg);

            const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
            path.setAttribute("class", "mindmap-link");
            path.setAttribute("d", cubicPath(x1, y1, x2, y2));
            path.setAttribute("fill", "none");
            path.setAttribute("stroke", "url(#" + gradId + ")");
            path.setAttribute("stroke-width", String(strokeW));
            path.setAttribute("stroke-linecap", "round");
            path.setAttribute("stroke-linejoin", "round");
            linesSvg.appendChild(path);
        });

        if (linkDrag) {
            updateGhostLink(linkDrag.x, linkDrag.y);
        }
    }

    function updateGhostLink(endX, endY) {
        const from = getNode(linkDrag.fromId);
        if (!from) {
            return;
        }
        const fromEl = canvas.querySelector('.mindmap-node[data-id="' + linkDrag.fromId + '"]');
        if (!fromEl) {
            return;
        }
        const fromR = clientRectToSvgUserRect(linesSvg, fromEl.getBoundingClientRect());
        fromR.width = fromR.right - fromR.left;
        fromR.height = fromR.bottom - fromR.top;
        const fc = { x: (fromR.left + fromR.right) / 2, y: (fromR.top + fromR.bottom) / 2 };
        const x1 = fc.x < endX ? fromR.right : fromR.left;
        const y1 = fc.y;
        let ghost = linesSvg.querySelector("#mindmapGhostLink");
        if (!ghost) {
            ghost = document.createElementNS("http://www.w3.org/2000/svg", "path");
            ghost.setAttribute("id", "mindmapGhostLink");
            ghost.setAttribute("fill", "none");
            ghost.setAttribute("stroke", "#10b981");
            ghost.setAttribute("stroke-width", "3");
            ghost.setAttribute("stroke-dasharray", "8 6");
            linesSvg.appendChild(ghost);
        }
        ghost.setAttribute("d", cubicPath(x1, y1, endX, endY));
    }

    function scheduleDrawLinks() {
        window.requestAnimationFrame(() => drawLinksFromDom());
    }

    function clearLinkTargets() {
        linkHoverId = null;
        canvas.querySelectorAll(".mindmap-node.link-drop-target").forEach((el) => {
            el.classList.remove("link-drop-target");
        });
    }

    function commitInlineEdit(nodeId) {
        const id = nodeId || editingNodeId;
        if (!id) {
            return;
        }
        const wrap = canvas.querySelector('.mindmap-node[data-id="' + id + '"]');
        const input = wrap && wrap.querySelector(".mindmap-node__input");
        const node = getNode(id);
        if (input && node) {
            node.text = input.value.trim() || "بدون عنوان";
            saveState();
        }
        if (editingNodeId === id) {
            editingNodeId = null;
        }
        render();
    }

    function cancelInlineEdit() {
        if (!editingNodeId) {
            return;
        }
        skipInlineBlurCommit = true;
        editingNodeId = null;
        render();
        setTimeout(function () {
            skipInlineBlurCommit = false;
        }, 0);
    }

    function startInlineEdit(nodeId) {
        if (editingNodeId && editingNodeId !== nodeId) {
            commitInlineEdit(editingNodeId);
        }
        editingNodeId = nodeId;
        syncSingleNodeSelection(nodeId);
        render();
    }

    function handleNodeLabelClick(nodeId, event) {
        if (suppressNextCanvasClick) {
            suppressNextCanvasClick = false;
            return;
        }
        if (event.ctrlKey || event.metaKey || event.shiftKey) {
            return;
        }
        event.stopPropagation();
        if (selectedNodeIds.size > 1) {
            return;
        }
        if (editingNodeId === nodeId) {
            return;
        }
        if (selectedNodeId === nodeId) {
            startInlineEdit(nodeId);
            return;
        }
        selectNode(nodeId);
    }

    function refreshSelectionVisuals() {
        canvas.querySelectorAll(".mindmap-node").forEach((el) => {
            const id = el.dataset.id;
            el.classList.toggle("active", !!id && selectedNodeIds.has(id));
            el.classList.toggle("connect-source", !!id && id === connectSourceId);
            el.classList.toggle(
                "link-drop-target",
                !!(id && linkDrag && id !== linkDrag.fromId && id === linkHoverId)
            );
        });
    }

    function applySelectionUi() {
        refreshSelectionVisuals();
        syncPanel();
    }

    function render() {
        canvas.innerHTML = "";
        removeGhostPath();

        const depthMap = buildNodeDepthMap();

        state.nodes.forEach((node) => {
            const cluster = document.createElement("div");
            cluster.className = "mindmap-node-cluster";
            cluster.dataset.nodeId = node.id;
            cluster.style.left = node.x + "px";
            cluster.style.top = node.y + "px";

            const wrap = document.createElement("div");
            wrap.className = "mindmap-node";
            wrap.dataset.id = node.id;
            const depth = depthMap.get(node.id) ?? 0;
            const isRoot = depth === 0;
            const isPrimary = depth === 1;
            const isLeaf = depth >= 2;
            wrap.classList.toggle("mindmap-node--root", isRoot);
            wrap.classList.toggle("mindmap-node--primary", isPrimary);
            wrap.classList.toggle("mindmap-node--leaf", isLeaf);
            const hasSize = typeof node.w === "number" || typeof node.h === "number";
            wrap.classList.toggle("mindmap-node--sized", hasSize);
            applyNodeSizing(wrap, node.fontSize);
            if (isRoot) {
                wrap.style.backgroundColor = node.background;
                wrap.style.color = node.textColor;
            } else {
                const accent = accentForNode(node);
                wrap.style.setProperty("--mm-accent", accent);
                wrap.style.color = accent;
            }

            if (selectedNodeIds.has(node.id)) {
                wrap.classList.add("active");
            }
            if (node.id === connectSourceId) {
                wrap.classList.add("connect-source");
            }
            if (node.id === linkHoverId && linkDrag && node.id !== linkDrag.fromId) {
                wrap.classList.add("link-drop-target");
            }

            if (typeof node.w === "number") {
                wrap.style.width = node.w + "px";
            } else {
                wrap.style.removeProperty("width");
            }
            if (typeof node.h === "number") {
                wrap.style.height = node.h + "px";
            } else {
                wrap.style.removeProperty("height");
            }

            const stack = document.createElement("div");
            stack.className = "mindmap-node__stack";

            const actions = document.createElement("div");
            actions.className = "mindmap-node__actions";
            const delBtn = document.createElement("button");
            delBtn.type = "button";
            delBtn.className = "mindmap-node__delete";
            delBtn.title = "حذف نود";
            delBtn.setAttribute("aria-label", "حذف نود");
            delBtn.innerHTML = '<i class="bi bi-trash" aria-hidden="true"></i>';
            delBtn.addEventListener("click", (e) => {
                e.stopPropagation();
                e.preventDefault();
                deleteNode(node.id);
            });
            delBtn.addEventListener("mousedown", (e) => {
                e.stopPropagation();
            });
            actions.appendChild(delBtn);
            stack.appendChild(actions);

            if (node.id === editingNodeId) {
                const input = document.createElement("input");
                input.type = "text";
                input.className = "mindmap-node__input";
                input.value = node.text;
                input.setAttribute("aria-label", "متن نود");
                input.addEventListener("click", (e) => e.stopPropagation());
                input.addEventListener("dblclick", (e) => e.stopPropagation());
                input.addEventListener("keydown", (e) => {
                    if (e.key === "Enter") {
                        e.preventDefault();
                        commitInlineEdit(node.id);
                    }
                    if (e.key === "Escape") {
                        e.preventDefault();
                        e.stopPropagation();
                        cancelInlineEdit();
                    }
                });
                input.addEventListener("blur", () => {
                    setTimeout(() => {
                        if (skipInlineBlurCommit) {
                            return;
                        }
                        if (editingNodeId === node.id) {
                            commitInlineEdit(node.id);
                        }
                    }, 0);
                });
                stack.appendChild(input);
                window.requestAnimationFrame(() => {
                    input.focus();
                    input.select();
                });
            } else {
                const row = document.createElement("div");
                row.className = "mindmap-node__label-row";
                if (isPrimary) {
                    const badge = document.createElement("span");
                    badge.className = "mindmap-node__badge";
                    badge.textContent = String(siblingOrdinal(node.id));
                    row.appendChild(badge);
                }
                const label = document.createElement("span");
                label.className = "mindmap-node__label";
                if (isLeaf) {
                    label.classList.add("mindmap-node__label--leaf");
                }
                label.textContent = node.text;
                label.title = "برای ویرایش روی عنوان کلیک کنید";
                label.addEventListener("click", (e) => {
                    handleNodeLabelClick(node.id, e);
                });
                label.addEventListener("dblclick", (e) => {
                    e.stopPropagation();
                    e.preventDefault();
                    startInlineEdit(node.id);
                });
                row.appendChild(label);
                stack.appendChild(row);
            }

            const foot = document.createElement("div");
            foot.className = "mindmap-node__footer mindmap-node__footer--add-only";
            const addChip = document.createElement("button");
            addChip.type = "button";
            addChip.className = "mindmap-node__chip mindmap-node__chip--add";
            addChip.title = "افزودن زیرشاخه (نود فرزند)";
            addChip.setAttribute("aria-label", "افزودن زیرشاخه");
            addChip.innerHTML = '<i class="bi bi-plus-lg" aria-hidden="true"></i>';
            addChip.addEventListener("click", (e) => {
                e.stopPropagation();
                e.preventDefault();
                spawnChild(node.id, "زیرشاخه جدید");
            });
            foot.appendChild(addChip);
            stack.appendChild(foot);

            const linkHandle = document.createElement("div");
            linkHandle.className = "mindmap-node__link-handle";
            const connector = document.createElement("button");
            connector.type = "button";
            connector.className = "mindmap-node__connector";
            connector.title = "بکش تا به نود دیگر وصل شود";
            connector.setAttribute("aria-label", "کشیدن خط اتصال");
            connector.innerHTML = '<i class="bi bi-arrow-left-short" aria-hidden="true"></i>';
            connector.addEventListener("mousedown", (e) => {
                e.stopPropagation();
                e.preventDefault();
                startLinkDrag(node.id, e);
            });
            connector.addEventListener("click", (e) => {
                e.stopPropagation();
            });
            linkHandle.appendChild(connector);

            const resizeHandle = document.createElement("button");
            resizeHandle.type = "button";
            resizeHandle.className = "mindmap-node__resize-handle";
            resizeHandle.title = "تغییر اندازه (عرض و ارتفاع)";
            resizeHandle.setAttribute("aria-label", "تغییر اندازه نود");
            resizeHandle.addEventListener("mousedown", (e) => {
                e.stopPropagation();
                e.preventDefault();
                startResize(node.id, e);
            });

            wrap.appendChild(stack);
            wrap.appendChild(linkHandle);
            wrap.appendChild(resizeHandle);

            cluster.appendChild(wrap);
            canvas.appendChild(cluster);
        });

        syncPanel();
        drawLinksFromDom();
        applyViewportTransform();
    }

    function startResize(nodeId, event) {
        if (event.button !== 0) {
            return;
        }
        const node = getNode(nodeId);
        if (!node) {
            return;
        }
        // هنگام resize، اگر در حال ویرایش متن بودیم، اول commit کنیم تا اندازه ثابت بماند.
        if (editingNodeId && editingNodeId !== nodeId) {
            commitInlineEdit(editingNodeId);
        }
        syncSingleNodeSelection(nodeId);
        refreshSelectionVisuals();

        const shell = canvas.querySelector('.mindmap-node[data-id="' + nodeId + '"]');
        if (!shell) {
            return;
        }
        const rect = shell.getBoundingClientRect();
        const worldW = Math.max(1, rect.width / viewScale);
        const worldH = Math.max(1, rect.height / viewScale);
        resizeState = {
            id: nodeId,
            sx: event.clientX,
            sy: event.clientY,
            sw: typeof node.w === "number" ? node.w : worldW,
            sh: typeof node.h === "number" ? node.h : worldH
        };
        document.body.style.cursor = "nwse-resize";
        window.addEventListener("mousemove", onResizeMove);
        window.addEventListener("mouseup", onResizeEnd, true);
    }

    function onResizeMove(event) {
        if (!resizeState) {
            return;
        }
        const node = getNode(resizeState.id);
        if (!node) {
            return;
        }
        const dx = (event.clientX - resizeState.sx) / viewScale;
        const dy = (event.clientY - resizeState.sy) / viewScale;
        const nextW = Math.max(80, Math.round(resizeState.sw + dx));
        const nextH = Math.max(44, Math.round(resizeState.sh + dy));
        node.w = nextW;
        node.h = nextH;

        const shell = canvas.querySelector('.mindmap-node[data-id="' + resizeState.id + '"]');
        if (shell) {
            shell.style.width = nextW + "px";
            shell.style.height = nextH + "px";
        }
        scheduleDrawLinks();
    }

    function onResizeEnd() {
        if (!resizeState) {
            return;
        }
        window.removeEventListener("mousemove", onResizeMove);
        window.removeEventListener("mouseup", onResizeEnd, true);
        document.body.style.cursor = "";
        resizeState = null;
        saveState();
        scheduleDrawLinks();
    }

    function spawnChild(parentId, label) {
        const parent = getNode(parentId);
        if (!parent) {
            return;
        }
        const text = String(label || "").trim() || "زیرشاخه";
        const existingChildren = state.links.filter((l) => l.from === parentId).length;
        const gap = 56;
        const accent = nextChildAccent(parentId);
        const child = normalizeNode({
            id: createId(),
            text,
            x: Math.max(8, parent.x - 230),
            y: Math.max(8, parent.y + existingChildren * gap),
            background: "#ffffff",
            textColor: accent,
            accentColor: accent,
            fontSize: clampNodeFontSize(parent.fontSize - 1),
            branchDraft: "",
            childBranchLabels: []
        });
        state.nodes.push(child);
        state.links.push({ from: parentId, to: child.id });
        syncSingleNodeSelection(child.id);
        saveState();
        render();
    }

    function selectNode(nodeId) {
        if (editingNodeId && editingNodeId !== nodeId) {
            commitInlineEdit(editingNodeId);
        }
        syncSingleNodeSelection(nodeId);
        if (editingNodeId === nodeId) {
            syncPanel();
            scheduleDrawLinks();
            return;
        }
        applySelectionUi();
    }

    function setPanelEnabled(enabled) {
        nodeTextInput.disabled = !enabled;
        nodeColorInput.disabled = !enabled;
        nodeTextColorInput.disabled = !enabled;
        nodeFontSizeInput.disabled = !enabled;
    }

    function syncPanel() {
        if (selectedNodeIds.size > 1) {
            setPanelEnabled(false);
            selectionHint.textContent =
                selectedNodeIds.size +
                " نود انتخاب شده؛ برای ویرایش پنل فقط یک نود را انتخاب کن، یا با موس کادر بکش.";
            return;
        }

        const node = getNode(selectedNodeId);
        if (!node) {
            setPanelEnabled(false);
            selectionHint.textContent =
                "یک نود انتخاب کن؛ روی پس‌زمینه کادر بکش تا چند نود با هم انتخاب شوند؛ Ctrl/⌘+کلیک برای اضافه یا حذف از انتخاب؛ Shift+کلیک بازه در لیست نودها. اسکرول روی بوم برای زوم؛ کلیک وسط/راست یا Space+کشیدن برای جابجایی بوم.";
            return;
        }

        setPanelEnabled(true);
        selectionHint.textContent =
            "عنوان را روی بوم کلیک کنید تا همان‌جا ویرایش شود؛ یا از پنل کنار استفاده کنید.";
        nodeTextInput.value = node.text;
        if (hasIncomingLink(node.id)) {
            nodeColorInput.value = accentForNode(node);
            nodeTextColorInput.value = accentForNode(node);
        } else {
            nodeColorInput.value = node.background;
            nodeTextColorInput.value = node.textColor;
        }
        nodeFontSizeInput.value = String(clampNodeFontSize(node.fontSize));
        updateNodeFontSizeLabel(node.fontSize);
    }

    function addNode() {
        if (editingNodeId) {
            commitInlineEdit(editingNodeId);
        }
        const node = normalizeNode({
            id: createId(),
            text: "ایده جدید",
            x: 80 + (state.nodes.length % 5) * 35,
            y: 60 + state.nodes.length * 20,
            background: "#ffffff",
            textColor: "#0f172a",
            accentColor: "",
            branchDraft: "",
            childBranchLabels: []
        });
        state.nodes.push(node);
        syncSingleNodeSelection(node.id);
        saveState();
        render();
    }

    function deleteNode(nodeId) {
        if (editingNodeId === nodeId) {
            editingNodeId = null;
        }
        state.nodes = state.nodes.filter((n) => n.id !== nodeId);
        state.links = state.links.filter((l) => l.from !== nodeId && l.to !== nodeId);
        selectedNodeIds.delete(nodeId);
        if (selectedNodeId === nodeId) {
            selectedNodeId = selectedNodeIds.size ? Array.from(selectedNodeIds)[0] : null;
        }
        if (nodeRangeAnchorId === nodeId) {
            nodeRangeAnchorId = selectedNodeId;
        }
        if (selectedNodeId === null) {
            selectedNodeIds.clear();
        }
        if (selectedNodeId !== null && selectedNodeIds.size === 0) {
            selectedNodeIds.add(selectedNodeId);
        }
        if (connectSourceId === nodeId) {
            connectSourceId = null;
        }
        saveState();
        render();
    }

    function handleCanvasClick(event) {
        if (suppressNextCanvasClick) {
            suppressNextCanvasClick = false;
            return;
        }
        if (event.target.closest(".mindmap-node__connector") || event.target.closest(".mindmap-node__chip")) {
            return;
        }
        if (event.target.closest(".mindmap-node__delete")) {
            return;
        }
        if (event.target.closest(".mindmap-node__input")) {
            return;
        }

        const shell = getMindmapShellFromTarget(event.target);
        if (!shell) {
            clearNodeMultiSelection();
            if (editingNodeId) {
                commitInlineEdit(editingNodeId);
            } else {
                applySelectionUi();
            }
            return;
        }

        const nodeId = shell.dataset.id;
        if (!nodeId) {
            return;
        }

        const meta = event.ctrlKey || event.metaKey;
        const shift = event.shiftKey;
        if (meta) {
            if (selectedNodeIds.has(nodeId)) {
                selectedNodeIds.delete(nodeId);
                if (selectedNodeId === nodeId) {
                    selectedNodeId = selectedNodeIds.size ? Array.from(selectedNodeIds)[0] : null;
                }
            } else {
                selectedNodeIds.add(nodeId);
                selectedNodeId = nodeId;
            }
            if (selectedNodeId && !selectedNodeIds.has(selectedNodeId)) {
                selectedNodeId = Array.from(selectedNodeIds)[0] || null;
            }
            nodeRangeAnchorId = selectedNodeIds.size ? selectedNodeId : null;
            applySelectionUi();
            return;
        }
        if (shift) {
            const anchorId =
                (nodeRangeAnchorId && getNode(nodeRangeAnchorId) ? nodeRangeAnchorId : null) ||
                selectedNodeId ||
                nodeId;
            const ia = state.nodes.findIndex((n) => n.id === anchorId);
            const ib = state.nodes.findIndex((n) => n.id === nodeId);
            if (ia >= 0 && ib >= 0) {
                const lo = Math.min(ia, ib);
                const hi = Math.max(ia, ib);
                const rangeIds = state.nodes.slice(lo, hi + 1).map((n) => n.id);
                setNodeSelectionFromIds(rangeIds, nodeId);
                if (!nodeRangeAnchorId || !getNode(nodeRangeAnchorId)) {
                    nodeRangeAnchorId = anchorId;
                }
                applySelectionUi();
                return;
            }
        }
        selectNode(nodeId);
    }

    function startLinkDrag(fromId, event) {
        const p = clientPointToSvgUser(linesSvg, event.clientX, event.clientY);
        linkDrag = {
            fromId,
            x: p.x,
            y: p.y
        };
        canvas.classList.add("is-connect-drag");
        document.body.style.cursor = "crosshair";
        updateGhostLink(linkDrag.x, linkDrag.y);
        window.addEventListener("mousemove", onLinkDragMove);
        window.addEventListener("mouseup", onLinkDragEnd, true);
    }

    function setLinkDropHighlight(nextId) {
        const safeNext = nextId && linkDrag && nextId !== linkDrag.fromId ? nextId : null;
        if (safeNext === linkHoverId) {
            return;
        }
        canvas.querySelectorAll(".mindmap-node.link-drop-target").forEach((el) => {
            el.classList.remove("link-drop-target");
        });
        linkHoverId = safeNext;
        if (linkHoverId) {
            const target = canvas.querySelector('.mindmap-node[data-id="' + linkHoverId + '"]');
            if (target) {
                target.classList.add("link-drop-target");
            }
        }
    }

    function onLinkDragMove(event) {
        if (!linkDrag) {
            return;
        }
        const p = clientPointToSvgUser(linesSvg, event.clientX, event.clientY);
        linkDrag.x = p.x;
        linkDrag.y = p.y;
        updateGhostLink(linkDrag.x, linkDrag.y);

        const el = document.elementFromPoint(event.clientX, event.clientY);
        const shell = el ? getMindmapShellFromTarget(el) : null;
        const nextId = shell && shell.dataset.id ? shell.dataset.id : null;
        setLinkDropHighlight(nextId);
    }

    function onLinkDragEnd(event) {
        if (!linkDrag) {
            return;
        }
        window.removeEventListener("mousemove", onLinkDragMove);
        window.removeEventListener("mouseup", onLinkDragEnd, true);

        const el = document.elementFromPoint(event.clientX, event.clientY);
        const shell = el ? getMindmapShellFromTarget(el) : null;
        const targetId = shell && shell.dataset.id ? shell.dataset.id : null;

        canvas.classList.remove("is-connect-drag");
        document.body.style.cursor = "";
        removeGhostPath();
        clearLinkTargets();

        if (targetId && targetId !== linkDrag.fromId) {
            const dup = state.links.some((l) => l.from === linkDrag.fromId && l.to === targetId);
            if (!dup) {
                state.links.push({ from: linkDrag.fromId, to: targetId });
                ensureBranchThemeFor(targetId);
                saveState();
            }
            syncSingleNodeSelection(targetId);
        }

        linkDrag = null;
        render();
    }

    function cancelLinkDrag() {
        if (!linkDrag) {
            return;
        }
        window.removeEventListener("mousemove", onLinkDragMove);
        window.removeEventListener("mouseup", onLinkDragEnd, true);
        canvas.classList.remove("is-connect-drag");
        document.body.style.cursor = "";
        removeGhostPath();
        clearLinkTargets();
        linkDrag = null;
        render();
    }

    function startDrag(event) {
        if (resizeState) {
            return;
        }
        if (event.button === 0 && spacePanArmed && viewport && stage) {
            if (!event.target.closest(".mindmap-zoom-toolbar")) {
                panDragState = {
                    sx: event.clientX,
                    sy: event.clientY,
                    px: viewPanX,
                    py: viewPanY
                };
                event.preventDefault();
                return;
            }
        }
        if (event.button !== 0) {
            return;
        }
        if (
            !getMindmapShellFromTarget(event.target) &&
            (event.target === canvas || (viewport && event.target === viewport)) &&
            !linkDrag
        ) {
            startMarquee(event);
            return;
        }
        if (
            event.target.closest(".mindmap-node__connector") ||
            event.target.closest(".mindmap-node__chip") ||
            event.target.closest(".mindmap-node__delete") ||
            event.target.closest(".mindmap-node__input") ||
            event.target.closest(".mindmap-node__resize-handle")
        ) {
            return;
        }
        const shell = getMindmapShellFromTarget(event.target);
        if (!shell || event.button !== 0) {
            return;
        }
        const nodeId = shell.dataset.id;
        const node = getNode(nodeId);
        if (!node) {
            return;
        }
        const moveIds =
            selectedNodeIds.has(nodeId) && selectedNodeIds.size > 1
                ? Array.from(selectedNodeIds).filter((id) => getNode(id))
                : [node.id];
        const initial = {};
        moveIds.forEach((id) => {
            const n = getNode(id);
            if (n) {
                initial[id] = { x: n.x, y: n.y };
            }
        });
        dragState = {
            primaryId: node.id,
            ids: moveIds,
            startClientX: event.clientX,
            startClientY: event.clientY,
            initial,
            moved: false
        };
        event.preventDefault();
    }

    function onDrag(event) {
        if (!dragState) {
            return;
        }
        const dx = event.clientX - dragState.startClientX;
        const dy = event.clientY - dragState.startClientY;
        if (!dragState.moved && (Math.abs(dx) > 2 || Math.abs(dy) > 2)) {
            dragState.moved = true;
        }
        dragState.ids.forEach((id) => {
            const n = getNode(id);
            const ini = dragState.initial[id];
            if (!n || !ini) {
                return;
            }
            n.x = Math.max(0, ini.x + dx);
            n.y = Math.max(0, ini.y + dy);
            const cluster = canvas.querySelector('.mindmap-node-cluster[data-node-id="' + id + '"]');
            if (cluster) {
                cluster.style.left = n.x + "px";
                cluster.style.top = n.y + "px";
            }
        });
        scheduleDrawLinks();
    }

    function stopDrag() {
        if (!dragState) {
            return;
        }
        if (dragState.moved) {
            suppressNextCanvasClick = true;
        }
        dragState = null;
        saveState();
        scheduleDrawLinks();
    }

    addNodeBtn.addEventListener("click", addNode);
    clearBtn.addEventListener("click", () => {
        resetViewTransform();
        editingNodeId = null;
        clearNodeMultiSelection();
        state = { nodes: [], links: [] };
        connectSourceId = null;
        cancelLinkDrag();
        saveState();
        render();
    });
    exportBtn.addEventListener("click", () => {
        const blob = new Blob([JSON.stringify(state, null, 2)], { type: "application/json" });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = "mindmap.json";
        link.click();
        URL.revokeObjectURL(url);
    });

    if (importJsonBtn && importJsonInput) {
        importJsonBtn.addEventListener("click", () => {
            importJsonInput.value = "";
            importJsonInput.click();
        });
        importJsonInput.addEventListener("change", () => {
            const file = importJsonInput.files && importJsonInput.files[0];
            if (!file) {
                return;
            }
            const reader = new FileReader();
            reader.onload = () => {
                const text = typeof reader.result === "string" ? reader.result : "";
                let parsed;
                try {
                    parsed = JSON.parse(text);
                } catch {
                    window.alert("متن فایل JSON قابل خواندن نیست.");
                    return;
                }
                const r = buildRawMindmapStateFromParsed(parsed);
                if (!r.ok) {
                    window.alert(r.error);
                    return;
                }
                if (!window.confirm("نقشهٔ فعلی با محتوای این فایل جایگزین شود؟")) {
                    return;
                }
                applyImportedSnapshot(r.next);
            };
            reader.onerror = () => {
                window.alert("خواندن فایل انجام نشد.");
            };
            reader.readAsText(file, "UTF-8");
        });
    }

    if (exportWordBtn) {
        exportWordBtn.addEventListener("click", async () => {
            const url = exportWordBtn.getAttribute("data-export-url") || "/MindMaps/ExportWord";
            try {
                const res = await fetch(url, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    credentials: "same-origin",
                    body: JSON.stringify(state)
                });
                if (!res.ok) {
                    let msg = "ساخت فایل Word انجام نشد.";
                    try {
                        const err = await res.json();
                        if (err && err.error) {
                            msg = err.error;
                        }
                    } catch {
                        /* ignore */
                    }
                    window.alert(msg);
                    return;
                }
                const blob = await res.blob();
                const objUrl = URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = objUrl;
                a.download = "mindmap.docx";
                a.click();
                URL.revokeObjectURL(objUrl);
            } catch {
                window.alert("خطا در ارتباط با سرور برای خروجی Word.");
            }
        });
    }

    nodeTextInput.addEventListener("input", () => {
        const node = getNode(selectedNodeId);
        if (!node) {
            return;
        }
        node.text = nodeTextInput.value || "بدون عنوان";
        saveState();
        if (editingNodeId === selectedNodeId) {
            const inp = canvas.querySelector('.mindmap-node[data-id="' + selectedNodeId + '"] .mindmap-node__input');
            if (inp) {
                inp.value = node.text;
            }
            return;
        }
        render();
    });
    nodeColorInput.addEventListener("input", () => {
        const node = getNode(selectedNodeId);
        if (!node) {
            return;
        }
        if (hasIncomingLink(node.id)) {
            node.accentColor = nodeColorInput.value;
            node.textColor = nodeColorInput.value;
            node.background = "#ffffff";
        } else {
            node.background = nodeColorInput.value;
        }
        saveState();
        if (editingNodeId === selectedNodeId) {
            const wrap = canvas.querySelector('.mindmap-node[data-id="' + selectedNodeId + '"]');
            if (wrap) {
                if (hasIncomingLink(node.id)) {
                    wrap.style.setProperty("--mm-accent", accentForNode(node));
                } else {
                    wrap.style.backgroundColor = node.background;
                }
            }
            return;
        }
        render();
    });
    nodeTextColorInput.addEventListener("input", () => {
        const node = getNode(selectedNodeId);
        if (!node) {
            return;
        }
        if (hasIncomingLink(node.id)) {
            node.accentColor = nodeTextColorInput.value;
            node.textColor = nodeTextColorInput.value;
            node.background = "#ffffff";
        } else {
            node.textColor = nodeTextColorInput.value;
        }
        saveState();
        if (editingNodeId === selectedNodeId) {
            const wrap = canvas.querySelector('.mindmap-node[data-id="' + selectedNodeId + '"]');
            if (wrap) {
                if (hasIncomingLink(node.id)) {
                    wrap.style.setProperty("--mm-accent", accentForNode(node));
                } else {
                    wrap.style.color = node.textColor;
                }
            }
            return;
        }
        render();
    });
    nodeFontSizeInput.addEventListener("input", () => {
        const node = getNode(selectedNodeId);
        if (!node) {
            return;
        }
        node.fontSize = clampNodeFontSize(nodeFontSizeInput.value);
        nodeFontSizeInput.value = String(node.fontSize);
        updateNodeFontSizeLabel(node.fontSize);
        saveState();
        if (editingNodeId === selectedNodeId) {
            const wrap = canvas.querySelector('.mindmap-node[data-id="' + selectedNodeId + '"]');
            if (wrap) {
                applyNodeSizing(wrap, node.fontSize);
                scheduleDrawLinks();
            }
            return;
        }
        render();
    });

    function onStageWheel(event) {
        if (!viewport) {
            return;
        }
        event.preventDefault();
        const factor = event.deltaY > 0 ? 0.92 : 1 / 0.92;
        zoomAtClientPoint(event.clientX, event.clientY, factor);
    }

    function onStagePanMouseDown(event) {
        if (!viewport || (event.button !== 1 && event.button !== 2)) {
            return;
        }
        if (event.target.closest(".mindmap-zoom-toolbar")) {
            return;
        }
        panDragState = {
            sx: event.clientX,
            sy: event.clientY,
            px: viewPanX,
            py: viewPanY
        };
        if (event.button === 2) {
            event.preventDefault();
        }
    }

    function onWindowMouseMoveForViewport(event) {
        if (panDragState) {
            viewPanX = panDragState.px + (event.clientX - panDragState.sx);
            viewPanY = panDragState.py + (event.clientY - panDragState.sy);
            applyViewportTransform();
            scheduleDrawLinks();
            return;
        }
        onDrag(event);
    }

    function onWindowMouseUpForViewport(event) {
        if (event.button === 1 || event.button === 2) {
            panDragState = null;
        }
        stopDrag();
    }

    if (stage && viewport) {
        stage.addEventListener("wheel", onStageWheel, { passive: false });
        stage.addEventListener("mousedown", onStagePanMouseDown);
        stage.addEventListener("contextmenu", (e) => e.preventDefault());
    }
    if (zoomInBtn && stage && viewport) {
        zoomInBtn.addEventListener("click", () => {
            const r = stage.getBoundingClientRect();
            zoomAtClientPoint(r.left + r.width / 2, r.top + r.height / 2, 1.12);
        });
    }
    if (zoomOutBtn && stage && viewport) {
        zoomOutBtn.addEventListener("click", () => {
            const r = stage.getBoundingClientRect();
            zoomAtClientPoint(r.left + r.width / 2, r.top + r.height / 2, 1 / 1.12);
        });
    }
    if (zoomResetBtn && viewport) {
        zoomResetBtn.addEventListener("click", () => {
            resetViewTransform();
        });
    }

    canvas.addEventListener("click", handleCanvasClick);
    canvas.addEventListener("mousedown", startDrag);
    window.addEventListener("mousemove", onWindowMouseMoveForViewport);
    window.addEventListener("mouseup", onWindowMouseUpForViewport);

    window.addEventListener("keydown", (e) => {
        if (e.code === "Space" && !e.repeat) {
            const t = e.target;
            if (!(t && t.closest && t.closest("input, textarea, select, [contenteditable='true'], button, a"))) {
                spacePanArmed = true;
                e.preventDefault();
            }
        }
        if (e.key === "Escape") {
            if (selectedNodeIds.size > 1) {
                if (selectedNodeId && selectedNodeIds.has(selectedNodeId)) {
                    syncSingleNodeSelection(selectedNodeId);
                } else {
                    syncSingleNodeSelection(Array.from(selectedNodeIds)[0]);
                }
                e.preventDefault();
                applySelectionUi();
                return;
            }
            if (editingNodeId) {
                cancelInlineEdit();
                e.preventDefault();
                return;
            }
            if (linkDrag) {
                cancelLinkDrag();
            }
            spacePanArmed = false;
            connectSourceId = null;
            applySelectionUi();
        }
    });

    window.addEventListener("keyup", (e) => {
        if (e.code === "Space") {
            spacePanArmed = false;
        }
    });

    window.addEventListener("blur", () => {
        spacePanArmed = false;
    });

    loadState();
    render();
})();
