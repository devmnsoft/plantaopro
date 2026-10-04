-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2308_ai_governanca_modelos_orcamento
-- Rodada 2 IA (Brief A.2) — governança dos provedores de IA:
--
--   1. ai_config: modelo próprio do fallback (não reusa o modelo do
--      provedor principal); orçamento mensal com moeda ISO-4217 explícita;
--      contadores mensais ATÔMICOS com rollover preguiçoso via
--      mes_referencia (ano*100+mês calculado no fuso America/Sao_Paulo):
--      usos_mes_atual (reserva de cota) e orcamento_usado_mes (custo).
--      A reserva e o reset de mês acontecem num único UPDATE condicional —
--      sem corrida entre SELECT/INSERT e sem perda de contagem em upgrade.
--   2. ai_usos: custo estimado/confirmado por chamada, flag de custo incerto
--      (ex.: timeout — o provedor pode ter processado), moeda e versão do
--      preço usado, carimbo de reconciliação; task_code estendido para
--      TESTAR_CONEXAO (testes de conexão também ficam auditados, fora da
--      cota das tarefas — a cota continua contando só SUCESSO das tarefas).
--   3. ai_precos_modelos: tabela versionada de preços por provedor/modelo
--      (por milhão de tokens), com seed a partir de documentação oficial
--      consultada em 2026-10-04. Modelo sem preço vigente → custo incerto
--      (a aplicação registra estimativa; o seed NÃO inventa preço).
--   4. ai_chamadas_ativas: slots distribuídos de concorrência (valem para
--      TODAS as instâncias do servidor, não apenas o processo): limpeza por
--      idade (>6 min) + inserção condicional; release explícito ao final de
--      cada chamada (finally).
--
-- Idempotência (contrato de reaplicação): ADD COLUMN IF NOT EXISTS,
-- DROP/ADD CONSTRAINT com nome fixo, CREATE ... IF NOT EXISTS, INSERT ...
-- ON CONFLICT DO NOTHING. Reaplicar sobre o schema atual não altera dados.
-- ============================================================================

-- 1) ai_config ---------------------------------------------------------------
ALTER TABLE plantaopro.ai_config
    ADD COLUMN IF NOT EXISTS fallback_modelo text,
    ADD COLUMN IF NOT EXISTS orcamento_mensal_moeda char(3) NOT NULL DEFAULT 'USD',
    ADD COLUMN IF NOT EXISTS mes_referencia integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS usos_mes_atual integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS orcamento_usado_mes numeric(18, 4) NOT NULL DEFAULT 0;

ALTER TABLE plantaopro.ai_config
    DROP CONSTRAINT IF EXISTS ai_config_fallback_modelo_check;
ALTER TABLE plantaopro.ai_config
    ADD CONSTRAINT ai_config_fallback_modelo_check
    CHECK (fallback_modelo IS NULL OR char_length(fallback_modelo) BETWEEN 1 AND 64);

ALTER TABLE plantaopro.ai_config
    DROP CONSTRAINT IF EXISTS ai_config_orcamento_mensal_moeda_check;
ALTER TABLE plantaopro.ai_config
    ADD CONSTRAINT ai_config_orcamento_mensal_moeda_check
    CHECK (orcamento_mensal_moeda ~ '^[A-Z]{3}$');

ALTER TABLE plantaopro.ai_config
    DROP CONSTRAINT IF EXISTS ai_config_usos_mes_atual_check;
ALTER TABLE plantaopro.ai_config
    ADD CONSTRAINT ai_config_usos_mes_atual_check CHECK (usos_mes_atual >= 0);

ALTER TABLE plantaopro.ai_config
    DROP CONSTRAINT IF EXISTS ai_config_orcamento_usado_mes_check;
ALTER TABLE plantaopro.ai_config
    ADD CONSTRAINT ai_config_orcamento_usado_mes_check CHECK (orcamento_usado_mes >= 0);

-- 2) ai_usos ------------------------------------------------------------------
ALTER TABLE plantaopro.ai_usos
    ADD COLUMN IF NOT EXISTS custo_estimado numeric(14, 4),
    ADD COLUMN IF NOT EXISTS custo_confirmado numeric(14, 4),
    ADD COLUMN IF NOT EXISTS custo_incerto boolean NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS moeda char(3) NOT NULL DEFAULT 'USD',
    ADD COLUMN IF NOT EXISTS preco_versao date,
    ADD COLUMN IF NOT EXISTS reconciliado_em timestamptz;

ALTER TABLE plantaopro.ai_usos
    DROP CONSTRAINT IF EXISTS ai_usos_custo_estimado_check;
