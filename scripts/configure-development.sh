#!/usr/bin/env bash
set -euo pipefail
API_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../backend/PlantaoPro.Api" && pwd)"
cd "$API_DIR"
: "${PLANTAOPRO_CONNECTION_STRING:?Defina PLANTAOPRO_CONNECTION_STRING sem commitar segredos}"
: "${PLANTAOPRO_JWT_KEY:?Defina PLANTAOPRO_JWT_KEY com 32+ caracteres}"
dotnet user-secrets set "ConnectionStrings:Default" "$PLANTAOPRO_CONNECTION_STRING"
dotnet user-secrets set "Jwt:Key" "$PLANTAOPRO_JWT_KEY"
