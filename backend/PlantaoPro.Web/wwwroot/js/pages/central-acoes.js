const root = document.querySelector('[data-action-center]');
const token = root?.querySelector('input[name="__RequestVerificationToken"]')?.value;
const dialog = document.querySelector('[data-snooze-dialog]');
const snoozeInput = dialog?.querySelector('[name="snoozedUntil"]');
const snoozeError = dialog?.querySelector('[data-snooze-error]');
const snoozeForm = dialog?.querySelector('[data-snooze-form]');
let pendingButton;

const notify = (message, type = 'success') => {
  if (window.PlantaoProToast?.show) window.PlantaoProToast.show({ message, type });
  else document.querySelector('#appLiveRegion')?.replaceChildren(document.createTextNode(message));
};

const submitSnooze = async button => {
  const until = new Date(snoozeInput?.value || '');
  if (Number.isNaN(until.valueOf()) || until <= new Date()) {
    if (snoozeError) snoozeError.textContent = 'Escolha uma data e hora futuras.';
    snoozeInput?.focus();
    return;
  }
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
};

root?.addEventListener('click', event => {
  const button = event.target.closest('[data-snooze]');
  if (!button) return;
  const tomorrow = new Date(Date.now() + 24 * 60 * 60 * 1000);
  pendingButton = button;
  if (snoozeInput) snoozeInput.value = new Date(tomorrow.getTime() - tomorrow.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  if (snoozeError) snoozeError.textContent = '';
  dialog?.showModal();
});

dialog?.addEventListener('close', () => {
  const button = pendingButton;
  pendingButton = undefined;
  if (dialog.returnValue === 'confirm' && button) void submitSnooze(button);
  else button?.focus();
});

snoozeForm?.addEventListener('submit', event => {
  if (event.submitter?.value !== 'confirm') return;
  const until = new Date(snoozeInput?.value || '');
  if (!Number.isNaN(until.valueOf()) && until > new Date()) return;
  event.preventDefault();
  if (snoozeError) snoozeError.textContent = 'Escolha uma data e hora futuras.';
  snoozeInput?.focus();
});
