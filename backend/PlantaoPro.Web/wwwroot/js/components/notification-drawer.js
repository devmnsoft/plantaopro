export class NotificationDrawer {
  constructor(api) { this.api = api; this.drawer = document.querySelector('#notificationDrawer'); this.trigger = null; this.items = []; }
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
      const messages = { 403: 'Seu perfil não tem permissão para acessar notificações.', 404: 'A central de notificações não está disponível neste ambiente.' };
      throw new Error(messages[response.status] || (response.status === 401 ? 'Sua sessão expirou. Entre novamente para consultar notificações.' : 'As notificações não puderam ser atualizadas.'));
    }
    if (response.status === 204) return null;
    const payload = await response.json();
    if (payload && typeof payload === 'object' && Object.hasOwn(payload, 'success') && payload.success === false) throw new Error(payload.message || 'As notificações não puderam ser atualizadas.');
    return payload && typeof payload === 'object' && Object.hasOwn(payload, 'data') ? payload.data : payload;
  }
  async refreshCount() {
    try { const items = await this.request('/nao-lidas'); document.querySelectorAll('[data-notification-count]').forEach(counter => { counter.querySelector('[data-notification-count-value]').textContent = String(items.length); counter.querySelector('[data-notification-count-description]').textContent = `${items.length} notificações não lidas`; counter.hidden = items.length === 0; }); }
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
    try { this.items = await this.request('/nao-lidas'); this.render(); } catch (error) { this.state('Notificações indisponíveis', error.message, true); } finally { list.setAttribute('aria-busy', 'false'); }
  }
  safeDestination(value) { if (!value) return null; try { const url = new URL(value, window.location.origin); return url.origin === window.location.origin ? `${url.pathname}${url.search}${url.hash}` : null; } catch { return null; } }
  render() {
    const category = this.drawer.querySelector('[data-notification-filter]').value;
    const items = this.items.filter(item => !category || String(item.categoria || '').toUpperCase() === category);
    this.drawer.querySelector('[data-notification-read-all]').hidden = this.items.length === 0;
    if (!items.length) { this.state('Tudo em dia', 'Não há notificações novas neste filtro. Novas atualizações reais aparecerão aqui.'); return; }
    const list = this.drawer.querySelector('[data-notification-list]'); list.replaceChildren();
    items.forEach(item => {
      const article = document.createElement('article'); article.className = 'notification-item'; article.dataset.id = item.id;
      const type = document.createElement('span'); type.textContent = item.categoria || 'SISTEMA';
      const title = document.createElement('h3'); title.textContent = item.titulo || 'Atualização';
      const message = document.createElement('p'); message.textContent = item.mensagem || '';
      const actions = document.createElement('div');
      const read = document.createElement('button'); read.type = 'button'; read.className = 'button button-subtle'; read.textContent = 'Marcar como lida'; read.addEventListener('click', () => this.read(item.id)); actions.append(read);
      const destination = this.safeDestination(item.destinoUrl);
      if (destination) { const link = document.createElement('a'); link.className = 'button button-subtle'; link.href = destination; link.textContent = 'Abrir origem'; actions.append(link); }
      article.append(type, title, message, actions); list.append(article);
    });
  }
  async read(id) { await this.request(`/${id}/lida`, { method: 'POST' }); this.items = this.items.filter(item => item.id !== id); this.render(); this.refreshCount(); }
  async readAll() { await this.request('/marcar-todas-lidas', { method: 'POST' }); this.items = []; this.render(); this.refreshCount(); }
}
