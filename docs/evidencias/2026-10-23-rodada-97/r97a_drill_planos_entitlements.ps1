# R97 (planos/precos configuraveis + entitlements): drill ponta a ponta.
# O admin configura o catalogo comercial via api/planos (codigo canonico, slug,
# publico/ordem, preco anual e os parametros de teste gratis por plano), o
# funil self-service respeita esses entitlements com mensagens honestas, o ANUAL
# contrata o preco anual configurado (antes a coluna valor_anual era morta),
# e a conversao trial->pago (nova rota converter-para-paga) abre o gate que o
# trial vencido havia fechado — pendencia herdada da R96. Provas:
#   ESTAGEM_1        API no ar (health)
#   PA_PLANO_ADMIN   admin cria plano com codigo/preco anual/dias de teste -> 200
#   PA_DUPLICA       mesmo codigo (normalizado) -> 409 "ja esta em uso"
#   PA_PLANO_EDIT    PUT move preco anual/dias -> 200 (detalhe confirma)
#   PA_CATALOGO      catalogo publico expoe valorAnualEfetivo e oculta HOMOLOGACAO_360
#   PA_TRIAL_TETO    diasTrial acima do teto do plano -> 400 sem resíduo
#   PA_TRIAL_SEM     plano com permite_trial=false -> 400 sem resíduo
#   PA_TRIAL_OK      TRIAL dentro do teto -> mundo completo sem cobranca inicial
#   PA_ANUAL_OK      ATIVA ANUAL -> valor contratado/cobranca = preco anual, fim 1 ano
#   PA_GATE_*        trial vencido fecha /uso (403); conversao abre; duplicata 400
# A limpeza por CNPJ/codigo no inicio garante rerun previsivel (crash-idempotencia);
# os tenants do TRIAL convertido e da ATIVA permanecem no banco ao final.
$ErrorActionPreference = 'Stop'

$repo  = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$db    = 'plantaopro_test'
$porta = 52077
$apiUrl = 'http://localhost:' + $porta
$apiDll = Join-Path $repo 'backend/PlantaoPro.Api/bin/Release/net10.0/PlantaoPro.Api.dll'
$psql  = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$env:PGPASSWORD = '123456'
$env:PGCLIENTENCODING = 'UTF8'
$saida = Join-Path $PSScriptRoot 'r97a_drill_saida.txt'

$linhas = New-Object System.Collections.Generic.List[string]
function Log([string]$l) { $linhas.Add($l); Write-Host $l }
function EscreverSaida() { Set-Content -Path $saida -Value $linhas -Encoding UTF8 }

$global:Falhou = $false
function Falhar([string]$msg) {
  $global:Falhou = $true
  Log ('FALHA ' + $msg)
  Log 'RESULTADO=FALHOU'
  EscreverSaida
  try { if ($global:PidApi) { Stop-Process -Id $global:PidApi -Force -ErrorAction SilentlyContinue } } catch {}
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
    $code = 0; $txt = ''
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
function Moeda([object]$v) { return ([decimal]$v).ToString('0.00', [System.Globalization.CultureInfo]::InvariantCulture) }
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
    $qTenant  = if ($p[1]) { "'" + $p[1] + "'" } else { 'null' }
    $qAssina  = if ($p[2]) { "'" + $p[2] + "'" } else { 'null' }
    $qUsuario = if ($p[3]) { "'" + $p[3] + "'" } else { 'null' }
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

Log ('R97A_DRILL data=' + (Get-Date -Format 'yyyyMMdd-HHmmss') + ' db=' + $db + ' porta=' + $porta)
$global:ApiProc = $null
$global:PidApi = $null

# ---------------------------------------------------------------- ESTAGEM 1
if (-not (Test-Path $apiDll)) { Falhar ('DLL da API ausente: repo=' + $repo + ' dll=' + $apiDll) }
if (-not (PortaLivre $porta)) { Falhar ('porta ' + $porta + ' ocupada antes do inicio') }
Log 'PORTAS_LIVRES_INICIO=SIM'
$env:ConnectionStrings__Default = 'Host=127.0.0.1;Port=5432;Database=' + $db + ';Username=postgres;Password=123456'
$env:Jwt__Key = 'r97-drill-dev-jwt-key-0123456789-abcd'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ApiSettings__BaseUrl = $apiUrl
$env:PlantaoProApi__BaseUrl = $apiUrl
$global:ApiProc = Start-Process -FilePath 'dotnet' -ArgumentList ('"' + $apiDll + '" --urls http://localhost:' + $porta) -WorkingDirectory (Join-Path $repo 'backend/PlantaoPro.Api') -WindowStyle Hidden -PassThru
if (-not (ApiAguardarNoAr 60)) { Falhar 'API nao ficou no ar em 60s' }
$owner = Get-NetTCPConnection -State Listen -LocalPort $porta -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $owner) { Falhar 'API no ar mas sem dono da porta' }
$global:PidApi = [int]$owner.OwningProcess
Log ('ESTAGEM_1_API_NO_AR ok=SIM pid=' + $global:PidApi)

