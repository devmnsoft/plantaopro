(() => {
    "use strict";

    document.querySelectorAll("[data-user-menu]").forEach(menu => {
        const trigger = menu.querySelector("[data-user-menu-trigger]");
        const dropdown = menu.querySelector('[role="menu"]');
        if (!(trigger instanceof HTMLButtonElement) || !(dropdown instanceof HTMLElement)) return;

        const items = () => [...dropdown.querySelectorAll('[role="menuitem"]')];
        const close = (restoreFocus = false) => {
            dropdown.hidden = true;
            trigger.setAttribute("aria-expanded", "false");
            if (restoreFocus) trigger.focus();
        };
        const open = () => {
            dropdown.hidden = false;
            trigger.setAttribute("aria-expanded", "true");
            items()[0]?.focus();
        };

        trigger.addEventListener("click", event => {
            event.stopPropagation();
            dropdown.hidden ? open() : close(true);
        });
        menu.addEventListener("keydown", event => {
            if (event.key === "Escape") {
                event.preventDefault();
                close(true);
            }
            if (["ArrowDown", "ArrowUp"].includes(event.key) && !dropdown.hidden) {
                event.preventDefault();
                const menuItems = items();
                const current = menuItems.indexOf(document.activeElement);
                const direction = event.key === "ArrowDown" ? 1 : -1;
                menuItems[(current + direction + menuItems.length) % menuItems.length]?.focus();
            }
        });
        document.addEventListener("click", event => {
            if (!menu.contains(event.target)) close();
        });
    });
})();
