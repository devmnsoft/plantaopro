(function(){
  const initTooltips=()=>document.querySelectorAll('[data-bs-toggle="tooltip"],[title]').forEach(el=>{if(window.bootstrap&&!el.dataset.ppTooltip){new bootstrap.Tooltip(el);el.dataset.ppTooltip='1';}});
  const autoCloseAlerts=()=>setTimeout(()=>document.querySelectorAll('.alert').forEach(a=>{if(window.bootstrap){bootstrap.Alert.getOrCreateInstance(a).close();}}),6000);
  const wireConfirmActions=()=>document.querySelectorAll('[data-confirm-action]').forEach(btn=>btn.addEventListener('click',()=>{const url=btn.getAttribute('data-confirm-action');if(url){window.location.href=url;}}));
  const markActiveMenu=()=>{const path=(window.location.pathname||'').toLowerCase();document.querySelectorAll('.nav-link-app').forEach(link=>{const href=(link.getAttribute('href')||'').toLowerCase();if(href&&path.startsWith(href)&&href!=='/'){link.classList.add('active');}})};
  const copyText=(text)=>navigator.clipboard?.writeText(text||'');
  window.PlantaoProUi={copyText};
  document.addEventListener('DOMContentLoaded',()=>{initTooltips();autoCloseAlerts();wireConfirmActions();markActiveMenu();});
})();
