# R100 - notificacoes pautadas no calendario fiscal (R84) + SLAs + escalonamento
# Execucao canonica: sobe API :52084 + Web :52083 (DLLs Release, banco vivo plantaopro_test)
# e prova o contrato da rodada de ponta a ponta:
#   PA_*             um plano de drill (sem vinculos) + dois mundos TRIAL pelo funil
#                    self-service; ADM360 habilitado no mundo A pela rota real
#                    habilitar-tenant (governanca, origem CONTRATO, acao HABILITAR)
#   CALENDARIO_PAUTA leitura lazy do calendario (GET calendario) pautando em melhor esforco:
#                    o periodo exibido 10/2026 tem 5 obrigacoes mensais ATRASADA na data do
#                    drill e vira notificacao de caixa dos gestores
#   T1               dump do banco ANTES do gatilho explicito (mundo A: 5 avisos base +
#                    3 escalonados para atraso >= limiar da fonte unica; mundo B: zero)
#   GERAR_IDEMPOTENTE gatilho explicito (POST notificacoes/gerar) nao duplica nada
#                    (chave canonica usuario+origem+titulo, on conflict do nothing)
#   CAIXA            a caixa in-app do usuario (plantaopro.notificacoes, mesma rota que a
#                    app usa) lista as 8 novas nao lidas, base + ESCALONADO
#   NOVEMBRO_AGENDADO periodo exibido seguinte com todos fora da janela oficial:
#                    tudo AGENDADA -> nenhuma nova notificacao
#   CONCLUIDA        concluir uma obrigacao tira-a dos alertas sem duplicar a caixa
#   CONTROLE         mundo sem ADM360: a visao de SLA existe, mas a caixa nao recebe
#   WEB_*            sessao do dono: /CalendarioFiscalWeb renderiza o cartao server-side
#                    "SLA e escalonamento" com badges ESCALONADO/CRITICO; /Notificacoes abre
# A limpeza no inicio garante rerun previsivel; os tenants permanecem ao final.
$ErrorActionPreference = 'Stop'

$repo     = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$db       = 'plantaopro_test'
$porta    = 52084
$webPorta = 52083
$apiUrl   = 'http://localhost:' + $porta
$webUrl   = 'http://localhost:' + $webPorta
$apiDll   = Join-Path $repo 'backend/PlantaoPro.Api/bin/Release/net10.0/PlantaoPro.Api.dll'
$webDll   = Join-Path $repo 'backend/PlantaoPro.Web/bin/Release/net10.0/PlantaoPro.Web.dll'
$psql     = "C:\Program Files\PostgreSQL\18\bin\psql.exe"

$env:PGPASSWORD = '123456'
$env:PGCLIENTENCODING = 'UTF8'
$saida = Join-Path $PSScriptRoot 'r100a_drill_saida.txt'
$t1arquivo = Join-Path $PSScriptRoot 'r100a_t1_alertas_banco.txt'
$cnpjAdm  = '99003003000166'
$cnpjCtrl = '99004004000177'

$linhas = New-Object System.Collections.Generic.List[string]
function Log([string]$l) { $linhas.Add($l); Write-Host $l }
function EscreverSaida() { Set-Content -Path $saida -Value $linhas -Encoding UTF8 }

