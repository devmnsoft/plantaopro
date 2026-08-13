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
    const escapeHtml = (value) => {
        const node = document.createElement('span');
        node.textContent = value || '';
        return node.innerHTML;
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
            results.innerHTML = '<p class="text-secondary p-3 mb-0">Nenhum resultado permitido para este termo.</p>';
            return;
        }
        results.innerHTML = allowedItems.map((item, index) => `
            <a class="pp-command-result" id="command-result-${index}" href="${encodeURI(item.route)}" role="option" aria-selected="false" tabindex="-1">
                <i class="bi ${escapeHtml(item.icon)}" aria-hidden="true"></i>
                <span><strong class="d-block">${escapeHtml(item.title)}</strong>
                ${item.subtitle ? `<small class="text-secondary">${escapeHtml(item.subtitle)}</small>` : ''}</span>
                <span class="pp-badge ms-auto">${escapeHtml(item.type)}</span>
            </a>`).join('');
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
            results.innerHTML = '<p class="text-secondary p-3 mb-0">Digite ao menos dois caracteres.</p>';
            return;
        }
        controller?.abort();
        controller = new AbortController();
        results.innerHTML = '<p class="text-secondary p-3 mb-0" role="status">Pesquisando…</p>';
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
                results.innerHTML = '<p class="text-danger p-3 mb-0" role="alert">Não foi possível pesquisar agora. Tente novamente.</p>';
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
