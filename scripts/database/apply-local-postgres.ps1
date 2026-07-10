param(
  [string]$HostName = $(if ($env:PGHOST) { $env:PGHOST } else { "localhost" }),
  [int]$Port = $(if ($env:PGPORT) { [int]$env:PGPORT } else { 5432 }),
  [string]$Database = $(if ($env:PGDATABASE) { $env:PGDATABASE } else { "plantaopro" }),
  [string]$User = $(if ($env:PGUSER) { $env:PGUSER } else { "postgres" }),
  [string]$Password = $(if ($env:PGPASSWORD) { $env:PGPASSWORD } else { "CHANGE_ME_LOCAL_POSTGRES" })
)
$ErrorActionPreference = "Stop"
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) { throw "psql não encontrado. Instale o cliente PostgreSQL antes de aplicar o banco." }
$Root = Resolve-Path (Join-Path $PSScriptRoot "../..")
function Invoke-SqlFile([string]$Path) {
  if (-not (Test-Path $Path)) { return }
  $relative = Resolve-Path $Path | ForEach-Object { $_.Path.Replace($Root.Path + [IO.Path]::DirectorySeparatorChar, "") }
  Write-Host "==> Aplicando $relative"
  $env:PGPASSWORD = $Password
  & psql -v ON_ERROR_STOP=1 -h $HostName -p $Port -U $User -d $Database -f $Path
  if ($LASTEXITCODE -ne 0) { throw "Falha ao aplicar $relative" }
}
Invoke-SqlFile (Join-Path $Root "database/PlantaoPro_PostgreSQL_Completo.sql")
Get-ChildItem (Join-Path $Root "database/migrations") -Filter "*.sql" -ErrorAction SilentlyContinue | Sort-Object Name | ForEach-Object { Invoke-SqlFile $_.FullName }
Invoke-SqlFile (Join-Path $Root "database/seeds.sql")
Get-ChildItem (Join-Path $Root "database/seeds") -Filter "*.sql" -ErrorAction SilentlyContinue | Sort-Object Name | ForEach-Object { Invoke-SqlFile $_.FullName }
Write-Host "Banco local PlantãoPro aplicado com sucesso em ${HostName}:${Port}/${Database}."
