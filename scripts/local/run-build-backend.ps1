$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path; Set-Location $Root
$LogDir = 'artifacts/ui-audit/runtime-logs/v183'; New-Item -ItemType Directory -Force $LogDir | Out-Null
$Log = Join-Path $LogDir 'build-backend-windows.log'
try {
  if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'BLOQUEADO: SDK dotnet não encontrado. Instale o .NET 10 SDK.' }
  dotnet --info 2>&1 | Tee-Object $Log
  dotnet restore backend/PlantaoPro.sln 2>&1 | Tee-Object $Log -Append; if ($LASTEXITCODE) { exit $LASTEXITCODE }
  dotnet build backend/PlantaoPro.sln -c Release --no-restore 2>&1 | Tee-Object $Log -Append; if ($LASTEXITCODE) { exit $LASTEXITCODE }
  dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build 2>&1 | Tee-Object $Log -Append; exit $LASTEXITCODE
} catch { $_ | Tee-Object $Log -Append; exit 127 }
