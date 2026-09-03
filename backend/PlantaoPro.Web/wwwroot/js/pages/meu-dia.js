import { WorkItemDrawer } from '../components/work-item-drawer.js';

const toast = (title, message, severity = 'success') => {
  const region = document.querySelector('[data-toast-region]');
  if (!region) return;
  const item = document.createElement('article');
  item.className = `app-toast ${severity}`;
  item.tabIndex = 0;
  const heading = document.createElement("strong"); heading.textContent = title; const copy = document.createElement("p"); copy.textContent = message; item.append(heading, copy);
  region.prepend(item);
  window.setTimeout(() => item.remove(), 7000);
};

if (document.querySelector('[data-work-item-drawer]')) {
  new WorkItemDrawer('/bff/operacao', toast).bind();
}
