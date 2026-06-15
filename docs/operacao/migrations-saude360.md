# Migrations Saúde 360

Para corrigir CID e financeiro clínico, aplique em ordem:

1. `database/migrations/2026_fix_cid_tabela_schema.sql`
2. `database/migrations/2026_fix_clinica_financeiro_minimo.sql`
3. `database/seeds/2026_demo_saude360_complementar.sql` quando desejar dados demo.

As scripts são idempotentes e não apagam dados existentes.
