# run-dev-start.ps1 -- inicia API e Web a partir do build existente (--no-build,
# sem disparar build concorrente), registra os PIDs e aguarda readiness HTTP.
#
# Procedimento unico recomendado: parar -> compilar -> iniciar -> verificar
#   powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-stop.ps1
#   powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-build.ps1
#   powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-start.ps1
#
# Uso:
#   powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-start.ps1 [-ReadyTimeoutSec 90]
#
# Exit codes: 0 ok | 3 instancia ja em execucao | 5 readiness nao atingida
param([int]$ReadyTimeoutSec = 90)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$BinPattern = Join-Path $Root 'backend*'
$logDir = Join-Path $Root 'artifacts\runtime-logs\dev'
New-Item -ItemType Directory -Force $logDir | Out-Null

function Get-AppUrl([string]$proj) {
  $ls = Join-Path $proj 'Properties\launchSettings.json'
  if (Test-Path $ls) {
    try {
      $j = Get-Content $ls -Raw | ConvertFrom-Json
      $u = $j.profiles.'https'.applicationUrl
      if ($u) { return ($u -split ';')[0] }
    } catch { }
  }
  return $null
}

function Test-PortOpen([string]$hostName, [int]$port) {
  try {
    $c = New-Object Net.Sockets.TcpClient
    $iar = $c.BeginConnect($hostName, $port, $null, $null)
    if ($iar.AsyncWaitHandle.WaitOne(1500)) { $c.EndConnect($iar); $c.Close(); return $true }
    $c.Close(); return $false
  } catch { return $false }
}

# prechecagem de conflito (evita dupla instancia sobre a mesma saida)
$busy = @(Get-CimInstance Win32_Process | Where-Object {
  $_.Name -match '^PlantaoPro\.(Web|Api)\.exe$' -and $_.ExecutablePath -and ($_.ExecutablePath -like $BinPattern)
})
if ($busy.Count -gt 0) {
  Write-Host 'BLOQUEADO: instancia ja esta em execucao:'
  foreach ($b in $busy) { Write-Host ('  PID ' + $b.ProcessId + ' ' + $b.Name + ' ' + $b.ExecutablePath) }
  Write-Host 'Para parar antes: powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-stop.ps1'
  exit 3
}

$apiProj = Join-Path $Root 'backend\PlantaoPro.Api'
$webProj = Join-Path $Root 'backend\PlantaoPro.Web'
$apiUrl = Get-AppUrl $apiProj
$webUrl = Get-AppUrl $webProj
if (-not $apiUrl -or -not $webUrl) { throw 'BLOQUEADO: nao foi possivel ler o launch profile https (launchSettings.json).' }

$stamp = Get-Date -Format yyyyMMdd-HHmmss
$apiOut = Join-Path $logDir ('api-' + $stamp + '.out.log')
$apiErr = Join-Path $logDir ('api-' + $stamp + '.err.log')
$webOut = Join-Path $logDir ('web-' + $stamp + '.out.log')
$webErr = Join-Path $logDir ('web-' + $stamp + '.err.log')

Write-Host ('Iniciando API  ({0}) ...' -f $apiUrl)
$apiProc = Start-Process -FilePath 'dotnet' -ArgumentList 'run', '--no-build', '--launch-profile', 'https' `
  -WorkingDirectory $apiProj -WindowStyle Hidden -PassThru `
  -RedirectStandardOutput $apiOut -RedirectStandardError $apiErr
Write-Host ('Iniciando Web  ({0}) ...' -f $webUrl)
$webProc = Start-Process -FilePath 'dotnet' -ArgumentList 'run', '--no-build', '--launch-profile', 'https' `
  -WorkingDirectory $webProj -WindowStyle Hidden -PassThru `
  -RedirectStandardOutput $webOut -RedirectStandardError $webErr

$reg = [ordered]@{
  root = $Root
  started_at = (Get-Date).ToString('o')
  api = @{ launcher_pid = [int]$apiProc.Id; url = $apiUrl; out_log = $apiOut; err_log = $apiErr }
  web = @{ launcher_pid = [int]$webProc.Id; url = $webUrl; out_log = $webOut; err_log = $webErr }
}
$regFile = Join-Path $logDir 'last-run.json'
$reg | ConvertTo-Json -Depth 4 | Set-Content -Path $regFile -Encoding utf8
Write-Host ('PIDs registrados em {0}' -f $regFile)

# aguarda readiness (porta aceitando conexoes) com limite de tempo
[Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
$apiPort = ([Uri]$apiUrl).Port; $webPort = ([Uri]$webUrl).Port
$deadline = (Get-Date).AddSeconds($ReadyTimeoutSec)
$okApi = $false; $okWeb = $false
do {
  if (-not $okApi -and (Test-PortOpen 'localhost' $apiPort)) { $okApi = $true }
  if (-not $okWeb -and (Test-PortOpen 'localhost' $webPort)) { $okWeb = $true }
  if (-not ($okApi -and $okWeb)) { Start-Sleep -Milliseconds 500 }
} while ((Get-Date) -lt $deadline -and -not ($okApi -and $okWeb))

if (-not ($okApi -and $okWeb)) {
  Write-Host ('ERRO: readiness nao atingida em {0}s (API={1}, Web={2}). Ultimas linhas dos logs:' -f $ReadyTimeoutSec, $okApi, $okWeb)
  foreach ($l in @($apiErr, $webErr, $apiOut, $webOut)) {
    Write-Host ('--- ' + $l + ' ---')
    if (Test-Path $l) { Get-Content $l -Tail 6 }
  }
  exit 5
}

# verificacao final: um request real a cada app e reporta o status
foreach ($pair in @(@('API', $apiUrl), @('WEB', $webUrl))) {
  $name = $pair[0]; $url = $pair[1]
  try {
    $req = [Net.HttpWebRequest]::Create($url)
    $req.Timeout = 5000
    $resp = $req.GetResponse()
    $status = [int]$resp.StatusCode; $resp.Close()
  } catch [System.Net.WebException] {
    $status = [int]($_.Exception.Response.StatusCode)
  }
  Write-Host ('{0}: respondendo (HTTP {1}) em {2}' -f $name, $status, $url)
}
Write-Host 'OK: API e Web no ar a partir do build atual.'
exit 0
