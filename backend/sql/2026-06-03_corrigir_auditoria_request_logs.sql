create schema if not exists plantaopro;

set search_path to plantaopro, public;

create extension if not exists pgcrypto;

-- ==========================================================
-- Auditoria central
-- ==========================================================

create table if not exists plantaopro.auditoria_acoes_criticas (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    entidade varchar(100) not null,
    entidade_id uuid null,
    acao varchar(100) not null,
    detalhes jsonb null,
    sucesso boolean not null default true,
    ip_origem varchar(64) null,
    perfil varchar(80) null,
    user_agent text null,
    reg_date timestamptz not null default now()
);

alter table if exists plantaopro.auditoria_acoes_criticas
    add column if not exists usuario_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists entidade varchar(100) not null default 'SISTEMA',
    add column if not exists entidade_id uuid null,
    add column if not exists acao varchar(100) not null default 'ACAO',
    add column if not exists detalhes jsonb null,
    add column if not exists sucesso boolean not null default true,
    add column if not exists ip_origem varchar(64) null,
    add column if not exists perfil varchar(80) null,
    add column if not exists user_agent text null,
    add column if not exists reg_date timestamptz not null default now();

alter table if exists plantaopro.auditoria_acoes_criticas
    add column if not exists reg_status char(1) not null default 'A';

alter table if exists plantaopro.auditoria_acoes_criticas
    alter column id set default gen_random_uuid(),
    alter column reg_status set default 'A';

create index if not exists ix_auditoria_acoes_criticas_cliente_data
on plantaopro.auditoria_acoes_criticas(cliente_id, reg_date desc);

create index if not exists ix_auditoria_acoes_criticas_usuario_data
on plantaopro.auditoria_acoes_criticas(usuario_id, reg_date desc);

create index if not exists ix_auditoria_acoes_criticas_entidade
on plantaopro.auditoria_acoes_criticas(entidade, entidade_id);

create index if not exists ix_auditoria_acoes_criticas_acao_data
on plantaopro.auditoria_acoes_criticas(acao, reg_date desc);

-- ==========================================================
-- Logs estruturados de request
-- ==========================================================

create table if not exists plantaopro.api_request_logs (
    id uuid primary key default gen_random_uuid(),
    endpoint text not null,
    metodo varchar(10) not null default 'GET',
    status_code integer not null default 0,
    sucesso boolean not null default true,
    duracao_ms bigint not null default 0,
    usuario_id uuid null,
    cliente_id uuid null,
    email varchar(255) null,
    perfil varchar(80) null,
    ip_origem varchar(64) null,
    user_agent text null,
    query_string text null,
    erro text null,
    reg_date timestamptz not null default now()
);

alter table if exists plantaopro.api_request_logs
    add column if not exists endpoint text not null default '',
    add column if not exists metodo varchar(10) not null default 'GET',
    add column if not exists status_code integer not null default 0,
    add column if not exists sucesso boolean not null default true,
    add column if not exists duracao_ms bigint not null default 0,
    add column if not exists usuario_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists email varchar(255) null,
    add column if not exists perfil varchar(80) null,
    add column if not exists ip_origem varchar(64) null,
    add column if not exists user_agent text null,
    add column if not exists query_string text null,
    add column if not exists erro text null,
    add column if not exists reg_date timestamptz not null default now();

alter table if exists plantaopro.api_request_logs
    alter column id set default gen_random_uuid();


-- Compatibilidade com versões anteriores que criaram colunas em inglês.
-- O código novo usa as colunas em português, mas defaults nas colunas legadas
-- evitam falhas de NOT NULL enquanto o banco é atualizado incrementalmente.
alter table if exists plantaopro.api_request_logs
    add column if not exists method varchar(12) not null default 'GET',
    add column if not exists duration_ms bigint not null default 0,
    add column if not exists ip varchar(80) null,
    add column if not exists error_message text null,
    add column if not exists reg_status char(1) not null default 'A';

alter table if exists plantaopro.api_request_logs
    alter column method set default 'GET',
    alter column duration_ms set default 0,
    alter column reg_status set default 'A';

create index if not exists ix_api_request_logs_reg_date
on plantaopro.api_request_logs(reg_date desc);

create index if not exists ix_api_request_logs_endpoint_data
on plantaopro.api_request_logs(endpoint, reg_date desc);

create index if not exists ix_api_request_logs_status_data
on plantaopro.api_request_logs(status_code, reg_date desc);

create index if not exists ix_api_request_logs_usuario_data
on plantaopro.api_request_logs(usuario_id, reg_date desc);

create index if not exists ix_api_request_logs_cliente_data
on plantaopro.api_request_logs(cliente_id, reg_date desc);

create index if not exists ix_api_request_logs_perfil_data
on plantaopro.api_request_logs(perfil, reg_date desc);

-- ==========================================================
-- Logs de erro, caso o projeto use ou venha a usar
-- ==========================================================

create table if not exists plantaopro.api_error_logs (
    id uuid primary key default gen_random_uuid(),
    endpoint text null,
    metodo varchar(10) null,
    status_code integer null,
    usuario_id uuid null,
    cliente_id uuid null,
    email varchar(255) null,
    perfil varchar(80) null,
    ip_origem varchar(64) null,
    user_agent text null,
    mensagem text not null,
    exception_type varchar(255) null,
    stack_trace text null,
    reg_date timestamptz not null default now()
);

alter table if exists plantaopro.api_error_logs
    add column if not exists endpoint text null,
    add column if not exists metodo varchar(10) null,
    add column if not exists status_code integer null,
    add column if not exists usuario_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists email varchar(255) null,
    add column if not exists perfil varchar(80) null,
    add column if not exists ip_origem varchar(64) null,
    add column if not exists user_agent text null,
    add column if not exists mensagem text not null default '',
    add column if not exists exception_type varchar(255) null,
    add column if not exists stack_trace text null,
    add column if not exists reg_date timestamptz not null default now();

alter table if exists plantaopro.api_error_logs
    alter column id set default gen_random_uuid();


-- Compatibilidade com versões anteriores que criaram colunas em inglês.
alter table if exists plantaopro.api_error_logs
    add column if not exists method varchar(12) not null default 'GET',
    add column if not exists ip varchar(80) null,
    add column if not exists error_message text null,
    add column if not exists reg_status char(1) not null default 'A';

alter table if exists plantaopro.api_error_logs
    alter column method set default 'GET',
    alter column reg_status set default 'A';

create index if not exists ix_api_error_logs_reg_date
on plantaopro.api_error_logs(reg_date desc);

create index if not exists ix_api_error_logs_status_data
on plantaopro.api_error_logs(status_code, reg_date desc);
