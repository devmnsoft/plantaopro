# R101 - relatorios com benchmark ("seus indicadores vs o segmento" por plano)
# Execucao canonica: sobe API :52086 + Web :52085 (DLLs Release, banco vivo plantaopro_test)
# e prova o contrato da rodada de ponta a ponta:
#   PA_*             um plano de drill (sem vinculos) + dois mundos TRIAL pelo funil
#                    self-service (A = ator, C = controle); RELATORIOS habilitado no mundo
#                    A pela rota real habilitar-tenant (governanca, origem CONTRATO, acao
#                    HABILITAR) + RELATORIOS.VER ao dono A via usuario_permissoes_especiais
#                    (o guard web decide por grants emitidos no login; o perfil
#                    ADMINISTRADOR_CLIENTE ainda nao tem grants no modulo RELATORIOS)
#   PEERS_FIXTURE    eventos de negocio deterministicos em 09/2026 para A/C/P1/P2
#                    (medicos, plantoes, escalas, pagamentos) com numeros fechados
#   T1               GET api/relatorios/benchmark?mes=2026-09 como dono A: seus 5
#                    indicadores vs mediana/media/percentil/pares do plano (dump salvo)
#   BENCHMARK_VAZIO  mes sem atividade nenhuma -> amostra insuficiente, tudo nulo
#   WEB_*            sessao do dono A: hub /Relatorios/Index com o cartao novo e a pagina
#                    /Relatorios/Benchmark server-side com os valores reais
#   CONTROLE_WEB     mundo C (sem RELATORIOS contratado): AccessDenied com motivo
#                    MODULO_NAO_CONTRATADO
# A limpeza no inicio garante rerun previsivel; os tenants permanecem ao final.
$ErrorActionPreference = 'Stop'

$repo     = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$db       = 'plantaopro_test'
$porta    = 52086
$webPorta = 52085
$apiUrl   = 'http://localhost:' + $porta
$webUrl   = 'http://localhost:' + $webPorta
$apiDll   = Join-Path $repo 'backend/PlantaoPro.Api/bin/Release/net10.0/PlantaoPro.Api.dll'
$webDll   = Join-Path $repo 'backend/PlantaoPro.Web/bin/Release/net10.0/PlantaoPro.Web.dll'
$psql     = "C:\Program Files\PostgreSQL\18\bin\psql.exe"

$env:PGPASSWORD = '123456'
$env:PGCLIENTENCODING = 'UTF8'
$saida = Join-Path $PSScriptRoot 'r101a_drill_saida.txt'
$t1arquivo = Join-Path $PSScriptRoot 'r101a_t1_benchmark_api.json'
$cnpjAdm  = '99005005000188'
$cnpjCtrl = '99006006000199'
$cnpjP1   = '99007007000211'
$cnpjP2   = '99008008000234'

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
# Requisicao generica (GET/POST): devolve Http+Json+Content e nunca lanca em 4xx/5xx.
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
function KpiObter($json, [string]$codigo) {
  foreach ($i in @($json.data.indicadores)) { if ([string]$i.codigo -eq $codigo) { return $i } }
  return $null
}
function NumProximo([double]$a, [double]$b) { return [math]::Abs($a - $b) -lt 0.01 }

# ------------------------------------------------- fixtures/clean por CNPJ (R101: negocio + mundo)
function LimparNegocioPorCnpj([string]$cnpj) {
  $sql = "delete from plantaopro.pagamentos where plantao_id in (select id from plantaopro.plantoes where cliente_id=(select id from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')='" + $cnpj + "'));"
  $sql += "delete from plantaopro.escalas where plantao_id in (select id from plantaopro.plantoes where cliente_id=(select id from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')='" + $cnpj + "'));"
  $sql += "delete from plantaopro.plantoes where cliente_id=(select id from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')='" + $cnpj + "');"
  $sql += "delete from plantaopro.medicos where cliente_id=(select id from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')='" + $cnpj + "');"
  $err = Psq $sql
  if ($err -match 'ERRO') { Falhar ('LimparNegocioPorCnpj ' + $cnpj + ': ' + $err) }
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
      $sql += ('delete from plantaopro.adm_obrigacoes_fiscais where tenant_id=' + "'" + $p[1] + "';")
      $sql += ('delete from plantaopro.tenant_modulos_historico where tenant_id=' + "'" + $p[1] + "';")
      $sql += ('delete from plantaopro.tenant_modulos where tenant_id=' + "'" + $p[1] + "';")
      $sql += ('delete from plantaopro.tenant_white_label where tenant_id=' + "'" + $p[1] + "';")
      $sql += ('delete from plantaopro.perfil_permissoes where perfil_id in (select id from plantaopro.perfis where tenant_id=' + "'" + $p[1] + "');")
      $sql += ('delete from plantaopro.perfis where tenant_id=' + "'" + $p[1] + "';")
      $sql += ('delete from plantaopro.tenants where id=' + "'" + $p[1] + "';")
    }
    if ($p[3]) {
      $sql += ('delete from plantaopro.notificacoes where usuario_id=' + "'" + $p[3] + "';")
      $sql += ('delete from plantaopro.usuarios_perfis where usuario_id=' + "'" + $p[3] + "';")
      $sql += ('delete from plantaopro.usuarios where id=' + "'" + $p[3] + "';")
    }
    if ($p[2]) { $sql += ('delete from plantaopro.assinaturas where id=' + "'" + $p[2] + "';") }
    if ($p[0]) { $sql += ('delete from plantaopro.clientes where id=' + "'" + $p[0] + "';") }
    $err = Psq $sql
    if ($err -match 'ERRO') { Falhar ('LimparMundoPorCnpj ' + $cnpj + ': ' + $err) }
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

