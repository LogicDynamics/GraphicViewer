window.GraphicViewer = {

    // Copy text to clipboard
    copyToClipboard: async function (text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch (err) {
            // Fallback for older browsers
            const ta = document.createElement('textarea');
            ta.value = text;
            ta.style.position = 'fixed';
            ta.style.opacity = '0';
            document.body.appendChild(ta);
            ta.focus();
            ta.select();
            const ok = document.execCommand('copy');
            document.body.removeChild(ta);
            return ok;
        }
    },

    // Normalize an SVG string — ensures viewBox, width, height are present
    // so Blazor can render it without collapsing to 0x0
    normalizeSvg: function (svgContent) {
        const parser = new DOMParser();
        const doc = parser.parseFromString(svgContent, 'image/svg+xml');
        const svg = doc.querySelector('svg');
        if (!svg) return svgContent;

        const w = svg.getAttribute('width');
        const h = svg.getAttribute('height');
        let vb = svg.getAttribute('viewBox');

        // Build viewBox from width/height if missing
        if (!vb && w && h) {
            const pw = parseFloat(w);
            const ph = parseFloat(h);
            if (!isNaN(pw) && !isNaN(ph)) {
                svg.setAttribute('viewBox', `0 0 ${pw} ${ph}`);
                vb = `0 0 ${pw} ${ph}`;
            }
        }

        // Build width/height from viewBox if missing
        if (vb && (!w || !h)) {
            const parts = vb.trim().split(/[\s,]+/);
            if (parts.length === 4) {
                if (!w) svg.setAttribute('width', parts[2]);
                if (!h) svg.setAttribute('height', parts[3]);
            }
        }

        // If still no dimensions, set a safe default
        if (!svg.getAttribute('viewBox')) {
            svg.setAttribute('viewBox', '0 0 100 100');
        }
        if (!svg.getAttribute('width'))  svg.setAttribute('width',  '100%');
        if (!svg.getAttribute('height')) svg.setAttribute('height', '100%');

        return new XMLSerializer().serializeToString(svg);
    }
};
