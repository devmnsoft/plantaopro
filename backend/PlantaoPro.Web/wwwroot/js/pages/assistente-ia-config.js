// P2 IA — teste de conexão do provedor (página de configuração admin).
(function () {
  const form = document.querySelector('[data-ai-test-form]');
  const out = document.querySelector('[data-ai-test-output]');
  const btn = document.querySelector('[data-ai-test-btn]');
  if (!form || !out || !btn) return;

  const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');

  form.addEventListener('submit', async event => {
    event.preventDefault();
    const body = new FormData(form);
    const token = tokenEl ? tokenEl.value : '';
    if (!body.has('__RequestVerificationToken') && token) body.append('__RequestVerificationToken', token);
    btn.disabled = true;
    out.replaceChildren(document.createTextNode('Testando conexão…'));
    let response;
    try {
      response = await fetch('/AssistenteIa/TestarConexao', { method: 'POST', body, credentials: 'same-origin', headers: { Accept: 'application/json' } });
    } catch (erro) {
      out.replaceChildren(document.createTextNode('Não foi possível falar com o servidor agora.'));
      btn.disabled = false;
      return;
    }
    let data = {};
    try { data = await response.json(); } catch (erro) { /* corpo vazio: mantém a mensagem */ }
    if (response.status === 401) { window.location.assign('/Account/Login'); return; }
    out.replaceChildren(document.createTextNode(data.mensagem || 'Sem resposta do servidor.'));
    btn.disabled = false;
  });
})();
