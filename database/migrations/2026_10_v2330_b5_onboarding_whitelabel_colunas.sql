-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2330_b5_onboarding_whitelabel_colunas
-- B5 (provisionar cliente novo): reconcilia tenant_white_label,
-- tenant_onboarding e tenant_onboarding_checklist. O provisionamento
-- (CriarWhiteLabelPadrao/CriarOnboarding) e as leituras usam o shape
-- canonico (self_service_white_label), mas os bancos vigentes tem o shape
-- generico (42703 no finalizar real). Adiciona as colunas que faltam.
--
-- Idempotencia: ADD COLUMN IF NOT EXISTS; reaplicacao nao muda nada.
-- ============================================================================

DO $migration$
BEGIN
    ALTER TABLE plantaopro.tenant_white_label
        ADD COLUMN IF NOT EXISTS nome_plataforma text NULL,
        ADD COLUMN IF NOT EXISTS cliente_nome text NULL,
        ADD COLUMN IF NOT EXISTS slogan text NULL,
        ADD COLUMN IF NOT EXISTS logo_url text NULL,
        ADD COLUMN IF NOT EXISTS logo_reduzida_url text NULL,
        ADD COLUMN IF NOT EXISTS favicon_url text NULL,
        ADD COLUMN IF NOT EXISTS cor_primaria text NULL,
        ADD COLUMN IF NOT EXISTS cor_secundaria text NULL,
        ADD COLUMN IF NOT EXISTS cor_fundo text NULL,
        ADD COLUMN IF NOT EXISTS cor_menu text NULL,
        ADD COLUMN IF NOT EXISTS tema text NULL,
        ADD COLUMN IF NOT EXISTS email_remetente text NULL,
        ADD COLUMN IF NOT EXISTS texto_boas_vindas text NULL,
        ADD COLUMN IF NOT EXISTS texto_rodape text NULL,
        ADD COLUMN IF NOT EXISTS login_banner_url text NULL;

    ALTER TABLE plantaopro.tenant_onboarding
        ADD COLUMN IF NOT EXISTS progresso int NULL,
        ADD COLUMN IF NOT EXISTS proxima_acao text NULL,
        ADD COLUMN IF NOT EXISTS iniciado_em timestamptz NULL,
        ADD COLUMN IF NOT EXISTS finalizado_em timestamptz NULL;

    ALTER TABLE plantaopro.tenant_onboarding_checklist
        ADD COLUMN IF NOT EXISTS onboarding_id uuid NULL,
        ADD COLUMN IF NOT EXISTS titulo text NULL,
        ADD COLUMN IF NOT EXISTS descricao text NULL,
        ADD COLUMN IF NOT EXISTS ordem int NULL,
        ADD COLUMN IF NOT EXISTS obrigatorio boolean NULL,
        ADD COLUMN IF NOT EXISTS concluido boolean NOT NULL DEFAULT false,
        ADD COLUMN IF NOT EXISTS concluido_em timestamptz NULL,
        ADD COLUMN IF NOT EXISTS link_acao text NULL;
END $migration$;
