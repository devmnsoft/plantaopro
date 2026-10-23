# R102 - regulacao da triagem + PWA triagem/recepcao + regras de cobranca adm360
# Execucao canonica: sobe API :52086 + Web :52085 (DLLs Release, banco vivo plantaopro_test)
# e prova o contrato da rodada de ponta a ponta:
#   MIGRACAO         aplica v2379 (colunas da regulacao na triagem + limpeza das orfaas
#                    adm360 + validate constraint dos dois FKs NOT VALID herdados da R95)
#   REG_*            mundo clinico pelo funil real + fixture deterministico
#                    (medico/paciente/agendamento/triagem): 409 sem check-in,
#                    400 sem orientacao, regulacao com sucesso (agendamento ->
#                    AGUARDANDO_CONSULTA + medico de destino + encaminhamento CONSULTA
#                    com orientacao + historico + auditoria) e a resposta canonica em T1
#   BIL_*            4 mundos deterministicos: gerar-mensal com ciclo ANUAL (so mes do
#                    aniversario data_inicio), gate data_fim (nenhuma fatura apos o fim),
#                    expiracao de trials vencidos (idempotente) e alterar-plano com
#                    recalculo PrecosPlano + reinicio de ciclo (ATIVA/TRIAL)
#   PWA_*            service-worker v2 (network-first navegacoes + fallback offline),
#                    /offline.html em PT, prompt de instalacao e o layout referenciando tudo
#   WEB_REG          tela /Triagem/Edit/{id} renderiza o botao Regularizar ativo
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
$saida = Join-Path $PSScriptRoot 'r102a_drill_saida.txt'
$t1arquivo = Join-Path $PSScriptRoot 'r102a_t1_regularizar_api.json'
$cnpjReg  = '99009009000257'
$cnpjM    = '99011011000303'
$cnpjA    = '99012012000326'
$cnpjF    = '99014014000372'
$cnpjT    = '99013013000349'
$emailReg = 'r102-drill-reg@plantaopro-local.test'
$senhaReg = 'R102-reg-2026#Kp9z'

# Captura de console dos apps (diagnostico em caso de falha; anexada a saida no Falhar)
$global:LogApiOut  = Join-Path $PSScriptRoot 'r102a_api_stdout.log'
$global:LogApiErr  = Join-Path $PSScriptRoot 'r102a_api_stderr.log'
$global:LogWebOut  = Join-Path $PSScriptRoot 'r102a_web_stdout.log'
$global:LogWebErr  = Join-Path $PSScriptRoot 'r102a_web_stderr.log'

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
  foreach ($par in @(@('API STDOUT', $global:LogApiOut), @('API STDERR', $global:LogApiErr), @('WEB STDOUT', $global:LogWebOut), @('WEB STDERR', $global:LogWebErr))) {
    if (Test-Path $par[1]) {
      Log ('--- ' + $par[0] + ' TAIL ---')
      Get-Content $par[1] -Tail 25 -ErrorAction SilentlyContinue | ForEach-Object { Log $_ }
      Log ('--- FIM TAIL ' + $par[0] + ' ---')
    }
  }
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

