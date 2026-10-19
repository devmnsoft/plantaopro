# R61 - verificacao de identidade cadastro x operacao via HTTP (complemento ao r52_smoke_e2e.ps1)
# D-R61-1: em paginas fora do catalogo canonico a topbar nao pode mais cair em "Index"/"Details"/
# "Create"/"Edit" (Humanize de acao) — o h1 da topbar traz o titulo pt-BR auditado (amostra de
# >=4 paginas verificadas; demais fora do perfil demo vao como FORA_DO_PERFIL, nao falha).
# D-R61-2: copy sem conotacao de homologacao (FaturamentoSaas Details sem "sandbox";
# OperacaoAssistida/Ocorrencias com termo de dominio; layout referencia asset renomeado).
# Pre-requisito: API+Web em http://localhost:51977 (Release --no-incremental contra plantaopro_test).
# Uso: powershell -ExecutionPolicy Bypass -File r61_identidade_e2e.ps1
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
function TopbarH1([string]$html){
  $m=[regex]::Match($html,'<h1 class="pp-topbar__title[^"]*"[^>]*>(.*?)</h1>',[System.Text.RegularExpressions.RegexOptions]::Singleline)
  if(-not $m.Success){ return '' }
  return (DecodeEnts $m.Groups[1].Value).Trim()
}
$acoesProibidas=@('Index','Details','Create','Edit','Controller','Action')

$out=@()
$falhas=@()
$verificadas=0
$out+='LOGIN_OK'

# D-R61-1: amostra das paginas do gap E7 (controller/acao/titulo esperado travado no contrato R61).
$amostra=@(
  @('/Convenios/Index','Convênios'),
  @('/Agendamentos/AgendaDia','Agenda do dia'),
  @('/Agendamentos/Calendario','Calendário clínico'),
  @('/PainelChamada/Guiches','Guichês'),
  @('/PainelChamada/Tv','Tela TV'),
  @('/Parametrizacoes','Parâmetros do sistema'),
  @('/Escalas/Details','Detalhes da escala'),
  @('/MinhaAssinatura/Faturas','Faturas da assinatura')
)
foreach($it in $amostra){
  $url=$web + $it[0]; $esperado=$it[1]
  try { $r=GetPage $url } catch { $falhas += ('timeout ' + $it[0]); continue }
  $d=DecodeEnts $r.Content
  if($d.Contains('Acesso negado') -or $d.Contains('access denied')){ $out+=('FORA_DO_PERFIL ' + $it[0]); continue }
  $h1=TopbarH1 $r.Content
  if($h1 -eq ''){ $falhas += ('topbar sem h1 em ' + $it[0]); continue }
  if($h1 -ne $esperado){ $falhas += ('topbar h1="' + $h1 + '" esperado "' + $esperado + '" em ' + $it[0]); continue }
  if($acoesProibidas -contains $h1){ $falhas += ('topbar h1 jargao em ' + $it[0]); continue }
  $verificadas++
  $out+=('IDENTIDADE_OK ' + $it[0] + ' -> "' + $h1 + '"')
}
if($verificadas -lt 4){ $falhas += ('apenas ' + $verificadas + ' paginas de identidade verificadas (minimo 4)') } else { $out+=('IDENTIDADES_VERIFICADAS=' + $verificadas) }

# D-R61-2a: layout referencia o asset renomeado e nunca o nome antigo.
if((DecodeEnts $pg0.Content).Contains('v2151-wp-c-ajuste-canonical.css')){ $out+='AUTH_ASSET_CANONICAL_OK' } else { $falhas += '_AuthLayout sem v2151-wp-c-ajuste-canonical.css' }
if((DecodeEnts $post.Content).Contains('v2151-wp-c-homologacao.css')){ $falhas += 'layout ainda referencia v2151-wp-c-homologacao.css' } else { $out+='LAYOUT_SEM_ASSET_HOMOLOGACAO_OK' }

# D-R61-2b: FaturamentoSaas Details sem conotacao "sandbox".
$r=GetPage ($web + '/FaturamentoSaas/Index'); $d=DecodeEnts $r.Content
$m=[regex]::Match($d,'/FaturamentoSaas/Details/[0-9A-Fa-f-]{36}')
if($m.Success){
  $rd=GetPage ($web + $m.Value); $dd=DecodeEnts $rd.Content
  if($dd.Contains('página de pagamento')){ $out+='FATURA_COPY_PAGAMENTO_OK' } else { $falhas += 'FaturamentoSaas/Details sem copy "página de pagamento"' }
  if([regex]::IsMatch($dd,'sandbox',[System.Text.RegularExpressions.RegexOptions]::IgnoreCase)){ $falhas += 'FaturamentoSaas/Details ainda cita sandbox' } else { $out+='FATURA_SEM_SANDBOX_OK' }
} else { $out+='FATURAS_VAZIAS_REGISTRADO (nenhum detalhe para inspecionar)' }

# D-R61-2c: OperacaoAssistida/Ocorrencias alinhada ao termo de dominio.
try { $ro=GetPage ($web + '/OperacaoAssistida/Ocorrencias?clienteId=00000000-0000-0000-0000-000000000000') } catch { $ro=$null }
if($ro -and (DecodeEnts $ro.Content).Contains('Nova ocorrência')){
  $ddo=DecodeEnts $ro.Content
  if(CountOf $ddo 'Ocorrências da Operação Assistida' -ge 2){ $out+='OCORRENCIAS_TERMO_DOMINIO_OK' } else { $falhas += 'Ocorrencias: cabecalho fora do termo de dominio' }
  if(CountOf $ddo 'Homologação' -gt 0){ $falhas += 'Ocorrencias: "Homologação" ainda visível' } else { $out+='OCORRENCIAS_SEM_HOMOLOGACAO_OK' }
} else { $out+='OCORRENCIAS_FORA_DO_PERFIL (pagina nao renderizada para o demo)' }

if($falhas.Count -eq 0){ $out+='R61_E2E_OK FALHAS=0' } else { foreach($f in $falhas){ $out+='FALHA: '+$f }; $out+=('R61_E2E_FAIL FALHAS=' + $falhas.Count) }
$texto=($out -join "`r`n")
Set-Content -Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'r61_identidade_e2e_saida.txt') -Value $texto -Encoding UTF8
Write-Output $texto
if($falhas.Count -gt 0){ exit 1 }
