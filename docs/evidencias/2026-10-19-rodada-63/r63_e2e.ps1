# R63 - mini e2e: a tela de login renderiza o guia guiado (pt-BR padrao e fr via Accept-Language).
# Pre-requisito (padrao de casa): Web em http://localhost:51977 com API em http://localhost:51976
# contra plantaopro_test. Uso: powershell -ExecutionPolicy Bypass -File r63_e2e.ps1
$ErrorActionPreference='Stop'
$web='http://localhost:51977'
$scriptDir=Split-Path -Parent $MyInvocation.MyCommand.Path
$logPath=Join-Path $scriptDir 'r63_e2e_saida.txt'

function DecodeEnts([string]$t){
  return [regex]::Replace($t, '&#(x)?([0-9A-Fa-f]+);', { param($m) $cp=0; if($m.Groups[1].Success -and $m.Groups[1].Value -ne ''){ $cp=[Convert]::ToInt32($m.Groups[2].Value,16) } else { $cp=[Convert]::ToInt32($m.Groups[2].Value,10) }; [string][char]$cp })
}
function CountOf([string]$text,[string]$sub){ $n=0; $i=$text.IndexOf($sub); while($i -ge 0){ $n++; $i=$text.IndexOf($sub,$i+1) }; return $n }

for($w=0;$w -lt 15;$w++){
  try { Invoke-WebRequest -Uri ($web+'/Account/Login') -UseBasicParsing -TimeoutSec 5 | Out-Null; break } catch { Start-Sleep -Seconds 2 }
}
$out=@(); $falhas=@()

# Padrão (pt-BR, sem Accept-Language): o guia existe com labels localizadas.
$pt=[System.Net.WebClient]::new()
$ptHtml=$pt.DownloadString($web+'/Account/Login')
$ptDec=DecodeEnts $ptHtml
$out+=('PT_LEN=' + $ptHtml.Length)
foreach($marc in @('id="identifierGuide"', 'data-text-email=', 'data-text-invalid-length=')){
  if((CountOf $ptHtml $marc) -lt 1){ $falhas+=("marcador ausente (pt): " + $marc) }
}
$rotuloCpf='CPF ' + [char]8212 + ' login do profissional habilitado pela institui' + [char]231 + [char]227 + 'o.'
if((CountOf $ptDec $rotuloCpf) -lt 1){ $falhas+=('rotulo cpf pt-BR ausente/decodificacao divergente') }
$rotuloCnpj='CNPJ ' + [char]8212 + ' login institucional da organiza' + [char]231 + [char]227 + 'o.'
if((CountOf $ptDec $rotuloCnpj) -lt 1){ $falhas+=('rotulo cnpj pt-BR ausente/decodificacao divergente') }
$out+=('PT_GUIA_OK cpf=' + (CountOf $ptDec $rotuloCpf) + ' cnpj=' + (CountOf $ptDec $rotuloCnpj))

# Francês via Accept-Language: o mesmo guia, traduzido.
$req=[System.Net.WebRequest]::Create($web+'/Account/Login')
$req.Headers.Add('Accept-Language','fr-FR,fr;q=0.9')
try { $resp=$req.GetResponse(); $sr=New-Object System.IO.StreamReader($resp.GetResponseStream(),[System.Text.Encoding]::UTF8); $frDec=DecodeEnts $sr.ReadToEnd(); $sr.Close(); $resp.Close() } catch { throw ('falha ao obter /Account/Login em fr: ' + $_.Exception.Message) }
$frCpf='CPF ' + [char]8212 + ' connexion du professionnel habil' + [char]233 + ' par l''institution.'
$frOk=(CountOf $frDec $frCpf) -ge 1
if($frOk -eq $false){ $frOk=(CountOf $frDec 'connexion du professionnel') -ge 1 }
if(-not $frOk){ $falhas+=('guia frances ausente (Accept-Language fr)') }
$out+=('FR_GUIA_OK=' + $frOk)

$out+=('FALHAS=' + $falhas.Count)
$out+=$falhas
Set-Content -Path $logPath -Value ($out -join "`n") -Encoding UTF8
if($falhas.Count -gt 0){ Write-Output ($out -join "`n"); throw ('R63 e2e com ' + $falhas.Count + ' falha(s)') }
Write-Output ($out -join "`n")
Write-Output 'R63_E2E_OK'
