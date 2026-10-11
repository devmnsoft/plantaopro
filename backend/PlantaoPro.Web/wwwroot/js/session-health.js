(() => {
    "use strict";

    // R7-D-F1: os textos nao moram mais no JavaScript. A view (_AppTopbar.cshtml)
    // aporta as variantes localizadas via data-attrs (fonte: UiTexts.resx x4);
    // este script apenas alterna entre online/offline conforme o estado do navegador.
    const status = document.querySelector("[data-session-health]");
    const label = status?.querySelector("[data-session-health-label]");
    if (!status || !label) return;

    const attr = (nome, fallback) => {
        const valor = status.getAttribute(nome);
        return valor ? valor : fallback;
    };

    const render = () => {
        const online = navigator.onLine;
        status.dataset.online = String(online);
        label.textContent = online
            ? attr("data-online-text", label.textContent)
            : attr("data-offline-text", "");
        status.setAttribute("aria-label", online
            ? attr("data-aria-online", "online")
            : attr("data-aria-offline", "offline"));
    };

    window.addEventListener("online", render);
    window.addEventListener("offline", render);
    window.addEventListener("pageshow", render);
    render();
})();
