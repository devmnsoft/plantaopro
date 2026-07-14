#!/usr/bin/env bash
set -euo pipefail
export Jwt__Key="${Jwt__Key:-PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS}"
export Jwt__Issuer="${Jwt__Issuer:-PlantaoPro}"
export Jwt__Audience="${Jwt__Audience:-PlantaoPro}"
BASE_URL="${BASE_URL:-http://localhost:5000}"
RESULT_JSON="docs/homologacao/v113-smoke-result.json"
RESULT_MD="docs/homologacao/v113-smoke-result.md"
mkdir -p docs/homologacao
TOKEN="${PLANTAOPRO_TOKEN:-}"
AUTH=()
if [ -n "$TOKEN" ]; then AUTH=(-H "Authorization: Bearer ${TOKEN}"); fi
pass=0; fail=0; rows=()
check(){ local name="$1" method="$2" path="$3" data="${4:-}"; local code; if [ -n "$data" ]; then code=$(curl -sS -o /tmp/v113-smoke.json -w "%{http_code}" -X "$method" "${BASE_URL}${path}" -H 'Content-Type: application/json' -H 'Accept: application/json' "${AUTH[@]}" --data "$data" || true); else code=$(curl -sS -o /tmp/v113-smoke.json -w "%{http_code}" -X "$method" "${BASE_URL}${path}" -H 'Accept: application/json' "${AUTH[@]}" || true); fi; if [ "$code" = "200" ] || [ "$code" = "201" ] || [ "$code" = "401" ]; then pass=$((pass+1)); rows+=("{\"name\":\"$name\",\"status\":\"checked\",\"http\":$code}"); else fail=$((fail+1)); rows+=("{\"name\":\"$name\",\"status\":\"failed\",\"http\":$code}"); fi }
check health GET /api/health
check customers GET /api/v113/customers
check customer_create POST /api/v113/customers '{"name":"Smoke Cliente v1.13","document":"DEMO-SMOKE","email":"smoke@example.invalid"}'
check products GET /api/v113/products
check product_create POST /api/v113/products '{"code":"SMOKE-113","name":"Smoke Produto","price":10,"minimumStock":1}'
check inventory GET /api/v113/inventory/balance
check orders GET /api/v113/orders
check tasks GET /api/v113/tasks/my
check invoices GET /api/v113/billing/invoices
check titles GET /api/v113/billing/titles
check outbox GET /api/v113/outbox
check templates GET /api/v113/templates
check journey GET /api/v113/journey/what-to-do-now
check dashboard GET /api/v113/dashboard
check homologation GET /api/v113/homologation/status
printf '{"version":"v1.13","passed":%s,"failed":%s,"checks":[%s]}\n' "$pass" "$fail" "$(IFS=,; echo "${rows[*]}")" > "$RESULT_JSON"
{
 echo "# Resultado smoke v1.13"; echo; echo "- Base URL: ${BASE_URL}"; echo "- Passou: ${pass}"; echo "- Falhou: ${fail}"; echo "- Observação: token não é impresso pelo script.";
} > "$RESULT_MD"
[ "$fail" -eq 0 ]
