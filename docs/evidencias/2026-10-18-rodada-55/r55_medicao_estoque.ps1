$ErrorActionPreference='Stop'
# R55 — Medição versionada da latência das queries da view adm360_saldos (banco de teste, tenant demo).
# Sequência controlada: (A) índice presente -> mede; (B) drop do índice -> mede (baseline de repro);
# (C) reaplica a migration v2354 -> mede de novo + prova que o plano usa o índice.
$env:PGPASSWORD='123456'
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$psqlArgs = @('-h','127.0.0.1','-p','5432','-U','postgres','-d','plantaopro_test','-X','-q')
$mig = 'C:\MNSOFT\plantaopro\database\migrations\2026_10_v2354_r55_indice_reservas_saldo.sql'
$t = 'd3f6584c-2c64-4e5a-9ea9-4e1428647502'
$countQ = "SELECT count(*) FROM plantaopro.adm360_saldos WHERE tenant_id='$t';"
$sumQ = "SELECT COALESCE(SUM(fisico),0), COALESCE(SUM(reservado),0), COALESCE(SUM(disponivel),0) FROM plantaopro.adm360_saldos WHERE tenant_id='$t';"
$rowQ = "SELECT produto_id, produto, lote_id, lote, validade, local_id, local, condicao, fisico, reservado, disponivel FROM plantaopro.adm360_saldos WHERE tenant_id='$t' ORDER BY produto, validade NULLS LAST LIMIT 100;"

function Medir([string]$tag, [bool]$printValues) {
  $out = & $psql @psqlArgs '-c' '\timing on' '-c' $countQ '-c' $sumQ '-c' $rowQ | Out-String
  $tempos = [regex]::Matches($out, '(?m)^Tempo: ([\d,]+) ms')
  if ($tempos.Count -ne 3) { throw "Medicao ${tag}: esperados 3 tempos, obtidos $($tempos.Count)" }
  $ms = { param($i) [double]($tempos[$i].Groups[1].Value -replace ',', '.') }
  Write-Output ($tag + ' COUNT_MS=' + $ms.Invoke(0) + ' SUM_MS=' + $ms.Invoke(1) + ' ROWS_MS=' + $ms.Invoke(2))
  if ($printValues) {
    $v = & $psql @psqlArgs '-c' "SELECT COALESCE(SUM(fisico),0) || '/' || COALESCE(SUM(reservado),0) || '/' || COALESCE(SUM(disponivel),0) FROM plantaopro.adm360_saldos WHERE tenant_id='$t';" | Where-Object { $_ -match '^\s*[\d.]+/[\d.]+/[\d.]+' }
    Write-Output ($tag + ' VALOR_FISICO_RESERVADO_DISPONIVEL=' + $v.Trim())
  }
}

# Estado inicial limpo: remove o índice de probe da recon, se ainda existir.
& $psql @psqlArgs '-c' "DROP INDEX IF EXISTS plantaopro.tmp_r55_probe_reservas;" | Out-Null

Write-Output '--- FASE A: indice presente ---'
& $psql @psqlArgs '-f' $mig | Out-Null
Medir 'A_COM_INDICE' $true

Write-Output '--- FASE B: baseline sem indice (repro) ---'
& $psql @psqlArgs '-c' "DROP INDEX IF EXISTS plantaopro.ix_adm360_reservas_saldo;" | Out-Null
Medir 'B_SEM_INDICE' $true

Write-Output '--- FASE C: migration v2354 reaplicada ---'
& $psql @psqlArgs '-f' $mig | Out-Null
Medir 'C_COM_INDICE' $true
& $psql @psqlArgs '-c' "SELECT count(*) FROM pg_indexes WHERE schemaname='plantaopro' AND tablename='adm360_reservas' AND indexname='ix_adm360_reservas_saldo';" | Where-Object { $_ -match '^\s*\d' }
& $psql @psqlArgs '-c' '\timing on' '-c' "EXPLAIN (ANALYZE, BUFFERS) $sumQ" | Select-String -Pattern 'ix_adm360_reservas_saldo|Execution Time'
Write-Output 'R55_MEDICAO_OK'
