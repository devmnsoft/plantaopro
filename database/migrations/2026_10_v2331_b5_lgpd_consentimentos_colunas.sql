-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2331_b5_lgpd_consentimentos_colunas
-- B5 (provisionar cliente novo): reconcilia plantaopro.lgpd_consentimentos.
-- O provisionamento grava (titular_email, aceito, origem, ip_origem) do shape
-- canonico (self_service_white_label), mas os bancos vigentes tem outro
-- shape (42703 no finalizar real). Adiciona as colunas que faltam; o codigo
-- passa a gravar tambem consentido/ip exigidos pelo shape vigente.
--
-- Idempotencia: ADD COLUMN IF NOT EXISTS; reaplicacao nao muda nada.
-- ============================================================================

DO $migration$
BEGIN
    ALTER TABLE plantaopro.lgpd_consentimentos
        ADD COLUMN IF NOT EXISTS titular_email text NULL,
        ADD COLUMN IF NOT EXISTS politica_id uuid NULL,
        ADD COLUMN IF NOT EXISTS aceito boolean NULL,
        ADD COLUMN IF NOT EXISTS origem text NULL,
        ADD COLUMN IF NOT EXISTS ip_origem text NULL;
END $migration$;
