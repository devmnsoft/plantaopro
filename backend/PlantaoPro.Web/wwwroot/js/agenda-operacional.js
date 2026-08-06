(() => {
    'use strict';

    const printButton = document.querySelector('[data-agenda-print]');
    if (!printButton) return;

    printButton.addEventListener('click', () => {
        printButton.setAttribute('aria-busy', 'true');
        window.requestAnimationFrame(() => {
            window.print();
            printButton.removeAttribute('aria-busy');
        });
    });
})();
