create schema if not exists plantaopro;
insert into plantaopro.v113_clientes(id,cliente_id,tenant_id,nome,documento,email,status,reg_status,created_at)
values ('11111111-1111-4111-8111-111111111113','00000000-0000-4000-8000-000000000113','00000000-0000-4000-8000-000000000113','Cliente Demo v1.13','DEMO-001','cliente.demo.v113@example.invalid','ACTIVE','A',now()) on conflict (id) do nothing;
insert into plantaopro.v113_produtos(id,cliente_id,tenant_id,codigo,nome,preco,estoque_minimo,status,reg_status,created_at)
values ('22222222-2222-4222-8222-222222222113','00000000-0000-4000-8000-000000000113','00000000-0000-4000-8000-000000000113','DEMO-PROD-113','Produto Demo v1.13',99.90,5,'ACTIVE','A',now()) on conflict (id) do nothing;
insert into plantaopro.v113_estoque_movimentos(id,cliente_id,tenant_id,produto_id,quantidade,tipo,observacao,status,reg_status,created_at)
values ('33333333-3333-4333-8333-333333333113','00000000-0000-4000-8000-000000000113','00000000-0000-4000-8000-000000000113','22222222-2222-4222-8222-222222222113',20,'ENTRY','Estoque inicial demo v1.13','ACTIVE','A',now()) on conflict (id) do nothing;
insert into plantaopro.v113_pedidos(id,cliente_id,tenant_id,cliente_operacional_id,status,total,reg_status,created_at)
values ('44444444-4444-4444-8444-444444444113','00000000-0000-4000-8000-000000000113','00000000-0000-4000-8000-000000000113','11111111-1111-4111-8111-111111111113','CONFIRMED',199.80,'A',now()) on conflict (id) do nothing;
insert into plantaopro.v113_pedido_itens(id,cliente_id,tenant_id,pedido_id,produto_id,quantidade,valor_unitario,status,reg_status,created_at)
values ('45454545-4545-4545-8545-454545454113','00000000-0000-4000-8000-000000000113','00000000-0000-4000-8000-000000000113','44444444-4444-4444-8444-444444444113','22222222-2222-4222-8222-222222222113',2,99.90,'ACTIVE','A',now()) on conflict (id) do nothing;
insert into plantaopro.v113_tarefas(id,cliente_id,tenant_id,pedido_id,titulo,status,responsavel,comentarios,reg_status,created_at)
values ('55555555-5555-4555-8555-555555555113','00000000-0000-4000-8000-000000000113','00000000-0000-4000-8000-000000000113','44444444-4444-4444-8444-444444444113','Separar pedido demo v1.13','PENDING','homologador','["Comentário demo seguro"]'::jsonb,'A',now()) on conflict (id) do nothing;
insert into plantaopro.v113_faturas(id,cliente_id,tenant_id,pedido_id,valor,status,reg_status,created_at)
values ('66666666-6666-4666-8666-666666666113','00000000-0000-4000-8000-000000000113','00000000-0000-4000-8000-000000000113','44444444-4444-4444-8444-444444444113',199.80,'ISSUED','A',now()) on conflict (id) do nothing;
insert into plantaopro.v113_titulos(id,cliente_id,tenant_id,fatura_id,valor,status,demo_boleto,vencimento,reg_status,created_at)
values ('77777777-7777-4777-8777-777777777113','00000000-0000-4000-8000-000000000113','00000000-0000-4000-8000-000000000113','66666666-6666-4666-8666-666666666113',199.80,'DEMO_BOLETO',true,now()+interval '7 days','A',now()) on conflict (id) do nothing;
insert into plantaopro.v113_templates(id,codigo,nome,descricao,status,reg_status,created_at) values
('88888888-8888-4888-8888-888888888113','pedido-faturamento','Pedido ao Faturamento v1.13','Template demo seguro para homologação.','ACTIVE','A',now()),
('88888888-8888-4888-8888-888888888114','separacao-pedido','Separação de Pedido v1.13','Template demo seguro para operação.','ACTIVE','A',now()) on conflict (id) do nothing;
insert into plantaopro.v113_template_instalacoes(id,cliente_id,tenant_id,template_id,status,reg_status,created_at) values ('89898989-8989-4898-8898-898989898113','00000000-0000-4000-8000-000000000113','00000000-0000-4000-8000-000000000113','88888888-8888-4888-8888-888888888113','INSTALLED','A',now()) on conflict (id) do nothing;
insert into plantaopro.v113_jornada_acoes(id,cliente_id,tenant_id,codigo,status,detalhe,open_url,ordem,reg_status,created_at) values
('99999999-9999-4999-8999-999999999113','00000000-0000-4000-8000-000000000113','00000000-0000-4000-8000-000000000113','database','READY','Migration e seed v1.13 aplicáveis.','/homologation',1,'A',now()),
('99999999-9999-4999-8999-999999999114','00000000-0000-4000-8000-000000000113','00000000-0000-4000-8000-000000000113','api-v113','READY','Endpoints v1.13 protegidos e envelopados.','/dashboard',2,'A',now()) on conflict (id) do nothing;
insert into plantaopro.v113_atividades(id,cliente_id,tenant_id,tipo,descricao,status,reg_status,created_at) values ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaa113','00000000-0000-4000-8000-000000000113','00000000-0000-4000-8000-000000000113','Dashboard','Dados reais PostgreSQL para homologação v1.13.','FUNCTIONAL','A',now()) on conflict (id) do nothing;
insert into plantaopro.v113_outbox_eventos(id,cliente_id,tenant_id,tipo,payload_ref,payload,status,reg_status,created_at) values
('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbb113','00000000-0000-4000-8000-000000000113','00000000-0000-4000-8000-000000000113','DEMO_BOLETO_CREATED','77777777-7777-4777-8777-777777777113','{"demo":true}'::jsonb,'PENDING','A',now()),
('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbb114','00000000-0000-4000-8000-000000000113','00000000-0000-4000-8000-000000000113','ORDER_CONFIRMED','44444444-4444-4444-8444-444444444113','{"demo":true}'::jsonb,'PROCESSED','A',now()) on conflict (id) do nothing;
insert into plantaopro.v113_outbox_logs(id,cliente_id,tenant_id,outbox_evento_id,status,detalhe,reg_status,created_at) values ('cccccccc-cccc-4ccc-8ccc-ccccccccc113','00000000-0000-4000-8000-000000000113','00000000-0000-4000-8000-000000000113','bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbb114','PROCESSED','Log demo sem envio externo de produção.','A',now()) on conflict (id) do nothing;
