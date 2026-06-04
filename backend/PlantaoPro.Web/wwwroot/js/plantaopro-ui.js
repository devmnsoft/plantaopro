(function(){
  const confirmModalId='pp-confirm-modal';
  const initTooltips=()=>document.querySelectorAll('[data-bs-toggle="tooltip"],[title]').forEach(el=>{if(window.bootstrap&&!el.dataset.ppTooltip){new bootstrap.Tooltip(el);el.dataset.ppTooltip='1';}});
  const autoCloseAlerts=()=>setTimeout(()=>document.querySelectorAll('.alert').forEach(a=>{if(window.bootstrap){bootstrap.Alert.getOrCreateInstance(a).close();}}),6000);
  const markActiveMenu=()=>{const path=(window.location.pathname||'').toLowerCase();document.querySelectorAll('.nav-link-app').forEach(link=>{const href=(link.getAttribute('href')||'').toLowerCase();if(href&&path.startsWith(href)&&href!=='/'){link.classList.add('active');}})};
  const copyText=(text)=>navigator.clipboard?.writeText(text||'');
  const sanitize=(value)=>String(value||'').replace(/[&<>'"]/g,(c)=>({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[c]));

  function showToast(type,message){
    const toast=window.PlantaoProToast;
    if(toast&&typeof toast[type]==='function'){toast[type](message);return;}
    if(toast&&typeof toast.info==='function'){toast.info(message);}
  }

  function ensureConfirmModal(){
    let modal=document.getElementById(confirmModalId);
    if(modal){return modal;}
    modal=document.createElement('div');
    modal.className='modal fade';
    modal.id=confirmModalId;
    modal.tabIndex=-1;
    modal.setAttribute('aria-hidden','true');
    modal.innerHTML='<div class="modal-dialog modal-dialog-centered"><div class="modal-content pp-confirm-modal pp-confirm-modal-warning"><div class="modal-header"><div><span class="pp-confirm-kicker">Confirmação necessária</span><h5 class="modal-title mb-0" data-pp-confirm-title>Confirmar ação</h5></div><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Fechar"></button></div><div class="modal-body"><p class="mb-0" data-pp-confirm-message>Deseja realmente continuar?</p></div><div class="modal-footer"><button type="button" class="btn btn-light" data-bs-dismiss="modal">Cancelar</button><button type="button" class="btn btn-primary" data-pp-confirm-action><i class="bi bi-check2-circle me-1"></i>Confirmar</button></div></div></div>';
    document.body.appendChild(modal);
    return modal;
  }

  function getConfirmOptions(el){
    return {
      title:el.getAttribute('data-confirm-title')||'Confirmar ação',
      message:el.getAttribute('data-confirm-message')||'Esta ação é sensível e será registrada em auditoria. Deseja continuar?',
      type:el.getAttribute('data-confirm-type')||'warning',
      text:el.getAttribute('data-confirm-text')||'Confirmar',
      url:el.getAttribute('data-confirm-url')||el.getAttribute('data-confirm-action')||el.getAttribute('href')||''
    };
  }

  function wireConfirmActions(){
    const modal=ensureConfirmModal();
    const instance=window.bootstrap&&window.bootstrap.Modal?bootstrap.Modal.getOrCreateInstance(modal):null;
    const titleEl=modal.querySelector('[data-pp-confirm-title]');
    const messageEl=modal.querySelector('[data-pp-confirm-message]');
    const actionButton=modal.querySelector('[data-pp-confirm-action],[data-pp-confirm-submit]');
    let pending=null;

    const openModal=(source,event)=>{
      if(event){event.preventDefault();event.stopImmediatePropagation();}
      if(!instance){
        showToast('error','Não foi possível abrir a confirmação. Atualize a página e tente novamente.');
        return;
      }

      pending=source;
      const options=getConfirmOptions(source);
      const safeType=String(options.type||'warning').replace(/[^a-z-]/gi,'').toLowerCase()||'warning';
      if(titleEl){titleEl.textContent=options.title;}
      if(messageEl){messageEl.textContent=options.message;}
      if(actionButton){
        actionButton.className='btn btn-'+safeType;
        actionButton.innerHTML='<i class="bi bi-check2-circle me-1"></i>'+sanitize(options.text);
      }
      const content=modal.querySelector('.pp-confirm-modal');
      if(content){
        Array.from(content.classList).forEach((className)=>{if(className.indexOf('pp-confirm-modal-')===0){content.classList.remove(className);}});
        content.classList.add('pp-confirm-modal-'+safeType);
      }
      instance.show();
    };

    document.querySelectorAll('[data-confirm="true"], [data-confirm-url], [data-confirm-action]').forEach(el=>{
      if(el.dataset.ppConfirmBound==='1'){return;}
      el.dataset.ppConfirmBound='1';

      if(el.tagName==='FORM'){
        el.addEventListener('submit',(event)=>{
          if(el.dataset.ppConfirmApproved==='1'){
            delete el.dataset.ppConfirmApproved;
            return;
          }
          openModal(el,event);
        });
        return;
      }

      el.addEventListener('click',(event)=>{
        const action=getConfirmOptions(el).url;
        const isButton=el.tagName==='BUTTON'||el.getAttribute('role')==='button';
        if(!action&&!isButton){return;}
        openModal(el,event);
      });
    });

    if(actionButton&&actionButton.dataset.ppSubmitBound!=='1'){
      actionButton.dataset.ppSubmitBound='1';
      actionButton.addEventListener('click',()=>{
        if(!pending){return;}
        const formId=pending.getAttribute('data-confirm-form');
        const targetForm=formId?document.getElementById(formId):pending.tagName==='FORM'?pending:pending.closest('form');
        const action=getConfirmOptions(pending).url;

        if(instance){instance.hide();}
        if(targetForm){
          targetForm.dataset.ppConfirmApproved='1';
          targetForm.requestSubmit();
          pending=null;
          return;
        }
        if(action){window.location.assign(action);}
        pending=null;
      });
    }
  }

  function wireSubmitLoading(){
    document.querySelectorAll('form').forEach(form=>{
      if(form.dataset.ppLoadingBound==='1'){return;}
      form.dataset.ppLoadingBound='1';
      form.addEventListener('submit',()=>{
        if(form.dataset.confirm==='true'&&form.dataset.ppConfirmApproved!=='1'){return;}
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
