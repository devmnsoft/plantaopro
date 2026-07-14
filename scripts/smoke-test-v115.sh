#!/usr/bin/env bash
set -u
export Jwt__Key="${Jwt__Key:-ci-demo-key-with-at-least-32-characters}"
export Jwt__Issuer="${Jwt__Issuer:-PlantaoPro}"
export Jwt__Audience="${Jwt__Audience:-PlantaoPro}"
BASE_URL="${BASE_URL:-http://localhost:5000}"
OUT_MD="docs/homologacao/v115-smoke-result.md"
OUT_JSON="docs/homologacao/v115-smoke-result.json"
mkdir -p docs/homologacao
endpoints=("/api/health" "/api/v115/faturamento/regras" "/api/v115/faturamento/contas-receber" "/api/v115/repasses-medicos" "/api/v115/glosas" "/api/v115/financeiro/alertas" "/api/v114/mobile/medico/dashboard")
printf '# Smoke v1.15\n\n' > "$OUT_MD"
printf '{"version":"v1.15","results":[' > "$OUT_JSON"
first=1
for ep in "${endpoints[@]}"; do
  code=$(curl -sS -o /tmp/v115-smoke.out -w "%{http_code}" "$BASE_URL$ep" || echo 000)
  status="WARN"; [ "$code" = "200" ] && status="PASS"; [ "$code" = "401" ] || [ "$code" = "403" ] && status="PASS_AUTH"
  printf -- '- %s `%s` HTTP %s\n' "$status" "$ep" "$code" >> "$OUT_MD"
  [ $first -eq 0 ] && printf ',' >> "$OUT_JSON"; first=0
  printf '{"endpoint":"%s","status":"%s","httpStatus":%s}' "$ep" "$status" "$code" >> "$OUT_JSON"
done
printf ']}\n' >> "$OUT_JSON"
