$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path; Set-Location $Root
if (-not (Get-Command node -ErrorAction SilentlyContinue)) { throw 'BLOQUEADO: Node.js ausente.' }; node --version
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) { throw 'BLOQUEADO: npm ausente.' }; npm --version
& "$PSScriptRoot/run-build-backend.ps1"; if ($LASTEXITCODE) { exit $LASTEXITCODE }
Write-Host 'Build concluído. Inicie em outro terminal:'
Write-Host '  dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj'
Write-Host 'Depois: $env:PLANTAOPRO_BASE_URL="http://localhost:5000"; npm run smoke:ui'
