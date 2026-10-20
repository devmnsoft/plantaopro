-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2361_r71c_ai_provedor_mistral
-- R71C (pedido do usuario, fora do plano dos 50 sprints): Mistral (api.mistral.ai)
-- entra como provider dedicado na camada canônica de IA — provedor comercial com
-- PLANO GRATUITO na La Plateforme (chave gratuita, sem cartão de crédito;
-- console.mistral.ai), wire format compatível com OpenAI e o MESMO contrato
-- dos demais provedores (chave do tenant prioritária sobre a global, auditoria
-- em ai_usos, governança de modelo por prefixo de família).
--
--   1. ai_config: estende os checks de provedor/fallback_provedor com 'mistral'
--      (as colunas já existem; apenas o domínio é ampliado).
--   2. ai_precos_modelos: estende o check de provedor com 'mistral', permitindo
--      que a operação versione o preço dos modelos quando publicado pela
--      Mistral (sem preço vigente o sistema já trata como custo incerto —
--      a aplicação não inventa preço).
--
-- Idempotência (contrato de reaplicação): DROP/ADD CONSTRAINT com nome fixo.
-- Reaplicar sobre o schema atual não altera dados.
-- ============================================================================

ALTER TABLE plantaopro.ai_config
    DROP CONSTRAINT IF EXISTS ai_config_provedor_check;
ALTER TABLE plantaopro.ai_config
    ADD CONSTRAINT ai_config_provedor_check
    CHECK (provedor IN ('groq', 'gemini', 'deepseek', 'drael', 'mistral'));

ALTER TABLE plantaopro.ai_config
    DROP CONSTRAINT IF EXISTS ai_config_fallback_provedor_check;
ALTER TABLE plantaopro.ai_config
    ADD CONSTRAINT ai_config_fallback_provedor_check
    CHECK (fallback_provedor IS NULL OR fallback_provedor IN ('groq', 'gemini', 'deepseek', 'drael', 'mistral'));

ALTER TABLE plantaopro.ai_precos_modelos
    DROP CONSTRAINT IF EXISTS ai_precos_modelos_provedor_check;
ALTER TABLE plantaopro.ai_precos_modelos
    ADD CONSTRAINT ai_precos_modelos_provedor_check
    CHECK (provedor IN ('groq', 'gemini', 'deepseek', 'drael', 'mistral'));
