create schema if not exists plantaopro;

create table if not exists plantaopro.auditoria_acoes_criticas(
    id uuid primary key,
    usuario_id uuid null,
    cliente_id uuid null,
    entidade varchar(80) not null,
    entidade_id uuid null,
    acao varchar(80) not null,
    detalhes jsonb not null default '{}'::jsonb,
    sucesso boolean not null default true,
    ip_origem varchar(80) null,
    perfil varchar(120) null,
    reg_date timestamp not null default now(),
    reg_status char(1) not null default 'A'
);

alter table plantaopro.auditoria_acoes_criticas add column if not exists usuario_id uuid null;
alter table plantaopro.auditoria_acoes_criticas add column if not exists cliente_id uuid null;
alter table plantaopro.auditoria_acoes_criticas add column if not exists entidade varchar(80) not null default 'SISTEMA';
alter table plantaopro.auditoria_acoes_criticas add column if not exists entidade_id uuid null;
alter table plantaopro.auditoria_acoes_criticas add column if not exists acao varchar(80) not null default 'EVENTO';
alter table plantaopro.auditoria_acoes_criticas add column if not exists detalhes jsonb not null default '{}'::jsonb;
alter table plantaopro.auditoria_acoes_criticas add column if not exists sucesso boolean not null default true;
alter table plantaopro.auditoria_acoes_criticas add column if not exists ip_origem varchar(80) null;
alter table plantaopro.auditoria_acoes_criticas add column if not exists perfil varchar(120) null;
alter table plantaopro.auditoria_acoes_criticas add column if not exists reg_date timestamp not null default now();
alter table plantaopro.auditoria_acoes_criticas add column if not exists reg_status char(1) not null default 'A';

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
    reg_date timestamp not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.acessos_negados_log(
    id uuid primary key,
    usuario_id uuid null,
    cliente_id uuid null,
    entidade varchar(80) null,
    entidade_id uuid null,
    motivo text not null,
    ip varchar(80) null,
    perfil varchar(120) null,
    reg_date timestamp not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.exportacao_logs(
    id uuid primary key,
    usuario_id uuid null,
    cliente_id uuid null,
    tipo varchar(80) not null,
    filtros jsonb not null default '{}'::jsonb,
    ip varchar(80) null,
    reg_date timestamp not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.usuario_atividade(
    id uuid primary key,
    usuario_id uuid not null,
    cliente_id uuid null,
    atividade varchar(120) not null,
    detalhes jsonb not null default '{}'::jsonb,
    reg_date timestamp not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.observabilidade_eventos(
    id uuid primary key,
    tipo varchar(80) not null,
    severidade varchar(30) not null,
    origem varchar(120) not null,
    mensagem text not null,
    detalhes jsonb not null default '{}'::jsonb,
    cliente_id uuid null,
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
    reg_date timestamp not null default now(),
    reg_status char(1) not null default 'A'
);

create index if not exists ix_auditoria_acoes_cliente_reg_date on plantaopro.auditoria_acoes_criticas(cliente_id, reg_date desc);
create index if not exists ix_auditoria_acoes_usuario_reg_date on plantaopro.auditoria_acoes_criticas(usuario_id, reg_date desc);
create index if not exists ix_auditoria_acoes_entidade on plantaopro.auditoria_acoes_criticas(entidade, entidade_id);
create index if not exists ix_api_request_endpoint_reg_date on plantaopro.api_request_logs(endpoint, reg_date desc);
create index if not exists ix_api_request_status_reg_date on plantaopro.api_request_logs(status_code, reg_date desc);
create index if not exists ix_api_request_cliente_reg_date on plantaopro.api_request_logs(cliente_id, reg_date desc);
create index if not exists ix_api_error_endpoint_reg_date on plantaopro.api_error_logs(endpoint, reg_date desc);
create index if not exists ix_api_error_status_reg_date on plantaopro.api_error_logs(status_code, reg_date desc);
create index if not exists ix_acessos_negados_cliente_reg_date on plantaopro.acessos_negados_log(cliente_id, reg_date desc);
create index if not exists ix_exportacao_logs_cliente_reg_date on plantaopro.exportacao_logs(cliente_id, reg_date desc);
create index if not exists ix_usuario_atividade_usuario_reg_date on plantaopro.usuario_atividade(usuario_id, reg_date desc);
create index if not exists ix_observabilidade_eventos_tipo_reg_date on plantaopro.observabilidade_eventos(tipo, reg_date desc);
create index if not exists ix_permissao_logs_cliente_reg_date on plantaopro.permissao_logs(cliente_id, reg_date desc);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_auditoria_acoes_criticas_reg_status'
          AND conrelid = 'plantaopro.auditoria_acoes_criticas'::regclass
    ) THEN
        ALTER TABLE plantaopro.auditoria_acoes_criticas
        ADD CONSTRAINT ck_auditoria_acoes_criticas_reg_status CHECK (reg_status in ('A','I','E'));
    END IF;
END $$;
