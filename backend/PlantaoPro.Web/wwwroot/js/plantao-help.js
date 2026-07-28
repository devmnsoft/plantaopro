(function(){
  window.PlantaoPro = window.PlantaoPro || {};
  document.querySelectorAll('[data-assistente-contextual]').forEach(function(box){
    var key = box.getAttribute('data-storage-key') || 'pp-assistente-contextual-recolhido';
    var body = box.querySelector('[data-assistente-body]');
    var toggle = box.querySelector('[data-assistente-toggle]');
    function setCollapsed(value){
      if (!body || !toggle) return;
      body.hidden = value;
      toggle.textContent = value ? 'Expandir' : 'Recolher';
      toggle.setAttribute('aria-expanded', value ? 'false' : 'true');
      window.localStorage.setItem(key, value ? '1' : '0');
    }
    setCollapsed(window.localStorage.getItem(key) === '1');
    if (toggle) toggle.addEventListener('click', function(){ setCollapsed(!body.hidden); });
    var understood = box.querySelector('[data-assistente-entendi]');
    if (understood) understood.addEventListener('click', function(){ setCollapsed(true); });
  });
}());
