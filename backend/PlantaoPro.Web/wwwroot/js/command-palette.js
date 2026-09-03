(() => {
    const dialog = document.querySelector('#commandPalette');
    const input = document.querySelector('#commandSearchInput');
    const results = document.querySelector('#commandSearchResults');
    const triggers = [...document.querySelectorAll('[data-command-open]')];
    if (!dialog || !input || !results || !triggers.length) return;

    let timer;
    let controller;
    let activeTrigger = triggers[0];
    let activeIndex = -1;

    const open = (trigger = activeTrigger) => {
        activeTrigger = trigger;
        dialog.showModal();
        triggers.forEach(item => item.setAttribute('aria-expanded', 'true'));
        window.setTimeout(() => input.focus(), 0);
    };
    const close = () => {
        if (dialog.open) dialog.close();
        triggers.forEach(item => item.setAttribute('aria-expanded', 'false'));
        input.removeAttribute('aria-activedescendant');
        activeIndex = -1;
        activeTrigger.focus();
    };
    const showMessage = (message, className = 'text-secondary p-3 mb-0', role = '') => {
        const copy = document.createElement('p');
        copy.className = className;
        copy.textContent = message;
        if (role) copy.setAttribute('role', role);
        results.replaceChildren(copy);
    };

    const safeRoute = (route) => {
        try {
            const url = new URL(route, window.location.origin);
            return url.origin === window.location.origin ? `${url.pathname}${url.search}${url.hash}` : null;
        } catch {
            return null;
        }
    };

    const render = (items) => {
        activeIndex = -1;
        input.removeAttribute('aria-activedescendant');
        const allowedItems = items.map(item => ({ ...item, route: safeRoute(item.route) })).filter(item => item.route);
        if (!allowedItems.length) {
            showMessage('Nenhum resultado permitido para este termo.');
            return;
        }
        results.replaceChildren(...allowedItems.map((item, index) => {
            const link = document.createElement('a');
            link.className = 'pp-command-result'; link.id = `command-result-${index}`; link.href = item.route;
            link.setAttribute('role', 'option'); link.setAttribute('aria-selected', 'false'); link.tabIndex = -1;
            const icon = document.createElement('i'); icon.className = `bi ${item.icon || ''}`; icon.setAttribute('aria-hidden', 'true');
            const copy = document.createElement('span'); const title = document.createElement('strong'); title.className = 'd-block'; title.textContent = item.title || ''; copy.append(title);
            if (item.subtitle) { const subtitle = document.createElement('small'); subtitle.className = 'text-secondary'; subtitle.textContent = item.subtitle; copy.append(subtitle); }
            const type = document.createElement('span'); type.className = 'pp-badge ms-auto'; type.textContent = item.type || '';
            link.append(icon, copy, type); return link;
        }));
    };

    const moveActive = (step) => {
        const options = [...results.querySelectorAll('[role="option"]')];
        if (!options.length) return;
        activeIndex = (activeIndex + step + options.length) % options.length;
        options.forEach((option, index) => option.setAttribute('aria-selected', String(index === activeIndex)));
        input.setAttribute('aria-activedescendant', options[activeIndex].id);
        options[activeIndex].scrollIntoView({ block: 'nearest' });
    };

    const search = async () => {
        const query = input.value.trim();
        if (query.length < 2) {
            showMessage('Digite ao menos dois caracteres.');
            return;
        }
        controller?.abort();
        controller = new AbortController();
        showMessage('Pesquisando…', 'text-secondary p-3 mb-0', 'status');
        try {
            const response = await fetch(`/GlobalSearch?q=${encodeURIComponent(query)}&limite=20`, {
                signal: controller.signal,
                headers: { Accept: 'application/json' }
            });
            if (!response.ok) throw new Error('search-failed');
            const payload = await response.json();
            render(payload.data?.items || []);
        } catch (error) {
            if (error.name !== 'AbortError')
                showMessage('Não foi possível pesquisar agora. Tente novamente.', 'text-danger p-3 mb-0', 'alert');
        }
    };

    triggers.forEach(trigger => {
        trigger.setAttribute('aria-expanded', 'false');
        trigger.addEventListener('click', () => open(trigger));
    });
    dialog.querySelector('[data-command-close]')?.addEventListener('click', close);
    input.addEventListener('input', () => {
        window.clearTimeout(timer);
        timer = window.setTimeout(search, 250);
    });
    input.addEventListener('keydown', (event) => {
        if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
            event.preventDefault();
            moveActive(event.key === 'ArrowDown' ? 1 : -1);
        }
        if (event.key === 'Enter' && activeIndex >= 0) {
            event.preventDefault();
            results.querySelectorAll('[role="option"]')[activeIndex]?.click();
        }
    });
    document.addEventListener('keydown', (event) => {
        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
            event.preventDefault();
            dialog.open ? close() : open(triggers[0]);
        }
        if (event.key === 'Escape' && dialog.open) {
            event.preventDefault();
            close();
        }
    });
    dialog.addEventListener('cancel', (event) => { event.preventDefault(); close(); });
})();
