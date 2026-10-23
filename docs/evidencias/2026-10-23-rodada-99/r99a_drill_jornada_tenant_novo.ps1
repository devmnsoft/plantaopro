# R99 (jornadas guiadas para tenants novos: os modulos do plano com incluido=true viram
# contrato efetivo no provisionamento + UI guiada da jornada): drill ponta a ponta com
# API e Web no ar sobre o banco vivo.
# Fluxo deterministico:
#   ESTAGEM_1        API no ar + Web no ar (health/login)
#   PRE_LIMPEZA      purga por CNPJ dos dois mundos do drill (incluindo tenant_modulos
#                    e historico) e por codigo de plano (crash-idempotencia; rerun
#                    previsivel)
#   PA_PLANO         superadmin cria 2 planos pelo admin; o primeiro recebe o modulo
#                    PLANTOES com incluido=true pela rota de governanca
#                    /api/modulos/{id}/vincular-plano; o segundo nao tem vinculo (controle)
#   PA_MUNDOS        dois mundos TRIAL (5 dias) pelo funil self-service: mundo 1 usa o
#                    plano com o modulo e traz telefone de contato (a jornada avanca por
#                    dados persistidos); mundo 2 usa o plano sem modulos (controle)
#   FUNIL_STATUS_SEED status logo apos o provisionamento: EM_ANDAMENTO, progresso=0 e
#                    proxima da seed (a avaliacao e lazy, so reavalia na leitura da jornada)
#   FUNIL_CONTRATO   dump t1 no banco: mundo 1 com tenant_modulos PLANTOES (origem PLANO),
#                    historico CONTRATAR_PLANO, modulos_efetivos=[PLANTOES] e checklist de
#                    9 etapas (6 gerais + 3 PLANTOES, sem SAUDE360/ADM360); mundo 2 sem
#                    modulos e com checklist geral de 6 etapas
#   FUNIL_REAVALIACAO GET checklist reavalia contra os dados: mundo 1 com dados da empresa
#                    completos -> progresso=14 (1 de 7 obrigatorias) e proxima = convite
#                    de equipe; mundo 2 permanece 0 com proxima = dados da empresa
#   PROXIMA_ACAO     GET proxima-acao devolve o item com link nao vazio e titulo identico
#                    ao resumo persistido
#   WEB_*            sessao do dono: /Onboarding/Index (CTA "Iniciar agora" apontando para
#                    o link da proxima etapa, progresso por grupo, 9 itens, sem grupos de
#                    modulo nao contratado), alvo do CTA (/ConvitesEquipe),
#                    /MinhaAssinatura (faixa de implantacao 14% + botao de continuidade),
#                    /PrimeirosPassos (guard corrigido + handoff) e /Cadastro/Sucesso
#                    (orientacao do que acontece depois do cadastro)
# A limpeza no inicio garante rerun previsivel; os tenants permanecem ao final.
$ErrorActionPreference = 'Stop'

