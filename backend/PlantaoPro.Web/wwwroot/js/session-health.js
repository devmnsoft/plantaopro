(() => {
    "use strict";

    const status = document.querySelector("[data-session-health]");
    const label = status?.querySelector("[data-session-health-label]");
    if (!status || !label) return;

    const render = () => {
        const online = navigator.onLine;
        status.dataset.online = String(online);
        label.textContent = online ? "Protegida" : "Sem conexão";
        status.setAttribute("aria-label", online
            ? "Conexão protegida com a plataforma"
            : "Sem conexão. Evite concluir alterações até reconectar.");
    };

    window.addEventListener("online", render);
    window.addEventListener("offline", render);
    window.addEventListener("pageshow", render);
    render();
})();
