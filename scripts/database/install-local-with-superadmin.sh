#!/usr/bin/env bash
set -euo pipefail
set +x
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
: "${PLANTAOPRO_CONNECTION_STRING:?Defina PLANTAOPRO_CONNECTION_STRING}"
: "${PLANTAOPRO_BOOTSTRAP_PASSWORD:?Defina PLANTAOPRO_BOOTSTRAP_PASSWORD em um secret local}"
export PLANTAOPRO_BOOTSTRAP_ENVIRONMENT="${PLANTAOPRO_BOOTSTRAP_ENVIRONMENT:-Development}"
export PLANTAOPRO_BOOTSTRAP_ADMIN_EMAIL="${PLANTAOPRO_BOOTSTRAP_ADMIN_EMAIL:-admin.global@plantaopro.local}"
export PLANTAOPRO_BOOTSTRAP_ADMIN_NAME="${PLANTAOPRO_BOOTSTRAP_ADMIN_NAME:-Super Administrador PlantãoPro}"
export PLANTAOPRO_BOOTSTRAP_FORCE_ROTATION="${PLANTAOPRO_BOOTSTRAP_FORCE_ROTATION:-true}"
hash="${PLANTAOPRO_BOOTSTRAP_PASSWORD_HASH:-}"
if [[ -z "$hash" ]]; then
  hash="$(dotnet run --project "$root/backend/PlantaoPro.Tools.Bootstrap/PlantaoPro.Tools.Bootstrap.csproj" -- hash-password)"
fi
trap 'unset hash PLANTAOPRO_BOOTSTRAP_PASSWORD PLANTAOPRO_BOOTSTRAP_PASSWORD_HASH' EXIT
psql "$PLANTAOPRO_CONNECTION_STRING" -X -v ON_ERROR_STOP=1 \
  -v bootstrap_environment="$PLANTAOPRO_BOOTSTRAP_ENVIRONMENT" \
  -v bootstrap_admin_email="$PLANTAOPRO_BOOTSTRAP_ADMIN_EMAIL" \
  -v bootstrap_admin_name="$PLANTAOPRO_BOOTSTRAP_ADMIN_NAME" \
  -v bootstrap_admin_password_hash="$hash" \
  -v bootstrap_force_rotation="$PLANTAOPRO_BOOTSTRAP_FORCE_ROTATION" \
  -f "$root/database/scrpt_completo.sql"
psql "$PLANTAOPRO_CONNECTION_STRING" -X -v ON_ERROR_STOP=1 \
  -v bootstrap_admin_email="$PLANTAOPRO_BOOTSTRAP_ADMIN_EMAIL" \
  -f "$root/scripts/database/verify-superadmin.sql"
echo "Instalação local concluída; credenciais e hashes não foram exibidos."
