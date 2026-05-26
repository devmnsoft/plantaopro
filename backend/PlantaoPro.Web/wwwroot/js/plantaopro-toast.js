(function(){
  const hostId='plantaopro-toast-host';
  function host(){let el=document.getElementById(hostId);if(!el){el=document.createElement('div');el.id=hostId;el.className='plantaopro-toast-host';document.body.appendChild(el);}return el;}
  function show(type,message,timeout){if(!message)return;const item=document.createElement('div');item.className=`plantaopro-toast plantaopro-toast-${type}`;item.innerHTML=`<div class="plantaopro-toast-body">${message}</div><button type="button" class="btn-close" aria-label="Fechar"></button>`;item.querySelector('button').addEventListener('click',()=>item.remove());host().appendChild(item);setTimeout(()=>item.classList.add('show'),20);setTimeout(()=>item.remove(),timeout||5000)}
  window.PlantaoProToast={success:(m,t)=>show('success',m,t),error:(m,t)=>show('error',m,t),warning:(m,t)=>show('warning',m,t),info:(m,t)=>show('info',m,t)};
})();
