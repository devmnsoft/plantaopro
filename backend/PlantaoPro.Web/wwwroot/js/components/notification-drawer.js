export class NotificationDrawer {
  constructor(api) { this.api = api; this.drawer = document.querySelector('#notificationDrawer'); this.trigger = null; this.items = Array.of(); }
  bind() {
    if (!this.drawer) return;
    document.querySelectorAll('[data-notification-open]').forEach(button => button.addEventListener('click', () => this.open(button)));
    this.drawer.querySelector('[data-notification-close]')?.addEventListener('click', () => this.close());
    this.drawer.querySelector('[data-notification-read-all]')?.addEventListener('click', () => this.readAll());
    this.drawer.querySelector('[data-notification-filter]')?.addEventListener('change', () => this.render());
    this.drawer.addEventListener('keydown', event => { if (event.key === 'Escape') this.close(); });
    this.refreshCount();
  }
  async request(path, options) {
    const response = await fetch(this.api + path, { credentials: 'same-origin', ...options, headers: { 'Content-Type': 'application/json', ...(options?.headers || {}) } });
    if (!response.ok) {
      const messages = { 401: 'Sua sessão expirou. Entre novamente para consultar as notificações.', 403: 'Você não tem permissão para consultar estas notificações.', 404: 'A central de notificações ainda não está disponível para esta conta.' };
      const error = new Error(messages[response.status] || (response.status >= 500 ? 'A central de notificações está temporariamente indisponível.' : 'As notificações não puderam ser atualizadas.'));
      error.status = response.status; throw error;
    }
    if (response.status === 204) return null;
    const payload = await response.json();
    return payload && Object.prototype.hasOwnProperty.call(payload, 'data') ? payload.data : payload;
  }
  async refreshCount() {
    try { const payload = await this.request('/nao-lidas'); const items = Array.isArray(payload) ? payload : []; document.querySelectorAll('[data-notification-count]').forEach(counter => { counter.querySelector('[data-notification-count-value]').textContent = items.length ? String(items.length) : ''; counter.hidden = items.length === 0; }); }
    catch { /* A ausência do backend não pode produzir contador fictício. */ }
  }
  async open(trigger) { this.trigger = trigger; this.drawer.hidden = false; document.querySelector('[data-overlay-backdrop]')?.removeAttribute('hidden'); this.drawer.querySelector('[data-notification-close]')?.focus(); await this.load(); }
  close() { this.drawer.hidden = true; document.querySelector('[data-overlay-backdrop]')?.setAttribute('hidden', ''); this.trigger?.focus(); }
  state(title, message, retry = false) {
    const list = this.drawer.querySelector('[data-notification-list]'); list.replaceChildren();
    const panel = document.createElement('div'); panel.className = 'pp-empty-state';
    const heading = document.createElement('h3'); heading.textContent = title;
    const copy = document.createElement('p'); copy.textContent = message; panel.append(heading, copy);
    if (retry) { const button = document.createElement('button'); button.type = 'button'; button.className = 'button button-primary'; button.textContent = 'Tentar novamente'; button.addEventListener('click', () => this.load()); panel.append(button); }
    list.append(panel);
  }
  async load() {
    const list = this.drawer.querySelector('[data-notification-list]'); list.setAttribute('aria-busy', 'true'); this.state('Carregando atualizações', 'Consultando a central de notificações…');
    try { const payload = await this.request('/nao-lidas'); this.items = Array.isArray(payload) ? payload : Array.of(); this.render(); } catch (error) { this.items = Array.of(); this.state('Notificações indisponíveis', error.message, error.status !== 401 && error.status !== 403 && error.status !== 404); } finally { list.setAttribute('aria-busy', 'false'); }
  }
  safeDestination(value) { if (!value) return null; try { const url = new URL(value, window.location.origin); return url.origin === window.location.origin ? `${url.pathname}${url.search}${url.hash}` : null; } catch { return null; } }
  render() {
    const category = this.drawer.querySelector('[data-notification-filter]').value;
    const items = this.items.filter(item => !category || item.categoria === category);
    const readAll = this.drawer.querySelector('[data-notification-read-all]'); readAll.hidden = this.items.length === 0; readAll.disabled = this.items.length === 0;
    if (!items.length) { this.state('Tudo em dia', 'Não há notificações novas neste filtro. Novas atualizações reais aparecerão aqui.'); return; }
    const list = this.drawer.querySelector('[data-notification-list]'); list.replaceChildren();
    items.forEach(item => {
      const article = document.createElement('article'); article.className = 'notification-item'; article.dataset.id = item.id;
      const type = document.createElement('span'); type.textContent = item.categoria || 'SISTEMA';
      const title = document.createElement('h3'); title.textContent = item.titulo || 'Atualização';
      const message = document.createElement('p'); message.textContent = item.mensagem || '';
      const actions = document.createElement('div');
      const read = document.createElement('button'); read.type = 'button'; read.className = 'button button-subtle'; read.textContent = 'Marcar como lida'; read.disabled = !item.id; read.addEventListener('click', () => this.read(item.id, read)); actions.append(read);
      const destination = this.safeDestination(item.destinoUrl);
      if (destination) { const link = document.createElement('a'); link.className = 'button button-subtle'; link.href = destination; link.textContent = 'Abrir origem'; actions.append(link); }
      article.append(type, title, message, actions); list.append(article);
    });
  }
  async read(id, button) { button.disabled = true; try { await this.request(`/${encodeURIComponent(id)}/lida`, { method: 'POST' }); this.items = this.items.filter(item => item.id !== id); this.render(); this.refreshCount(); } catch (error) { button.disabled = false; this.state('Não foi possível atualizar', error.message, true); } }
  async readAll() { const button = this.drawer.querySelector('[data-notification-read-all]'); button.disabled = true; try { await this.request('/marcar-todas-lidas', { method: 'POST' }); this.items = Array.of(); this.render(); this.refreshCount(); } catch (error) { button.disabled = false; this.state('Não foi possível atualizar', error.message, true); } }
}
