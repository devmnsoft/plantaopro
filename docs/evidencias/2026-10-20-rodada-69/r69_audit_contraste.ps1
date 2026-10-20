# R69 - Auditoria WCAG AA de contraste (texto/fundo) nas camadas CSS efetivas.
# Deterministico: parses as folhas em ordem de cascade, resolve var() no mapa :root,
# calcula a razao WCAG 2.1 e reporta pares abaixo do piso (4,5:1 texto normal; 3:1 nao-texto).
# Saida: tabela + resumo com marcadores R69_CONTRAST_OK / R69_CONTRAST_FAILHA e FALHAS=N.
param(
    [string]$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
)

$ErrorActionPreference = 'Stop'
$cssDir = Join-Path $Repo 'backend\PlantaoPro.Web\wwwroot\css'
$viewDir = Join-Path $Repo 'backend\PlantaoPro.Web\Views'

# Ordem de carga real dos layouts (_Layout.cshtml / _AuthLayout.cshtml), incluindo a
# expansao dos @import de plantaopro.css e os anexos por view. Dedupe preservando ordem.
$ordered = @(
    'vendor\bootstrap.min.css',
    'vendor\bootstrap-compat.css',
    'plantaopro.css',
    'design-system\tokens.css',
    'design-system\foundation.css',
    'design-system\typography.css',
    'design-system\layout.css',
    'design-system\navigation.css',
    'design-system\components.css',
    'design-system\buttons.css',
    'design-system\cards.css',
    'design-system\badges.css',
    'design-system\forms.css',
    'design-system\tables.css',
    'design-system\feedback.css',
    'design-system\states.css',
    'design-system\charts.css',
    'design-system\overlays.css',
    'design-system\workspaces.css',
    'design-system\clinical.css',
    'design-system\financial.css',
    'design-system\operations.css',
    'design-system\saas.css',
    'design-system\accessibility.css',
    'design-system\responsive.css',
    'design-system\white-label.css',
    'design-system\utilities.css',
    'design-system\premium-operations.css',
    'design-system\v2000-premium.css',
    'design-system\v2010-screen-polish.css',
    'design-system\v2116-premium-review.css',
    'design-system\v2117-design-stabilization.css',
    'design-system\v2118-functional-polish.css',
    'design-system\v2120-premium-final-polish.css',
    'design-system\v2130-forms.css',
    'design-system\v2121-saas-guided.css',
    'design-system\v2122-saas-commerce.css',
    'design-system\v2123-saas-operations.css',
    'design-system\v2124-operational-intelligence.css',
    'design-system\drawers.css',
    'design-system\v2126-form-feedback.css',
    'design-system\v2132-premium-global.css',
    'design-system\v2133-real-screens.css',
    'design-system\v2134-premium-qa.css',
    'design-system\v2137-global-ux.css',
    'design-system\v2138-session-internal.css',
    'design-system\v2139-premium-library.css',
    'design-system\v2140-guided-journeys.css',
    'design-system\v2141-premium-cockpit.css',
    'design-system\v2142-guided-premium.css',
    'design-system\v2143-qa-visual.css',
    'design-system\v2144-responsive-safe-actions.css',
    'design-system\v2145-onboarding-help.css',
    'design-system\v2146-pending-notifications.css',
    'design-system\kanban.css',
    'design-system\v2148-operational-premium.css',
    'pages\administrativo360-tema-claro.css',
    'design-system\v2150-bloco-c-canonical.css',
    'design-system\v2151-wp-c-ajuste-canonical.css',
    # Anexos por view (estao ativos nas telas densas correspondentes).
    'design-system\assistente-ia.css',
    'design-system\clinical-workspace.css',
    'design-system\v2070-notifications.css',
    'pages\command-center.css',
    'pages\administrativo360-organizacao.css',
    'pages\professional-portal.css',
    'pages\report-center.css',
    'pages\operational-reports.css',
    'pages\minha-disponibilidade.css',
    'pages\minha-central.css',
    'pages\meu-dia.css',
    'pages\v2119-shift-operations.css',
    'auth-login-v2127.css',
    'demo-access.css'
)
$files = @()
foreach ($rel in $ordered) { if ($rel -notin $files) { $files += $rel } }

