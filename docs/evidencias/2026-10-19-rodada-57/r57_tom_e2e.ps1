# R57 - verificacao de tom declarativo via HTTP (complemento ao r52_smoke_e2e.ps1)
# D-R57-1: exclamacao fim/meio-de-frase banida em texto visivel (ponto em vez).
# Pre-requisito: Web em http://localhost:51977 (Release --no-build contra plantaopro_test).
# Uso: powershell -ExecutionPolicy Bypass -File r57_tom_e2e.ps1
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

# 1) Tooltip do lote vencido: ausencia da forma antiga em qualquer pagina renderizada.
$e1=GetPage ($web + '/Administrativo360/Estoque')
$e1d=DecodeEnts $e1.Content
$badOld=CountOf $e1d 'Lote vencido!'
if($badOld -eq 0){ $out+='ESTOQUE_TOOLDIP_ANTIGO_AUSENTE_OK (Lote vencido! nao aparece)' } else { $falhas += ('Estoque contem a forma antiga com exclamacao: ' + $badOld) }
$badgeNew=CountOf $e1d 'title="Lote vencido"'
if($badgeNew -gt 0){ $out+=('ESTOQUE_TOOLDIP_NOVO_VISIVEL_OK (ocorrencias=' + $badgeNew + ')') } else { $out+='ESTOQUE_LINHA_VENCIDA_SEM_DADOS_OK (tooltip condicional ausente; ausencia da forma antiga ja provada)' }

# 2) Regra geral de tom nas paginas renderizadas: toda exclamacao em posicao de copy
#    (antes de aspa/espacote/agrupante/virgula/parentese) deve estar coberta pelo glifo
#    decorativo de alerta >!</span> (aria-hidden=true). != , !== e !important nao entram na classe.
foreach($pg in @('/Administrativo360/Estoque','/Administrativo360/Orcamentos')){
  $r=GetPage ($web + $pg)
  $d=DecodeEnts $r.Content
  $copy=[regex]::Matches($d, '![\s"''<,)]').Count
  $glifo=[regex]::Matches($d, '>!</span>').Count
  $sobra=$copy-$glifo
  if($sobra -le 0){ $out+=('TOM_OK ' + $pg + ' (copy=' + $copy + ' glifos=' + $glifo + ')') } else { $falhas += ('exclamacoes em posicao de copy fora do glifo em ' + $pg + ': ' + $sobra) }
}

if($falhas.Count -eq 0){ $out+='R57_E2E_OK FALHAS=0' } else { foreach($f in $falhas){ $out+='FALHA: '+$f }; $out+=('R57_E2E_FAIL FALHAS=' + $falhas.Count) }
$texto=($out -join "`r`n")
Set-Content -Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'r57_tom_e2e_saida.txt') -Value $texto -Encoding UTF8
Write-Output $texto
if($falhas.Count -gt 0){ exit 1 }
