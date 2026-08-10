(function(){
  const hostId='plantaopro-toast-host';
  function host(){
    let el=document.getElementById(hostId);
    if(!el){
      el=document.createElement('div');
      el.id=hostId;
      el.className='plantaopro-toast-host';
      el.setAttribute('aria-live','polite');
      el.setAttribute('aria-atomic','true');
      document.body.appendChild(el);
    }
    return el;
  }
  const types=['success','error','warning','info','loading','update','delete'];
  const metadata={success:['✓','Concluído'],error:['!','Não foi possível concluir'],warning:['!','Atenção'],info:['i','Informação'],loading:['↻','Processando'],update:['↻','Atualização'],delete:['×','Item excluído']};
  function safeType(type){return types.includes(type)?type:'info';}
  function show(type,message,timeout,title){
    if(!message)return;
    type=safeType(type);
    const item=document.createElement('div');
    item.className=`plantaopro-toast plantaopro-toast-${type}`;
    item.setAttribute('role',type==='error'?'alert':'status');
    const icon=document.createElement('span');
    icon.className='plantaopro-toast-icon';icon.setAttribute('aria-hidden','true');icon.textContent=metadata[type][0];
    const body=document.createElement('div');
    body.className='plantaopro-toast-body';
    const heading=document.createElement('div');heading.className='plantaopro-toast-title';heading.textContent=title||metadata[type][1];
    const copy=document.createElement('div');copy.className='plantaopro-toast-message';copy.textContent=String(message);
    body.append(heading,copy);
    const close=document.createElement('button');
    close.type='button';
    close.className='btn-close';
    close.setAttribute('aria-label','Fechar');
    close.addEventListener('click',()=>item.remove());
    item.appendChild(icon);item.appendChild(body);
    item.appendChild(close);
    host().appendChild(item);
    setTimeout(()=>item.classList.add('show'),20);
    if(type!=='loading')setTimeout(()=>item.remove(),timeout||((type==='error'||type==='warning')?8000:5000));
    return {close:()=>item.remove(),element:item};
  }
  window.PlantaoProToast={show,success:(m,t,title)=>show('success',m,t,title),error:(m,t,title)=>show('error',m,t,title),warning:(m,t,title)=>show('warning',m,t,title),info:(m,t,title)=>show('info',m,t,title),loading:(m,title)=>show('loading',m,0,title),update:(m,t,title)=>show('update',m,t,title),delete:(m,t,title)=>show('delete',m,t,title)};
})();
