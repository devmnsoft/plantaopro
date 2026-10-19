# R66 correcao pontual: migrations-wrapper com "\ir ../schema/NNN..." - o alvo ja esta no
# scrpt_completo (seccao curada), mas o \ir resolve relativo ao arquivo que o contem e
# quebraria a importacao unica (exit 3). Solucao: remover os blocos das seccoes de gap
# correspondentes dos dois scripts e as entradas do source-checksums.json.
# Executa para a lista $chaves; blocos ja ausentes sao pulados com NAOACHADO_PULAR.
$ErrorActionPreference = 'Stop'
$root = (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))
$enc = New-Object System.Text.UTF8Encoding($false)
$chaves = @(
  'R66 gap instalacao: 2026_08_v187_fechamento_operacional_financeiro',
  'R66 gap instalacao: 2026_09_v2158_financeiro_medico_conferencia'
)

foreach ($alvo in @('database/scrpt_completo.sql', 'database/pgadmin/instalar_no_banco_atual.sql')) {
    $p = Join-Path $root $alvo
    foreach ($chave in $chaves) {
        $linhas = Get-Content $p -Encoding UTF8
        $iHdr = -1
        for ($i = 0; $i -lt $linhas.Count; $i++) { if ($linhas[$i] -match [regex]::Escape($chave)) { $iHdr = $i; break } }
        if ($iHdr -lt 0) { Write-Output ('NAOACHADO_PULAR=' + $chave); continue }
        $ini = $iHdr - 1                       # linha de separador antes do cabecalho
        $fim = $iHdr + 2                       # pula cabecalho + separador proprio; procura separador da proxima seccao
        while ($fim -lt $linhas.Count -and $linhas[$fim] -notmatch '^-- =+') { $fim++ }
        $novas = $linhas[0..($ini - 1)] + $linhas[$fim..($linhas.Count - 1)]
        [IO.File]::WriteAllLines($p, $novas, $enc)
        Write-Output ('REMOVIDO=' + $alvo + ' BLOCO=' + $chave + ' LINHAS_CORTADAS=' + ($fim - $ini) + ' RESTANTES=' + $novas.Count)
    }
}

$chkPath = Join-Path $root 'database/source-checksums.json'
$raw = [IO.File]::ReadAllText($chkPath, [System.Text.Encoding]::UTF8)
$temBom = $raw.StartsWith("`u{FEFF}")
if ($temBom) { $raw = $raw.Substring(1) }
$usaCrlf = $raw.Contains("`r`n")
$dict = @{}
$doc = $raw | ConvertFrom-Json
foreach ($prop in $doc.PSObject.Properties) { $dict[$prop.Name] = $prop.Value }
$bases = @('2026_08_v187_fechamento_operacional_financeiro', '2026_09_v2158_financeiro_medico_conferencia')
$chavesRemovidas = @($dict.Keys | Where-Object { $bases | Where-Object { $_ -like '*' + $bases[0] } })  # placeholder substituido abaixo
$chavesRemovidas = @($dict.Keys | ForEach-Object { $k = $_; if ($bases | Where-Object { $k -match ([regex]::Escape($_)) }) { $k } })
foreach ($k in $chavesRemovidas) { $dict.Remove($k) }
$obj = [ordered]@{}
foreach ($k in ($dict.Keys | Sort-Object)) { $obj[$k] = $dict[$k] }
$jsonNovo = ([PSCustomObject]$obj | ConvertTo-Json -Depth 3) -replace "`r`n", "`n"
if ($usaCrlf) { $jsonNovo = $jsonNovo -replace "`n", "`r`n" }
[IO.File]::WriteAllText($chkPath, ($jsonNovo.TrimEnd("`n") + [Environment]::NewLine), (New-Object System.Text.UTF8Encoding($temBom)))
Write-Output ('CHECKSUMS_REMOVIDOS=' + $chavesRemovidas.Count + ' TOTAL_AGORA=' + $dict.Count)
