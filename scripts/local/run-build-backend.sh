#!/usr/bin/env bash
set -Eeuo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"; cd "$ROOT"
LOG_DIR=artifacts/ui-audit/runtime-logs/v183; mkdir -p "$LOG_DIR"
exec > >(tee "$LOG_DIR/build-backend-linux.log") 2>&1
command -v dotnet >/dev/null || { echo 'BLOQUEADO: SDK dotnet não encontrado. Instale o .NET 10 SDK.'; exit 127; }
dotnet --info
dotnet restore backend/PlantaoPro.sln
dotnet build backend/PlantaoPro.sln -c Release --no-restore
dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build
