(() => {
    const btn = document.getElementById("menuBtn");
    const menu = document.getElementById("mobileMenu");
    if (btn && menu) {
        btn.addEventListener("click", () => menu.classList.toggle("hidden"));
    }

    // Dark mode toggle (desktop + mobile buttons)
    const htmlRoot = document.documentElement;
    function applyThemeIcons(isDark) {
        document.querySelectorAll(".theme-toggle").forEach(btn => {
            const sun = btn.querySelector("[data-sun]");
            const moon = btn.querySelector("[data-moon]");
            if (sun && moon) {
                sun.classList.toggle("hidden", isDark);
                moon.classList.toggle("hidden", !isDark);
            }
        });
    }
    document.querySelectorAll(".theme-toggle").forEach(btn => {
        btn.addEventListener("click", () => {
            const isDark = htmlRoot.classList.toggle("dark");
            localStorage.setItem("subchron-theme", isDark ? "dark" : "light");
            applyThemeIcons(isDark);
        });
    });
    applyThemeIcons(htmlRoot.classList.contains("dark"));

    // Smooth scroll for in-page anchors like /Features#qr
    document.querySelectorAll('a[href*="#"]').forEach(a => {
        a.addEventListener("click", (e) => {
            const href = a.getAttribute("href") || "";
            const idx = href.indexOf("#");
            if (idx === -1) return;
            const id = href.slice(idx + 1);
            const el = document.getElementById(id);
            if (!el) return;
            e.preventDefault();
            el.scrollIntoView({ behavior: "smooth", block: "start" });
            history.pushState(null, "", href);
        });
    });
})();
