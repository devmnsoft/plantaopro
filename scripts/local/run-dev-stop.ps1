# run-dev-stop.ps1 -- para as instancias da API e da Web desta copia do PlantaoPro
# e libera os artefatos de build (DLLs) para compilacao.
#
# Regras de seguranca:
#   - somente processos desta copia (exe sob <root>\backend\** ou "dotnet run" pai deles);
#   - ordem filho -> pai;
#   - identidade (nome + caminho do exe) reconfirmada imediatamente antes de cada Stop-Process;
#   - -Force restrito aos PIDs identificados acima (nada alheio ao projeto e tocado).
#
# Uso:
#   powershell -ExecutionPolicy Bypass -File scripts/local/run-dev-stop.ps1 [-WaitSeconds 30]
#
# Exit codes: 0 ok | 2 processo nao encerrou no prazo | 4 artefato ainda bloqueado
param([int]$WaitSeconds = 30)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$BinPattern = Join-Path $Root 'backend*'

function Is-AppExe([psobject]$p) {
  return ($p.Name -match '^PlantaoPro\.(Web|Api)\.exe$' -and $p.ExecutablePath -and ($p.ExecutablePath -like $BinPattern))
}

function Confirm-AndKill([int]$pid_, [string]$kind) {
  # reconfirma identidade no momento exato da matança (evita race com reuso de PID)
  $p = Get-CimInstance Win32_Process -Filter ('ProcessId={0}' -f $pid_) -ErrorAction SilentlyContinue
  if (-not $p) { Write-Host ('  ja encerrado antes: PID ' + $pid_); return }
  $isApp = ($p.Name -match '^PlantaoPro\.(Web|Api)\.exe$')
  $isLauncher = ($p.Name -eq 'dotnet.exe' -and $p.CommandLine -match '\brun\b')
  if (-not $isApp -and -not $isLauncher) {
    Write-Host ('  ignorado (identidade mudou, nao matado): PID ' + $pid_ + ' agora=' + $p.Name)
    return
  }
  Stop-Process -Id $pid_ -Force -ErrorAction SilentlyContinue
  Write-Host ('  parado ({0}): PID {1} {2}' -f $kind, $pid_, $p.Name)
}

# 1) exes desta copia (rodeado direto ou por "dotnet run" / depuracao do VS)
$direct = @(Get-CimInstance Win32_Process | Where-Object { Is-AppExe $_ })
# 2) pais "dotnet run" cujos filhos sao os exes acima (para nao deixar o launcher zombie)
$parentIds = @()
foreach ($d in $direct) {
  $pp = Get-CimInstance Win32_Process -Filter ('ProcessId={0}' -f [int]$d.ParentProcessId) -ErrorAction SilentlyContinue
  if ($pp -and $pp.Name -eq 'dotnet.exe' -and $pp.CommandLine -match '\brun\b') { $parentIds += [int]$pp.ProcessId }
}
$parentIds = @($parentIds | Sort-Object -Unique)

if (-not $direct.Count -and -not $parentIds.Count) {
  Write-Host 'OK: nenhuma instancia da API/Web em execucao (nada a fazer).'
  exit 0
}

$t0 = Get-Date
Write-Host 'Instancias desta copia identificadas:'
foreach ($d in $direct) { Write-Host ('  PID {0} {1} [app]       {2}' -f $d.ProcessId, $d.Name, $d.ExecutablePath) }
foreach ($id in $parentIds) {
  $p = Get-CimInstance Win32_Process -Filter ('ProcessId={0}' -f $id) -ErrorAction SilentlyContinue
  if ($p) { Write-Host ('  PID {0} dotnet.exe  [dotnet run]  {1}' -f $id, $p.CommandLine) }
}

# filhos primeiro, depois os launchers (cada matada reconfirma a identidade)
foreach ($d in $direct) { Confirm-AndKill ([int]$d.ProcessId) 'app' }
Start-Sleep -Milliseconds 500
foreach ($id in $parentIds) { Confirm-AndKill $id 'launcher' }

# aguarda saida completa com limite de tempo
$ids = @(); foreach ($d in $direct) { $ids += [int]$d.ProcessId }; $ids += $parentIds
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
