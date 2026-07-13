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
  function safeType(type){return Array('success','error','warning','info').includes(type)?type:'info';}
  function show(type,message,timeout){
    if(!message)return;
    const item=document.createElement('div');
    item.className=`plantaopro-toast plantaopro-toast-${safeType(type)}`;
    item.setAttribute('role','status');
    const body=document.createElement('div');
    body.className='plantaopro-toast-body';
    body.textContent=String(message);
    const close=document.createElement('button');
    close.type='button';
    close.className='btn-close';
    close.setAttribute('aria-label','Fechar');
    close.addEventListener('click',()=>item.remove());
    item.appendChild(body);
    item.appendChild(close);
    host().appendChild(item);
    setTimeout(()=>item.classList.add('show'),20);
    setTimeout(()=>item.remove(),timeout||5000);
  }
  window.PlantaoProToast={success:(m,t)=>show('success',m,t),error:(m,t)=>show('error',m,t),warning:(m,t)=>show('warning',m,t),info:(m,t)=>show('info',m,t)};
})();
