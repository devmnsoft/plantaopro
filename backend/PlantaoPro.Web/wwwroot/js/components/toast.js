const queue = Array.of(), visible = new Map(), MAX = 3;
export function enqueueToast(message) {
  if (visible.has(message.code) || queue.some(x => x.code === message.code)) return;
  queue.push(message); renderNext();
}
function renderNext() {
  const region = document.querySelector('[data-toast-region]'); if (!region) return;
  while (visible.size < MAX && queue.length) {
    const message = queue.shift(), item = document.createElement('article'); item.className = `pp-toast pp-toast--${message.severity}`; item.tabIndex = 0;
    const title = document.createElement('strong'), description = document.createElement('p'), close = document.createElement('button');
    title.textContent = message.title; description.textContent = message.description; close.type = 'button'; close.textContent = 'Fechar'; close.setAttribute('aria-label', `Fechar: ${message.title}`);
    item.append(title, description, close); region.append(item); visible.set(message.code, item);
    const dismiss = () => { visible.delete(message.code); item.remove(); renderNext(); }; close.addEventListener('click', dismiss);
    if (!message.persistent) { let timer = setTimeout(dismiss, 6000); item.addEventListener('mouseenter', () => clearTimeout(timer), { once: true }); item.addEventListener('focusin', () => clearTimeout(timer), { once: true }); }
  }
}
