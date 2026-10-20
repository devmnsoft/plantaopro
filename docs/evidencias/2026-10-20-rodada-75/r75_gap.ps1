$ErrorActionPreference = 'Stop'
# raiz do repo = tres niveis acima de docs/evidencias/<data>-rodada-75/
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
Set-Location $root
$viewsRoot = Join-Path $root 'backend\PlantaoPro.Web\Views'
$ctrlRoot = Join-Path $root 'backend\PlantaoPro.Web\Controllers'
$ajuda = Join-Path $ctrlRoot 'AjudaController.cs'
$outArq = if ($args.Count -gt 0) { $args[0] } else { Join-Path $PSScriptRoot 'r75_inventario.txt' }
$out = New-Object System.IO.StreamWriter($outArq, $false, (New-Object System.Text.UTF8Encoding($false)))

function Get-CorpoMetodo([string]$txt, [int]$posAbre) {
    $prof = 0
    for ($i = $posAbre; $i -lt $txt.Length; $i++) {
        if ($txt[$i] -eq '{') { $prof++ } elseif ($txt[$i] -eq '}') { $prof--; if ($prof -eq 0) { return $txt.Substring($posAbre, $i - $posAbre + 1) } }
    }
    return ''
}
$classRe = [regex]'((?:[a-z]+\s+)*)class\s+(\w+)(\s*:\s*[^{]*)?\s*\{'
$assinRe = [regex]'(?:public|protected|private|internal)\s+(?:static\s+)?(?:(?:Value)?Task<\s*)?[A-Za-z_][\w?<>,\s]*?\s+(\w+)\s*(?:<[^>]*>)?\s*\([^;{)]*\)\s*(?:\{|=>)'
$qual = '(?:[A-Za-z_]\w*\s*\.\s*)*'
$viewRetRe = 'return\s+' + $qual + 'View\s*\('
$exprViewRe = '^\s*' + $qual + 'View\s*\('
$litRe = [regex]'View\s*\(\s*"([^"]+)"'

