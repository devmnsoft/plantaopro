import {NotificationDrawer} from '../components/notification-drawer.js';
import {QuickActionDrawer} from '../components/quick-action-drawer.js';
import {RealtimeClient} from './realtime-client.js';
const notifications=new NotificationDrawer('/bff/operacao/notificacoes');notifications.bind();
new QuickActionDrawer().bind();
const realtime=new RealtimeClient({onNotification:()=>notifications.refreshCount(),onWorkItem:()=>document.dispatchEvent(new CustomEvent('central:refresh'))});realtime.connect();
document.querySelectorAll('[data-drawer-close]').forEach(button=>button.addEventListener('click',()=>{const drawer=button.closest('.app-drawer');drawer.hidden=true;document.querySelector('[data-overlay-backdrop]').hidden=true;}));
