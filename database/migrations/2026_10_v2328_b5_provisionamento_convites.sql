-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2328_b5_provisionamento_convites
-- B5 (provisionar cliente novo de verdade):
--
-- 1. plantaopro.clientes: o finalizar self-service grava email/telefone/
--    cidade/estado/plano_id — colunas que faltam nos bancos vigentes (42703
--    no finalizar real). Adiciona sem tocar nas existentes.
-- 2. plantaopro.usuario_convites (NOVA): convite de equipe com expiracao e
--    uso unico. Guarda SÓ o hash SHA-256 do token (nunca o token); estado
--    derivado (PENDENTE = sem uso/revogacao e dentro da validade). Indice
--    unico parcial impede dois convites pendentes para o mesmo e-mail no
--    tenant (re-convite idempotente = 409 honesto, não duplicata).
--
-- Idempotencia: CREATE/ADD/INDEX IF NOT EXISTS; reaplicacao nao muda nada.
-- ============================================================================

DO $migration$
BEGIN
    -- 1. clientes: colunas do contrato de provisionamento.
    ALTER TABLE plantaopro.clientes
        ADD COLUMN IF NOT EXISTS email varchar(254) NULL,
        ADD COLUMN IF NOT EXISTS telefone varchar(40) NULL,
        ADD COLUMN IF NOT EXISTS cidade varchar(120) NULL,
        ADD COLUMN IF NOT EXISTS estado varchar(60) NULL,
        ADD COLUMN IF NOT EXISTS plano_id uuid NULL;

    -- 2. convites de equipe (token com expiracao + uso unico).
    CREATE TABLE IF NOT EXISTS plantaopro.usuario_convites (
        id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
        tenant_id uuid NOT NULL,
        cliente_id uuid NOT NULL,
        email varchar(254) NOT NULL,
        perfil_ids uuid[] NOT NULL,
        token_hash char(64) NOT NULL,
        expira_em timestamptz NOT NULL,
        usado_em timestamptz NULL,
        usado_por uuid NULL,
        revogado_em timestamptz NULL,
        criado_por uuid NULL,
        reg_date timestamptz NOT NULL DEFAULT now(),
        reg_update timestamptz NULL,
        reg_status char(1) NOT NULL DEFAULT 'A'
    );
    CREATE UNIQUE INDEX IF NOT EXISTS ux_usuario_convites_token
        ON plantaopro.usuario_convites(token_hash)
        WHERE reg_status = 'A';
    CREATE UNIQUE INDEX IF NOT EXISTS ux_usuario_convites_pendente
        ON plantaopro.usuario_convites(tenant_id, lower(email))
        WHERE reg_status = 'A' AND usado_em IS NULL AND revogado_em IS NULL;
    CREATE INDEX IF NOT EXISTS ix_usuario_convites_tenant
        ON plantaopro.usuario_convites(tenant_id, reg_date DESC);
END $migration$;
