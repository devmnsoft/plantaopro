# R66 correcao pontual: a migration 2026_v1410 e um wrapper com "\ir ../schema/270_v1410...",
# cujo alvo JA esta no scrpt_completo (secao curada). O \ir resolve relativo ao arquivo
# que o contem e quebraria a importacao unica (exit 3). Solucao: remover o bloco da seccao
# 165 dos dois scripts e a entrada correspondente do source-checksums.json.
$ErrorActionPreference = 'Stop'
$root = (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))
$enc = New-Object System.Text.UTF8Encoding($false)
$chave = 'R66 gap instalacao: 2026_v1410_cobertura_escalas_fechamento_financeiro'

foreach ($alvo in @('database/scrpt_completo.sql', 'database/pgadmin/instalar_no_banco_atual.sql')) {
    $p = Join-Path $root $alvo
    $linhas = Get-Content $p -Encoding UTF8
    $iHdr = -1
    for ($i = 0; $i -lt $linhas.Count; $i++) { if ($linhas[$i] -match [regex]::Escape($chave)) { $iHdr = $i; break } }
    if ($iHdr -lt 0) { Write-Output ('NAOACHADO_PULAR=' + $alvo); continue }
    $ini = $iHdr - 1                       # linha de separador antes do cabecalho
    $fim = $iHdr + 2                       # pula cabecalho + separador proprio; procura separador da proxima seccao
    while ($fim -lt $linhas.Count -and $linhas[$fim] -notmatch '^-- =+') { $fim++ }
    # $fim aponta para o separador da proxima seccao: corta ate $fim - 1
    $novas = $linhas[0..($ini - 1)] + $linhas[$fim..($linhas.Count - 1)]
    [IO.File]::WriteAllLines($p, $novas, $enc)
    Write-Output ('REMOVIDO=' + $alvo + ' LINHAS_CORTADAS=' + ($fim - $ini) + ' RESTANTES=' + $novas.Count)
}

$chkPath = Join-Path $root 'database/source-checksums.json'
$raw = [IO.File]::ReadAllText($chkPath, [System.Text.Encoding]::UTF8)
$temBom = $raw.StartsWith("`u{FEFF}")
if ($temBom) { $raw = $raw.Substring(1) }
$usaCrlf = $raw.Contains("`r`n")
$dict = @{}
$doc = $raw | ConvertFrom-Json
foreach ($prop in $doc.PSObject.Properties) { $dict[$prop.Name] = $prop.Value }
$chavesRemovidas = @($dict.Keys | Where-Object { $_ -like '*2026_v1410_cobertura_escalas_fechamento_financeiro*' })
foreach ($k in $chavesRemovidas) { $dict.Remove($k) }
$obj = [ordered]@{}
foreach ($k in ($dict.Keys | Sort-Object)) { $obj[$k] = $dict[$k] }
$jsonNovo = ([PSCustomObject]$obj | ConvertTo-Json -Depth 3) -replace "`r`n", "`n"
if ($usaCrlf) { $jsonNovo = $jsonNovo -replace "`n", "`r`n" }
[IO.File]::WriteAllText($chkPath, ($jsonNovo.TrimEnd("`n") + [Environment]::NewLine), (New-Object System.Text.UTF8Encoding($temBom)))
Write-Output ('CHECKSUMS_REMOVIDOS=' + $chavesRemovidas.Count + ' TOTAL_AGORA=' + $dict.Count)
