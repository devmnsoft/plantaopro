-- PlantãoPro v2.35.0 (R8-B7): glosa por item de lote + recurso com documentos (matriz §22).
-- Antes: a glosa v115 era registrada apenas por conta/título — não havia referência ao item do
-- lote (v116_faturamento_lote_itens), o recurso não tinha trilha própria (nem justificativa nem
-- documentos persistidos) e a coluna v113_faturas.glosa_reconhecida era lida na fórmula do saldo
-- sem writer (a glosa não afetava o saldo). Esta migração torna o ciclo
-- lote -> item -> glosa -> recurso -> resolucao reconciliável no saldo da conta.
-- Idempotente: add column if not exists + create table/index if not exists.

alter table plantaopro.v115_regras_glosa add column if not exists lote_item_id uuid null;
alter table plantaopro.v115_regras_glosa add column if not exists recurso_justificativa text null;
alter table plantaopro.v115_regras_glosa add column if not exists recurso_em timestamptz null;

create index if not exists ix_v115_glosa_lote_item on plantaopro.v115_regras_glosa (lote_item_id) where reg_status = 'A';

alter table plantaopro.v116_faturamento_lote_itens add column if not exists lote_id uuid null;

create index if not exists ix_v116_lote_itens_tenant_lote on plantaopro.v116_faturamento_lote_itens (tenant_id, lote_id);

create table if not exists plantaopro.v115_recurso_documentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    glosa_id uuid null,
    nome_arquivo text not null default '',
    tipo_mime text null,
    conteudo text null,
    tamanho int not null default 0,
    created_at timestamptz not null default now(),
    created_by uuid null,
    reg_status char(1) not null default 'A'
);

create index if not exists ix_v115_recurso_documentos_glosa on plantaopro.v115_recurso_documentos (glosa_id) where reg_status = 'A';
