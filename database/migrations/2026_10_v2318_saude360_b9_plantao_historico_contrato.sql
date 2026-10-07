-- ============================================================================
-- v2318 | B9 Saude 360 | plantao_historico alinhado ao contrato de codigo
-- ----------------------------------------------------------------------------
-- Problema: PlantaoHistoricoService.RegistrarAsync e PlantaoService.
-- ListarHistoricoAsync leem/gravam as colunas plantao_id, status_anterior,
-- status_novo, justificativa, usuario_id, reg_date e reg_status, mas a tabela
-- criada no schema base (030_operacao_plantoes.sql) conserva o layout generico
-- (id/tenant_id/codigo/nome/status/dados/criado_em/atualizado_em) — nenhuma
-- migracao anterior adicionou as colunas usadas pelo codigo. Resultado: toda
-- mudanca de status de plantao (publicar/cancelar/realizar/encerrar) falhava
-- com "column ... does not exist" em ambos os bancos (plantaopro e
-- plantaopro_test), tornando o estado terminal 'encerrado' inalcançavel pela
-- API — requisito central do bloco B9.
-- Decisao: acrescentar as colunas faltantes (idempotente; as colunas
-- genericas permanecem por compatibilidade). Nenhum dado historico dependia
-- do layout antigo (a tabela esta vazia em ambos os bancos nesta data).
-- Nota: plantaopro.historico_escala apresenta a mesma defasacao e segue em
-- backlog (fora da jornada critica B9: nenhuma acao testada aqui grava nela).
-- ============================================================================
set search_path to plantaopro, public;

alter table plantaopro.plantao_historico add column if not exists plantao_id uuid;
alter table plantaopro.plantao_historico add column if not exists status_anterior varchar(32);
alter table plantaopro.plantao_historico add column if not exists status_novo varchar(32);
alter table plantaopro.plantao_historico add column if not exists justificativa text;
alter table plantaopro.plantao_historico add column if not exists usuario_id uuid;
alter table plantaopro.plantao_historico add column if not exists reg_date timestamptz;
alter table plantaopro.plantao_historico add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_plantao_historico_plantao_regdate
    on plantaopro.plantao_historico (plantao_id, reg_date);

comment on column plantaopro.plantao_historico.plantao_id is
    'Plantao cuja mudanca de status esta sendo registrada (PlantaoHistoricoService).';
comment on column plantaopro.plantao_historico.status_anterior is
    'Status do plantao imediatamente antes da transicao.';
comment on column plantaopro.plantao_historico.status_novo is
    'Status do plantao apos a transicao.';
