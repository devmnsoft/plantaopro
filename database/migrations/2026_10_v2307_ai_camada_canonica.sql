-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2307_ai_camada_canonica
-- P2 IA: camada canônica de assistentes — configuração por tenant/tarefa
--        e auditoria de usos.
--
--   1. ai_config  : 1 linha por (tenant, tarefa). Habilitação por tenant,
--      provedor/modelo por tarefa, fallback explícito (destino aprovado),
--      chave do tenant cifrada no servidor (AES-GCM; o texto puro nunca é
--      gravado), limites de tokens/timeout/cota mensal e orçamento mensal.
--   2. ai_usos    : auditoria imutável de cada geração (sucesso ou falha):
--      quem, qual tarefa, qual provedor/modelo, tokens, duração, classe de
--      erro. Não guarda o texto do prompt nem da resposta (minimização:
--      apenas o identificador do contexto, nunca o conteúdo).
--
-- Idempotência: CREATE TABLE/INDEX IF NOT EXISTS; sem alteração de dados.
-- Sem FK de tenant: segue a convenção das demais tabelas operacionais
-- (ex.: tenant_modulos) que referenciam o tenant por uuid sem constraint.
-- ============================================================================

CREATE TABLE IF NOT EXISTS plantaopro.ai_config (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    task_code text NOT NULL CHECK (task_code IN ('MEU_DIA_RESUMO', 'COTACAO_ANALISE')),
    habilitada boolean NOT NULL DEFAULT FALSE,
    provedor text NOT NULL CHECK (provedor IN ('groq', 'gemini', 'deepseek')),
    modelo text NOT NULL CHECK (char_length(modelo) BETWEEN 1 AND 64),
    fallback_provedor text CHECK (fallback_provedor IN ('groq', 'gemini', 'deepseek')),
    api_key_cifrada bytea,
    chave_mascara text,
    limite_tokens_entrada integer NOT NULL DEFAULT 4000 CHECK (limite_tokens_entrada BETWEEN 256 AND 16000),
    limite_tokens_saida integer NOT NULL DEFAULT 1200 CHECK (limite_tokens_saida BETWEEN 64 AND 8000),
    timeout_s integer NOT NULL DEFAULT 30 CHECK (timeout_s BETWEEN 5 AND 120),
    cota_mensal_usos integer NOT NULL DEFAULT 100 CHECK (cota_mensal_usos BETWEEN 1 AND 100000),
    orcamento_mensal numeric(14, 2) CHECK (orcamento_mensal IS NULL OR orcamento_mensal >= 0),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (tenant_id, task_code)
);

CREATE TABLE IF NOT EXISTS plantaopro.ai_usos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    user_id uuid,
    task_code text NOT NULL CHECK (task_code IN ('MEU_DIA_RESUMO', 'COTACAO_ANALISE')),
    contexto_tipo text,
    contexto_id uuid,
    provedor text,
    modelo text,
    status text NOT NULL CHECK (status IN ('SUCESSO', 'FALHA')),
    erro_classe text,
    tokens_entrada integer,
    tokens_saida integer,
    duracao_ms integer,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ai_config_tenant ON plantaopro.ai_config (tenant_id);
CREATE INDEX IF NOT EXISTS ix_ai_usos_tenant_task_date ON plantaopro.ai_usos (tenant_id, task_code, created_at);
CREATE INDEX IF NOT EXISTS ix_ai_usos_tenant_date ON plantaopro.ai_usos (tenant_id, created_at);
