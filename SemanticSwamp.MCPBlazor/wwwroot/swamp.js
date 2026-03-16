/* Copied from SemanticSwamp.Blazor/wwwroot/swamp.js */
/* ---- Splash animation ---- */
window.splash = function (id) {
    const el = document.getElementById(id);
    if (!el) return;
    el.classList.remove("splash");
    void el.offsetWidth;
    el.classList.add("splash");
};

/* ---- Theme ---- */
window.getCurrentTheme = function () {
    return document.documentElement.getAttribute("data-theme") || "dawn";
};

window.getStoredTheme = function () {
    try {
        const s = localStorage.getItem("semantic-swamp-theme");
        if (s) return s;
    } catch { }
    return (window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches)
        ? "midnight" : "dawn";
};

window.applyTheme = function (theme) {
    document.documentElement.setAttribute("data-theme", theme);
    try { localStorage.setItem("semantic-swamp-theme", theme); } catch { }
};

/* ---- Waiting class ---- */
window.setWaitingClass = function (isWaiting) {
    document.documentElement.classList.toggle("is-waiting", !!isWaiting);
};

/* ---- Chat scroll ---- */
window.scrollChatToBottom = function (id) {
    const el = document.getElementById(id);
    if (el) el.scrollTop = el.scrollHeight;
};

/* ---- Bootstrap modals ---- */
window.showBootstrapModal = function (id) {
    const el = document.getElementById(id);
    if (el) bootstrap.Modal.getOrCreateInstance(el).show();
};

window.hideBootstrapModal = function (id) {
    const el = document.getElementById(id);
    if (el) {
        const m = bootstrap.Modal.getInstance(el);
        if (m) m.hide();
    }
};

/* ---- File input trigger ---- */
window.triggerFileInput = function (id) {
    const el = document.getElementById(id);
    if (el) el.click();
};

/* ---- File download ---- */
window.downloadFile = async function (url, filename) {
    try {
        const res = await fetch(url);
        if (!res.ok) throw new Error("Download failed: " + res.status);
        const blob = await res.blob();
        let name = filename || "download";
        const disp = res.headers.get("Content-Disposition") || "";
        const m = /filename\*\s*=\s*UTF-8''([^;]+)|filename\s*=\s*"?([^";]+)"?/i.exec(disp);
        if (m) name = decodeURIComponent((m[1] || m[2] || "").trim()) || name;
        const bUrl = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = bUrl; a.download = name;
        document.body.appendChild(a); a.click();
        setTimeout(() => { document.body.removeChild(a); URL.revokeObjectURL(bUrl); }, 100);
    } catch (e) { console.error(e); }
};

/* ---- Firefly canvas animation ---- */
(function initSwampCanvas() {
    const reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const canvas = document.getElementById("swampCanvas");
    if (!canvas || reduce) return;
    const ctx = canvas.getContext("2d", { alpha: true });
    if (!ctx) return;

    let w = 0, h = 0, dpr = 1;
    const mouse = { x: 0, y: 0, active: false };
    const rand = (min, max) => Math.random() * (max - min) + min;
    const clamp = (v, mn, mx) => Math.max(mn, Math.min(mx, v));
    const fireflies = [];
    const maxCount = clamp(Math.floor((window.innerWidth * window.innerHeight) / 26000), 22, 70);

    function resize() {
        dpr = window.devicePixelRatio || 1;
        w = window.innerWidth; h = window.innerHeight;
        canvas.width = Math.floor(w * dpr); canvas.height = Math.floor(h * dpr);
        canvas.style.width = w + "px"; canvas.style.height = h + "px";
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    }

    function newFly() {
        const base = Math.random();
        const hue = base < 0.33 ? rand(165, 190) : base < 0.66 ? rand(255, 280) : rand(310, 335);
        return {
            x: rand(0, w), y: rand(0, h), r: rand(1.0, 2.4), vx: rand(-0.25, 0.25),
            vy: rand(-0.2, 0.2), hue, phase: rand(0, Math.PI * 2),
            speed: rand(0.004, 0.012), alpha: rand(0.18, 0.45)
        };
    }

    function ensureCount() {
        while (fireflies.length < maxCount) fireflies.push(newFly());
        while (fireflies.length > maxCount) fireflies.pop();
    }

    function step(t) {
        ctx.clearRect(0, 0, w, h);
        ctx.globalCompositeOperation = "lighter";
        for (const f of fireflies) {
            f.phase += f.speed * (1 + Math.random() * 0.05);
            f.x += f.vx + Math.cos(f.phase) * 0.18;
            f.y += f.vy + Math.sin(f.phase * 0.9) * 0.16;
            if (mouse.active) {
                const dx = mouse.x - f.x, dy = mouse.y - f.y;
                const dist = Math.sqrt(dx * dx + dy * dy) || 1;
                const pull = clamp(180 / dist, 0, 1) * 0.002;
                f.x += dx * pull; f.y += dy * pull;
            }
            if (f.x < -20) f.x = w + 20; if (f.x > w + 20) f.x = -20;
            if (f.y < -20) f.y = h + 20; if (f.y > h + 20) f.y = -20;
            const pulse = 0.55 + 0.45 * Math.sin(f.phase * 2.2 + t * 0.001);
            const a = f.alpha * pulse;
            const g = ctx.createRadialGradient(f.x, f.y, 0, f.x, f.y, f.r * 12);
            g.addColorStop(0, `hsla(${f.hue},95%,72%,${a})`);
            g.addColorStop(1, `hsla(${f.hue},95%,72%,0)`);
            ctx.fillStyle = g;
            ctx.beginPath(); ctx.arc(f.x, f.y, f.r * 12, 0, Math.PI * 2); ctx.fill();
        }
        ctx.globalCompositeOperation = "source-over";
        requestAnimationFrame(step);
    }

    resize(); ensureCount(); requestAnimationFrame(step);
    window.addEventListener("resize", () => { resize(); ensureCount(); });
    window.addEventListener("mousemove", e => { mouse.x = e.clientX; mouse.y = e.clientY; mouse.active = true; });
    window.addEventListener("mouseleave", () => (mouse.active = false));
})();
