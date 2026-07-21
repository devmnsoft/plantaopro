#!/usr/bin/env bash
set -euo pipefail
scripts/apply-canonical-migrations.sh install
scripts/apply-canonical-migrations.sh upgrade
psql -v ON_ERROR_STOP=1 <<'SQL'
SELECT id, script_path, checksum, applied_at FROM plantaopro.schema_migrations ORDER BY id;
DO $$
DECLARE
  missing_count int;
BEGIN
  IF EXISTS (SELECT 1 FROM plantaopro.schema_migrations WHERE coalesce(checksum,'')='') THEN
    RAISE EXCEPTION 'schema_migrations contains empty checksum';
  END IF;
  IF EXISTS (SELECT id FROM plantaopro.schema_migrations GROUP BY id HAVING COUNT(*) > 1) THEN
    RAISE EXCEPTION 'schema_migrations contains duplicated migration id';
  END IF;
  SELECT count(*) INTO missing_count
  FROM (VALUES ('pacientes'),('agendamentos'),('triagens'),('consultas'),('cid_tabela'),('prescricoes'),('clinica_contas_receber')) AS required(table_name)
  WHERE NOT EXISTS (SELECT 1 FROM information_schema.tables t WHERE t.table_schema='plantaopro' AND t.table_name=required.table_name);
  IF missing_count > 0 THEN
    RAISE EXCEPTION 'required clinical tables are missing: %', missing_count;
  END IF;
END $$;
SQL
