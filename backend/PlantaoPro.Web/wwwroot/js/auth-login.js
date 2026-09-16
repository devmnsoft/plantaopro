(() => {
    "use strict";
    const form = document.getElementById("loginForm");
    const password = document.getElementById("senha");
    const toggle = document.querySelector("[data-password-toggle]");
    const warning = document.getElementById("capsLockWarning");
    const button = document.getElementById("btnLogin");
    const errorSummary = form?.querySelector("[data-login-errors]");
    const delayMessage = form?.querySelector("[data-login-delay]");
    const connectionStatus = form?.querySelector("[data-connection-status]");
    const progress = form?.querySelector("[data-login-progress]");
    const progressTitle = progress?.querySelector("[data-login-progress-title]");
    const progressDetail = progress?.querySelector("[data-login-progress-detail]");
    const idleLabel = button?.dataset.idleLabel || "Entrar";
    const resetSubmission = () => {
        if (form) delete form.dataset.requestStarted;
        button?.removeAttribute("disabled");
        button?.setAttribute("aria-busy", "false");
        button?.querySelector(".spinner-border")?.classList.add("d-none");
        delayMessage?.classList.add("d-none");
        const label = button?.querySelector(".label");
        if (label) label.textContent = idleLabel;
        progress?.setAttribute("hidden", "hidden");
    };

    const setProgress = (title, detail) => {
        if (!progress) return;
        progress.removeAttribute("hidden");
        if (progressTitle) progressTitle.textContent = title;
        if (progressDetail) progressDetail.textContent = detail;
    };

    const showValidationMessage = () => {
        resetSubmission();
        if (errorSummary) {
            errorSummary.textContent = "Revise os campos destacados antes de continuar.";
            errorSummary.classList.add("validation-summary-errors");
        }
    };

    const updateConnectionStatus = () => {
        if (!connectionStatus) return;
        connectionStatus.hidden = navigator.onLine;
        const requestInFlight = form?.dataset.requestStarted === "true";
        button?.toggleAttribute("disabled", !navigator.onLine || requestInFlight);
        if (!navigator.onLine) button?.setAttribute("aria-disabled", "true");
        else button?.removeAttribute("aria-disabled");
    };

    toggle?.addEventListener("click", () => {
        const reveal = password?.type === "password";
        if (password) password.type = reveal ? "text" : "password";
        toggle.setAttribute("aria-label", reveal ? "Ocultar senha" : "Mostrar senha");
        toggle.setAttribute("aria-pressed", String(reveal));
        password?.focus();
    });

    password?.addEventListener("keyup", event => {
        warning?.classList.toggle("d-none", !event.getModifierState("CapsLock"));
    });

    password?.addEventListener("blur", () => warning?.classList.add("d-none"));

    form?.addEventListener("submit", event => {
        if (event.defaultPrevented) return;
        const invalid = form.querySelector(":invalid");
        if (invalid instanceof HTMLElement) {
            event.preventDefault();
            showValidationMessage();
            invalid.setAttribute("aria-invalid", "true");
            invalid.focus();
            return;
        }
        if (button?.getAttribute("aria-busy") === "true") {
            event.preventDefault();
            return;
        }
        // O evento submit só ocorre depois que a validação nativa aceita os
        // campos. A partir daqui o navegador fará um POST real para form.action.
        form.dataset.requestStarted = "true";
        button?.setAttribute("aria-busy", "true");
        button?.setAttribute("disabled", "disabled");
        button?.querySelector(".spinner-border")?.classList.remove("d-none");
        const label = button?.querySelector(".label");
        if (label) label.textContent = "Verificando acesso…";
        setProgress("Validando acesso com segurança", "Aguarde enquanto conferimos sua conta e o contexto autorizado.");

        // O POST é nativo. O timeout pertence ao HttpClient do servidor, que encerra
        // a operação antes de devolver esta view; nunca liberamos um segundo POST
        // enquanto a primeira requisição ainda está em andamento.
    });

    form?.addEventListener("invalid", showValidationMessage, true);
    window.addEventListener("pageshow", () => {
        resetSubmission();
        updateConnectionStatus();
    });
    window.addEventListener("offline", () => {
        updateConnectionStatus();
        // A submissão nativa continua sob responsabilidade do servidor. Não
        // reabilite o botão enquanto esse POST ainda pode estar em andamento.
    });
    window.addEventListener("online", updateConnectionStatus);

    if (errorSummary?.textContent?.trim()) {
        resetSubmission();
        window.queueMicrotask(() => errorSummary.focus());
    }
    updateConnectionStatus();
})();
