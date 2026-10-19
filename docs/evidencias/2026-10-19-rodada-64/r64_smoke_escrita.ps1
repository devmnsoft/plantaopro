# R64 - smoke de escrita ponta a ponta (fila r53b1 §7 item 9): COMPRAS
# Fluxo real pelo navegador Web (forms + antiforgery), contra o demo Santa Casa:
#   1. seed idempotente aplicado 2x seguidas (prova de reaplicabilidade)
#   2. CRIAR pedido de compra (fornecedor/produto SMOKE R64 do seed)
#   3. APROVAR o pedido (congelamento de preços, Idempotency-Key APROV-{id})
#   4. RECEBER no estoque (lote, local interno, condicao LIBERADO)
#   5. provas no banco: situacao RECEBIDO, lote/movimento LIBERADO, titulo a pagar
# Pre-requisito (padrao de casa): API em http://localhost:51976 e Web em
# http://localhost:51977, Release, contra o banco definido em $db (padrao
# plantaopro_test). Uso: powershell -ExecutionPolicy Bypass -File r64_smoke_escrita.ps1
$ErrorActionPreference='Stop'
$web='http://localhost:51977'
$db=$env:R64_DB
if(-not $db){ $db='plantaopro_test' }
$psql='C:\Program Files\PostgreSQL\18\bin\psql.exe'
$repoRoot=Split-Path -Parent (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)))
$seed=Join-Path $repoRoot 'database/seeds/2026_r64_smoke_escrita_compras_demo.sql'
$scriptDir=Split-Path -Parent $MyInvocation.MyCommand.Path
$logPath=Join-Path $scriptDir 'r64_smoke_escrita_saida.txt'
$tenant='d3f6584c-2c64-4e5a-9ea9-4e1428647502'
$fornUuid='7c8f0a01-6d4a-4a63-9e11-a1b2c3d4e5f1'
$prodUuid='7c8f0a02-6d4a-4a63-9e11-a1b2c3d4e5f2'
$localUuid='7c8f0a03-6d4a-4a63-9e11-a1b2c3d4e5f3'
$fornNome='Fornecedor SMOKE R64 (teste)'
$falhas=@()
$okFase=$true
$pedidoId=''

