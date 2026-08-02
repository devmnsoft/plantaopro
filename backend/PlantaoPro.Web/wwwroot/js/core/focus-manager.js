export function focusFirstInvalid(container = document) {
  const field = container.querySelector('[aria-invalid="true"], .input-validation-error');
  field?.focus();
}
export function trapFocus(container, event) {
  if (event.key !== 'Tab') return;
  const nodes = [...container.querySelectorAll('button,[href],input,select,textarea,[tabindex]:not([tabindex="-1"])')].filter(x => !x.disabled);
  if (!nodes.length) return;
  const first = nodes[0], last = nodes[nodes.length - 1];
  if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
  else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
}
