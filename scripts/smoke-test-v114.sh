#!/usr/bin/env bash
set -euo pipefail
export Jwt__Key="${Jwt__Key:-PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS}"
export Jwt__Issuer="${Jwt__Issuer:-PlantaoPro}"
export Jwt__Audience="${Jwt__Audience:-PlantaoPro}"
BASE_URL="${BASE_URL:-http://localhost:5000}"
TOKEN="${JWT_TOKEN:-${TOKEN:-}}"
OUT_MD="docs/homologacao/v114-smoke-result.md"
OUT_JSON="docs/homologacao/v114-smoke-result.json"
mkdir -p docs/homologacao
headers=(-H "Content-Type: application/json")
if [ -n "$TOKEN" ]; then headers+=( -H "Authorization: Bearer $TOKEN" ); fi
endpoints=(
  "/api/health"
  "/api/v114/dashboard"
  "/api/v114/operacao/central"
  "/api/v114/operacao/atividades"
  "/api/v114/operacao/tarefas"
  "/api/v114/operacao/outbox"
  "/api/v114/itens-faturaveis"
  "/api/v114/faturamento/contas-receber"
  "/api/v114/faturamento/titulos"
  "/api/v114/faturamento/repasses-medicos"
  "/api/v114/faturamento/glosas"
  "/api/v114/jornada/progresso"
  "/api/v114/jornada/proximas-acoes"
  "/api/v114/templates-operacionais"
  "/api/v114/mobile/medico/dashboard"
)
printf '# Smoke v1.14 PlantãoPro\n\nExecutado em: %s\n\n' "$(date -u +%FT%TZ)" > "$OUT_MD"
printf '{"version":"v1.14","results":[' > "$OUT_JSON"
first=1
for ep in "${endpoints[@]}"; do
  code=$(curl -sS -o /tmp/v114-smoke-body -w '%{http_code}' "${headers[@]}" "$BASE_URL$ep" || true)
  status="WARN"
  if [ "$code" = "200" ]; then status="PASS"; fi
  if [ "$first" -eq 0 ]; then printf ',' >> "$OUT_JSON"; fi
  first=0
  printf '\n- %s `%s` HTTP %s' "$status" "$ep" "$code" >> "$OUT_MD"
  printf '{"endpoint":"%s","status":"%s","httpStatus":%s}' "$ep" "$status" "${code:-0}" >> "$OUT_JSON"
done
printf ']}\n' >> "$OUT_JSON"
printf '\n\nObservação: endpoints protegidos podem retornar 401/403 quando JWT não for informado; isso valida autenticação obrigatória. Boleto permanece demonstrativo.\n' >> "$OUT_MD"
