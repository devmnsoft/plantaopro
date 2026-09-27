# run-dev-build.ps1 -- compila a solucao PlantaoPro.sln (Debug) garantindo antes que
# nenhuma instancia da API/Web esteja segurando os artefatos (causa de MSB3021/MSB3027).
#
# Uso:
#   powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-build.ps1 [-SkipPrecheck]
#
# Exit codes: 0 ok | 3 instancia em execucao (conflito previsto) | outro = exit do dotnet
param([switch]$SkipPrecheck)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$Sln = Join-Path $Root 'backend\PlantaoPro.sln'
$BinPattern = Join-Path $Root 'backend*'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'BLOQUEADO: SDK dotnet nao encontrado. Instale o .NET 10 SDK.' }

# prechecagem de conflito: instancia rodando segurando os binarios
if (-not $SkipPrecheck) {
  $busy = @(Get-CimInstance Win32_Process | Where-Object {
    $_.Name -match '^PlantaoPro\.(Web|Api)\.exe$' -and $_.ExecutablePath -and ($_.ExecutablePath -like $BinPattern)
  })
  if ($busy.Count -gt 0) {
    Write-Host 'BLOQUEADO: instancia em execucao segurando os artefatos (MSB3021/MSB3027 previstos):'
    foreach ($b in $busy) { Write-Host ('  PID ' + $b.ProcessId + ' ' + $b.Name + ' ' + $b.ExecutablePath) }
    Write-Host 'Rode antes: powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-stop.ps1'
    exit 3
  }
}

$logDir = Join-Path $Root 'artifacts\runtime-logs\dev'
New-Item -ItemType Directory -Force $logDir | Out-Null
$log = Join-Path $logDir ('build-' + (Get-Date -Format yyyyMMdd-HHmmss) + '.log')

$out = dotnet restore $Sln 2>&1 | Tee-Object $log -Append
if ($LASTEXITCODE) { Write-Host ('RESTORE FALHOU (exit {0}). Log: {1}' -f $LASTEXITCODE, $log); exit $LASTEXITCODE }

$out = dotnet build $Sln -c Debug --no-restore --nologo -v m 2>&1 | Tee-Object $log -Append
$code = $LASTEXITCODE
$errLines = @($out | Select-String -Pattern 'MSB3021|MSB3027| error ')
if ($errLines.Count -gt 0) {
  Write-Host 'Erros de compilacao (detalhes no log):'
  $errLines | Select-Object -First 15 | ForEach-Object { Write-Host ('  ' + $_.Line) }
}
if ($code) { Write-Host ('BUILD FALHOU (exit {0}). Log: {1}' -f $code, $log); exit $code }
Write-Host ('BUILD OK (0 erros). Log: {0}' -f $log)
exit 0