function DecodeEnts([string]$t){
  return [regex]::Replace($t, '&#(x)?([0-9A-Fa-f]+);', { param($m) $cp=0; if($m.Groups[1].Success -and $m.Groups[1].Value -ne ''){ $cp=[Convert]::ToInt32($m.Groups[2].Value,16) } else { $cp=[Convert]::ToInt32($m.Groups[2].Value,10) }; [string][char]$cp })
}
function CountOf([string]$text,[string]$sub){ $n=0; $i=$text.IndexOf($sub); while($i -ge 0){ $n++; $i=$text.IndexOf($sub,$i+1) }; return $n }
function GetToken([string]$html){
  $p=$html.IndexOf('name="__RequestVerificationToken"')
  if($p -lt 0){ throw 'token antiforgery nao encontrado' }
  $v=$html.IndexOf('value="',$p)
  $s2=$v+7
  return $html.Substring($s2,$html.IndexOf('"',$s2)-$s2)
}
function SqlQ([string]$sql){
  $env:PGPASSWORD='123456'
  return (& $psql -h 127.0.0.1 -U postgres -d $db -Atc $sql) -join ''
}
$out=@()
$out+=("R64 SMOKE ESCRITA " + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss') + " banco=" + $db)

# ---- 0) web de pe ------------------------------------------------------------------
for($w=0;$w -lt 15;$w++){
  try { Invoke-WebRequest -Uri ($web+'/Account/Login') -UseBasicParsing -TimeoutSec 5 | Out-Null; break } catch { Start-Sleep -Seconds 2 }
}

# ---- 1) seed idempotente (duas aplicacoes seguidas) --------------------------------
$env:PGPASSWORD='123456'
& $psql -h 127.0.0.1 -U postgres -d $db -v ON_ERROR_STOP=1 -f $seed | Out-Null
if($LASTEXITCODE -ne 0){ $falhas+=('seed aplicacao 1 falhou (exit=' + $LASTEXITCODE + ')') ; $okFase=$false }
if($okFase){ & $psql -h 127.0.0.1 -U postgres -d $db -v ON_ERROR_STOP=1 -f $seed | Out-Null; if($LASTEXITCODE -ne 0){ $falhas+=('seed aplicacao 2 falhou (exit=' + $LASTEXITCODE + ')') ; $okFase=$false } }
if($okFase){
  $cForn=[int](SqlQ ("select count(*) from plantaopro.adm360_parceiros where id='" + $fornUuid + "'"))
  $cProd=[int](SqlQ ("select count(*) from plantaopro.adm360_produtos where id='" + $prodUuid + "'"))
  $cLoc =[int](SqlQ ("select count(*) from plantaopro.adm360_locais where id='" + $localUuid + "'"))
  $out+=("SEED_IDEMPOTENTE fornecedor=$cForn produto=$cProd local=$cLoc")
  if($cForn -ne 1 -or $cProd -ne 1 -or $cLoc -ne 1){ $falhas+=('seed com duplicidade apos 2 aplicacoes: ' + $cForn + '/' + $cProd + '/' + $cLoc); $okFase=$false }
}

# ---- login demo ---------------------------------------------------------------------
if($okFase){
  $s=New-Object Microsoft.PowerShell.Commands.WebRequestSession
  $pg=Invoke-WebRequest -Uri ($web+'/Account/Login') -WebSession $s -UseBasicParsing
  $tok=GetToken $pg.Content
  $body='Email='+[System.Uri]::EscapeDataString('gestor@santacasa-demo.example')+'&Senha='+[System.Uri]::EscapeDataString('SantaCasa!Demo2026#Gestor')+'&__RequestVerificationToken='+[System.Uri]::EscapeDataString($tok)
  $post=Invoke-WebRequest -Uri ($web+'/Account/Login') -Method Post -WebSession $s -Body $body -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -MaximumRedirection 10
  if([int]$post.StatusCode -ne 200){ throw ('login falhou status=' + [int]$post.StatusCode) }
  $out+='LOGIN_OK'
}
# warm-up (regra de casa: app recem-subido pode falhar na 1a request autenticada)
if($okFase){
  for($w=0;$w -lt 6;$w++){
    try { $wq=Invoke-WebRequest -Uri ($web+'/Administrativo360/PedidosCompra') -WebSession $s -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 10; if([int]$wq.StatusCode -eq 200){ break } } catch { Start-Sleep -Seconds 2 }
  }
}

# ---- 2) CRIAR pedido -------------------------------------------------------------------
if($okFase){
  $page=Invoke-WebRequest -Uri ($web+'/Administrativo360/PedidosCompra') -WebSession $s -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 10
  $mF=[regex]::Match($page.Content,'<option value="([0-9a-f-]{36})"[^>]*>Fornecedor SMOKE R64')
  $mP=[regex]::Match($page.Content,'<option value="([0-9a-f-]{36})"[^>]*>SMK64')
  if(-not $mF.Success){ $falhas+=('CRIAR: fornecedor SMOKE ausente no select (seed nao visivel na UI)'); $okFase=$false }
  elseif(-not $mP.Success){ $falhas+=('CRIAR: produto SMK64 ausente no select (seed nao visivel na UI)'); $okFase=$false }
}
if($okFase){
  $tok=GetToken $page.Content
  $previsao=(Get-Date).AddDays(5).ToString('yyyy-MM-dd')
  $validade=(Get-Date).AddYears(2).ToString('yyyy-MM-dd')
  $cBody='fornecedorId='+[System.Uri]::EscapeDataString($mF.Groups[1].Value)+'&previsao='+[System.Uri]::EscapeDataString($previsao)+'&frete=0.00'
  $cBody+='&produtoId='+[System.Uri]::EscapeDataString($mP.Groups[1].Value)+'&quantidade=2&precoUnitario=12.50&desconto=0.00'
  $cBody+='&__RequestVerificationToken='+[System.Uri]::EscapeDataString($tok)
  $criar=Invoke-WebRequest -Uri ($web+'/Administrativo360/CriarPedidoCompra') -Method Post -WebSession $s -Body $cBody -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 10
  $criou=((DecodeEnts $criar.Content)).Contains('cadastrado com sucesso em rascunho')
  if(-not $criou){ $falhas+=('CRIAR: sem flash de sucesso e sem linha de rascunho visivel'); $okFase=$false }
  $out+=('CRIAR_OK rascunho=' + $criou)
}

# ---- 3) APROVAR ---------------------------------------------------------------------------
if($okFase){
  $listR=Invoke-WebRequest -Uri ($web+'/Administrativo360/PedidosCompra?fornecedor='+[System.Uri]::EscapeDataString($fornNome)+'&situacao=RASCUNHO') -WebSession $s -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 10
  $mA=[regex]::Match($listR.Content,'AprovarPedidoCompra[\s\S]{0,400}?name="id" value="([0-9a-f-]{36})"')
  if(-not $mA.Success){ $falhas+=('APROVAR: pedido rascunho do fornecedor SMOKE nao localizado na lista filtrada'); $okFase=$false }
}
if($okFase -and $mA.Success){
  $pedidoId=$mA.Groups[1].Value
  $tok=GetToken $listR.Content
  $aBody='id='+[System.Uri]::EscapeDataString($pedidoId)+'&__RequestVerificationToken='+[System.Uri]::EscapeDataString($tok)
  $aprovar=Invoke-WebRequest -Uri ($web+'/Administrativo360/AprovarPedidoCompra') -Method Post -WebSession $s -Body $aBody -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 10
  $listA=Invoke-WebRequest -Uri ($web+'/Administrativo360/PedidosCompra?fornecedor='+[System.Uri]::EscapeDataString($fornNome)+'&situacao=APROVADO') -WebSession $s -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 10
  $aprovou=((DecodeEnts $aprovar.Content)).Contains('aprovado com sucesso') -and ((CountOf $listA.Content $pedidoId) -ge 1)
  if(-not $aprovou){ $falhas+=('APROVAR: pedido nao apareceu como APROVADO na lista filtrada'); $okFase=$false }
  $out+=('APROVAR_OK pedido='+$pedidoId)
}

# ---- 4) RECEBER ----------------------------------------------------------------------------
if($okFase){
  $recPage=Invoke-WebRequest -Uri ($web+'/Administrativo360/Recebimentos') -WebSession $s -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 10
  $recContent=$recPage.Content
  $mPed=[regex]::Match($recContent,'<option value="'+$pedidoId+'">[^<]*Fornecedor SMOKE R64')
  $mItem=[regex]::Match($recContent,'data-pedido="'+$pedidoId+'"[\s\S]{0,400}?<option value="([0-9a-f-]{36})">')
  $mLoc=[regex]::Match($recContent,'<option value="([0-9a-f-]{36})"[^>]*>SMK64ALM')
  if(-not $mPed.Success){ $falhas+=('RECEBER: pedido aprovado nao aparece em Recebimentos pendentes'); $okFase=$false }
  elseif(-not $mItem.Success){ $falhas+=('RECEBER: item do pedido nao exposto no seletor (saldo/F-R15-C)'); $okFase=$false }
  elseif(-not $mLoc.Success){ $falhas+=('RECEBER: local interno SMOKE nao exposto no seletor'); $okFase=$false }
}
if($okFase){
  $tok=GetToken $recContent
  $documento='NF-SMOKE-R64-'+(Get-Date -Format 'yyMMddHHmmss')
  $rBody='pedidoId='+[System.Uri]::EscapeDataString($pedidoId)+'&documento='+[System.Uri]::EscapeDataString($documento)
  $rBody+='&pedidoItemId='+[System.Uri]::EscapeDataString($mItem.Groups[1].Value)+'&quantidade=2&lote=LOTE-R64-SMOKE'
  $rBody+='&validade='+[System.Uri]::EscapeDataString($validade)+'&localId='+[System.Uri]::EscapeDataString($mLoc.Groups[1].Value)
  $rBody+='&documentoXmlId=&__RequestVerificationToken='+[System.Uri]::EscapeDataString($tok)
  $receber=Invoke-WebRequest -Uri ($web+'/Administrativo360/ConfirmarRecebimento') -Method Post -WebSession $s -Body $rBody -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 10
  $listRec=Invoke-WebRequest -Uri ($web+'/Administrativo360/PedidosCompra?fornecedor='+[System.Uri]::EscapeDataString($fornNome)+'&situacao=RECEBIDO') -WebSession $s -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 10
  $recebeu=((DecodeEnts $receber.Content)).Contains('Recebimento confirmado com sucesso') -and ((CountOf $listRec.Content $pedidoId) -ge 1)
  if(-not $recebeu){ $falhas+=('RECEBER: pedido nao fechou como RECEBIDO Total'); $okFase=$false }
  $out+=('RECEBER_OK documento='+$documento+' recebidoTotal=' + $recebeu)
}

# ---- 5) provas no banco (estado final) ------------------------------------------------------
if($okFase){
  $situ=(SqlQ ("select coalesce(situacao,'AUSENTE') from plantaopro.adm360_pedidos where id='" + $pedidoId + "'"))
  $loteN=[int](SqlQ ("select count(*) from plantaopro.adm360_lotes where tenant_id='" + $tenant + "' and codigo='LOTE-R64-SMOKE'"))
  $mov=(SqlQ ("select coalesce(sum(m.quantidade),0)::text || '/' || count(*) || '/' || coalesce(bool_and(m.condicao='LIBERADO'),false)::text from plantaopro.adm360_movimentos m join plantaopro.adm360_lotes l on l.id=m.lote_id where l.tenant_id='" + $tenant + "' and l.codigo='LOTE-R64-SMOKE' and m.tipo='ENTRADA'"))
  $tit=[int](SqlQ ("select count(*) from plantaopro.adm360_titulos_pagar tp where tp.tenant_id='" + $tenant + "' and tp.fornecedor_id='" + $fornUuid + "'"))
  $out+=('DB situacao=' + $situ + ' lotes=' + $loteN + ' movimentosEntrada(total/cx/liberadoTodas)=' + $mov + ' titulosPagarFornecedor=' + $tit)
  if($situ -ne 'RECEBIDO'){ $falhas+=('DB: situacao final do pedido != RECEBIDO (' + $situ + ')') }
  if($loteN -lt 1){ $falhas+=('DB: lote LOTE-R64-SMOKE nao criado no recebimento') }
  if($mov -notmatch '^(\d+(?:\.\d+)?)/(\d+)/true$'){ $falhas+=('DB: movimento de entrada nao integralmente LIBERADO: ' + $mov) }
  if($tit -lt 1){ $falhas+=('DB: obrigacao em contas a pagar nao gerada para o recebimento') }
}

$out+=('FALHAS=' + $falhas.Count)
$out+=$falhas
Set-Content -Path $logPath -Value ($out -join "`n") -Encoding UTF8
if($falhas.Count -gt 0){ Write-Output ($out -join "`n"); throw ('R64 smoke escrita com ' + $falhas.Count + ' falha(s)') }
Write-Output ($out -join "`n")
Write-Output 'R64_E2E_OK'
