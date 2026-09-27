# run-dev-stop.ps1 -- para as instancias da API e da Web desta copia do PlantaoPro
# e libera os artefatos de build (DLLs) para compilacao.
#
# Uso:
#   powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-stop.ps1 [-WaitSeconds 30]
#
# Exit codes: 0 ok | 2 processo nao encerrou no prazo | 4 artefato ainda bloqueado
param([int]$WaitSeconds = 30)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$BinPattern = Join-Path $Root 'backend*'

function Find-AppInstances {
  # 1) exes desta copia (rodeado direto ou por "dotnet run" / depuracao do VS)
  $direct = @(Get-CimInstance Win32_Process | Where-Object {
    $_.Name -match '^PlantaoPro\.(Web|Api)\.exe$' -and $_.ExecutablePath -and ($_.ExecutablePath -like $BinPattern)
  })
  # 2) pais "dotnet run" cujos filhos sao os exes acima (para nao deixar o launcher zombie)
  $parentIds = @()
  foreach ($d in $direct) {
    $pp = Get-CimInstance Win32_Process -Filter ('ProcessId={0}' -f [int]$d.ParentProcessId) -ErrorAction SilentlyContinue
    if ($pp -and $pp.Name -eq 'dotnet.exe' -and $pp.CommandLine -match '\brun\b') { $parentIds += [int]$pp.ProcessId }
  }
  $parents = @()
  foreach ($id in ($parentIds | Sort-Object -Unique)) {
    $p = Get-CimInstance Win32_Process -Filter ('ProcessId={0}' -f $id) -ErrorAction SilentlyContinue
    if ($p) { $parents += $p }
  }
  ,@{ Direct = $direct; Parents = $parents }
}

$result = Find-AppInstances
$apps = @($result.Direct); $runs = @($result.Parents)
if (-not $apps.Count -and -not $runs.Count) {
  Write-Host 'OK: nenhuma instancia da API/Web em execucao (nada a fazer).'
  exit 0
}

$t0 = Get-Date
Write-Host 'Instancias desta copia identificadas:'
foreach ($d in $apps)  { Write-Host ('  PID {0} {1} [app]   {2}' -f $d.ProcessId, $d.Name, $d.ExecutablePath) }
foreach ($p in $runs)  { Write-Host ('  PID {0} dotnet.exe  [dotnet run]  {1}' -f $p.ProcessId, $p.CommandLine) }

# filhos primeiro (o "dotnet run" percebe a saida e tambem termina), depois os launchers
foreach ($d in $apps) { Stop-Process -Id ([int]$d.ProcessId) -Force -ErrorAction SilentlyContinue; Write-Host ('  parado (app): PID ' + $d.ProcessId) }
Start-Sleep -Milliseconds 500
foreach ($p in $runs) {
  $still = Get-Process -Id ([int]$p.ProcessId) -ErrorAction SilentlyContinue
  if ($still) { Stop-Process -Id ([int]$p.ProcessId) -Force -ErrorAction SilentlyContinue; Write-Host ('  parado (launcher): PID ' + $p.ProcessId) }
}

# aguarda saida completa com limite de tempo
$ids = @(); foreach ($d in $apps) { $ids += [int]$d.ProcessId }; foreach ($p in $runs) { $ids += [int]$p.ProcessId }
$deadline = (Get-Date).AddSeconds($WaitSeconds)
$left = @()
do {
  $left = @(Get-CimInstance Win32_Process | Where-Object { $_.ProcessId -in $ids })
  if ($left.Count) { Start-Sleep -Milliseconds 300 }
} while ($left.Count -gt 0 -and (Get-Date) -lt $deadline)

if ($left.Count -gt 0) {
  Write-Host ('ERRO: processo(s) nao encerraram em {0}s:' -f $WaitSeconds)
  foreach ($l in $left) { Write-Host ('  PID ' + $l.ProcessId + ' ' + $l.Name) }
  Write-Host 'Investigue (ex.: depuracao aberta no Visual Studio) e rode novamente.'
  exit 2
}
Write-Host (('Processos parados em {0}s (limite {1}s).') -f [math]::Round(((Get-Date) - $t0).TotalSeconds, 1), $WaitSeconds)

# confere se os artefatos que causavam MSB3021/MSB3027 estao realmente livres
$probeFiles = @(
  (Join-Path $Root 'backend\PlantaoPro.Api\bin\Debug\net10.0\PlantaoPro.Application.dll'),
  (Join-Path $Root 'backend\PlantaoPro.Api\bin\Debug\net10.0\PlantaoPro.Domain.dll'),
  (Join-Path $Root 'backend\PlantaoPro.Api\bin\Debug\net10.0\PlantaoPro.Infrastructure.dll'),
  (Join-Path $Root 'backend\PlantaoPro.Api\bin\Debug\net10.0\PlantaoPro.CrossCutting.dll'),
  (Join-Path $Root 'backend\PlantaoPro.Api\bin\Debug\net10.0\PlantaoPro.Api.dll'),
  (Join-Path $Root 'backend\PlantaoPro.Web\bin\Debug\net10.0\PlantaoPro.Web.dll'),
  (Join-Path $Root 'backend\PlantaoPro.Web\bin\Debug\net10.0\PlantaoPro.CrossCutting.dll')
)
$locked = @()
foreach ($f in $probeFiles) {
  if (-not (Test-Path $f)) { continue }
  try { $s = [IO.File]::Open($f, 'Open', 'ReadWrite', 'None'); $s.Close() }
  catch { $locked += $f }
}
if ($locked.Count -gt 0) {
  Write-Host 'ERRO: artefato(s) ainda com handle aberto por outro processo:'
  foreach ($f in $locked) { Write-Host ('  ' + $f) }
  Write-Host 'Identifique quem segura o arquivo antes de compilar (ex.: handle.exe ou Process Explorer).'
  exit 4
}
Write-Host 'OK: processos parados e artefatos de build livres. Pode compilar.'
exit 0
