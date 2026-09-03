(function(){
  const confirmModalId='pp-confirm-modal';
  const initTooltips=()=>document.querySelectorAll('[data-bs-toggle="tooltip"],[title]').forEach(el=>{if(window.bootstrap&&!el.dataset.ppTooltip){new bootstrap.Tooltip(el);el.dataset.ppTooltip='1';}});
  const autoCloseAlerts=()=>setTimeout(()=>document.querySelectorAll('.alert').forEach(a=>{if(window.bootstrap){bootstrap.Alert.getOrCreateInstance(a).close();}}),6000);
  const markActiveMenu=()=>{const path=(window.location.pathname||'').toLowerCase();document.querySelectorAll('.nav-link-app').forEach(link=>{const href=(link.getAttribute('href')||'').toLowerCase();if(href&&path.startsWith(href)&&href!=='/'){link.classList.add('active');}})};
  const copyText=(text)=>navigator.clipboard?.writeText(text||'');
  const sanitize=(value)=>String(value||'').replace(/[&<>'"]/g,(c)=>({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[c]));

  function wireNavigation(){
    const body=document.body;
    const shell=document.querySelector('.app-shell');
    const toggles=[document.getElementById('sidebarToggle'),document.getElementById('mobileMenuToggle')].filter(Boolean);
    const overlay=document.getElementById('sidebarOverlay');
    const collapse=document.getElementById('sidebarCollapse');
    let returnFocus=null;
    const setOpen=(open,trigger)=>{
      body.classList.toggle('sidebar-open',open);
      toggles.forEach(toggle=>toggle.setAttribute('aria-expanded',String(open)));
      if(open){returnFocus=trigger||document.activeElement;document.querySelector('.app-sidebar a, .app-sidebar button')?.focus();}
      else if(returnFocus){returnFocus.focus();returnFocus=null;}
    };
    toggles.forEach(toggle=>{if(toggle.dataset.ppNavBound!=='1'){toggle.dataset.ppNavBound='1';toggle.addEventListener('click',()=>setOpen(!body.classList.contains('sidebar-open'),toggle));}});
    overlay?.addEventListener('click',()=>setOpen(false));
    document.querySelectorAll('.app-sidebar a').forEach(link=>link.addEventListener('click',()=>{if(window.matchMedia('(max-width: 991.98px)').matches){setOpen(false);}}));
    document.addEventListener('keydown',event=>{if(event.key==='Escape'&&body.classList.contains('sidebar-open')){setOpen(false);}});
    if(shell&&collapse){
      const collapsed=localStorage.getItem('pp-sidebar-collapsed')==='true';
      shell.classList.toggle('sidebar-collapsed',collapsed);collapse.setAttribute('aria-expanded',String(!collapsed));
      collapse.addEventListener('click',()=>{const next=!shell.classList.contains('sidebar-collapsed');shell.classList.toggle('sidebar-collapsed',next);collapse.setAttribute('aria-expanded',String(!next));localStorage.setItem('pp-sidebar-collapsed',String(next));});
    }
  }

  function showToast(type,message){
    const toast=window.PlantaoProToast;
    if(toast&&typeof toast[type]==='function'){toast[type](message);return;}
    if(toast&&typeof toast.info==='function'){toast.info(message);}
  }

  function ensureConfirmModal(){
    return document.getElementById(confirmModalId);
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
    const root=document.getElementById('pp-overlay-root');
    if(!modal||!root||root.dataset.ppConfirmRootBound==='1'){return;}
    root.dataset.ppConfirmRootBound='1';
    const titleEl=modal.querySelector('[data-pp-confirm-title]');
    const messageEl=modal.querySelector('[data-pp-confirm-message]');
    const actionButton=modal.querySelector('[data-pp-confirm-action],[data-pp-confirm-submit]');
    const loadingEl=modal.querySelector('[data-pp-confirm-loading]');
    const statusEl=modal.querySelector('[data-pp-confirm-status]');
    let pending=null;
    let returnFocus=null;

    const closeModal=()=>{
      root.classList.remove('is-active');
      modal.hidden=true;
      document.body.classList.remove('pp-modal-open');
      modal.setAttribute('aria-busy','false');
      if(actionButton){actionButton.disabled=false;}
      loadingEl?.classList.add('d-none');
      if(statusEl){statusEl.textContent='';}
      returnFocus?.focus();
      returnFocus=null;
    };
    const openModal=(source,event)=>{
      event?.preventDefault();
      event?.stopImmediatePropagation();
      pending=source;
      returnFocus=source;
      const options=getConfirmOptions(source);
      const safeType=String(options.type||'warning').replace(/[^a-z-]/gi,'').toLowerCase()||'warning';
      if(titleEl){titleEl.textContent=options.title;}
      if(messageEl){messageEl.textContent=options.message;}
      if(actionButton){actionButton.className='btn btn-'+safeType;actionButton.querySelector('span')?.replaceChildren(options.text);}
      Array.from(modal.classList).forEach(className=>{if(className.indexOf('pp-confirm-modal-')===0){modal.classList.remove(className);}});
      modal.classList.add('pp-confirm-modal-'+safeType);
      modal.hidden=false;
      root.classList.add('is-active');
      document.body.classList.add('pp-modal-open');
      modal.focus();
    };

    document.querySelectorAll('[data-confirm="true"], [data-confirm-url], [data-confirm-action]').forEach(el=>{
      if(el===actionButton||el.dataset.ppConfirmBound==='1'){return;}
      el.dataset.ppConfirmBound='1';
      const eventName=el.tagName==='FORM'?'submit':'click';
      el.addEventListener(eventName,event=>{
        if(el.dataset.ppConfirmApproved==='1'){return;}
        const action=getConfirmOptions(el).url;
        if(eventName==='click'&&!action&&el.tagName!=='BUTTON'&&el.getAttribute('role')!=='button'){return;}
        openModal(el,event);
      });
    });
    root.querySelectorAll('[data-bs-dismiss="modal"], [data-pp-confirm-cancel]').forEach(el=>el.addEventListener('click',closeModal));
    document.addEventListener('keydown',event=>{if(event.key==='Escape'&&root.classList.contains('is-active')){closeModal();}});
    actionButton?.addEventListener('click',()=>{
      if(!pending){return;}
      const approved=pending;
      const formId=approved.getAttribute('data-confirm-form');
      const targetForm=formId?document.getElementById(formId):approved.tagName==='FORM'?approved:approved.closest('form');
      const action=getConfirmOptions(approved).url;
      if(targetForm&&!targetForm.reportValidity()){closeModal();pending=null;return;}
      modal.setAttribute('aria-busy','true');
      actionButton.disabled=true;
      loadingEl?.classList.remove('d-none');
      if(statusEl){statusEl.textContent='Processando ação. Aguarde.';}
      if(targetForm){targetForm.dataset.ppConfirmApproved='1';targetForm.requestSubmit();pending=null;return;}
      if(action){window.location.assign(action);return;}
      closeModal();
      pending=null;
    });
  }


  async function parseAjaxResponse(response){
    const text=await response.text();
    if(response.redirected&&response.url){
      return {success:response.ok,message:response.ok?'Operação concluída com sucesso.':'Não foi possível concluir a operação.',redirectUrl:response.url};
    }
    if(!text){return {success:response.ok,message:response.ok?'Operação concluída com sucesso.':'Não foi possível concluir a operação.'};}
    try{
      const json=JSON.parse(text);
      return {
        success:json.success!==undefined?Boolean(json.success):response.ok,
        message:json.message||json.mensagem||json.error||json.title||'',
        redirectUrl:json.redirectUrl||json.redirect||json.url||'',
        errors:json.errors||null
      };
    }catch(e){
      return {success:response.ok,message:response.ok?'Operação concluída com sucesso.':text};
    }
  }

  function renderAjaxErrors(form,errors,message){
    let box=form.querySelector('[data-ajax-errors]');
    if(!box){
      box=document.createElement('div');
      box.className='pp-error-panel d-none';
      box.setAttribute('data-ajax-errors','true');
      form.prepend(box);
    }

    const list=new Array();
    if(message){list.push(message);}
    if(Array.isArray(errors)){errors.forEach(error=>{if(error){list.push(String(error));}});}
    else if(errors&&typeof errors==='object'){
      Object.keys(errors).forEach(key=>{
        const value=errors[key];
        if(Array.isArray(value)){value.forEach(item=>{if(item){list.push(String(item));}});}
        else if(value){list.push(String(value));}
      });
    }

    box.replaceChildren(...list.map(error=>{const item=document.createElement('div');item.textContent=error;return item;}));
    box.classList.toggle('d-none',list.length===0);
  }

  function setFormBusy(form,isBusy){
    form.querySelectorAll('button[type="submit"], .btn-submit').forEach(btn=>{
      if(isBusy){
        if(!btn.dataset.originalLabel){btn.dataset.originalLabel=btn.textContent.trim();}
        btn.classList.add('is-loading');
        btn.setAttribute('aria-busy','true');
        btn.replaceChildren();const spinner=document.createElement('span');spinner.className='spinner-border spinner-border-sm me-2';spinner.setAttribute('aria-hidden','true');btn.append(spinner,document.createTextNode('Processando...'));
        btn.disabled=true;
      }else{
        if(btn.dataset.originalLabel){btn.textContent=btn.dataset.originalLabel;}
        btn.classList.remove('is-loading');
        btn.removeAttribute('aria-busy');
        btn.disabled=false;
      }
    });
  }

  function wireAjaxForms(){
    document.querySelectorAll('form[data-ajax-form="true"], form[data-saude360-form]').forEach(form=>{
      if(form.dataset.ppAjaxBound==='1'){return;}
      form.dataset.ppAjaxBound='1';
      form.addEventListener('submit',async(event)=>{
        if(form.dataset.confirm==='true'&&form.dataset.ppConfirmApproved!=='1'){return;}
        event.preventDefault();

        renderAjaxErrors(form,null,'');
        setFormBusy(form,true);
        try{
          const formData=new FormData(form);
          const antiForgeryToken=formData.get('__RequestVerificationToken');
          const headers={'X-Requested-With':'XMLHttpRequest'};
          if(antiForgeryToken){headers.RequestVerificationToken=antiForgeryToken;}
          const response=await fetch(form.action||window.location.href,{
            method:(form.method||'POST').toUpperCase(),
            body:formData,
            headers
          });
          const payload=await parseAjaxResponse(response);
          if(response.ok&&payload.success!==false){
            showToast('success',payload.message||'Operação concluída com sucesso.');
            if(payload.redirectUrl){window.location.assign(payload.redirectUrl);return;}
            form.dispatchEvent(new CustomEvent('plantaopro:ajax-success',{bubbles:true,detail:payload}));
            return;
          }

          const message=payload.message||'Revise os dados e tente novamente.';
          renderAjaxErrors(form,payload.errors,message);
          showToast('error',message);
          form.dispatchEvent(new CustomEvent('plantaopro:ajax-error',{bubbles:true,detail:payload}));
        }catch(error){
          const message='Falha de comunicação. Verifique sua conexão e tente novamente.';
          renderAjaxErrors(form,null,message);
          showToast('error',message);
        }finally{
          setFormBusy(form,false);
          delete form.dataset.ppConfirmApproved;
        }
      });
    });
  }

  function wireSubmitLoading(){
    document.querySelectorAll('form').forEach(form=>{
      if(form.matches('[data-ajax-form="true"], [data-saude360-form]')){return;}
      // Fluxos que controlam o próprio estado (como autenticação) não podem
      // disputar aria-busy/disabled com o carregamento global.
      if(form.dataset.submitLoading==='manual'){return;}
      if(form.dataset.ppLoadingBound==='1'){return;}
      form.dataset.ppLoadingBound='1';
      form.addEventListener('submit',()=>{
        if(form.dataset.confirm==='true'&&form.dataset.ppConfirmApproved!=='1'){return;}
        form.querySelectorAll('button[type="submit"], .btn-submit').forEach(btn=>{
          if(btn.dataset.confirm==='true'){return;}
          btn.classList.add('is-loading');
          btn.setAttribute('aria-busy','true');
          if(!btn.dataset.originalLabel){btn.dataset.originalLabel=btn.textContent.trim();}
          btn.replaceChildren();const spinner=document.createElement('span');spinner.className='spinner-border spinner-border-sm me-2';spinner.setAttribute('aria-hidden','true');btn.append(spinner,document.createTextNode('Processando...'));
          btn.disabled=true;
        });
      });
    });
  }

  function onlyDigits(value){return String(value||'').replace(/\D/g,'');}
  function applyMasks(){
    document.querySelectorAll('[data-mask]').forEach(input=>{if(input.dataset.ppMaskBound==='1'){return;}input.dataset.ppMaskBound='1';input.addEventListener('input',()=>{let v=onlyDigits(input.value);if(input.dataset.mask==='cpf'){v=v.slice(0,11);input.value=v.replace(/(\d{3})(\d)/,'$1.$2').replace(/(\d{3})(\d)/,'$1.$2').replace(/(\d{3})(\d{1,2})$/,'$1-$2');}else if(input.dataset.mask==='cnpj'){v=v.slice(0,14);input.value=v.replace(/(\d{2})(\d)/,'$1.$2').replace(/(\d{3})(\d)/,'$1.$2').replace(/(\d{3})(\d)/,'$1/$2').replace(/(\d{4})(\d{1,2})$/,'$1-$2');}else if(input.dataset.mask==='phone'){v=v.slice(0,11);input.value=v.length>10?v.replace(/(\d{2})(\d{5})(\d{1,4})/,'($1) $2-$3'):v.replace(/(\d{2})(\d{4})(\d{1,4})/,'($1) $2-$3');}else if(input.dataset.mask==='crmuf'){input.value=String(input.value||'').replace(/[^a-z0-9/ -]/gi,'').toUpperCase().slice(0,20);}});});
  }

  window.PlantaoProUi={copyText,refresh:()=>{initTooltips();wireConfirmActions();wireAjaxForms();wireSubmitLoading();applyMasks();}};
  document.addEventListener('DOMContentLoaded',()=>{wireNavigation();initTooltips();autoCloseAlerts();wireConfirmActions();wireAjaxForms();wireSubmitLoading();applyMasks();markActiveMenu();});
})();
