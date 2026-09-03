(() => {
    "use strict";
    const form = document.getElementById("loginForm");
    const password = document.getElementById("senha");
    const toggle = document.querySelector("[data-password-toggle]");
    const warning = document.getElementById("capsLockWarning");
    const button = document.getElementById("btnLogin");
    const errorSummary = form?.querySelector("[data-login-errors]");
    const delayMessage = form?.querySelector("[data-login-delay]");
    const idleLabel = button?.dataset.idleLabel || "Entrar";
    let recoveryTimer;

    const resetSubmission = () => {
        window.clearTimeout(recoveryTimer);
        button?.removeAttribute("disabled");
        button?.setAttribute("aria-busy", "false");
        button?.querySelector(".spinner-border")?.classList.add("d-none");
        delayMessage?.classList.add("d-none");
        const label = button?.querySelector(".label");
        if (label) label.textContent = idleLabel;
    };

    const showValidationMessage = () => {
        resetSubmission();
        if (errorSummary) {
            errorSummary.textContent = "Revise os campos destacados antes de continuar.";
            errorSummary.classList.add("validation-summary-errors");
        }
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

        // Um POST tradicional deve navegar para outra página. Se a navegação for
        // interrompida pelo navegador, este limite devolve o controle ao usuário.
        recoveryTimer = window.setTimeout(() => {
            resetSubmission();
            delayMessage?.classList.remove("d-none");
            const liveRegion = document.getElementById("appLiveRegion");
            if (liveRegion) liveRegion.textContent = "A resposta está demorando. Você pode tentar entrar novamente.";
        }, 15000);
    });

    form?.addEventListener("invalid", showValidationMessage, true);
    window.addEventListener("pageshow", resetSubmission);
    window.addEventListener("offline", () => {
        if (button?.getAttribute("aria-busy") !== "true") return;
        resetSubmission();
        if (errorSummary) {
            errorSummary.textContent = "A conexão foi interrompida. Verifique sua internet e tente novamente.";
            errorSummary.classList.add("validation-summary-errors");
            errorSummary.focus();
        }
    });

    if (errorSummary?.textContent?.trim()) {
        resetSubmission();
        window.setTimeout(() => errorSummary.focus(), 0);
    }
})();
