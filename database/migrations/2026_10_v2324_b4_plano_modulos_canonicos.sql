-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2324_b4_plano_modulos_canonicos
-- B4 (matriz comercial canonica): reconcilia plantaopro.plano_modulos.
-- Tres migrations concorrentes criaram a tabela com shapes diferentes
-- (generico / habilitado-status / incluido-limite-preco_adicional); o codigo
-- canonico (LinkPlan + GET matriz) usa o terceiro. Como a tabela esta vazia
-- nos bancos conhecidos, a reconciliacao e aditiva e sem risco de dados:
--
--   1. garante as colunas canonicas (modulo_id, codigo_modulo, incluido,
--      limite, preco_adicional);
--   2. retroage incluido=false onde habilitado=false (bancos que ja tenham
--      linhas no shape antigo);
--   3. cria o indice unico ux_plano_modulos_ativo exigido pelo ON CONFLICT
--      do vincular-plano (sem ele, o upsert quebra).
--
-- Idempotencia: ADD COLUMN / CREATE INDEX IF NOT EXISTS + blocos
-- condicionais ao catalogo; reaplicacao nao muda nada.
-- ============================================================================

DO $migration$
BEGIN
    ALTER TABLE plantaopro.plano_modulos
        ADD COLUMN IF NOT EXISTS modulo_id uuid NULL,
        ADD COLUMN IF NOT EXISTS codigo_modulo text NULL,
        ADD COLUMN IF NOT EXISTS incluido boolean NOT NULL DEFAULT true,
        ADD COLUMN IF NOT EXISTS limite integer NULL,
        ADD COLUMN IF NOT EXISTS preco_adicional numeric(14, 2) NULL;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
         WHERE table_schema = 'plantaopro' AND table_name = 'plano_modulos'
           AND column_name = 'habilitado'
    ) THEN
        UPDATE plantaopro.plano_modulos
           SET incluido = false
         WHERE coalesce(habilitado, true) = false
           AND incluido = true;
    END IF;

    CREATE UNIQUE INDEX IF NOT EXISTS ux_plano_modulos_ativo
        ON plantaopro.plano_modulos(plano_id, modulo_id)
        WHERE reg_status = 'A' AND modulo_id IS NOT NULL;
END $migration$;
