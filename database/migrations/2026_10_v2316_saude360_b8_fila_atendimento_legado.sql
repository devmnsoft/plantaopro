-- ============================================================================
-- v2316 | B8 Saúde 360 | Tabela legada fila_atendimento (DDL ausente no repo)
-- ----------------------------------------------------------------------------
-- Root cause (R4-B8): FinalizarAsync (ConsultaApplicationService.cs, linha do
-- UPDATE em plantaopro.fila_atendimento) foi introduzido no commit 5406e514
-- (prontuário médico e faturamento v1.27.0) e faz parte da transação de
-- finalização de consulta, mas NENHUMA DDL do repositório cria a tabela.
-- Sem ela, a transação de finalização falha com 42P01 (relation does not
-- exist) em todo ambiente onde ela nunca existiu manualmente — incluindo os
-- bancos locais de homologação (plantaopro/plantaopro_test), o que impedia a
-- jornada consulta -> conta -> recebimento -> caixa exigida pelo B8.
--
-- A documentação de auditoria (mapa-saude360.md, rodada 3) lista
-- fila_atendimento entre as tabelas principais do módulo; esta migração
-- materializa esse contrato de forma idempotente (IF NOT EXISTS), preservando
-- o código existente sem módulos paralelas. O UPDATE da finalização só marca
-- status='FINALIZADO' onde já houver linhas; nenhum escritor insere nesta
-- versão (projeção legada de fila), então a criação é suficiente para a
-- jornada operar.
-- ============================================================================
set search_path to plantaopro, public;

create table if not exists plantaopro.fila_atendimento(
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid not null,
    atendimento_id uuid not null,
    status text not null default 'AGUARDANDO',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);

-- Índice de apoio ao UPDATE da finalização (predicado cliente + atendimento):
-- nome sem qualificação de schema (CREATE INDEX não admite; o search_path
-- acima posiciona em plantaopro). Tabela continua qualificada.
create index if not exists ix_fila_atendimento_cliente_atendimento
    on plantaopro.fila_atendimento(cliente_id, atendimento_id);
