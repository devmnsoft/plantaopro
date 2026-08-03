$ErrorActionPreference = 'Stop'
if (-not $env:PLANTAOPRO_CONNECTION_STRING) { throw 'Defina PLANTAOPRO_CONNECTION_STRING.' }
if (-not $env:PLANTAOPRO_BOOTSTRAP_PASSWORD) { throw 'Defina PLANTAOPRO_BOOTSTRAP_PASSWORD em um secret local.' }
if (-not $env:PLANTAOPRO_BOOTSTRAP_ENVIRONMENT) { $env:PLANTAOPRO_BOOTSTRAP_ENVIRONMENT = 'Development' }
if (-not $env:PLANTAOPRO_BOOTSTRAP_ADMIN_EMAIL) { $env:PLANTAOPRO_BOOTSTRAP_ADMIN_EMAIL = 'admin.global@plantaopro.local' }
if (-not $env:PLANTAOPRO_BOOTSTRAP_ADMIN_NAME) { $env:PLANTAOPRO_BOOTSTRAP_ADMIN_NAME = 'Super Administrador PlantãoPro' }
if (-not $env:PLANTAOPRO_BOOTSTRAP_FORCE_ROTATION) { $env:PLANTAOPRO_BOOTSTRAP_FORCE_ROTATION = 'true' }
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$hash = $env:PLANTAOPRO_BOOTSTRAP_PASSWORD_HASH
try {
    if (-not $hash) {
        $hash = & dotnet run --project (Join-Path $root 'backend/PlantaoPro.Tools.Bootstrap/PlantaoPro.Tools.Bootstrap.csproj') -- hash-password
        if ($LASTEXITCODE -ne 0) { throw 'Não foi possível gerar o hash BCrypt.' }
    }
    & psql $env:PLANTAOPRO_CONNECTION_STRING -X -v ON_ERROR_STOP=1 `
      -v "bootstrap_environment=$($env:PLANTAOPRO_BOOTSTRAP_ENVIRONMENT)" `
      -v "bootstrap_admin_email=$($env:PLANTAOPRO_BOOTSTRAP_ADMIN_EMAIL)" `
      -v "bootstrap_admin_name=$($env:PLANTAOPRO_BOOTSTRAP_ADMIN_NAME)" `
      -v "bootstrap_admin_password_hash=$hash" `
      -v "bootstrap_force_rotation=$($env:PLANTAOPRO_BOOTSTRAP_FORCE_ROTATION)" `
      -f (Join-Path $root 'database/scrpt_completo.sql')
    if ($LASTEXITCODE -ne 0) { throw 'A instalação SQL falhou.' }
    & psql $env:PLANTAOPRO_CONNECTION_STRING -X -v ON_ERROR_STOP=1 `
      -v "bootstrap_admin_email=$($env:PLANTAOPRO_BOOTSTRAP_ADMIN_EMAIL)" `
      -f (Join-Path $root 'scripts/database/verify-superadmin.sql')
    if ($LASTEXITCODE -ne 0) { throw 'A verificação do superadministrador falhou.' }
    Write-Output 'Instalação local concluída; credenciais e hashes não foram exibidos.'
}
finally {
    $hash = $null
    Remove-Item Env:PLANTAOPRO_BOOTSTRAP_PASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:PLANTAOPRO_BOOTSTRAP_PASSWORD_HASH -ErrorAction SilentlyContinue
}
