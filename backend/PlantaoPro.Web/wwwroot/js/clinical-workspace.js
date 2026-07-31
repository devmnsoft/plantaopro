(() => {
  'use strict';
  const root = document.querySelector('[data-clinical-workspace]');
  if (!root) return;
  const consultaId = root.dataset.consultaId;
  let versao = Number(root.dataset.versao || 1);
  const token = document.querySelector('meta[name="request-verification-token"]')?.content;
  const status = root.querySelector('[data-save-status]');
  const fields = ['anamnese', 'exameFisico', 'hipoteseDiagnostica', 'diagnostico', 'conduta', 'orientacoes', 'observacoes'];
  const value = name => root.querySelector(`[name="${name}"]`)?.value || '';
  const setStatus = (message, state) => { status.textContent = message; status.dataset.state = state; };
  async function request(path, options = {}) {
    const response = await fetch(path, { credentials: 'same-origin', headers: { 'Content-Type': 'application/json', ...(token ? { RequestVerificationToken: token } : {}) }, ...options });
    const body = await response.json();
    if (!response.ok) { const error = new Error(body.message || 'Não foi possível concluir a operação.'); error.status = response.status; throw error; }
    return body.data;
  }
  async function salvar() {
    const button = root.querySelector('[data-action="save"]'); button.disabled = true; setStatus('Salvando…', 'saving');
    try {
      const payload = Object.fromEntries(fields.map(name => [name, value(name)])); payload.versao = versao;
      const data = await request(`/api/consultas/${consultaId}/rascunho`, { method: 'PUT', body: JSON.stringify(payload) });
      versao = data.versao; root.dataset.versao = String(versao); setStatus(`Salvo às ${new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`, 'saved');
    } catch (error) {
      setStatus(error.status === 409 ? 'Há alterações mais recentes. Recarregue antes de salvar.' : error.message, 'error');
      if (error.status === 409) document.querySelector('#concurrencyConflictModal')?.showModal();
    } finally { button.disabled = false; }
  }
  async function finalizar() {
    const pendencias = await request(`/api/consultas/${consultaId}/pendencias-finalizacao`);
    const list = root.querySelector('[data-finalization-pending]'); list.replaceChildren(...pendencias.impeditivas.map(item => Object.assign(document.createElement('li'), { textContent: item })));
    const dialog = document.querySelector('#finalizationChecklistModal'); dialog.dataset.blocked = String(!pendencias.podeFinalizar); dialog.showModal();
  }
  root.addEventListener('click', event => { const action = event.target.closest('[data-action]')?.dataset.action; if (action === 'save') salvar(); if (action === 'finalize') finalizar().catch(e => setStatus(e.message, 'error')); if (action === 'reload') location.reload(); });
  document.querySelector('[data-confirm-finalization]')?.addEventListener('click', async event => { event.currentTarget.disabled = true; try { await request(`/api/consultas/${consultaId}/finalizar`, { method: 'POST', body: JSON.stringify({ versao }) }); location.assign('/Consultas'); } catch (e) { setStatus(e.message, 'error'); } finally { event.currentTarget.disabled = false; } });
})();
