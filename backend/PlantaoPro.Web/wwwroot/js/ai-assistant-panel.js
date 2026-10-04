// P2 IA — painéis do assistente (opcionais e não bloqueantes).
// O texto gerado pela IA é renderizado exclusivamente via textContent:
// nenhum HTML executável é inserido no DOM (dupla proteção — a saída
// também é HTML-escapada no servidor).
(function () {
  const panels = document.querySelectorAll('[data-ai-panel]');
  if (panels.length === 0) return;

  const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
  const token = tokenEl ? tokenEl.value : '';

  const setStatus = (panel, busy, message) => {
    const out = panel.querySelector('[data-ai-output]');
    const btn = panel.querySelector('[data-ai-run]');
    if (btn) {
      btn.disabled = busy;
      btn.setAttribute('aria-busy', busy ? 'true' : 'false');
    }
    panel.setAttribute('aria-busy', busy ? 'true' : 'false');
    if (out) out.replaceChildren(document.createTextNode(message));
  };

  const paint = (panel, data) => {
    setStatus(panel, false, data.mensagem || 'Sem resposta.');
    const meta = panel.querySelector('[data-ai-meta]');
    if (meta) {
      const partes = ['Fonte: ' + (data.provedor || 'desconhecida')];
      if (data.modelo) partes.push('Modelo: ' + data.modelo);
      if (data.fallbackUsado) partes.push('alternativa usada');
      meta.replaceChildren(document.createTextNode(partes.join(' · ')));
    }
    const box = panel.querySelector('[data-ai-text]');
    if (box) {
      box.replaceChildren();
      box.hidden = !data.texto;
      if (data.texto) box.append(document.createTextNode(data.texto));
    }
  };

  panels.forEach(panel => {
    const url = panel.dataset.aiUrl;
    const method = panel.dataset.aiMethod || 'POST';
    const btn = panel.querySelector('[data-ai-run]');
    if (!url || !btn) return;
    btn.addEventListener('click', async () => {
      setStatus(panel, true, 'Gerando… pode levar alguns segundos.');
      const body = method === 'POST' ? new FormData() : undefined;
      if (body && token) body.append('__RequestVerificationToken', token);
      let response;
      try {
        response = await fetch(url, { method, body, credentials: 'same-origin', headers: { Accept: 'application/json' } });
      } catch (erro) {
        paint(panel, { mensagem: 'Não foi possível falar com o servidor agora. Tente novamente.' });
        return;
      }
      let data = {};
      try { data = await response.json(); } catch (erro) { /* corpo vazio: mantém a mensagem */ }
      if (response.status === 401) { window.location.assign('/Account/Login'); return; }
      paint(panel, data);
    });
  });
})();
