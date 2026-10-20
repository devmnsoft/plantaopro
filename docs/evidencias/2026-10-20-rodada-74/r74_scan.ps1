$ErrorActionPreference = 'Stop'
Set-Location 'C:\MNSOFT\plantaopro'
$tmp = 'C:\Users\NCELL-DEV-020\AppData\Local\Temp\opencode'
$ds = 'backend\PlantaoPro.Web\wwwroot\css'

function Get-Luz($r,$g,$b) {
    function Canal($v) { $s = $v / 255.0; if ($s -le 0.03928) { return $s / 12.92 }; return [Math]::Pow(($s + 0.055) / 1.055, 2.4) }
    return (0.2126 * (Canal $r)) + (0.7152 * (Canal $g)) + (0.0722 * (Canal $b))
}
function Get-Razao($fgBg1,$fgBg2) {
    $la = Get-Luz $fgBg1[0] $fgBg1[1] $fgBg1[2]; $lb = Get-Luz $fgBg2[0] $fgBg2[1] $fgBg2[2]
    $hi = [Math]::Max($la,$lb); $lo = [Math]::Min($la,$lb)
    return ($hi + 0.05) / ($lo + 0.05)
}
function ConvertTo-Rgb([string]$spec) {
    $v = ("$spec").Trim()
    if ($v.EndsWith('!important')) { $v = $v.Substring(0, $v.Length - 10).Trim() }
    if ($v.StartsWith('#')) {
        $h = $v.Substring(1)
        if ($h.Length -eq 3) { $h = "$($h[0])$($h[0])$($h[1])$($h[1])$($h[2])$($h[2])" }
        if ($h.Length -lt 6) { return $null }
        $r = [Convert]::ToInt32($h.Substring(0,2),16); $g = [Convert]::ToInt32($h.Substring(2,2),16); $b = [Convert]::ToInt32($h.Substring(4,2),16)
        return @($r,$g,$b)
    }
    $m = [regex]::Match($v, 'rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)')
    if ($m.Success) { return @([int]$m.Groups[1].Value,[int]$m.Groups[2].Value,[int]$m.Groups[3].Value) }
    return $null
}
function Get-Blocos([string]$txt) {
    $i = 0
    while ($i -lt $txt.Length) {
        $ob = $txt.IndexOf('{', $i)
        if ($ob -lt 0) { break }
        $fe = $txt.IndexOf('}', $ob)
        if ($fe -lt 0) { break }
        $sel = $txt.Substring($i, $ob - $i).Trim()
        $dec = $txt.Substring($ob + 1, $fe - $ob - 1)
        [pscustomobject]@{ Sel = $sel; Dec = $dec; Pos = $ob }
        $i = $fe + 1
    }
}
function Get-LinNum([string]$txt, [int]$pos) {
    return ([regex]::Matches($txt.Substring(0, $pos), "`n").Count + 1)
}

$sw = New-Object System.IO.StreamWriter("$tmp\r74_gap_scan.txt", $false, (New-Object System.Text.UTF8Encoding($false)))

# --- PASS 1: mapa de tokens (arquivos da casa, sem vendor) ---
$mapas = @{}
$pares = @(
    @('design-system/tokens.css', (Join-Path $ds 'design-system\tokens.css')),
    @('design-system/v2150-bloco-c-canonical.css', (Join-Path $ds 'design-system\v2150-bloco-c-canonical.css')),
    @('design-system/v2151-wp-c-ajuste-canonical.css', (Join-Path $ds 'design-system\v2151-wp-c-ajuste-canonical.css'))
)
foreach ($par in $pares) {
    $nome = $par[0]; $p = Resolve-Path $par[1]
    $t = [IO.File]::ReadAllText($p.Path, [Text.Encoding]::UTF8)
    $blocoMap = @{}
    foreach ($m in [regex]::Matches($t, ':root\s*\{')) {
        $ini = $m.Index + $m.Length - 1
        $prof = 1; $j = $ini + 1
        while ($j -lt $t.Length -and $prof -gt 0) { if ($t[$j] -eq '{') { $prof++ } elseif ($t[$j] -eq '}') { $prof-- }; $j++ }
        $corpo = $t.Substring($ini + 1, $j - $ini - 2)
        foreach ($d in [regex]::Matches($corpo, '(--[a-zA-Z0-9-]+)\s*:\s*([^;{}]+)')) { $blocoMap[$d.Groups[1].Value] = $d.Groups[2].Value.Trim() }
    }
    $mapas[$nome] = $blocoMap
    foreach ($k in ($blocoMap.Keys | Sort-Object)) { $sw.WriteLine("TOKEN|$nome|$k|$($blocoMap[$k])") }
}
foreach ($nome in $mapas.Keys) { $sw.WriteLine("TOKENCOUNT|$nome|$($mapas[$nome].Count)") }
$sw.WriteLine('')

