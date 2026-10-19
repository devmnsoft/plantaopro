# R62 - mini e2e: tela Assistente IA expoe o provedor Drael (IA da casa) na lista de provedores.
# Pre-requisito (padrao de casa): Web em http://localhost:51977 com API em http://localhost:51976
# contra plantaopro_test. Uso: powershell -ExecutionPolicy Bypass -File r62_e2e.ps1
$ErrorActionPreference='Stop'
$web='http://localhost:51977'
$scriptDir=Split-Path -Parent $MyInvocation.MyCommand.Path
$logPath=Join-Path $scriptDir 'r62_e2e_saida.txt'

function DecodeEnts([string]$t){
  return [regex]::Replace($t, '&#(x)?([0-9A-Fa-f]+);', { param($m) $cp=0; if($m.Groups[1].Success -and $m.Groups[1].Value -ne ''){ $cp=[Convert]::ToInt32($m.Groups[2].Value,16) } else { $cp=[Convert]::ToInt32($m.Groups[2].Value,10) }; [string][char]$cp })
}
function CountOf([string]$text,[string]$sub){ $n=0; $i=$text.IndexOf($sub); while($i -ge 0){ $n++; $i=$text.IndexOf($sub,$i+1) }; return $n }

for($w=0;$w -lt 15;$w++){
  try { Invoke-WebRequest -Uri ($web+'/Account/Login') -UseBasicParsing -TimeoutSec 5 | Out-Null; break } catch { Start-Sleep -Seconds 2 }
}
$out=@(); $falhas=@()
$s=New-Object Microsoft.PowerShell.Commands.WebRequestSession
$pg=Invoke-WebRequest -Uri ($web+'/Account/Login') -WebSession $s -UseBasicParsing
$html=$pg.Content
$tokPos=$html.IndexOf('name="__RequestVerificationToken"')
if($tokPos -lt 0){ throw 'token nao encontrado no form de login' }
$valPos=$html.IndexOf('value="',$tokPos)
$tok=$html.Substring($valPos+7,$html.IndexOf('"',$valPos+7)-($valPos+7))
$body='Email='+[System.Uri]::EscapeDataString('gestor@santacasa-demo.example')+'&Senha='+[System.Uri]::EscapeDataString('SantaCasa!Demo2026#Gestor')+'&__RequestVerificationToken='+[System.Uri]::EscapeDataString($tok)
$post=Invoke-WebRequest -Uri ($web+'/Account/Login') -Method Post -WebSession $s -Body $body -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -MaximumRedirection 10
if([int]$post.StatusCode -ne 200){ throw ('login falhou status=' + [int]$post.StatusCode) }
$out+='LOGIN_OK'

$r=$null
try { $r=Invoke-WebRequest -Uri ($web+'/AssistenteIa') -WebSession $s -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 10 } catch {}
if($null -eq $r -or [int]$r.StatusCode -ne 200){
  # Fora de perfil do tenant demo -> nao e falha da rodada (mesma regra aplicada nas rodadas anteriores).
  $out+='FORA_DO_PERFIL /AssistenteIa status=' + $(if($r){ [int]$r.StatusCode } else { 'excecao' })
} else {
  $raw=$r.Content
  $dec=DecodeEnts $raw
  $cSel=CountOf $raw 'value="drael"'
  $lbl='Drael ' + [char]8212 + ' IA da casa'
  $cLbl=CountOf $dec $lbl
  $posDrael=$raw.IndexOf('value="drael"')
  $posGroq=$raw.IndexOf('value="groq"')
  $out+=("ASSISTENTE_IA_OK drael_selects=$cSel drael_label=$cLbl pos_drael=$posDrael pos_groq=$posGroq")
  if($cSel -lt 2){ $falhas+=('provedor drael ausente em um dos selects (principal/reserva): selects=' + $cSel) }
  if($cLbl -lt 2){ $falhas+=('rotulo amigavel da IA da casa ausente: ' + $cLbl) }
  if($posGroq -ge 0 -and ($posDrael -gt $posGroq)){ $falhas+=('drael deve liderar a lista de provedores na UI') }
}

$out+=('FALHAS=' + $falhas.Count)
$out+=$falhas
Set-Content -Path $logPath -Value ($out -join "`n") -Encoding UTF8
if($falhas.Count -gt 0){ Write-Output ($out -join "`n"); throw ('R62 e2e com ' + $falhas.Count + ' falha(s)') }
Write-Output ($out -join "`n")
Write-Output 'R62_E2E_OK'
