#!/usr/bin/env bash
set -euo pipefail
scripts/apply-canonical-migrations.sh
scripts/apply-canonical-migrations.sh
psql -v ON_ERROR_STOP=1 <<'SQL'
SELECT 'schema' WHERE EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name='plantaopro');
SELECT COUNT(*) AS applied_migrations FROM plantaopro.schema_migrations;
SELECT COUNT(*) AS duplicated_tables FROM (
  SELECT table_schema, table_name, COUNT(*) FROM information_schema.tables WHERE table_schema='plantaopro' GROUP BY 1,2 HAVING COUNT(*) > 1
) d;
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM plantaopro.schema_migrations) THEN
    RAISE EXCEPTION 'no canonical migrations applied';
  END IF;
END $$;
SQL
