# setup-local-config.ps1 -- cria os user-secrets locais que os apps precisam para
# rodar em Development SEM segredos versionados nos launchSettings.json.
#
# Idempotente: so cria chaves ausentes (usa -Force para sobrescrever as que existirem).
# Nunca imprime valores, apenas NOMES das chaves por projeto.
#
# Uso:
#   powershell -ExecutionPolicy Bypass -File scripts/local/setup-local-config.ps1 [-Force] [-DbPassword <senha>]
#
# A senha do Postgres local pode ser passada com -DbPassword ou pela variavel
# PLANTAOPRO_DEV_DB_PASSWORD (padrao desta maquina: 123456).
#
# Exit codes: 0 ok | 2 SDK dotnet ausente
param(
  [switch]$Force,
  [string]$DbPassword = $env:PLANTAOPRO_DEV_DB_PASSWORD,
  [string]$DbUser = 'postgres',
  [string]$DbHost = '127.0.0.1',
  [int]$DbPort = 5432
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  Write-Host 'BLOQUEADO: SDK dotnet nao encontrado. Instale o .NET 10 SDK.'
  exit 2
}
if ([string]::IsNullOrWhiteSpace($DbPassword)) { $DbPassword = '123456' }

$apiCs = Join-Path $Root 'backend\PlantaoPro.Api\PlantaoPro.Api.csproj'
$webCs = Join-Path $Root 'backend\PlantaoPro.Web\PlantaoPro.Web.csproj'
# IDs espelham <UserSecretsId> nos csproj (SDK 10 nao tem "user-secrets get"; lemos o arquivo local)
$apiSecretId = 'plantaopro-api-development'
$webSecretId = 'plantaopro-web-development'

function Get-SecretNames([string]$csproj) {
  # SDK 10 imprime linhas "Nome = valor" sem header; nomes sao o texto antes do ' = '
  $lines = @(dotnet user-secrets list --project $csproj 2>$null)
  return @($lines | Where-Object { $_ -match ' = ' } | ForEach-Object { ($_ -split ' = ')[0].Trim() })
}

function Get-SecretValue([string]$secretId, [string]$name) {
  # SDK 10 grava em AppData\Roaming; SDKs anteriores usavam AppData\Local
  $file = Join-Path $env:APPDATA ('Microsoft\UserSecrets\' + $secretId + '\secrets.json')
  if (-not (Test-Path $file)) { $file = Join-Path $env:LOCALAPPDATA ('Microsoft\UserSecrets\' + $secretId + '\secrets.json') }
  if (-not (Test-Path $file)) { return '' }
  try {
    $j = Get-Content $file -Raw | ConvertFrom-Json
    return [string]$j.$name
  } catch { return '' }
}

function Invoke-ConfigProject([string]$label, [string]$csproj, [hashtable]$wanted) {
  Write-Host ('[' + $label + '] chaves verificadas:')
  $existing = @(Get-SecretNames $csproj)
  foreach ($k in $wanted.Keys) {
    if ($existing -contains $k) {
      if ($Force) {
        dotnet user-secrets set $k $wanted[$k] --project $csproj | Out-Null
        Write-Host ('  [sobrescrito] ' + $k)
      } else {
        Write-Host ('  [mantido]     ' + $k)
      }
    } else {
      dotnet user-secrets set $k $wanted[$k] --project $csproj | Out-Null
      Write-Host ('  [criado]      ' + $k)
    }
  }
}

# Jwt key: gera apenas se ausente (ou com -Force). 64 chars, base64 de 48 bytes.
$jwtKey = (Get-SecretValue $apiSecretId 'Jwt:Key')
if ((-not $Force) -and $jwtKey.Trim().Length -ge 32) {
  Write-Host ('Jwt__Key ja existe no user-secrets da API (mantida; use -Force para gerar nova).')
} else {
  $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
  $bytes = New-Object byte[] 48
  $rng.GetBytes($bytes)
  $jwtKey = [Convert]::ToBase64String($bytes)
  Write-Host 'Jwt__Key gerada (nova chave -- tokens emitidos antes exigem re-login).'
}

$commonCs = 'Host={0};Port={1};Database=postgres;Username={2};Password={3};Pooling=true;Maximum Pool Size=50;Minimum Pool Size=0;Timeout=30;Command Timeout=60;Search Path=PlantaoPro,public;Application Name={4}'
$csApi = $commonCs -f $DbHost, $DbPort, $DbUser, $DbPassword, 'PlantaoPro.api'
$csWeb = $commonCs -f $DbHost, $DbPort, $DbUser, $DbPassword, 'PlantaoPro.web'

# IMPORTANTE: o secrets.json eh PLANO (nao ha hierarquia). Em chaves planas o separador
# de seccao e o ':' (a conversao de '__' em ':' so vale para variaveis de ambiente).
# Ex.: "ConnectionStrings:Default", "Jwt:Key".
$legacyUnderscore = @(
  'ConnectionStrings__Default',
  'Jwt__Key',
  'Jwt__Issuer',
  'Jwt__Audience',
  'Database__AllowLegacyPostgresDatabase',
  'Database__AllowDevelopmentAutoCreate'
)

function Remove-LegacyKeys([string]$csproj) {
  $have = @(Get-SecretNames $csproj)
  foreach ($l in $legacyUnderscore) {
    if ($have -contains $l) {
      dotnet user-secrets remove $l --project $csproj | Out-Null
      Write-Host ('  [limpado legado] ' + $l)
    }
  }
}

Remove-LegacyKeys $apiCs
Remove-LegacyKeys $webCs

Invoke-ConfigProject 'API' $apiCs @{
  'ConnectionStrings:Default'            = $csApi
  'Jwt:Key'                               = $jwtKey
  'Jwt:Issuer'                            = 'PlantaoPro'
  'Jwt:Audience'                          = 'PlantaoPro'
  'Database:AllowLegacyPostgresDatabase'  = 'true'
  'Database:AllowDevelopmentAutoCreate'   = 'true'
}
Invoke-ConfigProject 'WEB' $webCs @{
  'ConnectionStrings:Default' = $csWeb
}

Write-Host 'OK: configuracao local pronta (valores nunca impressos).'
Write-Host 'Proximo passo: powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-start.ps1'
exit 0
