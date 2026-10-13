-- PlantãoPro v2.35.1 (R8-B10): unicidade canônica VERSIONADA de cid_tabela (§29 item 2 — drift de schema).
-- Antecedente: cid_tabela é um vocabulário de CID VERSIONADO (CID-10/CID-11). O importador
-- (Saude360ClinicalService, upsert ~L580) grava via ON CONFLICT (versao, upper(codigo)) WHERE reg_status='A'
-- AND codigo<>'', ou seja, o MESMO código pode existir em várias versões ao mesmo tempo.
-- Drift: 6 DDLs criaram cid_tabela com formas divergentes; sobraram TRÊS índices únicos sobre codigo:
--   ux_cid_tabela_codigo              UNIQUE (codigo)                                                    -> bloq. mesmo código em 2 versões
--   ux_cid_tabela_codigo_ativo        UNIQUE (upper(codigo)) WHERE reg_status='A' AND codigo<>''         -> idem (só ativos)
--   ux_cid_tabela_versao_codigo_ativo UNIQUE (versao, upper(codigo)) WHERE reg_status='A' AND codigo<>'' -> CANÔNICO (alvo do upsert)
-- Os dois primeiros contradizem o modelo versionado: importar o mesmo código sob duas versões falhava
-- com 23505 no índice plano (bug latente — o import nunca cruzou versões nos testes até aqui).
-- Correção (relaxação monotônica, sem perda de dados; 0 duplicidades existentes no base): remove as duas
-- unicidades NÃO-versionadas e reafirma a versionada (canônica). Idempotente. Sem alteração de colunas —
-- o superset já converge via alter-add idempotente dos DDLs anteriores na ordem de aplicação; a
-- deduplicação dos 6 blocos CREATE segue o rito §29 (nenhuma deleção de tabela/DDL nesta rodada).
-- Robustez: em alguns ambientes cada unicidade é um índice puro (CREATE UNIQUE INDEX); em outros,
-- uma RESTRIÇÃO de tabela (ADD CONSTRAINT ... UNIQUE) — e DROP INDEX não remove índice que sustenta
-- restrição. Por isso, para cada nome: tenta DROP CONSTRAINT (se for restrição) e depois DROP INDEX
-- (se for índice puro, ou vira no-op após a restrição). Idempotente nos dois sentidos.
alter table if exists plantaopro.cid_tabela drop constraint if exists ux_cid_tabela_codigo;
drop index if exists plantaopro.ux_cid_tabela_codigo;
alter table if exists plantaopro.cid_tabela drop constraint if exists ux_cid_tabela_codigo_ativo;
drop index if exists plantaopro.ux_cid_tabela_codigo_ativo;
-- Reafirma a unicidade versionada (canônica; é o alvo do ON CONFLICT do importador).
-- OBS: o NOME do índice não leva o esquema (Postgres: CREATE INDEX [nome] ON esquema.tabela).
create unique index if not exists ux_cid_tabela_versao_codigo_ativo
    on plantaopro.cid_tabela (versao, upper(codigo)) where reg_status = 'A' and codigo <> '';
