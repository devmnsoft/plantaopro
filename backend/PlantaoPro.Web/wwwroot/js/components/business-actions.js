(() => {
  const supportedErrors = new Map([
    [400, 'Revise os dados informados.'], [401, 'Sua sessão expirou. Entre novamente.'],
    [403, 'Você não tem permissão para esta ação.'], [404, 'O registro não foi encontrado.'],
    [409, 'O status atual não permite esta ação.'], [422, 'Uma regra de negócio impediu a ação.']
  ]);

  function notify(kind, text) {
    if (window.PlantaoProToast?.show) window.PlantaoProToast.show(kind, text);
    const live = document.getElementById('appLiveRegion');
    if (live) live.textContent = text;
  }

  async function execute(button) {
    if (button.disabled || button.getAttribute('aria-busy') === 'true') return;
    const action = button.dataset.businessAction;
    if (!action) return;
    const url = new URL(action, window.location.origin);
    if (url.origin !== window.location.origin) {
      notify('error', 'A ação foi bloqueada porque não pertence a este sistema.');
      return;
    }
    button.disabled = true;
    button.setAttribute('aria-busy', 'true');
    try {
      const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
      const payload = button.dataset.businessPayload ? JSON.parse(button.dataset.businessPayload) : {};
      const response = await fetch(url, {
        method: 'POST', credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json', ...(token ? { RequestVerificationToken: token } : {}) },
        body: JSON.stringify(payload)
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok || result.success === false) throw Object.assign(new Error(result.message || supportedErrors.get(response.status) || 'A ação não foi concluída.'), { status: response.status });
      notify('success', result.message || 'Ação persistida com sucesso.');
      button.dispatchEvent(new CustomEvent('plantaopro:business-success', { bubbles: true, detail: result }));
      if (button.dataset.reloadOnSuccess === 'true') window.location.reload();
    } catch (error) {
      notify('error', error.message || supportedErrors.get(error.status) || 'Falha de comunicação.');
      button.disabled = false;
    } finally { button.removeAttribute('aria-busy'); }
  }

  document.addEventListener('click', event => {
    const button = event.target instanceof Element ? event.target.closest('[data-business-action]') : null;
    if (button) execute(button);
  });
})();
