# R59 - verificacao de acessibilidade avancada via HTTP (complemento ao r52_smoke_e2e.ps1)
# D-R59-1: tabela densa renderizada contem scope="col" em todos os <th e zero <th sem scope;
# o <thead nao recebeu atributo estranho. D-R59-2/3/4: bundle bootstrap v5.3.x no layout,
# live region role=status presente, landmarks (aside/main/header/footer) renderizados.
# Pagina de login (layout auth): skip-link + main + live region.
# Pre-requisito: Web em http://localhost:51977 (Release --no-incremental contra plantaopro_test).
# Uso: powershell -ExecutionPolicy Bypass -File r59_a11y_e2e.ps1
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

# --- Pagina densa: Estoque (Administrativo360) ---
$r=GetPage ($web + '/Administrativo360/Estoque')
$d=DecodeEnts $r.Content

$thTags=[regex]::Matches($d,'<th(?=[\s>])[^>]*>')
$semScope=@()
foreach($m in $thTags){ if($m.Value -notmatch 'scope='){ $semScope += $m.Value.Substring(0,[Math]::Min(60,$m.Value.Length)) } }
$nCol=CountOf $d 'scope="col"'
if($thTags.Count -ge 5){ $out+=('TH_RENDERIZADOS_OK (n=' + $thTags.Count + ')') } else { $falhas += ('cabeçalhos esperados >= 5 na pagina densa, encontrados ' + $thTags.Count) }
if($nCol -ge 5){ $out+=('SCOPE_COL_OK (n=' + $nCol + ')') } else { $falhas += ('scope="col" esperados >= 5, encontrados ' + $nCol) }
if($semScope.Count -eq 0){ $out+='TH_SEM_SCOPE_ZERO_OK' } else { foreach($x in $semScope){ $falhas += ('<th sem scope renderizado: ' + $x) } }
$theadEstranho=([regex]::Matches($d,'<t(head|foot)[^>]*scope=')).Count
if($theadEstranho -eq 0){ $out+='THEAD_TFOOT_LIMPOS_OK' } else { $falhas += ('thead/tfoot com scope estranho: ' + $theadEstranho) }

if(CountOf $d 'bootstrap.bundle.min.js' -ge 1){ $out+='BUNDLE_BS_OK' } else { $falhas += 'bootstrap.bundle.min.js ausente no layout renderizado' }
if(CountOf $d 'id="appLiveRegion" role="status" aria-live="polite"' -ge 1){ $out+='LIVE_REGION_OK' } else { $falhas += 'live region appLiveRegion ausente' }
if(CountOf $d '<aside ' -ge 1 -and CountOf $d 'id="appSidebar" aria-label=' -ge 1){ $out+='LANDMARK_ASIDE_OK' } else { $falhas += 'landmark aside/appSidebar ausente' }
if(CountOf $d '<header class="pp-topbar app-topbar">' -ge 1){ $out+='LANDMARK_HEADER_OK' } else { $falhas += 'landmark header/topbar ausente' }
if(CountOf $d '<footer class="pp-footer app-footer">' -ge 1){ $out+='LANDMARK_FOOTER_OK' } else { $falhas += 'landmark footer ausente' }
if(CountOf $d '<main class="pp-content app-content" id="mainContent" tabindex="-1">' -ge 1){ $out+='LANDMARK_MAIN_OK' } else { $falhas += 'landmark main/mainContent ausente' }

# --- Pagina de login (layout auth, sessao nova) ---
$s2=New-Object Microsoft.PowerShell.Commands.WebRequestSession
$l=Invoke-WebRequest -Uri ($web + '/Account/Login') -WebSession $s2 -UseBasicParsing -MaximumRedirection 5 -TimeoutSec 60
$dl=DecodeEnts $l.Content
if(CountOf $dl '<a class="skip-link" href="#mainContent">' -ge 1){ $out+='AUTH_SKIP_LINK_OK' } else { $falhas += 'skip-link ausente no layout auth' }
if(CountOf $dl '<main id="mainContent"' -ge 1){ $out+='AUTH_MAIN_OK' } else { $falhas += 'main ausente no layout auth' }
if(CountOf $dl 'id="appLiveRegion" role="status" aria-live="polite"' -ge 1){ $out+='AUTH_LIVE_REGION_OK' } else { $falhas += 'live region ausente no layout auth' }

if($falhas.Count -eq 0){ $out+='R59_E2E_OK FALHAS=0' } else { foreach($f in $falhas){ $out+='FALHA: '+$f }; $out+=('R59_E2E_FAIL FALHAS=' + $falhas.Count) }
$texto=($out -join "`r`n")
Set-Content -Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'r59_a11y_e2e_saida.txt') -Value $texto -Encoding UTF8
Write-Output $texto
if($falhas.Count -gt 0){ exit 1 }
