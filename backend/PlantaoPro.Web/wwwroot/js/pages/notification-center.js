const root = document.querySelector('[data-notification-center], [data-notification-preferences]');
if (root) {
  const api = root.dataset.api;
  const request = async (path, options = {}) => {
    const response = await fetch(api + path, { credentials: 'same-origin', ...options, headers: { 'Content-Type': 'application/json', ...(options.headers || {}) } });
    if (!response.ok) throw new Error(response.status === 403 ? 'Seu perfil não possui acesso a esta ação.' : 'A central está temporariamente indisponível. Tente novamente.');
    const payload = await response.json(); return payload?.data ?? payload;
  };
  const safeUrl = value => { if (!value) return null; try { const url = new URL(value, location.origin); return url.origin === location.origin ? `${url.pathname}${url.search}` : null; } catch { return null; } };
  const state = (title, copy, retry) => { const box=root.querySelector('[data-state]'); if (!box) return; box.hidden=false; box.replaceChildren(); const heading=document.createElement('strong'); heading.textContent=title; const text=document.createElement('small'); text.textContent=copy; box.append(heading,text); if(retry){const button=document.createElement('button');button.type='button';button.className='button button-subtle';button.textContent='Tentar novamente';button.addEventListener('click',load);box.append(button);} };
  const action = async (id, operation, button) => { button.disabled=true; try { await request(`/${encodeURIComponent(id)}/${operation}`,{method:'POST'}); await load(); } catch(error) { button.disabled=false; state('Não foi possível atualizar',error.message,true); } };
  const render = items => {
    const list=root.querySelector('[data-list]'); const box=root.querySelector('[data-state]'); list.replaceChildren();
    if(!items.length){list.hidden=true;state('Tudo em dia','Nenhuma notificação corresponde aos filtros escolhidos.');return;}
    box.hidden=true; list.hidden=false;
    items.forEach(item=>{const card=document.createElement('article');card.className=`notification-card priority-${(item.prioridade||'baixa').toLowerCase()}`;
      const meta=document.createElement('div');meta.className='notification-card__meta'; const priority=document.createElement('span');priority.className='priority-badge';priority.textContent=item.prioridade||'BAIXA';const status=document.createElement('span');status.className='status-badge';status.textContent=(item.status||'NAO_LIDA').replace('_',' ');const date=document.createElement('time');date.dateTime=item.criadaEm;date.textContent=new Intl.DateTimeFormat('pt-BR',{dateStyle:'short',timeStyle:'short'}).format(new Date(item.criadaEm));meta.append(priority,status,date);
      const title=document.createElement('h3');title.textContent=item.titulo;const message=document.createElement('p');message.textContent=item.mensagem;const actions=document.createElement('div');actions.className='notification-card__actions';
      if(!item.lida){const read=document.createElement('button');read.type='button';read.className='button button-subtle';read.textContent='Marcar como lida';read.addEventListener('click',()=>action(item.id,'lida',read));actions.append(read);}
      [['arquivar','Arquivar'],['resolver','Resolver']].forEach(([key,label])=>{const button=document.createElement('button');button.type='button';button.className='button button-subtle';button.textContent=label;button.addEventListener('click',()=>action(item.id,key,button));actions.append(button);});
      const destination=safeUrl(item.destinoUrl);if(destination){const link=document.createElement('a');link.className='button button-primary';link.href=destination;link.textContent='Abrir contexto';actions.append(link);} card.append(meta,title,message,actions);list.append(card);});
  };
  async function load(){const form=root.querySelector('[data-filters]');if(!form)return;state('Carregando notificações','Organizando as prioridades do seu turno…');try{const query=new URLSearchParams(new FormData(form));[...query].forEach(([k,v])=>{if(!v)query.delete(k);});render(await request(`?${query}`));}catch(error){state('Notificações indisponíveis',error.message,true);}}
  root.querySelector('[data-filters]')?.addEventListener('submit',event=>{event.preventDefault();load();});
  root.querySelector('[data-read-all]')?.addEventListener('click',async event=>{event.currentTarget.disabled=true;try{await request('/marcar-todas-lidas',{method:'POST'});await load();}finally{event.currentTarget.disabled=false;}});
  root.querySelector('[data-preference-form]')?.addEventListener('submit',async event=>{event.preventDefault();const preferences=[...root.querySelectorAll('.preference-card')].map(card=>({categoria:card.dataset.category,tipoEvento:card.dataset.event,inApp:true,email:false,push:false,whatsapp:false,ativo:card.querySelector('[data-active]').checked}));const message=root.querySelector('[data-preference-message]');try{await request('/preferencias',{method:'PUT',body:JSON.stringify({preferences})});message.textContent='Preferências salvas com segurança.';}catch(error){message.textContent=error.message;}});
  load();
}