# -------------------------------------------------- limpeza para rerun previsivel
foreach ($cj in @('97001001000111', '97002002000122', '97003003000133', '97004004000144')) { LimparMundoPorCnpj $cj | Out-Null }
LimparPlanoPorCodigo 'R97_PLANO_DRILL' | Out-Null
Log 'PRE_LIMPEZA_RERUN ok=SIM'

$tokAdm = FazerLogin 'superadmin@plantaopro.local' 'Super@123456'

# -------------------------------------------------- payload administrativo do plano
function PayloadPlano([double]$anual, [double]$dias, [bool]$permiteTrial) {
  return @{
    nome = 'R97 Plano Drill'
    descricao = 'Plano do drill r97'
    valorMensal = 100.00
    limiteMedicos = 0; limiteHospitais = 0; limitePlantoesMes = 0
    permiteMobile = $false; permiteBi = $false; permiteRelatoriosAvancados = $false; permiteIntegracoes = $false
    codigo = 'r97_plano_drill'
    slug = 'r97-plano-drill'
    valorAnual = $anual
    publico = $true; destaque = $false; ordem = 950; permiteWhiteLabel = $false
    permiteTrial = $permiteTrial
    diasTrial = $dias
  }
}

# ---------------------------------------------------------------- PA_PLANO_ADMIN (criar)
$rCriar = HttpRequest 'POST' ($apiUrl + '/api/planos') $tokAdm (PayloadPlano 1150.00 5 $true)
if ($rCriar.Http -ne 200) { Falhar ('PA_PLANO_ADMIN http ' + $rCriar.Http + ' ' + $rCriar.Json.message) }
$idPlano = [string]$rCriar.Json.data
Log ('PA_PLANO_ADMIN_OK http=200 plano=' + $idPlano)

# ---------------------------------------------------------------- PA_DUPLICA_CODIGO (409)
$rDup = HttpRequest 'POST' ($apiUrl + '/api/planos') $tokAdm (PayloadPlano 1150.00 5 $true)
$dupMsg = ''
if ($rDup.Json) { $dupMsg = [string]$rDup.Json.message }
$dupOk = ($dupMsg -like '*em uso*')
Log ('PA_DUPLICA_CODIGO_OK http=409 msg_em_uso=' + ($(if ($dupOk) { 'SIM' } else { 'NAO' })))
if ($rDup.Http -ne 409) { Falhar ('PA_DUPLICA http ' + $rDup.Http + ' esperado 409') }
if (-not $dupOk) { Falhar ('PA_DUPLICA mensagem=' + $dupMsg) }