$repo     = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$db       = 'plantaopro_test'
$porta    = 52082
$webPorta = 52081
$apiUrl   = 'http://localhost:' + $porta
$webUrl   = 'http://localhost:' + $webPorta
$apiDll   = Join-Path $repo 'backend/PlantaoPro.Api/bin/Release/net10.0/PlantaoPro.Api.dll'
$webDll   = Join-Path $repo 'backend/PlantaoPro.Web/bin/Release/net10.0/PlantaoPro.Web.dll'
$psql     = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$env:PGPASSWORD = '123456'
$env:PGCLIENTENCODING = 'UTF8'
$saida = Join-Path $PSScriptRoot 'r99a_drill_saida.txt'
$t1arquivo = Join-Path $PSScriptRoot 'r99a_t1_contrato_banco.txt'
$cnpjTrial  = '99001001000144'
$cnpjSemMod = '99002002000155'

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
# Requisicao generica (GET/POST/PUT): devolve Http+Json e nunca lanca em 4xx/5xx.
function HttpRequest([string]$metodo, [string]$url, [string]$token, $payload) {
  try {
    $hdr = @{}
    if ($token) { $hdr['Authorization'] = 'Bearer ' + $token }
    if ($null -ne $payload) {
      $bodyJ = $payload | ConvertTo-Json -Depth 6
      $r = Invoke-WebRequest $url -Method $metodo -Body $bodyJ -ContentType 'application/json' -Headers $hdr -UseBasicParsing
      return [pscustomobject]@{ Http = [int]$r.StatusCode; Json = ($r.Content | ConvertFrom-Json) }
    } else {
      $r = Invoke-WebRequest $url -Method $metodo -Headers $hdr -UseBasicParsing
      return [pscustomobject]@{ Http = [int]$r.StatusCode; Json = ($r.Content | ConvertFrom-Json) }
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
    return [pscustomobject]@{ Http = $code; Json = $json }
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
      # R99: o provisionamento agora escreve tenant_modulos + historico; purgar antes do tenant.
      $sql += ('delete from plantaopro.tenant_modulos_historico where tenant_id=' + "'" + $p[1] + "';")
      $sql += ('delete from plantaopro.tenant_modulos where tenant_id=' + "'" + $p[1] + "';")
      $sql += ('delete from plantaopro.tenant_white_label where tenant_id=' + "'" + $p[1] + "';")
      $sql += ('delete from plantaopro.perfil_permissoes where perfil_id in (select id from plantaopro.perfis where tenant_id=' + "'" + $p[1] + "');")
      $sql += ('delete from plantaopro.perfis where tenant_id=' + "'" + $p[1] + "';")
      $sql += ('delete from plantaopro.tenants where id=' + "'" + $p[1] + "';")
    }
    if ($p[3]) {
      $sql += ('delete from plantaopro.usuarios_perfis where usuario_id=' + "'" + $p[3] + "';")
      $sql += ('delete from plantaopro.usuarios where id=' + "'" + $p[3] + "';")
    }
    if ($p[2]) { $sql += ('delete from plantaopro.assinaturas where id=' + "'" + $p[2] + "';") }
    if ($p[0]) { $sql += ('delete from plantaopro.clientes where id=' + "'" + $p[0] + "';") }
    Psq $sql | Out-Null
  }
  Psq ("delete from plantaopro.cadastro_cliente_solicitacoes where regexp_replace(cnpj,'\\D','','g')='" + $cnpj + "'") | Out-Null
}
# Purga as linhas de IA (config/usos/slots) dos tenants antes de derrubar os mundos.
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

Log ('R99A_DRILL data=' + (Get-Date -Format 'yyyyMMdd-HHmmss') + ' db=' + $db + ' api=' + $porta + ' web=' + $webPorta)

# ---------------------------------------------------------------- ESTAGEM 1: portas + apps
if (-not (Test-Path $apiDll)) { Falhar ('DLL da API ausente: ' + $apiDll) }
if (-not (Test-Path $webDll)) { Falhar ('DLL da Web ausente: ' + $webDll) }
if (-not (PortaLivre $porta) -or (-not (PortaLivre $webPorta))) { Falhar ('portas ocupadas antes do inicio (api=' + $porta + ' web=' + $webPorta + ')') }
Log 'PORTAS_LIVRES_INICIO=SIM'

$env:ConnectionStrings__Default = 'Host=127.0.0.1;Port=5432;Database=' + $db + ';Username=postgres;Password=123456'
$env:Jwt__Key = 'r99-drill-dev-jwt-key-0123456789-abcd'
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
LimparAiPorCnpjs @($cnpjTrial, $cnpjSemMod) | Out-Null
LimparMundoPorCnpj $cnpjTrial | Out-Null
LimparMundoPorCnpj $cnpjSemMod | Out-Null
LimparPlanoPorCodigo 'R99_PLANO_DRILL' | Out-Null
LimparPlanoPorCodigo 'R99_PLANO_SEM_MOD' | Out-Null
Log 'PRE_LIMPEZA_RERUN ok=SIM'

$tokAdm = FazerLogin 'superadmin@plantaopro.local' 'Super@123456'