$retViewTotal = 0
$classes = @()
$metodos = @()
foreach ($f in (Get-ChildItem $ctrlRoot -Filter '*.cs' -Recurse | Sort-Object FullName)) {
    $txt = [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
    $retViewTotal += ([regex]::Matches($txt, $viewRetRe)).Count
    foreach ($cm in $classRe.Matches($txt)) {
        $nomeC = $cm.Groups[2].Value
        $mods = $cm.Groups[1].Value
        $isAbstract = $mods -match 'abstract'
        $baseTipo = ($cm.Groups[3].Value -replace '^\s*:','').Trim().Split(',')[0].Trim()
        $baseTipo = ([regex]::Match($baseTipo, '^\w+')).Value
        $dir = $nomeC -replace 'Controller$', ''
        $corpoS = Get-CorpoMetodo $txt ($cm.Index + $cm.Length - 1)
        $classes += [pscustomobject]@{ Nome = $nomeC; Abstract = $isAbstract; Base = $baseTipo; Dir = $dir; IsController = ($nomeC -like '*Controller') }
        foreach ($m in $assinRe.Matches($corpoS)) {
            $nomeM = $m.Groups[1].Value
            $isExpr = $corpoS[$m.Index + $m.Length - 1] -ne '{'
            if ($isExpr) {
                $resto = $corpoS.Substring($m.Index + $m.Length)
                $mm = [regex]::Match($resto, '^\s*([\s\S]*?);')
                $corpoM = if ($mm.Success) { $mm.Groups[1].Value } else { '' }
            } else {
                $corpoM = Get-CorpoMetodo $corpoS ($m.Index + $m.Length - 1)
            }
            $prevClose = $corpoS.LastIndexOf('}', $m.Index)
            $win = if ($prevClose -ge 0) { $corpoS.Substring($prevClose + 1, $m.Index - $prevClose - 1) } else { $corpoS.Substring(0, $m.Index) }
            $vis = $m.Value.TrimStart()
            $publico = $vis -match '^public'
            $retOk = $vis -match '(?:(?:Value)?Task<\s*)?(?:IActionResult|ActionResult)\s*>?\s*'
            $metodos += [pscustomobject]@{
                Classe = $nomeC; Dir = $dir; Nome = $nomeM; Corpo = $corpoM; Expr = $isExpr
                NonAction = ($win -match '\[NonAction'); HttpPost = ($win -match '\[HttpPost'); HttpGet = ($win -match '\[HttpGet')
                AcaoLike = ($publico -and $retOk); Publico = $publico
            }
        }
    }
}

function Testa-Render([string]$corpo, [bool]$expr) {
    if ($corpo -match $viewRetRe) { return $true }
    if ($expr -and $corpo -match $exprViewRe) { return $true }
    return $false
}
$render = @{}
foreach ($mtd in $metodos) { if (Testa-Render $mtd.Corpo $mtd.Expr) { $render[$mtd.Nome] = $true } }
$alterou = $true
while ($alterou) {
    $alterou = $false
    foreach ($mtd in $metodos) {
        if ($render.ContainsKey($mtd.Nome)) { continue }
        foreach ($h in $render.Keys) {
            if ([regex]::IsMatch($mtd.Corpo, '\b' + [regex]::Escape($h) + '\s*(?:<[^>()]*>)?\s*\(')) { $render[$mtd.Nome] = $true; $alterou = $true; break }
        }
    }
}
# mapa por classe (para delegacao intra-classe) e global de fallback
$metodoPorClasse = @{}
$corpoPorNome = @{}
foreach ($mtd in $metodos) {
    $k = $mtd.Classe + '|' + $mtd.Nome
    if (-not $metodoPorClasse.ContainsKey($k)) { $metodoPorClasse[$k] = $mtd }
    if (-not $corpoPorNome.ContainsKey($mtd.Nome)) { $corpoPorNome[$mtd.Nome] = $mtd }
}
$porNome = @{}
foreach ($c in $classes) { if (-not $porNome.ContainsKey($c.Nome)) { $porNome[$c.Nome] = $c } }

# dado o contexto (classe concreta) e um alvo, sobe a hierarquia buscando o metodo
function Resolve-Metodo([string]$ctx, [string]$alvo) {
    $c = $ctx; $prof = 0
    while ($c -and $prof -lt 8) {
        if ($metodoPorClasse.ContainsKey($c + '|' + $alvo)) { return $metodoPorClasse[$c + '|' + $alvo] }
        if ($porNome.ContainsKey($c)) { $c = $porNome[$c].Base } else { $c = $null }
        $prof++
    }
    if ($corpoPorNome.ContainsKey($alvo)) { return $corpoPorNome[$alvo] }
    return $null
}

# acoes efetivas: declaradas na classe concreta OU herdadas publicas de base abstrata
$acoesEfetivas = @()
$classesVistas = @{}
foreach ($c in $classes) {
    if ($c.Abstract) { continue }
    if (-not $c.IsController) { continue }
    if ($classesVistas.ContainsKey($c.Nome)) { continue }   # evita parciais duplicados
    $classesVistas[$c.Nome] = $true
    $propios = @($metodos | Where-Object { $_.Classe -eq $c.Nome -and $_.AcaoLike -and -not $_.NonAction })
    $nomesProprios = $propios | ForEach-Object { $_.Nome.ToLowerInvariant() }
    foreach ($pp in $propios) {
        $acoesEfetivas += [pscustomobject]@{ Ctx = $c.Nome; Dir = $c.Dir; Nome = $pp.Nome; Corpo = $pp.Corpo; Expr = $pp.Expr; HttpPost = $pp.HttpPost; HttpGet = $pp.HttpGet; HerdadoDe = '' }
    }
    $pai = $c.Base; $prof = 0
    while ($pai -and $porNome.ContainsKey($pai) -and $porNome[$pai].Abstract -and $prof -lt 8) {
        $b = $porNome[$pai]
        foreach ($bm in ($metodos | Where-Object { $_.Classe -eq $b.Nome -and $_.AcaoLike -and -not $_.NonAction })) {
            if ($nomesProprios -contains $bm.Nome.ToLowerInvariant()) { continue }
            $acoesEfetivas += [pscustomobject]@{ Ctx = $c.Nome; Dir = $c.Dir; Nome = $bm.Nome; Corpo = $bm.Corpo; Expr = $bm.Expr; HttpPost = $bm.HttpPost; HttpGet = $bm.HttpGet; HerdadoDe = $b.Nome }
        }
        $pai = $b.Base; $prof++
    }
}

function Normaliza-ViewKey([string]$v) {
    if ($v -match '^~/Views/(.*)\.cshtml$') { return @{ Key = $Matches[1]; Dinamico = ($Matches[1] -match '\{') } }
    if ($v -match '^~/Views/(.*)$') { return @{ Key = $Matches[1]; Dinamico = ($Matches[1] -match '\{') } }
    if ($v -match '/') { return @{ Key = $v; Dinamico = ($v -match '\{') } }
    return @{ Key = $v; SemBarra = $true }
}
function Testa-ArquivoExiste([string]$key) {
    $k = $key.Replace('/', [IO.Path]::DirectorySeparatorChar)
    if (Test-Path (Join-Path $viewsRoot ($k + '.cshtml'))) { return $true }
    $partes = $key.Split('/')
    if ($partes.Count -eq 2 -and (Test-Path (Join-Path (Join-Path $viewsRoot 'Shared') ($partes[1] + '.cshtml')))) { return $true }
    return $false
}

$linhas = @()
foreach ($a in $acoesEfetivas) {
    $corpo = $a.Corpo; $expr = $a.Expr; $chamada = $null; $modo = $null; $chave = $null
    for ($i = 0; $i -lt 6 -and -not $modo; $i++) {
        $mv = $litRe.Match($corpo)
        if (($corpo -match $viewRetRe) -or ($expr -and $corpo -match $exprViewRe)) {
            if ($mv.Success) {
                $n = Normaliza-ViewKey $mv.Groups[1].Value
                if ($n.SemBarra) { $chave = $a.Dir + '/' + $n.Key } else { $chave = $n.Key }
                $modo = $(if ($n.Dinamico) { 'DINA' } elseif ($chamada) { 'LIT via ' + $chamada } else { 'LIT' })
            } else {
                $chave = $a.Dir + '/' + $a.Nome
                $modo = if ($chamada) { 'NOME via ' + $chamada } else { 'NOME' }
            }
        } else {
            $hEncontrado = $null
            foreach ($h in ($render.Keys | Sort-Object)) {
                if ([regex]::IsMatch($corpo, '\b' + [regex]::Escape($h) + '\s*(?:<[^>()]*>)?\s*\(')) { $hEncontrado = $h; break }
            }
            if (-not $hEncontrado) { break }
            $chamada = if ($chamada) { $chamada + '>' + $hEncontrado } else { $hEncontrado }
            $dono = Resolve-Metodo $a.Ctx $hEncontrado
            if (-not $dono) { break }
            $corpo = $dono.Corpo; $expr = $dono.Expr
        }
    }
    if (-not $modo) { continue }
    $linhas += [pscustomobject]@{
        K = ($a.Dir.ToLowerInvariant() + '|' + $a.Nome.ToLowerInvariant())
        Controller = $a.Dir; Acao = $a.Nome; View = $chave; Modo = $modo
        HttpPost = [bool]$a.HttpPost; HerdadoDe = [string]$a.HerdadoDe
    }
}
$linhas = @($linhas | Group-Object K | ForEach-Object { $_.Group | Select-Object -First 1 })
$paginas = $linhas | Sort-Object Controller, Acao

# ---------- B) artigos declarados em AjudaController ----------
$atxt = [IO.File]::ReadAllText($ajuda, [Text.Encoding]::UTF8)
$ponteiroRe = [regex]'Artigo\("[^"]+",\s*"[^"]+",\s*"[^"]+",\s*"[^"]+",\s*"[^"]*",\s*"([^"]+)",\s*"([^"]+)"\s*,\s*\d+\s*,'
$artigos = @()
foreach ($m in $ponteiroRe.Matches($atxt)) {
    $artigos += [pscustomobject]@{ Controller = $m.Groups[1].Value; Action = $m.Groups[2].Value }
}
$idsArtigo = @([regex]::Matches($atxt, 'Artigo\("([^"]+)"') | ForEach-Object { $_.Groups[1].Value })
$titulosArtigo = @([regex]::Matches($atxt, 'Artigo\("[^"]+",\s*"[^"]+",\s*"[^"]+",\s*"([^"]*)"') | ForEach-Object { $_.Groups[1].Value })

# ---------- C) classificacao ----------
$manuais = @('Ajuda', 'Manual')
$cobertoPor = @{}
for ($i = 0; $i -lt $artigos.Count; $i++) {
    $cobertoPor[$artigos[$i].Controller.ToLowerInvariant() + '/' + $artigos[$i].Action.ToLowerInvariant()] = $idsArtigo[$i]
}
$cobertas = 0; $semArtigo = 0; $foraEscopo = 0; $aux = 0; $poteQuebra = 0; $fileOk = 0
foreach ($p in $paginas) {
    $existe = Testa-ArquivoExiste $p.View
    if (-not $existe) { $estado = 'ARQ_FALTA' } else { $estado = 'ARQ_OK'; $fileOk++ }
    $herd = if ($p.HerdadoDe) { 'herdado-de-' + $p.HerdadoDe } else { '' }
    $out.WriteLine("PAGINA|$($p.Controller)|$($p.Acao)|$($p.View)|$estado|$($p.Modo)|$herd")
}
$out.WriteLine('')
for ($i = 0; $i -lt $artigos.Count; $i++) {
    $a = $artigos[$i]
    $out.WriteLine("ARTIGO|id=$($idsArtigo[$i])|$($a.Controller)/$($a.Action)|titulo=$($titulosArtigo[$i])")
}
$out.WriteLine('')
$paresPaginas = @{}
foreach ($p in $paginas) { $paresPaginas[$p.Controller.ToLowerInvariant() + '/' + $p.Acao.ToLowerInvariant()] = $true }
foreach ($p in $paginas) {
    $chave = $p.Controller.ToLowerInvariant() + '/' + $p.Acao.ToLowerInvariant()
    $existe = Testa-ArquivoExiste $p.View
    if (-not $existe) {
        if ($p.HttpPost -or $p.Modo -eq 'DINA' -or ($p.HerdadoDe)) { $aux++; $out.WriteLine("ACO_AUX|$($p.Controller)|$($p.Acao)|$($p.View)|submissao dinamica ou helper herdado") }
        else { $poteQuebra++; $out.WriteLine("POTE_QUEBRA|$($p.Controller)|$($p.Acao)|$($p.View)|pagina GET sem arquivo resolvido") }
        continue
    }
    if ($manuais -contains $p.Controller) { $foraEscopo++; $out.WriteLine("FORA_ESCOPO|$($p.Controller)|$($p.Acao)|superficie do proprio manual"); continue }
    if ($cobertoPor.ContainsKey($chave)) { $cobertas++; $out.WriteLine("COBERTA|$($p.Controller)|$($p.Acao)|artigo $($cobertoPor[$chave])") }
    else { $semArtigo++; $out.WriteLine("SEM_ARTIGO|$($p.Controller)|$($p.Acao)") }
}
$out.WriteLine('')
$out.WriteLine("RESUMO|RETURNS_VIEW_BRUTOS=$retViewTotal|ACOES_EFETIVAS_RESOLVIDAS=$($paginas.Count)|FILE_OK=$fileOk|ARTIGOS=$($artigos.Count)|COBERTAS=$cobertas|FORA_ESCOPO=$foraEscopo|GAP_SEM_ARTIGO=$semArtigo|ACO_AUX=$aux|POTE_QUEBRA=$poteQuebra")
$orfaos = @($artigos | Where-Object { -not ($paresPaginas.ContainsKey($_.Controller.ToLowerInvariant() + '/' + $_.Action.ToLowerInvariant())) })
for ($i = 0; $i -lt $orfaos.Count; $i++) {
    $o = $orfaos[$i]
    $idx = [array]::IndexOf($artigos, $o)
    $out.WriteLine("ARTIGO_ORFAO|id=$($idsArtigo[$idx])|aponta para $($o.Controller)/$($o.Action) fora do inventario")
}
if (-not $orfaos.Count) { $out.WriteLine('ARTIGOS_ORFAOS=NENHUM') }
$out.Dispose()
Write-Output ('INVENTARIO_OK returns_brutos=' + $retViewTotal + ' paginas=' + $paginas.Count + ' cobertas=' + $cobertas + ' gap=' + $semArtigo + ' aux=' + $aux + ' pote_quebra=' + $poteQuebra)
