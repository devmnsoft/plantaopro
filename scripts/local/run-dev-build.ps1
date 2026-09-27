# run-dev-build.ps1 -- compila a solucao backend\PlantaoPro.sln (Debug), garantindo antes
# que nenhuma instancia da API/Web esteja segurando os artefatos (MSB3021/MSB3027).
# Registra o resultado em artifacts\runtime-logs\dev\last-build.json; o run-dev-start.ps1
# bloqueia (exit 4) se este arquivo nao existir ou nao estiver "ok".
#
# Uso:
#   powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-build.ps1 [-SkipPrecheck]
#
# Exit codes: 0 ok | 2 SDK dotnet ausente | 3 instancia em execucao | outro != 0 = falha do dotnet
param([switch]$SkipPrecheck)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$Sln = Join-Path $Root 'backend\PlantaoPro.sln'
$BinPattern = Join-Path $Root 'backend*'
$logDir = Join-Path $Root 'artifacts\runtime-logs\dev'
New-Item -ItemType Directory -Force $logDir | Out-Null
$stateFile = Join-Path $logDir 'last-build.json'

function Write-BuildState([string]$status, [string]$logPath, [int]$exitCode) {
  $head = ''
  try { $head = git -C $Root rev-parse --short HEAD 2>$null } catch { }
  $st = [ordered]@{
    built_at  = (Get-Date).ToString('o')
    status    = $status      # ok | failed
    exit_code = $exitCode
    log       = $logPath
    git_head  = if ($head) { @($head)[0] } else { '' }
  }
  $st | ConvertTo-Json -Depth 3 | Set-Content -Path $stateFile -Encoding utf8
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  Write-Host 'BLOQUEADO: SDK dotnet nao encontrado. Instale o .NET 10 SDK.'
  exit 2
}

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

$log = Join-Path $logDir ('build-' + (Get-Date -Format yyyyMMdd-HHmmss) + '.log')

dotnet restore $Sln 2>&1 | Tee-Object $log -Append | Out-Null
if ($LASTEXITCODE) {
  Write-BuildState 'failed' $log $LASTEXITCODE
  Write-Host ('RESTORE FALHOU (exit {0}). Log: {1}' -f $LASTEXITCODE, $log)
  exit $LASTEXITCODE
}

dotnet build $Sln -c Debug --no-restore --nologo -v m 2>&1 | Tee-Object $log -Append | Select-String -Pattern ' error |MSB302[17]' | Select-Object -First 15 | ForEach-Object { Write-Host ('  ' + $_.Line) }
$code = $LASTEXITCODE
if ($code) {
  Write-BuildState 'failed' $log $code
  Write-Host ('BUILD FALHOU (exit {0}). Log: {1}' -f $code, $log)
  exit $code
}
Write-BuildState 'ok' $log 0
Write-Host ('BUILD OK (0 erros). Estado: {0}. Log: {1}' -f $stateFile, $log)
exit 0
