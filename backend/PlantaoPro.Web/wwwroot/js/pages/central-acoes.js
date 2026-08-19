const root = document.querySelector('[data-action-center]');
const token = root?.querySelector('input[name="__RequestVerificationToken"]')?.value;

const notify = (message, type = 'success') => {
  if (window.PlantaoProToast?.show) window.PlantaoProToast.show({ message, type });
  else document.querySelector('#appLiveRegion')?.replaceChildren(document.createTextNode(message));
};

root?.addEventListener('click', async event => {
  const button = event.target.closest('[data-snooze]');
  if (!button) return;
  const tomorrow = new Date(Date.now() + 24 * 60 * 60 * 1000);
  const answer = window.prompt('Adiar até (data e hora):', tomorrow.toISOString().slice(0, 16));
  if (!answer) return;
  const until = new Date(answer);
  if (Number.isNaN(until.valueOf()) || until <= new Date()) { notify('Informe uma data futura válida.', 'warning'); return; }

  button.disabled = true;
  const body = new FormData(); body.append('key', button.dataset.snooze); body.append('snoozedUntil', until.toISOString());
  if (token) body.append('__RequestVerificationToken', token);
  try {
    const response = await fetch('/Pendencias/adiar', { method: 'POST', body, credentials: 'same-origin', headers: { Accept: 'application/json' } });
    const payload = await response.json().catch(() => ({}));
    if (!response.ok) throw new Error(payload.message || 'Não foi possível adiar a ação.');
    button.closest('[data-item-key]')?.remove();
    notify(payload.message || 'Ação adiada.');
  } catch (error) { notify(error.message, 'error'); button.disabled = false; }
});