# ---------------------------------------------------------------- PA_PLANO_EDIT (preco anual/dias)
$rEd1 = HttpRequest 'PUT' ($apiUrl + '/api/planos/' + $idPlano) $tokAdm (PayloadPlano 1250.00 7 $true)
if ($rEd1.Http -ne 200) { Falhar ('PA_PLANO_EDIT http ' + $rEd1.Http) }
$d1 = (HttpRequest 'GET' ($apiUrl + '/api/planos/' + $idPlano) $tokAdm $null).Json.data
if (-not ($d1)) { Falhar 'PA_PLANO_EDIT detalhe vazio' }
Log ('PA_PLANO_EDIT_OK http=200 valor_anual=' + (Moeda $d1.valorAnual) + ' dias_trial=' + $d1.diasTrial)
if ((Moeda $d1.valorAnual) -ne '1250.00') { Falhar ('PA_PLANO_EDIT valor_anual=' + (Moeda $d1.valorAnual)) }
if ([int]$d1.diasTrial -ne 7) { Falhar ('PA_PLANO_EDIT dias_trial=' + $d1.diasTrial) }
if ([string]$d1.codigo -ne 'R97_PLANO_DRILL') { Falhar ('PA_PLANO_EDIT codigo nao normalizado=' + $d1.codigo) }

# ---------------------------------------------------------------- PA_PLANO_EDIT2 (volta ao teto 5)
$rEd2 = HttpRequest 'PUT' ($apiUrl + '/api/planos/' + $idPlano) $tokAdm (PayloadPlano 1250.00 5 $true)
$d2 = (HttpRequest 'GET' ($apiUrl + '/api/planos/' + $idPlano) $tokAdm $null).Json.data
Log ('PA_PLANO_EDIT2_OK http=200 dias_trial=' + $d2.diasTrial)
if ($rEd2.Http -ne 200 -or [int]$d2.diasTrial -ne 5) { Falhar ('PA_PLANO_EDIT2 http ' + $rEd2.Http + ' dias ' + $d2.diasTrial) }

# ---------------------------------------------------------------- PA_CATALOGO (publico)
$cat = (HttpRequest 'GET' ($apiUrl + '/api/public/planos') '' $null).Json.data
$catArr = @($cat)
$doDrill = $catArr | Where-Object { $_.slug -eq 'r97-plano-drill' } | Select-Object -First 1
if (-not $doDrill) { Falhar 'catalogo publico sem o plano do drill' }
$homId = Psq ("select id from plantaopro.planos where upper(codigo)='HOMOLOGACAO_360'")
$internoVizivel = ($catArr | Where-Object { [string]$_.id -eq $homId }) -ne $null
$efetivo = Moeda $doDrill.valorAnualEfetivo
Log ('PA_CATALOGO_OK total=' + $catArr.Count + ' tem_r97=SIM anual_efetivo=' + $efetivo + ' interno=' + ($(if ($internoVizivel) { 'SIM' } else { 'NAO' })))
if ($efetivo -ne '1250.00') { Falhar ('catalogo valorAnualEfetivo=' + $efetivo) }
if ($internoVizivel) { Falhar 'plano interno HOMOLOGACAO_360 visivel no catalogo publico' }
if (-not [bool]$doDrill.permiteTrial) { Falhar 'catalogo sem o entitlement permiteTrial' }
if ([int]$doDrill.diasTrial -ne 5) { Falhar ('catalogo diasTrial=' + $doDrill.diasTrial) }

