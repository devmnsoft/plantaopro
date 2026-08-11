(() => {
    const drawer = document.querySelector('[data-pp-detail-drawer]');
    if (!drawer) return;
    const backdrop = document.querySelector('[data-overlay-backdrop]');
    const loading = drawer.querySelector('[data-detail-loading]');
    const error = drawer.querySelector('[data-detail-error]');
    const content = drawer.querySelector('[data-detail-content]');
    let trigger = null;
    const setText = (selector, value) => { drawer.querySelector(selector).textContent = value || 'Não informado'; };
    const close = () => { drawer.hidden = true; if (backdrop) backdrop.hidden = true; document.body.classList.remove('pp-drawer-open'); trigger?.focus(); };
    const appendMetadata = (label, value) => {
        if (!value) return;
        const group = document.createElement('div'); const term = document.createElement('dt'); const description = document.createElement('dd');
        term.textContent = label; description.textContent = value; group.append(term, description); drawer.querySelector('[data-detail-metadata]').append(group);
    };
    const appendAction = (label, url, primary = false) => {
        if (!url) return;
        const link = document.createElement('a'); link.className = primary ? 'button button-primary' : 'button button-subtle'; link.href = url; link.textContent = label; drawer.querySelector('[data-detail-actions]').append(link);
    };
    const open = (button) => {
        trigger = button; drawer.hidden = false; if (backdrop) backdrop.hidden = false; document.body.classList.add('pp-drawer-open'); loading.hidden = false; error.hidden = true; content.hidden = true;
        drawer.querySelector('[data-detail-body]').setAttribute('aria-busy', 'true'); drawer.querySelector('[data-detail-metadata]').replaceChildren(); drawer.querySelector('[data-detail-actions]').replaceChildren(); drawer.querySelector('[data-detail-timeline]').replaceChildren();
        window.requestAnimationFrame(() => {
            try {
                const data = button.dataset; setText('[data-detail-kind]', data.detailKind); setText('[data-detail-title]', data.detailTitle); setText('[data-detail-status]', data.detailStatus); setText('[data-detail-summary]', data.detailSummary);
                appendMetadata('Unidade / origem', data.detailOrigin); appendMetadata('Período / competência', data.detailPeriod); appendMetadata('Responsável', data.detailOwner); appendMetadata('Composição', data.detailComposition); appendMetadata('Referência', data.detailReference);
                const event = document.createElement('li'); const eventTitle = document.createElement('strong'); const eventDescription = document.createElement('span'); eventTitle.textContent = data.detailStatus || 'Registro disponível'; eventDescription.textContent = data.detailHistory || 'Consulte o detalhe completo para ver a trilha registrada.'; event.append(eventTitle, eventDescription); drawer.querySelector('[data-detail-timeline]').append(event);
                appendAction(data.detailPrimaryLabel || 'Abrir detalhe completo', data.detailPrimaryUrl, true); appendAction(data.detailSecondaryLabel, data.detailSecondaryUrl); loading.hidden = true; content.hidden = false;
            } catch { loading.hidden = true; error.hidden = false; }
            finally { drawer.querySelector('[data-detail-body]').setAttribute('aria-busy', 'false'); drawer.querySelector('[data-detail-close]').focus(); }
        });
    };
    document.addEventListener('click', (event) => { const button = event.target.closest('[data-detail-open]'); if (button) open(button); if (event.target.closest('[data-detail-close]')) close(); if (backdrop && event.target === backdrop && !drawer.hidden) close(); });
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape' && !drawer.hidden) close();
        if (event.key === 'Tab' && !drawer.hidden) { const focusable = [...drawer.querySelectorAll('button:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])')]; if (!focusable.length) return; const first = focusable[0]; const last = focusable[focusable.length - 1]; if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); } else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); } }
    });
})();
