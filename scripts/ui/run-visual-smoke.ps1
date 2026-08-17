# Runner de homologação visual v1.80.0.
$ErrorActionPreference = 'Stop'
if (-not $env:PLANTAOPRO_BASE_URL) { throw 'Defina PLANTAOPRO_BASE_URL. Ex.: $env:PLANTAOPRO_BASE_URL="http://127.0.0.1:5000"' }
if ($env:PLANTAOPRO_PUBLIC_ONLY -ne '1' -and -not $env:PLANTAOPRO_STORAGE_STATE) { throw 'Defina PLANTAOPRO_STORAGE_STATE para rotas autenticadas. Após login: await page.context().storageState({ path: "playwright/.auth/user.json" }). Para apenas rotas públicas: $env:PLANTAOPRO_PUBLIC_ONLY="1".' }
node scripts/ui/visual-smoke.mjs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
