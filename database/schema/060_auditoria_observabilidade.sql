-- Auditoria e observabilidade preservadas a partir das origens históricas normalizadas pelo gerador.
SET search_path TO plantaopro, public;

-- DDL canônico idempotente v1.18.9
CREATE TABLE IF NOT EXISTS plantaopro.auditoria (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.auditoria_acoes_criticas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id uuid NULL,
    cliente_id uuid NULL,
    entidade varchar(100) NOT NULL DEFAULT 'SISTEMA',
    entidade_id uuid NULL,
    acao varchar(100) NOT NULL DEFAULT 'ACAO',
    detalhes jsonb NULL,
    sucesso boolean NOT NULL DEFAULT true,
    ip_origem varchar(64) NULL,
    perfil varchar(80) NULL,
    user_agent text NULL,
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A'
);

-- Compatibilidade com instalações existentes que criaram a tabela com shape genérico:
-- adiciona as colunas de auditoria usadas pelo código em execução.
ALTER TABLE IF EXISTS plantaopro.auditoria_acoes_criticas
    ADD COLUMN IF NOT EXISTS usuario_id uuid NULL,
    ADD COLUMN IF NOT EXISTS cliente_id uuid NULL,
    ADD COLUMN IF NOT EXISTS entidade varchar(100) NOT NULL DEFAULT 'SISTEMA',
    ADD COLUMN IF NOT EXISTS entidade_id uuid NULL,
    ADD COLUMN IF NOT EXISTS acao varchar(100) NOT NULL DEFAULT 'ACAO',
    ADD COLUMN IF NOT EXISTS detalhes jsonb NULL,
    ADD COLUMN IF NOT EXISTS sucesso boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS ip_origem varchar(64) NULL,
    ADD COLUMN IF NOT EXISTS perfil varchar(80) NULL,
    ADD COLUMN IF NOT EXISTS user_agent text NULL,
    ADD COLUMN IF NOT EXISTS reg_date timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A';

ALTER TABLE IF EXISTS plantaopro.auditoria_acoes_criticas
    ALTER COLUMN id SET DEFAULT gen_random_uuid();

CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_cliente_data ON plantaopro.auditoria_acoes_criticas(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_usuario_data ON plantaopro.auditoria_acoes_criticas(usuario_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_entidade ON plantaopro.auditoria_acoes_criticas(entidade, entidade_id);
CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_acao_data ON plantaopro.auditoria_acoes_criticas(acao, reg_date DESC);
CREATE TABLE IF NOT EXISTS plantaopro.auditoria_eventos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.api_request_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    endpoint text NOT NULL DEFAULT '',
    metodo varchar(10) NOT NULL DEFAULT 'GET',
    method varchar(12) NOT NULL DEFAULT 'GET',
    status_code integer NOT NULL DEFAULT 0,
    sucesso boolean NOT NULL DEFAULT true,
    duracao_ms bigint NOT NULL DEFAULT 0,
    duration_ms bigint NOT NULL DEFAULT 0,
    usuario_id uuid NULL,
    cliente_id uuid NULL,
    email varchar(255) NULL,
    perfil varchar(80) NULL,
    ip_origem varchar(64) NULL,
    ip varchar(80) NULL,
    user_agent text NULL,
    query_string text NULL,
    erro text NULL,
    error_message text NULL,
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A'
);

-- Compatibilidade com instalações existentes que criaram a tabela com shape genérico:
-- adiciona as colunas de observabilidade usadas pelo middleware e pelos controllers.
ALTER TABLE IF EXISTS plantaopro.api_request_logs
    ADD COLUMN IF NOT EXISTS endpoint text NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS metodo varchar(10) NOT NULL DEFAULT 'GET',
    ADD COLUMN IF NOT EXISTS method varchar(12) NOT NULL DEFAULT 'GET',
    ADD COLUMN IF NOT EXISTS status_code integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS sucesso boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS duracao_ms bigint NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS duration_ms bigint NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS usuario_id uuid NULL,
    ADD COLUMN IF NOT EXISTS cliente_id uuid NULL,
    ADD COLUMN IF NOT EXISTS email varchar(255) NULL,
    ADD COLUMN IF NOT EXISTS perfil varchar(80) NULL,
    ADD COLUMN IF NOT EXISTS ip_origem varchar(64) NULL,
    ADD COLUMN IF NOT EXISTS ip varchar(80) NULL,
    ADD COLUMN IF NOT EXISTS user_agent text NULL,
    ADD COLUMN IF NOT EXISTS query_string text NULL,
    ADD COLUMN IF NOT EXISTS erro text NULL,
    ADD COLUMN IF NOT EXISTS error_message text NULL,
    ADD COLUMN IF NOT EXISTS reg_date timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A';

ALTER TABLE IF EXISTS plantaopro.api_request_logs
    ALTER COLUMN id SET DEFAULT gen_random_uuid();

CREATE INDEX IF NOT EXISTS ix_api_request_logs_reg_date ON plantaopro.api_request_logs(reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_endpoint_data ON plantaopro.api_request_logs(endpoint, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_status_data ON plantaopro.api_request_logs(status_code, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_usuario_data ON plantaopro.api_request_logs(usuario_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_cliente_data ON plantaopro.api_request_logs(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_perfil_data ON plantaopro.api_request_logs(perfil, reg_date DESC);
CREATE TABLE IF NOT EXISTS plantaopro.api_error_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    endpoint text NULL,
    metodo varchar(10) NULL,
    method varchar(12) NOT NULL DEFAULT 'GET',
    status_code integer NULL,
    success boolean NOT NULL DEFAULT true,
    usuario_id uuid NULL,
    cliente_id uuid NULL,
    email varchar(255) NULL,
    perfil varchar(80) NULL,
    ip_origem varchar(64) NULL,
    ip varchar(80) NULL,
    user_agent text NULL,
    query_string text NULL,
    duration_ms bigint NOT NULL DEFAULT 0,
    mensagem text NOT NULL DEFAULT '',
    error_message text NULL,
    exception_type varchar(255) NULL,
    stack_trace text NULL,
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A'
);

-- Compatibilidade com instalações existentes que criaram a tabela com shape genérico:
-- adiciona as colunas de observabilidade usadas pelo middleware e pelos controllers.
ALTER TABLE IF EXISTS plantaopro.api_error_logs
    ADD COLUMN IF NOT EXISTS endpoint text NULL,
    ADD COLUMN IF NOT EXISTS metodo varchar(10) NULL,
    ADD COLUMN IF NOT EXISTS method varchar(12) NOT NULL DEFAULT 'GET',
    ADD COLUMN IF NOT EXISTS status_code integer NULL,
    ADD COLUMN IF NOT EXISTS success boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS usuario_id uuid NULL,
    ADD COLUMN IF NOT EXISTS cliente_id uuid NULL,
    ADD COLUMN IF NOT EXISTS email varchar(255) NULL,
    ADD COLUMN IF NOT EXISTS perfil varchar(80) NULL,
    ADD COLUMN IF NOT EXISTS ip_origem varchar(64) NULL,
    ADD COLUMN IF NOT EXISTS ip varchar(80) NULL,
    ADD COLUMN IF NOT EXISTS user_agent text NULL,
    ADD COLUMN IF NOT EXISTS query_string text NULL,
    ADD COLUMN IF NOT EXISTS duration_ms bigint NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS mensagem text NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS error_message text NULL,
    ADD COLUMN IF NOT EXISTS exception_type varchar(255) NULL,
    ADD COLUMN IF NOT EXISTS stack_trace text NULL,
    ADD COLUMN IF NOT EXISTS reg_date timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A';

ALTER TABLE IF EXISTS plantaopro.api_error_logs
    ALTER COLUMN id SET DEFAULT gen_random_uuid();

CREATE INDEX IF NOT EXISTS ix_api_error_logs_reg_date ON plantaopro.api_error_logs(reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_error_logs_status_data ON plantaopro.api_error_logs(status_code, reg_date DESC);
CREATE TABLE IF NOT EXISTS plantaopro.background_job_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.logs_operacionais (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.eventos_sistema (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.acessos_negados_log (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.permissao_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
