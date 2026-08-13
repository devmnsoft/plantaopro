#!/usr/bin/env bash
set -euo pipefail
if [[ -z "${PLANTAOPRO_BASE_URL:-}" ]]; then echo 'Erro: defina PLANTAOPRO_BASE_URL. Ex.: PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 scripts/ui/run-visual-smoke.sh' >&2; exit 2; fi
if [[ "${PLANTAOPRO_PUBLIC_ONLY:-0}" != "1" && -z "${PLANTAOPRO_STORAGE_STATE:-}" ]]; then echo 'Erro: defina PLANTAOPRO_STORAGE_STATE para rotas autenticadas. Capture após login com: await page.context().storageState({ path: "playwright/.auth/user.json" }). Para apenas rotas públicas, use PLANTAOPRO_PUBLIC_ONLY=1.' >&2; exit 2; fi
echo 'Smoke visual v1.70: artefatos em artifacts/ui-audit/screenshots/v170/.'
node scripts/ui/visual-smoke.mjs