# Templates de fixture (aqui-string sem interpolar + .Replace, padrao R100)
$tplEstrutura = @'
insert into plantaopro.clientes(id,nome,status,cnpj,reg_status,criado_em,reg_date)
values(gen_random_uuid(),'{NOME}','ATIVO','{CNPJ}','A',now(),now());
insert into plantaopro.assinaturas(cliente_id,plano_id,status,data_inicio,valor_mensal,periodicidade,reg_status,reg_date)
select id,'{PLANO}','ATIVA',date '2026-09-01',100.00,'MENSAL','A',now() from plantaopro.clientes where cnpj='{CNPJ}';
insert into plantaopro.medicos(nome,cliente_id,status,reg_status)
select '{NOME} Medico '||n, c.id, 'ATIVO','A' from generate_series(1,{QMED}) n
cross join (select id from plantaopro.clientes where cnpj='{CNPJ}') c;
'@
$tplPlantoes = @'
insert into plantaopro.plantoes(cliente_id,vagas,vagas_disponiveis,status,data_inicio,reg_date,reg_status)
select c.id,{VAGAS},{DISP},'{STATUS}',('{INICIO}'::timestamptz) + ((g.n||' day')::interval),'{REGDATE}'::timestamptz,'A'
from generate_series(0,{N}) as g(n) cross join (select id from plantaopro.clientes where cnpj='{CNPJ}') c;
'@
$tplEscalas = @'
with pcl as (select array_agg(id order by data_inicio) ids from plantaopro.plantoes where cliente_id=(select id from plantaopro.clientes where cnpj='{CNPJ}')),
mcl as (select id medico_id from plantaopro.medicos where cliente_id=(select id from plantaopro.clientes where cnpj='{CNPJ}') order by id limit 1)insert into plantaopro.escalas(cliente_id,medico_id,plantao_id,status,reg_date,reg_status)
select (select id from plantaopro.clientes where regexp_replace(cnpj,'\D', '', 'g')='{CNPJ}'), m.medico_id, pc.ids[(g.n % array_length(pc.ids,1))+1],'{STATUS}','{REGESCALA}'::timestamptz,'A'
from generate_series(0,{LIMITE}-1) as g(n) cross join pcl pc cross join mcl m
where {COND};
'@
$tplPagamentos = @'
insert into plantaopro.pagamentos(cliente_id,escala_id,medico_id,plantao_id,valor_pago,valor_previsto,status,data_pagamento,reg_status)
select e.cid, e.eid, e.mid, e.pid, {VALOR}, {VALOR}, 'pago', date '{DATA}', 'A'
from (select e.id eid, e.medico_id mid, e.plantao_id pid, p.cliente_id cid, row_number() over (order by e.reg_date, e.id) rn
      from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id
      where p.cliente_id=(select id from plantaopro.clientes where cnpj='{CNPJ}')) e
where e.rn <= {N};
'@
function ExecutarTemplate([string]$tpl, [hashtable]$vars) {
  $sql = $tpl
  foreach ($k in $vars.Keys) { $sql = $sql.Replace('{' + $k + '}', [string]$vars[$k]) }
  $err = Psq $sql
  if ($err -match 'ERRO') { Falhar ('ExecutarTemplate falhou: ' + $err + ' --- SQL --- ' + $sql) }
}

Log ('R101A_DRILL data=' + (Get-Date -Format 'yyyyMMdd-HHmmss') + ' db=' + $db + ' api=' + $porta + ' web=' + $webPorta)

# ---------------------------------------------------------------- ESTAGEM 1: portas + apps
if (-not (Test-Path $apiDll)) { Falhar ('DLL da API ausente: ' + $apiDll) }
if (-not (Test-Path $webDll)) { Falhar ('DLL da Web ausente: ' + $webDll) }
if (-not (PortaLivre $porta) -or (-not (PortaLivre $webPorta))) { Falhar ('portas ocupadas antes do inicio (api=' + $porta + ' web=' + $webPorta + ')') }
Log 'PORTAS_LIVRES_INICIO=SIM'

