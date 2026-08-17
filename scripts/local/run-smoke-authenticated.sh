#!/usr/bin/env bash
set -Eeuo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"; cd "$ROOT"
LOG_DIR=artifacts/ui-audit/runtime-logs/v183; mkdir -p "$LOG_DIR"
: "${PLANTAOPRO_BASE_URL:=http://localhost:5000}"
: "${PLANTAOPRO_STORAGE_STATE:=artifacts/auth/storage-state.json}"
[[ -f "$PLANTAOPRO_STORAGE_STATE" ]] || { echo 'BLOQUEADO: gere o storage state com npm run smoke:auth.'; exit 3; }
export PLANTAOPRO_BASE_URL PLANTAOPRO_STORAGE_STATE
npm run smoke:ui 2>&1 | tee "$LOG_DIR/smoke-authenticated-linux.log"
