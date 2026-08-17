#!/usr/bin/env bash
set -Eeuo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"; cd "$ROOT"
LOG_DIR=artifacts/ui-audit/runtime-logs/v183; mkdir -p "$LOG_DIR"
command -v node >/dev/null || { echo 'BLOQUEADO: Node.js ausente.'; exit 127; }; node --version
command -v npm >/dev/null || { echo 'BLOQUEADO: npm ausente.'; exit 127; }; npm --version
scripts/local/run-build-backend.sh
cat <<'MSG'
Build concluído. Inicie em outro terminal:
  dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj
Depois execute o smoke público:
  PLANTAOPRO_BASE_URL=http://localhost:5000 npm run smoke:ui
MSG
