(() => {
    "use strict";
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
    });

    document.querySelectorAll("[data-focus-invalid]").forEach(form => {
        const invalid = form.querySelector(".input-validation-error, [aria-invalid='true']");
        if (invalid instanceof HTMLElement) invalid.focus();
    });

    document.querySelectorAll("[data-unsaved-form]").forEach(form => {
        let dirty = false;
        form.addEventListener("input", () => { dirty = true; });
        form.addEventListener("submit", () => { dirty = false; });
        window.addEventListener("beforeunload", event => {
            if (!dirty) return;
            event.preventDefault();
            event.returnValue = "";
        });
    });
})();
