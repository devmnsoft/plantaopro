(() => {
  const root = document.querySelector('[data-clinical-workspace]');
  if (!root) return;
  const id = root.dataset.consultaId;
  const base = root.dataset.apiBase;
  const form = root.querySelector('#clinical-form');
  const status = root.querySelector('[data-save-status]');
  let saving = false;
  let dirty = false;
  let timer;
  const field = name => form.elements.namedItem(name);
  const message = text => { status.textContent = text; };

  async function api(path, options = {}) {
    const response = await fetch(`${base}/${id}${path}`, {
      credentials: 'same-origin',
      headers: {
        'Content-Type': 'application/json',
        RequestVerificationToken: document.querySelector('input[name="__RequestVerificationToken"]')?.value || '',
        ...(options.headers || {})
      },
      ...options
    });
    const payload = await response.json().catch(() => ({ message: 'Não foi possível processar a resposta.' }));
    if (!response.ok) {
      const error = new Error(payload.message || 'Operação não concluída.');
      error.status = response.status;
      error.payload = payload;
      throw error;
    }
    return payload.data;
  }

  function replaceChildren(container, values, emptyText) {
    const fragment = document.createDocumentFragment();
    if (values.length === 0) {
      const empty = document.createElement('p');
      empty.textContent = emptyText;
      fragment.append(empty);
    } else {
      values.forEach(value => {
        const item = document.createElement('p');
        item.textContent = value;
        fragment.append(item);
      });
    }
    container.replaceChildren(fragment);
  }

  function renderCids(cids) {
    const container = root.querySelector('[data-cids]');
    if (cids.length === 0) {
      replaceChildren(container, [], 'Nenhum CID vinculado.');
      return;
    }
    const fragment = document.createDocumentFragment();
    cids.forEach(cid => {
      const badge = document.createElement('span');
      badge.className = 'badge text-bg-light me-2';
      badge.textContent = `${cid.codigo} — ${cid.descricao}${cid.principal ? ' (principal)' : ''}`;
      fragment.append(badge);
    });
    container.replaceChildren(fragment);
  }

  async function load() {
    message('Carregando…');
    try {
      const workspace = await api('/workspace');
      const consulta = workspace.consulta;
      root.querySelector('[data-patient-name]').textContent = workspace.nomeSocial || workspace.pacienteNome;
      root.querySelector('[data-patient-gender]').textContent = workspace.sexoGenero || '';
      root.querySelector('[data-risk]').textContent = workspace.classificacaoRisco || 'Sem classificação';
      root.querySelector('[data-status]').textContent = consulta.status;
      root.querySelector('[data-allergies]').textContent = workspace.alergias || 'Nenhuma alergia informada';
      root.querySelector('[data-plan]').textContent = workspace.plano || 'Não informado';
      root.querySelector('[data-unit]').textContent = workspace.unidade || '—';
      ['anamnese', 'exameFisico', 'hipoteseDiagnostica', 'diagnostico', 'conduta', 'orientacoes'].forEach(name => { field(name).value = consulta[name] || ''; });
      field('versao').value = consulta.versao;
      renderCids(workspace.cids || []);
      dirty = false;
      message('Dados atualizados');
    } catch (error) { message(error.message); }
  }

  async function save() {
    if (saving || !dirty) return;
    saving = true;
    root.querySelector('[data-save]').disabled = true;
    message('Salvando…');
    const body = { versao: Number(field('versao').value), anamnese: field('anamnese').value, exameFisico: field('exameFisico').value, hipoteseDiagnostica: field('hipoteseDiagnostica').value, diagnostico: field('diagnostico').value, conduta: field('conduta').value, orientacoes: field('orientacoes').value, observacoes: null };
    try {
      const consulta = await api('/rascunho', { method: 'PUT', body: JSON.stringify(body) });
      field('versao').value = consulta.versao;
      dirty = false;
      message(`Salvo às ${new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`);
    } catch (error) {
      message(error.message);
      if (error.status === 409) root.querySelector('[data-conflict-modal]').showModal();
    } finally {
      saving = false;
      root.querySelector('[data-save]').disabled = false;
    }
  }

  form.addEventListener('input', () => { dirty = true; message('Alterações não salvas'); clearTimeout(timer); timer = setTimeout(save, 1800); });
  root.querySelector('[data-save]').addEventListener('click', save);
  root.querySelector('[data-reload]').addEventListener('click', () => location.reload());
  root.querySelector('[data-finalize]').addEventListener('click', async () => {
    const modal = root.querySelector('[data-finalize-modal]');
    try {
      const pending = await api('/pendencias-finalizacao');
      replaceChildren(root.querySelector('[data-pendencies]'), [...pending.impeditivas, ...pending.alertas], 'Prontuário pronto para finalização.');
      root.querySelector('[data-confirm-finalization]').disabled = !pending.podeFinalizar;
      modal.showModal();
    } catch (error) { message(error.message); }
  });
  root.querySelector('[data-close-finalization]').addEventListener('click', () => root.querySelector('[data-finalize-modal]').close());
  root.querySelector('[data-confirm-finalization]').addEventListener('click', async event => {
    const button = event.currentTarget;
    const errorBox = root.querySelector('[data-finalize-error]');
    button.disabled = true;
    button.setAttribute('aria-busy', 'true');
    errorBox.hidden = true;
    try {
      const result = await api('/finalizar', { method: 'POST', body: JSON.stringify({ versao: Number(field('versao').value), tipoFaturamento: Number(root.querySelector('[data-billing-type]').value), valorBruto: Number(root.querySelector('[data-billing-value]').value), desconto: 0, coparticipacao: 0, justificativa: root.querySelector('[data-billing-reason]').value }) });
      if (result.podeAbrirFaturamento && result.financeiroId) {
        const billing = root.querySelector('[data-open-billing]');
        billing.classList.remove('disabled');
        billing.removeAttribute('aria-disabled');
        billing.removeAttribute('tabindex');
        billing.href = `/FaturamentoClinico?atendimentoId=${encodeURIComponent(result.consulta.atendimentoId)}`;
      }
      message(result.podeAbrirFaturamento ? 'Consulta finalizada com financeiro real.' : 'Consulta finalizada sem gerar valor financeiro.');
      root.querySelector('[data-finalize-modal]').close();
      button.disabled = true;
      root.querySelector('[data-finalize]').disabled = true;
    } catch (error) {
      errorBox.textContent = error.message;
      errorBox.hidden = false;
      button.disabled = false;
    } finally { button.removeAttribute('aria-busy'); }
  });
  load();
})();
