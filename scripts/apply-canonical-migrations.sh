#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

psql -v ON_ERROR_STOP=1 <<'SQL'
CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE TABLE IF NOT EXISTS plantaopro.schema_migrations (
    id text PRIMARY KEY,
    script_path text NOT NULL,
    checksum text NOT NULL,
    applied_at timestamptz NOT NULL DEFAULT now()
);
SQL

apply_script() {
    local id="$1"
    local relative_path="$2"
    local path="$ROOT_DIR/$relative_path"

    if [[ ! -f "$path" ]]; then
        echo "Migration script not found: $relative_path" >&2
        exit 1
    fi

    local checksum
    checksum="$(sha256sum "$path" | awk '{print $1}')"

    local already_applied
    already_applied="$(psql -v ON_ERROR_STOP=1 -At -c "SELECT 1 FROM plantaopro.schema_migrations WHERE id = '$id' LIMIT 1;")"
    if [[ "$already_applied" == "1" ]]; then
        echo "Skipping already applied migration: $id ($relative_path)"
        return 0
    fi

    echo "Applying migration: $id ($relative_path)"
    psql -v ON_ERROR_STOP=1 -f "$path"
    psql -v ON_ERROR_STOP=1 -c "INSERT INTO plantaopro.schema_migrations (id, script_path, checksum) VALUES ('$id', '$relative_path', '$checksum');"
}

apply_script "000_base_schema" "database/PlantaoPro_PostgreSQL_Completo.sql"
apply_script "010_common_saas_premium" "database/20260525_evolucao_saas_premium.sql"
apply_script "020_common_self_service_white_label" "database/migrations/2026_plantao_pro_self_service_white_label.sql"
apply_script "030_common_white_label_self_service" "database/migrations/2026_plantao_pro_white_label_self_service.sql"
apply_script "040_saude360_base_minima" "database/migrations/2026_saude360_base_clinica_minima.sql"
apply_script "050_saude360_base" "database/migrations/2026_plantao_pro_saude360_base_clinica.sql"
apply_script "060_saude360_modulos" "database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql"
apply_script "070_saude360_consultas" "database/migrations/2026_saude360_consultas_base.sql"
apply_script "080_saude360_cid_prescricao" "database/migrations/2026_saude360_cid_prescricao.sql"
apply_script "090_saude360_convenios" "database/migrations/2026_saude360_convenios_planos_saude.sql"
apply_script "100_saude360_financeiro" "database/migrations/2026_saude360_financeiro_clinica.sql"
apply_script "110_saas_inteligente" "database/migrations/2026_plantao_pro_saas_inteligente.sql"
apply_script "120_saas_inteligente_funcional" "database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql"
apply_script "130_saas_auditavel" "database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql"
apply_script "140_saas_comercial_core" "database/migrations/2026_saas_comercial_core.sql"
apply_script "200_v113_operacional" "database/migrations/2026_v113_operacional_real.sql"
apply_script "210_v114_consolidacao" "database/migrations/2026_v114_consolidacao_produto.sql"
apply_script "220_v115_faturamento" "database/migrations/2026_v115_regras_faturamento_repasses.sql"
apply_script "230_v116_operacional" "database/migrations/2026_v116_consolidacao_operacional_final.sql"
apply_script "240_v117_runtime" "database/migrations/2026_v117_hardening_v116_runtime.sql"
apply_script "900_seed_v113" "database/seeds/2026_demo_v113_operacional.sql"
apply_script "910_seed_v114" "database/seeds/2026_demo_v114_consolidacao_produto.sql"
apply_script "920_seed_v115" "database/seeds/2026_demo_v115_regras_faturamento.sql"
apply_script "930_seed_v116" "database/seeds/2026_demo_v116_consolidacao_operacional.sql"
apply_script "940_seed_v117" "database/seeds/2026_demo_v117_runtime_integrado.sql"
