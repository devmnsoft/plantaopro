$ErrorActionPreference = 'Stop'
Set-Location 'C:\MNSOFT\plantaopro'
$out = 'C:\Users\NCELL-DEV-020\AppData\Local\Temp\opencode\r73_gap_scan.txt'
$sw = New-Object System.IO.StreamWriter($out, $false, (New-Object System.Text.UTF8Encoding($false)))

# --- vocabularios -------------------------------------------------------------
# Acrónimos em CAIXA ALTA (case-sensitive, word-boundary) — jargão em texto visível.
$acr = 'API|BFF|UUID|GUID|BLOB|CRUD|CSRF|JWT|OAuth|SQL|JSON|CHECKSUM|WEBHOOK|ANTIFORGERY'
# Palavras em minúscula (case-insensitive, word-boundary) — valores de resx (contexto seguro).
$palavras = 'tenant|tenants|inquilino|inquilinos|outbox|payload|payloads|json|http|antiforgery|server|servers|log|logs|fallback|cache|webhook|webhooks|hash|checksum|checksums|seed|seeds|bootstrap|exceção|excecao|backend|frontend|migração|migracao|migration|schema|stack|blob|multipart|cookie|cookies|debug|uuid|guid|api|bff|endpoint|endpoints'
$frases = 'banco de dados|base de dados|ocorreu uma exce|uma exceção|stack trace|trace de pilha|código de status|status code|codigo de status|token de sess[ãa]o cr[uú]|link expirou sem|sem conectividade com o servidor'

function Test-WordRe([string]$haystack, [bool]$ignoreCase, [string]$pattern) {
    $opts = if ($ignoreCase) { [System.Text.RegularExpressions.RegexOptions]::IgnoreCase } else { [System.Text.RegularExpressions.RegexOptions]::None }
    return [System.Text.RegularExpressions.Regex]::Matches($haystack, '\b(' + $pattern + ')\b', $opts)
}

$totResx = 0; $totView = 0
# --- 1) resx: todos os valores das 4 culturas --------------------------------
foreach ($nome in @('UiTexts.resx', 'UiTexts.en.resx', 'UiTexts.es.resx', 'UiTexts.fr.resx')) {
    $p = "backend\PlantaoPro.Web\Localization\$nome"
    $t = [IO.File]::ReadAllText((Resolve-Path $p).Path, [Text.Encoding]::UTF8)
    $datas = [System.Text.RegularExpressions.Regex]::Matches($t, '<data name="([^"]+)"[^>]*>\s*<value>(.*?)</value>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    foreach ($d in $datas) {
        $chave = $d.Groups[1].Value
        $valor = $d.Groups[2].Value
        if ([string]::IsNullOrWhiteSpace($valor)) { continue }
        $hits = @()
        foreach ($m in Test-WordRe $valor $true $palavras) { $hits += ('i:' + $m.Groups[1].Value) }
        foreach ($m in Test-WordRe $valor $false $acr) { $hits += ('cs:' + $m.Groups[1].Value) }
        foreach ($h in $hits) {
            $snip = $valor -replace '\s+', ' '
            if ($snip.Length -gt 160) { $snip = $snip.Substring(0, 160) + '…' }
            $sw.WriteLine("RESX|$nome|$chave|$h|$snip")
            $totResx++
        }
    }
}
# --- 2) views: frases (ci) + acrónimos (CS) linha a linha --------------------
$viewsDir = 'backend\PlantaoPro.Web\Views'
foreach ($f in (Get-ChildItem $viewsDir -Recurse -Filter '*.cshtml' | Where-Object { $_.FullName -notmatch '\\(Models|Shared\\_Layout)\\' })) {
    $rel = $f.FullName.Substring((Resolve-Path $viewsDir).Path.Length + 1) -replace '\\', '/'
    $lines = [IO.File]::ReadAllLines($f.FullName, [Text.Encoding]::UTF8)
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $ln = $lines[$i]
        if ([string]::IsNullOrWhiteSpace($ln)) { continue }
        $hits = @()
        foreach ($m in [System.Text.RegularExpressions.Regex]::Matches($ln, '(' + $frases + ')', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) { $hits += ('i:' + $m.Value) }
        foreach ($m in [System.Text.RegularExpressions.Regex]::Matches($ln, '\b(' + $acr + ')\b', [System.Text.RegularExpressions.RegexOptions]::None)) { $hits += ('cs:' + $m.Groups[1].Value) }
        foreach ($m in [System.Text.RegularExpressions.Regex]::Matches($ln, '\b(payload|endpoints?|webhooks?|outbox|bootstrap|multipart|stack trace|c[oó]digo de status)\b', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) { $hits += ('i:' + $m.Value) }
        foreach ($h in $hits) {
            $snip = $ln.Trim()
            if ($snip.Length -gt 160) { $snip = $snip.Substring(0, 160) + '…' }
            $sw.WriteLine("VIEW|$rel|L$($i+1)|$h|$snip")
            $totView++
        }
    }
}
$sw.WriteLine("")
$sw.WriteLine("TOTAL_RESX_HITS=" + $totResx)
$sw.WriteLine("TOTAL_VIEW_HITS=" + $totView)
$sw.Dispose()
Write-Output ('SCAN_DONE resx=' + $totResx + ' view=' + $totView)
