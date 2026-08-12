(() => {
    const dialog = document.querySelector('#commandPalette');
    const input = document.querySelector('#commandSearchInput');
    const results = document.querySelector('#commandSearchResults');
    const trigger = document.querySelector('[data-command-open]');
    if (!dialog || !input || !results || !trigger) return;

    let timer;
    let controller;

    const open = () => {
        dialog.showModal();
        window.setTimeout(() => input.focus(), 0);
    };
    const close = () => {
        if (dialog.open) dialog.close();
        trigger.focus();
    };
    const escapeHtml = (value) => {
        const node = document.createElement('span');
        node.textContent = value || '';
        return node.innerHTML;
    };

    const render = (items) => {
        if (!items.length) {
            results.innerHTML = '<p class="text-secondary p-3 mb-0">Nenhum resultado permitido para este termo.</p>';
            return;
        }
        results.innerHTML = items.map((item) => `
            <a class="pp-command-result" href="${encodeURI(item.route)}" role="option">
                <i class="bi ${escapeHtml(item.icon)}" aria-hidden="true"></i>
                <span><strong class="d-block">${escapeHtml(item.title)}</strong>
                ${item.subtitle ? `<small class="text-secondary">${escapeHtml(item.subtitle)}</small>` : ''}</span>
                <span class="pp-badge ms-auto">${escapeHtml(item.type)}</span>
            </a>`).join('');
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

    trigger.addEventListener('click', open);
    dialog.querySelector('[data-command-close]')?.addEventListener('click', close);
    input.addEventListener('input', () => {
        window.clearTimeout(timer);
        timer = window.setTimeout(search, 250);
    });
    document.addEventListener('keydown', (event) => {
        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
            event.preventDefault();
            dialog.open ? close() : open();
        }
        if (event.key === 'Escape' && dialog.open) {
            event.preventDefault();
            close();
        }
    });
    dialog.addEventListener('cancel', (event) => { event.preventDefault(); close(); });
})();
