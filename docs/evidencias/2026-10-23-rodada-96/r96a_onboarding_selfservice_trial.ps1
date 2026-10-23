# R96 (B6 onboarding self-service): drill do funil publico com teste gratis.
# O cadastro self-service (POST /api/public/cadastro/finalizar) agora aceita
# statusAssinatura=TRIAL com diasTrial explicito (1..90): a assinatura nasce TRIAL,
# SEM cobranca inicial, e com data_fim = teste + 30 dias de janela de conversao
# (MESMO nucleo do provisionamento manual B2B). Status ausente = ATIVA com cobranca
# inicial (comportamento B5 preservado). Este drill sobe a API standalone contra o
# banco vivo e prova, ponta a ponta:
#   ESTAGEM_1  API no ar (health)
#   ESTAGEM_2  catalogo publico de planos + FAQ do teste gratis presente (v2375)
#   PA_TRIAL   cadastro TRIAL 14d -> mundo completo (tenant/cliente/assinatura TRIAL
#              com trial_fim e fim=teste+30d, sem cobranca inicial, LGPD consentimento,
#              onboarding materializado) -> login do dono -> onboarding/status 200
#              -> minha-assinatura expoe o TRIAL com as datas reais
#   PA_DUPLICATA mesmo CNPJ novo e-mail -> 400 honesto sem residuo
#   PA_VALIDACAO diasTrial 0/91/ausente e status invalido -> 400 sem residuo
#   PA_ATIVA   status ausente (legado B5) -> ATIVA com cobranca inicial ABERTA visivel
#              em minha-assinatura/faturas
# A limpeza por CNPJ no inicio garante rerun previsivel (padrao B5 crash-idempotencia);
# os tenants do TRIAL e da ATIVA permanecem no banco ao final (cadastros reais).
$ErrorActionPreference = 'Stop'

