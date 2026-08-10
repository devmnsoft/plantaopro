(() => {
    "use strict";
    const fieldsSelector = "input:not([type=hidden]):not([type=submit]), select, textarea";

    function connectValidation(form) {
        form.querySelectorAll("[data-valmsg-for]").forEach(error => {
            const name = error.getAttribute("data-valmsg-for");
            const field = name ? form.elements.namedItem(name) : null;
            if (!(field instanceof HTMLElement)) return;
            if (!error.id) error.id = `${field.id || name}-error`;
            const describedBy = new Set((field.getAttribute("aria-describedby") || "").split(/\s+/).filter(Boolean));
            describedBy.add(error.id);
            field.setAttribute("aria-describedby", Array.from(describedBy).join(" "));
            if (error.textContent.trim()) field.setAttribute("aria-invalid", "true");
            const observer = new MutationObserver(() => {
                field.setAttribute("aria-invalid", String(Boolean(error.textContent.trim())));
            });
            observer.observe(error, { childList: true, subtree: true, characterData: true });
        });
    }

    function validateDates(form) {
        const starts = form.querySelector('[name="DataInicio"]');
        const ends = form.querySelector('[name="DataFim"]');
        if (!(starts instanceof HTMLInputElement) || !(ends instanceof HTMLInputElement)) return true;
        const valid = !starts.value || !ends.value || new Date(ends.value) > new Date(starts.value);
        ends.setCustomValidity(valid ? "" : "O término precisa ser posterior ao início.");
        return valid;
    }

    function focusFirstInvalid(form) {
        const invalid = form.querySelector(".input-validation-error, :invalid, [aria-invalid='true']");
        if (invalid instanceof HTMLElement) {
            invalid.focus({ preventScroll: true });
            invalid.scrollIntoView({ behavior: "smooth", block: "center" });
        }
    }

    function wireDirtyState(form) {
        if (!form.matches("[data-unsaved-form]")) return;
        let dirty = false;
        let banner = form.querySelector("[data-unsaved-banner]");
        if (!banner) {
            banner = document.createElement("span");
            banner.className = "pp-unsaved-changes-banner";
            banner.dataset.unsavedBanner = "true";
            banner.setAttribute("role", "status");
            banner.textContent = "● Existem alterações não salvas";
            form.querySelector(".pp-form-status, .pp-form-footer, .pp-form-sticky-footer")?.prepend(banner);
        }
        const setDirty = value => { dirty = value; banner?.classList.toggle("is-visible", value); };
        form.addEventListener("input", () => setDirty(true));
        form.addEventListener("change", () => setDirty(true));
        form.addEventListener("submit", () => setDirty(false));
        window.addEventListener("beforeunload", event => { if (dirty) event.preventDefault(); });
    }

    document.querySelectorAll("[data-character-counter]").forEach(counter => {
        const field = document.getElementById(counter.dataset.for);
        if (!field) return;
        const update = () => { counter.textContent = `${field.value.length.toLocaleString("pt-BR")} de ${field.maxLength.toLocaleString("pt-BR")} caracteres`; };
        field.addEventListener("input", update); update();
    });

    document.querySelectorAll("form").forEach(form => {
        connectValidation(form); wireDirtyState(form);
        form.querySelectorAll(fieldsSelector).forEach(field => field.addEventListener("change", () => validateDates(form)));
        form.addEventListener("submit", event => {
            if (!validateDates(form) || !form.checkValidity()) {
                event.preventDefault();
                form.classList.add("was-validated");
                focusFirstInvalid(form);
                window.PlantaoProToast?.error("Revise os campos destacados antes de continuar.", { title: "Há informações pendentes" });
            }
        }, true);
        if (form.matches("[data-focus-invalid]")) focusFirstInvalid(form);
    });
})();
