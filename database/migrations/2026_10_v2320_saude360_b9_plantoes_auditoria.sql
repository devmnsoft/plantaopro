-- ============================================================================
-- v2320 | B9 Saude 360 | plantoes com colunas de auditoria usadas pelo codigo
-- ----------------------------------------------------------------------------
-- Problema: PlantaoService.ChangeStatusAsync (L1223), a edicao de plantao
-- (L1135), a deteccao de conflito (L1385) e o aceite de convite operacional
-- (L1487) executam update em plantaopro.plantoes usando reg_update,
-- updated_by e conflito_detectado, mas a tabela criada no schema base
-- (030_operacao_plantoes.sql) termina as colunas em reg_status/reg_date/
-- created_by — nenhuma migracao anterior adicionou essas tres colunas.
-- Resultado: qualquer transicao de status de plantao (publicar/cancelar/
-- realizar/encerrar), edicao, deteccao de conflito ou aceite de convite
-- falha com "column reg_update does not exist" em bases instaladas pelo
-- object-catalog — mesma classe da defasacao corrigida em v2318 para
-- plantao_historico.
-- Decisao: acrescentar as colunas faltantes de forma idempotente e preservar
-- as colunas genericas por compatibilidade (padrao de 345_v2069_reparar_base_
-- notificacoes / v2318). Dados existentes permanecem intactos; linhas ja
-- existentes recebem updated_by null e reg_update null, o que e consistente
-- com o restante do esquema (colunas de auditoria nullable).
-- ============================================================================
set search_path to plantaopro, public;

alter table plantaopro.plantoes add column if not exists updated_by uuid;
alter table plantaopro.plantoes add column if not exists reg_update timestamptz;
alter table plantaopro.plantoes add column if not exists conflito_detectado boolean not null default false;

comment on column plantaopro.plantoes.reg_update is
    'Data/hora da ultima alteracao do plantao (PlantaoService).';
comment on column plantaopro.plantoes.updated_by is
    'Usuario que fez a ultima alteracao do plantao.';
comment on column plantaopro.plantoes.conflito_detectado is
    'Flag de conflito operacional detectada no aceite de escala (PlantaoService).';
