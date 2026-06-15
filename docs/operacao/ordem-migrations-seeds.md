# Ordem de migrations e seeds Saúde 360

Ordem obrigatória em ambientes novos ou Development:

1. `database/PlantaoPro_PostgreSQL_Completo.sql`.
2. Migrations estruturais antigas em `database/migrations/`.
3. Migrations Saúde 360 já existentes.
4. `database/migrations/2026_saude360_convenios_planos_saude.sql`.
5. `database/migrations/2026_saude360_financeiro_clinica.sql`.
6. `database/migrations/2026_saude360_cid_oficial.sql`.
7. Seeds base.
8. `database/seeds/2026_saude360_demo_convenios_financeiro_cid.sql`.

Seeds demo usam `to_regclass` antes de inserir para evitar erro `42P01` quando alguma tabela ainda não foi criada. Migrations permanecem idempotentes com `create table if not exists`, `alter table ... add column if not exists` e `create index if not exists`.
