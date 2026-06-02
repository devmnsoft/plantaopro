create schema if not exists plantaopro;

create table if not exists plantaopro.auditoria_acoes_criticas(
    id uuid primary key,
    usuario_id uuid null,
    cliente_id uuid null,
    entidade varchar(80) not null default 'SISTEMA',
    entidade_id uuid null,
    acao varchar(80) not null default 'EVENTO',
    detalhes jsonb not null default '{}'::jsonb,
    sucesso boolean not null default true,
    ip_origem varchar(80) null,
    perfil varchar(120) null,
    user_agent text null,
    reg_date timestamp not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.api_request_logs(
    id uuid primary key,
    endpoint text not null,
    method varchar(12) not null,
    status_code int not null,
    usuario_id uuid null,
    cliente_id uuid null,
    perfil varchar(120) null,
    ip varchar(80) null,
    duration_ms bigint not null default 0,
    sucesso boolean not null default true,
    error_message text null,
    correlation_id varchar(80) null,
    reg_date timestamp not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.api_error_logs(
    id uuid primary key,
    endpoint text not null,
    method varchar(12) not null,
    status_code int not null,
    usuario_id uuid null,
    cliente_id uuid null,
    perfil varchar(120) null,
    ip varchar(80) null,
    error_message text null,
    correlation_id varchar(80) null,
    reg_date timestamp not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.acessos_negados_log(
    id uuid primary key,
    usuario_id uuid null,
    cliente_id uuid null,
    entidade varchar(80) null,
    entidade_id uuid null,
    acao varchar(80) null,
    motivo text not null,
    ip varchar(80) null,
    perfil varchar(120) null,
    reg_date timestamp not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.permissao_logs(
    id uuid primary key,
    usuario_id uuid null,
    cliente_id uuid null,
    permissao varchar(120) not null,
    autorizado boolean not null,
    motivo text null,
    perfil varchar(120) null,
    reg_date timestamp not null default now(),
    reg_status char(1) not null default 'A'
);

alter table plantaopro.auditoria_acoes_criticas add column if not exists user_agent text null;
alter table plantaopro.api_request_logs add column if not exists correlation_id varchar(80) null;
alter table plantaopro.api_error_logs add column if not exists correlation_id varchar(80) null;
alter table plantaopro.acessos_negados_log add column if not exists acao varchar(80) null;
alter table plantaopro.permissao_logs add column if not exists perfil varchar(120) null;

create index if not exists ix_auditoria_acoes_acao_reg_date on plantaopro.auditoria_acoes_criticas(acao, reg_date desc);
create index if not exists ix_acessos_negados_usuario_reg_date on plantaopro.acessos_negados_log(usuario_id, reg_date desc);
create index if not exists ix_permissao_logs_usuario_reg_date on plantaopro.permissao_logs(usuario_id, reg_date desc);
create index if not exists ix_observabilidade_eventos_cliente_reg_date on plantaopro.observabilidade_eventos(cliente_id, reg_date desc);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_api_request_logs_reg_status'
          AND conrelid = 'plantaopro.api_request_logs'::regclass
    ) THEN
        ALTER TABLE plantaopro.api_request_logs
        ADD CONSTRAINT ck_api_request_logs_reg_status CHECK (reg_status in ('A','I','E'));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_api_error_logs_reg_status'
          AND conrelid = 'plantaopro.api_error_logs'::regclass
    ) THEN
        ALTER TABLE plantaopro.api_error_logs
        ADD CONSTRAINT ck_api_error_logs_reg_status CHECK (reg_status in ('A','I','E'));
    END IF;
END $$;
