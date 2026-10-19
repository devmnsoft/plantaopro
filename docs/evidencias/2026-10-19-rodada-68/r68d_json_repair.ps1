# R68 (auxiliar): repara aspas duplas de fechamento perdidas em entradas dos dois
# source-checksums.json (efeito de execucao anterior do r68c cuja substituicao nao
# reemitia a aspa final do hash) e valida o JSON via ConvertFrom-Json.
$ErrorActionPreference = 'Stop'
$root = 'C:\MNSOFT\plantaopro'
$enc = New-Object System.Text.UTF8Encoding($false)
$jfiles = @(
  (Join-Path $root 'database/source-checksums.json'),
  (Join-Path $root 'docs/evidencias/2026-10-19-rodada-66/_backup_source-checksums.json')
)
$keys = @(
  'database/schema/000_schema_canonico_base.sql',
  'database/schema/010_identity_access.sql',
  'database/migrations/2026_v1186_schema_permissoes_compatibilidade.sql'
)
foreach ($jf in $jfiles) {
  $j = [IO.File]::ReadAllText($jf, $enc)
  $alterou = $false
  foreach ($k in $keys) {
    $p = ('"' + [regex]::Escape($k) + '":\s*"') + '([0-9a-f]{64})'
    $m = (New-Object System.Text.RegularExpressions.Regex $p).Matches($j)
    if ($m.Count -eq 0) { Write-Host ((Split-Path $jf -Leaf) + ' SEM_CHAVE ' + $k); continue }
    if ($m.Count -ne 1) { throw ((Split-Path $jf -Leaf) + ' chave ' + $k + ' matches=' + $m.Count) }
    $pos = $m[0].Index + $m[0].Length
    $ch = $j[$pos]
    if ($ch -eq [char]'"') {
      Write-Host ((Split-Path $jf -Leaf) + ' OK       ' + $k)
    } elseif ($ch -eq [char]',' -or $ch -eq [char]'}') {
      $j = $j.Insert($pos, '"')
      $alterou = $true
      Write-Host ((Split-Path $jf -Leaf) + ' REPARADO ' + $k)
    } else {
      throw ((Split-Path $jf -Leaf) + ' chave ' + $k + ' char inesperado apos hash codigo=' + [int]$ch)
    }
  }
  if ($alterou) { [IO.File]::WriteAllText($jf, $j, $enc) }
  try { $null = $j | ConvertFrom-Json; Write-Host ((Split-Path $jf -Leaf) + ' JSON_VALIDO') }
  catch { throw ((Split-Path $jf -Leaf) + ' JSON_INVALIDO: ' + $_.Exception.Message) }
}
Write-Host 'R68D_OK'
