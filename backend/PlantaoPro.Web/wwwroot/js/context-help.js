(() => {
    const drawer = document.querySelector('[data-context-help-open]');
    const panel = document.getElementById('contextHelpDrawer');
    if (!drawer || !panel) return;

    const backdrop = document.querySelector('.pp-help-backdrop');
    const closeButtons = document.querySelectorAll('[data-context-help-close]');
    let previousFocus;

    const close = () => {
        panel.classList.remove('is-open');
        panel.setAttribute('aria-hidden', 'true');
        drawer.setAttribute('aria-expanded', 'false');
        if (backdrop) backdrop.hidden = true;
        document.body.classList.remove('pp-help-visible');
        previousFocus?.focus();
    };
    const open = () => {
        previousFocus = document.activeElement;
        panel.classList.add('is-open');
        panel.setAttribute('aria-hidden', 'false');
        drawer.setAttribute('aria-expanded', 'true');
        if (backdrop) backdrop.hidden = false;
        document.body.classList.add('pp-help-visible');
        panel.focus();
    };

    drawer.addEventListener('click', open);
    closeButtons.forEach(button => button.addEventListener('click', close));
    document.addEventListener('keydown', event => { if (event.key === 'Escape' && panel.classList.contains('is-open')) close(); });
})();
