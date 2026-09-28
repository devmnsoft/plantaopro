<#
.SYNOPSIS
  Bloco E (Homologacao Administrativo 360) - Verifica que os seeds oficiais podem ser
  re-aplicados SEM criar linhas duplicadas (idempotencia), no banco unico de testes.

.DESCRIPTION
  Conjunto de seeds aplicado em cada "run":
    1. Conjunto canonico local (o mesmo de scripts/database/apply-local-postgres.ps1):
       database/seeds.sql + database/seeds/*.sql (topo, ordenados por nome).
    2. Seeds opt-in de desenvolvimento (ordem documentada no README da pasta),
       quando -IncludeDevelopment (padrao true):
         database/seeds/development/121_acesso_demo_local.sql
         database/seeds/development/122_usuarios_homologacao_plantaopro.sql
         database/seeds/development/130_operacao_demo_santacasa.sql
         database/seeds/development/140_administrativo360_demo.sql
         database/seeds/development/141_administrativo360_suprimentos_demo.sql
       Excluidos de proposito: 120_acesso_demo_local.sql (legado, substituido por 121)
       e reset_usuarios_homologacao_plantaopro.sql (operacao guardada por variaveis
       psql, nao e um seed).

  Procedimento (SeedRuns vezes, padrao 2 = "seed x2" do brief):
    S0 (antes) -> [run 1: seeds completos] -> S1 -> [run 2: seeds completos] -> S2
      * delta_first  = S1 - S0 : linhas que a primeira aplicacao completou (informativo;
        esperado quando o banco ja tinha dados parciais);
      * delta_second = S2 - S1 : DUPLICACAO propriamente dita — deve ser ZERO em todas
        as tabelas para a verificacao passar.

  Contagens cobrem TODAS as tabelas base de schemas de usuario (qualquer schema,
  menos pg_*/information_schema). Exit code = quantidade de tabelas com
  delta_second diferente de zero (0 = IDEMPOTENTE). Falha em qualquer arquivo de
  seed => exit 1.

.PARAMETER DatabaseName
  Banco alvo (padrao plantaopro_test - banco unico de testes).
.PARAMETER SeedRuns
  Quantas vezes aplicar o conjunto completo de seeds (padrao 2).
.PARAMETER IncludeDevelopment
  Inclui os seeds opt-in de development (padrao true).
.PARAMETER RepoRoot
  Raiz do repo (padrao: dois niveis acima deste script).
.PARAMETER PgHost / PgPort / PgUser / PgPassword
  Conexao psql (senha lida de $env:PGPASSWORD; padrao local 123456 quando ausente).
.PARAMETER LogPath
  Quando informado, grava transcript completo no arquivo indicado.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts\homologacao\check-seed-idempotency.ps1 -LogPath .\seed-idempotencia.log
#>
param(
  [string]$DatabaseName = 'plantaopro_test',
  [int]$SeedRuns = 2,
  [bool]$IncludeDevelopment = $true,
  [string]$RepoRoot = '',
  [string]$PgHost = '127.0.0.1',
  [int]$PgPort = 5432,
  [string]$PgUser = 'postgres',
  [string]$PgPassword = $(if ($env:PGPASSWORD) { $env:PGPASSWORD } else { '123456' }),
  [string]$LogPath = ''
)

$ErrorActionPreference = 'Continue'

if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path }
if ($LogPath) { Start-Transcript -Path $LogPath -Append | Out-Null }
if (-not $env:PGPASSWORD) { Write-Host 'AVISO: $env:PGPASSWORD vazio - usando senha local padrao "123456" (mesmo padrao dos defaults versionados).' }

function Find-Psql {
  $cmd = Get-Command psql -ErrorAction SilentlyContinue
  if ($cmd) { return $cmd.Source }
  $found = Get-ChildItem 'C:\Program Files\PostgreSQL' -Directory -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending |
    ForEach-Object { Join-Path $_.FullName 'bin\psql.exe' } |
    Where-Object { Test-Path $_ } |
    Select-Object -First 1
  if ($found) { return $found }
  throw "psql nao encontrado (PATH nem C:\Program Files\PostgreSQL\*)."
}

