# ============================================================
# R71 - Semantica de cores: auditoria deterministica do mapa de estados
# ------------------------------------------------------------
# PASS A (mapa): percorre wwwroot/css (exceto vendor) bloco a bloco.
#   Marca qualquer bloco cujo seletor mencione classe de estado
#   (token "status-" apos ponto ou tracerto, ex.: .status-pendente,
#   .pp-status-badge.is-success) e cujas declaracoes color/background*
#   usem valor BRUTO fora dos tons canonicos var(--pp-tone-<tom>-ink|soft),
#   currentColor, inherit ou transparent. Apos R71 a cor de estado vem
#   unicamente do mapa canônico (v2151, secao 9): qualquer outra tinta
#   em seletor de estado e FALHA (mapa duplicado/divergente).
# PASS B (cor-sola): percorre Views/**/*.cshtml tag a tag. Marca
#   elementos cuja classe tenha token de familia (badge, chip, pill,
#   dot, indicator, text-bg-*) e cujo conteudo inline nao contenha
#   texto legivel nem icone <i>, sem aria-label nem title. Status
#   comunicado so por cor = FALHA. Dots decorativos que fazem parte
#   de um container rotulado (role="status" com texto vizinho) entram
#   na whitelist por classe exata; fundamentacao em r71_relatorio.md.
# Uso: powershell -File r71_audit_mapa.ps1 -Out <arquivo de saida>
# Saidas: linhas A|<css>|L<n>|<seletor> e B|<view>|L<n>|<tag>; marcador
#   R71_MAP_AUDIT FALHAS=n FALHAS_MAPA=a FALHAS_COR_SOLA=b CSS=m VIEWS=k
# Exit code = FALHAS (teto 99). Sem dependencias fora do PowerShell 5.1.
param([string]$Out = '')
$ErrorActionPreference = 'Stop'
$repo    = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$cssDir  = Join-Path $repo 'backend\PlantaoPro.Web\wwwroot\css'
$viewDir = Join-Path $repo 'backend\PlantaoPro.Web\Views'

$linhas = New-Object System.Collections.Generic.List[string]
$falhasA = 0
$falhasB = 0

# ---- PASS A: cor de estado fora dos tons canonicos ---------------------
$selStatusRe = '[.-]status-[a-z][a-z-]*'
$propRe = [regex]'^\s*(color|background[a-z-]*)\s*:\s*(.+)$'
$cssFiles = Get-ChildItem $cssDir -Recurse -Filter *.css | Where-Object { $_.FullName -notlike '*\vendor\*' } | Sort-Object FullName
foreach ($f in $cssFiles) {
    $txt = [IO.File]::ReadAllText($f.FullName)
    $i = 0; $len = $txt.Length
    while ($i -lt $len) {
        $ob = $txt.IndexOf('{', $i)
        if ($ob -lt 0) { break }
        $fe = $txt.IndexOf('}', $ob)
        if ($fe -lt 0) { break }
        $sel = $txt.Substring($i, $ob - $i).Trim()
        $dec = $txt.Substring($ob + 1, $fe - $ob - 1)
        $i = $fe + 1
        if ($sel -notmatch $selStatusRe) { continue }
        foreach ($p in ($dec -split ';')) {
            $m = $propRe.Match($p)
            if (-not $m.Success) { continue }
            $val = $m.Groups[2].Value.Trim()
            if ($val -eq 'currentColor' -or $val -eq 'inherit' -or $val -eq 'transparent') { continue }
            if ($val -match '^var\(\s*--pp-tone-[a-z]+-(?:ink|soft)') { continue }
            if ($val -match '^var\(\s*--pp-text') { continue }   # tipografia neutra (rotulos/muted), nao cor de estado
            $lin = ([regex]::Matches($txt.Substring(0, $ob), '\n')).Count + 1
            $selCorta = $sel
            if ($selCorta.Length -gt 80) { $selCorta = $selCorta.Substring(0, 80) }
            $linhas.Add(('A|{0}|L{1}|{2}' -f $f.Name, $lin, $selCorta))
            $falhasA++
            break
        }
    }
}

