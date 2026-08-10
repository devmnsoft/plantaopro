(() => {
    "use strict";
    const humanizeForms = () => document.querySelectorAll("form:not([data-pp-form-ready])").forEach(form => {
        form.dataset.ppFormReady = "true";
        form.classList.add("pp-form");
        form.querySelectorAll(".mb-3, .form-group").forEach(field => field.classList.add("pp-form-field"));
        form.querySelectorAll(".form-label").forEach(label => label.classList.add("pp-form-label"));
        form.querySelectorAll(".form-help, .form-text").forEach(help => help.classList.add("pp-field-help"));
        form.querySelectorAll("[asp-validation-summary], .validation-summary-errors, [data-valmsg-summary]").forEach(summary => summary.classList.add("pp-validation-summary"));
        form.querySelectorAll(".alert-danger[data-valmsg-summary], .alert-danger.validation-summary-errors").forEach(summary => summary.classList.remove("alert", "alert-danger"));
        form.querySelectorAll(".d-flex.gap-2").forEach(actions => { if (actions.querySelector('[type="submit"]')) actions.classList.add("pp-form-actions"); });
    });
    humanizeForms();
    document.querySelectorAll("[data-character-counter]").forEach(counter => {
        const field = document.getElementById(counter.dataset.for);
        if (!field) return;
        const update = () => { counter.textContent = `${field.value.length} de ${field.maxLength.toLocaleString("pt-BR")} caracteres`; };
        field.addEventListener("input", update);
        update();
    });

    document.querySelectorAll("form").forEach(form => {
        form.querySelectorAll(".field-validation-error:not(:empty)").forEach(error => {
            const fieldName = error.getAttribute("data-valmsg-for");
            const field = fieldName ? form.elements.namedItem(fieldName) : null;
            if (!(field instanceof HTMLElement)) return;
            if (!error.id) error.id = `${field.id || fieldName}-error`;
            field.setAttribute("aria-invalid", "true");
            const describedBy = new Set((field.getAttribute("aria-describedby") || "").split(/\s+/).filter(Boolean));
            describedBy.add(error.id);
            field.setAttribute("aria-describedby", Array.from(describedBy).join(" "));
        });
        form.addEventListener("invalid", event => {
            const field = event.target;
            if (!(field instanceof HTMLElement)) return;
            field.setAttribute("aria-invalid", "true");
            const error = form.querySelector(`[data-valmsg-for="${CSS.escape(field.getAttribute("name") || "")}"]`);
            if (error && !error.textContent.trim()) error.textContent = field.validity.typeMismatch ? "Confira o formato informado para continuar." : "Preencha este campo para continuar.";
        }, true);
        form.addEventListener("input", event => {
            const field = event.target;
            if (field instanceof HTMLInputElement || field instanceof HTMLSelectElement || field instanceof HTMLTextAreaElement) {
                if (field.validity.valid) field.removeAttribute("aria-invalid");
            }
        });
        form.addEventListener("submit", event => {
            if (form.checkValidity()) return;
            event.preventDefault();
            const summary = form.querySelector(".pp-validation-summary, [data-valmsg-summary]");
            if (summary) {
                summary.classList.remove("validation-summary-valid");
                if (!summary.textContent.trim()) summary.innerHTML = '<strong class="pp-validation-summary__title">Revise os campos destacados</strong><span>Há informações obrigatórias ou em formato inválido antes de continuar.</span>';
                summary.setAttribute("tabindex", "-1"); summary.focus();
            }
            (form.querySelector(":invalid"))?.focus();
        });
    });

    document.querySelectorAll("[data-focus-invalid]").forEach(form => {
        const invalid = form.querySelector(".input-validation-error, [aria-invalid='true']");
        if (invalid instanceof HTMLElement) invalid.focus();
    });

    document.querySelectorAll("[data-unsaved-form], form.pp-form").forEach(form => {
        let dirty = false;
        const banner = document.createElement("div");
        banner.className = "pp-unsaved-changes-banner pp-unsaved-indicator";
        banner.setAttribute("role", "status");
        banner.innerHTML = '<span class="pp-alert__icon" aria-hidden="true">!</span><div><strong class="pp-alert__title">Alterações não salvas</strong><p class="pp-alert__description">Salve o formulário antes de sair para não perder as informações.</p></div>';
        const actions = form.querySelector(".pp-form-actions, .pp-form-footer");
        if (actions) actions.before(banner);
        form.addEventListener("input", () => { dirty = true; form.classList.add("is-dirty"); });
        form.addEventListener("submit", () => { dirty = false; form.classList.remove("is-dirty"); });
        window.addEventListener("beforeunload", event => {
            if (!dirty) return;
            event.preventDefault();
            event.returnValue = "";
        });
    });
})();
