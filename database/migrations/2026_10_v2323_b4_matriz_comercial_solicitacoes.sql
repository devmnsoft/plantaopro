-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2323_b4_matriz_comercial_solicitacoes
-- B4 (matriz comercial canonica): reconcilia o shape das tabelas comerciais que
-- duas migrations concorrentes criaram com tipos diferentes (o "IF NOT EXISTS"
-- congela o shape de quem rodou primeiro):
--
-- 1. plantaopro.planos: publico/destaque/permite_white_label nasceram TEXT
--    ('true'/'false'/NULL) mas o codigo le como boolean (ListarPlanosPublicos
--    quebrava com 42804 e o /planos/publicos devolvia 500 para todo mundo);
--    ordem nasceu TEXT mas o codigo ordena como numero. Conversao com USING
--    explicito: NULL vira false/999 (negacao por padrao em exposicao publica;
--    decisao registrada em r5b4-matriz-comercial.md).
-- 2. plantaopro.upgrade_solicitacoes / downgrade_solicitacoes: nasceram com
--    colunas genericas (codigo/nome/dados), mas o self-service insere colunas
--    ricas (assinatura_id/plano_destino_id/motivo/...) -> 500 em
--    solicitar-upgrade/downgrade/cancelamento. Adiciona as colunas que faltam
--    + colunas de decisao (decidido_por/decidido_em/justificativa_decisao) para
--    a aprovacao B2B das solicitacoes (estados explicitos).
--
-- Idempotencia: cada bloco verifica o catalogo antes de alterar; reaplicacao
-- nao muda nada. Nao ha UPDATE de dados alem da conversao de tipo (USING).
-- ============================================================================

DO $migration$
BEGIN
    -- 1a. planos.publico TEXT -> boolean (NULL vira false: plano interno/rascunho
    -- nao aparece no catalogo publico ate decisao comercial explicita).
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
         WHERE table_schema = 'plantaopro' AND table_name = 'planos'
           AND column_name = 'publico' AND data_type <> 'boolean'
    ) THEN
        ALTER TABLE plantaopro.planos ALTER COLUMN publico TYPE boolean
            USING (coalesce(lower(nullif(publico, '')), 'false') IN ('true', 't', '1', 's', 'sim', 'y', 'yes'));
    END IF;

    -- 1b. planos.destaque TEXT -> boolean (NULL vira false).
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
         WHERE table_schema = 'plantaopro' AND table_name = 'planos'
           AND column_name = 'destaque' AND data_type <> 'boolean'
    ) THEN
        ALTER TABLE plantaopro.planos ALTER COLUMN destaque TYPE boolean
            USING (coalesce(lower(nullif(destaque, '')), 'false') IN ('true', 't', '1', 's', 'sim', 'y', 'yes'));
    END IF;

    -- 1c. planos.permite_white_label TEXT -> boolean (NULL vira false).
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
         WHERE table_schema = 'plantaopro' AND table_name = 'planos'
           AND column_name = 'permite_white_label' AND data_type <> 'boolean'
    ) THEN
        ALTER TABLE plantaopro.planos ALTER COLUMN permite_white_label TYPE boolean
            USING (coalesce(lower(nullif(permite_white_label, '')), 'false') IN ('true', 't', '1', 's', 'sim', 'y', 'yes'));
    END IF;

    -- 1d. planos.ordem TEXT -> integer (nao-numerico/NULL vira 999 = fim da lista,
    -- mesmo fallback do ORDER BY do catalogo publico).
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
         WHERE table_schema = 'plantaopro' AND table_name = 'planos'
           AND column_name = 'ordem' AND data_type <> 'integer'
    ) THEN
        ALTER TABLE plantaopro.planos ALTER COLUMN ordem TYPE integer
            USING (CASE WHEN ordem ~ '^[0-9]+$' THEN ordem::integer ELSE 999 END);
    END IF;

    -- 2a. upgrade_solicitacoes: colunas ricas do self-service + decisao B2B.
    ALTER TABLE plantaopro.upgrade_solicitacoes
        ADD COLUMN IF NOT EXISTS assinatura_id uuid NULL,
        ADD COLUMN IF NOT EXISTS plano_atual_id uuid NULL,
        ADD COLUMN IF NOT EXISTS plano_destino_id uuid NULL,
        ADD COLUMN IF NOT EXISTS motivo text NULL,
        ADD COLUMN IF NOT EXISTS solicitado_por uuid NULL,
        ADD COLUMN IF NOT EXISTS decidido_por uuid NULL,
        ADD COLUMN IF NOT EXISTS decidido_em timestamptz NULL,
        ADD COLUMN IF NOT EXISTS justificativa_decisao text NULL;
    CREATE INDEX IF NOT EXISTS ix_upgrade_solicitacoes_assinatura_id
        ON plantaopro.upgrade_solicitacoes(assinatura_id);

    -- 2b. downgrade_solicitacoes: idem + flags de impacto (cancelamento usa esta
    -- tabela com status='CANCELAMENTO_SOLICITADO' e plano_destino = plano atual).
    ALTER TABLE plantaopro.downgrade_solicitacoes
        ADD COLUMN IF NOT EXISTS assinatura_id uuid NULL,
        ADD COLUMN IF NOT EXISTS plano_atual_id uuid NULL,
        ADD COLUMN IF NOT EXISTS plano_destino_id uuid NULL,
        ADD COLUMN IF NOT EXISTS motivo text NULL,
        ADD COLUMN IF NOT EXISTS impacto_validado boolean NOT NULL DEFAULT false,
        ADD COLUMN IF NOT EXISTS bloqueado boolean NOT NULL DEFAULT false,
        ADD COLUMN IF NOT EXISTS solicitado_por uuid NULL,
        ADD COLUMN IF NOT EXISTS decidido_por uuid NULL,
        ADD COLUMN IF NOT EXISTS decidido_em timestamptz NULL,
        ADD COLUMN IF NOT EXISTS justificativa_decisao text NULL;
    CREATE INDEX IF NOT EXISTS ix_downgrade_solicitacoes_assinatura_id
        ON plantaopro.downgrade_solicitacoes(assinatura_id);
END $migration$;