$repo  = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$db    = 'plantaopro_test'
$porta = 51977
$apiUrl = 'http://localhost:' + $porta
$apiDll = Join-Path $repo 'backend/PlantaoPro.Api/bin/Release/net10.0/PlantaoPro.Api.dll'
$psql  = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$env:PGPASSWORD = '123456'
$env:PGCLIENTENCODING = 'UTF8'
$saida = Join-Path $PSScriptRoot 'r96a_drill_saida.txt'

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
function PostFinalizar([hashtable]$payload) {
  $bodyJ = $payload | ConvertTo-Json -Depth 6
  try {
    $r = Invoke-WebRequest ($apiUrl + '/api/public/cadastro/finalizar') -Method Post -Body $bodyJ -ContentType 'application/json' -UseBasicParsing
    return [pscustomobject]@{ Http = [int]$r.StatusCode; Json = ($r.Content | ConvertFrom-Json) }
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

Log ('R96A_DRILL data=' + (Get-Date -Format 'yyyyMMdd-HHmmss') + ' db=' + $db + ' porta=' + $porta)
$global:ApiProc = $null
$global:PidApi = $null

# ---------------------------------------------------------------- ESTAGEM 1
if (-not (Test-Path $apiDll)) { Falhar ('DLL da API ausente: repo=' + $repo + ' dll=' + $apiDll) }
if (-not (PortaLivre $porta)) { Falhar ('porta ' + $porta + ' ocupada antes do inicio') }
Log 'PORTAS_LIVRES_INICIO=SIM'
$env:ConnectionStrings__Default = 'Host=127.0.0.1;Port=5432;Database=' + $db + ';Username=postgres;Password=123456'
$env:Jwt__Key = 'r96-drill-dev-jwt-key-0123456789-abcd'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ApiSettings__BaseUrl = $apiUrl
$env:PlantaoProApi__BaseUrl = $apiUrl
$global:ApiProc = Start-Process -FilePath 'dotnet' -ArgumentList ('"' + $apiDll + '" --urls http://localhost:' + $porta) -WorkingDirectory (Join-Path $repo 'backend/PlantaoPro.Api') -WindowStyle Hidden -PassThru
if (-not (ApiAguardarNoAr 60)) { Falhar 'API nao ficou no ar em 60s' }
$owner = Get-NetTCPConnection -State Listen -LocalPort $porta -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $owner) { Falhar 'API no ar mas sem dono da porta' }
$global:PidApi = [int]$owner.OwningProcess
Log ('ESTAGEM_1_API_NO_AR ok=SIM pid=' + $global:PidApi)

# ---------------------------------------------------------------- ESTAGEM 2 (catalogo + FAQ v2375)
$c = Invoke-WebRequest ($apiUrl + '/api/public/planos') -UseBasicParsing
$planos = (@($c.Content | ConvertFrom-Json).data)
if ($planos.Count -eq 0) { Falhar 'catalogo publico de planos vazio' }
$plano = $planos | Where-Object { $_.destaque -eq $true } | Select-Object -First 1
if (-not $plano) { $plano = $planos[0] }
$faqr = Invoke-WebRequest ($apiUrl + '/api/public/planos/faq') -UseBasicParsing
$faqTexto = (@($faqr.Content | ConvertFrom-Json).data | ForEach-Object { $_.pergunta }) -join ' / '
if ($faqTexto -notlike '*funciona o teste*') { Falhar 'FAQ publico sem a pergunta do teste gratis (v2375 nao aplicada?)' }
Log ('ESTAGEM_2_CATALOGO_OK planos=' + $planos.Count + ' plano=' + $plano.nome + ' faq_trial=SIM')

# -------------------------------------------------- limpeza para rerun previsivel
foreach ($cj in @('96967788000199', '96965566000144', '96960000000101', '96960000000102', '96960000000103', '96960000000104')) { LimparMundoPorCnpj $cj | Out-Null }
Log 'PRE_LIMPEZA_RERUN ok=SIM'

# ---------------------------------------------------------------- PA_TRIAL
$t1email = 'r96-drill-trial@plantaopro-local.test'
$r1 = PostFinalizar @{
  empresa      = @{ nomeFantasia = 'R96 Drill Trial'; razaoSocial = 'R96 Drill Trial LTDA'; cnpj = '96967788000199'; segmento = 'Clinica'; cidade = 'Recife'; uf = 'PE'; emailCorporativo = $t1email }
  plano        = @{ planoId = [string]$plano.id; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
  usuarioAdmin = @{ nome = 'R96 Trial Admin'; email = $t1email; cargo = 'Gestora'; senha = 'R96-trial-2026#Kp9z' }
  statusAssinatura = 'TRIAL'
  diasTrial      = 14
}
if ($r1.Http -ne 200) { Falhar ('PA_TRIAL http ' + $r1.Http + ' ' + $r1.Json.message) }
$d1 = $r1.Json.data
Log ('PA_TRIAL_CRIADO solicitacao=' + $d1.solicitacaoId + ' tenant=' + $d1.tenantId + ' cliente=' + $d1.clienteId + ' assinatura=' + $d1.assinaturaId)

$chkStatus   = Psq ("select coalesce(status,'') from plantaopro.assinaturas where id='" + $d1.assinaturaId + "'")
$chkTrialFim = Psq ("select (a.data_trial_fim::date = (current_date + 14)) from plantaopro.assinaturas a where a.id='" + $d1.assinaturaId + "'")
$chkFim      = Psq ("select (a.data_fim::date = (current_date + 14 + 30)) from plantaopro.assinaturas a where a.id='" + $d1.assinaturaId + "'")
$chkCobranca = Psq ("select count(*) from plantaopro.cadastro_cliente_pagamentos_iniciais p where p.assinatura_id='" + $d1.assinaturaId + "'")
$chkLgpd     = Psq ("select count(*) from plantaopro.lgpd_consentimentos l where l.tenant_id='" + $d1.tenantId + "' and l.base_legal='consentimento' and l.aceito=true")
$chkOnb      = Psq ("select coalesce(status,'') from plantaopro.tenant_onboarding where tenant_id='" + $d1.tenantId + "'")
$chkChecklist= Psq ("select count(*) from plantaopro.tenant_onboarding_checklist c where c.tenant_id='" + $d1.tenantId + "'")
if ($chkStatus -ne 'TRIAL')        { Falhar ('PA_TRIAL status=' + $chkStatus) }
if ($chkTrialFim -ne 't')          { Falhar 'PA_TRIAL data_trial_fim != hoje+14' }
if ($chkFim -ne 't')               { Falhar 'PA_TRIAL data_fim != hoje+44 (teste+30d conversao)' }
if ($chkCobranca -ne '0')          { Falhar ('PA_TRIAL cobranca inicial inesperada=' + $chkCobranca) }
if ($chkLgpd -ne '1')              { Falhar 'PA_TRIAL LGPD consentimento ausente' }
if ($chkOnb -ne 'EM_ANDAMENTO')    { Falhar ('PA_TRIAL onboarding=' + $chkOnb) }
if ([int]$chkChecklist -lt 1)      { Falhar 'PA_TRIAL checklist vazio' }
Set-Content -Path (Join-Path $PSScriptRoot 'r96a_t1_trial_banco.txt') -Value @(
  'assinatura_id=' + $d1.assinaturaId,
  'status=' + $chkStatus,
  'data_inicio=' + (Psq ("select data_inicio::text from plantaopro.assinaturas where id='" + $d1.assinaturaId + "'")),
  'data_trial_fim=' + (Psq ("select data_trial_fim::text from plantaopro.assinaturas where id='" + $d1.assinaturaId + "'")),
  'data_fim=' + (Psq ("select data_fim::text from plantaopro.assinaturas where id='" + $d1.assinaturaId + "'")),
  'cobranca_inicial_linhas=' + $chkCobranca,
  'lgpd_base_legal=consentimento aceito=true linhas=' + $chkLgpd,
  'onboarding_status=' + $chkOnb,
  'checklist_linhas=' + $chkChecklist,
  'tenant_nome=' + (Psq ("select nome from plantaopro.tenants where id='" + $d1.tenantId + "'"))
) -Encoding UTF8
Log ('PA_TRIAL_OK status=TRIAL trial_fim_ok=SIM fim_conv_ok=SIM cobranca_inicial=0 lgpd=consentimento onboarding=EM_ANDAMENTO checklist=' + $chkChecklist)

# funcional: login do dono + onboarding/status + minha-assinatura
$tok1 = FazerLogin $t1email 'R96-trial-2026#Kp9z'
$ro = Invoke-WebRequest ($apiUrl + '/api/onboarding/status') -Headers @{ Authorization = 'Bearer ' + $tok1 } -UseBasicParsing
$jo = ($ro.Content | ConvertFrom-Json)
if ([int]$ro.StatusCode -ne 200) { Falhar ('onboarding/status http ' + [int]$ro.StatusCode) }
$rm = Invoke-WebRequest ($apiUrl + '/api/minha-assinatura') -Headers @{ Authorization = 'Bearer ' + $tok1 } -UseBasicParsing
$jm = ($rm.Content | ConvertFrom-Json).data
if ($jm.status -ne 'TRIAL') { Falhar ('minha-assinatura status=' + $jm.status) }
if (-not $jm.dataTrialFim)   { Falhar 'minha-assinatura dataTrialFim ausente' }
if (-not $jm.dataFim)        { Falhar 'minha-assinatura dataFim ausente' }
Log ('FUNCIONAL_API_OK login_dono=SIM onboarding_http=200 minha_assinatura_http=200 minha_status=TRIAL dataTrialFim=' + ([datetime]$jm.dataTrialFim).ToString('yyyy-MM-dd') + ' dataFim=' + ([datetime]$jm.dataFim).ToString('yyyy-MM-dd'))

# ---------------------------------------------------------------- PA_DUPLICATA
$r2 = PostFinalizar @{
  empresa      = @{ nomeFantasia = 'R96 Drill Trial Dup'; razaoSocial = 'R96 Drill Trial Dup LTDA'; cnpj = '96967788000199'; cidade = 'Recife'; uf = 'PE'; emailCorporativo = 'r96-drill-dup@plantaopro-local.test' }
  plano        = @{ planoId = [string]$plano.id; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
  usuarioAdmin = @{ nome = 'R96 Dup Admin'; email = 'r96-drill-dup@plantaopro-local.test'; senha = 'R96-dup-2026#Kp9z' }
  statusAssinatura = 'TRIAL'
  diasTrial      = 30
}
$dupMsg = if ($r2.Json) { [string]$r2.Json.message } else { '' }
if ($r2.Http -ne 400) { Falhar ('PA_DUPLICATA http ' + $r2.Http + ' esperado 400') }
if ($dupMsg -notlike '*CNPJ*') { Falhar ('PA_DUPLICATA mensagem=' + $dupMsg) }
$dupResiduo = Psq ("select count(*) from plantaopro.usuarios where lower(email)=lower('r96-drill-dup@plantaopro-local.test')")
if ($dupResiduo -ne '0') { Falhar ('PA_DUPLICATA residuo=' + $dupResiduo) }
Log ('PA_DUPLICATA_OK http=400 msg_cnpj=SIM residuo=0')

# ---------------------------------------------------------------- PA_VALIDACAO
$casosV = @(
  @{ rotulo = 'dias_0';    payload_extra = @{ diasTrial = 0 } },
  @{ rotulo = 'dias_91';   payload_extra = @{ diasTrial = 91 } },
  @{ rotulo = 'dias_ausente'; payload_extra = @{} },
  @{ rotulo = 'status_ruim'; payload_extra = @{ diasTrial = 30; statusAssinatura = 'PILOTO' } }
)
$iV = 0
$residuosV = 0
foreach ($cv in $casosV) {
  $iV++
  $cnpjV = '969600000001' + ('{0:D2}' -f $iV)
  $plV = @{
    empresa      = @{ nomeFantasia = ('R96 Val ' + $iV); razaoSocial = ('R96 Val ' + $iV + ' LTDA'); cnpj = $cnpjV; cidade = 'Recife'; uf = 'PE'; emailCorporativo = ('r96-val-' + $iV + '@plantaopro-local.test') }
    plano        = @{ planoId = [string]$plano.id; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
    usuarioAdmin = @{ nome = ('R96 Val Admin ' + $iV); email = ('r96-val-' + $iV + '@plantaopro-local.test'); senha = 'R96-val-2026#Kp9z' }
    statusAssinatura = 'TRIAL'
  }
  foreach ($k in $cv.payload_extra.Keys) { $plV[$k] = $cv.payload_extra[$k] }
  $rv = PostFinalizar $plV
  $mv = if ($rv.Json) { [string]$rv.Json.message } else { '' }
  if ($rv.Http -ne 400) { Falhar ('PA_VALIDACAO ' + $cv.rotulo + ' http ' + $rv.Http) }
  if ($cv.rotulo -eq 'status_ruim') {
    if ($mv -notlike '*TRIAL ou ATIVA*') { Falhar ('PA_VALIDACAO status_ruim msg=' + $mv) }
  } elseif ($mv -notlike '*dias*') {
    Falhar ('PA_VALIDACAO ' + $cv.rotulo + ' msg=' + $mv)
  }
  $residuosV += [int](Psq ("select count(*) from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')='" + $cnpjV + "'"))
}
if ($residuosV -ne 0) { Falhar ('PA_VALIDACAO residuos=' + $residuosV) }
Log 'PA_VALIDACAO_OK casos=4 http_todos=400 residuos=0'

# ---------------------------------------------------------------- PA_ATIVA (status ausente = legado B5)
$t3email = 'r96-drill-ativa@plantaopro-local.test'
$r3 = PostFinalizar @{
  empresa      = @{ nomeFantasia = 'R96 Drill Ativa'; razaoSocial = 'R96 Drill Ativa LTDA'; cnpj = '96965566000144'; segmento = 'Clinica'; cidade = 'Olinda'; uf = 'PE'; emailCorporativo = $t3email }
  plano        = @{ planoId = [string]$plano.id; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
  usuarioAdmin = @{ nome = 'R96 Ativa Admin'; email = $t3email; cargo = 'Gestor'; senha = 'R96-ativa-2026#Kp9z' }
}
if ($r3.Http -ne 200) { Falhar ('PA_ATIVA http ' + $r3.Http + ' ' + $r3.Json.message) }
$d3 = $r3.Json.data
$aStatus   = Psq ("select coalesce(status,'') from plantaopro.assinaturas where id='" + $d3.assinaturaId + "'")
$aTrialNul = Psq ("select (data_trial_fim is null) from plantaopro.assinaturas where id='" + $d3.assinaturaId + "'")
$aFimOk    = Psq ("select (data_fim::date between (current_date + 31) and (current_date + 61)) from plantaopro.assinaturas where id='" + $d3.assinaturaId + "'")
$aCobranca = Psq ("select coalesce(status,'') from plantaopro.cadastro_cliente_pagamentos_iniciais where assinatura_id='" + $d3.assinaturaId + "' limit 1")
if ($aStatus -ne 'ATIVA')        { Falhar ('PA_ATIVA status=' + $aStatus) }
if ($aTrialNul -ne 't')          { Falhar 'PA_ATIVA data_trial_fim deveria ser nula' }
if ($aFimOk -ne 't')             { Falhar 'PA_ATIVA data_fim fora da janela de 1 mes' }
if ($aCobranca -ne 'ABERTO')     { Falhar ('PA_ATIVA cobranca inicial=' + $aCobranca) }
$tok3 = FazerLogin $t3email 'R96-ativa-2026#Kp9z'
$rf = Invoke-WebRequest ($apiUrl + '/api/minha-assinatura/faturas') -Headers @{ Authorization = 'Bearer ' + $tok3 } -UseBasicParsing
$jf = @(($rf.Content | ConvertFrom-Json).data)
$fCad = $jf | Where-Object { $_.origem -eq 'CADASTRO' }
if ($jf.Count -lt 1 -or -not $fCad) { Falhar 'PA_ATIVA fatura CADASTRO nao visivel em minha-assinatura' }
Set-Content -Path (Join-Path $PSScriptRoot 'r96a_t3_ativa_banco.txt') -Value @(
  'assinatura_id=' + $d3.assinaturaId,
  'status=' + $aStatus,
  'data_trial_fim=null',
  'data_fim=' + (Psq ("select data_fim::text from plantaopro.assinaturas where id='" + $d3.assinaturaId + "'")),
  'cobranca_inicial_status=' + $aCobranca,
  'faturas_api_total=' + $jf.Count,
  'faturas_api_cadastro=' + @($fCad).Count
) -Encoding UTF8
Log ('PA_ATIVA_OK status=ATIVA trial_fim_nulo=SIM fim_1mes=SIM cobranca=ABERTO faturas_api=' + $jf.Count + ' login_dono=SIM')

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
