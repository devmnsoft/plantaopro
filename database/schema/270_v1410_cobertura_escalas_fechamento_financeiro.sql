-- PlantãoPro v1.41.0 — cobertura, execução, fechamento e origem financeira.
-- Modelo aditivo e idempotente; todas as entidades operacionais carregam o tenant.
set search_path to plantaopro, public;

create table if not exists cobertura_sugestoes (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, plantao_id uuid not null,
    medico_id uuid not null, score smallint not null check (score between 0 and 100),
    criterios jsonb not null default '{}'::jsonb, elegivel boolean not null,
    impedimentos jsonb not null default '[]'::jsonb, calculado_em timestamptz not null default now(),
    unique (tenant_id, plantao_id, medico_id)
);
create index if not exists ix_cobertura_sugestoes_ranking on cobertura_sugestoes(tenant_id, plantao_id, elegivel, score desc);

create table if not exists cobertura_convites (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, plantao_id uuid not null,
    medico_id uuid not null, status varchar(20) not null default 'PENDENTE'
        check (status in ('PENDENTE','ACEITO','RECUSADO','CANCELADO','EXPIRADO')),
    mensagem text, criado_por uuid not null, criado_em timestamptz not null default now(),
    reenviado_em timestamptz, respondido_em timestamptz, cancelado_em timestamptz, motivo text
);
create unique index if not exists ux_cobertura_convite_pendente
    on cobertura_convites(tenant_id, plantao_id, medico_id) where status = 'PENDENTE';

create table if not exists escala_transicoes (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, escala_id uuid not null,
    estado_anterior varchar(24), estado_novo varchar(24) not null
        check (estado_novo in ('SOLICITADA','CONFIRMADA','RECUSADA','CANCELADA','SUBSTITUIDA','REALIZADA','AUSENTE','EM_FECHAMENTO','FECHADA')),
    motivo text, novo_medico_id uuid, executado_por uuid not null, executado_em timestamptz not null default now(), metadata jsonb not null default '{}'::jsonb
);
create index if not exists ix_escala_transicoes_timeline on escala_transicoes(tenant_id, escala_id, executado_em desc);

create table if not exists fechamento_plantao (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, plantao_id uuid not null,
    status varchar(24) not null default 'EM_CONFERENCIA' check (status in ('EM_CONFERENCIA','COM_DIVERGENCIA','APROVADO','FECHADO','REABERTO')),
    iniciado_por uuid not null, iniciado_em timestamptz not null default now(), aprovado_por uuid, aprovado_em timestamptz,
    fechado_em timestamptz, reaberto_em timestamptz, motivo_reabertura text, versao integer not null default 1
);
create unique index if not exists ux_fechamento_plantao_ativo on fechamento_plantao(tenant_id, plantao_id) where status <> 'REABERTO';

create table if not exists fechamento_plantao_escalas (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, fechamento_id uuid not null references fechamento_plantao(id),
    escala_id uuid not null, presenca boolean not null, horas_previstas numeric(6,2) not null default 0,
    horas_realizadas numeric(6,2) not null default 0 check (horas_realizadas >= 0), valor_previsto numeric(14,2) not null default 0,
    valor_calculado numeric(14,2) not null default 0, conferido_por uuid, conferido_em timestamptz, unique(tenant_id, fechamento_id, escala_id)
);
create table if not exists fechamento_divergencias (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, fechamento_id uuid not null references fechamento_plantao(id),
    escala_id uuid, tipo varchar(40) not null, descricao text not null check (length(trim(descricao)) >= 3),
    status varchar(20) not null default 'ABERTA' check (status in ('ABERTA','RESOLVIDA','CANCELADA')),
    criada_por uuid not null, criada_em timestamptz not null default now(), resolucao text, resolvida_por uuid, resolvida_em timestamptz
);
create table if not exists fechamento_aprovacoes (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, fechamento_id uuid not null references fechamento_plantao(id),
    aprovado_por uuid not null, decisao varchar(16) not null check (decisao in ('APROVADO','REJEITADO','REABERTO')),
    justificativa text, criado_em timestamptz not null default now()
);
create table if not exists financeiro_pagamento_origem (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, pagamento_id uuid not null,
    fechamento_id uuid not null references fechamento_plantao(id), escala_id uuid not null,
    criado_em timestamptz not null default now(), unique(tenant_id, pagamento_id), unique(tenant_id, escala_id)
);
create table if not exists work_item_contextos (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, work_item_id uuid not null,
    tipo varchar(40) not null, entidade_id uuid not null, rota_segura text not null, dados jsonb not null default '{}'::jsonb,
    criado_em timestamptz not null default now(), unique(tenant_id, work_item_id, tipo, entidade_id)
);

