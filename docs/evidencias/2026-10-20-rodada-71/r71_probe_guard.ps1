$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$src = [IO.File]::ReadAllText((Join-Path $root 'backend\PlantaoPro.Web\Localization\EstadoVisual.cs'))
$mapa = @{}
foreach ($m in [regex]::Matches($src, 'M\("([a-z0-9][a-z0-9-]*)"\s*,\s*"([a-z]+)"\s*,\s*"(bi-[a-z0-9-]+)"\)')) { $mapa[$m.Groups[1].Value] = "$($m.Groups[2].Value)|$($m.Groups[3].Value)" }
$sql = [IO.File]::ReadAllText((Join-Path $root 'database\scrpt_completo.sql'))
$guardRe = [regex]'check\s*\(\s*status\s+in\s*\(([^)]*)\)'
$valores = $guardRe.Matches($sql) | ForEach-Object { $_.Groups[1].Value } | ForEach-Object { [regex]::Matches($_, "'([A-Z][A-Z0-9_]*)'") } | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
function Norm([string]$v) { $v = $v.Trim().ToLower(); $v = $v -replace ' ', '-' -replace '_', '-'; $v = $v -replace 'á','a' -replace 'à','a' -replace 'ã','a' -replace 'â','a' -replace 'é','e' -replace 'ê','e' -replace 'í','i' -replace 'ó','o' -replace 'õ','o' -replace 'ô','o' -replace 'ú','u' -replace 'ç','c'; return $v }
Write-Output ("SLUGS_MAPA=" + $mapa.Count)
Write-Output ("VALORES_GUARD=" + $valores.Count)
$fora = @()
foreach ($v in $valores) { $s = Norm $v; if (-not $mapa.ContainsKey($s)) { $fora += "$v -> $s" } }
if ($fora.Count -eq 0) { Write-Output 'FORA=NENHUM' } else { $fora | ForEach-Object { Write-Output ("FORA|$_") } }