$env:ConnectionStrings__Default = 'Host=127.0.0.1;Port=5432;Database=' + $db + ';Username=postgres;Password=123456'
$env:Jwt__Key = 'r101-drill-dev-jwt-key-0123456789-abcd'
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
Psq ("delete from plantaopro.usuario_permissoes_especiais where usuario_id in (select id from plantaopro.usuarios where email in ('r101-drill-adm@plantaopro-local.test','r101-drill-ctrl@plantaopro-local.test'))") | Out-Null
foreach ($cj in @($cnpjAdm, $cnpjCtrl, $cnpjP1, $cnpjP2)) { LimparAiPorCnpjs @($cj) | Out-Null; LimparNegocioPorCnpj $cj | Out-Null; LimparMundoPorCnpj $cj | Out-Null }
LimparPlanoPorCodigo 'R101_PLANO_DRILL' | Out-Null
Log 'PRE_LIMPEZA_RERUN ok=SIM'

$tokSup = FazerLogin 'superadmin@plantaopro.local' 'Super@123456'

# ---------------------------------------------------------------- PA_PLANO (governanca, sem vinculos)
$rPlano = HttpRequest 'POST' ($apiUrl + '/api/planos') $tokSup @{
  nome = 'R101 Plano Drill'
  descricao = 'Plano do drill r101 (sem vinculos; RELATORIOS entra por governanca por tenant)'
  valorMensal = 100.00
  limiteMedicos = 0; limiteHospitais = 0; limitePlantoesMes = 0
  permiteMobile = $false; permiteBi = $false; permiteRelatoriosAvancados = $false; permiteIntegracoes = $false
  codigo = 'r101_plano_drill'
  slug = 'r101-plano-drill'
  valorAnual = 1250.00
  publico = $true; destaque = $false; ordem = 958; permiteWhiteLabel = $false
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
  empresa      = @{ nomeFantasia = 'R101 Drill Adm'; razaoSocial = 'R101 Drill Adm LTDA'; cnpj = $cnpjAdm; segmento = 'Clinica'; cidade = 'Recife'; uf = 'PE'; telefone = '(81) 99999-0020'; emailCorporativo = 'r101-drill-adm@plantaopro-local.test' }
  plano        = @{ planoId = $idPlano; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
  usuarioAdmin = @{ nome = 'R101 Adm Admin'; email = 'r101-drill-adm@plantaopro-local.test'; cargo = 'Gestora'; senha = 'R101-adm-2026#Kp9z' }
  statusAssinatura = 'TRIAL'
  diasTrial      = 5
}
if ($rA.Http -ne 200) { Falhar ('PA_MUNDOS adm http ' + $rA.Http + ' ' + $(if ($rA.Json) { $rA.Json.message } else { 'sem corpo' })) }
$aTenantId = [string]$rA.Json.data.tenantId
$aStatus = Psq ("select coalesce(status,'') from plantaopro.assinaturas where id='" + $rA.Json.data.assinaturaId + "'")
if ($aStatus -ne 'TRIAL') { Falhar ('PA_MUNDOS adm status=' + $aStatus) }

$rB = HttpRequest 'POST' ($apiUrl + '/api/public/cadastro/finalizar') '' @{
  empresa      = @{ nomeFantasia = 'R101 Drill Ctrl'; razaoSocial = 'R101 Drill Ctrl LTDA'; cnpj = $cnpjCtrl; segmento = 'Clinica'; cidade = 'Olinda'; uf = 'PE'; telefone = '(81) 99999-0021'; emailCorporativo = 'r101-drill-ctrl@plantaopro-local.test' }
  plano        = @{ planoId = $idPlano; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
  usuarioAdmin = @{ nome = 'R101 Ctrl Admin'; email = 'r101-drill-ctrl@plantaopro-local.test'; cargo = 'Gestor'; senha = 'R101-ctrl-2026#Kp9z' }
  statusAssinatura = 'TRIAL'
  diasTrial      = 5
}
if ($rB.Http -ne 200) { Falhar ('PA_MUNDOS ctrl http ' + $rB.Http + ' ' + $(if ($rB.Json) { $rB.Json.message } else { 'sem corpo' })) }
$bTenantId = [string]$rB.Json.data.tenantId
$bStatus = Psq ("select coalesce(status,'') from plantaopro.assinaturas where id='" + $rB.Json.data.assinaturaId + "'")
if ($bStatus -ne 'TRIAL') { Falhar ('PA_MUNDOS ctrl status=' + $bStatus) }
Log ('PA_MUNDOS_OK adm_tenant=' + $aTenantId + ' adm_status=TRIAL ctrl_tenant=' + $bTenantId + ' ctrl_status=TRIAL')

# ---------------------------------------------------------------- PA_RELATORIOS (governanca real: habilitar-tenant)
$rMod = HttpRequest 'GET' ($apiUrl + '/api/modulos') $tokSup $null
if ($rMod.Http -ne 200) { Falhar ('PA_RELATORIOS listagem de modulos http ' + $rMod.Http) }
$idRel = $null
foreach ($m in @($rMod.Json.data)) { if ([string]$m.codigo -eq 'RELATORIOS') { $idRel = [string]$m.id } }
if (-not $idRel) { Falhar 'PA_RELATORIOS modulo RELATORIOS nao encontrado no catalogo' }

$rHab = HttpRequest 'POST' ($apiUrl + '/api/modulos/' + $idRel + '/habilitar-tenant') $tokSup @{ tenantId = $aTenantId; precoContratado = 0; limiteContratado = 0; motivo = 'Drill R101 - benchmark do plano' }
if ($rHab.Http -ne 200) { Falhar ('PA_RELATORIOS habilitar http ' + $rHab.Http + ' ' + $(if ($rHab.Json) { $rHab.Json.message } else { 'sem corpo' })) }
$nContr = Psq ("select count(*) from plantaopro.tenant_modulos where tenant_id='" + $aTenantId + "' and reg_status='A' and upper(codigo_modulo)='RELATORIOS'")
$oContr = Psq ("select coalesce(origem,'') from plantaopro.tenant_modulos where tenant_id='" + $aTenantId + "' and reg_status='A' and upper(codigo_modulo)='RELATORIOS'")
$hContr = Psq ("select count(*) from plantaopro.tenant_modulos_historico h join plantaopro.tenant_modulos tm on tm.id=h.tenant_modulo_id where tm.tenant_id='" + $aTenantId + "' and h.acao='HABILITAR'")
$efRel  = Psq ("select coalesce(string_agg(upper(codigo),',' order by 1),'vazio') from plantaopro.modulos_efetivos('" + $aTenantId + "')")
if ($nContr -ne '1') { Falhar ('PA_RELATORIOS contrato=' + $nContr + ' esperado 1') }
if ($oContr -ne 'CONTRATO') { Falhar ('PA_RELATORIOS origem=' + $oContr + ' esperado CONTRATO') }
if ($hContr -ne '1') { Falhar ('PA_RELATORIOS historico=' + $hContr + ' esperado 1') }
if ($efRel -ne 'RELATORIOS') { Falhar ('PA_RELATORIOS efetivos=' + $efRel + ' esperado RELATORIOS') }
Log 'PA_RELATORIOS_OK http=200 origem=CONTRATO historico=1 efetivos=[RELATORIOS]'

# ---------------------------------------------------------------- PA_PERMISSAO (guard web: permissao por usuario)
# O guard web decide pelas claims de permissao emitidas no login (grants de perfil +
# excecoes por usuario; sem fallback por papel). O perfil ADMINISTRADOR_CLIENTE ainda nao
# tem grants no modulo RELATORIOS (lacuna de catalogo registrada na rodada) - o dono A
# recebe RELATORIOS.VER por usuario_permissoes_especiais, escopo por usuario/tenant.
$uPer = Psq ("select id from plantaopro.permissoes where replace(upper(codigo),':','.')='RELATORIOS.VER' and reg_status='A' limit 1")
if (-not $uPer) { Falhar 'PA_PERMISSAO permissao RELATORIOS.VER ausente do catalogo' }
$err = Psq ("insert into plantaopro.usuario_permissoes_especiais(tenant_id,usuario_id,permissao_id,permitido,justificativa) select t.id, u.id, '" + $uPer + "', true, 'Drill R101 - benchmark do plano' from plantaopro.usuarios u join plantaopro.tenants t on t.id=u.tenant_id where u.email='r101-drill-adm@plantaopro-local.test' and not exists (select 1 from plantaopro.usuario_permissoes_especiais x where x.usuario_id=u.id and x.permissao_id='" + $uPer + "' and x.reg_status='A')")
if ($err -match 'ERRO') { Falhar ('PA_PERMISSAO insercao: ' + $err) }
$nPer = Psq ("select count(*) from plantaopro.usuario_permissoes_especiais x where x.reg_status='A' and x.permitido=true and x.permissao_id='" + $uPer + "' and exists (select 1 from plantaopro.usuarios u where u.id=x.usuario_id and u.email='r101-drill-adm@plantaopro-local.test')")
if ($nPer -ne '1') { Falhar ('PA_PERMISSAO grants=' + $nPer + ' esperado 1') }
Log 'PA_PERMISSAO_OK usuario=donoA permissao=RELATORIOS.VER escopo=tenant'

# ---------------------------------------------------------------- PEERS_FIXTURE (P1/P2/C estruturais + eventos 09/2026)
# Números fechados para 2026-09 (mesmo plano do mundo A; as estatísticas agregadas somam todos
# os donos com dado no mês e a contagem de pares exclui o próprio dono):
#   A : 2 médicos, 8 plantões (4v/1disp = 75%), 8 confirmadas + 2 canceladas (20%),
#       preenchimento 10h, 2 pagamentos de 200 (média 200)  -> 8/2 = 4.0 por médico
#   C : 1 médico,  2 plantões (4v/0 = 100%), 2 confirmadas (0%),
#       preenchimento 8h, 1 pagamento de 300                -> 2/1 = 2.0 por médico
#   P1: 1 médico,  4 plantões (três 4v/0 + um 4v/2 = 87.5%), 4 confirmadas + 1 cancelada (20%),
#       preenchimento 5h, 1 pagamento de 100                -> 4/1 = 4.0 por médico
#   P2: 4 médicos, 12 plantões (4v/2 = 50%), 12 confirmadas (0%),
#       preenchimento 15h, 2 pagamentos de 400              -> 12/4 = 3.0 por médico
ExecutarTemplate $tplEstrutura @{ NOME='R101 Peer 1'; CNPJ=$cnpjP1; PLANO=$idPlano; QMED=1 }
ExecutarTemplate $tplEstrutura @{ NOME='R101 Peer 2'; CNPJ=$cnpjP2; PLANO=$idPlano; QMED=4 }
# Mundo A (funil): só a estrutura de médicos entra aqui (o cliente/assinatura já existem).
$cliAdm = Psq ("select id from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')='" + $cnpjAdm + "'")
$err = Psq ("insert into plantaopro.medicos(nome,cliente_id,status,reg_status) select 'R101 Adm Medico '||n, '" + $cliAdm + "', 'ATIVO','A' from generate_series(1,2) n;")
if ($err -match 'ERRO') { Falhar ('PEERS_FIXTURE medicos A: ' + $err) }

ExecutarTemplate $tplPlantoes @{ CNPJ=$cnpjAdm; VAGAS=4; DISP=1; STATUS='confirmado'; INICIO='2026-09-01 14:00:00+00'; REGDATE='2026-08-25 00:00:00+00'; N=7 }
ExecutarTemplate $tplEscalas  @{ CNPJ=$cnpjAdm; STATUS='confirmada'; REGESCALA='2026-08-25 10:00:00+00'; LIMITE=10; COND='g.n < 8' }
ExecutarTemplate $tplEscalas  @{ CNPJ=$cnpjAdm; STATUS='cancelado';  REGESCALA='2026-08-25 10:00:00+00'; LIMITE=10; COND='g.n >= 8' }
ExecutarTemplate $tplPagamentos @{ CNPJ=$cnpjAdm; VALOR=200; DATA='2026-09-15'; N=2 }

ExecutarTemplate $tplPlantoes @{ CNPJ=$cnpjP1; VAGAS=4; DISP=0; STATUS='confirmado'; INICIO='2026-09-05 10:00:00+00'; REGDATE='2026-08-20 00:00:00+00'; N=2 }
# P1 precisa do quarto plantão 4v/2 (média 87.5%): insercao direta final do lote.
$cliP1 = Psq ("select id from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')='" + $cnpjP1 + "'")
$err = Psq ("insert into plantaopro.plantoes(cliente_id,vagas,vagas_disponiveis,status,data_inicio,reg_date,reg_status) values('" + $cliP1 + "',4,2,'confirmado','2026-09-08 10:00:00+00'::timestamptz,'2026-08-20 00:00:00+00'::timestamptz,'A');")
if ($err -match 'ERRO') { Falhar ('PEERS_FIXTURE plantao extra P1: ' + $err) }

ExecutarTemplate $tplEscalas  @{ CNPJ=$cnpjP1; STATUS='confirmada'; REGESCALA='2026-08-20 05:00:00+00'; LIMITE=5; COND='g.n < 4' }
ExecutarTemplate $tplEscalas  @{ CNPJ=$cnpjP1; STATUS='cancelado';  REGESCALA='2026-08-20 05:00:00+00'; LIMITE=5; COND='g.n >= 4' }
ExecutarTemplate $tplPagamentos @{ CNPJ=$cnpjP1; VALOR=100; DATA='2026-09-20'; N=1 }

ExecutarTemplate $tplPlantoes @{ CNPJ=$cnpjP2; VAGAS=4; DISP=2; STATUS='confirmado'; INICIO='2026-09-01 08:00:00+00'; REGDATE='2026-08-18 00:00:00+00'; N=11 }
ExecutarTemplate $tplEscalas  @{ CNPJ=$cnpjP2; STATUS='confirmada'; REGESCALA='2026-08-18 15:00:00+00'; LIMITE=12; COND='true' }
ExecutarTemplate $tplPagamentos @{ CNPJ=$cnpjP2; VALOR=400; DATA='2026-09-25'; N=2 }

# Mundo C (funil, controle web): eventos reais para fechar a amostra em 3 pares com dado.
$cliCtrl = Psq ("select id from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')='" + $cnpjCtrl + "'")
$err = Psq ("insert into plantaopro.medicos(nome,cliente_id,status,reg_status) values('R101 Ctrl Medico', '" + $cliCtrl + "', 'ATIVO','A');")
if ($err -match 'ERRO') { Falhar ('PEERS_FIXTURE medico C: ' + $err) }
ExecutarTemplate $tplPlantoes @{ CNPJ=$cnpjCtrl; VAGAS=4; DISP=0; STATUS='confirmado'; INICIO='2026-09-10 10:00:00+00'; REGDATE='2026-08-25 00:00:00+00'; N=1 }
ExecutarTemplate $tplEscalas  @{ CNPJ=$cnpjCtrl; STATUS='confirmada'; REGESCALA='2026-08-25 08:00:00+00'; LIMITE=2; COND='true' }
ExecutarTemplate $tplPagamentos @{ CNPJ=$cnpjCtrl; VALOR=300; DATA='2026-09-12'; N=1 }


function ContarCnpj([string]$cnpj, [string]$tabela) {
  if ($tabela -eq 'pagamentos') {
    return Psq ("select count(*) from plantaopro.pagamentos g where g.plantao_id in (select id from plantaopro.plantoes where cliente_id=(select id from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')='" + $cnpj + "'))")
  }
  return Psq ("select count(*) from plantaopro." + $tabela + " x join plantaopro.clientes c on c.id=x.cliente_id where regexp_replace(c.cnpj,'\\D','','g')='" + $cnpj + "'")
}
$nPA = [int](ContarCnpj $cnpjAdm 'plantoes') + [int](ContarCnpj $cnpjAdm 'escalas') + [int](ContarCnpj $cnpjAdm 'pagamentos') + [int](ContarCnpj $cnpjAdm 'medicos')
$nP1 = [int](ContarCnpj $cnpjP1 'plantoes') + [int](ContarCnpj $cnpjP1 'escalas') + [int](ContarCnpj $cnpjP1 'pagamentos') + [int](ContarCnpj $cnpjP1 'medicos')
$nP2 = [int](ContarCnpj $cnpjP2 'plantoes') + [int](ContarCnpj $cnpjP2 'escalas') + [int](ContarCnpj $cnpjP2 'pagamentos') + [int](ContarCnpj $cnpjP2 'medicos')
$nPC = [int](ContarCnpj $cnpjCtrl 'plantoes') + [int](ContarCnpj $cnpjCtrl 'escalas') + [int](ContarCnpj $cnpjCtrl 'pagamentos') + [int](ContarCnpj $cnpjCtrl 'medicos')
if ($nPA -ne 22) { Falhar ('PEERS_FIXTURE eventos adm=' + $nPA + ' esperado 22') }
if ($nP1 -ne 11) { Falhar ('PEERS_FIXTURE eventos p1=' + $nP1 + ' esperado 11') }
if ($nP2 -ne 30) { Falhar ('PEERS_FIXTURE eventos p2=' + $nP2 + ' esperado 30') }
if ($nPC -ne 6) { Falhar ('PEERS_FIXTURE eventos ctrl=' + $nPC + ' esperado 6') }
Log ('PEERS_FIXTURE_OK adm_eventos=22 ctrl_eventos=6 p1_eventos=11 p2_eventos=30 total=69')

$tokAdm  = FazerLogin 'r101-drill-adm@plantaopro-local.test' 'R101-adm-2026#Kp9z'
$tokCtrl = FazerLogin 'r101-drill-ctrl@plantaopro-local.test' 'R101-ctrl-2026#Kp9z'

# ---------------------------------------------------------------- T1 (dump da chamada canônica: seus 5 indicadores vs o plano)
$rBen = HttpRequest 'GET' ($apiUrl + '/api/relatorios/benchmark?mes=2026-09') $tokAdm $null
if ($rBen.Http -ne 200) { Falhar ('T1 http ' + $rBen.Http + ' ' + $rBen.Content) }
[IO.File]::WriteAllText($t1arquivo, $rBen.Content, (New-Object System.Text.UTF8Encoding($false)))
$j = $rBen.Json
if (-not [bool]$j.success) { Falhar ('T1 sucesso=NAO ' + [string]$j.message) }
if ([string]$j.data.planoCodigo -ne 'R101_PLANO_DRILL') { Falhar ('T1 planoCodigo=' + [string]$j.data.planoCodigo) }
if ([string]$j.data.mesReferencia -ne '2026-09') { Falhar ('T1 mesReferencia=' + [string]$j.data.mesReferencia) }
if ([int]$j.data.amostraMinimaPeers -ne 3) { Falhar ('T1 amostraMinimaPeers=' + [int]$j.data.amostraMinimaPeers + ' esperado 3') }
if ([bool]$j.data.amostraInsuficiente) { Falhar 'T1 amostraInsuficiente=SIM esperado NAO' }
if (-not [bool]$j.data.temDadosProprios) { Falhar 'T1 temDadosProprios=NAO esperado SIM' }

function AssertKpi($json, [string]$codigo, [double]$meu, [double]$mediana, [double]$media, [int]$percentil, [int]$pares) {
  $i = KpiObter $json $codigo
  if ($null -eq $i) { Falhar ('AssertKpi ' + $codigo + ' ausente na resposta') }
  if (-not (NumProximo ([double]$i.meuValor) $meu)) { Falhar ('AssertKpi ' + $codigo + ' meuValor=' + [string]$i.meuValor + ' esperado ~' + $meu) }
  if (-not (NumProximo ([double]$i.medianaPlano) $mediana)) { Falhar ('AssertKpi ' + $codigo + ' mediana=' + [string]$i.medianaPlano + ' esperado ~' + $mediana) }
  if (-not (NumProximo ([double]$i.mediaPlano) $media)) { Falhar ('AssertKpi ' + $codigo + ' media=' + [string]$i.mediaPlano + ' esperado ~' + $media) }
  if ([int]$i.percentilMeu -ne $percentil) { Falhar ('AssertKpi ' + $codigo + ' percentil=' + [string]$i.percentilMeu + ' esperado ' + $percentil) }
  if ([int]$i.peersNaAmostra -ne $pares) { Falhar ('AssertKpi ' + $codigo + ' pares=' + [string]$i.peersNaAmostra + ' esperado ' + $pares) }
}
AssertKpi $j 'BENCH_PLANTOES_POR_MEDICO'         4.0   3.5   3.25    67 3
AssertKpi $j 'BENCH_COBERTURA_VAGAS_PCT'         75.0  81.25 78.125  33 3
AssertKpi $j 'BENCH_CANCELAMENTO_ESCALAS_PCT'    20.0  10.0  10.0    67 3
AssertKpi $j 'BENCH_TEMPO_MEDIO_PREENCHIMENTO_H' 10.0  9.0   9.5     67 3
AssertKpi $j 'BENCH_VALOR_MEDIO_PLANTAO_PAGO'    200.0 250.0 250.0   33 3
Log 'T1_DUMP_OK arquivo=r101a_t1_benchmark_api.json plano=R101_PLANO_DRILL mes=2026-09 kpis=5'

# ---------------------------------------------------------------- BENCHMARK_VAZIO (mes sem atividade -> amostra insuficiente)
$rVaz = HttpRequest 'GET' ($apiUrl + '/api/relatorios/benchmark?mes=2025-01') $tokAdm $null
if ($rVaz.Http -ne 200) { Falhar ('BENCHMARK_VAZIO http ' + $rVaz.Http + ' ' + $rVaz.Content) }
$jv = $rVaz.Json
if (-not [bool]$jv.success) { Falhar ('BENCHMARK_VAZIO sucesso=NAO ' + [string]$jv.message) }
if (-not [bool]$jv.data.amostraInsuficiente) { Falhar 'BENCHMARK_VAZIO amostraInsuficiente=NAO esperado SIM' }
$nNulos = 0
foreach ($i in @($jv.data.indicadores)) {
  if ($null -eq $i.meuValor -and $null -eq $i.medianaPlano -and $null -eq $i.mediaPlano -and $null -eq $i.percentilMeu -and [int]$i.peersNaAmostra -eq 0) { $nNulos++ }
}
if ($nNulos -ne 5) { Falhar ('BENCHMARK_VAZIO kpis_totais_nulos=' + $nNulos + ' esperado 5') }
Log 'BENCHMARK_VAZIO_OK http=200 mes=2025-01 insuficiente=SIM valores_nulos=5'

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
$bodyLogin = 'Email=' + [System.Uri]::EscapeDataString('r101-drill-adm@plantaopro-local.test') + '&Senha=' + [System.Uri]::EscapeDataString('R101-adm-2026#Kp9z') + '&__RequestVerificationToken=' + [System.Uri]::EscapeDataString($antiTok)
$post = Invoke-WebRequest -Uri ($webUrl + '/Account/Login') -Method Post -WebSession $sessao -Body $bodyLogin -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -MaximumRedirection 10
if ([int]$post.StatusCode -ne 200) { Falhar ('WEB_LOGIN http ' + [int]$post.StatusCode) }
Log 'WEB_LOGIN_OK http=200'

function WebObter([string]$rota, $sess) {
  for ($w = 0; $w -lt 6; $w++) {
    try { $q = Invoke-WebRequest -Uri ($webUrl + $rota) -WebSession $sess -UseBasicParsing -TimeoutSec 60 -MaximumRedirection 10; if ([int]$q.StatusCode -eq 200) { return $q } }
    catch { Start-Sleep 2 }
  }
  return $null
}

# /Relatorios/Index: o cartao do benchmark esta no catalogo
$qHub = WebObter '/Relatorios/Index' $sessao
if ($null -eq $qHub) { Falhar 'WEB_HUB http != 200 apos 6 tentativas' }
$mCartao = [bool]($qHub.Content -like '*Benchmark do plano*')
if (-not $mCartao) { Falhar 'WEB_HUB cartao Benchmark do plano ausente' }
Log 'WEB_HUB_OK http=200 cartao=SIM'

# /Relatorios/Benchmark: pagina server-side com os valores reais do T1
$qBen = WebObter '/Relatorios/Benchmark?mes=2026-09' $sessao
if ($null -eq $qBen) { Falhar 'WEB_BENCHMARK http != 200 apos 6 tentativas' }
$hBen = $qBen.Content
function DumpBenchmark([string]$h) { try { [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'r101a_debug_benchmark.html'), $h, (New-Object System.Text.UTF8Encoding($false))) } catch {} }
$mKpi  = [bool]($hBen -like '*data-kpi="BENCH_COBERTURA_VAGAS_PCT"*')
$mPlano = [bool]($hBen -like '*R101_PLANO_DRILL*')
$mVal  = [bool]($hBen -like '*75,00*')
$mPct  = [bool]($hBen -like '*67% da amostra*')
if (-not $mKpi) { DumpBenchmark $hBen; Falhar ('WEB_BENCHMARK marcador kpi ausente (bytes=' + $hBen.Length + ')') }
if (-not $mPlano) { DumpBenchmark $hBen; Falhar 'WEB_BENCHMARK plano R101_PLANO_DRILL ausente' }
if (-not $mVal) { DumpBenchmark $hBen; Falhar 'WEB_BENCHMARK valor 75,00 (cobertura/mediana) ausente' }
if (-not $mPct) { DumpBenchmark $hBen; Falhar 'WEB_BENCHMARK posicao 67% da amostra ausente' }
Log 'WEB_BENCHMARK_OK http=200 kpi=SIM plano=SIM valor=SIM percentil=SIM'

# ---------------------------------------------------------------- CONTROLE_WEB (mundo sem RELATORIOS: AccessDenied)
$sessaoC = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$pgC = Invoke-WebRequest -Uri ($webUrl + '/Account/Login') -WebSession $sessaoC -UseBasicParsing
$htmlC = $pgC.Content
$tokC = $htmlC.IndexOf('name="__RequestVerificationToken"')
if ($tokC -lt 0) { Falhar 'CONTROLE_WEB_LOGIN token antiforgery nao encontrado' }
$valC = $htmlC.IndexOf('value="', $tokC)
$tC1 = $valC + 'value="'.Length
$tC2 = $htmlC.IndexOf('"', $tC1)
$antiC = $htmlC.Substring($tC1, $tC2 - $tC1)
$bodyC = 'Email=' + [System.Uri]::EscapeDataString('r101-drill-ctrl@plantaopro-local.test') + '&Senha=' + [System.Uri]::EscapeDataString('R101-ctrl-2026#Kp9z') + '&__RequestVerificationToken=' + [System.Uri]::EscapeDataString($antiC)
$postC = Invoke-WebRequest -Uri ($webUrl + '/Account/Login') -Method Post -WebSession $sessaoC -Body $bodyC -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -MaximumRedirection 10
if ([int]$postC.StatusCode -ne 200) { Falhar ('CONTROLE_WEB_LOGIN http ' + [int]$postC.StatusCode) }
$qC = WebObter '/Relatorios/Benchmark' $sessaoC
if ($null -eq $qC) { Falhar 'CONTROLE_WEB http != 200 apos 6 tentativas' }
$mMotivo = [bool]($qC.Content -like '*MODULO_NAO_CONTRATADO*')
if (-not $mMotivo) { Falhar 'CONTROLE_WEB motivo MODULO_NAO_CONTRATADO ausente' }
Log 'CONTROLE_WEB_OK http=200 motivo=MODULO_NAO_CONTRATADO'

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
