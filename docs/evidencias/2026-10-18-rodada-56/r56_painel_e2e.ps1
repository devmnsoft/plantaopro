# R56 - prova e2e da passada de copy "Dashboard" -> "Painel": o TEXTO VISÍVEL das telas-chave
# nao pode mais conter a palavra "dashboard" (tags, scripts, estilos e atributos sao removidos;
# entidades HTML numericas/hex sao decodificadas antes do match). Complemento ao r52_smoke_e2e.ps1.
# NOTA: este arquivo deve ser mantido em UTF-8 COM BOM (PS 5.1 le .ps1 sem BOM como ANSI e corrompe
# os termos acentuados das asserts).
# Pre-requisito: Web em http://localhost:51977 (Release --no-build contra plantaopro_test).
# Uso: powershell -ExecutionPolicy Bypass -File r56_painel_e2e.ps1
$ErrorActionPreference='Stop'
$web='http://localhost:51977'
$s=New-Object Microsoft.PowerShell.Commands.WebRequestSession
$pg0=Invoke-WebRequest -Uri ($web+'/Account/Login') -WebSession $s -UseBasicParsing -MaximumRedirection 5
$html=$pg0.Content
$tokPos=$html.IndexOf('name="__RequestVerificationToken"')
if($tokPos -lt 0){ throw 'token nao encontrado no form de login' }
$valPos=$html.IndexOf('value="',$tokPos)
$tokStart=$valPos+'value="'.Length
$tokEnd=$html.IndexOf('"',$tokStart)
$tok=$html.Substring($tokStart,$tokEnd-$tokStart)
$body='Email='+[System.Uri]::EscapeDataString('gestor@santacasa-demo.example')+'&Senha='+[System.Uri]::EscapeDataString('SantaCasa!Demo2026#Gestor')+'&__RequestVerificationToken='+[System.Uri]::EscapeDataString($tok)
$post=Invoke-WebRequest -Uri ($web+'/Account/Login') -Method Post -WebSession $s -Body $body -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -MaximumRedirection 10
if([int]$post.StatusCode -ne 200){ throw ('login falhou status=' + [int]$post.StatusCode) }

# Mesma regra do smoke R52: o Razor desta base emite acentos/tracos como entidades numericas HEX.
function DecodeEnts([string]$t){
  return [regex]::Replace($t, '&#(x)?([0-9A-Fa-f]+);', { param($m) $cp=0; if($m.Groups[1].Success -and $m.Groups[1].Value -ne ''){ $cp=[Convert]::ToInt32($m.Groups[2].Value,16) } else { $cp=[Convert]::ToInt32($m.Groups[2].Value,10) }; [string][char]$cp })
}
function GetPage($url){
  for($t=0;$t -lt 3;$t++){
    try { return Invoke-WebRequest -Uri $url -WebSession $s -UseBasicParsing -TimeoutSec 60 -MaximumRedirection 10 } catch { Start-Sleep -Seconds 2 }
  }
  throw ('nao respondeu apos 3 tentativas: ' + $url)
}
# Texto visivel: remove blocos script/style (JS menciona URLs com 'dashboard'), remove tags/atributos
# (asp-action/app-icon/classes permanecem so dentro de tags) e decodifica entidades.
function VisibleText([string]$h){
  $t=[regex]::Replace($h,'<script\b.*?</script>',' ',[System.Text.RegularExpressions.RegexOptions]::Singleline)
  $t=[regex]::Replace($t,'<style\b.*?</style>',' ',[System.Text.RegularExpressions.RegexOptions]::Singleline)
  $t=[regex]::Replace($t,'<!--.*?-->',' ',[System.Text.RegularExpressions.RegexOptions]::Singleline)
  $t=[regex]::Replace($t,'<[^>]+>',' ')
  return (DecodeEnts $t)
}
# Positivos sao expressoes regulares simples (sem escapamento de metacaracteres nos termos usados).
function CheckPage([string]$name,[string]$url,[string[]]$positivos){
  $r=GetPage $url
  $txt=VisibleText $r.Content
  $out+=("$name len=" + $r.Content.Length)
  if($txt -match '(?i)\bdashboards?\b'){ $script:falhas += ("${name}: texto visivel ainda contem a palavra 'dashboard'") }
  foreach($p in $positivos){
    if($txt -notmatch $p){ $script:falhas += ("${name}: positivo ausente: '$p'") }
  }
  return $txt
}

