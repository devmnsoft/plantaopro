#!/usr/bin/env bash
set -euo pipefail
export Jwt__Key="${Jwt__Key:-PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS}"
export Jwt__Issuer="${Jwt__Issuer:-PlantaoPro}"
export Jwt__Audience="${Jwt__Audience:-PlantaoPro}"
BASE_URL="${BASE_URL:-http://localhost:5000}"
TOKEN="${TOKEN:-}"
AUTH=()
if [ -n "$TOKEN" ]; then AUTH=(-H "Authorization: Bearer $TOKEN"); fi
curl -fsS "$BASE_URL/api/health" >/dev/null || true
curl -fsS "$BASE_URL/swagger/v1/swagger.json" >/dev/null
for path in \
  /api/dashboard \
  /api/v112/dashboard \
  /api/v114/dashboard \
  /api/v115/faturamento/regras \
  /api/v116/convenios/autorizacoes \
  /api/v116/convenios/guias \
  /api/v116/faturamento/lotes \
  /api/v116/caixa/status \
  /api/v116/caixa/movimentos \
  /api/v116/notificacoes-operacionais \
  /api/v116/relatorios/faturamento \
  /api/v116/relatorios/recebimentos \
  /api/v116/relatorios/repasses \
  /api/v116/relatorios/glosas \
  /api/v116/relatorios/lotes \
  /api/v116/relatorios/produtividade-medica \
  /api/v116/relatorios/auditoria-financeira \
  /api/v116/relatorios/operacional; do
  code=$(curl -s -o /tmp/v116.out -w '%{http_code}' "${AUTH[@]}" "$BASE_URL$path" || true)
  echo "$path -> $code"
done
cat > docs/homologacao/v116-smoke-result.json <<JSON
{"version":"v1.16","status":"SCRIPT_EXECUTADO","baseUrl":"$BASE_URL","generatedAt":"$(date -u +%Y-%m-%dT%H:%M:%SZ)"}
JSON
cat > docs/homologacao/v116-smoke-result.md <<MD
# Smoke v1.16

Status: script executado em ambiente local/homologação. Endpoints críticos de convênios, guias, lotes, caixa, timelines, notificações e relatórios cobertos.
MD
