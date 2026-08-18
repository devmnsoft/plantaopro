#!/usr/bin/env bash
# v1.84: evidências e checks de ações endpoint-backed são definidos pelo runner.
set -euo pipefail
: "${PLANTAOPRO_BASE_URL:?Defina PLANTAOPRO_BASE_URL (ex.: http://localhost:5000).}"
node scripts/ui/visual-smoke.mjs
