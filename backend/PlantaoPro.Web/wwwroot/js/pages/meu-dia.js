import { WorkItemDrawer } from '../components/work-item-drawer.js';

const toast = (title, message, severity = 'success') => {
  const region = document.querySelector('[data-toast-region]');
  if (!region) return;
  const item = document.createElement('article');
  item.className = `app-toast ${severity}`;
  item.tabIndex = 0;
  item.innerHTML = `<strong>${title}</strong><p>${message}</p>`;
  region.prepend(item);
  window.setTimeout(() => item.remove(), 7000);
};

if (document.querySelector('[data-work-item-drawer]')) {
  new WorkItemDrawer('/bff/operacao', toast).bind();
}
