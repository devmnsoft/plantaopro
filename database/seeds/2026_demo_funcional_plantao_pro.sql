-- Seed demo funcional PlantãoPro: idempotente, sem dados reais.
-- Execute após migrations base Saúde 360.
DO $$
BEGIN
    RAISE NOTICE 'Seed demo funcional PlantãoPro deve ser aplicado após criação das tabelas assistenciais, financeiras e SaaS.';
END $$;
