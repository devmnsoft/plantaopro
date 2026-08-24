(function () {
    'use strict';
    const app = document.getElementById('implantacao-app');
    if (!app) return;
    let etapas = Array.of();
    const byId = id => document.getElementById(id);
    const text = (id, value) => { byId(id).textContent = value; };

    function renderizarEtapas() {
        const filtro = byId('filtro-status').value;
        const alvo = byId('lista-etapas');
        const timeline = byId('timeline');
        alvo.replaceChildren();
        timeline.replaceChildren();
        etapas.forEach(etapa => {
            const item = document.createElement('li');
            item.textContent = `${etapa.ordem}. ${etapa.nome} — ${etapa.status}`;
            timeline.appendChild(item);
        });
        const filtradas = etapas.filter(etapa => !filtro || etapa.status === filtro);
        byId('empty-state').classList.toggle('d-none', filtradas.length > 0);
        filtradas.forEach(etapa => {
            const col = document.createElement('div');
            col.className = 'col-md-6 col-xl-4';
            const card = document.createElement('article');
            card.className = 'card h-100';
            const body = document.createElement('div');
            body.className = 'card-body';
            const title = document.createElement('h3');
            title.className = 'h6';
            title.textContent = `${etapa.ordem}. ${etapa.nome}`;
            const details = document.createElement('p');
            details.textContent = `Responsável: ${etapa.responsavel} · Prazo: ${new Date(etapa.prazo).toLocaleDateString('pt-BR')}`;
            const button = document.createElement('button');
            button.type = 'button'; button.className = 'btn btn-sm btn-outline-primary'; button.textContent = 'Validar etapa';
            button.addEventListener('click', () => validarEtapa(etapa.codigo));
            body.append(title, details, button); card.appendChild(body); col.appendChild(card); alvo.appendChild(col);
        });
    }

    async function carregarImplantacao() {
        byId('implantacao-loading').classList.remove('d-none');
        byId('implantacao-error').classList.add('d-none');
        try {
            const responses = await Promise.all([fetch(app.dataset.prontidaoUrl), fetch(app.dataset.etapasUrl), fetch(app.dataset.relatorioUrl)]);
            if (responses.some(response => !response.ok)) throw new Error('Não foi possível carregar a implantação. Tente novamente.');
            const [prontidao, etapasResponse, relatorio] = await Promise.all(responses.map(response => response.json()));
            etapas = etapasResponse.data || [];
            const percentual = prontidao.data && prontidao.data.prontidaoPercentual || 0;
            text('kpi-prontidao', `${percentual}%`); text('kpi-etapas', etapas.length);
            text('kpi-pendencias', etapas.reduce((total, etapa) => total + ((etapa.pendencias || []).length), 0));
            text('kpi-alertas', etapas.filter(etapa => etapa.status !== 'OK').length);
            byId('barra-prontidao').style.width = `${percentual}%`; text('barra-prontidao', `${percentual}%`);
            text('relatorio', JSON.stringify(relatorio.data, null, 2)); renderizarEtapas();
        } catch (error) { text('implantacao-error', error.message); byId('implantacao-error').classList.remove('d-none'); }
        finally { byId('implantacao-loading').classList.add('d-none'); }
    }
    async function post(url) { const response = await fetch(url, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' } }); if (!response.ok) throw new Error('A validação não foi concluída.'); await carregarImplantacao(); }
    function validarEtapa(codigo) { return post(`${app.dataset.etapasUrl}/${encodeURIComponent(codigo)}/validar`); }
    byId('filtro-status').addEventListener('change', renderizarEtapas);
    byId('atualizar-implantacao').addEventListener('click', carregarImplantacao);
    byId('validar-tudo').addEventListener('click', () => post(app.dataset.validarTudoUrl));
    carregarImplantacao();
}());
