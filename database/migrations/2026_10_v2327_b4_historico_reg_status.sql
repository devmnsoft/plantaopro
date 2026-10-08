-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2327_b4_historico_reg_status
-- B4: complementa a v2326 — plantaopro.assinatura_historico tambem nao tem
-- reg_status nos bancos vigentes (o shape canonico auditavel tem). Sem ela,
-- o INSERT do alterar-plano/aprovacao quebra com 42703.
--
-- Idempotencia: ADD COLUMN IF NOT EXISTS; reaplicacao nao muda nada.
-- ============================================================================

DO $migration$
BEGIN
    ALTER TABLE plantaopro.assinatura_historico
        ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A';
END $migration$;
