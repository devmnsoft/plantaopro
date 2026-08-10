(() => {
    "use strict";
    const hostId = "plantaopro-toast-host";
    const config = {
        success: { title: "Tudo certo", icon: "✓", timeout: 5000 },
        error: { title: "Não foi possível concluir", icon: "!", timeout: 9000 },
        warning: { title: "Atenção", icon: "!", timeout: 7000 },
        info: { title: "Informação", icon: "i", timeout: 6000 },
        loading: { title: "Processando", icon: "↻", timeout: 30000 },
        update: { title: "Atualização", icon: "↻", timeout: 6000 },
        delete: { title: "Item excluído", icon: "×", timeout: 7000 }
    };

    function host() {
        let element = document.getElementById(hostId);
        if (!element) {
            element = document.createElement("section");
            element.id = hostId;
            element.className = "pp-toast-host";
            element.setAttribute("aria-label", "Notificações");
            element.setAttribute("aria-live", "polite");
            element.setAttribute("aria-relevant", "additions");
            document.body.appendChild(element);
        }
        return element;
    }

    function normalize(type, message, options) {
        const safeType = Object.hasOwn(config, type) ? type : "info";
        const settings = typeof options === "number" ? { timeout: options } : (options || {});
        return { type: safeType, message: String(message || ""), ...config[safeType], ...settings };
    }

    function show(type, message, options) {
        const toast = normalize(type, message, options);
        if (!toast.message) return null;
        const item = document.createElement("article");
        item.className = `pp-toast pp-toast--${toast.type}`;
        item.setAttribute("role", toast.type === "error" ? "alert" : "status");
        item.innerHTML = `<span class="pp-toast__icon" aria-hidden="true"></span><div><strong class="pp-toast__title"></strong><div class="pp-toast__message"></div></div>`;
        item.querySelector(".pp-toast__icon").textContent = toast.icon;
        item.querySelector(".pp-toast__title").textContent = toast.title;
        item.querySelector(".pp-toast__message").textContent = toast.message;
        const close = document.createElement("button");
        close.type = "button";
        close.className = "btn-close";
        close.setAttribute("aria-label", "Fechar notificação");
        close.addEventListener("click", () => item.remove());
        item.appendChild(close);
        host().appendChild(item);
        requestAnimationFrame(() => item.classList.add("show"));
        if (toast.timeout > 0) window.setTimeout(() => item.remove(), toast.timeout);
        return { close: () => item.remove(), element: item };
    }

    window.PlantaoProToast = { show };
    Object.keys(config).forEach(type => { window.PlantaoProToast[type] = (message, options) => show(type, message, options); });
})();
