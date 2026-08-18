-- PlantaoPro v1.87.0 - fechamento operacional e contestacao financeira reais.
-- Evolui as estruturas de v1.41 sem criar um dominio concorrente.
set search_path to plantaopro, public;

alter table plantaopro.fechamento_plantao
    add column if not exists cliente_id uuid,
    add column if not exists unidade_id uuid,
    add column if not exists hospital_id uuid,
    add column if not exists data_referencia date,
    add column if not exists valor_previsto numeric(14,2) not null default 0,
    add column if not exists valor_apurado numeric(14,2) not null default 0,
    add column if not exists horas_previstas numeric(8,2) not null default 0,
    add column if not exists horas_realizadas numeric(8,2) not null default 0,
    add column if not exists conferido_por uuid,
    add column if not exists conferido_em timestamptz,
    add column if not exists devolvido_por uuid,
    add column if not exists devolvido_em timestamptz,
    add column if not exists motivo_devolucao varchar(500),
    add column if not exists financeiro_gerado_por uuid,
    add column if not exists financeiro_gerado_em timestamptz,
    add column if not exists concluido_em timestamptz,
    add column if not exists atualizado_por uuid,
    add column if not exists atualizado_em timestamptz not null default now();

alter table plantaopro.fechamento_plantao drop constraint if exists fechamento_plantao_status_check;
alter table plantaopro.fechamento_plantao add constraint fechamento_plantao_status_check
 check (status in ('ABERTO','EM_CONFERENCIA','COM_DIVERGENCIA','AGUARDANDO_APROVACAO','APROVADO','DEVOLVIDO','FINANCEIRO_GERADO','CONCLUIDO','CANCELADO','FECHADO','REABERTO'));

alter table plantaopro.fechamento_plantao_escalas
    add column if not exists medico_id uuid,
    add column if not exists plantao_id uuid,
    add column if not exists status_escala varchar(24),
    add column if not exists inicio_previsto timestamptz,
    add column if not exists fim_previsto timestamptz,
    add column if not exists inicio_realizado timestamptz,
    add column if not exists fim_realizado timestamptz,
    add column if not exists possui_divergencia boolean not null default false,
    add column if not exists observacao varchar(500),
    add column if not exists criado_em timestamptz not null default now(),
    add column if not exists atualizado_em timestamptz not null default now();

alter table plantaopro.fechamento_divergencias
    add column if not exists fechamento_item_id uuid,
    add column if not exists valor_anterior numeric(14,2),
    add column if not exists valor_proposto numeric(14,2),
    add column if not exists motivo varchar(500);

create table if not exists plantaopro.fechamento_historico (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, cliente_id uuid not null,
    fechamento_id uuid not null references plantaopro.fechamento_plantao(id), evento varchar(50) not null,
    status_anterior varchar(24), status_novo varchar(24), descricao varchar(500), dados jsonb not null default '{}'::jsonb,
    executado_por uuid not null, executado_em timestamptz not null default now()
);

create table if not exists plantaopro.pagamento_contestacoes (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, cliente_id uuid not null,
    pagamento_id uuid not null references plantaopro.pagamentos(id), motivo varchar(500) not null,
    status varchar(20) not null default 'ABERTA', valor_original numeric(14,2) not null,
    valor_proposto numeric(14,2), aberto_por uuid not null, aberto_em timestamptz not null default now(),
    decisao varchar(30), justificativa_resolucao varchar(1000), valor_resolvido numeric(14,2),
    resolvido_por uuid, resolvido_em timestamptz, created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
    constraint ck_pagamento_contestacao_status check (status in ('ABERTA','RESOLVIDA','CANCELADA')),
    constraint ck_pagamento_contestacao_decisao check (decisao is null or decisao in ('MANTER_VALOR','AJUSTAR_VALOR','CANCELAR_PAGAMENTO'))
);

create unique index if not exists ux_pagamento_contestacao_aberta on plantaopro.pagamento_contestacoes(tenant_id,pagamento_id) where status='ABERTA';
create index if not exists ix_fechamento_status on plantaopro.fechamento_plantao(tenant_id,status,iniciado_em desc);
create index if not exists ix_fechamento_historico_timeline on plantaopro.fechamento_historico(tenant_id,fechamento_id,executado_em desc);
create index if not exists ix_fechamento_divergencias_abertas on plantaopro.fechamento_divergencias(tenant_id,fechamento_id,status);
create index if not exists ix_contestacoes_status on plantaopro.pagamento_contestacoes(tenant_id,status,aberto_em desc);

