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
        const bruto = String(event.dataset.status || '');
        const rotulo = bruto.replaceAll('_', ' ').replaceAll('-', ' ');
        const alvo = badge.querySelector('[data-status-label]');
        if (alvo) alvo.textContent = rotulo; else badge.textContent = rotulo;
        badge.setAttribute('aria-label', `Status: ${rotulo}`);
        // R71: sincroniza tom e icone pelo mapa canonico de estados (subconjunto de
        // backend/PlantaoPro.Web/Localization/EstadoVisual.cs; contrato travado em
        // R71SemanticaDeCoresContratoTests). Nunca so cor: o badge sempre tem icone+texto.
        const MAPA_R71 = { confirmado:['success','bi-check-circle-fill'], realizado:['success','bi-check-circle-fill'], realizada:['success','bi-check-circle-fill'], concluido:['success','bi-check-circle-fill'], pago:['success','bi-check-circle-fill'], ativo:['success','bi-check-circle-fill'], aberto:['warning','bi-clock-fill'], pendente:['warning','bi-clock-fill'], aguardando:['warning','bi-clock-fill'], rascunho:['warning','bi-clock-fill'], solicitado:['warning','bi-clock-fill'], solicitada:['warning','bi-clock-fill'], suspenso:['warning','bi-clock-fill'], atrasado:['warning','bi-exclamation-triangle-fill'], vencido:['warning','bi-exclamation-triangle-fill'], contestado:['warning','bi-exclamation-triangle-fill'], divergente:['warning','bi-exclamation-triangle-fill'], 'com-divergencia':['warning','bi-exclamation-triangle-fill'], conflito:['danger','bi-exclamation-octagon-fill'], cancelado:['danger','bi-x-circle-fill'], rejeitado:['danger','bi-x-circle-fill'], expirado:['danger','bi-x-circle-fill'], bloqueado:['danger','bi-exclamation-octagon-fill'], falha:['danger','bi-exclamation-octagon-fill'], inativo:['secondary','bi-circle-slash'], encerrado:['secondary','bi-circle-slash'], arquivada:['secondary','bi-archive-fill'] };
        const slug = bruto.trim().toLowerCase().replace(/[\s_]+/g, '-').normalize('NFD').replace(/[\u0300-\u036f]/g, '');
        const par = MAPA_R71[slug] || ['secondary', 'bi-question-circle'];
        ['success', 'warning', 'info', 'danger', 'secondary'].forEach(t => badge.classList.remove('pp-status--' + t));
        badge.classList.add('pp-status--' + par[0]);
        const icone = badge.querySelector('.bi');
        if (icone) icone.className = 'bi ' + par[1];
        drawer.querySelector('[data-agenda-full]').href = `/Plantoes/Details/${event.dataset.eventId}`;
        drawer.hidden = false;
        if (backdrop) backdrop.hidden = false;
        drawer.querySelector('[data-agenda-close]').focus();
    }));
    drawer?.querySelectorAll('[data-agenda-close]').forEach(button => button.addEventListener('click', close));
    document.addEventListener('keydown', event => { if (event.key === 'Escape' && drawer && !drawer.hidden) close(); });
})();
