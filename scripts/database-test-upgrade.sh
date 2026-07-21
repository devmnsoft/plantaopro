#!/usr/bin/env bash
set -euo pipefail
psql -v ON_ERROR_STOP=1 -f database/PlantaoPro_PostgreSQL_Completo.sql
psql -v ON_ERROR_STOP=1 -f database/migrations/2026_v113_operacional_real.sql
psql -v ON_ERROR_STOP=1 <<'SQL'
CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE TABLE IF NOT EXISTS plantaopro.schema_migrations (
    id text PRIMARY KEY,
    script_path text NOT NULL,
    checksum text NOT NULL,
    applied_at timestamptz NOT NULL DEFAULT now()
);
SQL
scripts/apply-canonical-migrations.sh
scripts/apply-canonical-migrations.sh
psql -v ON_ERROR_STOP=1 <<'SQL'
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM plantaopro.schema_migrations WHERE id='240_v117_runtime') THEN
    RAISE EXCEPTION 'upgrade did not reach v117 runtime migration';
  END IF;
END $$;
SQL