# --- PASS 2: pares literais cor+fundo no CSS nao-vendor ---
$cssDir = (Resolve-Path $ds).Path
$cssArqs = Get-ChildItem $cssDir -Recurse -Filter '*.css' | Where-Object { $_.FullName -notmatch '\\vendor\\' }
foreach ($f in $cssArqs) {
    $rel = $f.FullName.Substring($cssDir.Length + 1) -replace '\\','/'
    $t = [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
    foreach ($bl in (Get-Blocos $t)) {
        if ($bl.Sel -match '@media|@keyframes|@supports') { continue }
        $decs = $bl.Dec -split ';'
        $cor = $null; $fundo = $null; $temImg = $false
        foreach ($d in $decs) {
            $mCor = [regex]::Match($d, '^\s*color\s*:\s*(.+)$')
            if ($mCor.Success) { $cor = $mCor.Groups[1].Value.Trim() }
            $mBg = [regex]::Match($d, '^\s*background(?:-color)?\s*:\s*(.+)$')
            if ($mBg.Success) {
                $bv = $mBg.Groups[1].Value.Trim()
                if ($bv -match 'url\(') { $temImg = $true } else { $fundo = $bv }
            }
            if ($d -match '^\s*background-image\s*:' ) { $temImg = $true }
        }
        if ([string]::IsNullOrEmpty($cor) -or [string]::IsNullOrEmpty($fundo)) { continue }
        if ($cor -match '^(var\(|currentColor|inherit|transparent)$') { continue }
        if ($fundo -match '^(var\(|currentColor|inherit|transparent)$') { continue }
        $rgbFg = ConvertTo-Rgb $cor; $rgbBg = ConvertTo-Rgb $fundo
        if ($null -eq $rgbFg -or $null -eq $rgbBg) { continue }
        # Fundo em gradiente ou translucido: composicao depende da superficie por baixo (nao resolvido estaticamente).
        $puls = $false
        if ($fundo -match 'gradient' -or $cor -match 'gradient') { $puls = $true }
        foreach ($vSpec in @($fundo, $cor)) {
            $mRgba = [regex]::Match($vSpec, 'rgba?\(([^)]*)\)')
            if (-not $mRgba.Success) { continue }
            $dentro = $mRgba.Groups[1].Value
            if ($dentro -match '/\s*(\d+(?:\.\d+)?)%?') {
                $opac = [double]::Parse($Matches[1], [System.Globalization.CultureInfo]::InvariantCulture)
                if ($dentro.TrimEnd().EndsWith('%')) { $opac = $opac / 100.0 }
                if ($opac -lt 1.0) { $puls = $true }
            }
            else {
                $canales = $dentro -split ','
                if ($canales.Count -eq 4 -and $canales[3].Trim() -match '^\d*\.?\d+$') {
                    if ([double]::Parse($canales[3].Trim(), [System.Globalization.CultureInfo]::InvariantCulture) -lt 1.0) { $puls = $true }
                }
                elseif ($canales.Count -eq 4) { $puls = $true }
            }
        }
        $raz = Get-Razao $rgbFg $rgbBg
        if ($puls) {
            $selC = $bl.Sel; if ($selC.Length -gt 100) { $selC = $selC.Substring(0,100) }
            $lnCss = Get-LinNum $t $bl.Pos
            $sw.WriteLine("CONTRASTE_CSS_INFO|$rel|L$lnCss|$([Math]::Round($raz,2))|$($cor) sobre $($fundo)|$($selC)")
            continue
        }
        # Icones (indicador nao-texto): piso 3:1. Estado desabilitado: isento (WCAG 1.4.3 - UI inativa).
        $limite = 4.5
        $iconico = ($bl.Sel -match 'icon') -or ($bl.Sel -match '>\s*i(:|[\s,{}]|$)')
        if ($iconico) { $limite = 3.0 }
        if ($bl.Sel -match ':disabled|\.disabled') { $limite = 0.0 }
        if ($raz -lt $limite) {
            $selC = $bl.Sel; if ($selC.Length -gt 100) { $selC = $selC.Substring(0,100) }
            $lnCss = Get-LinNum $t $bl.Pos
            $sw.WriteLine("CONTRASTE_CSS|$rel|L$lnCss|$([Math]::Round($raz,2))|$($cor) sobre $($fundo)|$($selC)")
        } elseif ($temImg) {
            $selC = $bl.Sel; if ($selC.Length -gt 100) { $selC = $selC.Substring(0,100) }
            $lnCss = Get-LinNum $t $bl.Pos
            $sw.WriteLine("CONTRASTE_CSS_IMAGEM|$rel|L$lnCss|$([Math]::Round($raz,2))|$($cor) sobre $($fundo)+imagem|$($selC)")
        }
    }
}
$sw.WriteLine('')

# --- PASS 3: estilos inline nas views com cor+fundo literais ---
$viewsDir = 'backend\PlantaoPro.Web\Views'
foreach ($f in (Get-ChildItem $viewsDir -Recurse -Filter '*.cshtml')) {
    $rel = $f.FullName.Substring((Resolve-Path $viewsDir).Path.Length + 1) -replace '\\','/'
    $t = [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
    $linhas = [IO.File]::ReadAllLines($f.FullName, [Text.Encoding]::UTF8)
    for ($i = 0; $i -lt $linhas.Count; $i++) {
        foreach ($m in [regex]::Matches($linhas[$i], 'style\s*=\s*"([^"]+)"')) {
            $st = $m.Groups[1].Value
            $mCor = [regex]::Match($st, '(?:^|;)\s*color\s*:\s*([^;]+)')
            $mBg = [regex]::Match($st, '(?:^|;)\s*background(?:-color)?\s*:\s*([^;]+)')
            if (-not ($mCor.Success -and $mBg.Success)) { continue }
            $cor = $mCor.Groups[1].Value.Trim(); $fundo = $mBg.Groups[1].Value.Trim()
            if ($cor -match '^(var\(|currentColor|inherit|transparent)$') { continue }
            if ($fundo -match '^(var\(|currentColor|inherit|transparent)$') { continue }
            $rgbFg = ConvertTo-Rgb $cor; $rgbBg = ConvertTo-Rgb $fundo
            if ($null -eq $rgbFg -or $null -eq $rgbBg) { continue }
            $raz = Get-Razao $rgbFg $rgbBg
            if ($raz -lt 4.5) {
                $snip = $st; if ($snip.Length -gt 120) { $snip = $snip.Substring(0,120) }
                $sw.WriteLine("CONTRASTE_INLINE|$rel|L$($i+1)|$([Math]::Round($raz,2))|$snip")
            }
        }
    }
}
$sw.WriteLine('')

# --- PASS 4: cor de funcao sem texto em views (classes utilitarias em elementos vazios) ---
$corUtil = '(?:^|\s)(?:bg|text|border)-(?:success|danger|warning|info)\b|text-bg-(?:success|danger|warning|info)\b'
foreach ($f in (Get-ChildItem $viewsDir -Recurse -Filter '*.cshtml')) {
    $rel = $f.FullName.Substring((Resolve-Path $viewsDir).Path.Length + 1) -replace '\\','/'
    $t = [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
    foreach ($m in [regex]::Matches($t, '<([a-z][a-z0-9]*)\b([^<>]*)>')) {
        $attrs = $m.Groups[2].Value
        if ($attrs.Contains('aria-label=') -or $attrs.Contains('title=') -or $attrs.Contains('<')) { continue }
        if ($attrs -match 'asp-validation-(for|summary)') { continue }
        $cm = [regex]::Match($attrs, 'class\s*=\s*"([^"]*)"')
        if (-not $cm.Success) { continue }
        $cl = $cm.Groups[1].Value
        if (-not [regex]::IsMatch($cl, $corUtil)) { continue }
        $start = $m.Index + $m.Length
        $nextLt = $t.IndexOf('<', $start)
        $inner = if ($nextLt -lt 0) { $t.Substring($start) } else { $t.Substring($start, $nextLt - $start) }
        if ($inner -match '\S') { continue }
        $ok = $false
        if ($nextLt -ge 0) {
            if ($nextLt + 1 -lt $t.Length -and $t[$nextLt] -eq '<' -and $t[$nextLt + 1] -eq 'i') {
                # icone como primeiro filho: aceita se ha texto apos o </i>
                $endI = $t.IndexOf('</i>', $nextLt)
                if ($endI -ge 0) {
                    $afterI = $endI + 4
                    $next2 = $t.IndexOf('<', $afterI)
                    $seg = if ($next2 -lt 0) { $t.Substring($afterI) } else { $t.Substring($afterI, [Math]::Min(300, $next2 - $afterI)) }
                    if ($seg -match '[A-Za-z0-9]') { $ok = $true }
                    # banner rotulado (role=/alert): texto dentro do primeiro container filho
                    if (-not $ok -and ($attrs.IndexOf('role=', [System.StringComparison]::Ordinal) -ge 0 -or $cl -match '\balert')) {
                        $regiao = $t.Substring($afterI, [Math]::Min(300, $t.Length - $afterI))
                        if ([regex]::Replace($regiao, '<[^>]*>', '') -match '[A-Za-z0-9]') { $ok = $true }
                    }
                }
            } else {
                $ok = $true # container: semantica resolvida mais fundo
            }
        }
        if (-not $ok -and $cl.IndexOf('bg-opacity-25', [System.StringComparison]::Ordinal) -ge 0) { $ok = $true } # chip decorativo de icone (precedente R71)
        if (-not $ok -and $cl -match 'fs-\d') { $ok = $true } # glifo decorativo dimensionado (rotulo vive fora do elemento)
        if ($ok) { continue }
        $lin = Get-LinNum $t $m.Index
        $snip = $m.Value; if ($snip.Length -gt 140) { $snip = $snip.Substring(0,140) }
        $sw.WriteLine("COR_SEM_TEXTO|$rel|L$lin|$snip")
    }
}
$sw.Dispose()
Write-Output ('SCAN_DONE pass1..4 -> ' + "$tmp\r74_gap_scan.txt")
