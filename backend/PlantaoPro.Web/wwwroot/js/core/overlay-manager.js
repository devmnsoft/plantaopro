import { trapFocus } from './focus-manager.js';
class OverlayManager {
  constructor() { this.stack = []; }
  open(element, trigger = document.activeElement) {
    this.stack.push({ element, trigger }); element.hidden = false; element.setAttribute('aria-modal', 'true');
    document.body.classList.add('overlay-open'); element.addEventListener('keydown', this.onKeyDown); element.querySelector('[autofocus],button')?.focus();
  }
  close(element) { const entry = this.stack.find(x => x.element === element); element.hidden = true; this.stack = this.stack.filter(x => x !== entry); if (!this.stack.length) document.body.classList.remove('overlay-open'); entry?.trigger?.focus(); }
  onKeyDown = event => { const current = this.stack.at(-1); if (!current) return; if (event.key === 'Escape') this.close(current.element); else trapFocus(current.element, event); };
}
export const overlays = new OverlayManager();
