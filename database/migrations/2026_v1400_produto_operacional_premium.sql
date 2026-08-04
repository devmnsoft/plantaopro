-- PlantãoPro v1.40.0 — trilha operacional, cobertura e fechamento.
-- Estruturas aditivas, idempotentes e isoladas por tenant.

alter table if exists plantaopro.saved_views
    add column if not exists filtros jsonb not null default '{}'::jsonb;
alter table if exists plantaopro.saved_views
    add column if not exists visualizacao varchar(24) not null default 'TABELA';

create table if not exists plantaopro.operational_action_history (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    usuario_id uuid,
    entidade varchar(40) not null,
    entidade_id uuid not null,
    acao varchar(60) not null,
    status_anterior varchar(40),
    status_novo varchar(40),
    motivo text,
    comentario text,
    metadata jsonb not null default '{}'::jsonb,
    ocorrido_em timestamptz not null default now()
);
create index if not exists ix_operational_action_history_entity
    on plantaopro.operational_action_history(tenant_id, entidade, entidade_id, ocorrido_em desc);

create table if not exists plantaopro.cobertura_auditoria (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    plantao_id uuid not null,
    medico_id uuid,
    convite_id uuid,
    usuario_id uuid,
    acao varchar(60) not null,
    motivo text,
    criterios_ranking jsonb not null default '{}'::jsonb,
    ocorrido_em timestamptz not null default now()
);
create index if not exists ix_cobertura_auditoria_plantao
    on plantaopro.cobertura_auditoria(tenant_id, plantao_id, ocorrido_em desc);

create table if not exists plantaopro.fechamento_auditoria (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    plantao_id uuid not null,
    escala_id uuid,
    pagamento_id uuid,
    usuario_id uuid,
    acao varchar(60) not null,
    valor_anterior numeric(14,2),
    valor_novo numeric(14,2),
    justificativa text,
    metadata jsonb not null default '{}'::jsonb,
    ocorrido_em timestamptz not null default now()
);
create index if not exists ix_fechamento_auditoria_plantao
    on plantaopro.fechamento_auditoria(tenant_id, plantao_id, ocorrido_em desc);
