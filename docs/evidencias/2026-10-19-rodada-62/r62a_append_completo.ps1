$ErrorActionPreference = 'Stop'
$root = (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))
$mig = Join-Path $root 'database/migrations/2026_10_v2356_r62_ai_provedor_drael.sql'
$sha = (Get-FileHash $mig -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Output ("SHA256=" + $sha)

$c = Get-Content (Join-Path $root 'database/scrpt_completo.sql') -Encoding UTF8
# Reaproveita os caracteres exatos da seccao anterior (Seccao/em-dash) para manter a codificacao.
$oldHdr = $c[10229]
$numIdx = $oldHdr.IndexOf('126')
$prefix = $oldHdr.Substring(0, $numIdx)      # "-- Seccao "
$tail = $oldHdr.Substring($numIdx + 3)       # " — R56 ..."
$dashPart = $tail.Substring(0, $tail.IndexOf('R'))   # " — "
$newHdr = $prefix + '127' + $dashPart + 'R62 AI Provedor Drael'
Write-Output ("NOVA_HDRA_PRESO=" + $newHdr.Length)

$sep = $c[10228]
$enc = New-Object System.Text.UTF8Encoding($false)

$migText = (Get-Content $mig -Encoding UTF8) -join "`r`n"
$block = $sep + "`r`n" + $newHdr + "`r`n" + $sep + "`r`n" +
         "`r`n" +
         "-- SOURCE: database/migrations/2026_10_v2356_r62_ai_provedor_drael.sql`r`n" +
         "-- SOURCE-SHA256: " + $sha + "`r`n" +
         $migText + "`r`n"

foreach ($alvo in @('database/scrpt_completo.sql', 'database/pgadmin/instalar_no_banco_atual.sql')) {
    $p = Join-Path $root $alvo
    if ((Get-Content $p -Encoding UTF8) -match 'v2356_r62') { Write-Output ("JAPENDIDO_PULAR=" + $alvo); continue }
    $antes = [IO.File]::ReadAllBytes($p); $tailAntes = ($antes[-4..-1] | ForEach-Object { $_.ToString('X2') }) -join ''
    [IO.File]::AppendAllText($p, $block, $enc)
    $depois = [IO.File]::ReadAllBytes($p); $tailDepois = ($depois[-4..-1] | ForEach-Object { $_.ToString('X2') }) -join ''
    Write-Output ("APENDIDO=$alvo ANTES_TAIL=" + $tailAntes + " DEPOIS_TAIL=" + $tailDepois + " GANHOU_BYTES=" + ($depois.Length - $antes.Length))
}
