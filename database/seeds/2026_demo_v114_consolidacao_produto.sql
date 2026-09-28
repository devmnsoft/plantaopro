-- Shim legado: ON CONFLICT DO NOTHING sem alvo não desduplicava nessas tabelas
-- (único unique é o pkey de id uuid gerado). Agora usa not exists na chave
-- natural, mantendo o payload demo original.
insert into plantaopro.v114_checklist_implantacao(titulo,perfil_responsavel,status,ordem)
select v.titulo, v.perfil_responsavel, v.status, v.ordem
from (values
('Cadastrar unidade clínica','ADMIN_CLIENTE','PENDENTE',1),('Validar agenda do dia','RECEPCAO','PENDENTE',2),('Configurar itens faturáveis','FINANCEIRO','PENDENTE',3),('Publicar primeiro plantão','COORDENACAO','PENDENTE',4)) v(titulo,perfil_responsavel,status,ordem)
where not exists (select 1 from plantaopro.v114_checklist_implantacao c where c.titulo = v.titulo and c.perfil_responsavel = v.perfil_responsavel and c.ordem = v.ordem);

insert into plantaopro.v114_timelines(entidade,evento,resumo,perfil)
select t.entidade, t.evento, t.resumo, t.perfil
from (values
('ATENDIMENTO','CONSULTA_FINALIZADA_SEM_FATURAMENTO','Pendência operacional para gerar conta a receber.','FINANCEIRO'),('PLANTAO','REPASSE_PENDENTE','Plantão realizado aguardando repasse médico.','FINANCEIRO')) t(entidade,evento,resumo,perfil)
where not exists (select 1 from plantaopro.v114_timelines x where x.entidade = t.entidade and x.evento = t.evento);
