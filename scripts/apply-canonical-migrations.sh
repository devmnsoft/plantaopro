#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APPLY_DEMO_SEEDS="${APPLY_DEMO_SEEDS:-0}"
MODE="${1:-install}"
if [[ "$MODE" != "install" && "$MODE" != "baseline" && "$MODE" != "upgrade" ]]; then
    echo "Usage: $0 install|baseline|upgrade" >&2
    exit 64
fi

psql -v ON_ERROR_STOP=1 <<'SQL'
CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE TABLE IF NOT EXISTS plantaopro.schema_migrations (
    id text PRIMARY KEY,
    script_path text NOT NULL,
    checksum text NOT NULL,
    applied_at timestamptz NOT NULL DEFAULT now()
);
SQL

sql_literal() { printf "%s" "$1" | sed "s/'/''/g"; }

if [[ "$MODE" == "install" ]]; then
    echo "Installing from database/scrpt_completo.sql"
    psql -v ON_ERROR_STOP=1 -f "$ROOT_DIR/database/scrpt_completo.sql"
    exit 0
fi

validate_baseline_object() {
    local object_name="$1"
    local exists
    exists="$(psql -v ON_ERROR_STOP=1 -At -c "SELECT EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='plantaopro' AND c.relname='${object_name}');")"
    if [[ "$exists" != "t" ]]; then
        echo "Baseline validation failed: missing plantaopro.${object_name}" >&2
        exit 3
    fi
}

if [[ "$MODE" == "baseline" ]]; then
    BASELINE_VERSION="${BASELINE_VERSION:-legacy-core}"
    BASELINE_FILE="$ROOT_DIR/database/baselines/${BASELINE_VERSION}.json"
    if [[ ! -f "$BASELINE_FILE" ]]; then
        echo "Baseline desconhecido: $BASELINE_VERSION" >&2
        exit 3
    fi
    mapfile -t REQUIRED_OBJECTS < <(python3 - "$BASELINE_FILE" <<'PYBASELINE'
import json, sys
for name in json.load(open(sys.argv[1], encoding='utf-8')).get('requiredObjects', []):
    print(name.split('.')[-1])
PYBASELINE
)
    for required_object in "${REQUIRED_OBJECTS[@]}"; do
        validate_baseline_object "$required_object"
    done
fi

MIGRATION_IDS=()
MIGRATION_PATHS=()
MIGRATION_TRANSACTIONAL=()
MIGRATION_CATEGORY=()

register_migration() {
    MIGRATION_IDS+=("$1")
    MIGRATION_PATHS+=("$2")
    MIGRATION_TRANSACTIONAL+=("$3")
    MIGRATION_CATEGORY+=("$4")
}

apply_script() {
    local id="$1"
    local relative_path="$2"
    local transactional="${3:-true}"
    local category="${4:-operacional}"
    local path="$ROOT_DIR/$relative_path"

    if [[ ! -f "$path" ]]; then
        echo "Migration script not found: $relative_path" >&2
        exit 1
    fi

    local checksum escaped_id escaped_path stored_checksum tmp_sql
    checksum="$(sha256sum "$path" | awk '{print $1}')"
    escaped_id="$(sql_literal "$id")"
    escaped_path="$(sql_literal "$relative_path")"

    stored_checksum="$(psql -v ON_ERROR_STOP=1 -At -c "SELECT checksum FROM plantaopro.schema_migrations WHERE id = '$escaped_id' LIMIT 1;")"
    if [[ -n "$stored_checksum" ]]; then
        if [[ "$stored_checksum" != "$checksum" ]]; then
            echo "Checksum mismatch for migration $id ($relative_path). Stored=$stored_checksum Current=$checksum" >&2
            exit 2
        fi
        echo "Skipping already applied migration: $id ($relative_path)"
        return 0
    fi

    if [[ "$MODE" == "baseline" ]]; then
        echo "Baselining validated migration: $id ($relative_path, transactional=$transactional, category=$category)"
        psql -v ON_ERROR_STOP=1 -c "INSERT INTO plantaopro.schema_migrations (id, script_path, checksum) VALUES ('$escaped_id', '$escaped_path', '$checksum');"
        return 0
    fi

    echo "Applying migration: $id ($relative_path, transactional=$transactional, category=$category)"
    tmp_sql="$(mktemp)"
    if [[ "$transactional" == "true" ]]; then
        {
            echo "BEGIN;"
            printf '\\i %s\n' "$path"
            printf "INSERT INTO plantaopro.schema_migrations (id, script_path, checksum) VALUES ('%s', '%s', '%s');\n" "$escaped_id" "$escaped_path" "$checksum"
            echo "COMMIT;"
        } > "$tmp_sql"
    else
        {
            printf '\\i %s\n' "$path"
            printf "INSERT INTO plantaopro.schema_migrations (id, script_path, checksum) VALUES ('%s', '%s', '%s');\n" "$escaped_id" "$escaped_path" "$checksum"
        } > "$tmp_sql"
    fi
    psql -v ON_ERROR_STOP=1 -f "$tmp_sql"
    rm -f "$tmp_sql"
}

