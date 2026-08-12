(() => {
    const modal = document.querySelector('#agendaActionModal');
    const confirmButton = modal?.querySelector('[data-agenda-confirm]');
    const reasonGroup = modal?.querySelector('[data-agenda-reason]');
    const reason = modal?.querySelector('#agendaActionReason');
    const error = modal?.querySelector('[data-agenda-error]');
    if (!modal || !confirmButton || !reasonGroup || !reason || !error) return;

    let agendaId = '';
    let operation = '';
    modal.addEventListener('show.bs.modal', (event) => {
        const trigger = event.relatedTarget;
        agendaId = trigger?.dataset.agendaId || '';
        operation = trigger?.dataset.agendaOperation || '';
        modal.querySelector('#agendaActionTitle').textContent = trigger?.dataset.agendaLabel || 'Confirmar ação';
        reasonGroup.hidden = operation !== 'cancelar';
        reason.value = '';
        error.hidden = true;
    });

    confirmButton.addEventListener('click', async () => {
        if (operation === 'cancelar' && !reason.value.trim()) {
            error.textContent = 'Informe o motivo do cancelamento.';
            error.hidden = false;
            reason.focus();
            return;
        }
        confirmButton.disabled = true;
        confirmButton.setAttribute('aria-busy', 'true');
        error.hidden = true;
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        const body = new URLSearchParams({ id: agendaId, operacao: operation, motivo: reason.value.trim() });
        try {
            const response = await fetch(modal.dataset.actionUrl, { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded', RequestVerificationToken: token }, body });
            const payload = await response.json();
            if (!response.ok || !payload.success) throw new Error(payload.message || 'Não foi possível concluir a ação.');
            const toast = document.querySelector('#agendaActionToast');
            toast.querySelector('.toast-body').textContent = payload.message || 'Ação concluída.';
            bootstrap.Modal.getInstance(modal)?.hide();
            bootstrap.Toast.getOrCreateInstance(toast).show();
            window.setTimeout(() => window.location.reload(), 700);
        } catch (requestError) {
            error.textContent = requestError.message;
            error.hidden = false;
        } finally {
            confirmButton.disabled = false;
            confirmButton.removeAttribute('aria-busy');
        }
    });
})();
