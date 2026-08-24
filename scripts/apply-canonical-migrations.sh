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

    if [[ "$id" == "2026_v187_fechamento_operacional_financeiro" ]]; then
        local fechamento_plantao
        fechamento_plantao="$(psql -v ON_ERROR_STOP=1 -At -c "SELECT to_regclass('plantaopro.fechamento_plantao');")"
        if [[ -z "$fechamento_plantao" ]]; then
            echo "Migration precondition failed: plantaopro.fechamento_plantao must exist before $id" >&2
            exit 3
        fi
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
while IFS=$'\t' read -r migration_id migration_path transactional; do
    apply_script "$migration_id" "$migration_path" "$transactional" migration
 done < <(python3 - "$ROOT_DIR/database/migration-manifest.json" "$MODE" "${BASELINE_FILE:-}" <<'PYMANIFEST'
import json, os, sys
manifest=json.load(open(sys.argv[1],encoding='utf-8'))
items=[item for item in manifest['migrations'] if item.get('status') == 'active']
if sys.argv[2] == 'baseline':
    baseline=json.load(open(sys.argv[3],encoding='utf-8'))
    next_source=baseline.get('nextMigration')
    matches=[i for i,item in enumerate(items) if os.path.basename(item['source']) == next_source]
    if len(matches) != 1:
        raise SystemExit(f"Baseline nextMigration inválida ou ambígua: {next_source!r}")
    items=items[:matches[0]]
for item in items:
    print(item['version'],item['source'],str(item.get('transactional',True)).lower(),sep='\t')
PYMANIFEST
)

if [[ "$APPLY_DEMO_SEEDS" == "1" ]]; then
    apply_script "900_seed_v113" "database/seeds/2026_demo_v113_operacional.sql"
    apply_script "910_seed_v114" "database/seeds/2026_demo_v114_consolidacao_produto.sql"
    apply_script "920_seed_v115" "database/seeds/2026_demo_v115_regras_faturamento.sql"
    apply_script "930_seed_v116" "database/seeds/2026_demo_v116_consolidacao_operacional.sql"
    apply_script "940_seed_v117" "database/seeds/2026_demo_v117_runtime_integrado.sql"
fi
