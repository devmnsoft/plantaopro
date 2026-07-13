#!/usr/bin/env bash
set -euo pipefail

HOST="${PGHOST:-localhost}"
PORT="${PGPORT:-5432}"
DATABASE="${PGDATABASE:-plantaopro}"
USER="${PGUSER:-postgres}"
PASSWORD="${PGPASSWORD:-CHANGE_ME_LOCAL_POSTGRES}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --host) HOST="$2"; shift 2 ;;
    --port) PORT="$2"; shift 2 ;;
    --database|--db) DATABASE="$2"; shift 2 ;;
    --user) USER="$2"; shift 2 ;;
    --password) PASSWORD="$2"; shift 2 ;;
    *) echo "Parâmetro desconhecido: $1" >&2; exit 2 ;;
  esac
done

if ! command -v psql >/dev/null 2>&1; then
  echo "psql não encontrado. Instale o cliente PostgreSQL antes de aplicar o banco." >&2
  exit 127
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
run_sql() {
  local file="$1"
  [[ -f "$file" ]] || return 0
  echo "==> Aplicando ${file#$ROOT_DIR/}"
  PGPASSWORD="$PASSWORD" psql -v ON_ERROR_STOP=1 -h "$HOST" -p "$PORT" -U "$USER" -d "$DATABASE" -f "$file"
}

run_sql "$ROOT_DIR/database/PlantaoPro_PostgreSQL_Completo.sql"
if compgen -G "$ROOT_DIR/database/migrations/*.sql" > /dev/null; then
  while IFS= read -r file; do run_sql "$file"; done < <(find "$ROOT_DIR/database/migrations" -maxdepth 1 -type f -name '*.sql' | sort)
fi
run_sql "$ROOT_DIR/database/seeds.sql"
if compgen -G "$ROOT_DIR/database/seeds/*.sql" > /dev/null; then
  while IFS= read -r file; do run_sql "$file"; done < <(find "$ROOT_DIR/database/seeds" -maxdepth 1 -type f -name '*.sql' | sort)
fi

echo "Banco local PlantãoPro aplicado com sucesso em $HOST:$PORT/$DATABASE."