# ---------------------------------------------------------------- PA_PLANO (governanca de modulos)
function PayloadPlano([string]$codigo, [string]$slug, [string]$nome, [int]$ordem) {
  return @{
    nome = $nome
    descricao = 'Plano do drill r99'
    valorMensal = 100.00
    limiteMedicos = 0; limiteHospitais = 0; limitePlantoesMes = 0
    permiteMobile = $false; permiteBi = $false; permiteRelatoriosAvancados = $false; permiteIntegracoes = $false
    codigo = $codigo
    slug = $slug
    valorAnual = 1250.00
    publico = $true; destaque = $false; ordem = $ordem; permiteWhiteLabel = $false
    permiteTrial = $true
    diasTrial = 5
  }
}
$rPlano1 = HttpRequest 'POST' ($apiUrl + '/api/planos') $tokAdm (PayloadPlano 'r99_plano_drill' 'r99-plano-drill' 'R99 Plano Drill' 952)
if ($rPlano1.Http -ne 200) { Falhar ('PA_PLANO plano1 http ' + $rPlano1.Http + ' ' + $(if ($rPlano1.Json) { $rPlano1.Json.message } else { 'sem corpo' })) }
$idPlano1 = [string]$rPlano1.Json.data

$rPlano2 = HttpRequest 'POST' ($apiUrl + '/api/planos') $tokAdm (PayloadPlano 'r99_plano_sem_mod' 'r99-plano-sem-mod' 'R99 Plano Sem Modulo' 953)
if ($rPlano2.Http -ne 200) { Falhar ('PA_PLANO plano2 http ' + $rPlano2.Http + ' ' + $(if ($rPlano2.Json) { $rPlano2.Json.message } else { 'sem corpo' })) }
$idPlano2 = [string]$rPlano2.Json.data

$rMod = HttpRequest 'GET' ($apiUrl + '/api/modulos') $tokAdm $null
if ($rMod.Http -ne 200) { Falhar ('PA_PLANO listagem de modulos http ' + $rMod.Http) }
$idPlantoes = $null
foreach ($m in @($rMod.Json.data)) { if ([string]$m.codigo -eq 'PLANTOES') { $idPlantoes = [string]$m.id } }
if (-not $idPlantoes) { Falhar 'PA_PLANO modulo PLANTOES nao encontrado no catalogo' }

$rVinc = HttpRequest 'POST' ($apiUrl + '/api/modulos/' + $idPlantoes + '/vincular-plano') $tokAdm @{ planoId = $idPlano1; incluido = $true }
if ($rVinc.Http -ne 200) { Falhar ('PA_PLANO vincular PLANTOES http ' + $rVinc.Http + ' ' + $(if ($rVinc.Json) { $rVinc.Json.message } else { 'sem corpo' })) }

$nVinc1 = Psq ("select count(*) from plantaopro.plano_modulos pm join plantaopro.modulos_sistema ms on ms.id=pm.modulo_id where pm.plano_id='" + $idPlano1 + "' and pm.reg_status='A' and pm.incluido=true and upper(ms.codigo)='PLANTOES'")
$nVinc2 = Psq ("select count(*) from plantaopro.plano_modulos where plano_id='" + $idPlano2 + "' and reg_status='A'")
if ($nVinc1 -ne '1') { Falhar ('PA_PLANO vinculos_plano1=' + $nVinc1 + ' esperado 1') }
if ($nVinc2 -ne '0') { Falhar ('PA_PLANO vinculos_plano2=' + $nVinc2 + ' esperado 0') }
Log ('PA_PLANO_OK plano1=' + $idPlano1 + ' vinculos_plano1=1 plano2=' + $idPlano2 + ' vinculos_plano2=0')

