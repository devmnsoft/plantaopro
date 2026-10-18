# R54 - verificacao da paginacao via HTTP (complemento ao r52_smoke_e2e.ps1)
# Pre-requisito: Web em http://localhost:51977 (Release --no-build contra plantaopro_test).
# Uso: powershell -ExecutionPolicy Bypass -File r54_pager_e2e.ps1
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

$out=@()
$falhas=@()
$out+='LOGIN_OK -> ' + $post.BaseResponse.ResponseUri.ToString()

# Orçamentos: pagina 1 (contagem) + pagina 2 com porPagina=50 (faixa deslocada + link de retorno)
$r1=GetPage ($web + '/Administrativo360/Orcamentos')
$d1=DecodeEnts $r1.Content
$out+=('ORC_P1 len=' + $r1.Content.Length)
if($d1 -notmatch 'Mostrando 1.{0,16}?100 de'){ $falhas += 'Orcamentos p1: contagem "Mostrando 1..100" ausente' }
if($r1.Content -notmatch 'aria-label="Navega'){ $falhas += 'Orcamentos p1: grupo de paginação ausente' }
$r2=GetPage ($web + '/Administrativo360/Orcamentos?pagina=2&porPagina=50')
$d2=DecodeEnts $r2.Content
$out+=('ORC_P2 len=' + $r2.Content.Length)
if($d2 -notmatch 'Mostrando 51.{0,16}?100 de'){ $falhas += 'Orcamentos p2(porPagina=50): faixa "Mostrando 51..100" ausente' }
if($r2.Content -notmatch 'pagina=1'){ $falhas += 'Orcamentos p2: link de retorno para pagina=1 ausente' }

# Estoque: pagina 1 (contagem + cards com somas) + pagina 2 (faixa deslocada)
$e1=GetPage ($web + '/Administrativo360/Estoque')
$de1=DecodeEnts $e1.Content
$out+=('EST_P1 len=' + $e1.Content.Length)
if($de1 -notmatch 'Mostrando 1.{0,16}?100 de'){ $falhas += 'Estoque p1: contagem "Mostrando 1..100" ausente' }
if($de1 -notmatch 'Saldo F.sico em Cust'){ $falhas += 'Estoque p1: card de resumo ausente' }
$e2=GetPage ($web + '/Administrativo360/Estoque?pagina=2&porPagina=50')
$de2=DecodeEnts $e2.Content
$out+=('EST_P2 len=' + $e2.Content.Length)
if($de2 -notmatch 'Mostrando 51.{0,16}?100 de'){ $falhas += 'Estoque p2(porPagina=50): faixa "Mostrando 51..100" ausente' }

# Filtro preservado na navegação: pagina 2 de uma busca ativa mantém a busca no link
$b1=GetPage ($web + '/Administrativo360/Orcamentos?busca=ORC&pagina=2&porPagina=50')
$db1=DecodeEnts $b1.Content
$out+=('ORC_BUSCA_P2 len=' + $b1.Content.Length)
if($b1.Content -notmatch 'busca=ORC'){ $falhas += 'Orcamentos p2 com busca: filtro nao preservado nos links' }

$out+=('FALHAS=' + $falhas.Count)
$out+=$falhas
$out | ForEach-Object { Write-Output $_ }
if($falhas.Count -gt 0){ throw ('R54 pager e2e com ' + $falhas.Count + ' falha(s)') }
Write-Output 'R54_PAGER_E2E_OK'
