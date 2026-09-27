# run-dev-start.ps1 -- inicia API e Web a partir do build existente (dotnet run --no-build),
# registra os PIDs da tentativa e aguarda readiness HTTP REAL (endpoint + marcador no corpo),
# com liveness re-probe apos a inicializacao. Em falha, mata apenas os processos desta
# tentativa e imprime diagnostico com segredos redigidos.
#
# Procedimento unico recomendado: parar -> compilar -> iniciar -> verificar
#   powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-stop.ps1
#   powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-build.ps1
#   powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-start.ps1
#
# Uso:
#   powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-start.ps1 [-ReadyTimeoutSec 90] [-SkipBuildGate]
#
# Exit codes:
#   0 ok (readiness + liveness atingidos)
#   2 prerequisitos ausentes (SDK dotnet, launchSettings ilegivel, user-secrets ausentes)
#   3 instancia ja em execucao (rode run-dev-stop.ps1 antes)
#   4 sem build ok registrado (artifacts\runtime-logs\dev\last-build.json ausente ou != ok)
#   5 readiness/liveness nao atingida (diagnostico + cleanup automatico desta tentativa)
param(
  [int]$ReadyTimeoutSec = 90,
  [switch]$SkipBuildGate
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$BinPattern = Join-Path $Root 'backend*'
$logDir = Join-Path $Root 'artifacts\runtime-logs\dev'
New-Item -ItemType Directory -Force $logDir | Out-Null

[Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

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

function Get-SecretNames([string]$csproj) {
  # SDK 10 imprime linhas "Nome = valor" sem header; nomes sao o texto antes do ' = '
  $lines = @(dotnet user-secrets list --project $csproj 2>$null)
  return @($lines | Where-Object { $_ -match ' = ' } | ForEach-Object { ($_ -split ' = ')[0].Trim() })
}

# SDK 10 nao tem "user-secrets get": lemos o arquivo local do user-secrets
# (SDK 10 grava em AppData\Roaming; SDKs anteriores usavam AppData\Local)
function Get-SecretValue([string]$secretId, [string]$name) {
  $file = Join-Path $env:APPDATA ('Microsoft\UserSecrets\' + $secretId + '\secrets.json')
  if (-not (Test-Path $file)) { $file = Join-Path $env:LOCALAPPDATA ('Microsoft\UserSecrets\' + $secretId + '\secrets.json') }
  if (-not (Test-Path $file)) { return '' }
  try {
    $j = Get-Content $file -Raw | ConvertFrom-Json
    return [string]$j.$name
  } catch { return '' }
}

function Redact([string]$text, [string[]]$secrets) {
  if (-not $text) { return '' }
  foreach ($s in $secrets) { if ($s -and $s.Length -ge 4) { $text = $text.Replace($s, '***REDIGIDO***') } }
  return $text
}

# probe HTTP real: status 200 + todos os marcadores presentes no corpo
function Test-HttpReady([string]$url, [string[]]$markers) {
  try {
    $req = [Net.HttpWebRequest]::Create($url); $req.Method = 'GET'; $req.Timeout = 6000
    $resp = $req.GetResponse()
    $code = [int]$resp.StatusCode
    $sr = New-Object IO.StreamReader($resp.GetResponseStream())
    $body = $sr.ReadToEnd(); $sr.Close(); $resp.Close()
    if ($code -ne 200) { return @{ ok = $false; detail = ('HTTP ' + $code) } }
    foreach ($m in $markers) {
      if ($body.IndexOf($m, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        return @{ ok = $false; detail = ('HTTP 200 porem marcador ausente: ' + $m) }
      }
    }
    return @{ ok = $true; detail = ('HTTP 200 + markers ok (corpo ' + $body.Length + ' chars)') }
  } catch [System.Net.WebException] {
    if ($_.Exception.Response) {
      return @{ ok = $false; detail = ('HTTP ' + [int]$_.Exception.Response.StatusCode) }
    }
    return @{ ok = $false; detail = 'conexao ainda indisponivel (reintentando)' }
  } catch {
    return @{ ok = $false; detail = $_.Exception.Message }
  }
}

function Stop-Attempt([int[]]$launcherPids) {
  # identidade reconfirmada imediatamente antes de matar; ordem filho -> pai
  $kids = @(Get-CimInstance Win32_Process | Where-Object { $_.ParentProcessId -in $launcherPids })
  foreach ($k in $kids) {
    $live = Get-Process -Id ([int]$k.ProcessId) -ErrorAction SilentlyContinue
    if ($live -and $live.Name -match '^PlantaoPro\.(Api|Web)\.exe$') {
      Stop-Process -Id ([int]$k.ProcessId) -Force -ErrorAction SilentlyContinue
      Write-Host ('  parado (app desta tentativa): PID ' + $k.ProcessId + ' ' + $live.Name)
    }
  }
  Start-Sleep -Milliseconds 400
  foreach ($lp in $launcherPids) {
    $live = Get-CimInstance Win32_Process -Filter ('ProcessId={0}' -f $lp) -ErrorAction SilentlyContinue
    if ($live -and $live.Name -eq 'dotnet.exe') {
      Stop-Process -Id $lp -Force -ErrorAction SilentlyContinue
      Write-Host ('  parado (launcher desta tentativa): PID ' + $lp)
    }
  }
}

# --- 1) prerequisitos -----------------------------------------------------------
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  Write-Host 'BLOQUEADO: SDK dotnet nao encontrado. Instale o .NET 10 SDK.'
  exit 2
}

# --- 2) ja em execucao? ---------------------------------------------------------
$busy = @(Get-CimInstance Win32_Process | Where-Object {
  $_.Name -match '^PlantaoPro\.(Web|Api)\.exe$' -and $_.ExecutablePath -and ($_.ExecutablePath -like $BinPattern)
})
if ($busy.Count -gt 0) {
  Write-Host 'BLOQUEADO: instancia ja esta em execucao:'
  foreach ($b in $busy) { Write-Host ('  PID ' + $b.ProcessId + ' ' + $b.Name + ' ' + $b.ExecutablePath) }
  Write-Host 'Para parar antes: powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-stop.ps1'
  exit 3
}

# --- 3) gate de build -----------------------------------------------------------
$stateFile = Join-Path $logDir 'last-build.json'
if (-not $SkipBuildGate) {
  if (-not (Test-Path $stateFile)) {
    Write-Host "BLOQUEADO (exit 4): sem build registrado em $stateFile."
    Write-Host 'Compile antes: powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-build.ps1'
    exit 4
  }
  try {
    $st = Get-Content $stateFile -Raw | ConvertFrom-Json
  } catch {
    Write-Host 'BLOQUEADO (exit 4): last-build.json ilegivel.'
    exit 4
  }
  if ($st.status -ne 'ok') {
    Write-Host ('BLOQUEADO (exit 4): ultimo build registrado como "{0}" em {1}.' -f $st.status, $st.built_at)
    Write-Host 'Compile antes: powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-build.ps1'
    exit 4
  }
  Write-Host ('Build registrado ok em {0} (head {1}).' -f $st.built_at, $st.git_head)
} else {
  Write-Host 'AVISO: gate de build pulado (-SkipBuildGate).'
}

# --- 4) URLs e user-secrets -----------------------------------------------------
$apiProj = Join-Path $Root 'backend\PlantaoPro.Api'
$webProj = Join-Path $Root 'backend\PlantaoPro.Web'
$apiCs   = Join-Path $apiProj 'PlantaoPro.Api.csproj'
$webCs   = Join-Path $webProj 'PlantaoPro.Web.csproj'
# IDs espelham <UserSecretsId> nos csproj (usados para redigir segredos no diagnostico)
$apiSecretId = 'plantaopro-api-development'
$apiUrl = Get-AppUrl $apiProj
$webUrl = Get-AppUrl $webProj
if (-not $apiUrl -or -not $webUrl) {
  Write-Host 'BLOQUEADO: nao foi possivel ler o launch profile https (launchSettings.json).'
  exit 2
}

# chaves com ':' (secrets.json eh plano; '__' nao vira seccao la dentro)
$neededApi = @('ConnectionStrings:Default','Jwt:Key','Jwt:Issuer','Jwt:Audience','Database:AllowLegacyPostgresDatabase','Database:AllowDevelopmentAutoCreate')
$neededWeb = @('ConnectionStrings:Default')
$haveApi = @(Get-SecretNames $apiCs)
$haveWeb = @(Get-SecretNames $webCs)
$missing = @()
foreach ($n in $neededApi) { if ($haveApi -notcontains $n) { $missing += ('API:' + $n) } }
foreach ($n in $neededWeb) { if ($haveWeb -notcontains $n) { $missing += ('WEB:' + $n) } }
if ($missing.Count -gt 0) {
  Write-Host 'BLOQUEADO: user-secrets ausentes (nomes apenas):'
  foreach ($m in $missing) { Write-Host ('  ' + $m) }
  Write-Host 'Configure: powershell -ExecutionPolicy Bypass -File scripts/local/setup-local-config.ps1'
  exit 2
}

# valores usados SOMENTE para redigir o diagnostico (nao sao impressos)
$secretRedact = @((Get-SecretValue $apiSecretId 'Jwt:Key'))
$cs = (Get-SecretValue $apiSecretId 'ConnectionStrings:Default')
$m = [regex]::Match($cs, '(?i)Password=([^;]+)')
if ($m.Success) { $secretRedact += $m.Groups[1].Value }

# --- 5) inicia a tentativa ------------------------------------------------------
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
  root       = $Root
  started_at = (Get-Date).ToString('o')
  api        = @{ launcher_pid = [int]$apiProc.Id; url = $apiUrl; out_log = $apiOut; err_log = $apiErr }
  web        = @{ launcher_pid = [int]$webProc.Id; url = $webUrl; out_log = $webOut; err_log = $webErr }
}
$regFile = Join-Path $logDir 'last-run.json'
$reg | ConvertTo-Json -Depth 4 | Set-Content -Path $regFile -Encoding utf8
Write-Host ('PIDs registrados em {0}' -f $regFile)

$launcherPids = @([int]$apiProc.Id, [int]$webProc.Id)

function Invoke-Failure([string]$reason) {
  Write-Host ('ERRO: ' + $reason)
  foreach ($l in @($apiErr, $webErr, $apiOut, $webOut)) {
    Write-Host ('--- ' + $l + ' ---')
    if (Test-Path $l) { Get-Content $l -Tail 8 | ForEach-Object { Write-Host (Redact $_ $script:secretRedact) } }
  }
  Write-Host 'Encerrando apenas os processos desta tentativa...'
  Stop-Attempt $launcherPids
  exit 5
}

# --- 6) readiness HTTP real -----------------------------------------------------
$apiProbe  = @{ url = ($apiUrl.TrimEnd('/') + '/api/health'); markers = @('"status":"Healthy"') }
$webProbe  = @{ url = ($webUrl.TrimEnd('/') + '/Account/Login'); markers = @('__RequestVerificationToken') }
$deadline = (Get-Date).AddSeconds($ReadyTimeoutSec)
$okApi = $false; $okWeb = $false
do {
  if (-not $okApi) { $r = Test-HttpReady $apiProbe.url $apiProbe.markers; if ($r.ok) { $okApi = $true } }
  if (-not $okWeb) { $r = Test-HttpReady $webProbe.url $webProbe.markers; if ($r.ok) { $okWeb = $true } }
  if (-not ($okApi -and $okWeb)) { Start-Sleep -Milliseconds 700 }
} while ((Get-Date) -lt $deadline -and -not ($okApi -and $okWeb))

if (-not ($okApi -and $okWeb)) {
  Invoke-Failure ("readiness nao atingida em {0}s (API={1}, Web={2})." -f $ReadyTimeoutSec, $okApi, $okWeb)
}
Write-Host ('API  ready: GET /api/health -> Healthy ({0})' -f $apiProbe.url)
Write-Host ('Web  ready: GET /Account/Login -> 200 com form ({0})' -f $webProbe.url)

# --- 7) liveness pos-init (prova que a inicializacao em segundo plano nao derruba) ---
Start-Sleep -Seconds 5
$rApi = Test-HttpReady $apiProbe.url $apiProbe.markers
$rWeb = Test-HttpReady $webProbe.url $webProbe.markers
if (-not ($rApi.ok -and $rWeb.ok)) {
  Invoke-Failure ('liveness pos-init falhou: API=' + $rApi.detail + ' | Web=' + $rWeb.detail)
}
Write-Host 'Liveness pos-init OK (re-probe a 5s).'

Write-Host 'OK: API e Web no ar a partir do build atual.'
exit 0
