-- ============================================================================
-- PlantaPro — Administrativo 360 | Migration v2202
-- Reconciliação de auditoria e observabilidade (delta do 060 em 8c183e2)
-- Instalações existentes criaram auditoria_acoes_criticas, api_request_logs e
-- api_error_logs com o shape genérico histórico (tenant_id/codigo/nome/status/
-- dados/criado_em/atualizado_em). O commit 8c183e2 reformulou
-- database/schema/060_auditoria_observabilidade.sql para o shape novo usado pelo
-- RequestLoggingMiddleware e pelo auditor central, incluindo os índices de
-- consulta. Esta migration aplica o MESMO delta às bases existentes, de forma
-- idempotente, convergindo fresh-install e upgrade ao mesmo shape final:
--   * ADD COLUMN IF NOT EXISTS das colunas novas e das colunas genéricas legadas
--     (qualquer shape intermediário converge ao shape final canônico);
--   * DEFAULT gen_random_uuid() preservado na coluna id;
--   * CREATE INDEX IF NOT EXISTS dos índices de consulta.
-- Nenhuma coluna existente é alterada nem removida; histórico preservado.
-- ============================================================================

-- auditoria_acoes_criticas
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
    ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A',
    ADD COLUMN IF NOT EXISTS tenant_id uuid NULL,
    ADD COLUMN IF NOT EXISTS codigo text NULL,
    ADD COLUMN IF NOT EXISTS nome text NULL,
    ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO',
    ADD COLUMN IF NOT EXISTS dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS criado_em timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS atualizado_em timestamptz NULL;

ALTER TABLE IF EXISTS plantaopro.auditoria_acoes_criticas
    ALTER COLUMN id SET DEFAULT gen_random_uuid();

CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_cliente_data ON plantaopro.auditoria_acoes_criticas(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_usuario_data ON plantaopro.auditoria_acoes_criticas(usuario_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_entidade ON plantaopro.auditoria_acoes_criticas(entidade, entidade_id);
CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_acao_data ON plantaopro.auditoria_acoes_criticas(acao, reg_date DESC);

-- api_request_logs
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
    ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A',
    ADD COLUMN IF NOT EXISTS tenant_id uuid NULL,
    ADD COLUMN IF NOT EXISTS codigo text NULL,
    ADD COLUMN IF NOT EXISTS nome text NULL,
    ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO',
    ADD COLUMN IF NOT EXISTS dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS criado_em timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS atualizado_em timestamptz NULL;

ALTER TABLE IF EXISTS plantaopro.api_request_logs
    ALTER COLUMN id SET DEFAULT gen_random_uuid();

CREATE INDEX IF NOT EXISTS ix_api_request_logs_reg_date ON plantaopro.api_request_logs(reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_endpoint_data ON plantaopro.api_request_logs(endpoint, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_status_data ON plantaopro.api_request_logs(status_code, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_usuario_data ON plantaopro.api_request_logs(usuario_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_cliente_data ON plantaopro.api_request_logs(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_perfil_data ON plantaopro.api_request_logs(perfil, reg_date DESC);

-- api_error_logs
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
    ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A',
    ADD COLUMN IF NOT EXISTS tenant_id uuid NULL,
    ADD COLUMN IF NOT EXISTS codigo text NULL,
    ADD COLUMN IF NOT EXISTS nome text NULL,
    ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO',
    ADD COLUMN IF NOT EXISTS dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS criado_em timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS atualizado_em timestamptz NULL;

ALTER TABLE IF EXISTS plantaopro.api_error_logs
    ALTER COLUMN id SET DEFAULT gen_random_uuid();

CREATE INDEX IF NOT EXISTS ix_api_error_logs_reg_date ON plantaopro.api_error_logs(reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_error_logs_status_data ON plantaopro.api_error_logs(status_code, reg_date DESC);
