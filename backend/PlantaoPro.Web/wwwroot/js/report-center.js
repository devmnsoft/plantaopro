(() => {
    "use strict";
    const root = document.querySelector("[data-report-center]");
    if (!root) return;

    try {
        const search = root.querySelector("[data-report-search]");
        const cards = [...root.querySelectorAll("[data-report-card]")];
        const categoryButtons = [...root.querySelectorAll("[data-category]")];
        const count = root.querySelector("[data-report-count]");
        const empty = root.querySelector("[data-report-empty]");
        const summary = root.querySelector("[data-filter-summary]");
        const description = root.querySelector("[data-filter-description]");
        let activeCategory = "";

        const normalize = value => (value || "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("pt-BR").trim();
        const apply = () => {
            const term = normalize(search.value);
            let visible = 0;
            cards.forEach(card => {
                const searchable = normalize(`${card.dataset.title} ${card.dataset.description} ${card.dataset.category}`);
                const show = (!term || searchable.includes(term)) && (!activeCategory || card.dataset.category === activeCategory);
                card.hidden = !show;
                if (show) visible += 1;
            });
            count.textContent = `${visible} ${visible === 1 ? "relatório disponível" : "relatórios disponíveis"}`;
            empty.hidden = visible !== 0;
            summary.hidden = !term && !activeCategory;
            description.textContent = [term && `busca “${search.value.trim()}”`, activeCategory && `categoria ${activeCategory}`].filter(Boolean).join(" · ");
        };
        const clear = () => {
            search.value = "";
            activeCategory = "";
            categoryButtons.forEach((button, index) => { button.classList.toggle("is-active", index === 0); button.setAttribute("aria-pressed", index === 0 ? "true" : "false"); });
            apply();
            search.focus();
        };
        search.addEventListener("input", apply);
        categoryButtons.forEach(button => button.addEventListener("click", () => {
            activeCategory = button.dataset.category || "";
            categoryButtons.forEach(item => { const selected = item === button; item.classList.toggle("is-active", selected); item.setAttribute("aria-pressed", selected ? "true" : "false"); });
            apply();
        }));
        root.querySelectorAll("[data-clear-reports]").forEach(button => button.addEventListener("click", clear));
    } catch {
        const error = root.querySelector("[data-report-error]");
        if (error) error.hidden = false;
    }
})();