# ---------------------------------------------------------------- PA_MUNDOS (funil self-service, TRIAL 5d)
$rT = HttpRequest 'POST' ($apiUrl + '/api/public/cadastro/finalizar') '' @{
  empresa      = @{ nomeFantasia = 'R99 Drill Trial'; razaoSocial = 'R99 Drill Trial LTDA'; cnpj = $cnpjTrial; segmento = 'Clinica'; cidade = 'Recife'; uf = 'PE'; telefone = '(81) 99999-0001'; emailCorporativo = 'r99-drill-trial@plantaopro-local.test' }
  plano        = @{ planoId = $idPlano1; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
  usuarioAdmin = @{ nome = 'R99 Trial Admin'; email = 'r99-drill-trial@plantaopro-local.test'; cargo = 'Gestora'; senha = 'R99-trial-2026#Kp9z' }
  statusAssinatura = 'TRIAL'
  diasTrial      = 5
}
if ($rT.Http -ne 200) { Falhar ('PA_MUNDOS trial http ' + $rT.Http + ' ' + $(if ($rT.Json) { $rT.Json.message } else { 'sem corpo' })) }
$tTenantId = [string]$rT.Json.data.tenantId
$tStatus = Psq ("select coalesce(status,'') from plantaopro.assinaturas where id='" + $rT.Json.data.assinaturaId + "'")
if ($tStatus -ne 'TRIAL') { Falhar ('PA_MUNDOS trial status=' + $tStatus) }

$rS = HttpRequest 'POST' ($apiUrl + '/api/public/cadastro/finalizar') '' @{
  empresa      = @{ nomeFantasia = 'R99 Drill SemMod'; razaoSocial = 'R99 Drill SemMod LTDA'; cnpj = $cnpjSemMod; segmento = 'Clinica'; cidade = 'Olinda'; uf = 'PE'; emailCorporativo = 'r99-drill-semmod@plantaopro-local.test' }
  plano        = @{ planoId = $idPlano2; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
  usuarioAdmin = @{ nome = 'R99 SemMod Admin'; email = 'r99-drill-semmod@plantaopro-local.test'; cargo = 'Gestor'; senha = 'R99-semmod-2026#Kp9z' }
  statusAssinatura = 'TRIAL'
  diasTrial      = 5
}
if ($rS.Http -ne 200) { Falhar ('PA_MUNDOS semmod http ' + $rS.Http + ' ' + $(if ($rS.Json) { $rS.Json.message } else { 'sem corpo' })) }
$sTenantId = [string]$rS.Json.data.tenantId
$sStatus = Psq ("select coalesce(status,'') from plantaopro.assinaturas where id='" + $rS.Json.data.assinaturaId + "'")
if ($sStatus -ne 'TRIAL') { Falhar ('PA_MUNDOS semmod status=' + $sStatus) }
Log ('PA_MUNDOS_OK trial_tenant=' + $tTenantId + ' trial_status=TRIAL semmod_tenant=' + $sTenantId + ' semmod_status=TRIAL')

$tokTrial = FazerLogin 'r99-drill-trial@plantaopro-local.test' 'R99-trial-2026#Kp9z'
$tokSemMod = FazerLogin 'r99-drill-semmod@plantaopro-local.test' 'R99-semmod-2026#Kp9z'

# ---------------------------------------------------------------- FUNIL_STATUS_SEED (leitura pura, sem reavaliacao ainda)
$rSeed = HttpRequest 'GET' ($apiUrl + '/api/onboarding/status') $tokTrial $null
$seedStatus = ''; $seedProg = ''; $seedProx = ''
if ($rSeed.Json) { $seedStatus = [string]$rSeed.Json.data.status; $seedProg = [string]$rSeed.Json.data.progresso; $seedProx = [string]$rSeed.Json.data.proximaAcao }
if ($rSeed.Http -ne 200) { Falhar ('FUNIL_STATUS_SEED http ' + $rSeed.Http) }
if ($seedStatus -ne 'EM_ANDAMENTO') { Falhar ('FUNIL_STATUS_SEED status=' + $seedStatus) }
if ($seedProg -ne '0') { Falhar ('FUNIL_STATUS_SEED progresso=' + $seedProg + ' esperado 0') }
if ($seedProx -notlike '*empresa*') { Falhar ('FUNIL_STATUS_SEED proxima=' + $seedProx) }
Log ('FUNIL_STATUS_SEED http=200 status=EM_ANDAMENTO progresso=0 proxima_seed=SIM')

# ---------------------------------------------------------------- FUNIL_CONTRATO (dump t1 antes de qualquer reavaliacao)
$tplT1 = @'
select 'T1_{TAG} tenant='||t.id||
 ' modulos=['||coalesce((select string_agg(upper(ms.codigo),',' order by ms.codigo) from plantaopro.tenant_modulos tm join plantaopro.modulos_sistema ms on ms.id=tm.modulo_id where tm.tenant_id=t.id and tm.reg_status='A'),'')||']'||
 ' origens=['||coalesce((select origem from plantaopro.tenant_modulos where tenant_id=t.id and reg_status='A' limit 1),'')||']'||
 ' efetivos=['||coalesce((select string_agg(upper(codigo),',' order by 1) from plantaopro.modulos_efetivos(t.id)),'vazio')||']'||
 ' historico='||(select count(*) from plantaopro.tenant_modulos_historico h join plantaopro.tenant_modulos tm on tm.id=h.tenant_modulo_id where tm.tenant_id=t.id and h.acao='CONTRATAR_PLANO')||
 ' checklist_total='||(select count(*) from plantaopro.tenant_onboarding_checklist where tenant_id=t.id)||
 ' codigos='||(select coalesce(string_agg(codigo,',' order by ordem),'') from plantaopro.tenant_onboarding_checklist where tenant_id=t.id)||
 ' onb='||(select coalesce(status,'')||'_'||progresso::text from plantaopro.tenant_onboarding where tenant_id=t.id)||
 ' seed_empresa='||(case when coalesce((select proxima_acao from plantaopro.tenant_onboarding where tenant_id=t.id),'') like '%empresa%' then 'SIM' else 'NAO' end)
 from plantaopro.tenants t where t.id='{TENANT}'
'@
$sqlT1a = $tplT1.Replace('{TAG}', 'MUNDO1').Replace('{TENANT}', $tTenantId)
$sqlT1b = $tplT1.Replace('{TAG}', 'MUNDO2').Replace('{TENANT}', $sTenantId)
$l1 = Psq $sqlT1a
$l2 = Psq $sqlT1b
Set-Content -Path $t1arquivo -Value @($l1, $l2) -Encoding UTF8

$codigosEsperados1 = 'ONB_EMPRESA_DADOS,ONB_EQUIPE_CONVIDAR,ONB_EQUIPE_ACESSO,ONB_IDENTIDADE,ONB_PREFERENCIAS,ONB_PL_CRIAR,ONB_PL_PUBLICAR,ONB_PL_ESCALA,ONB_REVISAO'
$codigosEsperados2 = 'ONB_EMPRESA_DADOS,ONB_EQUIPE_CONVIDAR,ONB_EQUIPE_ACESSO,ONB_IDENTIDADE,ONB_PREFERENCIAS,ONB_REVISAO'
if ($l1 -notlike 'T1_MUNDO1*') { Falhar ('FUNIL_CONTRATO linha mundo1 ausente: ' + $l1) }
if ($l1 -notlike '*modulos=`[PLANTOES]*') { Falhar ('FUNIL_CONTRATO mundo1 modulos: ' + $l1) }
if ($l1 -notlike '*origens=`[PLANO]*') { Falhar ('FUNIL_CONTRATO mundo1 origens: ' + $l1) }
if ($l1 -notlike '*efetivos=`[PLANTOES]*') { Falhar ('FUNIL_CONTRATO mundo1 efetivos: ' + $l1) }
if ($l1 -notlike '*historico=1*') { Falhar ('FUNIL_CONTRATO mundo1 historico: ' + $l1) }
if ($l1 -notlike '*checklist_total=9*') { Falhar ('FUNIL_CONTRATO mundo1 checklist_total: ' + $l1) }
if ($l1 -notlike ('*codigos=' + $codigosEsperados1 + '*')) { Falhar ('FUNIL_CONTRATO mundo1 codigos: ' + $l1) }
if ($l1 -notlike '*onb=EM_ANDAMENTO_0*') { Falhar ('FUNIL_CONTRATO mundo1 onb: ' + $l1) }
if ($l1 -notlike '*seed_empresa=SIM*') { Falhar ('FUNIL_CONTRATO mundo1 seed: ' + $l1) }
if ($l2 -notlike 'T1_MUNDO2*') { Falhar ('FUNIL_CONTRATO linha mundo2 ausente: ' + $l2) }
if ($l2 -notlike '*modulos=`[]*') { Falhar ('FUNIL_CONTRATO mundo2 modulos: ' + $l2) }
if ($l2 -notlike '*efetivos=`[vazio]*') { Falhar ('FUNIL_CONTRATO mundo2 efetivos: ' + $l2) }
if ($l2 -notlike '*historico=0*') { Falhar ('FUNIL_CONTRATO mundo2 historico: ' + $l2) }
if ($l2 -notlike '*checklist_total=6*') { Falhar ('FUNIL_CONTRATO mundo2 checklist_total: ' + $l2) }
if ($l2 -notlike ('*codigos=' + $codigosEsperados2 + '*')) { Falhar ('FUNIL_CONTRATO mundo2 codigos: ' + $l2) }
if ($l2 -notlike '*onb=EM_ANDAMENTO_0*') { Falhar ('FUNIL_CONTRATO mundo2 onb: ' + $l2) }
Log ('FUNIL_CONTRATO_OK mundo1=9_moderulos_mundo2=6_geral onb=EM_ANDAMENTO_0 seed=SIM')
Log ('T1_DUMP_OK arquivo=r99a_t1_contrato_banco.txt')

# ---------------------------------------------------------------- FUNIL_REAVALIACAO (GET checklist reavalia contra os dados)
$rCl = HttpRequest 'GET' ($apiUrl + '/api/onboarding/checklist') $tokTrial $null
if ($rCl.Http -ne 200) { Falhar ('FUNIL_REAVALIACAO checklist http ' + $rCl.Http) }
$nEtapas = @($rCl.Json.data).Count
$rProg = HttpRequest 'GET' ($apiUrl + '/api/onboarding/status') $tokTrial $null
$progValor = ''; $progProx = ''
if ($rProg.Json) { $progValor = [string]$rProg.Json.data.progresso; $progProx = [string]$rProg.Json.data.proximaAcao }
if ($nEtapas -ne 9) { Falhar ('FUNIL_REAVALIACAO etapas=' + $nEtapas + ' esperado 9') }
if ($progValor -ne '14') { Falhar ('FUNIL_REAVALIACAO progresso=' + $progValor + ' esperado 14 (1 de 7 obrigatorias)') }
if ($progProx -notlike '*Convidar o primeiro integrante*') { Falhar ('FUNIL_REAVALIACAO proxima=' + $progProx) }
Log ('FUNIL_REAVALIACAO_OK http=200 etapas=9 progresso=14 proxima_convite=SIM')

# controle: mesmo funil, plano sem modulos -> jornada geral imovel, proxima segue a empresa
$rCl2 = HttpRequest 'GET' ($apiUrl + '/api/onboarding/checklist') $tokSemMod $null
if ($rCl2.Http -ne 200) { Falhar ('FUNIL_CONTROLE checklist http ' + $rCl2.Http) }
$nEtapas2 = @($rCl2.Json.data).Count
$rPr2 = HttpRequest 'GET' ($apiUrl + '/api/onboarding/status') $tokSemMod $null
$pr2 = ''; $pr2p = ''
if ($rPr2.Json) { $pr2 = [string]$rPr2.Json.data.progresso; $pr2p = [string]$rPr2.Json.data.proximaAcao }
if ($nEtapas2 -ne 6) { Falhar ('FUNIL_CONTROLE etapas=' + $nEtapas2 + ' esperado 6') }
if ($pr2 -ne '0') { Falhar ('FUNIL_CONTROLE progresso=' + $pr2 + ' esperado 0 (telefone ausente de proposito)') }
if ($pr2p -notlike '*Completar os dados da empresa*') { Falhar ('FUNIL_CONTROLE proxima=' + $pr2p) }
Log ('FUNIL_CONTROLE_OK etapas=6 progresso=0 proxima_empresa=SIM')

# ---------------------------------------------------------------- PROXIMA_ACAO (item com link, titulo e estado coerentes)
$rPx = HttpRequest 'GET' ($apiUrl + '/api/onboarding/proxima-acao') $tokTrial $null
$pxTitulo = ''; $pxLink = ''; $pxStatus = ''
if ($rPx.Json) { $pxTitulo = [string]$rPx.Json.data.titulo; $pxLink = [string]$rPx.Json.data.linkAcao; $pxStatus = [string]$rPx.Json.data.status }
if ($rPx.Http -ne 200) { Falhar ('PROXIMA_ACAO http ' + $rPx.Http) }
if ($pxTitulo -notlike '*Convidar o primeiro integrante*') { Falhar ('PROXIMA_ACAO titulo=' + $pxTitulo) }
if ($pxLink -ne '/ConvitesEquipe/Index') { Falhar ('PROXIMA_ACAO link=' + $pxLink) }
if ($pxStatus -ne 'PENDENTE') { Falhar ('PROXIMA_ACAO status=' + $pxStatus) }
Log ('PROXIMA_ACAO_OK http=200 titulo_convite=SIM link=/ConvitesEquipe/Index status=PENDENTE')

# ---------------------------------------------------------------- WEB: sessao do dono do mundo 1
$sessao = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$pg0 = Invoke-WebRequest -Uri ($webUrl + '/Account/Login') -WebSession $sessao -UseBasicParsing
$html0 = $pg0.Content
$tokPos = $html0.IndexOf('name="__RequestVerificationToken"')
if ($tokPos -lt 0) { Falhar 'WEB_LOGIN token antiforgery nao encontrado na pagina de login' }
$valPos = $html0.IndexOf('value="', $tokPos)
$tokStart = $valPos + 'value="'.Length
$tokEnd = $html0.IndexOf('"', $tokStart)
$antiTok = $html0.Substring($tokStart, $tokEnd - $tokStart)
$bodyLogin = 'Email=' + [System.Uri]::EscapeDataString('r99-drill-trial@plantaopro-local.test') + '&Senha=' + [System.Uri]::EscapeDataString('R99-trial-2026#Kp9z') + '&__RequestVerificationToken=' + [System.Uri]::EscapeDataString($antiTok)
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

# /Onboarding/Index: CTA do hero + progresso por grupo + somente os modulos contratados
$qOnb = WebObter '/Onboarding/Index'
if ($null -eq $qOnb) { Falhar 'WEB_ONBOARDING http != 200 apos 6 tentativas' }
$hOnb = $qOnb.Content
$nItens = ([regex]::Matches($hOnb, '<li class="pp-implementation-item')).Count
$ctaOk = $hOnb -like '*class="btn btn-primary" href="/ConvitesEquipe/Index"*'
$ctaTexto = $hOnb -like '*Iniciar agora*'
$proxOk = $hOnb -like '*Convidar o primeiro integrante da equipe*'
$ringOk = $hOnb -like '*<strong>14%</strong>*'
$grupoGeral = $hOnb -like '*1 de 4 concl*'
$grupoPlantoes = $hOnb -like '*0 de 3 concl*'
$semSaude = $hOnb -notlike '*Cadastrar a unidade de atendimento*' -and $hOnb -notlike '*Cadastrar o primeiro paciente*'
$semAdm = $hOnb -notlike '*operacao administrativa*'
if ($nItens -ne 9) { Falhar ('WEB_ONBOARDING itens=' + $nItens + ' esperado 9') }
if (-not $ctaOk) { Falhar 'WEB_ONBOARDING CTA com link da proxima etapa ausente' }
if (-not $ctaTexto) { Falhar 'WEB_ONBOARDING rotulo do CTA (Iniciar agora) ausente' }
if (-not $proxOk) { Falhar 'WEB_ONBOARDING proximo passo no resumo ausente' }
if (-not $ringOk) { Falhar 'WEB_ONBOARDING anel de progresso 14% ausente' }
if (-not $grupoGeral) { Falhar 'WEB_ONBOARDING progresso do grupo geral (1 de 4) ausente' }
if (-not $grupoPlantoes) { Falhar 'WEB_ONBOARDING progresso do grupo plantoes (0 de 3) ausente' }
if (-not $semSaude) { Falhar 'WEB_ONBOARDING etapa SAUDE360 apareceu sem contrato' }
if (-not $semAdm) { Falhar 'WEB_ONBOARDING etapa ADM360 apareceu sem contrato' }
Log 'WEB_ONBOARDING_OK http=200 itens=9 cta=SIM cta_link=SIM proxima_convite=SIM progresso=14 grupo_geral=1de4 grupo_plantoes=0de3 sem_saudes360=SIM sem_adm360=SIM'

# alvo do CTA: a tela da proxima etapa abre para o dono
$qCta = WebObter '/ConvitesEquipe/Index'
if ($null -eq $qCta) { Falhar 'WEB_LINK_PROXIMA http != 200 apos 6 tentativas' }
Log 'WEB_LINK_PROXIMA_OK http=200'

# /MinhaAssinatura: faixa de implantacao (melhor esforco) com o percentual real
$qAs = WebObter '/MinhaAssinatura/Index'
if ($null -eq $qAs) { Falhar 'WEB_ASSINATURA http != 200 apos 6 tentativas' }
$hAs = $qAs.Content
$mSua = [bool]($hAs -like '*Sua implanta*')
# GOTCHA PS 5.1: -like ancora no fim da string; sem o '*' de fechamento so casava se o
# HTML terminasse em "14%" (reproduzido no r99a_probe: 'x 14% y' -like '*14%' = False).
$mPct = [bool]($hAs -like '*14%*')
$mPr  = [bool]($hAs -like '*pronta*')
$faixaOk = $mSua -and $mPct -and $mPr
$botaoOk = [bool]($hAs -like '*Continuar implanta*')
function DumpAssinatura([string]$h) { try { [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'r99a_debug_minhaassinatura.html'), $h, (New-Object System.Text.UTF8Encoding($false))) } catch {} }
if (-not $faixaOk) { DumpAssinatura $hAs; Falhar ('WEB_ASSINATURA faixa ausente (sua=' + $mSua + ' pct14=' + $mPct + ' pronta=' + $mPr + ' bytes=' + $hAs.Length + ' eh_login=' + [bool]($hAs -like '*__RequestVerificationToken*') + ')') }
if (-not $botaoOk) { DumpAssinatura $hAs; Falhar 'WEB_ASSINATURA botao de continuidade ausente' }
Log 'WEB_ASSINATURA_OK http=200 faixa=SIM percentual=14 botao=SIM'

# /PrimeirosPassos: guard corrigido (AJEDA comum) + handoff para a jornada
$qPp = WebObter '/PrimeirosPassos/Index'
if ($null -eq $qPp) { Falhar 'WEB_PRIMEIROS_PASSOS http != 200 apos 6 tentativas' }
$hPp = $qPp.Content
if ($hPp -notlike '*Primeiros Passos*') { Falhar 'WEB_PRIMEIROS_PASSOS titulo da pagina ausente' }
if ($hPp -notlike '*Ver minha implanta*') { Falhar 'WEB_PRIMEIROS_PASSOS handoff para a jornada ausente' }
Log 'WEB_PRIMEIROS_PASSOS_OK http=200 handoff=SIM'

# /Cadastro/Sucesso: orientacao do que acontece depois do cadastro (pagina publica; acessada
# com a sessao autenticada para cobrir tanto o [AllowAnonymous] quanto um eventual redirect)
try {
  $qSu = Invoke-WebRequest -Uri ($webUrl + '/Cadastro/Sucesso') -WebSession $sessao -UseBasicParsing -TimeoutSec 30
  $hSu = $qSu.Content
  $okSu = [int]$qSu.StatusCode -eq 200 -and ($hSu -like '*jornada guiada*')
  if (-not $okSu) { Falhar 'WEB_SUCESSO orientacao da jornada ausente' }
  Log 'WEB_SUCESSO_OK http=200 orientacao=SIM'
} catch {
  Falhar ('WEB_SUCESSO http falhou: ' + $_.Exception.Message)
}

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
