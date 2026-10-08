-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2325_b4_plano_recursos_colunas
-- B4 (matriz comercial canonica): reconcilia plantaopro.plano_recursos.
-- O shape canonico (saas_inteligente_auditavel) tem recurso/habilitado/
-- limite, mas o contrato vigente da API (GET/PUT planos/{id}/recursos e a
-- matriz B4) usa codigo/nome/descricao — colunas que faltam nos bancos onde
-- outra migration criou a tabela primeiro (42703 no GET/PUT). Adiciona as
-- colunas que faltam sem tocar nas existentes.
--
-- Idempotencia: ADD COLUMN IF NOT EXISTS; reaplicacao nao muda nada.
-- ============================================================================

DO $migration$
BEGIN
    ALTER TABLE plantaopro.plano_recursos
        ADD COLUMN IF NOT EXISTS codigo varchar(80) NULL,
        ADD COLUMN IF NOT EXISTS nome varchar(160) NULL,
        ADD COLUMN IF NOT EXISTS descricao text NULL;
END $migration$;
