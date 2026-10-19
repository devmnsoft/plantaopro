# R52 - smoke e2e completa (API+Web) + performance percebida (linha de base)
# Pre-requisito (padrao de casa): API em http://localhost:51976 e Web em http://localhost:51977,
# ambos Release --no-build contra plantaopro_test (env ConnectionStrings__Default).
# Uso: powershell -ExecutionPolicy Bypass -File r52_smoke_e2e.ps1
$ErrorActionPreference='Stop'
$api='http://localhost:51976'
$web='http://localhost:51977'
$scriptDir=Split-Path -Parent $MyInvocation.MyCommand.Path
$logPath=Join-Path $scriptDir 'r52_smoke_e2e.log'

function WaitUp($url,$secs){
  for($i=0;$i -lt $secs;$i++){
    try { $r=Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 2 -MaximumRedirection 5; return $r } catch {
      if($_.Exception.Response){ return $_.Exception.Response } else { Start-Sleep -Seconds 1 }
    }
  }
  throw ('nao subiu em ' + $secs + 's: ' + $url)
}
function CountOf([string]$text,[string]$sub){ $n=0; $i=$text.IndexOf($sub); while($i -ge 0){ $n++; $i=$text.IndexOf($sub,$i+1) }; return $n }
# O Razor desta base aplica HtmlEncoder que emite acentos como entidades numericas
# (ex.: "conte&#xEF;do" - formato HEX) - comportamento conhecido e travado pelo
# R7E1I18nFundacaoTests. Decodifica entidades (hex e decimal) antes de comparar
# marcadores com acento.
function DecodeEnts([string]$t){
  return [regex]::Replace($t, '&#(x)?([0-9A-Fa-f]+);', { param($m) $cp=0; if($m.Groups[1].Success -and $m.Groups[1].Value -ne ''){ $cp=[Convert]::ToInt32($m.Groups[2].Value,16) } else { $cp=[Convert]::ToInt32($m.Groups[2].Value,10) }; [string][char]$cp })
}
function HasAcento([string]$s){ foreach($ch in $s.ToCharArray()){ if([int]$ch -gt 127){ return $true } }; return $false }

WaitUp ($api+'/health') 90 | Out-Null
for($i=0;$i -lt 120;$i++){
  try { Invoke-WebRequest -Uri ($web+'/Account/Login') -UseBasicParsing -TimeoutSec 2 | Out-Null; break } catch {
    if($_.Exception.Response){ break } else { Start-Sleep -Seconds 1 }
  }
}
$out=@()
$out+='R52 SMOKE E2E ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
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
$out+=('LOGIN_OK -> ' + $post.BaseResponse.ResponseUri.ToString())

# Warm-up do pipeline: os primeiros requests apos o startup podem responder antes de
# toda a pipeline de rotas estar pronta (observado na execucao R52: 404s transitos).
$warm=0
for($w=0;$w -lt 6;$w++){
  try { $wq=Invoke-WebRequest -Uri ($web+'/Ajuda/Index') -WebSession $s -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 10; if([int]$wq.StatusCode -eq 200){ $warm=$w+1; break } } catch { Start-Sleep -Seconds 2 }
}
if($warm -eq 0){ throw 'warm-up nao estabilizou /Ajuda/Index em 6 tentativas' }
$out+=('WARMUP_OK aps ' + $warm + ' tentativa(s)')

