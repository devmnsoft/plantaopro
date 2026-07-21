-- v1.18.7 Central de Segurança e Administração de Acessos
SET search_path TO plantaopro, public;

-- v1.18.7 Central de Segurança: sessões, refresh tokens, políticas e auditoria.
CREATE TABLE IF NOT EXISTS plantaopro.auth_sessoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NOT NULL,
    dispositivo_nome text NOT NULL DEFAULT 'Dispositivo não identificado', ip_mascarado text NULL, user_agent_sanitizado text NULL,
    iniciado_em timestamptz NOT NULL DEFAULT now(), ultimo_uso_em timestamptz NULL, expira_em timestamptz NULL,
    revogada_em timestamptz NULL, motivo_revogacao text NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.auth_refresh_tokens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), sessao_id uuid NOT NULL, usuario_id uuid NOT NULL, token_hash text NOT NULL,
    emitido_em timestamptz NOT NULL DEFAULT now(), expira_em timestamptz NOT NULL, usado_em timestamptz NULL, substituido_por_id uuid NULL,
    revogado_em timestamptz NULL, motivo_revogacao text NULL, reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.auth_revogacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), sessao_id uuid NULL, usuario_id uuid NOT NULL, motivo text NOT NULL,
    revogado_por uuid NULL, ip_mascarado text NULL, reg_date timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.senha_historico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid NOT NULL, senha_hash text NOT NULL,
    origem text NOT NULL DEFAULT 'ALTERACAO_SENHA', reg_date timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.politicas_senha (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, tamanho_minimo int NOT NULL DEFAULT 10,
    exige_maiuscula boolean NOT NULL DEFAULT true, exige_minuscula boolean NOT NULL DEFAULT true, exige_numero boolean NOT NULL DEFAULT true,
    exige_especial boolean NOT NULL DEFAULT true, historico_quantidade int NOT NULL DEFAULT 5, expiracao_dias int NOT NULL DEFAULT 90,
    tentativas_permitidas int NOT NULL DEFAULT 5, bloqueio_minutos int NOT NULL DEFAULT 30, troca_obrigatoria boolean NOT NULL DEFAULT false,
    proibir_senhas_comuns boolean NOT NULL DEFAULT true, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL
);
CREATE INDEX IF NOT EXISTS ix_auth_sessoes_usuario_status ON plantaopro.auth_sessoes(usuario_id, reg_status, ultimo_uso_em DESC);
CREATE INDEX IF NOT EXISTS ix_auth_refresh_tokens_sessao ON plantaopro.auth_refresh_tokens(sessao_id, expira_em DESC);
CREATE INDEX IF NOT EXISTS ix_auth_revogacoes_usuario ON plantaopro.auth_revogacoes(usuario_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_senha_historico_usuario ON plantaopro.senha_historico(usuario_id, reg_date DESC);
CREATE UNIQUE INDEX IF NOT EXISTS ux_politicas_senha_tenant ON plantaopro.politicas_senha(coalesce(tenant_id, '00000000-0000-0000-0000-000000000000'::uuid)) WHERE reg_status='A';
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_auth_refresh_tokens_sessao') THEN ALTER TABLE plantaopro.auth_refresh_tokens ADD CONSTRAINT fk_auth_refresh_tokens_sessao FOREIGN KEY (sessao_id) REFERENCES plantaopro.auth_sessoes(id); END IF;
END $$;