$global:Falhou = $false
$global:PidApi = $null
$global:PidWeb = $null
function PararApps() {
  try { if ($global:PidWeb) { Stop-Process -Id $global:PidWeb -Force -ErrorAction SilentlyContinue } } catch {}
  try { if ($global:PidApi) { Stop-Process -Id $global:PidApi -Force -ErrorAction SilentlyContinue } } catch {}
}
function Falhar([string]$msg) {
  $global:Falhou = $true
  Log ('FALHA ' + $msg)
  Log 'RESULTADO=FALHOU'
  EscreverSaida
  PararApps
  exit 1
}
function Psq([string]$sql) {
  # EAP=Stop transforma stderr nativo do psql em erro terminante (PS 5.1);
  # aqui o erro vem como TEXTO na saida e as asseroes do drill decidem.
  $prevEap = $ErrorActionPreference
  $ErrorActionPreference = 'Continue'
  try { $out = & $psql -h localhost -U postgres -d $db -X -q -t -A -c $sql 2>&1 | Out-String }
  finally { $ErrorActionPreference = $prevEap }
  return $out.Trim()
}
function PortaLivre([string]$p) {
  return (-not (Get-NetTCPConnection -State Listen -LocalPort $p -ErrorAction SilentlyContinue))
}
function ApiAguardarNoAr([int]$tentativas) {
  for ($i = 0; $i -lt $tentativas; $i++) {
    Start-Sleep 1
    try { $h = Invoke-WebRequest ($apiUrl + '/api/health') -UseBasicParsing -TimeoutSec 2; if ([int]$h.StatusCode -eq 200) { return $true } } catch {}
  }
  return $false
}
function WebAguardarNoAr([int]$tentativas) {
  for ($i = 0; $i -lt $tentativas; $i++) {
    Start-Sleep 1
    try { Invoke-WebRequest ($webUrl + '/Account/Login') -UseBasicParsing -TimeoutSec 2 | Out-Null; return $true }
    catch { if ($_.Exception.Response) { return $true } }
  }
  return $false
}
function FazerLogin([string]$email, [string]$senha) {
  $bodyJ = @{ Email = $email; Senha = $senha } | ConvertTo-Json
  $rj = Invoke-WebRequest ($apiUrl + '/api/auth/login') -Method Post -Body $bodyJ -ContentType 'application/json' -UseBasicParsing
  if ([int]$rj.StatusCode -ne 200) { throw ('login ' + $email + ' http ' + [int]$rj.StatusCode) }
  $j = $rj.Content | ConvertFrom-Json
  $tok = $j.data.token
  if (-not $tok) { throw 'login ' + $email + ' sem token' }
  if ([bool]$j.data.tenantContextRequired) {
    $r2 = Invoke-WebRequest ($apiUrl + '/api/auth/refresh-context') -Method Post -Headers @{ Authorization = 'Bearer ' + $tok } -UseBasicParsing
    $tok = ($r2.Content | ConvertFrom-Json).data.token
  }
  return $tok
}
# Requisicao generica (GET/POST): devolve Http+Json e nunca lanca em 4xx/5xx.
function HttpRequest([string]$metodo, [string]$url, [string]$token, $payload) {
  try {
    $hdr = @{}
    if ($token) { $hdr['Authorization'] = 'Bearer ' + $token }
    if ($null -ne $payload) {
      $bodyJ = $payload | ConvertTo-Json -Depth 6
      $r = Invoke-WebRequest $url -Method $metodo -Body $bodyJ -ContentType 'application/json' -Headers $hdr -UseBasicParsing
      return [pscustomobject]@{ Http = [int]$r.StatusCode; Json = ($r.Content | ConvertFrom-Json); Content = [string]$r.Content }
    } else {
      $r = Invoke-WebRequest $url -Method $metodo -Headers $hdr -UseBasicParsing
      return [pscustomobject]@{ Http = [int]$r.StatusCode; Json = ($r.Content | ConvertFrom-Json); Content = [string]$r.Content }
    }
  } catch {
    $resp = $_.Exception.Response
    $code = 0
    $txt = ''
    if ($resp) {
      $code = [int]$resp.StatusCode
      $sr = New-Object IO.StreamReader($resp.GetResponseStream())
      $txt = $sr.ReadToEnd()
    }
    $json = $null
    try { if ($txt) { $json = $txt | ConvertFrom-Json } } catch {}
    return [pscustomobject]@{ Http = $code; Json = $json; Content = $txt }
  }
}
function LimparPlanoPorCodigo([string]$codigo) {
  $q = 'select id from plantaopro.planos where upper(codigo)=upper(''' + $codigo + ''')'
  $sql  = 'delete from plantaopro.plano_recursos where plano_id in (' + $q + ');'
  $sql += 'delete from plantaopro.plano_modulos where plano_id in (' + $q + ');'
  $sql += 'delete from plantaopro.plano_faq where plano_id in (' + $q + ');'
  $sql += 'delete from plantaopro.assinatura_historico where assinatura_id in (select id from plantaopro.assinaturas where plano_id in (' + $q + '));'
  $sql += 'delete from plantaopro.assinaturas where plano_id in (' + $q + ');'
  $sql += 'delete from plantaopro.planos where upper(codigo)=upper(''' + $codigo + ''');'
  Psq $sql | Out-Null
}
function LimparMundoPorCnpj([string]$cnpj) {
  $ids = Psq ("select c.id || '|' || coalesce((select id from plantaopro.tenants where cliente_id=c.id)::text,'') || '|' || coalesce((select id from plantaopro.assinaturas where cliente_id=c.id)::text,'') || '|' || coalesce((select id from plantaopro.usuarios where cliente_id=c.id)::text,'') from plantaopro.clientes c where regexp_replace(cnpj,'\\D','','g')='" + $cnpj + "'")
  foreach ($linha in (($ids -replace "`r","") -split "`n")) {
    if (-not $linha) { continue }
    $p = $linha.Split('|')
    $qCliente = if ($p[0]) { "'" + $p[0] + "'" } else { 'null' }
    $qAssina = if ($p[2]) { "'" + $p[2] + "'" } else { 'null' }
    $sql = ('delete from plantaopro.cadastro_cliente_pagamentos_iniciais where assinatura_id=' + $qAssina + ';')
    $sql += ('delete from plantaopro.assinatura_historico where assinatura_id=' + $qAssina + ';')
    $sql += ('delete from plantaopro.cliente_alertas where cliente_id=' + $qCliente + ';')
    $sql += ('delete from plantaopro.cliente_bloqueios where cliente_id=' + $qCliente + ';')
    $sql += ('delete from plantaopro.assinatura_uso where cliente_id=' + $qCliente + ';')
    $sql += ('delete from plantaopro.faturas_saas where cliente_id=' + $qCliente + ';')
    $sql += ('delete from plantaopro.paineis_publicos where cliente_id=' + $qCliente + ';')
    $sql += ('delete from plantaopro.painel_publico_tokens where cliente_id=' + $qCliente + ';')
    $sql += ('delete from plantaopro.lgpd_consentimentos where cliente_id=' + $qCliente + ';')
    $sql += ('delete from plantaopro.tenant_onboarding_checklist where cliente_id=' + $qCliente + ';')
    $sql += ('delete from plantaopro.tenant_onboarding where cliente_id=' + $qCliente + ';')
    if ($p[1]) {
      # R100: purga o estado fiscal + contratos de modulo antes de derrubar o tenant.
      $sql += ('delete from plantaopro.adm_obrigacoes_fiscais where tenant_id=' + "'" + $p[1] + "';")
      $sql += ('delete from plantaopro.tenant_modulos_historico where tenant_id=' + "'" + $p[1] + "';")
      $sql += ('delete from plantaopro.tenant_modulos where tenant_id=' + "'" + $p[1] + "';")
      $sql += ('delete from plantaopro.tenant_white_label where tenant_id=' + "'" + $p[1] + "';")
      $sql += ('delete from plantaopro.perfil_permissoes where perfil_id in (select id from plantaopro.perfis where tenant_id=' + "'" + $p[1] + "');")
      $sql += ('delete from plantaopro.perfis where tenant_id=' + "'" + $p[1] + "';")
      $sql += ('delete from plantaopro.tenants where id=' + "'" + $p[1] + "';")
    }
    if ($p[3]) {
      # R100: a caixa do usuario vai junto (notificacoes pautadas pelo calendario fiscal).
      $sql += ('delete from plantaopro.notificacoes where usuario_id=' + "'" + $p[3] + "';")
      $sql += ('delete from plantaopro.usuarios_perfis where usuario_id=' + "'" + $p[3] + "';")
      $sql += ('delete from plantaopro.usuarios where id=' + "'" + $p[3] + "';")
    }
    if ($p[2]) { $sql += ('delete from plantaopro.assinaturas where id=' + "'" + $p[2] + "';") }
    if ($p[0]) { $sql += ('delete from plantaopro.clientes where id=' + "'" + $p[0] + "';") }
    Psq $sql | Out-Null
  }
  Psq ("delete from plantaopro.cadastro_cliente_solicitacoes where regexp_replace(cnpj,'\\D','','g')='" + $cnpj + "'") | Out-Null
}
function LimparAiPorCnpjs([string[]]$cnpjs) {
  foreach ($cj in $cnpjs) {
    $t = Psq ("select coalesce((select id from plantaopro.tenants where cliente_id=c.id)::text,'') from plantaopro.clientes c where regexp_replace(cnpj,'\\D','','g')='" + $cj + "'")
    if ($t) {
      Psq ("delete from plantaopro.ai_chamadas_ativas where tenant_id='" + $t + "';" +
           "delete from plantaopro.ai_usos where tenant_id='" + $t + "';" +
           "delete from plantaopro.ai_config where tenant_id='" + $t + "';") | Out-Null
    }
  }
}
function ContarCaixaFiscal([string]$tenantId) {
  return Psq ("select count(*) from plantaopro.notificacoes n join plantaopro.usuarios u on u.id=n.usuario_id where u.tenant_id='" + $tenantId + "' and n.reg_status='A' and n.origem_tipo like 'OBRIGACAO_FISCAL%'")
}

Log ('R100A_DRILL data=' + (Get-Date -Format 'yyyyMMdd-HHmmss') + ' db=' + $db + ' api=' + $porta + ' web=' + $webPorta)

# ---------------------------------------------------------------- ESTAGEM 1: portas + apps
if (-not (Test-Path $apiDll)) { Falhar ('DLL da API ausente: ' + $apiDll) }
if (-not (Test-Path $webDll)) { Falhar ('DLL da Web ausente: ' + $webDll) }
if (-not (PortaLivre $porta) -or (-not (PortaLivre $webPorta))) { Falhar ('portas ocupadas antes do inicio (api=' + $porta + ' web=' + $webPorta + ')') }
Log 'PORTAS_LIVRES_INICIO=SIM'

$env:ConnectionStrings__Default = 'Host=127.0.0.1;Port=5432;Database=' + $db + ';Username=postgres;Password=123456'
$env:Jwt__Key = 'r100-drill-dev-jwt-key-0123456789-abcd'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
# regra de casa (R12): com execucao direta da DLL o content root muda - BaseUrl explicito
$env:ApiSettings__BaseUrl = $apiUrl
$env:PlantaoProApi__BaseUrl = $apiUrl

$apiProc = Start-Process -FilePath 'dotnet' -ArgumentList ('"' + $apiDll + '" --urls http://localhost:' + $porta) -WorkingDirectory (Join-Path $repo 'backend/PlantaoPro.Api') -WindowStyle Hidden -PassThru
if (-not (ApiAguardarNoAr 90)) { Falhar 'API nao ficou no ar em 90s' }
$global:PidApi = [int]$apiProc.Id
Log ('ESTAGEM_1_API_NO_AR ok=SIM pid=' + $global:PidApi)

$webProc = Start-Process -FilePath 'dotnet' -ArgumentList ('"' + $webDll + '" --urls http://localhost:' + $webPorta) -WorkingDirectory (Join-Path $repo 'backend/PlantaoPro.Web') -WindowStyle Hidden -PassThru
if (-not (WebAguardarNoAr 90)) { Falhar 'Web nao ficou no ar em 90s' }
$global:PidWeb = [int]$webProc.Id
Log ('ESTAGEM_1_WEB_NO_AR ok=SIM pid=' + $global:PidWeb)

# -------------------------------------------------- limpeza para rerun previsivel
LimparAiPorCnpjs @($cnpjAdm, $cnpjCtrl) | Out-Null
LimparMundoPorCnpj $cnpjAdm | Out-Null
LimparMundoPorCnpj $cnpjCtrl | Out-Null
LimparPlanoPorCodigo 'R100_PLANO_DRILL' | Out-Null
Log 'PRE_LIMPEZA_RERUN ok=SIM'

$tokSup = FazerLogin 'superadmin@plantaopro.local' 'Super@123456'

# ---------------------------------------------------------------- PA_PLANO (governanca, sem vinculos)
$rPlano = HttpRequest 'POST' ($apiUrl + '/api/planos') $tokSup @{
  nome = 'R100 Plano Drill'
  descricao = 'Plano do drill r100 (sem vinculos; ADM360 entra por governanca por tenant)'
  valorMensal = 100.00
  limiteMedicos = 0; limiteHospitais = 0; limitePlantoesMes = 0
  permiteMobile = $true; permiteBi = $false; permiteRelatoriosAvancados = $false; permiteIntegracoes = $false
  codigo = 'r100_plano_drill'
  slug = 'r100-plano-drill'
  valorAnual = 1250.00
  publico = $true; destaque = $false; ordem = 956; permiteWhiteLabel = $false
  permiteTrial = $true
  diasTrial = 5
}
if ($rPlano.Http -ne 200) { Falhar ('PA_PLANO http ' + $rPlano.Http + ' ' + $(if ($rPlano.Json) { $rPlano.Json.message } else { 'sem corpo' })) }
$idPlano = [string]$rPlano.Json.data
$nVinc = Psq ("select count(*) from plantaopro.plano_modulos where plano_id='" + $idPlano + "' and reg_status='A'")
if ($nVinc -ne '0') { Falhar ('PA_PLANO vinculos=' + $nVinc + ' esperado 0') }
Log ('PA_PLANO_OK plano=' + $idPlano + ' vinculos=0')

# ---------------------------------------------------------------- PA_MUNDOS (funil self-service, TRIAL 5d)
$rA = HttpRequest 'POST' ($apiUrl + '/api/public/cadastro/finalizar') '' @{
  empresa      = @{ nomeFantasia = 'R100 Drill Adm'; razaoSocial = 'R100 Drill Adm LTDA'; cnpj = $cnpjAdm; segmento = 'Clinica'; cidade = 'Recife'; uf = 'PE'; telefone = '(81) 99999-0010'; emailCorporativo = 'r100-drill-adm@plantaopro-local.test' }
  plano        = @{ planoId = $idPlano; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
  usuarioAdmin = @{ nome = 'R100 Adm Admin'; email = 'r100-drill-adm@plantaopro-local.test'; cargo = 'Gestora'; senha = 'R100-adm-2026#Kp9z' }
  statusAssinatura = 'TRIAL'
  diasTrial      = 5
}
if ($rA.Http -ne 200) { Falhar ('PA_MUNDOS adm http ' + $rA.Http + ' ' + $(if ($rA.Json) { $rA.Json.message } else { 'sem corpo' })) }
$aTenantId = [string]$rA.Json.data.tenantId
$aStatus = Psq ("select coalesce(status,'') from plantaopro.assinaturas where id='" + $rA.Json.data.assinaturaId + "'")
if ($aStatus -ne 'TRIAL') { Falhar ('PA_MUNDOS adm status=' + $aStatus) }

$rB = HttpRequest 'POST' ($apiUrl + '/api/public/cadastro/finalizar') '' @{
  empresa      = @{ nomeFantasia = 'R100 Drill Ctrl'; razaoSocial = 'R100 Drill Ctrl LTDA'; cnpj = $cnpjCtrl; segmento = 'Clinica'; cidade = 'Olinda'; uf = 'PE'; telefone = '(81) 99999-0011'; emailCorporativo = 'r100-drill-ctrl@plantaopro-local.test' }
  plano        = @{ planoId = $idPlano; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
  usuarioAdmin = @{ nome = 'R100 Ctrl Admin'; email = 'r100-drill-ctrl@plantaopro-local.test'; cargo = 'Gestor'; senha = 'R100-ctrl-2026#Kp9z' }
  statusAssinatura = 'TRIAL'
  diasTrial      = 5
}
if ($rB.Http -ne 200) { Falhar ('PA_MUNDOS ctrl http ' + $rB.Http + ' ' + $(if ($rB.Json) { $rB.Json.message } else { 'sem corpo' })) }
$bTenantId = [string]$rB.Json.data.tenantId
$bStatus = Psq ("select coalesce(status,'') from plantaopro.assinaturas where id='" + $rB.Json.data.assinaturaId + "'")
if ($bStatus -ne 'TRIAL') { Falhar ('PA_MUNDOS ctrl status=' + $bStatus) }
Log ('PA_MUNDOS_OK adm_tenant=' + $aTenantId + ' adm_status=TRIAL ctrl_tenant=' + $bTenantId + ' ctrl_status=TRIAL')

# ---------------------------------------------------------------- PA_ADM360 (governanca real: habilitar-tenant)
$rMod = HttpRequest 'GET' ($apiUrl + '/api/modulos') $tokSup $null
if ($rMod.Http -ne 200) { Falhar ('PA_ADM360 listagem de modulos http ' + $rMod.Http) }
$idAdm360 = $null
foreach ($m in @($rMod.Json.data)) { if ([string]$m.codigo -eq 'ADM360') { $idAdm360 = [string]$m.id } }
if (-not $idAdm360) { Falhar 'PA_ADM360 modulo ADM360 nao encontrado no catalogo' }

$rHab = HttpRequest 'POST' ($apiUrl + '/api/modulos/' + $idAdm360 + '/habilitar-tenant') $tokSup @{ tenantId = $aTenantId; precoContratado = 0; limiteContratado = 0; motivo = 'Drill R100 - pauta fiscal' }
if ($rHab.Http -ne 200) { Falhar ('PA_ADM360 habilitar http ' + $rHab.Http + ' ' + $(if ($rHab.Json) { $rHab.Json.message } else { 'sem corpo' })) }
$nContr = Psq ("select count(*) from plantaopro.tenant_modulos where tenant_id='" + $aTenantId + "' and reg_status='A' and upper(codigo_modulo)='ADM360'")
$oContr = Psq ("select coalesce(origem,'') from plantaopro.tenant_modulos where tenant_id='" + $aTenantId + "' and reg_status='A' and upper(codigo_modulo)='ADM360'")
$hContr = Psq ("select count(*) from plantaopro.tenant_modulos_historico h join plantaopro.tenant_modulos tm on tm.id=h.tenant_modulo_id where tm.tenant_id='" + $aTenantId + "' and h.acao='HABILITAR'")
$efAdm  = Psq ("select coalesce(string_agg(upper(codigo),',' order by 1),'vazio') from plantaopro.modulos_efetivos('" + $aTenantId + "')")
if ($nContr -ne '1') { Falhar ('PA_ADM360 contrato=' + $nContr + ' esperado 1') }
if ($oContr -ne 'CONTRATO') { Falhar ('PA_ADM360 origem=' + $oContr + ' esperado CONTRATO') }
if ($hContr -ne '1') { Falhar ('PA_ADM360 historico=' + $hContr + ' esperado 1') }
if ($efAdm -ne 'ADM360') { Falhar ('PA_ADM360 efetivos=' + $efAdm + ' esperado ADM360') }
Log 'PA_ADM360_OK http=200 origem=CONTRATO historico=1 efetivos=[ADM360]'

$tokAdm  = FazerLogin 'r100-drill-adm@plantaopro-local.test' 'R100-adm-2026#Kp9z'
$tokCtrl = FazerLogin 'r100-drill-ctrl@plantaopro-local.test' 'R100-ctrl-2026#Kp9z'

# ---------------------------------------------------------------- CALENDARIO_PAUTA (hook lazy no GET calendario)
# Periodo exibido 10/2026 na data do drill: as 5 obrigacoes mensais ja estao ATRASADA
# (dias 7/15/15/20/20 vs hoje). A pauta lazy deveria ter criado a caixa antes do dump t1.
$rCal = HttpRequest 'GET' ($apiUrl + '/api/calendario-fiscal/calendario?mes=10&ano=2026') $tokAdm $null
if ($rCal.Http -ne 200) { Falhar ('CALENDARIO_PAUTA http ' + $rCal.Http + ' ' + $(if ($rCal.Json) { [string]$rCal.Json.erro } else { 'sem corpo' })) }
$nItens  = @($rCal.Json.itens).Count
$nAtras  = @($rCal.Json.itens | Where-Object { $_.estado -eq 'ATRASADA' }).Count
$janela  = [int]$rCal.Json.janelaAlertaDias
if ($nItens -ne 5) { Falhar ('CALENDARIO_PAUTA itens=' + $nItens + ' esperado 5') }
if ($nAtras -ne 5) { Falhar ('CALENDARIO_PAUTA atrasadas=' + $nAtras + ' esperado 5') }
if ($janela -ne 7) { Falhar ('CALENDARIO_PAUTA janela=' + $janela + ' esperado 7 (fonte unica)') }
Log ('CALENDARIO_PAUTA_OK http=200 itens=5 atrasadas=5')

# ---------------------------------------------------------------- T1 (dump do banco antes do gatilho explicito)
$tplT1 = @'
select 'T1_{TAG} tenant='||t.id||
 ' efetivos=['||coalesce((select string_agg(upper(codigo),',' order by 1) from plantaopro.modulos_efetivos(t.id)),'vazio')||']'||
 ' bases='||(select count(*) from plantaopro.notificacoes n join plantaopro.usuarios u on u.id=n.usuario_id where u.tenant_id=t.id and n.reg_status='A' and n.origem_tipo='OBRIGACAO_FISCAL')||
 ' escal='||(select count(*) from plantaopro.notificacoes n join plantaopro.usuarios u on u.id=n.usuario_id where u.tenant_id=t.id and n.reg_status='A' and n.origem_tipo='OBRIGACAO_FISCAL_ESCALONADA')||
 ' total='||(select count(*) from plantaopro.notificacoes n join plantaopro.usuarios u on u.id=n.usuario_id where u.tenant_id=t.id and n.reg_status='A' and n.origem_tipo like 'OBRIGACAO_FISCAL%')||
 ' lida_falsa='||(select count(*) from plantaopro.notificacoes n join plantaopro.usuarios u on u.id=n.usuario_id where u.tenant_id=t.id and n.reg_status='A' and n.origem_tipo like 'OBRIGACAO_FISCAL%' and n.lida=false)||
 ' dist_id='||(select count(distinct n.origem_id) from plantaopro.notificacoes n join plantaopro.usuarios u on u.id=n.usuario_id where u.tenant_id=t.id and n.reg_status='A' and n.origem_tipo like 'OBRIGACAO_FISCAL%')
 from plantaopro.tenants t where t.id='{TENANT}'
'@
$sqlT1a = $tplT1.Replace('{TAG}', 'MUNDO_A').Replace('{TENANT}', $aTenantId)
$sqlT1b = $tplT1.Replace('{TAG}', 'MUNDO_B').Replace('{TENANT}', $bTenantId)
$l1 = Psq $sqlT1a
$l2 = Psq $sqlT1b
Set-Content -Path $t1arquivo -Value @($l1, $l2) -Encoding UTF8
if ($l1 -notlike 'T1_MUNDO_A*') { Falhar ('T1 linha mundo A ausente: ' + $l1) }
if ($l1 -notlike '*efetivos=`[ADM360]*') { Falhar ('T1 mundo A modulos: ' + $l1) }
if ($l1 -notlike '*bases=5*') { Falhar ('T1 mundo A bases: ' + $l1) }
if ($l1 -notlike '*escal=3*') { Falhar ('T1 mundo A escal (atraso>=7): ' + $l1) }
if ($l1 -notlike '*total=8*') { Falhar ('T1 mundo A total: ' + $l1) }
if ($l1 -notlike '*lida_falsa=8*') { Falhar ('T1 mundo A lida_falsa: ' + $l1) }
if ($l1 -notlike '*dist_id=8*') { Falhar ('T1 mundo A dist_id (origens deterministicas): ' + $l1) }
if ($l2 -notlike 'T1_MUNDO_B*') { Falhar ('T1 linha mundo B ausente: ' + $l2) }
if ($l2 -notlike '*efetivos=`[vazio]*') { Falhar ('T1 mundo B modulos: ' + $l2) }
if ($l2 -notlike '*total=0*') { Falhar ('T1 mundo B total: ' + $l2) }
Log 'T1_DUMP_OK arquivo=r100a_t1_alertas_banco.txt linhas=2'

# ---------------------------------------------------------------- GERAR_IDEMPOTENTE (gatilho explicito)
$rGen = HttpRequest 'POST' ($apiUrl + '/api/calendario-fiscal/notificacoes/gerar?mes=10&ano=2026') $tokAdm $null
if ($rGen.Http -ne 200) { Falhar ('GERAR_IDEMPOTENTE http ' + $rGen.Http + ' ' + $(if ($rGen.Json) { [string]$rGen.Json.erro } else { 'sem corpo' })) }
$criadas = [int]$rGen.Json.notificacoesCriadas
$alertas = @($rGen.Json.alertas).Count
if ($criadas -ne 0) { Falhar ('GERAR_IDEMPOTENTE criadas=' + $criadas + ' esperado 0 (dedupe canonico)') }
if ($alertas -ne 5) { Falhar ('GERAR_IDEMPOTENTE alertas=' + $alertas + ' esperado 5') }
$totalG = ContarCaixaFiscal $aTenantId
if ($totalG -ne '8') { Falhar ('GERAR_IDEMPOTENTE total_mantido=' + $totalG + ' esperado 8') }
Log 'GERAR_IDEMPOTENTE_OK http=200 criadas=0 alertas=5 total_mantido=8'

# ---------------------------------------------------------------- CAIXA (a caixa in-app lista base + escalonado)
# Mesma caixa dos avisos de fatura/fechamento/escala (plantaopro.notificacoes): a app consome
# a lista paginada por usuario. Contamos os itens de tipo 'fiscal' para nao depender de eventuais
# avisos de outro fluxo criados no mesmo tenant.
$rCai = HttpRequest 'GET' ($apiUrl + '/api/mobile/notificacoes?page=1&pageSize=100') $tokAdm $null
if ($rCai.Http -ne 200) { Falhar ('CAIXA http ' + $rCai.Http + ' ' + $(if ($rCai.Json) { [string]$rCai.Json.message } else { 'sem corpo' })) }
$itens = @($rCai.Json.data.items)
$fiscais = @($itens | Where-Object { [string]$_.tipo -eq 'fiscal' })
$caiTxt = $rCai.Content
$mEsc = [bool]($caiTxt -like '*ESCALONADO*')
$mBase = [bool]($caiTxt -like '*Obriga*')
if ($fiscais.Count -ne 8) { Falhar ('CAIXA fiscais=' + $fiscais.Count + ' esperado 8') }
if (-not $mEsc) { Falhar 'CAIXA titulo ESCALONADO ausente na lista' }
if (-not $mBase) { Falhar 'CAIXA titulo base (Obriga...) ausente na lista' }
Log 'CAIXA_OK http=200 fiscais=8 escalonado=SIM base=SIM'

# ---------------------------------------------------------------- NOVEMBRO_AGENDADO (fora da janela -> zero novas)
$rNov = HttpRequest 'GET' ($apiUrl + '/api/calendario-fiscal/calendario?mes=11&ano=2026') $tokAdm $null
if ($rNov.Http -ne 200) { Falhar ('NOVEMBRO_AGENDADO http ' + $rNov.Http) }
$nAgend = @($rNov.Json.itens | Where-Object { $_.estado -eq 'AGENDADA' }).Count
if (@($rNov.Json.itens).Count -ne 5) { Falhar ('NOVEMBRO_AGENDADO itens esperado 5') }
if ($nAgend -ne 5) { Falhar ('NOVEMBRO_AGENDADO agendadas=' + $nAgend + ' esperado 5') }
$rGen2 = HttpRequest 'POST' ($apiUrl + '/api/calendario-fiscal/notificacoes/gerar?mes=11&ano=2026') $tokAdm $null
if ($rGen2.Http -ne 200) { Falhar ('NOVEMBRO_AGENDADO gerar http ' + $rGen2.Http) }
$criadas2 = [int]$rGen2.Json.notificacoesCriadas
$totalN = ContarCaixaFiscal $aTenantId
if ($criadas2 -ne 0) { Falhar ('NOVEMBRO_AGENDADO criadas=' + $criadas2 + ' esperado 0 (tudo AGENDADA)') }
if ($totalN -ne '8') { Falhar ('NOVEMBRO_AGENDADO total_mantido=' + $totalN + ' esperado 8') }
Log 'NOVEMBRO_AGENDADO_OK http=200 agendadas=5 criadas=0 total_mantido=8'

# ---------------------------------------------------------------- CONCLUIDA (sai dos alertas, nao duplica a caixa)
$rConc = HttpRequest 'POST' ($apiUrl + '/api/calendario-fiscal/alternar-conclusao') $tokAdm @{ codigo = 'OBRIGACAO_FISCAL_DAS_SIMPLES'; mesPeriodo = 10; anoPeriodo = 2026 }
if ($rConc.Http -ne 200) { Falhar ('CONCLUIDA http ' + $rConc.Http + ' ' + $(if ($rConc.Json) { [string]$rConc.Json.erro } else { 'sem corpo' })) }
$done = Psq ("select count(*) from plantaopro.adm_obrigacoes_fiscais where tenant_id='" + $aTenantId + "' and codigo_obrigacao='OBRIGACAO_FISCAL_DAS_SIMPLES' and mes_referencia=9 and ano_referencia=2026 and concluida_em is not null")
if ($done -ne '1') { Falhar ('CONCLUIDA concluida_em=' + $done + ' esperado 1 (referencia 09/2026)') }
$rGen3 = HttpRequest 'POST' ($apiUrl + '/api/calendario-fiscal/notificacoes/gerar?mes=10&ano=2026') $tokAdm $null
if ($rGen3.Http -ne 200) { Falhar ('CONCLUIDA gerar http ' + $rGen3.Http) }
$criadas3 = [int]$rGen3.Json.notificacoesCriadas
$alertas3 = @($rGen3.Json.alertas).Count
$totalC = ContarCaixaFiscal $aTenantId
if ($criadas3 -ne 0) { Falhar ('CONCLUIDA criadas=' + $criadas3 + ' esperado 0 (avisos nunca duplicam)') }
if ($alertas3 -ne 4) { Falhar ('CONCLUIDA alertas_apos=' + $alertas3 + ' esperado 4 (DAS concluida sai)') }
if ($totalC -ne '8') { Falhar ('CONCLUIDA total_mantido=' + $totalC + ' esperado 8') }
Log 'CONCLUIDA_OK http=200 alertas_apos=4 total_mantido=8'

# ---------------------------------------------------------------- CONTROLE (sem ADM360: SLA existe, caixa nao recebe)
$rGenB = HttpRequest 'POST' ($apiUrl + '/api/calendario-fiscal/notificacoes/gerar?mes=10&ano=2026') $tokCtrl $null
if ($rGenB.Http -ne 200) { Falhar ('CONTROLE gerar http ' + $rGenB.Http) }
$criadasB = [int]$rGenB.Json.notificacoesCriadas
$alertasB = @($rGenB.Json.alertas).Count
$totalB = ContarCaixaFiscal $bTenantId
if ($criadasB -ne 0) { Falhar ('CONTROLE criadas=' + $criadasB + ' esperado 0 (sem ADM360 nao ha destinatarios)') }
if ($alertasB -ne 5) { Falhar ('CONTROLE alertas=' + $alertasB + ' esperado 5 (visao de SLA independente do contrato)') }
if ($totalB -ne '0') { Falhar ('CONTROLE total=' + $totalB + ' esperado 0') }
Log 'CONTROLE_OK http=200 criadas=0 alertas=5 total=0'

# ---------------------------------------------------------------- WEB: sessao do dono do mundo A
$sessao = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$pg0 = Invoke-WebRequest -Uri ($webUrl + '/Account/Login') -WebSession $sessao -UseBasicParsing
$html0 = $pg0.Content
$tokPos = $html0.IndexOf('name="__RequestVerificationToken"')
if ($tokPos -lt 0) { Falhar 'WEB_LOGIN token antiforgery nao encontrado na pagina de login' }
$valPos = $html0.IndexOf('value="', $tokPos)
$tokStart = $valPos + 'value="'.Length
$tokEnd = $html0.IndexOf('"', $tokStart)
$antiTok = $html0.Substring($tokStart, $tokEnd - $tokStart)
$bodyLogin = 'Email=' + [System.Uri]::EscapeDataString('r100-drill-adm@plantaopro-local.test') + '&Senha=' + [System.Uri]::EscapeDataString('R100-adm-2026#Kp9z') + '&__RequestVerificationToken=' + [System.Uri]::EscapeDataString($antiTok)
$post = Invoke-WebRequest -Uri ($webUrl + '/Account/Login') -Method Post -WebSession $sessao -Body $bodyLogin -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -MaximumRedirection 10
if ([int]$post.StatusCode -ne 200) { Falhar ('WEB_LOGIN http ' + [int]$post.StatusCode) }
Log 'WEB_LOGIN_OK http=200'

function WebObter([string]$rota) {
  for ($w = 0; $w -lt 6; $w++) {
    try { $q = Invoke-WebRequest -Uri ($webUrl + $rota) -WebSession $script:sessao -UseBasicParsing -TimeoutSec 60 -MaximumRedirection 10; if ([int]$q.StatusCode -eq 200) { return $q } }
    catch { Start-Sleep 2 }
  }
  return $null
}

# /CalendarioFiscalWeb: cartao server-side "SLA e escalonamento" com os badges reais
$qCal = WebObter '/CalendarioFiscalWeb/Index'
if ($null -eq $qCal) { Falhar 'WEB_CALENDARIO http != 200 apos 6 tentativas' }
$hCal = $qCal.Content
$mCartao = [bool]($hCal -like '*SLA e escalonamento*')
$mEscCal = [bool]($hCal -like '*ESCALONADO*')
$mCrit   = [bool]($hCal -like '*CRITICO*')
function DumpCalendario([string]$h) { try { [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'r100a_debug_calendario.html'), $h, (New-Object System.Text.UTF8Encoding($false))) } catch {} }
if (-not $mCartao) { DumpCalendario $hCal; Falhar ('WEB_CALENDARIO cartao de SLA ausente (bytes=' + $hCal.Length + ')') }
if (-not $mEscCal) { DumpCalendario $hCal; Falhar 'WEB_CALENDARIO badge ESCALONADO ausente (3 pendencias >= limiar)' }
if (-not $mCrit) { DumpCalendario $hCal; Falhar 'WEB_CALENDARIO badge CRITICO ausente' }
Log 'WEB_CALENDARIO_OK http=200 cartao=SIM escalonado=SIM critico=SIM'

# /Notificacoes: a caixa abre para o dono
$qNt = WebObter '/Notificacoes/Index'
if ($null -eq $qNt) { Falhar 'WEB_NOTIFICACOES http != 200 apos 6 tentativas' }
Log 'WEB_NOTIFICACOES_OK http=200'

# ---------------------------------------------------------------- teardown
PararApps
Start-Sleep 2
foreach ($pp in @($porta, $webPorta)) {
  $resto = Get-NetTCPConnection -State Listen -LocalPort $pp -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($resto) { try { Stop-Process -Id ([int]$resto.OwningProcess) -Force -ErrorAction SilentlyContinue } catch {}; Start-Sleep 2 }
}
if (-not (PortaLivre $porta) -or (-not (PortaLivre $webPorta))) { Falhar 'portas ainda ocupadas no teardown' }
Log 'PORTAS_LIVRES=SIM'
if ($global:Falhou) { Falhar 'resultado final negativo' }
Log 'RESULTADO=PASSEI'
EscreverSaida
exit 0
