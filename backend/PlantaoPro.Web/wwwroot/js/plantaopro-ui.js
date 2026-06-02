(function(){
  const initTooltips=()=>document.querySelectorAll('[data-bs-toggle="tooltip"],[title]').forEach(el=>{if(window.bootstrap&&!el.dataset.ppTooltip){new bootstrap.Tooltip(el);el.dataset.ppTooltip='1';}});
  const autoCloseAlerts=()=>setTimeout(()=>document.querySelectorAll('.alert').forEach(a=>{if(window.bootstrap){bootstrap.Alert.getOrCreateInstance(a).close();}}),6000);
  const markActiveMenu=()=>{const path=(window.location.pathname||'').toLowerCase();document.querySelectorAll('.nav-link-app').forEach(link=>{const href=(link.getAttribute('href')||'').toLowerCase();if(href&&path.startsWith(href)&&href!=='/'){link.classList.add('active');}})};
  const copyText=(text)=>navigator.clipboard?.writeText(text||'');
  const sanitize=(value)=>String(value||'').replace(/[&<>'"]/g,(c)=>({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[c]));

  function ensureConfirmModal(){
    let modal=document.getElementById('ppGlobalConfirmModal');
    if(modal){return modal;}
    modal=document.createElement('div');
    modal.className='modal fade';
    modal.id='ppGlobalConfirmModal';
    modal.tabIndex=-1;
    modal.setAttribute('aria-hidden','true');
    modal.innerHTML='<div class="modal-dialog modal-dialog-centered"><div class="modal-content pp-confirm-modal"><div class="modal-header"><div><span class="pp-confirm-kicker">Confirmação necessária</span><h5 class="modal-title mb-0" data-pp-confirm-title>Confirmar ação</h5></div><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Fechar"></button></div><div class="modal-body"><p class="mb-0" data-pp-confirm-message>Deseja realmente continuar?</p></div><div class="modal-footer"><button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancelar</button><button type="button" class="btn btn-danger" data-pp-confirm-submit><i class="bi bi-check2-circle me-1"></i>Confirmar</button></div></div></div>';
    document.body.appendChild(modal);
    return modal;
  }

  function wireConfirmActions(){
    const modal=ensureConfirmModal();
    const instance=window.bootstrap?bootstrap.Modal.getOrCreateInstance(modal):null;
    const titleEl=modal.querySelector('[data-pp-confirm-title]');
    const messageEl=modal.querySelector('[data-pp-confirm-message]');
    const submit=modal.querySelector('[data-pp-confirm-submit]');
    let pending=null;
    document.querySelectorAll('[data-confirm="true"], [data-confirm-action]').forEach(el=>{
      if(el.dataset.ppConfirmBound==='1'){return;}
      el.dataset.ppConfirmBound='1';
      el.addEventListener('click',(event)=>{
        const action=el.getAttribute('data-confirm-action')||el.getAttribute('href')||'';
        if(!action && el.tagName!=='BUTTON'){return;}
        event.preventDefault();
        pending=el;
        const type=el.getAttribute('data-confirm-type')||'danger';
        titleEl.textContent=el.getAttribute('data-confirm-title')||'Confirmar ação';
        messageEl.textContent=el.getAttribute('data-confirm-message')||'Esta ação é sensível e será registrada em auditoria. Deseja continuar?';
        submit.className='btn btn-'+type;
        submit.innerHTML='<i class="bi bi-check2-circle me-1"></i>'+(el.getAttribute('data-confirm-text')||'Confirmar');
        if(instance){instance.show();}
      });
    });
    if(submit && submit.dataset.ppSubmitBound!=='1'){
      submit.dataset.ppSubmitBound='1';
      submit.addEventListener('click',()=>{
        if(!pending){return;}
        const formId=pending.getAttribute('data-confirm-form');
        const action=pending.getAttribute('data-confirm-action')||pending.getAttribute('href')||'';
        if(formId){document.getElementById(formId)?.requestSubmit();return;}
        const form=pending.closest('form');
        if(form && pending.getAttribute('type')==='submit'){form.requestSubmit();return;}
        if(action){window.location.href=action;}
      });
    }
  }

  function wireSubmitLoading(){
    document.querySelectorAll('form').forEach(form=>{
      if(form.dataset.ppLoadingBound==='1'){return;}
      form.dataset.ppLoadingBound='1';
      form.addEventListener('submit',()=>{
        form.querySelectorAll('button[type="submit"], .btn-submit').forEach(btn=>{
          if(btn.dataset.confirm==='true'){return;}
          btn.classList.add('is-loading');
          btn.setAttribute('aria-busy','true');
          if(!btn.dataset.originalHtml){btn.dataset.originalHtml=btn.innerHTML;}
          btn.innerHTML='<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Processando...';
          btn.disabled=true;
        });
      });
    });
  }

  function onlyDigits(value){return String(value||'').replace(/\D/g,'');}
  function applyMasks(){
    document.querySelectorAll('[data-mask="cpf"]').forEach(input=>input.addEventListener('input',()=>{let v=onlyDigits(input.value).slice(0,11);input.value=v.replace(/(\d{3})(\d)/,'$1.$2').replace(/(\d{3})(\d)/,'$1.$2').replace(/(\d{3})(\d{1,2})$/,'$1-$2');}));
    document.querySelectorAll('[data-mask="cnpj"]').forEach(input=>input.addEventListener('input',()=>{let v=onlyDigits(input.value).slice(0,14);input.value=v.replace(/(\d{2})(\d)/,'$1.$2').replace(/(\d{3})(\d)/,'$1.$2').replace(/(\d{3})(\d)/,'$1/$2').replace(/(\d{4})(\d{1,2})$/,'$1-$2');}));
    document.querySelectorAll('[data-mask="phone"]').forEach(input=>input.addEventListener('input',()=>{let v=onlyDigits(input.value).slice(0,11);input.value=v.length>10?v.replace(/(\d{2})(\d{5})(\d{1,4})/,'($1) $2-$3'):v.replace(/(\d{2})(\d{4})(\d{1,4})/,'($1) $2-$3');}));
    document.querySelectorAll('[data-mask="crmuf"]').forEach(input=>input.addEventListener('input',()=>{input.value=sanitize(input.value).toUpperCase().slice(0,20);}));
  }

  window.PlantaoProUi={copyText,refresh:()=>{initTooltips();wireConfirmActions();wireSubmitLoading();applyMasks();}};
  document.addEventListener('DOMContentLoaded',()=>{initTooltips();autoCloseAlerts();wireConfirmActions();wireSubmitLoading();applyMasks();markActiveMenu();});
})();
