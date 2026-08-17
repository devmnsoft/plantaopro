$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path; Set-Location $Root
if (-not $env:PLANTAOPRO_BASE_URL) { $env:PLANTAOPRO_BASE_URL = 'http://localhost:5000' }
if (-not $env:PLANTAOPRO_STORAGE_STATE) { $env:PLANTAOPRO_STORAGE_STATE = 'artifacts/auth/storage-state.json' }
if (-not (Test-Path $env:PLANTAOPRO_STORAGE_STATE)) { Write-Error 'BLOQUEADO: gere o storage state com npm run smoke:auth.'; exit 3 }
$LogDir = 'artifacts/ui-audit/runtime-logs/v183'; New-Item -ItemType Directory -Force $LogDir | Out-Null
npm run smoke:ui 2>&1 | Tee-Object (Join-Path $LogDir 'smoke-authenticated-windows.log'); exit $LASTEXITCODE
