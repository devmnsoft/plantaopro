#!/usr/bin/env bash
set -euo pipefail
BASE_URL="${BASE_URL:-http://localhost:5000}"
RESULT_JSON="${RESULT_JSON:-docs/homologacao/v117-smoke-result.json}"
RESULT_MD="${RESULT_MD:-docs/homologacao/v117-smoke-result.md}"
SMOKE_EMAIL="${SMOKE_EMAIL:-admin.demo@plantaopro.local}"
SMOKE_PASSWORD="${SMOKE_PASSWORD:-PlantaoProDemo!2026}"
mkdir -p "$(dirname "$RESULT_JSON")"
results=(); failures=0; TOKEN="${TOKEN:-}"
record(){ local name="$1" path="$2" code="$3" expected="$4"; local status="PASSED"; if [[ ! " $expected " =~ " $code " ]]; then status="FAILED"; failures=$((failures+1)); fi; results+=("{\"name\":\"$name\",\"path\":\"$path\",\"httpCode\":\"$code\",\"expected\":\"$expected\",\"status\":\"$status\"}"); echo "$status $path -> $code"; }
check(){ local name="$1" path="$2" expected="$3" auth="${4:-no}" code; if [[ "$auth" == "yes" ]]; then code=$(curl -sS -o /tmp/smoke.out -w '%{http_code}' -H "Authorization: Bearer $TOKEN" "$BASE_URL$path" || echo 000); else code=$(curl -sS -o /tmp/smoke.out -w '%{http_code}' "$BASE_URL$path" || echo 000); fi; code="${code:(-3)}"; record "$name" "$path" "$code" "$expected"; }
check health /api/health "200"
check swagger /swagger/v1/swagger.json "200"
if [[ -z "$TOKEN" ]]; then
  login_body=$(printf '{"email":"%s","senha":"%s"}' "$SMOKE_EMAIL" "$SMOKE_PASSWORD")
  login=$(curl -sS -o /tmp/smoke-login.json -w '%{http_code}' -H 'Content-Type: application/json' -d "$login_body" "$BASE_URL/api/auth/login" || echo 000)
  login="${login:(-3)}"; record login /api/auth/login "$login" "200"
  if [[ "$login" == "200" ]] && command -v jq >/dev/null 2>&1; then TOKEN=$(jq -r '.data.token // .Data.Token // .data.Token // empty' /tmp/smoke-login.json); fi
fi
if [[ -z "$TOKEN" ]]; then echo "FAILED token ausente para endpoints autenticados"; failures=$((failures+1)); else
for path in /api/dashboard /api/v112/dashboard /api/v113/dashboard /api/v114/dashboard /api/v115/faturamento/regras /api/v116/convenios/autorizacoes /api/v116/convenios/guias /api/v116/faturamento/lotes /api/v116/caixa/status /api/v116/caixa/movimentos /api/v116/notificacoes-operacionais /api/v116/relatorios/faturamento /api/v116/relatorios/recebimentos /api/v116/relatorios/repasses /api/v116/relatorios/glosas /api/v116/relatorios/lotes /api/v116/relatorios/produtividade-medica /api/v116/relatorios/auditoria-financeira /api/v116/relatorios/operacional; do check api "$path" "200" yes; done
fi
status="PASSED"; [[ "$failures" -eq 0 ]] || status="FAILED"
printf '{"version":"v1.17","status":"%s","baseUrl":"%s","generatedAt":"%s","failures":%s,"results":[%s]}\n' "$status" "$BASE_URL" "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$failures" "$(IFS=,; echo "${results[*]}")" > "$RESULT_JSON"
cat > "$RESULT_MD" <<MD
# Smoke v1.17

Status: $status

Falhas: $failures
MD
[[ "$failures" -eq 0 ]]
