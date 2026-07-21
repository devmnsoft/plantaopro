#!/usr/bin/env bash
set -euo pipefail
psql -v ON_ERROR_STOP=1 -f database/PlantaoPro_PostgreSQL_Completo.sql
psql -v ON_ERROR_STOP=1 -f database/migrations/2026_v113_operacional_real.sql
psql -v ON_ERROR_STOP=1 <<'SQL'
CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE TABLE IF NOT EXISTS plantaopro.upgrade_preservation_probe(
  entidade text PRIMARY KEY,
  marcador text NOT NULL
);
INSERT INTO plantaopro.upgrade_preservation_probe(entidade, marcador) VALUES
('cliente','cliente-v1182'),('tenant','tenant-v1182'),('usuario','usuario-v1182'),('medico','medico-v1182'),('hospital','hospital-v1182'),('especialidade','especialidade-v1182'),('plantao','plantao-v1182'),('escala','escala-v1182'),('pagamento','pagamento-v1182'),('paciente','paciente-v1182'),('agendamento','agendamento-v1182')
ON CONFLICT (entidade) DO UPDATE SET marcador=excluded.marcador;
SQL
scripts/apply-canonical-migrations.sh baseline
scripts/apply-canonical-migrations.sh upgrade
scripts/apply-canonical-migrations.sh upgrade
psql -v ON_ERROR_STOP=1 <<'SQL'
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM plantaopro.schema_migrations WHERE id='240_v117_runtime') THEN
    RAISE EXCEPTION 'upgrade did not reach v117 runtime migration';
  END IF;
  IF (SELECT COUNT(*) FROM plantaopro.upgrade_preservation_probe) <> 11 THEN
    RAISE EXCEPTION 'upgrade did not preserve legacy probe data';
  END IF;
  IF EXISTS (SELECT 1 FROM plantaopro.schema_migrations WHERE coalesce(checksum,'')='') THEN
    RAISE EXCEPTION 'empty checksum after upgrade';
  END IF;
END $$;
SQL
