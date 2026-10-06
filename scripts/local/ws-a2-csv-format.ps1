$ErrorActionPreference = 'Stop'
$root = 'C:\MNSOFT\plantaopro\backend\PlantaoPro.Web\Controllers'
$files = @(
  'Administrativo360Controller.RelatoriosFinanceiros.cs',
  'Administrativo360Controller.Financeiro.cs',
  'Administrativo360Controller.Relatorios.cs'
)
foreach ($f in $files) {
  $p = Join-Path $root $f
  $b = [System.IO.File]::ReadAllBytes($p)
  $hasBom = ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
  Write-Host ("{0} BOM={1}" -f $f, $hasBom)
  $c = [System.Text.Encoding]::UTF8.GetString($b)
  if ($hasBom) { $c = $c.Substring(1) }
  $n = [regex]::Replace($c, '([A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*)\.ToString\("0\.00", System\.Globalization\.CultureInfo\.InvariantCulture\)', 'ValorHumano.Format($1)')
  if ($n -ceq $c) {
    Write-Host ("SEM ALTERACAO: {0}" -f $f)
    continue
  }
  if ($n -notmatch 'PlantaoPro\.CrossCutting\.Localization') {
    $n = "using PlantaoPro.CrossCutting.Localization;`n" + $n
  }
  $enc = New-Object System.Text.UTF8Encoding($hasBom)
  [System.IO.File]::WriteAllText($p, $n, $enc)
  $trocas = ([regex]::Matches($c, 'ToString\("0\.00", System\.Globalization\.CultureInfo\.InvariantCulture\)')).Count
  Write-Host ("OK: {0} (trocas={1})" -f $f, $trocas)
}
