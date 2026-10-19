-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2356_r62_ai_provedor_drael
-- R62 (r53b1 §7): Drael (drael.sh) entra como provider dedicado de IA — a IA da
-- casa, com o mesmo contrato canônico dos demais provedores (chave do tenant
-- prioritária sobre a global, auditoria em ai_usos, governança de modelo).
--
--   1. ai_config: estende os checks de provedor/fallback_provedor com 'drael'
--      (as colunas já existem; apenas o domínio é ampliado).
--   2. ai_precos_modelos: estende o check de provedor com 'drael', permitindo
--      que a operação versione o preço do drael-v1 quando publicado pela
--      Drael (sem preço vigente o sistema já trata como custo incerto —
--      a aplicação NÃO inventa preço).
--
-- Idempotência (contrato de reaplicação): DROP/ADD CONSTRAINT com nome fixo.
-- Reaplicar sobre o schema atual não altera dados.
-- ============================================================================

ALTER TABLE plantaopro.ai_config
    DROP CONSTRAINT IF EXISTS ai_config_provedor_check;
ALTER TABLE plantaopro.ai_config
    ADD CONSTRAINT ai_config_provedor_check
    CHECK (provedor IN ('groq', 'gemini', 'deepseek', 'drael'));

ALTER TABLE plantaopro.ai_config
    DROP CONSTRAINT IF EXISTS ai_config_fallback_provedor_check;
ALTER TABLE plantaopro.ai_config
    ADD CONSTRAINT ai_config_fallback_provedor_check
    CHECK (fallback_provedor IS NULL OR fallback_provedor IN ('groq', 'gemini', 'deepseek', 'drael'));

ALTER TABLE plantaopro.ai_precos_modelos
    DROP CONSTRAINT IF EXISTS ai_precos_modelos_provedor_check;
ALTER TABLE plantaopro.ai_precos_modelos
    ADD CONSTRAINT ai_precos_modelos_provedor_check
    CHECK (provedor IN ('groq', 'gemini', 'deepseek', 'drael'));
