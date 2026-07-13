insert into plantaopro.v114_checklist_implantacao(titulo,perfil_responsavel,status,ordem)
values ('Cadastrar unidade clínica','ADMIN_CLIENTE','PENDENTE',1),('Validar agenda do dia','RECEPCAO','PENDENTE',2),('Configurar itens faturáveis','FINANCEIRO','PENDENTE',3),('Publicar primeiro plantão','COORDENACAO','PENDENTE',4)
on conflict do nothing;
insert into plantaopro.v114_timelines(entidade,evento,resumo,perfil)
values ('ATENDIMENTO','CONSULTA_FINALIZADA_SEM_FATURAMENTO','Pendência operacional para gerar conta a receber.','FINANCEIRO'),('PLANTAO','REPASSE_PENDENTE','Plantão realizado aguardando repasse médico.','FINANCEIRO')
on conflict do nothing;
