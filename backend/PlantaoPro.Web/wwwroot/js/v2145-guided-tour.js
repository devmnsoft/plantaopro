(() => {
    "use strict";
    const storagePrefix = "plantaopro-tour-dismissed:";
    let activeElement = null;
    let current = 0;
    let steps = [];

    const close = (dismiss = false) => {
        if (dismiss && steps.length) localStorage.setItem(storagePrefix + document.body.dataset.activeTour, "true");
        activeElement?.classList.remove("pp-tour-focus");
        document.querySelector(".pp-tour-backdrop")?.remove();
        document.querySelector(".pp-tour-card")?.remove();
        document.body.removeAttribute("data-active-tour");
        activeElement = null;
    };

    const render = () => {
        activeElement?.classList.remove("pp-tour-focus");
        const item = steps[current];
        activeElement = item.element;
        activeElement.classList.add("pp-tour-focus");
        activeElement.scrollIntoView({ behavior: "smooth", block: "center" });
        const card = document.querySelector(".pp-tour-card");
        card.querySelector("[data-tour-progress]").textContent = `Etapa ${current + 1} de ${steps.length}`;
        card.querySelector("[data-tour-title]").textContent = item.title;
        card.querySelector("[data-tour-description]").textContent = item.description;
        card.querySelector("[data-tour-back]").disabled = current === 0;
        card.querySelector("[data-tour-next]").textContent = current === steps.length - 1 ? "Concluir" : "Próximo";
        card.focus();
    };

    const start = name => {
        if (localStorage.getItem(storagePrefix + name) === "true") return;
        steps = [...document.querySelectorAll("[data-tour-step]")].map(element => {
            const [title, description] = element.dataset.tourStep.split("|");
            return { element, title, description };
        });
        if (!steps.length) return;
        document.body.dataset.activeTour = name;
        document.body.insertAdjacentHTML("beforeend", `<div class="pp-tour-backdrop" aria-hidden="true"></div><section class="pp-tour-card" role="dialog" aria-modal="true" aria-labelledby="ppTourTitle" tabindex="-1"><div class="pp-tour-card__header"><span class="pp-eyebrow">Tour guiado</span><span class="pp-tour-card__progress" data-tour-progress></span></div><h2 id="ppTourTitle" data-tour-title></h2><p data-tour-description></p><div class="pp-tour-card__actions"><label class="pp-tour-card__dismiss"><input type="checkbox" data-tour-dismiss> Não mostrar novamente</label><button type="button" class="btn btn-outline-secondary btn-sm" data-tour-back>Voltar</button><button type="button" class="btn btn-primary btn-sm" data-tour-next>Próximo</button></div></section>`);
        const card = document.querySelector(".pp-tour-card");
        card.querySelector("[data-tour-back]").addEventListener("click", () => { if (current > 0) { current--; render(); } });
        card.querySelector("[data-tour-next]").addEventListener("click", () => { if (current < steps.length - 1) { current++; render(); } else close(card.querySelector("[data-tour-dismiss]").checked); });
        current = 0;
        render();
    };

    document.addEventListener("click", event => {
        const trigger = event.target.closest("[data-tour-start]");
        if (trigger) start(trigger.dataset.tourStart);
    });
    document.addEventListener("keydown", event => { if (event.key === "Escape" && document.querySelector(".pp-tour-card")) close(false); });
})();