$out=@()
$falhas=@()
$out+='LOGIN_OK -> ' + $post.BaseResponse.ResponseUri.ToString()

# Tela principal: sidebar agora exibe "Painel" (chave Nav.Dashboard, valor pt-BR renomeado).
$pgHome=CheckPage 'HOME' ($web + '/Home/Dashboard') @('Painel')

# Painel gerencial: heading virou "Painel Gerencial ..." e o subheading do resumo segue renderizado.
$gestao=CheckPage 'GESTAO_DASH' ($web + '/Administrativo360/Gestao/Dashboard') @('Painel Gerencial')
if($gestao -match [regex]::Escape('Indicadores reais e consolidados')){ $out+='GESTAO_DASH_SUBHEADING_OK' } else { $falhas += 'GESTAO_DASH: subheading do painel ausente' }

# Estoque: pagina de modulo de suprimentos + zero 'dashboard' visivel (capturar retorno p/ nao
# despejar o texto visivel na saida; mesmo padrao tolerante do r54_pager_e2e.ps1 no card de resumo).
$pgEst=CheckPage 'ESTOQUE' ($web + '/Administrativo360/Estoque') @('Saldo F.sico em Cust')

# Minha Assinatura (Módulos): os codigos de permissao MODULO.ACAO sao identificadores estaveis
# (D-R56-1), nao copy — sao removidos antes da assert. O nome de EXIBICAO do modulo DASHBOARD vem
# de modulos_sistema (v2355): se o tenant demo tem o modulo/capacidade contratado, o nome exibido
# ao lado da descricao invariante deve ser "Painel"/"Painel Clinico".
$rAs=GetPage ($web + '/MinhaAssinatura/Modulos')
$ta=VisibleText $rAs.Content
$ta2=[regex]::Replace($ta,'\b[A-Z][A-Z0-9_]*\.[A-Z][A-Z0-9_]*\b',' ')
$out+=('ASSINATURA_MODULOS len=' + $rAs.Content.Length)
if($ta2 -match '(?i)\bdashboards?\b'){ $falhas += 'ASSINATURA_MODULOS: texto visivel ainda contem "dashboard" fora de codigos MODULO.ACAO' }
if($ta2 -match [regex]::Escape('Indicadores e visão geral')){
  if($ta2 -notmatch [regex]::Escape('Painel')){ $falhas += 'ASSINATURA_MODULOS: descricao do modulo presente mas nome "Painel" ausente (v2355 nao aplicado?)' }
  else { $out+='ASSINATURA_MODULO_DASHBOARD_RENOMEADO_OK' }
} else {
  $out+='ASSINATURA_MODULO_DASHBOARD_SKIP (nao contratado no tenant demo)'
}
if($ta2 -match [regex]::Escape('Capacidade inclusa no pacote SAUDE360: visao operacional')){
  if($ta2 -notmatch [regex]::Escape('Painel Clinico')){ $falhas += 'ASSINATURA_MODULOS: capacidade CLINICA_DASHBOARD sem o nome "Painel Clinico" (v2355 nao aplicado?)' }
  else { $out+='ASSINATURA_CAPACIDADE_CLINICA_DASHBOARD_RENOMEADA_OK' }
}

$out+=('FALHAS=' + $falhas.Count)
$out+=$falhas
$out | ForEach-Object { Write-Output $_ }
if($falhas.Count -gt 0){ throw ('R56 painel e2e com ' + $falhas.Count + ' falha(s)') }
Write-Output 'R56_E2E_OK'