# ------------------------------------------------- limpeza por CNPJ (negocio clinico + mundo)
function CliIdDe([string]$cnpj) {
  return "(select id from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')='" + $cnpj + "')"
}
function LimparClinicoPorCnpj([string]$cnpj) {
  $cid = CliIdDe $cnpj
  $sql = "delete from plantaopro.triagem_historico where triagem_id in (select id from plantaopro.triagens where cliente_id=" + $cid + ");"
  $sql += "delete from plantaopro.triagem_encaminhamentos where triagem_id in (select id from plantaopro.triagens where cliente_id=" + $cid + ");"
  $sql += "delete from plantaopro.triagens where cliente_id=" + $cid + ";"
  $sql += "delete from plantaopro.paciente_consentimentos where paciente_id in (select id from plantaopro.pacientes where cliente_id=" + $cid + ");"
  $sql += "delete from plantaopro.paciente_documentos where paciente_id in (select id from plantaopro.pacientes where cliente_id=" + $cid + ");"
  $sql += "delete from plantaopro.paciente_enderecos where paciente_id in (select id from plantaopro.pacientes where cliente_id=" + $cid + ");"
  $sql += "delete from plantaopro.paciente_contatos where paciente_id in (select id from plantaopro.pacientes where cliente_id=" + $cid + ");"
  $sql += "delete from plantaopro.paciente_historico where paciente_id in (select id from plantaopro.pacientes where cliente_id=" + $cid + ");"
  $sql += "delete from plantaopro.agendamento_checkins where agendamento_id in (select id from plantaopro.agendamentos where cliente_id=" + $cid + ");"
  $sql += "delete from plantaopro.agendamento_historico where agendamento_id in (select id from plantaopro.agendamentos where cliente_id=" + $cid + ");"
  $sql += "delete from plantaopro.agendamentos where cliente_id=" + $cid + ";"
  $sql += "delete from plantaopro.pacientes where cliente_id=" + $cid + ";"
  $sql += "delete from plantaopro.medicos where cliente_id=" + $cid + ";"
  $err = Psq $sql
  if ($err -match 'ERRO') { Falhar ('LimparClinicoPorCnpj ' + $cnpj + ': ' + $err) }
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
function FunilCadastrar([string]$nome, [string]$cnpj, [string]$email, [string]$senha, [string]$uf) {
  $r = HttpRequest 'POST' ($apiUrl + '/api/public/cadastro/finalizar') '' @{
    empresa      = @{ nomeFantasia = $nome; razaoSocial = $nome + ' LTDA'; cnpj = $cnpj; segmento = 'Clinica'; cidade = 'Recife'; uf = $uf; telefone = '(81) 99999-1020'; emailCorporativo = $email }
    plano        = @{ planoId = $script:idPlano; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
    usuarioAdmin = @{ nome = $nome; email = $email; cargo = 'Gestor'; senha = $senha }
    statusAssinatura = 'TRIAL'
    diasTrial      = 5
  }
  if ($r.Http -ne 200) { Falhar ('FUNIL ' + $cnpj + ' http ' + $r.Http + ' ' + $(if ($r.Json) { $r.Json.message } else { 'sem corpo' })) }
  $assina = Psq ("select id from plantaopro.assinaturas where cliente_id=" + (CliIdDe $cnpj))
  if (-not $assina) { Falhar ('FUNIL ' + $cnpj + ' assinatura ausente') }
  return [pscustomobject]@{ Tenant = [string]$r.Json.data.tenantId; Assinatura = [string]$assina; Cliente = (CliIdDe $cnpj) }
}
function FaturasDe([string]$cid, [string]$mes) {
  return Psq ("select count(*) || '|' || coalesce(sum(cast(valor as numeric(10,2)))::text,'0') from plantaopro.faturas_saas where reg_status='A' and cliente_id=" + $cid + " and competencia=date '" + $mes + "-01'")
}
function WebObter([string]$rota, $sess) {
  for ($w = 0; $w -lt 6; $w++) {
    try { $q = Invoke-WebRequest -Uri ($webUrl + $rota) -WebSession $sess -UseBasicParsing -TimeoutSec 60 -MaximumRedirection 10; if ([int]$q.StatusCode -eq 200) { return $q } }
    catch { Start-Sleep 2 }
  }
  return $null
}

Log ('R102A_DRILL data=' + (Get-Date -Format 'yyyyMMdd-HHmmss') + ' db=' + $db + ' api=' + $porta + ' web=' + $webPorta)

# ---------------------------------------------------------------- ESTAGEM 1: portas + apps
if (-not (Test-Path $apiDll)) { Falhar ('DLL da API ausente: ' + $apiDll) }
if (-not (Test-Path $webDll)) { Falhar ('DLL da Web ausente: ' + $webDll) }
if (-not (PortaLivre $porta) -or (-not (PortaLivre $webPorta))) { Falhar ('portas ocupadas antes do inicio (api=' + $porta + ' web=' + $webPorta + ')') }
Log 'PORTAS_LIVRES_INICIO=SIM'

$env:ConnectionStrings__Default = 'Host=127.0.0.1;Port=5432;Database=' + $db + ';Username=postgres;Password=123456'
$env:Jwt__Key = 'r102-drill-dev-jwt-key-0123456789-abcd'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
# regra de casa (R12): com execucao direta da DLL o content root muda - BaseUrl explicito
$env:ApiSettings__BaseUrl = $apiUrl
$env:PlantaoProApi__BaseUrl = $apiUrl

Remove-Item $global:LogApiOut,$global:LogApiErr,$global:LogWebOut,$global:LogWebErr -ErrorAction SilentlyContinue
$apiProc = Start-Process -FilePath 'dotnet' -ArgumentList ('"' + $apiDll + '" --urls http://localhost:' + $porta) -WorkingDirectory (Join-Path $repo 'backend/PlantaoPro.Api') -WindowStyle Hidden -PassThru -RedirectStandardOutput $global:LogApiOut -RedirectStandardError $global:LogApiErr
if (-not (ApiAguardarNoAr 90)) { Falhar 'API nao ficou no ar em 90s' }
$global:PidApi = [int]$apiProc.Id
Log ('ESTAGEM_1_API_NO_AR ok=SIM pid=' + $global:PidApi)

$webProc = Start-Process -FilePath 'dotnet' -ArgumentList ('"' + $webDll + '" --urls http://localhost:' + $webPorta) -WorkingDirectory (Join-Path $repo 'backend/PlantaoPro.Web') -WindowStyle Hidden -PassThru -RedirectStandardOutput $global:LogWebOut -RedirectStandardError $global:LogWebErr
if (-not (WebAguardarNoAr 90)) { Falhar 'Web nao ficou no ar em 90s' }
$global:PidWeb = [int]$webProc.Id
Log ('ESTAGEM_1_WEB_NO_AR ok=SIM pid=' + $global:PidWeb)

# -------------------------------------------------- limpeza para rerun previsivel
Psq ("delete from plantaopro.usuario_permissoes_especiais where usuario_id in (select id from plantaopro.usuarios where email='" + $emailReg + "')") | Out-Null
foreach ($cj in @($cnpjReg, $cnpjM, $cnpjA, $cnpjF, $cnpjT)) { LimparClinicoPorCnpj $cj | Out-Null; LimparMundoPorCnpj $cj | Out-Null }
LimparPlanoPorCodigo 'R102_PLANO_DRILL' | Out-Null
LimparPlanoPorCodigo 'R102_PLANO_B' | Out-Null
Log 'PRE_LIMPEZA_RERUN ok=SIM'

$tokSup = FazerLogin 'superadmin@plantaopro.local' 'Super@123456'

# ---------------------------------------------------------------- MIGRACAO (v2379: regulacao + integridade adm360)
$migFile = Join-Path $repo 'database/migrations/2026_10_v2379_r102_regulacao_triagem_integridade_adm360.sql'
# PS 5.1: stderr nativo do psql vira registro de erro no pipeline (com EAP=Stop lanca);
# com EAP=Continue + ON_ERROR_STOP=1 o EXIT CODE do psql e o arbitro; o texto e diagnostico
# (nao casar com 'ERRO': o rendering dos registros ecoa 'ON_ERROR_STOP' e falsaria o match).
$prevEap = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
try {
  $migOut = (& $psql -h localhost -U postgres -d $db -X -q -v ON_ERROR_STOP=1 -f $migFile 2>&1 | Out-String)
  $migExit = $LASTEXITCODE
} finally { $ErrorActionPreference = $prevEap }
if ($migExit -ne 0) {
  $resumo = ($migOut -replace '\s+', ' ')
  if ($resumo.Length -gt 300) { $resumo = $resumo.Substring(0, 300) }
  Falhar ('MIGRACAO v2379 exit=' + $migExit + ': ' + $resumo)
}
$nCols = Psq ("select count(*) from information_schema.columns where table_schema='plantaopro' and table_name='triagem_encaminhamentos' and column_name in ('medico_id','observacao')")
if ($nCols -ne '2') { Falhar ('MIGRACAO colunas regulacao=' + $nCols + ' esperado 2') }
$vVale = Psq ("select coalesce(convalidated::text,'ausente') from pg_constraint where conname='adm360_vale_itens_vale_id_fkey' limit 1")
$vEnv  = Psq ("select coalesce(convalidated::text,'ausente') from pg_constraint where conname='adm360_cotacao_envios_resposta_id_fkey' limit 1")
if ($vVale -ne 'true' -or $vEnv -ne 'true') { Falhar ('MIGRACAO constraints validadas vale=' + $vVale + ' envio=' + $vEnv + ' esperado true/true') }
$orfas = Psq ("select (select count(*) from plantaopro.adm360_vale_itens i where not exists (select 1 from plantaopro.adm360_vales v where v.id=i.vale_id)) || '|' || (select count(*) from plantaopro.adm360_cotacao_envios e where e.resposta_id is not null and not exists (select 1 from plantaopro.adm360_cotacao_respostas r where r.id=e.resposta_id))")
if ($orfas -ne '0|0') { Falhar ('MIGRACAO orfaas restantes=' + $orfas + ' esperado 0|0') }
Log 'MIGRACAO_OK v2379 colunas_regulacao=2 fk_validadas=t orfaas=0|0'

# ---------------------------------------------------------------- PLANO (governanca)
$rPlano = HttpRequest 'POST' ($apiUrl + '/api/planos') $tokSup @{
  nome = 'R102 Plano Drill'
  descricao = 'Plano do drill r102 (regulacao + regras de cobranca)'
  valorMensal = 100.00
  limiteMedicos = 0; limiteHospitais = 0; limitePlantoesMes = 0
  permiteMobile = $false; permiteBi = $false; permiteRelatoriosAvancados = $false; permiteIntegracoes = $false
  codigo = 'r102_plano_drill'
  slug = 'r102-plano-drill'
  valorAnual = 1188.00
  publico = $true; destaque = $false; ordem = 961; permiteWhiteLabel = $false
  permiteTrial = $true
  diasTrial = 5
}
if ($rPlano.Http -ne 200) { Falhar ('PLANO drill http ' + $rPlano.Http + ' ' + $(if ($rPlano.Json) { $rPlano.Json.message } else { 'sem corpo' })) }
$script:idPlano = [string]$rPlano.Json.data

$rPlanoB = HttpRequest 'POST' ($apiUrl + '/api/planos') $tokSup @{
  nome = 'R102 Plano B'
  descricao = 'Plano de destino do drill r102 (upgrade em alterar-plano)'
  valorMensal = 250.00
  limiteMedicos = 0; limiteHospitais = 0; limitePlantoesMes = 0
  permiteMobile = $false; permiteBi = $false; permiteRelatoriosAvancados = $false; permiteIntegracoes = $false
  codigo = 'r102_plano_b'
  slug = 'r102-plano-b'
  valorAnual = 2880.00
  publico = $true; destaque = $false; ordem = 962; permiteWhiteLabel = $false
  permiteTrial = $true
  diasTrial = 5
}
if ($rPlanoB.Http -ne 200) { Falhar ('PLANO b http ' + $rPlanoB.Http + ' ' + $(if ($rPlanoB.Json) { $rPlanoB.Json.message } else { 'sem corpo' })) }
$idPlanoB = [string]$rPlanoB.Json.data
Log ('PLANO_OK drill=' + $script:idPlano + ' b=' + $idPlanoB + ' mensal=100/250 anual=1188/2880')

# ---------------------------------------------------------------- REG_MUNDO (funil + fixture clinico)
$mReg = FunilCadastrar 'R102 Reg Clinic' $cnpjReg $emailReg $senhaReg 'PE'

# O gate clinico (Saude360ModuleFilter) exige o modulo SAUDE360 contratado no tenant
# (modulo_efetivo) - a rota real de governanca habilita por tenant (origem CONTRATO).
$rMod = HttpRequest 'GET' ($apiUrl + '/api/modulos') $tokSup $null
if ($rMod.Http -ne 200) { Falhar ('REG_MODULO listagem http ' + $rMod.Http) }
$idS360 = $null
foreach ($m in @($rMod.Json.data)) { if ([string]$m.codigo -eq 'SAUDE360') { $idS360 = [string]$m.id } }
if (-not $idS360) { Falhar 'REG_MODULO modulo SAUDE360 nao encontrado no catalogo' }
$rHab = HttpRequest 'POST' ($apiUrl + '/api/modulos/' + $idS360 + '/habilitar-tenant') $tokSup @{ tenantId = $mReg.Tenant; precoContratado = 0; limiteContratado = 0; motivo = 'Drill R102 - regulacao da triagem' }
if ($rHab.Http -ne 200) { Falhar ('REG_MODULO habilitar http ' + $rHab.Http + ' ' + $(if ($rHab.Json) { $rHab.Json.message } else { 'sem corpo' })) }
$nEfet = Psq ("select coalesce(plantaopro.modulo_efetivo('" + $mReg.Tenant + "','SAUDE360'),false)::text")
if ($nEfet -ne 'true') { Falhar ('REG_MODULO efetivo=' + $nEfet + ' esperado true') }
Log ('REG_MODULO_OK saude360 tenant=' + $mReg.Tenant + ' origem=CONTRATO')

# O dono do mundo nao tem grants TRIAGEM.* no perfil ADMINISTRADOR_CLIENTE (lacuna de
# catalogo conhecida; o guard web decide por grants emitidos no login) - o drill concede
# TRIAGEM.EDITAR por usuario_permissoes_especiais (mesmo padrao do RELATORIOS.VER da R101).
$idPermEditar = Psq ("select id from plantaopro.permissoes where codigo='TRIAGEM.EDITAR'")
if (-not $idPermEditar) { Falhar 'REG_PERMISSAO permissao TRIAGEM.EDITAR ausente no catalogo' }
$errPer = Psq ("insert into plantaopro.usuario_permissoes_especiais(tenant_id,usuario_id,permissao_id,permitido,justificativa) select t.id, u.id, '" + $idPermEditar + "', true, 'Drill R102 - regulacao da triagem' from plantaopro.usuarios u join plantaopro.tenants t on t.id=u.tenant_id where u.email='" + $emailReg + "' and not exists (select 1 from plantaopro.usuario_permissoes_especiais x where x.usuario_id=u.id and x.permissao_id='" + $idPermEditar + "' and x.reg_status='A')")
if ($errPer -match 'ERRO') { Falhar ('REG_PERMISSAO ' + $errPer) }
$nPer = Psq ("select count(*) from plantaopro.usuario_permissoes_especiais x where x.reg_status='A' and x.permitido=true and x.permissao_id='" + $idPermEditar + "' and exists (select 1 from plantaopro.usuarios u where u.id=x.usuario_id and u.email='" + $emailReg + "')")
if ($nPer -ne '1') { Falhar ('REG_PERMISSAO grants=' + $nPer + ' esperado 1') }
Log 'REG_PERMISSAO_OK TRIAGEM.EDITAR via usuario_permissoes_especiais'

$tokReg = FazerLogin $emailReg $senhaReg

$tplClinico = @'
insert into plantaopro.medicos(nome,cliente_id,status,reg_status) select 'R102 Reg Medico', {CID}, 'ATIVO','A';
insert into plantaopro.pacientes(tenant_id,codigo,nome,status,criado_em,atualizado_em,cliente_id,consentimento_lgpd,reg_status,reg_date)
select null,'R102-PAC','R102 Paciente Reg','ATIVO',now(),now(),{CID},true,'A',now();
insert into plantaopro.agendamentos(cliente_id,paciente_id,medico_id,data_inicio,data_fim,tipo,status,reg_date,reg_status)
select {CID}, (select id from plantaopro.pacientes where codigo='R102-PAC'), (select id from plantaopro.medicos where cliente_id={CID} order by reg_date desc limit 1),
 now() - interval '1 hour', now() + interval '2 hours', 'CONSULTA', 'AGENDADO', now(), 'A';
insert into plantaopro.triagens(cliente_id,paciente_id,agendamento_id,classificacao_risco,queixa_principal,status,reg_date,reg_status)
select {CID}, (select id from plantaopro.pacientes where codigo='R102-PAC'), (select id from plantaopro.agendamentos where cliente_id={CID} order by reg_date desc limit 1),
 null, 'Dor toracica leve', 'AGUARDANDO', now(), 'A';
'@
$sqlFix = $tplClinico.Replace('{CID}', (CliIdDe $cnpjReg))
$err = Psq $sqlFix
if ($err -match 'ERRO') { Falhar ('REG_FIXTURE ' + $err + ' --- SQL --- ' + $sqlFix) }

$idMed = Psq ("select id from plantaopro.medicos where cliente_id=" + (CliIdDe $cnpjReg))
$idPac = Psq ("select id from plantaopro.pacientes where codigo='R102-PAC'")
$idAgend = Psq ("select id from plantaopro.agendamentos where cliente_id=" + (CliIdDe $cnpjReg))
$idTriagem = Psq ("select id from plantaopro.triagens where cliente_id=" + (CliIdDe $cnpjReg))
if (-not $idMed -or -not $idPac -or -not $idAgend -or -not $idTriagem) { Falhar 'REG_FIXTURE ids incompletos' }
Log ('REG_FIXTURE_OK medico=' + $idMed + ' paciente=' + $idPac + ' agendamento=' + $idAgend + ' triagem=' + $idTriagem)

$payloadReg = @{
  AtendimentoId = [guid]$idAgend
  PacienteId    = [guid]$idPac
  MedicoId      = [guid]$idMed
  QueixaPrincipal = 'Dor toracica leve'
  PressaoSistolica = 120
  PressaoDiastolica = 80
  FrequenciaCardiaca = 76
  ClassificacaoRisco = 'AMARELA'
  Observacoes = 'Regulado para clinico geral: reavaliar PA em 1h.'
}

# 409: sem check-in nao ha paciente em fluxo para regular
$r409 = HttpRequest 'POST' ($apiUrl + '/api/triagens/' + $idTriagem + '/regularizar') $tokReg $payloadReg
if ($r409.Http -ne 409) { Falhar ('REG_409 http ' + $r409.Http + ' ' + $r409.Content) }
if (-not ($r409.Content -like '*check-in*')) { Falhar ('REG_409 motivo check-in ausente: ' + $r409.Content) }
Log 'REG_409_OK http=409 motivo=sem_check-in'

$err = Psq ("update plantaopro.agendamentos set status='CHECKIN_REALIZADO', updated_at=now() where id='" + $idAgend + "'")
if ($err -match 'ERRO') { Falhar ('REG_CHECKIN ' + $err) }

# 400: regulacao sem orientacao registrada
$payloadSemObs = $payloadReg.Clone()
$payloadSemObs.Observacoes = ''
$r400 = HttpRequest 'POST' ($apiUrl + '/api/triagens/' + $idTriagem + '/regularizar') $tokReg $payloadSemObs
if ($r400.Http -ne 400) { Falhar ('REG_400 http ' + $r400.Http + ' ' + $r400.Content) }
if (-not ($r400.Content -like '*registrada*')) { Falhar ('REG_400 motivo orientacao ausente: ' + $r400.Content) }
Log 'REG_400_OK http=400 motivo=sem_orientacao'

# ---------------------------------------------------------------- REG_SUCESSO (regulacao canonica + T1)
$nAudAntes = [int](Psq ("select count(*) from plantaopro.auditoria_acoes_criticas where entidade_id='" + $idTriagem + "' and lower(acao) like '%regularizar%'"))
$rReg = HttpRequest 'POST' ($apiUrl + '/api/triagens/' + $idTriagem + '/regularizar') $tokReg $payloadReg
if ($rReg.Http -ne 200) { Falhar ('REG_SUCESSO http ' + $rReg.Http + ' ' + $rReg.Content) }
if (-not [bool]$rReg.Json.success) { Falhar ('REG_SUCESSO sucesso=NAO ' + [string]$rReg.Json.message) }
[IO.File]::WriteAllText($t1arquivo, $rReg.Content, (New-Object System.Text.UTF8Encoding($false)))

$agStatus = Psq ("select coalesce(status,'') from plantaopro.agendamentos where id='" + $idAgend + "'")
$agMed    = Psq ("select coalesce(medico_id::text,'') from plantaopro.agendamentos where id='" + $idAgend + "'")
$triStatus = Psq ("select coalesce(status,'') from plantaopro.triagens where id='" + $idTriagem + "'")
if ($agStatus -ne 'AGUARDANDO_CONSULTA') { Falhar ('REG_SUCESSO agendamento.status=' + $agStatus) }
if ($agMed -ne $idMed) { Falhar ('REG_SUCESSO agendamento.medico_id=' + $agMed + ' esperado ' + $idMed) }
if ($triStatus -ne 'FINALIZADA') { Falhar ('REG_SUCESSO triagem.status=' + $triStatus) }

$enc = Psq ("select count(*) || '|' || coalesce(string_agg(medico_id::text,' '),'-') || '|' || count(*) filter (where orientacao_ok) from (select medico_id, case when observacao like '%Regulado%' then true else false end orientacao_ok from plantaopro.triagem_encaminhamentos where triagem_id='" + $idTriagem + "' and reg_status='A' and destino='CONSULTA') e")
$parts = $enc.Split('|')
if ($parts[0] -ne '1') { Falhar ('REG_SUCESSO encaminhamentos=' + $parts[0] + ' esperado 1') }
if ($parts[1] -ne $idMed) { Falhar ('REG_SUCESSO encaminhamento.medico_id=' + $parts[1] + ' esperado ' + $idMed) }
if ($parts[2] -ne '1') { Falhar ('REG_SUCESSO encaminhamento.observacao orientacao ausente') }

$nHist = Psq ("select count(*) from plantaopro.triagem_historico where triagem_id='" + $idTriagem + "' and acao='regularizar' and detalhes->>'orientacao' like '%Regulado%'")
if ($nHist -ne '1') { Falhar ('REG_SUCESSO historico regularizar=' + $nHist + ' esperado 1') }
$nAudDepois = [int](Psq ("select count(*) from plantaopro.auditoria_acoes_criticas where entidade_id='" + $idTriagem + "' and lower(acao) like '%regularizar%'"))
if ($nAudDepois -lt ($nAudAntes + 1)) { Falhar ('REG_SUCESSO auditoria delta=' + ($nAudDepois - $nAudAntes) + ' esperado >=1') }
Log ('REG_SUCESSO_OK http=200 agendamento=AGUARDANDO_CONSULTA medico_destino=SIM encaminhamento=1 historico=1 auditoria=+' + ($nAudDepois - $nAudAntes))
Log 'T1_DUMP_OK arquivo=r102a_t1_regularizar_api.json acao=regularizar triagem=FINALIZADA'

# ---------------------------------------------------------------- BIL_MUNDOS (4 mundos deterministicos)
$mM = FunilCadastrar 'R102 Bil Mensal' $cnpjM 'r102-drill-m@plantaopro-local.test' 'R102-m-2026#Kp9z' 'PE'
$mA = FunilCadastrar 'R102 Bil Anual' $cnpjA 'r102-drill-a@plantaopro-local.test' 'R102-a-2026#Kp9z' 'PE'
$mF = FunilCadastrar 'R102 Bil FimPeriodo' $cnpjF 'r102-drill-f@plantaopro-local.test' 'R102-f-2026#Kp9z' 'PE'
$mT = FunilCadastrar 'R102 Bil Trial' $cnpjT 'r102-drill-t@plantaopro-local.test' 'R102-t-2026#Kp9z' 'PE'

$err = Psq ("update plantaopro.assinaturas set status='ATIVA', periodicidade='MENSAL', valor_contratado=100, dia_vencimento=10, data_inicio=date '2026-09-01', data_fim=date '2027-10-01', reg_update=now() where id='" + $mM.Assinatura + "'")
if ($err -match 'ERRO') { Falhar ('BIL_CFG_M ' + $err) }
$err = Psq ("update plantaopro.assinaturas set status='ATIVA', periodicidade='ANUAL', valor_contratado=1188, dia_vencimento=10, data_inicio=date '2026-09-01', data_fim=date '2027-09-01', reg_update=now() where id='" + $mA.Assinatura + "'")
if ($err -match 'ERRO') { Falhar ('BIL_CFG_A ' + $err) }
$err = Psq ("update plantaopro.assinaturas set status='ATIVA', periodicidade='MENSAL', valor_contratado=100, dia_vencimento=10, data_inicio=date '2026-08-01', data_fim=date '2026-09-30', reg_update=now() where id='" + $mF.Assinatura + "'")
if ($err -match 'ERRO') { Falhar ('BIL_CFG_F ' + $err) }
$err = Psq ("update plantaopro.assinaturas set data_trial_fim=current_date - 2, reg_update=now() where id='" + $mT.Assinatura + "'")
if ($err -match 'ERRO') { Falhar ('BIL_CFG_T ' + $err) }
Log 'BIL_CFG_OK M=ATIVA_MENSAL A=ATIVA_ANUAL F=ATIVA_FIM_2026_09 T=TRIAL_VENCIDO'

# ---------------------------------------------------------------- GERAR_SET (2026-09: aniversariado anual + ultimo mes do fim)
$rGer1 = HttpRequest 'POST' ($apiUrl + '/api/faturamento-saas/gerar-mensal') $tokSup @{ competencia = '2026-09-15' }
if ($rGer1.Http -ne 200) { Falhar ('GERAR_SET http ' + $rGer1.Http + ' ' + $rGer1.Content) }
$fM = FaturasDe $mM.Cliente '2026-09'
$fA = FaturasDe $mA.Cliente '2026-09'
$fF = FaturasDe $mF.Cliente '2026-09'
$fT = FaturasDe $mT.Cliente '2026-09'
if ($fM -ne '1|100.00') { Falhar ('GERAR_SET mensal 09=' + $fM + ' esperado 1|100.00') }
if ($fA -ne '1|1188.00') { Falhar ('GERAR_SET anual 09 (mes do aniversario)=' + $fA + ' esperado 1|1188.00') }
if ($fF -ne '1|100.00') { Falhar ('GERAR_SET fim_periodo 09 (ultimo mes dentro)=' + $fF + ' esperado 1|100.00') }
if ($fT -ne '0|0') { Falhar ('GERAR_SET trial 09=' + $fT + ' esperado 0|0') }
Log 'GERAR_SET_OK 2026-09 M=1x100 A=1x1188 F=1x100 T=0'

# ---------------------------------------------------------------- GERAR_OUT_CYCLE (2026-10: anual fora do aniversario + periodo encerrado)
$rGer2 = HttpRequest 'POST' ($apiUrl + '/api/faturamento-saas/gerar-mensal') $tokSup @{ competencia = '2026-10-15' }
if ($rGer2.Http -ne 200) { Falhar ('GERAR_OUT_CYCLE http ' + $rGer2.Http + ' ' + $rGer2.Content) }
$fM10 = FaturasDe $mM.Cliente '2026-10'
$fA10 = FaturasDe $mA.Cliente '2026-10'
$fF10 = FaturasDe $mF.Cliente '2026-10'
if ($fM10 -ne '1|100.00') { Falhar ('GERAR_OUT_CYCLE mensal 10=' + $fM10 + ' esperado 1|100.00') }
if ($fA10 -ne '0|0') { Falhar ('GERAR_OUT_CYCLE anual 10 (fora do mes do aniversario)=' + $fA10 + ' esperado 0|0') }
if ($fF10 -ne '0|0') { Falhar ('GERAR_OUT_CYCLE fim_periodo 10 (apos data_fim)=' + $fF10 + ' esperado 0|0') }
Log 'GERAR_OUT_CYCLE_OK 2026-10 M=1x100 A=0 (ciclo anual) F=0 (gate data_fim)'

# ---------------------------------------------------------------- TRIAL_EXPIRAR (idempotente + historico)
$rExp1 = HttpRequest 'POST' ($apiUrl + '/api/assinaturas/trials/expirar-vencidos') $tokSup $null
if ($rExp1.Http -ne 200) { Falhar ('TRIAL_EXPIRAR http ' + $rExp1.Http + ' ' + $rExp1.Content) }
if (-not [bool]$rExp1.Json.success) { Falhar ('TRIAL_EXPIRAR sucesso=NAO ' + [string]$rExp1.Json.message) }
if ([int]$rExp1.Json.data -lt 1) { Falhar ('TRIAL_EXPIRAR expiradas=' + [string]$rExp1.Json.data + ' esperado >=1') }
$tStatus = Psq ("select coalesce(status,'') from plantaopro.assinaturas where id='" + $mT.Assinatura + "'")
$tHist = Psq ("select count(*) from plantaopro.assinatura_historico where assinatura_id='" + $mT.Assinatura + "' and acao='TRIAL_EXPIRADO' and status_novo='VENCIDA'")
if ($tStatus -ne 'VENCIDA') { Falhar ('TRIAL_EXPIRAR status=' + $tStatus + ' esperado VENCIDA') }
if ($tHist -ne '1') { Falhar ('TRIAL_EXPIRAR historico=' + $tHist + ' esperado 1') }
$rExp2 = HttpRequest 'POST' ($apiUrl + '/api/assinaturas/trials/expirar-vencidos') $tokSup $null
if ($rExp2.Http -ne 200) { Falhar ('TRIAL_EXPIRAR_2 http ' + $rExp2.Http + ' ' + $rExp2.Content) }
if ([int]$rExp2.Json.data -ne 0) { Falhar ('TRIAL_EXPIRAR_2 segunda chamada expiradas=' + [string]$rExp2.Json.data + ' esperado 0') }
Log ('TRIAL_EXPIRAR_OK primeira>=1 status=VENCIDA historico=1 segunda=0 expiradas_primeira=' + [string]$rExp1.Json.data)

# ---------------------------------------------------------------- ALTERAR_PLANO (PrecosPlano + ciclo + trial preserva datas)
$trialFimAntes = Psq ("select data_trial_fim::text from plantaopro.assinaturas where id='" + $mReg.Assinatura + "'")
$rAltM = HttpRequest 'POST' ($apiUrl + '/api/assinaturas/' + $mM.Assinatura + '/alterar-plano') $tokSup @{ planoId = $idPlanoB; justificativa = 'Drill R102 - upgrade mensal' }
if ($rAltM.Http -ne 200) { Falhar ('ALTERAR_PLANO_M http ' + $rAltM.Http + ' ' + $rAltM.Content) }
$chkM = Psq ("select plano_id::text || '|' || cast(valor_contratado as numeric(10,2))::text || '|' || (data_inicio = current_date)::text || '|' || ((data_fim - data_inicio)) from plantaopro.assinaturas where id='" + $mM.Assinatura + "'")
$pM = $chkM.Split('|')
if ($pM[0] -ne $idPlanoB) { Falhar ('ALTERAR_PLANO_M plano=' + $pM[0] + ' esperado ' + $idPlanoB) }
if ($pM[1] -ne '250.00') { Falhar ('ALTERAR_PLANO_M valor=' + $pM[1] + ' esperado 250.00') }
if ($pM[2] -ne 'true') { Falhar ('ALTERAR_PLANO_M data_inicio reiniciada=' + $pM[2] + ' esperado true') }
if ([int]$pM[3] -lt 28 -or [int]$pM[3] -gt 31) { Falhar ('ALTERAR_PLANO_M ciclo meses=' + $pM[3] + ' esperado 28..31') }

$rAltA = HttpRequest 'POST' ($apiUrl + '/api/assinaturas/' + $mA.Assinatura + '/alterar-plano') $tokSup @{ planoId = $idPlanoB; justificativa = 'Drill R102 - upgrade anual' }
if ($rAltA.Http -ne 200) { Falhar ('ALTERAR_PLANO_A http ' + $rAltA.Http + ' ' + $rAltA.Content) }
$chkA = Psq ("select plano_id::text || '|' || cast(valor_contratado as numeric(10,2))::text || '|' || ((data_fim - data_inicio)) from plantaopro.assinaturas where id='" + $mA.Assinatura + "'")
$pA = $chkA.Split('|')
if ($pA[0] -ne $idPlanoB) { Falhar ('ALTERAR_PLANO_A plano=' + $pA[0] + ' esperado ' + $idPlanoB) }
if ($pA[1] -ne '2880.00') { Falhar ('ALTERAR_PLANO_A valor anual=' + $pA[1] + ' esperado 2880.00') }
if ([int]$pA[2] -lt 364 -or [int]$pA[2] -gt 366) { Falhar ('ALTERAR_PLANO_A ciclo anos=' + $pA[2] + ' esperado 364..366') }

$trialFimDepois = Psq ("select data_trial_fim::text from plantaopro.assinaturas where id='" + $mReg.Assinatura + "'")
$rAltT = HttpRequest 'POST' ($apiUrl + '/api/assinaturas/' + $mReg.Assinatura + '/alterar-plano') $tokSup @{ planoId = $idPlanoB; justificativa = 'Drill R102 - trial em outro plano' }
if ($rAltT.Http -ne 200) { Falhar ('ALTERAR_PLANO_TRIAL http ' + $rAltT.Http + ' ' + $rAltT.Content) }
$chkT = Psq ("select plano_id::text || '|' || cast(valor_contratado as numeric(10,2))::text || '|' || coalesce(data_trial_fim::text,'-') || '|' || coalesce(status,'') from plantaopro.assinaturas where id='" + $mReg.Assinatura + "'")
$pT = $chkT.Split('|')
if ($pT[0] -ne $idPlanoB) { Falhar ('ALTERAR_PLANO_TRIAL plano=' + $pT[0]) }
if ($pT[1] -ne '250.00') { Falhar ('ALTERAR_PLANO_TRIAL valor=' + $pT[1] + ' esperado 250.00') }
if ($pT[2] -ne $trialFimAntes) { Falhar ('ALTERAR_PLANO_TRIAL data_trial_fim alterada (' + $trialFimAntes + ' -> ' + $pT[2] + ')') }
if ($pT[3] -ne 'TRIAL') { Falhar ('ALTERAR_PLANO_TRIAL status=' + $pT[3] + ' esperado TRIAL') }

$rAltX = HttpRequest 'POST' ($apiUrl + '/api/assinaturas/' + $mT.Assinatura + '/alterar-plano') $tokSup @{ planoId = $idPlanoB; justificativa = 'Drill R102 - vencida deve recusar' }
if ($rAltX.Http -ne 400) { Falhar ('ALTERAR_PLANO_VENCIDA http ' + $rAltX.Http + ' esperado 400 ' + $rAltX.Content) }
if (-not ($rAltX.Content -like '*ativas ou em trial*')) { Falhar ('ALTERAR_PLANO_VENCIDA motivo ausente: ' + $rAltX.Content) }
Log 'ALTERAR_PLANO_OK M=250+ciclo_1m A=2880+ciclo_1y TRIAL=datas_preservadas VENCIDA=400'

# ---------------------------------------------------------------- PWA (service worker v2 + offline + install prompt)
$rSw = WebObter '/service-worker.js' $null
if ($null -eq $rSw) { Falhar 'PWA_SW http != 200 apos 6 tentativas' }
foreach ($marcador in @('plantaopro-static-v2', "mode === 'navigate'", '/offline.html', 'SKIP_WAITING')) {
  if (-not ($rSw.Content -like '*' + $marcador + '*')) { Falhar ('PWA_SW marcador ausente: ' + $marcador) }
}
$rOff = WebObter '/offline.html' $null
if ($null -eq $rOff) { Falhar 'PWA_OFFLINE http != 200 apos 6 tentativas' }
foreach ($marcador in @('pt-BR', 'Tentar novamente', 'servidor', 'Verifique')) {
  if (-not ($rOff.Content -like '*' + $marcador + '*')) { Falhar ('PWA_OFFLINE marcador ausente: ' + $marcador) }
}
$rInst = WebObter '/js/pwa-install.js' $null
if ($null -eq $rInst) { Falhar 'PWA_INSTALL_JS http != 200 apos 6 tentativas' }
if (-not ($rInst.Content -like '*beforeinstallprompt*')) { Falhar 'PWA_INSTALL_JS beforeinstallprompt ausente' }
$rMan = WebObter '/manifest.json' $null
if ($null -eq $rMan) { Falhar 'PWA_MANIFEST http != 200 apos 6 tentativas' }
Log 'PWA_ASSETS_OK sw=v2_network_first offline.html=PT pwa-install.js=beforeinstallprompt manifest=200'

# O wiring PWA vive no _Layout (tela do app: triagem/recepcao); a pagina de login usa
# _AuthLayout minimal por design - a assercao de layout e feita na tela autenticada (WEB_REG).

# ---------------------------------------------------------------- WEB_REG (botao Regularizar ativo na tela da triagem)
$sessao = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$pg0 = Invoke-WebRequest -Uri ($webUrl + '/Account/Login') -WebSession $sessao -UseBasicParsing
$html0 = $pg0.Content
$tokPos = $html0.IndexOf('name="__RequestVerificationToken"')
if ($tokPos -lt 0) { Falhar 'WEB_REG_LOGIN token antiforgery nao encontrado na pagina de login' }
$valPos = $html0.IndexOf('value="', $tokPos)
$tokStart = $valPos + 'value="'.Length
$tokEnd = $html0.IndexOf('"', $tokStart)
$antiTok = $html0.Substring($tokStart, $tokEnd - $tokStart)
$bodyLogin = 'Email=' + [System.Uri]::EscapeDataString($emailReg) + '&Senha=' + [System.Uri]::EscapeDataString($senhaReg) + '&__RequestVerificationToken=' + [System.Uri]::EscapeDataString($antiTok)
$post = Invoke-WebRequest -Uri ($webUrl + '/Account/Login') -Method Post -WebSession $sessao -Body $bodyLogin -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -MaximumRedirection 10
if ([int]$post.StatusCode -ne 200) { Falhar ('WEB_REG_LOGIN http ' + [int]$post.StatusCode) }

$qEdit = WebObter ('/Triagem/Edit/' + $idTriagem) $sessao
if ($null -eq $qEdit) { Falhar 'WEB_REG_EDIT http != 200 apos 6 tentativas' }
$hEdit = $qEdit.Content
if (-not ($hEdit -like '*Regularizar*')) { Falhar 'WEB_REG_EDIT botao Regularizar ausente' }
if (-not ($hEdit -like '*/Triagem/Regularizar*')) { Falhar 'WEB_REG_EDIT acao /Triagem/Regularizar ausente' }
# A tela da triagem (superficie do PWA) renderiza o _Layout com todo o wiring PWA
if (-not ($hEdit -like '*pwa-install.js*')) { Falhar 'PWA_LAYOUT script pwa-install.js ausente no _Layout da triagem' }
if (-not ($hEdit -like '*manifest.json*')) { Falhar 'PWA_LAYOUT link manifest ausente no _Layout da triagem' }
if (-not ($hEdit -like '*service-worker-registration*')) { Falhar 'PWA_LAYOUT registro service-worker ausente no _Layout da triagem' }
Log 'WEB_REG_EDIT_OK http=200 botao=Regularizar acao=/Triagem/Regularizar'
Log 'PWA_LAYOUT_OK _Layout da triagem referencia pwa-install.js + manifest.json + service-worker-registration'

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
