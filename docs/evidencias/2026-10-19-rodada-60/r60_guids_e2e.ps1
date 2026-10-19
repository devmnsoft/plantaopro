# R60 - verificacao de GUIDs/fragmentos via HTTP (complemento ao r52_smoke_e2e.ps1)
# D-R60-1/D-R60-1b: nas paginas renderizadas nao pode haver o padrao de fragmento antigo
# (8 hex seguidos de elipse U+2026) nem artifact 'Substring(' do source vazando.
# Paginas: AssistenteIa/Index (custos IA por cliente), Administrativo360/Fiscal/Notas (origem),
# MinhaAgenda/MeusPagamentos (fechamento - se o perfil demo a alcancar).
# Pre-requisito: Web em http://localhost:51977 (Release --no-incremental contra plantaopro_test).
# Uso: powershell -ExecutionPolicy Bypass -File r60_guids_e2e.ps1
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
# Padrao do fragmento antigo: exatamente 8 hex seguidos de elipse U+2026.
$fragPattern='\b[0-9A-Fa-f]{8}\u2026'

$out=@()
$falhas=@()
$out+='LOGIN_OK -> ' + $post.BaseResponse.ResponseUri.ToString()

$r=GetPage ($web + '/AssistenteIa')
$d=DecodeEnts $r.Content
if([regex]::IsMatch($d,$fragPattern)){ $falhas += 'AssistenteIa: fragmento 8-hex+elipse ainda visível' } else { $out+='IA_SEM_FRAGMENTO_OK' }
if(CountOf $d 'Substring(' -gt 0){ $falhas += 'AssistenteIa: artifact Substring( no HTML' } else { $out+='IA_SEM_ARTIFACT_OK' }
$nGui=([regex]::Matches($d,'\b[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\b')).Count
if($nGui -ge 1){ $out+=('IA_GUI_IDS_OK (ids inteiros renderizados n=' + $nGui + ')') } else { $out+='IA_SEM_USOS_REGISTRADO (nenhum id na pagina)' }

$r=GetPage ($web + '/Administrativo360/Fiscal/Notas')
$d=DecodeEnts $r.Content
if([regex]::IsMatch($d,$fragPattern)){ $falhas += 'Fiscal/Notas: fragmento 8-hex+elipse ainda visível' } else { $out+='NOTAS_SEM_FRAGMENTO_OK' }
if(CountOf $d 'Substring(' -gt 0){ $falhas += 'Fiscal/Notas: artifact Substring( no HTML' } else { $out+='NOTAS_SEM_ARTIFACT_OK' }

$mp=$null
try { $mp=GetPage ($web + '/MinhaAgenda/MeusPagamentos') } catch {}
$dm= if($mp){ DecodeEnts $mp.Content } else { '' }
if(CountOf $dm 'Plantão considerado no fechamento' -ge 1){
  if([regex]::IsMatch($dm,'no fechamento <strong>[0-9A-Fa-f]{8}</strong>')){ $falhas += 'MeusPagamentos: FechamentoId truncado a 8 hex' } else { $out+='MEUSPAGAMENTOS_FECHAMENTO_OK' }
} else { $out+='MEUSPAGAMENTOS_FORA_DO_PERFIL (pagina diferente ou vazia para o demo)' }

if($falhas.Count -eq 0){ $out+='R60_E2E_OK FALHAS=0' } else { foreach($f in $falhas){ $out+='FALHA: '+$f }; $out+=('R60_E2E_FAIL FALHAS=' + $falhas.Count) }
$texto=($out -join "`r`n")
Set-Content -Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'r60_guids_e2e_saida.txt') -Value $texto -Encoding UTF8
Write-Output $texto
if($falhas.Count -gt 0){ exit 1 }