apply_script "000_base_schema" "database/PlantaoPro_PostgreSQL_Completo.sql" false base
apply_script "010_common_saas_premium" "database/20260525_evolucao_saas_premium.sql" true saas
apply_script "020_common_self_service_white_label" "database/migrations/2026_plantao_pro_self_service_white_label.sql" true saas
apply_script "030_common_white_label_self_service" "database/migrations/2026_plantao_pro_white_label_self_service.sql" true saas
apply_script "040_saude360_base_minima" "database/migrations/2026_saude360_base_clinica_minima.sql" true clinico
apply_script "050_saude360_base" "database/migrations/2026_plantao_pro_saude360_base_clinica.sql" true clinico
apply_script "060_saude360_modulos" "database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql" true clinico
apply_script "070_saude360_consultas" "database/migrations/2026_saude360_consultas_base.sql" true clinico
apply_script "080_saude360_cid_prescricao" "database/migrations/2026_saude360_cid_prescricao.sql" true clinico
apply_script "090_saude360_convenios" "database/migrations/2026_saude360_convenios_planos_saude.sql" true clinico
apply_script "100_saude360_financeiro" "database/migrations/2026_saude360_financeiro_clinica.sql" true clinico
apply_script "110_saas_inteligente" "database/migrations/2026_plantao_pro_saas_inteligente.sql" true saas
apply_script "120_saas_inteligente_funcional" "database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql" true saas
apply_script "130_saas_auditavel" "database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql" true saas
apply_script "140_saas_comercial_core" "database/migrations/2026_saas_comercial_core.sql" true saas
apply_script "200_v113_operacional" "database/migrations/2026_v113_operacional_real.sql" true operacional
apply_script "210_v114_consolidacao" "database/migrations/2026_v114_consolidacao_produto.sql" true operacional
apply_script "220_v115_faturamento" "database/migrations/2026_v115_regras_faturamento_repasses.sql" true operacional
apply_script "230_v116_operacional" "database/migrations/2026_v116_consolidacao_operacional_final.sql" true operacional
apply_script "240_v117_runtime" "database/migrations/2026_v117_hardening_v116_runtime.sql" true operacional

if [[ "$APPLY_DEMO_SEEDS" == "1" ]]; then
    apply_script "900_seed_v113" "database/seeds/2026_demo_v113_operacional.sql"
    apply_script "910_seed_v114" "database/seeds/2026_demo_v114_consolidacao_produto.sql"
    apply_script "920_seed_v115" "database/seeds/2026_demo_v115_regras_faturamento.sql"
    apply_script "930_seed_v116" "database/seeds/2026_demo_v116_consolidacao_operacional.sql"
    apply_script "940_seed_v117" "database/seeds/2026_demo_v117_runtime_integrado.sql"
fi
