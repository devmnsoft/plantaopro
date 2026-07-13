-- Índices de performance para homologação premium Saúde 360.
create index if not exists ix_saude360_pacientes_cliente_nome on plantaopro.pacientes (cliente_id, nome_completo);
create index if not exists ix_saude360_pacientes_cliente_cpf on plantaopro.pacientes (cliente_id, cpf);
create index if not exists ix_saude360_agendamentos_cliente_data_status on plantaopro.agendamentos (cliente_id, data_inicio, status);
create index if not exists ix_saude360_triagens_cliente_status_risco on plantaopro.triagens (cliente_id, status, classificacao_risco);
create index if not exists ix_saude360_consultas_cliente_status_data on plantaopro.consultas (cliente_id, status, data_inicio);
create index if not exists ix_saude360_cid_codigo on plantaopro.cid_tabela (codigo);
create index if not exists ix_saude360_cid_descricao on plantaopro.cid_tabela (descricao);
create index if not exists ix_saude360_contas_receber_cliente_status_vencimento on plantaopro.clinica_contas_receber (cliente_id, status, vencimento);
create index if not exists ix_saude360_convenios_cliente_status on plantaopro.convenios (cliente_id, status);
create index if not exists ix_saude360_planos_saude_cliente_status on plantaopro.planos_saude (cliente_id, status);
