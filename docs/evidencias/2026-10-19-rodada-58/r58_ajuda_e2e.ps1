# R58 - verificacao do tour guiado consumindo artigos via HTTP (complemento ao r52_smoke_e2e.ps1)
# D-R58-4: o roteiro do tour v2145 em /Ajuda/Index = 3 passos fixos de UI + 1 passo por artigo
# filtrado pelo perfil (data-tour-step nas ancocas de artigo). Sem mudanca de JS.
# Pre-requisito: Web em http://localhost:51977 (Release --no-build contra plantaopro_test).
# Uso: powershell -ExecutionPolicy Bypass -File r58_ajuda_e2e.ps1
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
function CountOf([string]$text,[string]$sub){ $n=0; $i=$text.IndexOf($sub); while($i -ge 0){ $n++; $i=$text.IndexOf($sub,$i+1) }; return $n }

$out=@()
$falhas=@()
$out+='LOGIN_OK -> ' + $post.BaseResponse.ResponseUri.ToString()

$r=GetPage ($web + '/Ajuda/Index')
$d=DecodeEnts $r.Content

# Botao do tour intacto (infra v2145).
if(CountOf $d 'data-tour-start="help-center"' -ge 1){ $out+='TOUR_BTN_OK' } else { $falhas += 'botao Fazer tour (data-tour-start) ausente' }

# Passos fixos de orientacao permanecem.
foreach($fixo in @('data-tour-step="Busca rápida|','data-tour-step="Atalhos principais|','data-tour-step="Manuais curtos|')){
  if(CountOf $d $fixo -ge 1){ $out+=('PASSO_FIXO_OK ' + $fixo.Substring(15,12) + '...') } else { $falhas += ('passo fixo ausente: ' + $fixo) }
}

# D-R58-4: todo artigo renderizado vira passo do tour (rotulo = Titulo do artigo).
$nArtigos=CountOf $d 'class="pp-help-articles__number"'
$nPassosArtigos=CountOf $d 'data-tour-step="Como '
$totalPassos=CountOf $d 'data-tour-step='
if($nArtigos -ge 3){ $out+=('ARTIGOS_RENDERIZADOS_OK (n=' + $nArtigos + ')') } else { $falhas += ('artigos do perfil esperados >= 3, encontrados ' + $nArtigos) }
if($nPassosArtigos -eq $nArtigos){ $out+=('ROTEIRO_CONSOME_ARTIGOS_OK (passos de artigo = ' + $nPassosArtigos + ')') } else { $falhas += ('passos de artigo != artigos renderizados: passos=' + $nPassosArtigos + ' artigos=' + $nArtigos) }
if($totalPassos -eq ($nArtigos + 3)){ $out+=('TOTAL_PASSOS_TOUR_OK (total=' + $totalPassos + ' = 3 fixos + ' + $nArtigos + ' artigos)') } else { $falhas += ('total de passos inesperado: total=' + $totalPassos + ' esperado=' + ($nArtigos + 3)) }

# Conteudo novo da rodada visivel quando o perfil o permite (demo = ADMINISTRADOR_CLIENTE:
# ve o manual da instituicao; recepcao/parceiro sao travados pelo contrato R58 na fonte).
if(CountOf $d 'data-tour-step="Como criar orçamento|' -ge 1){ $out+='PASSO_EXEMPLO_OK (Como criar orçamento)' } else { $falhas += 'passo de exemplo (Como criar orçamento) ausente para o perfil demo' }

if($falhas.Count -eq 0){ $out+='R58_E2E_OK FALHAS=0' } else { foreach($f in $falhas){ $out+='FALHA: '+$f }; $out+=('R58_E2E_FAIL FALHAS=' + $falhas.Count) }
$texto=($out -join "`r`n")
Set-Content -Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'r58_ajuda_e2e_saida.txt') -Value $texto -Encoding UTF8
Write-Output $texto
if($falhas.Count -gt 0){ exit 1 }
