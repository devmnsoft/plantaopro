#!/usr/bin/env bash
set -euo pipefail
: "${PLANTAOPRO_BASE_URL:?Defina PLANTAOPRO_BASE_URL (ex.: http://localhost:5000).}"
node scripts/ui/visual-smoke.mjs
