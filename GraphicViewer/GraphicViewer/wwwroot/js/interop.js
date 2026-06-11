window.GraphicViewer = {

    // ── Clipboard ─────────────────────────────────────────────────────────────
    copyToClipboard: async function (text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch (err) {
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

    // ── SVG Normalization ─────────────────────────────────────────────────────
    normalizeSvg: function (svgContent) {
        const parser = new DOMParser();
        const doc = parser.parseFromString(svgContent, 'image/svg+xml');
        const svg = doc.querySelector('svg');
        if (!svg) return svgContent;
        const w = svg.getAttribute('width');
        const h = svg.getAttribute('height');
        let vb = svg.getAttribute('viewBox');
        if (!vb && w && h) {
            const pw = parseFloat(w), ph = parseFloat(h);
            if (!isNaN(pw) && !isNaN(ph)) {
                svg.setAttribute('viewBox', `0 0 ${pw} ${ph}`);
                vb = `0 0 ${pw} ${ph}`;
            }
        }
        if (vb && (!w || !h)) {
            const parts = vb.trim().split(/[\s,]+/);
            if (parts.length === 4) {
                if (!w) svg.setAttribute('width', parts[2]);
                if (!h) svg.setAttribute('height', parts[3]);
            }
        }
        if (!svg.getAttribute('viewBox')) svg.setAttribute('viewBox', '0 0 100 100');
        if (!svg.getAttribute('width')) svg.setAttribute('width', '100%');
        if (!svg.getAttribute('height')) svg.setAttribute('height', '100%');
        return new XMLSerializer().serializeToString(svg);
    },

    // ── Image Download ────────────────────────────────────────────────────────
    downloadImage: function (dataUrl, fileName) {
        const a = document.createElement('a');
        a.href = dataUrl;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    },

    // ── Blob URL Management ───────────────────────────────────────────────────
    createBlobUrl: function (bytes, mimeType) {
        const blob = new Blob([new Uint8Array(bytes)], { type: mimeType });
        return URL.createObjectURL(blob);
    },

    revokeBlobUrl: function (url) {
        if (url && url.startsWith('blob:')) URL.revokeObjectURL(url);
    },

    // ── Video / Audio Controls ────────────────────────────────────────────────
    stopVideo: function (el) {
        if (el) { el.pause(); el.currentTime = 0; }
    },

    playVideo: function (el) {
        if (el) el.play();
    },

    pauseVideo: function (el) {
        if (el) el.pause();
    },

    setVolume: function (el, volume) {
        if (el) el.volume = Math.max(0, Math.min(1, volume));
    },

    // ── Audio-specific ────────────────────────────────────────────────────────
    seekAudio: function (el, time) {
        if (el && isFinite(time)) el.currentTime = time;
    },

    getAudioTimes: function (el) {
        if (!el) return [0, 0];
        return [
            isFinite(el.currentTime) ? el.currentTime : 0,
            isFinite(el.duration) ? el.duration : 0
        ];
    },

    // Power Off button ------------------------------------------------------------
    navigateTo: function (url) {
        window.location.href = url;
    },


    // ── localStorage (YouTube playlist persistence) ───────────────────────────
    setLocalStorage: function (key, value) {
        try {
            localStorage.setItem(key, value);
            return true;
        } catch (e) {
            console.warn('localStorage write failed:', e);
            return false;
        }
    },

    getLocalStorage: function (key) {
        try {
            return localStorage.getItem(key);
        } catch (e) {
            console.warn('localStorage read failed:', e);
            return null;
        }
    },

    removeLocalStorage: function (key) {
        try {
            localStorage.removeItem(key);
        } catch (e) {
            console.warn('localStorage remove failed:', e);
        }
    }
};
