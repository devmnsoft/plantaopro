#!/usr/bin/env bash
set -euo pipefail
set +x
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
command -v psql >/dev/null || { echo 'psql 16+ não encontrado.' >&2; exit 2; }
command -v dotnet >/dev/null || { echo 'dotnet não encontrado.' >&2; exit 2; }
[[ "$(psql --version | sed -E 's/.* ([0-9]+).*/\1/')" -ge 16 ]] || { echo 'psql 16+ é obrigatório.' >&2; exit 2; }
: "${PGDATABASE:=postgres}" "${PLANTAOPRO_DATABASE:=plantaopro}" "${PLANTAOPRO_OWNER:=plantaopro_owner}" "${PLANTAOPRO_APP_ROLE:=plantaopro_app}"
: "${PLANTAOPRO_ENVIRONMENT:=Development}" "${PLANTAOPRO_INSTALL_MODE:=UPGRADE}" "${PLANTAOPRO_ADMIN_EMAIL:=admin.global@plantaopro.local}"
if [[ -z "${PLANTAOPRO_APP_PASSWORD:-}" ]]; then read -r -s -p 'Senha da role da aplicação: ' PLANTAOPRO_APP_PASSWORD; echo >&2; fi
if [[ -z "${PLANTAOPRO_BOOTSTRAP_PASSWORD_HASH:-}" ]]; then
  if [[ -z "${PLANTAOPRO_BOOTSTRAP_PASSWORD:-}" ]]; then read -r -s -p 'Senha inicial do superadministrador: ' PLANTAOPRO_BOOTSTRAP_PASSWORD; echo >&2; fi
  export PLANTAOPRO_BOOTSTRAP_PASSWORD
  PLANTAOPRO_BOOTSTRAP_PASSWORD_HASH="$(dotnet run --project "$root/backend/PlantaoPro.Tools.Bootstrap/PlantaoPro.Tools.Bootstrap.csproj" -- hash-password)"
fi
trap 'unset PLANTAOPRO_APP_PASSWORD PLANTAOPRO_BOOTSTRAP_PASSWORD PLANTAOPRO_BOOTSTRAP_PASSWORD_HASH' EXIT
psql -X -d "$PGDATABASE" -v ON_ERROR_STOP=1 \
 -v installation_environment="$PLANTAOPRO_ENVIRONMENT" -v install_mode="$PLANTAOPRO_INSTALL_MODE" \
 -v maintenance_database="$PGDATABASE" -v target_database="$PLANTAOPRO_DATABASE" -v database_owner="$PLANTAOPRO_OWNER" \
 -v application_role="$PLANTAOPRO_APP_ROLE" -v application_role_password="$PLANTAOPRO_APP_PASSWORD" \
 -v bootstrap_admin=true -v bootstrap_admin_email="$PLANTAOPRO_ADMIN_EMAIL" \
 -v bootstrap_admin_password_hash="$PLANTAOPRO_BOOTSTRAP_PASSWORD_HASH" -f "$root/database/instalar_plantaopro.psql"
mkdir -p "$root/.local"; umask 077
jwt="$(python3 -c 'import secrets; print(secrets.token_urlsafe(64))')"
cat > "$root/.local/plantaopro.env" <<ENV
ConnectionStrings__Default=Host=${PGHOST:-localhost};Port=${PGPORT:-5432};Database=$PLANTAOPRO_DATABASE;Username=$PLANTAOPRO_APP_ROLE;Password=$PLANTAOPRO_APP_PASSWORD
Jwt__Issuer=PlantaoPro
Jwt__Audience=PlantaoPro
Jwt__Key=$jwt
ASPNETCORE_ENVIRONMENT=$PLANTAOPRO_ENVIRONMENT
ENV
chmod 600 "$root/.local/plantaopro.env"
echo "PlantãoPro — instalação concluída; banco=$PLANTAOPRO_DATABASE; servidor=${PGHOST:-localhost}; porta=${PGPORT:-5432}; usuário=$PLANTAOPRO_APP_ROLE; ambiente=$root/.local/plantaopro.env; status=APROVADO"