# ---------- utilitarios de cor ----------
function Get-ChannelLin([double]$v) {
    $s = $v / 255.0
    if ($s -le 0.03928) { return ($s / 12.92) }
    return [Math]::Pow(($s + 0.055) / 1.055, 2.4)
}
function Get-WcagLum($r, $g, $b) {
    return (0.2126 * (Get-ChannelLin $r)) + (0.7152 * (Get-ChannelLin $g)) + (0.0722 * (Get-ChannelLin $b))
}
function Get-ContrastRatio($c1, $c2) {
    $l1 = Get-WcagLum $c1.R $c1.G $c1.B
    $l2 = Get-WcagLum $c2.R $c2.G $c2.B
    $hi = [Math]::Max($l1, $l2); $lo = [Math]::Min($l1, $l2)
    return ($hi + 0.05) / ($lo + 0.05)
}
function Resolve-CssValue([string]$val, $map, [int]$depth) {
    if ($null -eq $val) { return $null }
    if ($depth -gt 6) { return $null }
    $v = $val.Trim()
    if ($v -match '!important$') { $v = ($v -replace '!important\s*$','').Trim() }
    if ($v -eq '') { return $null }
    if ($v -eq 'currentColor' -or $v -eq 'transparent') { return $null }
    if ($v -eq 'white') { $v = '#ffffff' } elseif ($v -eq 'black') { $v = '#000000' }
    # referencia direta a um custom property (uso interno da auditoria)
    if ($v.StartsWith('--')) {
        if ($map.ContainsKey($v)) { return (Resolve-CssValue $map[$v] $map ($depth + 1)) }
        return $null
    }
    # var(--name) ou var(--name, fallback)
    if ($v.StartsWith('var(') -and $v.EndsWith(')')) {
        $inner = $v.Substring(4, $v.Length - 5)
        $d = 0; $ci = -1
        for ($i = 0; $i -lt $inner.Length; $i++) {
            if ($inner[$i] -eq '(') { $d++ } elseif ($inner[$i] -eq ')') { $d-- }
            elseif ($inner[$i] -eq ',' -and $d -eq 0) { $ci = $i; break }
        }
        $name = $inner; $fallback = ''
        if ($ci -ge 0) { $name = $inner.Substring(0, $ci); $fallback = $inner.Substring($ci + 1) }
        $name = $name.Trim()
        if ($map.ContainsKey($name)) {
            return (Resolve-CssValue $map[$name] $map ($depth + 1))
        }
        if ($fallback.Trim() -ne '') { return (Resolve-CssValue $fallback $map ($depth + 1)) }
        return $null
    }
    # rgb()/rgba()/color() com canais possivelmente var()
    if ($v -match '^(rgba?|color)\(') {
        $inner = $v.Substring($v.IndexOf('(') + 1)
        if ($inner.EndsWith(')')) { $inner = $inner.Substring(0, $inner.Length - 1) }
        # substitui var(--x) por valor do mapa (suporta tripletes r,g,b de bootstrap)
        $inner = [regex]::Replace($inner, 'var\(\s*(--[a-z0-9-]+)\s*\)', {
            param($m)
            $nm = $m.Groups[1].Value
            if ($script:tokenMap.ContainsKey($nm)) { return $script:tokenMap[$nm] }
            return '0'
        })
        $alpha = 1.0
        $channels = @()
        if ($inner.Contains(',')) {
            $parts = $inner.Split(',') | ForEach-Object { $_.Trim() }
            foreach ($p in $parts[0..2]) { $channels += $p }
            if ($parts.Count -gt 3) {
                try { $alpha = [double]$parts[3] } catch { $alpha = 1.0 }
            }
        } else {
            # espacado: "r g b / a"
            $slashed = $inner.Split('/')
            $spaceParts = @($slashed[0].Trim() -split '\s+')
            foreach ($p in $spaceParts[0..2]) { $channels += $p }
            if ($slashed.Count -gt 1) {
                $a = $slashed[1].Trim()
                if ($a.EndsWith('%')) { $alpha = ([double]($a -replace '%$','')) / 100.0 } else { try { $alpha = [double]$a } catch { $alpha = 1.0 } }
            }
        }
        if ($channels.Count -lt 3) { return $null }
        $rgbs = @()
        foreach ($p in $channels) {
            $t = $p.Trim()
            if ($t.EndsWith('%')) { $rgbs += [Math]::Round(([double]($t -replace '%$','')) * 2.55) }
            else { try { $rgbs += [int][double]$t } catch { return $null } }
        }
        $col = @{ R = [int]$rgbs[0]; G = [int]$rgbs[1]; B = [int]$rgbs[2]; A = $alpha; AOrig = $alpha; Src = $v }
        # funde com branco se translucido
        if ($col.A -lt 1.0) {
            $col.R = [int][Math]::Round($col.R * $col.A + 255 * (1.0 - $col.A))
            $col.G = [int][Math]::Round($col.G * $col.A + 255 * (1.0 - $col.A))
            $col.B = [int][Math]::Round($col.B * $col.A + 255 * (1.0 - $col.A))
            $col.A = 1.0
        }
        return $col
    }
    if ($v -match '^(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})$') {
        return @{ R = [int]$Matches[1]; G = [int]$Matches[2]; B = [int]$Matches[3]; A = 1.0; AOrig = 1.0; Src = $v }
    }
    if ($v -match '^#[0-9a-fA-F]{3}$') {
        $h = $v.Substring(1)
        $r = [Convert]::ToInt32($h[0].ToString() + $h[0], 16)
        $g = [Convert]::ToInt32($h[1].ToString() + $h[1], 16)
        $b = [Convert]::ToInt32($h[2].ToString() + $h[2], 16)
        return @{ R = $r; G = $g; B = $b; A = 1.0; AOrig = 1.0; Src = $v }
    }
    if ($v -match '^#[0-9a-fA-F]{6,8}$') {
        $r = [Convert]::ToInt32($v.Substring(1, 2), 16)
        $g = [Convert]::ToInt32($v.Substring(3, 2), 16)
        $b = [Convert]::ToInt32($v.Substring(5, 2), 16)
        return @{ R = $r; G = $g; B = $b; A = 1.0; AOrig = 1.0; Src = $v }
    }
    return $null
}