ALTER TABLE plantaopro.ai_usos
    ADD CONSTRAINT ai_usos_custo_estimado_check
    CHECK (custo_estimado IS NULL OR custo_estimado >= 0);

ALTER TABLE plantaopro.ai_usos
    DROP CONSTRAINT IF EXISTS ai_usos_custo_confirmado_check;
ALTER TABLE plantaopro.ai_usos
    ADD CONSTRAINT ai_usos_custo_confirmado_check
    CHECK (custo_confirmado IS NULL OR custo_confirmado >= 0);

ALTER TABLE plantaopro.ai_usos
    DROP CONSTRAINT IF EXISTS ai_usos_task_code_check;
ALTER TABLE plantaopro.ai_usos
    ADD CONSTRAINT ai_usos_task_code_check
    CHECK (task_code IN ('MEU_DIA_RESUMO', 'COTACAO_ANALISE', 'TESTAR_CONEXAO'));

CREATE INDEX IF NOT EXISTS ix_ai_usos_incertos
    ON plantaopro.ai_usos (tenant_id, created_at)
    WHERE status = 'FALHA' AND custo_incerto AND reconciliado_em IS NULL;

-- 3) ai_precos_modelos ----------------------------------------------------------
CREATE TABLE IF NOT EXISTS plantaopro.ai_precos_modelos (
    provedor text NOT NULL CHECK (provedor IN ('groq', 'gemini', 'deepseek')),
    modelo text NOT NULL CHECK (char_length(modelo) BETWEEN 1 AND 64),
    moeda char(3) NOT NULL DEFAULT 'USD',
    preco_entrada_milhao numeric(14, 6) NOT NULL CHECK (preco_entrada_milhao >= 0),
    preco_saida_milhao numeric(14, 6) NOT NULL CHECK (preco_saida_milhao >= 0),
    versao_de date NOT NULL,
    versao_ate date,
    atualizado_em timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (provedor, modelo, versao_de)
);

CREATE INDEX IF NOT EXISTS ix_ai_precos_vigencia
    ON plantaopro.ai_precos_modelos (provedor, modelo, versao_de DESC);

-- Seed de preços por milhão de tokens (moeda USD), consultado em 2026-10-04:
--   Groq     : docs.groq.com (catálogo self-service; llama-* enterprise-only desde
--              2026-08-26 — última tarifa pública registrada tem versao_ate).
--   Gemini   : ai.google.dev/gemini-api/docs/pricing
--   DeepSeek : api-docs.deepseek.com (DeepSeek usa peak/off-peak; o seed registra
--              o PEAK — conservador — como preço vigente permanente).
INSERT INTO plantaopro.ai_precos_modelos
    (provedor, modelo, moeda, preco_entrada_milhao, preco_saida_milhao, versao_de, versao_ate)
VALUES
    -- Groq (self-service desde 2026-08-26):
    ('groq', 'gpt-oss-20b', 'USD', 0.075, 0.30, '2026-08-26', NULL),
    ('groq', 'gpt-oss-120b', 'USD', 0.15, 0.60, '2026-08-26', NULL),
    -- Última tarifa pública registrada (enterprise-only desde 2026-08-26 → sem preço vigente):
    ('groq', 'llama-3.3-70b-versatile', 'USD', 0.59, 0.79, '2026-01-01', '2026-08-25'),
    -- Gemini (tier base ≤ 200k tokens de entrada):
    ('gemini', 'gemini-2.5-flash', 'USD', 0.30, 2.50, '2025-06-17', NULL),
    ('gemini', 'gemini-2.5-flash-lite', 'USD', 0.10, 0.40, '2025-06-17', NULL),
    ('gemini', 'gemini-2.5-pro', 'USD', 1.25, 10.00, '2025-06-17', NULL),
    -- DeepSeek (preço peak; deepseek-chat/deepseek-reasoner aposentados em 2026-07-24):
    ('deepseek', 'deepseek-flash', 'USD', 0.30, 1.20, '2026-09-01', NULL),
    ('deepseek', 'deepseek-v4-pro', 'USD', 1.32, 3.96, '2026-09-01', NULL)
ON CONFLICT (provedor, modelo, versao_de) DO NOTHING;

-- 4) ai_chamadas_ativas ---------------------------------------------------------
CREATE TABLE IF NOT EXISTS plantaopro.ai_chamadas_ativas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    task_code text NOT NULL,
    criado_em timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ai_chamadas_ativas_criado
    ON plantaopro.ai_chamadas_ativas (criado_em);