function Invoke-PgText {
  param([string[]]$PsqlArgs)
  $env:PGPASSWORD = $PgPassword
  $out = (& $PsqlBin @PsqlArgs '-X' '-w' '-v' 'ON_ERROR_STOP=1' 2>&1 | Out-String)
  if ($LASTEXITCODE -ne 0) { throw ("psql falhou (exit " + $LASTEXITCODE + "): " + $out.Trim()) }
  return $out
}

function Get-TableCounts {
  # Devolve hashtable "schema.table" -> count (bigint). -1 para tabelas nao consultaveis.
  $sql = @("DROP TABLE IF EXISTS pp_seed_count_probe;",
           "CREATE TEMP TABLE pp_seed_count_probe (schema_name text, table_name text, cnt bigint);",
           "DO `$`$ DECLARE r RECORD; c bigint; BEGIN FOR r IN SELECT table_schema AS s, table_name AS t FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_schema NOT LIKE 'pg_%' AND table_schema <> 'information_schema' ORDER BY 1,2 LOOP BEGIN EXECUTE format('SELECT count(*) FROM %I.%I', r.s, r.t) INTO c; INSERT INTO pp_seed_count_probe VALUES (r.s, r.t, c); EXCEPTION WHEN OTHERS THEN INSERT INTO pp_seed_count_probe VALUES (r.s, r.t, -1); END; END LOOP; END `$`$;",
           "SELECT schema_name || '.' || table_name, cnt FROM pp_seed_count_probe ORDER BY 1;",
           "DROP TABLE IF EXISTS pp_seed_count_probe;") -join ' '
  $raw = Invoke-PgText @('-h', $PgHost, '-p', ([string]$PgPort), '-U', $PgUser, '-d', $DatabaseName, '-At', '-F', ' | ', '-c', $sql)
  $map = @{}
  foreach ($line in ($raw -split "`r?`n")) {
    $t = $line.Trim()
    if (-not $t) { continue }
    $idx = $t.LastIndexOf(' | ')
    if ($idx -lt 0) { continue }
    $name = $t.Substring(0, $idx)
    $val = $t.Substring($idx + 3)
    $n = 0L
    if ([long]::TryParse($val.Trim(), [ref]$n)) { $map[$name] = $n }
  }
  return $map
}

function Get-Delta([hashtable]$Before, [hashtable]$After) {
  $list = New-Object System.Collections.Generic.List[object]
  foreach ($k in ($Before.Keys + $After.Keys | Sort-Object -Unique)) {
    $b = 0L
    if ($Before.ContainsKey($k)) { $b = $Before[$k] }
    $a = 0L
    if ($After.ContainsKey($k)) { $a = $After[$k] }
    $d = $a - $b
    if ($d -ne 0) { $list.Add([pscustomobject]@{ Tabela = $k; Antes = $b; Depois = $a; Delta = $d }) }
  }
  return $list
}

