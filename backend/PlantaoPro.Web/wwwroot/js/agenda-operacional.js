(() => {
    'use strict';

    const printButton = document.querySelector('[data-agenda-print]');
    printButton?.addEventListener('click', () => {
        printButton.setAttribute('aria-busy', 'true');
        window.requestAnimationFrame(() => {
            window.print();
            printButton.removeAttribute('aria-busy');
        });
    });

    const drawer = document.querySelector('[data-agenda-drawer]');
    const backdrop = document.querySelector('[data-overlay-backdrop]');
    let trigger;
    const close = () => {
        if (!drawer) return;
        drawer.hidden = true;
        if (backdrop) backdrop.hidden = true;
        trigger?.focus();
    };
    document.querySelectorAll('[data-agenda-detail]').forEach(button => button.addEventListener('click', () => {
        const event = button.closest('[data-agenda-event]');
        if (!drawer || !event) return;
        trigger = button;
        drawer.querySelector('[data-agenda-title]').textContent = event.dataset.title;
        drawer.querySelector('[data-agenda-hospital]').textContent = event.dataset.hospital;
        drawer.querySelector('[data-agenda-specialty]').textContent = event.dataset.specialty;
        drawer.querySelector('[data-agenda-period]').textContent = event.dataset.period;
        drawer.querySelector('[data-agenda-value]').textContent = event.dataset.value;
        drawer.querySelector('[data-agenda-coverage]').textContent = event.dataset.coverage;
        const badge = drawer.querySelector('.status-badge');
        badge.textContent = event.dataset.status;
        badge.setAttribute('aria-label', `Status: ${event.dataset.status}`);
        drawer.querySelector('[data-agenda-full]').href = `/Plantoes/Details/${event.dataset.eventId}`;
        drawer.hidden = false;
        if (backdrop) backdrop.hidden = false;
        drawer.querySelector('[data-agenda-close]').focus();
    }));
    drawer?.querySelectorAll('[data-agenda-close]').forEach(button => button.addEventListener('click', close));
    document.addEventListener('keydown', event => { if (event.key === 'Escape' && drawer && !drawer.hidden) close(); });
})();