# Paginas da jornada (rota, marcador obrigatorio no HTML renderizado)
$paginas=@(
  @('/MinhaCentral','Minha Central'),
  @('/Administrativo360/Vendas','Vendas'),
  @('/Administrativo360/Orcamentos','Orcamento'),
  @('/Administrativo360/PedidosCompra','Pedido'),
  @('/Administrativo360/Estoque','Estoque'),
  @('/Administrativo360/TitulosReceber','Titulo'),
  @('/Administrativo360/RelatoriosFinanceiros','Relatorio'),
  @('/Notificacoes/Preferencias','Notifica'),
  @('/Ajuda/Index',('Opera' + [char]231 + [char]227 + 'o da institui' + [char]231 + [char]227 + 'o')),
  @('/Ajuda/Index','Manuais por rotina'),
  @('/Ajuda/Artigo?id=adm-orcamento',('Como criar or' + [char]231 + 'amento')),
  @('/Ajuda/Artigo?id=central-fatura','Como resolver fatura vencida'),
  @('/Ajuda/Modulo?id=instituicao','Abrir tela relacionada')
)
$sw=[System.Diagnostics.Stopwatch]::StartNew()
$falhas=@()
$idx=0
foreach($p in $paginas){
  $idx++
  $ok=$false; $ms=0
  for($t=0;$t -lt 3 -and -not $ok;$t++){
    $sw.Restart()
    try {
      $r=Invoke-WebRequest -Uri ($web+$p[0]) -WebSession $s -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 10
      $ms=[int]$sw.ElapsedMilliseconds
      if([int]$r.StatusCode -ne 200){ if($t -eq 2){ $falhas+=("$($p[0]) status=" + [int]$r.StatusCode) }; Start-Sleep -Seconds 2; continue }
      $alvo=$r.Content
      if(HasAcento $p[1]){ $alvo=DecodeEnts $r.Content }
      if((CountOf $alvo $p[1]) -lt 1){ if($t -eq 2){ $falhas+=("$($p[0]) marcador ausente: " + $p[1]); $out+=("PAGE_ERR[$idx] $($p[0]) ${ms}ms marcador ausente") } ; Start-Sleep -Milliseconds 500; continue }
      $ok=$true
      $suf=''
      if($t -gt 0){ $suf=' (retry ' + $t + ')' }
      $out+=("PAGE_OK[$idx] $p[0] ${ms}ms len=" + $r.Content.Length + $suf)
    } catch {
      $ms=[int]$sw.ElapsedMilliseconds
      if($t -eq 2){ $falhas+=("$($p[0]) excecao: " + $_.Exception.Message) }
      $out+=("PAGE_ERR[$idx] $p[0] ${ms}ms " + $_.Exception.Message)
      Start-Sleep -Seconds 2
    }
  }
}
$sw.Stop()

# Verificacoes transversais na ultima pagina (layout autenticado)
$last=Invoke-WebRequest -Uri ($web+'/Ajuda/Index') -WebSession $s -UseBasicParsing -MaximumRedirection 10
$cAria=CountOf $last.Content 'aria-label='
$cV=CountOf $last.Content 'v2151-wp-c-ajuste-canonical.css'
$cAcc=CountOf $last.Content 'accessibility.css'
$cSkip=CountOf $last.Content 'class="skip-link"'
$out+="AJUDA_ARIA=$cAria V2151=$cV ACCESSIBILITY_CSS=$cAcc SKIP_LINK=$cSkip"
if($cAria -lt 5){ $falhas+=("aria-label insuficiente na Ajuda ($cAria)") }
if($cV -lt 1){ $falhas+=("v2151 ausente no layout autenticado") }
if($cAcc -lt 1){ $falhas+=("accessibility.css ausente no layout autenticado") }
if($cSkip -lt 1){ $falhas+=("skip-link ausente no layout autenticado") }

# Performance percebida: primeiro descarta uma passagem de aquecimento por rota
# (primeira carga de view/dependencias distorce a amostra), depois mede.
# Paginas de lista densa com payload HTML gigante (R52 encontrou ~17MB em Orcamentos)
# sao reportadas como HEAVY e ficam fora da media padrao (pendencia documentada p/ R53+).
$rotas=@('/MinhaCentral','/Administrativo360/Vendas','/Administrativo360/Orcamentos','/Ajuda/Index','/Notificacoes/Preferencias')
foreach($r3 in $rotas){ try { Invoke-WebRequest -Uri ($web+$r3) -WebSession $s -UseBasicParsing -TimeoutSec 30 | Out-Null } catch { Start-Sleep -Milliseconds 500 } }
$totais=0; $n=0
$perf=@()
$heavy=@()
foreach($r2 in $rotas){
  $sw2=[System.Diagnostics.Stopwatch]::StartNew()
  $x=Invoke-WebRequest -Uri ($web+$r2) -WebSession $s -UseBasicParsing -TimeoutSec 60
  $m2=[int]$sw2.ElapsedMilliseconds
  if($x.Content.Length -gt 1048576){ $heavy+=("$r2=${m2}ms len=" + $x.Content.Length); continue }
  $totais+=$m2; $n++
  $perf+=("$r2=${m2}ms")
}
if($n -eq 0){ throw 'nenhuma rota padrao para medir performance' }
$media=[int]($totais/$n)
$out+=("PERF " + ($perf -join ' ') + " MEDIA_PADRAO=${media}ms (n=$n)")
foreach($h in $heavy){ $out+=('HEAVY ' + $h) }
if($media -ge 3000){ $falhas+=("performance acima do orcamento (media padrao ${media}ms >= 3000ms)") }

$out+=('FALHAS=' + $falhas.Count)
$out+=$falhas
Set-Content -Path $logPath -Value ($out -join "`n") -Encoding UTF8
if($falhas.Count -gt 0){ Write-Output ($out -join "`n"); throw ('R52 smoke com ' + $falhas.Count + ' falha(s)') }
Write-Output ($out -join "`n")
Write-Output 'R52_E2E_OK'
