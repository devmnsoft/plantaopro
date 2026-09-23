#!/usr/bin/env bash
set -euo pipefail
psql -v ON_ERROR_STOP=1 -f database/fixtures/legacy-supported.sql
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
  IF NOT EXISTS (SELECT 1 FROM plantaopro.schema_migrations WHERE id='2026_v117_hardening_v116_runtime') THEN
    RAISE EXCEPTION 'upgrade did not reach v117 runtime migration';
  END IF;
  IF (SELECT COUNT(*) FROM plantaopro.upgrade_preservation_probe) <> 11 THEN
    RAISE EXCEPTION 'upgrade did not preserve legacy probe data';
  END IF;
  IF EXISTS (SELECT 1 FROM plantaopro.schema_migrations WHERE coalesce(checksum,'')='') THEN
    RAISE EXCEPTION 'empty checksum after upgrade';
  END IF;
  IF NOT EXISTS (
    SELECT 1 FROM plantaopro.usuarios u
    JOIN plantaopro.usuarios_perfis up ON up.usuario_id=u.id
    JOIN plantaopro.perfis p ON p.id=up.perfil_id
    WHERE u.id='20000000-0000-0000-0000-000000000001'
      AND u.email='legado.preservado@example.invalid'
      AND u.senha_hash='HASH_LEGADO_NAO_AUTENTICAVEL'
      AND p.id='10000000-0000-0000-0000-000000000001'
      AND p.nome='Perfil legado preservado'
  ) THEN
    RAISE EXCEPTION 'upgrade did not preserve legacy user/profile relationship';
  END IF;
  IF NOT EXISTS (
    SELECT 1 FROM plantaopro.plantoes
    WHERE id='40000000-0000-0000-0000-000000000001'
      AND valor=1575.50 AND vagas=2 AND status='ABERTO'
  ) THEN
    RAISE EXCEPTION 'upgrade did not preserve legacy shift values';
  END IF;
  IF to_regclass('plantaopro.notifications') IS NULL
     OR to_regclass('plantaopro.notification_preferences') IS NULL THEN
    RAISE EXCEPTION 'notification base was not repaired before v2.07.0';
  END IF;
END $$;
SQL
