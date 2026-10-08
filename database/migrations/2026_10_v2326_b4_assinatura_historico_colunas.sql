-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2326_b4_assinatura_historico_colunas
-- B4 (estados explicitos): reconcilia plantaopro.assinatura_historico.
-- O alterar-plano manual B2B (e a aprovacao de upgrade/downgrade B4) grava
-- (plano_id_anterior, plano_id_novo, acao, justificativa) — colunas que
-- nenhuma migration criou nos bancos vigentes (42703 no ALTERAR_PLANO real).
-- Adiciona as colunas que faltam sem tocar nas existentes.
--
-- Idempotencia: ADD COLUMN IF NOT EXISTS; reaplicacao nao muda nada.
-- ============================================================================

DO $migration$
BEGIN
    ALTER TABLE plantaopro.assinatura_historico
        ADD COLUMN IF NOT EXISTS plano_id_anterior uuid NULL,
        ADD COLUMN IF NOT EXISTS plano_id_novo uuid NULL,
        ADD COLUMN IF NOT EXISTS acao varchar(40) NULL,
        ADD COLUMN IF NOT EXISTS justificativa text NULL,
        ADD COLUMN IF NOT EXISTS reg_update timestamptz NULL;
END $migration$;