$PsqlBin = Find-Psql
Write-Host '=== SEED IDEMPOTENCIA (Bloco E) ==='
Write-Host ("data={0}  db={1}  seedRuns={2}  includeDevelopment={3}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz'), $DatabaseName, $SeedRuns, $IncludeDevelopment)
Write-Host ("psql={0}" -f $PsqlBin)
Write-Host ("repo={0}" -f $RepoRoot)

# ---------- conjunto de seeds ----------
$seedFiles = @()
$mainSeed = Join-Path $RepoRoot 'database\seeds.sql'
if (Test-Path $mainSeed) { $seedFiles += $mainSeed }
$dirSeeds = Get-ChildItem (Join-Path $RepoRoot 'database\seeds') -Filter '*.sql' -ErrorAction SilentlyContinue | Sort-Object Name
foreach ($f in $dirSeeds) { $seedFiles += $f.FullName }
if ($IncludeDevelopment) {
  $devOrder = @(
    '121_acesso_demo_local.sql',
    '122_usuarios_homologacao_plantaopro.sql',
    '130_operacao_demo_santacasa.sql',
    '140_administrativo360_demo.sql',
    '141_administrativo360_suprimentos_demo.sql'
  )
  foreach ($name in $devOrder) {
    $p = Join-Path (Join-Path $RepoRoot 'database\seeds\development') $name
    if (Test-Path $p) { $seedFiles += $p } else { Write-Host ("AVISO: arquivo de seed nao encontrado: {0}" -f $p) }
  }
}
Write-Host ("arquivos de seed por run: {0}" -f $seedFiles.Count)

function Invoke-SeedRun([int]$RunIndex) {
  # Alguns seeds demo referenciam tabelas sem qualificar o schema (ex.: INSERT INTO clientes).
  # Via pgadmin/instalador o usuario da sessao ja ve o schema; via psql -f puro nao.
  # Prependemos "SET search_path TO plantaopro, public;" em um arquivo temporario por seed
  # (escrito com UTF-8 sem BOM para preservar acentos).
  foreach ($sf in $seedFiles) {
    $tmp = [IO.Path]::ChangeExtension([IO.Path]::GetTempFileName(), '.sql')
    try {
      $orig = [IO.File]::ReadAllText($sf)
      [IO.File]::WriteAllText($tmp, "SET search_path TO plantaopro, public;" + [Environment]::NewLine + $orig, (New-Object System.Text.UTF8Encoding($false)))
      & $PsqlBin -h $PgHost -p ([string]$PgPort) -U $PgUser -d $DatabaseName '-X' '-w' '-v' 'ON_ERROR_STOP=1' -f $tmp *> $null
      if ($LASTEXITCODE -ne 0) { throw ("Falha ao aplicar seed {0} (run {1}, exit {2})" -f (Split-Path $sf -Leaf), $RunIndex, $LASTEXITCODE) }
    } finally {
      Remove-Item $tmp -ErrorAction SilentlyContinue
    }
  }
  Write-Host ("[run {0}/{1}] seeds aplicados ok ({2} arquivos)" -f $RunIndex, $SeedRuns, $seedFiles.Count)
}

$snaps = @{}
$snaps['S0'] = Get-TableCounts
Write-Host ("tabelas monitoradas: {0}" -f $snaps['S0'].Count)

for ($i = 1; $i -le $SeedRuns; $i++) {
  try {
    Invoke-SeedRun $i
  } catch {
    Write-Host ("SEED FALHOU: {0}" -f $_.Exception.Message)
    if ($LogPath) { Stop-Transcript | Out-Null }
    exit 1
  }
  $snaps[('S' + $i)] = Get-TableCounts
}

# ---------- diff ----------
$d1 = @(Get-Delta $snaps['S0'] $snaps['S1'])
$d2 = @()
if ($SeedRuns -ge 2) { $d2 = @(Get-Delta $snaps['S1'] $snaps['S2']) } else { $d2 = $d1 }

Write-Host ''
Write-Host ('--- delta primeira aplicacao (S1-S0; informativo: linhas de catch-up) ---')
if ($d1.Count -eq 0) { Write-Host '  (nenhuma linha adicionada na primeira aplicacao)' }
else {
  foreach ($d in $d1) { Write-Host ('  {0}: {1} -> {2} (delta {3})' -f $d.Tabela, $d.Antes, $d.Depois, $d.Delta) }
}
Write-Host ''
Write-Host ('--- delta segunda aplicacao (S2-S1; deve ser ZERO em todas as tabelas) ---')
if ($d2.Count -eq 0) {
  Write-Host '  (nenhum delta) => SEED IDEMPOTENTE: segunda aplicacao nao criou linhas duplicadas'
} else {
  foreach ($d in ($d2 | Sort-Object Delta -Descending)) { Write-Host ('  {0}: {1} -> {2} (delta {3})' -f $d.Tabela, $d.Antes, $d.Depois, $d.Delta) }
  Write-Host ('{0} tabela(s) com delta != 0' -f $d2.Count)
}
Write-Host ''
Write-Host ("RESULTADO: fails={0}" -f $d2.Count)
if ($LogPath) { Stop-Transcript | Out-Null }
exit $d2.Count
