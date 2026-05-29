-- PlantaoPro Enterprise - auditoria central, observabilidade e segurança multiempresa

create schema if not exists plantaopro;

create table if not exists plantaopro.auditoria_acoes_criticas (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    entidade varchar(120) not null,
    entidade_id uuid null,
    acao varchar(120) not null,
    detalhes jsonb null,
    sucesso boolean not null default true,
    ip_origem varchar(80) null,
    perfil varchar(120) null,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table plantaopro.auditoria_acoes_criticas
    add column if not exists usuario_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists entidade varchar(120),
    add column if not exists entidade_id uuid null,
    add column if not exists acao varchar(120),
    add column if not exists detalhes jsonb null,
    add column if not exists sucesso boolean not null default true,
    add column if not exists ip_origem varchar(80) null,
    add column if not exists perfil varchar(120) null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

alter table plantaopro.auditoria_acoes_criticas
    add column if not exists registro_id uuid null,
    add column if not exists ip varchar(80) null,
    add column if not exists user_agent text null,
    add column if not exists descricao text null;

update plantaopro.auditoria_acoes_criticas set entidade_id = registro_id where entidade_id is null and registro_id is not null;
update plantaopro.auditoria_acoes_criticas set ip_origem = ip where ip_origem is null and ip is not null;
update plantaopro.auditoria_acoes_criticas set detalhes = jsonb_build_object('descricao', descricao, 'userAgent', user_agent) where detalhes is null and (descricao is not null or user_agent is not null);

create index if not exists idx_auditoria_criticas_cliente_data on plantaopro.auditoria_acoes_criticas(cliente_id, reg_date desc);
create index if not exists idx_auditoria_criticas_usuario_data on plantaopro.auditoria_acoes_criticas(usuario_id, reg_date desc);
create index if not exists idx_auditoria_criticas_entidade_id on plantaopro.auditoria_acoes_criticas(entidade, entidade_id);
create index if not exists idx_auditoria_criticas_acao_data on plantaopro.auditoria_acoes_criticas(acao, reg_date desc);

create table if not exists plantaopro.api_request_logs (
    id uuid primary key default gen_random_uuid(),
    endpoint text not null,
    method varchar(16) not null,
    status_code int not null,
    duration_ms numeric(12,2) not null,
    cliente_id uuid null,
    usuario_id uuid null,
    perfil varchar(120) null,
    ip varchar(80) null,
    sucesso boolean not null default true,
    error_message text null,
    reg_date timestamp without time zone not null default now()
);

alter table plantaopro.api_request_logs
    add column if not exists perfil varchar(120) null,
    add column if not exists ip varchar(80) null,
    add column if not exists sucesso boolean not null default true,
    add column if not exists error_message text null;

create index if not exists idx_api_request_logs_cliente_data on plantaopro.api_request_logs(cliente_id, reg_date desc);
create index if not exists idx_api_request_logs_usuario_data on plantaopro.api_request_logs(usuario_id, reg_date desc);
create index if not exists idx_api_request_logs_endpoint_data on plantaopro.api_request_logs(endpoint, reg_date desc);
create index if not exists idx_api_request_logs_status_data on plantaopro.api_request_logs(status_code, reg_date desc);

create table if not exists plantaopro.api_error_logs (
    id uuid primary key default gen_random_uuid(),
    endpoint text not null,
    method varchar(16) not null,
    status_code int not null,
    error_message text not null,
    cliente_id uuid null,
    usuario_id uuid null,
    perfil varchar(120) null,
    ip varchar(80) null,
    reg_date timestamp without time zone not null default now()
);

alter table plantaopro.api_error_logs
    add column if not exists perfil varchar(120) null,
    add column if not exists ip varchar(80) null;

create index if not exists idx_api_error_logs_cliente_data on plantaopro.api_error_logs(cliente_id, reg_date desc);
create index if not exists idx_api_error_logs_usuario_data on plantaopro.api_error_logs(usuario_id, reg_date desc);
create index if not exists idx_api_error_logs_endpoint_data on plantaopro.api_error_logs(endpoint, reg_date desc);
create index if not exists idx_api_error_logs_status_data on plantaopro.api_error_logs(status_code, reg_date desc);

create table if not exists plantaopro.acessos_negados_log (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    entidade varchar(120) null,
    entidade_id uuid null,
    motivo text not null,
    ip varchar(80) null,
    perfil varchar(120) null,
    reg_date timestamp without time zone not null default now()
);

create table if not exists plantaopro.exportacao_logs (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    tipo varchar(80) not null,
    filtros text null,
    status varchar(40) not null default 'GERADO',
    total_registros bigint not null default 0,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.usuario_atividade (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    atividade varchar(120) not null,
    detalhes jsonb null,
    ip varchar(80) null,
    reg_date timestamp without time zone not null default now()
);

create table if not exists plantaopro.observabilidade_eventos (
    id uuid primary key default gen_random_uuid(),
    tipo varchar(80) not null,
    severidade varchar(30) not null default 'INFO',
    origem varchar(120) null,
    mensagem text not null,
    detalhes jsonb null,
    cliente_id uuid null,
    usuario_id uuid null,
    reg_date timestamp without time zone not null default now()
);

create table if not exists plantaopro.permissao_logs (
    id uuid primary key default gen_random_uuid(),
    usuario_id uuid null,
    cliente_id uuid null,
    permissao varchar(120) not null,
    permitido boolean not null,
    motivo text null,
    ip varchar(80) null,
    perfil varchar(120) null,
    reg_date timestamp without time zone not null default now()
);

create index if not exists idx_acessos_negados_cliente_data on plantaopro.acessos_negados_log(cliente_id, reg_date desc);
create index if not exists idx_exportacao_logs_cliente_data on plantaopro.exportacao_logs(cliente_id, reg_date desc);
create index if not exists idx_usuario_atividade_usuario_data on plantaopro.usuario_atividade(usuario_id, reg_date desc);
create index if not exists idx_observabilidade_eventos_tipo_data on plantaopro.observabilidade_eventos(tipo, reg_date desc);
create index if not exists idx_permissao_logs_usuario_data on plantaopro.permissao_logs(usuario_id, reg_date desc);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_api_request_logs_status_code'
          AND conrelid = 'plantaopro.api_request_logs'::regclass
    ) THEN
        ALTER TABLE plantaopro.api_request_logs
        ADD CONSTRAINT ck_api_request_logs_status_code
        CHECK (status_code between 100 and 599);
    END IF;
END $$;