# ---- PASS B: status so por cor (sem texto nem icone) -------------------
# Whitelist: marcadores decorativos dentro de containers rotulados com
# role="status" e texto no proprio container (Login conexao, topbar
# sessao, timeline da assinatura). O texto vizinho comunica o estado.
$whitelist = @('pp-timeline-dot', 'pp-login-connection__dot', 'pp-session-health__dot', 'bg-opacity-25', 'save-indicator')
$candTokRe = '(?:status-badge|status-[a-z][a-z-]*|pp-status(?:-badge|-with-icon|--[a-z]+)?|text-bg-(?:success|danger|warning|info|secondary)|(?<![a-z-])(?:badge|chip|chips|pill|pills|dot|dots|indicator|pulsar)\b)'
$tagRe     = [regex]'<([a-z][a-z0-9]*)\b((?:"[^"]*"|''[^'']*''|[^>""''])*)>'
$classRe   = [regex]'class\s*=\s*"([^"]*)"'
$viewFiles = Get-ChildItem $viewDir -Recurse -Filter *.cshtml | Sort-Object FullName
foreach ($v in $viewFiles) {
    $txt = [IO.File]::ReadAllText($v.FullName)
    foreach ($m in $tagRe.Matches($txt)) {
        $attrs = $m.Groups[2].Value
        if ($attrs -match 'aria-label=' -or $attrs -match 'title=' -or $attrs -match 'data-notif') { continue }
        $cm = $classRe.Match($attrs)
        if (-not $cm.Success) { continue }
        $classVal = $cm.Groups[1].Value
        if ($classVal -notmatch $candTokRe) { continue }
        $tokens = $classVal -split '\s+'
        $isWhitelisted = $false
        foreach ($w in $whitelist) { if ($tokens -contains $w) { $isWhitelisted = $true; break } }
        if ($isWhitelisted) { continue }
        $start = $m.Index + $m.Length
        $nextLt = $txt.IndexOf('<', $start)
        $inner = ''
        if ($nextLt -lt 0) { $inner = $txt.Substring($start) }
        else { $inner = $txt.Substring($start, $nextLt - $start) }
        if ($inner -match '[A-Za-z0-9]') { continue }   # ha texto inline: ok
        if ($nextLt -lt 0) { }                           # vazio ate o fim do arquivo: segue para flag
        elseif ($txt.Substring($nextLt).StartsWith('<i')) {
            # icone como primeiro filho: procura texto apos o </i> (padrao <i></i>rotulo)
            $endI = $txt.IndexOf('</i>', $nextLt)
            $seg = ''
            if ($endI -ge 0) {
                $afterI = $endI + 4
                $next2 = $txt.IndexOf('<', $afterI)
                if ($next2 -lt 0) { $seg = $txt.Substring($afterI) } else { $seg = $txt.Substring($afterI, $next2 - $afterI) }
            }
            if ($seg -match '[A-Za-z0-9]') { continue }  # tem texto apos o icone: ok
        }
        elseif ($txt[$nextLt] -eq '<') { continue }      # container: semantica resolvida mais fundo
        $lin = ([regex]::Matches($txt.Substring(0, $m.Index), '\n')).Count + 1
        $rel = $v.FullName.Substring($viewDir.Length + 1).Replace('\', '/')
        $snip = $m.Value
        if ($snip.Length -gt 120) { $snip = $snip.Substring(0, 120) }
        $linhas.Add(('B|{0}|L{1}|{2}' -f $rel, $lin, $snip))
        $falhasB++
    }
}

# ---- saida --------------------------------------------------------------
$total = $falhasA + $falhasB
$marker = ('R71_MAP_AUDIT FALHAS={0} FALHAS_MAPA={1} FALHAS_COR_SOLA={2} CSS={3} VIEWS={4}' -f $total, $falhasA, $falhasB, $cssFiles.Count, $viewFiles.Count)
$linhas.Add($marker)
$body = ($linhas -join "`r`n") + "`r`n"
if ($Out) { [IO.File]::WriteAllText([IO.Path]::GetFullPath($Out), $body, (New-Object System.Text.UTF8Encoding($false))) }
Write-Host $body -NoNewline
exit ([Math]::Min(99, $total))
