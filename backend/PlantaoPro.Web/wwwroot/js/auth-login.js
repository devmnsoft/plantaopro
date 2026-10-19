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

    // R63 - login guiado: identifica em tempo real qual identificador esta sendo
    // digitado (e-mail / CPF / CNPJ), aplica mascara progressiva, guia a quantidade
    // de digitos e valida os digitos verificadores oficiais. Espelha a regra canonica
    // do servidor (LoginIdentifierNormalizer/LoginIdentifierChecksums): "@" = e-mail,
    // 11 digitos = CPF, 14 digitos = CNPJ. Documento com erro claro nao sai do
    // navegador, protegendo o usuario de perder tentativas e alcancar o bloqueio.
    const identifierField = form?.querySelector("[data-login-identifier]");
    const identifierGuide = form?.querySelector("[data-identifier-guide]");

    const onlyDigits = value => (value || "").replace(/\D/g, "");
    const cpfDigitsValid = d => {
        if (d.length !== 11 || /^(\d)\1{10}$/.test(d)) return false;
        const dv = (base, firstWeight) => {
            let sum = 0;
            for (let i = 0; i < base.length; i++) sum += (base.charCodeAt(i) - 48) * (firstWeight - i);
            const rest = (sum * 10) % 11;
            return rest === 10 ? 0 : rest;
        };
        return dv(d.slice(0, 9), 10) === d.charCodeAt(9) - 48 && dv(d.slice(0, 10), 11) === d.charCodeAt(10) - 48;
    };
    const cnpjDigitsValid = d => {
        if (d.length !== 14 || /^(\d)\1{13}$/.test(d)) return false;
        const dv = (base, weights) => {
            let sum = 0;
            for (let i = 0; i < base.length; i++) sum += (base.charCodeAt(i) - 48) * weights[i];
            const rest = sum % 11;
            return rest < 2 ? 0 : 11 - rest;
        };
        const w1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        const w2 = [6].concat(w1);
        return dv(d.slice(0, 12), w1) === d.charCodeAt(12) - 48 && dv(d.slice(0, 13), w2) === d.charCodeAt(13) - 48;
    };
    const formatDigits = (kind, digits) => {
        let out;
        if (kind === "cpf") {
            out = digits.slice(0, 3);
            if (digits.length > 3) out += "." + digits.slice(3, 6);
            if (digits.length > 6) out += "." + digits.slice(6, 9);
            if (digits.length > 9) out += "-" + digits.slice(9, 11);
        } else {
            out = digits.slice(0, 2);
            if (digits.length > 2) out += "." + digits.slice(2, 5);
            if (digits.length > 5) out += "." + digits.slice(5, 8);
            if (digits.length > 8) out += "/" + digits.slice(8, 12);
            if (digits.length > 12) out += "-" + digits.slice(12, 14);
        }
        return out;
    };
    const identifierState = raw => {
        const value = (raw || "").trim();
        if (!value) return "empty";
        if (value.includes("@")) return "email";
        const digits = onlyDigits(value);
        if (!digits) return "typing";
        if (digits.length > 14) return "invalid-length";
        if (value.replace(/[.\-/ ]/g, "") !== digits) return "typing";
        if (digits.length === 11) return cpfDigitsValid(digits) ? "cpf" : "invalid-cpf";
        if (digits.length === 14) return cnpjDigitsValid(digits) ? "cnpj" : "invalid-cnpj";
        if (digits.length >= 12) return "incomplete-cnpj";
        return "incomplete-cpf";
    };
    const updateIdentifierGuide = () => {
        if (!identifierField || !identifierGuide) return;
        const t = identifierGuide.dataset;
        if (identifierField.value.trim() && onlyDigits(identifierField.value).length >= 2
            && /^[0-9.\-/ ]+$/.test(identifierField.value.trim())
            && onlyDigits(identifierField.value).length <= 14) {
            const digits = onlyDigits(identifierField.value);
            const masked = formatDigits(digits.length >= 12 ? "cnpj" : "cpf", digits);
            if (masked !== identifierField.value) identifierField.value = masked;
        }
        const show = text => { identifierGuide.textContent = text; identifierGuide.hidden = false; };
        const setBlocked = message => {
            identifierField.setCustomValidity(message);
            identifierField.setAttribute("aria-invalid", message ? "true" : "false");
        };
        switch (identifierState(identifierField.value)) {
            case "empty": identifierGuide.hidden = true; setBlocked(""); break;
            case "email": show(t.textEmail); setBlocked(""); break;
            case "typing": identifierGuide.hidden = true; setBlocked(""); break;
            case "cpf": show(t.textCpf); setBlocked(""); break;
            case "cnpj": show(t.textCnpj); setBlocked(""); break;
            case "incomplete-cpf": show(t.textIncompleteCpf); setBlocked(""); break;
            case "incomplete-cnpj": show(t.textIncompleteCnpj); setBlocked(""); break;
            case "invalid-cpf": show(t.textInvalidCpf); setBlocked(t.textInvalidCpf); break;
            case "invalid-cnpj": show(t.textInvalidCnpj); setBlocked(t.textInvalidCnpj); break;
            case "invalid-length": show(t.textInvalidLength); setBlocked(t.textInvalidLength); break;
        }
    };
    identifierField?.addEventListener("input", updateIdentifierGuide);
    updateIdentifierGuide();

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

    const contextMessage = document.querySelector(".pp-auth-context-message");
    if (errorSummary?.textContent?.trim()) {
        resetSubmission();
        window.queueMicrotask(() => errorSummary.focus());
    } else if (contextMessage) {
        // Chegada via ?reason=... (sessão expirada, logout etc.): leva o foco
        // até a mensagem de contexto para teclado e leitores de tela.
        window.queueMicrotask(() => contextMessage.focus());
    }
    updateConnectionStatus();
})();
