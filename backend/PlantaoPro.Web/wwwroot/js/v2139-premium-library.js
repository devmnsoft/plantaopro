(() => {
    "use strict";
    const clamp = value => Math.min(100, Math.max(0, Number.parseFloat(value) || 0));
    const safeColor = value => /^(#[0-9a-f]{3,8}|rgb[a]?\([0-9.,% ]+\)|hsl[a]?\([0-9.,% ]+\))$/i.test(value || "") ? value : null;
    document.querySelectorAll("[data-progress]").forEach(element => {
        const progress = clamp(element.dataset.progress);
        element.style.width = `${progress}%`;
        element.closest('[role="progressbar"]')?.setAttribute("aria-valuenow", String(progress));
    });
    document.querySelectorAll("[data-preview-background]").forEach(element => {
        const color = safeColor(element.dataset.previewBackground);
        if (color) element.style.backgroundColor = color;
    });
    document.querySelectorAll("[data-preview-color]").forEach(element => {
        const color = safeColor(element.dataset.previewColor);
        if (color) element.style.color = color;
    });
    document.querySelectorAll("[data-preview-gradient-start][data-preview-gradient-end]").forEach(element => {
        const start = safeColor(element.dataset.previewGradientStart);
        const end = safeColor(element.dataset.previewGradientEnd);
        if (start && end) {
            element.classList.add("pp-preview-gradient");
            element.style.backgroundImage = `linear-gradient(135deg, ${start}, ${end})`;
        }
    });
})();