# ---------- parser css (walker de chaves, achata @media/@supports) ----------
function Get-CssRules([string]$text) {
    $out = New-Object System.Collections.Generic.List[object]
    $text = [regex]::Replace($text, '/\*.*?\*/', ' ', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $n = $text.Length
    $i = 0
    while ($i -lt $n) {
        $ob = $text.IndexOf('{', $i)
        if ($ob -lt 0) { break }
        $head = $text.Substring($i, $ob - $i).Trim()
        $depth = 1; $j = $ob + 1
        while ($j -lt $n -and $depth -gt 0) {
            $ch = $text[$j]
            if ($ch -eq '{') { $depth++ } elseif ($ch -eq '}') { $depth-- }
            $j++
        }
        $body = $text.Substring($ob + 1, $j - $ob - 2)
        if ($head -match '^@keyframes' -or $head -match '^@font-face' -or $head -match '^@page') {
            $i = $j; continue
        }
        if ($head -match '^@(media|supports|container|layer|document)(.*)$') {
            foreach ($r in (Get-CssRules $body)) { $out.Add($r) }
            $i = $j; continue
        }
        if ($head -ne '') {
            $out.Add([pscustomobject]@{ Selector = $head; Body = $body })
        }
        $i = $j
    }
    return $out
}

# ---------- passo 1: mapa de custom properties :root (ultima definicao vence) ----------
$global:tokenMap = @{}
$readFiles = @()
foreach ($rel in $files) {
    $full = Join-Path $cssDir $rel
    if (-not (Test-Path $full)) { Write-Output ("AVISO arquivo ausente: {0}" -f $rel); continue }
    $txt = [IO.File]::ReadAllText($full)
    $readFiles += $full
    foreach ($r in (Get-CssRules $txt)) {
        if ($r.Selector -like ':root*' -or $r.Selector -like '*:root*') {
            foreach ($decl in ($r.Body.Split(';'))) {
                $pos = $decl.IndexOf(':')
                if ($pos -le 0) { continue }
                $nm = $decl.Substring(0, $pos).Trim()
                if ($nm.StartsWith('--')) { $tokenMap[$nm] = $decl.Substring($pos + 1).Trim() }
            }
        }
    }
}

# ---------- passo 2: pares self-contained (color + background na mesma regra) ----------
$white = @{ R = 255; G = 255; B = 255; A = 1.0; Src = '#ffffff' }
$funcRe = '(badge|pill|status|state|alert|tag|chip|kanban|toast|notif|table|row|cell|btn|button|label|amount|total|metric|kpi|stat|score|risk|triage|pending|priority|severity|indicator|placa|resumo|value)'
$seen = @{}
$rows = New-Object System.Collections.Generic.List[object]

$allPairs = New-Object System.Collections.Generic.List[object]
for ($fi = 0; $fi -lt $readFiles.Count; $fi++) {
    $rel = $files[$fi]
    $txt = [IO.File]::ReadAllText($readFiles[$fi])
    foreach ($r in (Get-CssRules $txt)) {
        $fgVal = $null; $bgVal = $null
        foreach ($decl in ($r.Body.Split(';'))) {
            $pos = $decl.IndexOf(':')
            if ($pos -le 0) { continue }
            $nm = $decl.Substring(0, $pos).Trim().ToLower()
            $vv = $decl.Substring($pos + 1).Trim()
            if ($nm -eq 'color' -or $nm -eq '-webkit-text-fill-color') { if ($fgVal -eq $null) { $fgVal = $vv } }
            elseif (($nm -eq 'background-color' -or $nm -eq 'background') -and ($bgVal -eq $null) -and ($vv -match '^#|^rgb|^var\(|^color\(|^white|^black')) { $bgVal = $vv }
        }
        if (($fgVal -ne $null) -and ($bgVal -ne $null)) {
            $allPairs.Add([pscustomobject]@{ File = $rel; Selector = $r.Selector; Fg = $fgVal; Bg = $bgVal })
        }
    }
}

# agrega: vencedor por (selector|cor fg|cor bg) mantendo o arquivo mais tardio
$agg = @{}
foreach ($p in $allPairs) {
    $fg = Resolve-CssValue $p.Fg $tokenMap 0
    $bg = Resolve-CssValue $p.Bg $tokenMap 0
    if ($null -eq $fg -or $null -eq $bg) { continue }
    $key = "$($p.Selector)|$($fg.Src)|$($bg.Src)"
    $agg[$key] = [pscustomobject]@{ File = $p.File; Selector = $p.Selector; Fg = $fg; Bg = $bg }
}

$falhas = 0; $dur = 0; $info = 0; $okCount = 0
$outLines = New-Object System.Collections.Generic.List[string]
$outLines.Add('== R69 auditoria WCAG AA de contraste (texto/fundo) ==')
$outLines.Add(('Total arquivos parseados: {0}' -f $readFiles.Count))
$outLines.Add(('Total regras com par color+fundo resolvido: {0}' -f $agg.Count))
$outLines.Add('')
$outLines.Add('--- A) Tokens canonicos x superficies (tokens.css) ---')

$curated = @(
    @('texto primario sobre surface', '--pp-text-primary', '#ffffff', 4.5),
    @('texto primario sobre pagina', '--pp-text-primary', '--pp-page-background', 4.5),
    @('texto primario sobre subtle', '--pp-text-primary', '--pp-surface-subtle', 4.5),
    @('texto primario sobre hover-bg', '--pp-text-primary', '--pp-hover-bg', 4.5),
    @('texto secundario sobre surface', '--pp-text-secondary', '#ffffff', 4.5),
    @('texto secundario sobre pagina', '--pp-text-secondary', '--pp-page-background', 4.5),
    @('texto secundario sobre hover-bg', '--pp-text-secondary', '--pp-hover-bg', 4.5),
    @('texto muted sobre surface', '--pp-text-muted', '#ffffff', 4.5),
    @('texto muted sobre pagina', '--pp-text-muted', '--pp-page-background', 4.5),
    @('texto soft sobre surface', '--pp-text-soft', '#ffffff', 4.5),
    @('texto soft sobre pagina', '--pp-text-soft', '--pp-page-background', 4.5),
    @('texto inverso sobre superficie inversa', '--pp-text-inverse', '--pp-surface-inverse', 4.5),
    @('sidebar texto sobre navy', '--pp-sidebar-text', '--pp-brand-dark', 4.5),
    @('sidebar muted sobre navy', '--pp-sidebar-muted', '--pp-brand-dark', 4.5),
    @('clinico texto sobre clinico bg', '--pp-clinical-text', '--pp-clinical-bg', 4.5),
    @('clinico muted sobre clinico bg', '--pp-clinical-muted', '--pp-clinical-bg', 4.5),
    @('sucesso sobre soft', '--pp-success', '--pp-success-soft', 4.5),
    @('sucesso sobre branco', '--pp-success', '#ffffff', 4.5),
    @('alerta sobre soft', '--pp-warning', '--pp-warning-soft', 4.5),
    @('alerta sobre branco', '--pp-warning', '#ffffff', 4.5),
    @('perigo sobre soft', '--pp-danger', '--pp-danger-soft', 4.5),
    @('perigo sobre branco', '--pp-danger', '#ffffff', 4.5),
    @('info sobre soft', '--pp-info', '--pp-info-soft', 4.5),
    @('info sobre branco', '--pp-info', '#ffffff', 4.5),
    @('branco sobre sucesso', '#ffffff', '--pp-success', 4.5),
    @('branco sobre alerta', '#ffffff', '--pp-warning', 4.5),
    @('branco sobre perigo', '#ffffff', '--pp-danger', 4.5),
    @('branco sobre info', '#ffffff', '--pp-info', 4.5),
    @('branco sobre brand primary', '#ffffff', '--pp-brand-primary', 4.5),
    @('branco sobre brand primary hover', '#ffffff', '--pp-brand-primary-hover', 4.5),
    @('branco sobre brand dark', '#ffffff', '--pp-brand-dark', 4.5),
    @('branco sobre brand ink', '#ffffff', '--pp-brand-ink', 4.5),
    @('branco sobre navy-900', '#ffffff', '--pp-navy-900', 4.5),
    @('branco sobre blue-600', '#ffffff', '--pp-blue-600', 4.5),
    @('acento teal como indicador (nao-texto)', '#ffffff', '--pp-clinical-teal', 3.0),
    @('acento cyan como indicador (nao-texto)', '#ffffff', '--pp-clinical-cyan', 3.0),
    @('acento green como indicador (nao-texto)', '#ffffff', '--pp-clinical-green', 3.0),
    @('acento amber como indicador (nao-texto)', '#ffffff', '--pp-clinical-amber', 3.0),
    @('acento accent como indicador (nao-texto)', '#ffffff', '--pp-color-accent', 3.0),
    @('branco sobre color success', '#ffffff', '--pp-color-success', 4.5),
    @('branco sobre color warning', '#ffffff', '--pp-color-warning', 4.5),
    @('branco sobre color danger', '#ffffff', '--pp-color-danger', 4.5),
    @('focus ring sobre pagina (nao-texto)', '--pp-focus-ring', '--pp-page-background', 3.0),
    @('brand primary sobre pagina (nao-texto)', '--pp-brand-primary', '--pp-page-background', 3.0)
)
foreach ($c in $curated) {
    $label = $c[0]; $fg = Resolve-CssValue $c[1] $tokenMap 0; $bg = Resolve-CssValue $c[2] $tokenMap 0; $req = [double]$c[3]
    if ($null -eq $fg -or $null -eq $bg) { $outLines.Add(('NRESOLVIDO  {0} ({1} x {2})' -f $label, $c[1], $c[2])); continue }
    $ratio = [Math]::Round((Get-ContrastRatio $fg $bg), 2)
    if ($ratio -ge $req) { $status = 'OK'; $okCount++ } else { $status = 'FALHA'; $falhas++ }
    $outLines.Add(('{0,-9}  {1,-42} fg={2} bg={3} ratio={4} req={5}' -f $status, $label, $fg.Src, $bg.Src, $ratio, $req))
}

$outLines.Add('')
$outLines.Add('--- B) Pares self-contained por regra (cascade efetivo) ---')
$falhas = 0; $ctxCount = 0
$sortedKeys = $agg.Keys | Sort-Object
foreach ($key in $sortedKeys) {
    $p = $agg[$key]
    # WCAG 1.4.3: componentes inativos (desabilitados) ficam fora do piso de contraste.
    if ($p.Selector -match ':disabled|\.disabled\b') { continue }
    $ratio = [Math]::Round((Get-ContrastRatio $p.Fg $p.Bg), 2)
    $func = [bool]($p.Selector -match $funcRe)
    # Icones/superficies de icone sao objeto grafico nao-texto: piso 3:1 (WCAG 1.4.11).
    $isIcon = [bool]($p.Selector -match 'icon')
    $req = 4.5
    if ($isIcon) { $req = 3.0 }
    # Fundo translucido fino: a cor efetiva depende da superficie por baixo (ex.: navy).
    # Relatado como contextual; so vira falha se ja falha duramente contra o pior caso.
    $translucentBg = ($null -ne $p.Bg.AOrig) -and ($p.Bg.AOrig -le 0.30)
    if ($ratio -ge $req) { $status = 'OK'; $okCount++ }
    elseif ($translucentBg) { $status = 'CTX'; $ctxCount++ }
    elseif ($func) { $status = 'FALHA'; $falhas++ }
    elseif ($ratio -lt 3.0) { $status = 'DURA'; $dur++; $falhas++ }
    else { $status = 'INFO'; $info++ }
    $sel = $p.Selector; if ($sel.Length -gt 90) { $sel = $sel.Substring(0, 87) + '...' }
    $outLines.Add(('{0,-7}  ratio={1,-6} req={2,-4} sel={3} fg={4} bg={5} file={6}' -f $status, $ratio, $req, $sel, $p.Fg.Src, $p.Bg.Src, $p.File))
}

# ---------- D) paleta funcional efetiva do Bootstrap (badge/text/bg/outline/link) ----------
$bsNames = @('primary', 'success', 'warning', 'info', 'danger', 'secondary')
$outLines.Add('')
$outLines.Add('--- D) Bootstrap: pares funcionais efetivos (vendor + compat, cascade real) ---')

function Get-BsOutlineColor([string]$name) {
    $val = $null
    foreach ($f in $script:readFiles) {
        $t = [IO.File]::ReadAllText($f)
        $m = [regex]::Match($t, ('\{?\.btn-outline-' + $name + '\s*\{[^{}]*?--bs-btn-color:\s*(#[0-9a-fA-F]{3,8})'))
        if ($m.Success) { $val = $m.Groups[1].Value }
    }
    return $val
}

foreach ($bn in $bsNames) {
    $triple = $null
    if ($tokenMap.ContainsKey('--bs-' + $bn + '-rgb')) { $triple = $tokenMap['--bs-' + $bn + '-rgb'] }
    $bg = Resolve-CssValue ('--bs-' + $bn + '-rgb') $tokenMap 0
    if ($null -ne $bg) {
        $r1 = [Math]::Round((Get-ContrastRatio $white $bg), 2)
        if ($r1 -ge 4.5) { $st = 'OK'; $okCount++ } else { $st = 'FALHA'; $falhas++ }
        $outLines.Add(('{0,-7}  ratio={1,-6} badge brancosobre bg {2} bg={3} src={4}' -f $st, $r1, $bn, $bg.Src, ('--bs-' + $bn + '-rgb')))
        $r2 = [Math]::Round((Get-ContrastRatio $bg $white), 2)
        if ($r2 -ge 4.5) { $st = 'OK'; $okCount++ } else { $st = 'FALHA'; $falhas++ }
        $outLines.Add(('{0,-7}  ratio={1,-6} utilitario de texto {2} sobre branco src={3}' -f $st, $r2, $bn, ('--bs-' + $bn + '-rgb')))
        $pageBg = Resolve-CssValue '--pp-page-background' $tokenMap 0
        if ($null -ne $pageBg) {
            $r3 = [Math]::Round((Get-ContrastRatio $bg $pageBg), 2)
            if ($r3 -ge 4.5) { $st = 'OK'; $okCount++ } else { $st = 'FALHA'; $falhas++ }
            $outLines.Add(('{0,-7}  ratio={1,-6} utilitario de texto {2} sobre pagina bg={3}' -f $st, $r3, $bn, $pageBg.Src))
        }
    } else {
        $outLines.Add(('NRESOLVIDO  --bs-{0}-rgb = {1}' -f $bn, $triple))
    }
    $oc = Get-BsOutlineColor $bn
    if ($null -ne $oc) {
        $ocCol = Resolve-CssValue $oc $tokenMap 0
        $r4 = [Math]::Round((Get-ContrastRatio $ocCol $white), 2)
        if ($r4 -ge 4.5) { $st = 'OK'; $okCount++ } else { $st = 'FALHA'; $falhas++ }
        $outLines.Add(('{0,-7}  ratio={1,-6} botao contorno {2} fg={3} sobre branco' -f $st, $r4, $bn, $oc))
    }
}
if ($tokenMap.ContainsKey('--bs-link-color-rgb')) {
    $lc = Resolve-CssValue '--bs-link-color-rgb' $tokenMap 0
    if ($null -ne $lc) {
        $r5 = [Math]::Round((Get-ContrastRatio $lc $white), 2)
        if ($r5 -ge 4.5) { $st = 'OK'; $okCount++ } else { $st = 'FALHA'; $falhas++ }
        $outLines.Add(('{0,-7}  ratio={1,-6} link primario fg={2} sobre branco' -f $st, $r5, $lc.Src))
    }
}

# ---------- C) blocos <style> e style="..." nos views (telas densas inline) ----------
$outLines.Add('')
$outLines.Add('--- C) Estilos inline em views (<style> e style=...) ---')
$inlineFiles = Get-ChildItem $viewDir -Recurse -File *.cshtml
foreach ($vf in $inlineFiles) {
    $vtxt = [IO.File]::ReadAllText($vf.FullName)
    $blocks = [regex]::Matches($vtxt, '<style[^>]*>(.*?)</style>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    foreach ($b in $blocks) {
        foreach ($r in (Get-CssRules $b.Groups[1].Value)) {
            $fgVal = $null; $bgVal = $null
            foreach ($decl in ($r.Body.Split(';'))) {
                $pos = $decl.IndexOf(':')
                if ($pos -le 0) { continue }
                $nm = $decl.Substring(0, $pos).Trim().ToLower()
                $vv = $decl.Substring($pos + 1).Trim()
                if ($nm -eq 'color') { if ($fgVal -eq $null) { $fgVal = $vv } }
                elseif (($nm -eq 'background-color' -or $nm -eq 'background') -and ($bgVal -eq $null) -and ($vv -match '^#|^rgb|^var\(')) { $bgVal = $vv }
            }
            if (($fgVal -ne $null) -and ($bgVal -ne $null)) {
                $fg = Resolve-CssValue $fgVal $tokenMap 0; $bg = Resolve-CssValue $bgVal $tokenMap 0
                if ($null -eq $fg -or $null -eq $bg) { continue }
                $ratio = [Math]::Round((Get-ContrastRatio $fg $bg), 2)
                $st = 'OK'
                if ($ratio -lt 4.5 -and $r.Selector -match $funcRe) { $st = 'FALHA'; $falhas++ }
                elseif ($ratio -lt 3.0) { $st = 'DURA'; $falhas++ }
                elseif ($ratio -lt 4.5) { $st = 'INFO' }
                $sel = $r.Selector; if ($sel.Length -gt 80) { $sel = $sel.Substring(0, 77) + '...' }
                $outLines.Add(('{0,-7}  ratio={1,-6} sel={2} fg={3} bg={4} file={5}' -f $st, $ratio, $sel, $fg.Src, $bg.Src, $vf.Name))
            }
        }
    }
}

$outLines.Add('')
$outLines.Add(('RESUMO: OK={0} INFO={1} CTX={2} FALHAS={3}' -f $okCount, $info, $ctxCount, $falhas))
if ($falhas -eq 0) { $outLines.Add('R69_CONTRAST_OK') } else { $outLines.Add('R69_CONTRAST_FAILHA') }
$outLines.Add(('FALHAS={0}' -f $falhas))

$report = ($outLines -join "`r`n")
[Console]::Out.WriteLine($report)
$code = 0
if ($falhas -gt 0) { $code = 1 }
exit $code
