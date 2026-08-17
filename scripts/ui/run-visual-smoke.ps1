$ErrorActionPreference = 'Stop'
if (-not $env:PLANTAOPRO_BASE_URL) { throw 'Defina PLANTAOPRO_BASE_URL (ex.: http://localhost:5000).' }
node scripts/ui/visual-smoke.mjs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