# ---------------------------------------------------------------- PA_TRIAL_TETO (6 > 5)
$rTeto = HttpRequest 'POST' ($apiUrl + '/api/public/cadastro/finalizar') '' @{
  empresa      = @{ nomeFantasia = 'R97 Drill Teto'; razaoSocial = 'R97 Drill Teto LTDA'; cnpj = '97003003000133'; segmento = 'Clinica'; cidade = 'Recife'; uf = 'PE'; emailCorporativo = 'r97-drill-teto@plantaopro-local.test' }
  plano        = @{ planoId = $idPlano; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
  usuarioAdmin = @{ nome = 'R97 Teto Admin'; email = 'r97-drill-teto@plantaopro-local.test'; cargo = 'Gestor'; senha = 'R97-teto-2026#Kp9z' }
  statusAssinatura = 'TRIAL'
  diasTrial      = 6
}
$tetoMsg = ''; if ($rTeto.Json) { $tetoMsg = [string]$rTeto.Json.message }
$tetoResiduo = Psq ("select count(*) from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')='97003003000133'")
Log ('PA_TRIAL_TETO_OK http=400 msg_teto=' + ($(if ($tetoMsg -like '*5 dias*') { 'SIM' } else { 'NAO' })) + ' residuo=' + $tetoResiduo)
if ($rTeto.Http -ne 400) { Falhar ('PA_TRIAL_TETO http ' + $rTeto.Http + ' esperado 400') }
if ($tetoMsg -notlike '*5 dias*') { Falhar ('PA_TRIAL_TETO msg=' + $tetoMsg) }
if ($tetoResiduo -ne '0') { Falhar ('PA_TRIAL_TETO residuo=' + $tetoResiduo) }

# ---------------------------------------------------------------- PA_TRIAL_SEM_PERMISSAO
$rSemEd = HttpRequest 'PUT' ($apiUrl + '/api/planos/' + $idPlano) $tokAdm (PayloadPlano 1250.00 5 $false)
if ($rSemEd.Http -ne 200) { Falhar ('PA_TRIAL_SEM_PERMISSAO edit http ' + $rSemEd.Http) }
$rSem = HttpRequest 'POST' ($apiUrl + '/api/public/cadastro/finalizar') '' @{
  empresa      = @{ nomeFantasia = 'R97 Drill SemTrial'; razaoSocial = 'R97 Drill SemTrial LTDA'; cnpj = '97004004000144'; segmento = 'Clinica'; cidade = 'Recife'; uf = 'PE'; emailCorporativo = 'r97-drill-semtrial@plantaopro-local.test' }
  plano        = @{ planoId = $idPlano; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
  usuarioAdmin = @{ nome = 'R97 SemTrial Admin'; email = 'r97-drill-semtrial@plantaopro-local.test'; cargo = 'Gestor'; senha = 'R97-semtrial-2026#Kp9z' }
  statusAssinatura = 'TRIAL'
  diasTrial      = 5
}
$semMsg = ''; if ($rSem.Json) { $semMsg = [string]$rSem.Json.message }
$semResiduo = Psq ("select count(*) from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')='97004004000144'")
Log ('PA_TRIAL_SEM_PERMISSAO_OK http=400 msg_sem_trial=' + ($(if ($semMsg -like '*teste gratuito*') { 'SIM' } else { 'NAO' })) + ' residuo=' + $semResiduo)
if ($rSem.Http -ne 400) { Falhar ('PA_TRIAL_SEM_PERMISSAO http ' + $rSem.Http + ' esperado 400') }
if ($semMsg -notlike '*teste gratuito*') { Falhar ('PA_TRIAL_SEM_PERMISSAO msg=' + $semMsg) }
if ($semResiduo -ne '0') { Falhar ('PA_TRIAL_SEM_PERMISSAO residuo=' + $semResiduo) }
$rSemVoltar = HttpRequest 'PUT' ($apiUrl + '/api/planos/' + $idPlano) $tokAdm (PayloadPlano 1250.00 5 $true)
if ($rSemVoltar.Http -ne 200) { Falhar ('PA_TRIAL_SEM_PERMISSAO restaurar http ' + $rSemVoltar.Http) }

# ---------------------------------------------------------------- PA_TRIAL_OK (5 dias, dentro do teto)
$rT = HttpRequest 'POST' ($apiUrl + '/api/public/cadastro/finalizar') '' @{
  empresa      = @{ nomeFantasia = 'R97 Drill Trial'; razaoSocial = 'R97 Drill Trial LTDA'; cnpj = '97001001000111'; segmento = 'Clinica'; cidade = 'Recife'; uf = 'PE'; emailCorporativo = 'r97-drill-trial@plantaopro-local.test' }
  plano        = @{ planoId = $idPlano; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
  usuarioAdmin = @{ nome = 'R97 Trial Admin'; email = 'r97-drill-trial@plantaopro-local.test'; cargo = 'Gestora'; senha = 'R97-trial-2026#Kp9z' }
  statusAssinatura = 'TRIAL'
  diasTrial      = 5
}
if ($rT.Http -ne 200) { Falhar ('PA_TRIAL_OK http ' + $rT.Http + ' ' + $rT.Json.message) }
$dT = $rT.Json.data
$tStatus   = Psq ("select coalesce(status,'') from plantaopro.assinaturas where id='" + $dT.assinaturaId + "'")
$tTrialFim = Psq ("select (a.data_trial_fim::date = (current_date + 5)) from plantaopro.assinaturas a where a.id='" + $dT.assinaturaId + "'")
$tCobranca = Psq ("select count(*) from plantaopro.cadastro_cliente_pagamentos_iniciais p where p.assinatura_id='" + $dT.assinaturaId + "'")
Log ('PA_TRIAL_OK tenant=' + $dT.tenantId + ' assinatura=' + $dT.assinaturaId + ' status=' + $tStatus + ' trial_fim_ok=' + ($(if ($tTrialFim -eq 't') { 'SIM' } else { 'NAO' })) + ' cob_inicial=' + $tCobranca)
if ($tStatus -ne 'TRIAL') { Falhar ('PA_TRIAL_OK status=' + $tStatus) }
if ($tTrialFim -ne 't') { Falhar 'PA_TRIAL_OK data_trial_fim != hoje+5' }
if ($tCobranca -ne '0') { Falhar ('PA_TRIAL_OK cobranca inicial=' + $tCobranca) }

# ---------------------------------------------------------------- PA_ANUAL_OK (preco anual + ciclo de 1 ano)
$rA = HttpRequest 'POST' ($apiUrl + '/api/public/cadastro/finalizar') '' @{
  empresa      = @{ nomeFantasia = 'R97 Drill Anual'; razaoSocial = 'R97 Drill Anual LTDA'; cnpj = '97002002000122'; segmento = 'Clinica'; cidade = 'Olinda'; uf = 'PE'; emailCorporativo = 'r97-drill-anual@plantaopro-local.test' }
  plano        = @{ planoId = $idPlano; periodicidade = 'ANUAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
  usuarioAdmin = @{ nome = 'R97 Anual Admin'; email = 'r97-drill-anual@plantaopro-local.test'; cargo = 'Gestor'; senha = 'R97-anual-2026#Kp9z' }
}
if ($rA.Http -ne 200) { Falhar ('PA_ANUAL_OK http ' + $rA.Http + ' ' + $rA.Json.message) }
$dA = $rA.Json.data
$aStatus     = Psq ("select coalesce(status,'') from plantaopro.assinaturas where id='" + $dA.assinaturaId + "'")
$aPeriodic   = Psq ("select coalesce(periodicidade,'') from plantaopro.assinaturas where id='" + $dA.assinaturaId + "'")
$aValor      = Psq ("select round(valor_contratado::numeric,2)::text from plantaopro.assinaturas where id='" + $dA.assinaturaId + "'")
$aCobranca   = Psq ("select round(coalesce(sum(p.valor),0)::numeric,2)::text from plantaopro.cadastro_cliente_pagamentos_iniciais p where p.assinatura_id='" + $dA.assinaturaId + "'")
$aFimAno     = Psq ("select (a.data_fim::date between (current_date + 364) and (current_date + 366)) from plantaopro.assinaturas a where a.id='" + $dA.assinaturaId + "'")
Log ('PA_ANUAL_OK tenant=' + $dA.tenantId + ' assinatura=' + $dA.assinaturaId + ' status=' + $aStatus + ' periodicidade=' + $aPeriodic + ' valor=' + $aValor + ' cob=' + $aCobranca + ' fim_1ano=' + ($(if ($aFimAno -eq 't') { 'SIM' } else { 'NAO' })))
if ($aStatus -ne 'ATIVA') { Falhar ('PA_ANUAL_OK status=' + $aStatus) }
if ($aPeriodic -ne 'ANUAL') { Falhar ('PA_ANUAL_OK periodicidade=' + $aPeriodic) }
if ($aValor -ne '1250.00') { Falhar ('PA_ANUAL_OK valor_contratado=' + $aValor) }
if ($aCobranca -ne '1250.00') { Falhar ('PA_ANUAL_OK cobranca inicial=' + $aCobranca) }
if ($aFimAno -ne 't') { Falhar 'PA_ANUAL_OK data_fim fora do ciclo de 1 ano' }

# ---------------------------------------------------------------- PA_GATE_ATIVO (trial vigente)
$tokTrial = FazerLogin 'r97-drill-trial@plantaopro-local.test' 'R97-trial-2026#Kp9z'
$gm = HttpRequest 'GET' ($apiUrl + '/api/minha-assinatura') $tokTrial $null
$gu = HttpRequest 'GET' ($apiUrl + '/api/minha-assinatura/uso') $tokTrial $null
Log ('PA_GATE_ATIVO_OK minha_http=' + $gm.Http + ' minha_status=' + $gm.Json.data.status + ' uso_http=' + $gu.Http)
if ($gm.Http -ne 200) { Falhar ('PA_GATE_ATIVO minha http ' + $gm.Http) }
if ([string]$gm.Json.data.status -ne 'TRIAL') { Falhar ('PA_GATE_ATIVO status=' + $gm.Json.data.status) }
if ($gu.Http -ne 200) { Falhar ('PA_GATE_ATIVO uso http ' + $gu.Http) }

# ---------------------------------------------------------------- PA_GATE_VENCIDO (backdate -> 403)
Psq ("update plantaopro.assinaturas set data_trial_fim = current_date - 1 where id='" + $dT.assinaturaId + "'") | Out-Null
$gv = HttpRequest 'GET' ($apiUrl + '/api/minha-assinatura/uso') $tokTrial $null
$gvMsg = ''; if ($gv.Json) { $gvMsg = [string]$gv.Json.message }
Log ('PA_GATE_VENCIDO_OK uso_http=' + $gv.Http + ' msg_encerrado=' + ($(if ($gvMsg -like '*experimental encerrado*') { 'SIM' } else { 'NAO' })))
if ($gv.Http -ne 403) { Falhar ('PA_GATE_VENCIDO uso http ' + $gv.Http + ' esperado 403') }
if ($gvMsg -notlike '*experimental encerrado*') { Falhar ('PA_GATE_VENCIDO msg=' + $gvMsg) }

# ---------------------------------------------------------------- PA_CONVERSAO (trial -> paga)
$rc = HttpRequest 'POST' ($apiUrl + '/api/assinaturas/' + $dT.assinaturaId + '/converter-para-paga') $tokAdm @{ justificativa = 'conversao drill r97' }
$cStatus    = Psq ("select coalesce(status,'') from plantaopro.assinaturas where id='" + $dT.assinaturaId + "'")
$cTrialNulo = Psq ("select (data_trial_fim is null) from plantaopro.assinaturas where id='" + $dT.assinaturaId + "'")
$cValor     = Psq ("select round(valor_contratado::numeric,2)::text from plantaopro.assinaturas where id='" + $dT.assinaturaId + "'")
$cFimMes    = Psq ("select (data_fim::date between (current_date + 31) and (current_date + 61)) from plantaopro.assinaturas where id='" + $dT.assinaturaId + "'")
$cHist      = Psq ("select count(*) from plantaopro.assinatura_historico where assinatura_id='" + $dT.assinaturaId + "' and acao='CONVERSAO_TRIAL_PAGO'")
Log ('PA_CONVERSAO_OK http=' + $rc.Http + ' status=' + $cStatus + ' trial_fim=' + ($(if ($cTrialNulo -eq 't') { 'nulo' } else { 'cheio' })) + ' valor=' + $cValor + ' fim_1mes=' + ($(if ($cFimMes -eq 't') { 'SIM' } else { 'NAO' })) + ' historico=' + $cHist)
if ($rc.Http -ne 200) { Falhar ('PA_CONVERSAO http ' + $rc.Http + ' ' + $(if ($rc.Json) { $rc.Json.message } else { 'sem corpo' })) }
if ($cStatus -ne 'ATIVA') { Falhar ('PA_CONVERSAO status=' + $cStatus) }
if ($cTrialNulo -ne 't') { Falhar 'PA_CONVERSAO data_trial_fim nao limpa' }
if ($cValor -ne '100.00') { Falhar ('PA_CONVERSAO valor=' + $cValor) }
if ($cFimMes -ne 't') { Falhar 'PA_CONVERSAO data_fim fora de 1 mes' }
if ($cHist -ne '1') { Falhar ('PA_CONVERSAO historico=' + $cHist) }

# ---------------------------------------------------------------- PA_CONVERSAO_DUPLO (400)
$rd = HttpRequest 'POST' ($apiUrl + '/api/assinaturas/' + $dT.assinaturaId + '/converter-para-paga') $tokAdm @{ justificativa = 'tentativa duplicada' }
$rdMsg = ''; if ($rd.Json) { $rdMsg = [string]$rd.Json.message }
Log ('PA_CONVERSAO_DUPLO_OK http=' + $rd.Http + ' msg_apenas_trial=' + ($(if ($rdMsg -like '*assinaturas em teste*') { 'SIM' } else { 'NAO' })))
if ($rd.Http -ne 400) { Falhar ('PA_CONVERSAO_DUPLO http ' + $rd.Http + ' esperado 400') }
if ($rdMsg -notlike '*assinaturas em teste*') { Falhar ('PA_CONVERSAO_DUPLO msg=' + $rdMsg) }

# ---------------------------------------------------------------- PA_GATE_ABERTO (gate reaberto)
$ga = HttpRequest 'GET' ($apiUrl + '/api/minha-assinatura') $tokTrial $null
$gaU = HttpRequest 'GET' ($apiUrl + '/api/minha-assinatura/uso') $tokTrial $null
Log ('PA_GATE_ABERTO_OK minha_http=' + $ga.Http + ' minha_status=' + $ga.Json.data.status + ' uso_http=' + $gaU.Http)
if ($ga.Http -ne 200) { Falhar ('PA_GATE_ABERTO minha http ' + $ga.Http) }
if ([string]$ga.Json.data.status -ne 'ATIVA') { Falhar ('PA_GATE_ABERTO status=' + $ga.Json.data.status) }
if ($gaU.Http -ne 200) { Falhar ('PA_GATE_ABERTO uso http ' + $gaU.Http) }

# ---------------------------------------------------------------- teardown
try { if ($global:PidApi) { Stop-Process -Id $global:PidApi -Force -ErrorAction SilentlyContinue } } catch {}
Start-Sleep 2
$resto = Get-NetTCPConnection -State Listen -LocalPort $porta -ErrorAction SilentlyContinue | Select-Object -First 1
if ($resto) { try { Stop-Process -Id ([int]$resto.OwningProcess) -Force -ErrorAction SilentlyContinue } catch {}; Start-Sleep 2 }
if (-not (PortaLivre $porta)) { Falhar ('porta ' + $porta + ' ainda ocupada no teardown') }
Log 'PORTAS_LIVRES=SIM'
if ($global:Falhou) { Falhar 'resultado final negativo' }
Log 'RESULTADO=PASSEI'
EscreverSaida
exit 0
