#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${PLANTAOPRO_API_BASE_URL:-http://localhost:5000}"
ADMIN_EMAIL="${PLANTAOPRO_ADMIN_EMAIL:-admin@plantaopro.local}"
ADMIN_PASSWORD="${PLANTAOPRO_ADMIN_PASSWORD:-admin123}"

echo "Smoke API PlantãoPro em ${BASE_URL}"

request() {
  local method="$1" path="$2" expected="$3" body="${4:-}"
  local tmp status
  tmp="$(mktemp)"
  if [[ -n "$body" ]]; then
    status="$(curl -sS -o "$tmp" -w '%{http_code}' -X "$method" "${BASE_URL}${path}" -H 'Content-Type: application/json' --data "$body")"
  else
    status="$(curl -sS -o "$tmp" -w '%{http_code}' -X "$method" "${BASE_URL}${path}")"
  fi
  if [[ "$status" != "$expected" ]]; then
    echo "Falha em ${method} ${path}: HTTP ${status}, esperado ${expected}" >&2
    sed -n '1,40p' "$tmp" >&2
    rm -f "$tmp"
    exit 1
  fi
  rm -f "$tmp"
  echo "OK ${method} ${path} -> ${status}"
}

request GET / 200
request GET /api/health 200
request GET /api/health/db 200
request GET /swagger 200

login_payload="{\"email\":\"${ADMIN_EMAIL}\",\"password\":\"${ADMIN_PASSWORD}\"}"
login_response="$(mktemp)"
login_status="$(curl -sS -o "$login_response" -w '%{http_code}' -X POST "${BASE_URL}/api/auth/login" -H 'Content-Type: application/json' --data "$login_payload")"
if [[ "$login_status" != "200" ]]; then
  echo "Falha no login admin: HTTP ${login_status}" >&2
  sed -n '1,40p' "$login_response" >&2
  rm -f "$login_response"
  exit 1
fi
TOKEN="$(python3 - "$login_response" <<'PY'
import json,sys
with open(sys.argv[1]) as f: data=json.load(f)
for key in ('token','accessToken','jwt'):
    if isinstance(data,dict) and data.get(key): print(data[key]); break
else:
    if isinstance(data,dict) and isinstance(data.get('data'),dict):
        d=data['data']
        print(d.get('token') or d.get('accessToken') or d.get('jwt') or '')
PY
)"
rm -f "$login_response"
if [[ -z "$TOKEN" ]]; then
  echo "Login respondeu 200, mas sem token em campo conhecido (token/accessToken/jwt)." >&2
  exit 1
fi

auth_status="$(curl -sS -o /dev/null -w '%{http_code}' "${BASE_URL}/api/usuarios/me" -H "Authorization: Bearer ${TOKEN}")"
case "$auth_status" in
  200|204) echo "OK GET /api/usuarios/me autenticado -> ${auth_status}" ;;
  *) echo "Falha em endpoint autenticado /api/usuarios/me: HTTP ${auth_status}" >&2; exit 1 ;;
esac

echo "Smoke API concluído sem expor token."
