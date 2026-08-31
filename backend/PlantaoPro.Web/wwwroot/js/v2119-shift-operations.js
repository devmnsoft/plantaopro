(() => {
    const viewbar = document.querySelector('[data-shift-viewbar]');
    if (!viewbar) return;

    const buttons = [...viewbar.querySelectorAll('[data-shift-view]')];
    const panels = [...document.querySelectorAll('[data-shift-panel]')];
    const announce = document.getElementById('appLiveRegion');

    const activate = (view) => {
        buttons.forEach((button) => {
            const active = button.dataset.shiftView === view;
            button.classList.toggle('is-active', active);
            button.setAttribute('aria-pressed', active ? 'true' : 'false');
        });
        panels.forEach((panel) => { panel.hidden = panel.dataset.shiftPanel !== view; });
        if (announce) announce.textContent = view === 'kanban' ? 'Visão kanban ativada.' : 'Visão em lista ativada.';
    };

    buttons.forEach((button) => button.addEventListener('click', () => activate(button.dataset.shiftView)));
})();
