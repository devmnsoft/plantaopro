# R103 - integracoes: WhatsApp (opt-in) + e-mail transacional + API publica com chaves
# Execucao canonica: sobe API :52186 + Web :52185 (DLLs Release, banco vivo plantaopro_test)
# e prova o contrato da rodada de ponta a ponta:
#   MIGRACAO         aplica v2380 (mensagens_saida + api_chaves_usos + colunas de
#                    rate limit em api_rate_limits que a criacao de chaves ja inseria)
#   REG_*            mundo clinico pelo funil real + modulos SAUDE360/NOTIFICACOES
#                    efetivos + grant NOTIFICACOES.VER + fixture deterministico
#                    (medico + 3 pacientes + agendamento)
#   OPTIN_*          opt-in WhatsApp/e-mail por paciente via consentimento LGPD
#                    (conceder os dois canais + estado no banco + auditoria)
#   MSG_*            envio manual nos quatro resultados: ENVIADA (provider LOCAL),
#                    SEM_DESTINO e BLOQUEADA_SEM_CONSENTIMENTO + caixa de saida
#   CHAVE_*          ciclo da API publica: criacao (hash SHA256 + limites), 401 com
#                    chave inexistente, identidade, situacao do agendamento, paciente,
#                    403 sem escopo, 429 por janela de minuto, trilha de uso + logs do
#                    portal e 401 apos revogar
#   CHECKIN_*        gatilho transacional: check-in do agendamento notifica o paciente
#                    nos dois canais com opt-in (2 linhas CHECKIN_REALIZADO ENVIADA)
#                    sem quebrar a acao clinica
#   WEB_OUTBOX       tela /Notificacoes/Preferencias renderiza o outbox server-side
# A limpeza no inicio garante rerun previsivel; o tenant permanece ao final.
$ErrorActionPreference = 'Stop'

$repo     = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$db       = 'plantaopro_test'
$porta    = 52186
$webPorta = 52185
$apiUrl   = 'http://localhost:' + $porta
$webUrl   = 'http://localhost:' + $webPorta
$apiDll   = Join-Path $repo 'backend/PlantaoPro.Api/bin/Release/net10.0/PlantaoPro.Api.dll'
$webDll   = Join-Path $repo 'backend/PlantaoPro.Web/bin/Release/net10.0/PlantaoPro.Web.dll'
$psql     = "C:\Program Files\PostgreSQL\18\bin\psql.exe"

$env:PGPASSWORD = '123456'
$env:PGCLIENTENCODING = 'UTF8'
$saida = Join-Path $PSScriptRoot 'r103a_drill_saida.txt'
$t1arquivo = Join-Path $PSScriptRoot 'r103a_t1_publico_situacao.json'
$cnpjInt  = '99015015000190'
$emailInt = 'r103-drill-int@plantaopro-local.test'
$senhaInt = 'R103-int-2026#Kp9z'

# Captura de console dos apps (diagnostico em caso de falha; anexada a saida no Falhar)
$global:LogApiOut  = Join-Path $PSScriptRoot 'r103a_api_stdout.log'
$global:LogApiErr  = Join-Path $PSScriptRoot 'r103a_api_stderr.log'
$global:LogWebOut  = Join-Path $PSScriptRoot 'r103a_web_stdout.log'
$global:LogWebErr  = Join-Path $PSScriptRoot 'r103a_web_stderr.log'

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
function Sha256Hex([string]$texto) {
  return [BitConverter]::ToString([System.Security.Cryptography.SHA256]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes($texto))).Replace('-','').ToLowerInvariant()
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
  return HttpRequestH $metodo $url $token @{} $payload
}
# Idem com headers extras (ex.: X-Api-Key na API publica).
function HttpRequestH([string]$metodo, [string]$url, [string]$token, [hashtable]$hdrsExtra, $payload) {
  try {
    $hdr = @{}
    if ($token) { $hdr['Authorization'] = 'Bearer ' + $token }
    foreach ($k in $hdrsExtra.Keys) { $hdr[$k] = $hdrsExtra[$k] }
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
  $sql = "delete from plantaopro.triagem_fila where agendamento_id in (select id from plantaopro.agendamentos where cliente_id=" + $cid + ");"
  $sql += "delete from plantaopro.painel_chamada_fila where agendamento_id in (select id from plantaopro.agendamentos where cliente_id=" + $cid + ");"
  $sql += "delete from plantaopro.triagem_historico where triagem_id in (select id from plantaopro.triagens where cliente_id=" + $cid + ");"
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
    # R103: integridade do mundo de integracoes antes de remover o tenant (ordem das FKs)
    $sql = ('delete from plantaopro.cadastro_cliente_pagamentos_iniciais where assinatura_id=' + $qAssina + ';')
    $sql += ('delete from plantaopro.mensagens_saida where cliente_id=' + $qCliente + ';')
    if ($p[1]) {
      $sql += ('delete from plantaopro.api_chaves_usos where chave_id in (select id from plantaopro.api_chaves where tenant_id=' + "'" + $p[1] + "');")
      $sql += ('delete from plantaopro.api_rate_limits where api_chave_id in (select id from plantaopro.api_chaves where tenant_id=' + "'" + $p[1] + "');")
      $sql += ('delete from plantaopro.api_chaves where tenant_id=' + "'" + $p[1] + "';")
    }
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
    empresa      = @{ nomeFantasia = $nome; razaoSocial = $nome + ' LTDA'; cnpj = $cnpj; segmento = 'Clinica'; cidade = 'Recife'; uf = $uf; telefone = '(81) 99999-1030'; emailCorporativo = $email }
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
function WebObter([string]$rota, $sess) {
  for ($w = 0; $w -lt 6; $w++) {
    try { $q = Invoke-WebRequest -Uri ($webUrl + $rota) -WebSession $sess -UseBasicParsing -TimeoutSec 60 -MaximumRedirection 10; if ([int]$q.StatusCode -eq 200) { return $q } }
    catch { Start-Sleep 2 }
  }
  return $null
}

Log ('R103A_DRILL data=' + (Get-Date -Format 'yyyyMMdd-HHmmss') + ' db=' + $db + ' api=' + $porta + ' web=' + $webPorta)

# ---------------------------------------------------------------- ESTAGEM 1: portas + apps
if (-not (Test-Path $apiDll)) { Falhar ('DLL da API ausente: ' + $apiDll) }
if (-not (Test-Path $webDll)) { Falhar ('DLL da Web ausente: ' + $webDll) }
if (-not (PortaLivre $porta) -or (-not (PortaLivre $webPorta))) { Falhar ('portas ocupadas antes do inicio (api=' + $porta + ' web=' + $webPorta + ')') }
Log 'PORTAS_LIVRES_INICIO=SIM'

$env:ConnectionStrings__Default = 'Host=127.0.0.1;Port=5432;Database=' + $db + ';Username=postgres;Password=123456'
$env:Jwt__Key = 'r103-drill-dev-jwt-key-0123456789-abcd'
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
Psq ("delete from plantaopro.usuario_permissoes_especiais where usuario_id in (select id from plantaopro.usuarios where email='" + $emailInt + "')") | Out-Null
LimparClinicoPorCnpj $cnpjInt | Out-Null
LimparMundoPorCnpj $cnpjInt | Out-Null
LimparPlanoPorCodigo 'R103_PLANO_DRILL' | Out-Null
Log 'PRE_LIMPEZA_RERUN ok=SIM'

$tokSup = FazerLogin 'superadmin@plantaopro.local' 'Super@123456'

# ---------------------------------------------------------------- MIGRACAO (v2380: mensageria + chaves)
$migFile = Join-Path $repo 'database/migrations/2026_10_v2380_r103_integracoes_whatsapp_email_api_publica.sql'
$prevEap = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
try {
  $migOut = (& $psql -h localhost -U postgres -d $db -X -q -v ON_ERROR_STOP=1 -f $migFile 2>&1 | Out-String)
  $migExit = $LASTEXITCODE
} finally { $ErrorActionPreference = $prevEap }
if ($migExit -ne 0) {
  $resumo = ($migOut -replace '\s+', ' ')
  if ($resumo.Length -gt 300) { $resumo = $resumo.Substring(0, 300) }
  Falhar ('MIGRACAO v2380 exit=' + $migExit + ': ' + $resumo)
}
$nMsgCols = Psq ("select count(*) from information_schema.columns where table_schema='plantaopro' and table_name='mensagens_saida' and column_name in ('canal','destino','tipo_evento','status','provider','paciente_id')")
if ($nMsgCols -ne '6') { Falhar ('MIGRACAO colunas mensagens_saida=' + $nMsgCols + ' esperado 6') }
$nUsos = Psq ("select count(*) from information_schema.tables where table_schema='plantaopro' and table_name='api_chaves_usos'")
if ($nUsos -ne '1') { Falhar ('MIGRACAO tabela api_chaves_usos=' + $nUsos + ' esperado 1') }
$nRateCols = Psq ("select count(*) from information_schema.columns where table_schema='plantaopro' and table_name='api_rate_limits' and column_name in ('api_chave_id','limite_minuto','limite_dia')")
if ($nRateCols -ne '3') { Falhar ('MIGRACAO colunas api_rate_limits=' + $nRateCols + ' esperado 3') }
$ckMsg = Psq ("select count(*) from pg_constraint c join pg_class t on t.oid=c.conrelid where t.relname='mensagens_saida' and c.contype='c'")
if ($ckMsg -lt '2') { Falhar ('MIGRACAO constraints mensagens_saida=' + $ckMsg + ' esperado >=2') }
Log 'MIGRACAO_OK v2380 mensagens_saida=SIM api_chaves_usos=SIM rate_limit_cols=3 constraints>=2'

# ---------------------------------------------------------------- PLANO (governanca)
$rPlano = HttpRequest 'POST' ($apiUrl + '/api/planos') $tokSup @{
  nome = 'R103 Plano Drill'
  descricao = 'Plano do drill r103 (integracoes whatsapp/e-mail/api publica)'
  valorMensal = 100.00
  limiteMedicos = 0; limiteHospitais = 0; limitePlantoesMes = 0
  permiteMobile = $false; permiteBi = $false; permiteRelatoriosAvancados = $false; permiteIntegracoes = $true
  codigo = 'r103_plano_drill'
  slug = 'r103-plano-drill'
  valorAnual = 1188.00
  publico = $true; destaque = $false; ordem = 963; permiteWhiteLabel = $false
  permiteTrial = $true
  diasTrial = 5
}
if ($rPlano.Http -ne 200) { Falhar ('PLANO drill http ' + $rPlano.Http + ' ' + $(if ($rPlano.Json) { $rPlano.Json.message } else { 'sem corpo' })) }
$script:idPlano = [string]$rPlano.Json.data
Log ('PLANO_OK drill=' + $script:idPlano + ' mensal=100 anual=1188 integracoes=SIM')

# ---------------------------------------------------------------- REG_MUNDO (funil + modulos + grants)
$mInt = FunilCadastrar 'R103 Integracao Clinic' $cnpjInt $emailInt $senhaInt 'PE'

# Gates de modulo: SAUDE360 (audiencia clinica da mensageria/checkin) e NOTIFICACOES
# (guard web da tela Preferencias). A rota real de governanca habilita por tenant.
$rMod = HttpRequest 'GET' ($apiUrl + '/api/modulos') $tokSup $null
if ($rMod.Http -ne 200) { Falhar ('REG_MODULO listagem http ' + $rMod.Http) }
$idS360 = $null
$idNotif = $null
foreach ($m in @($rMod.Json.data)) {
  if ([string]$m.codigo -eq 'SAUDE360') { $idS360 = [string]$m.id }
  if ([string]$m.codigo -eq 'NOTIFICACOES') { $idNotif = [string]$m.id }
}
if (-not $idS360 -or -not $idNotif) { Falhar 'REG_MODULO modulos SAUDE360/NOTIFICACOES nao encontrados no catalogo' }
$rHab1 = HttpRequest 'POST' ($apiUrl + '/api/modulos/' + $idS360 + '/habilitar-tenant') $tokSup @{ tenantId = $mInt.Tenant; precoContratado = 0; limiteContratado = 0; motivo = 'Drill R103 - mensageria e check-in transacional' }
if ($rHab1.Http -ne 200) { Falhar ('REG_MODULO habilitar SAUDE360 http ' + $rHab1.Http + ' ' + $(if ($rHab1.Json) { $rHab1.Json.message } else { 'sem corpo' })) }
$rHab2 = HttpRequest 'POST' ($apiUrl + '/api/modulos/' + $idNotif + '/habilitar-tenant') $tokSup @{ tenantId = $mInt.Tenant; precoContratado = 0; limiteContratado = 0; motivo = 'Drill R103 - outbox de mensageria na central' }
if ($rHab2.Http -ne 200) { Falhar ('REG_MODULO habilitar NOTIFICACOES http ' + $rHab2.Http + ' ' + $(if ($rHab2.Json) { $rHab2.Json.message } else { 'sem corpo' })) }
$ef1 = Psq ("select coalesce(plantaopro.modulo_efetivo('" + $mInt.Tenant + "','SAUDE360'),false)::text")
$ef2 = Psq ("select coalesce(plantaopro.modulo_efetivo('" + $mInt.Tenant + "','NOTIFICACOES'),false)::text")
if ($ef1 -ne 'true' -or $ef2 -ne 'true') { Falhar ('REG_MODULO efetivos saude360=' + $ef1 + ' notificacoes=' + $ef2 + ' esperado true/true') }
Log 'REG_MODULO_OK saude360=CONTRATO notificacoes=CONTRATO'

# O dono do mundo nao tem NOTIFICACOES.VER no perfil ADMINISTRADOR_CLIENTE (mesma lacuna
# de catalogo da TRIAGEM.EDITAR da R102) - o drill concede por usuario_permissoes_especiais.
$idPermVer = Psq ("select id from plantaopro.permissoes where codigo='NOTIFICACOES.VER'")
if (-not $idPermVer) { Falhar 'REG_PERMISSAO permissao NOTIFICACOES.VER ausente no catalogo' }
$errPer = Psq ("insert into plantaopro.usuario_permissoes_especiais(tenant_id,usuario_id,permissao_id,permitido,justificativa) select t.id, u.id, '" + $idPermVer + "', true, 'Drill R103 - outbox de mensageria' from plantaopro.usuarios u join plantaopro.tenants t on t.id=u.tenant_id where u.email='" + $emailInt + "' and not exists (select 1 from plantaopro.usuario_permissoes_especiais x where x.usuario_id=u.id and x.permissao_id='" + $idPermVer + "' and x.reg_status='A')")
if ($errPer -match 'ERRO') { Falhar ('REG_PERMISSAO ' + $errPer) }
$nPer = Psq ("select count(*) from plantaopro.usuario_permissoes_especiais x where x.reg_status='A' and x.permitido=true and x.permissao_id='" + $idPermVer + "' and exists (select 1 from plantaopro.usuarios u where u.id=x.usuario_id and u.email='" + $emailInt + "')")
if ($nPer -ne '1') { Falhar ('REG_PERMISSAO grants=' + $nPer + ' esperado 1') }
Log 'REG_PERMISSAO_OK NOTIFICACOES.VER via usuario_permissoes_especiais'

$cidInt = CliIdDe $cnpjInt
$tokInt = FazerLogin $emailInt $senhaInt

# ---------------------------------------------------------------- REG_FIXTURE (medico + 3 pacientes + agendamento)
$tplClinico = @"
insert into plantaopro.medicos(nome,cliente_id,status,reg_status) select 'R103 Medico', {CID}, 'ATIVO','A';
insert into plantaopro.pacientes(tenant_id,codigo,nome,nome_completo,telefone,email,status,criado_em,atualizado_em,cliente_id,consentimento_lgpd,reg_status,reg_date)
select null,'R103-PAC-1','R103 Paciente Ativo','R103 Paciente Ativo','+5581999990001','pac1.r103@plantaopro-local.test','ATIVO',now(),now(),{CID},false,'A',now();
insert into plantaopro.pacientes(tenant_id,codigo,nome,nome_completo,telefone,email,status,criado_em,atualizado_em,cliente_id,consentimento_lgpd,reg_status,reg_date)
select null,'R103-PAC-2','R103 Paciente SemContato','R103 Paciente SemContato','','','ATIVO',now(),now(),{CID},false,'A',now();
insert into plantaopro.pacientes(tenant_id,codigo,nome,nome_completo,telefone,email,status,criado_em,atualizado_em,cliente_id,consentimento_lgpd,reg_status,reg_date)
select null,'R103-PAC-3','R103 Paciente SemOptIn','R103 Paciente SemOptIn','+5581999990003','','ATIVO',now(),now(),{CID},false,'A',now();
insert into plantaopro.agendamentos(cliente_id,paciente_id,medico_id,data_inicio,data_fim,tipo,status,reg_date,reg_status)
select {CID}, (select id from plantaopro.pacientes where codigo='R103-PAC-1'), (select id from plantaopro.medicos where cliente_id={CID} order by reg_date desc limit 1),
  now() - interval '1 hour', now() + interval '2 hours', 'CONSULTA', 'AGENDADO', now(), 'A';
"@
$sqlFix = $tplClinico.Replace('{CID}', $cidInt)
$err = Psq $sqlFix
if ($err -match 'ERRO') { Falhar ('REG_FIXTURE ' + $err + ' --- SQL --- ' + $sqlFix) }

$idMed   = Psq ("select id from plantaopro.medicos where cliente_id=" + $cidInt)
$idPac1  = Psq ("select id from plantaopro.pacientes where codigo='R103-PAC-1'")
$idPac2  = Psq ("select id from plantaopro.pacientes where codigo='R103-PAC-2'")
$idPac3  = Psq ("select id from plantaopro.pacientes where codigo='R103-PAC-3'")
$idAgend = Psq ("select id from plantaopro.agendamentos where cliente_id=" + $cidInt)
if (-not $idMed -or -not $idPac1 -or -not $idPac2 -or -not $idPac3 -or -not $idAgend) { Falhar 'REG_FIXTURE ids incompletos' }
Log ('REG_FIXTURE_OK medico=' + $idMed + ' pac1=' + $idPac1 + ' pac2=' + $idPac2 + ' pac3=' + $idPac3 + ' agendamento=' + $idAgend)

# ---------------------------------------------------------------- OPT_IN (opt-in dos dois canais no P1)
$rOp1 = HttpRequest 'POST' ($apiUrl + '/api/mensageria/opt-in') $tokInt @{ PacienteId = [guid]$idPac1; Canal = 'WHATSAPP'; Conceder = $true }
if ($rOp1.Http -ne 200) { Falhar ('OPTIN_WHATSAPP http ' + $rOp1.Http + ' ' + $rOp1.Content) }
if (-not [bool]$rOp1.Json.success) { Falhar ('OPTIN_WHATSAPP sucesso=NAO ' + [string]$rOp1.Json.message) }
$rOp2 = HttpRequest 'POST' ($apiUrl + '/api/mensageria/opt-in') $tokInt @{ PacienteId = [guid]$idPac1; Canal = 'EMAIL'; Conceder = $true }
if ($rOp2.Http -ne 200) { Falhar ('OPTIN_EMAIL http ' + $rOp2.Http + ' ' + $rOp2.Content) }
if (-not [bool]$rOp2.Json.success) { Falhar ('OPTIN_EMAIL sucesso=NAO ' + [string]$rOp2.Json.message) }
$canaisResp = (@($rOp2.Json.data.canais) | Sort-Object) -join ','
if ($canaisResp -ne 'EMAIL,WHATSAPP') { Falhar ('OPTIN_EMAIL canais=' + $canaisResp + ' esperado EMAIL,WHATSAPP') }
$canalBanco = Psq ("select coalesce(upper(consentimento_lgpd_canal),'') from plantaopro.pacientes where id='" + $idPac1 + "'")
$consentBanco = Psq ("select coalesce(consentimento_lgpd,false)::text from plantaopro.pacientes where id='" + $idPac1 + "'")
if ($canalBanco -ne 'EMAIL,WHATSAPP') { Falhar ('OPTIN_BANCO canais=' + $canalBanco + ' esperado EMAIL,WHATSAPP') }
if ($consentBanco -ne 'True' -and $consentBanco -ne 'true') { Falhar ('OPTIN_BANCO consentimento=' + $consentBanco + ' esperado true') }
$nOptAud = Psq ("select count(*) from plantaopro.auditoria_acoes_criticas where cliente_id=" + $cidInt + " and entidade='MENSAGERIA_OPTIN'")
if ($nOptAud -ne '2') { Falhar ('OPTIN_AUDITORIA linhas=' + $nOptAud + ' esperado 2') }
Log 'OPTIN_OK whatsapp+email canal_banco=EMAIL,WHATSAPP auditoria=2'

# ---------------------------------------------------------------- MSG (quatro resultados do envio manual)
$rMsg1 = HttpRequest 'POST' ($apiUrl + '/api/mensageria/enviar') $tokInt @{ PacienteId = [guid]$idPac1; Canal = 'WHATSAPP'; TipoEvento = 'DRILL_R103'; Mensagem = 'Seu atendimento esta confirmado (drill R103).' }
if ($rMsg1.Http -ne 200) { Falhar ('MSG_WA_P1 http ' + $rMsg1.Http + ' ' + $rMsg1.Content) }
if ([string]$rMsg1.Json.data.status -ne 'ENVIADA') { Falhar ('MSG_WA_P1 status=' + [string]$rMsg1.Json.data.status + ' esperado ENVIADA') }
if ([string]$rMsg1.Json.data.provider -ne 'LOCAL') { Falhar ('MSG_WA_P1 provider=' + [string]$rMsg1.Json.data.provider + ' esperado LOCAL') }
$rMsg2 = HttpRequest 'POST' ($apiUrl + '/api/mensageria/enviar') $tokInt @{ PacienteId = [guid]$idPac1; Canal = 'EMAIL'; TipoEvento = 'DRILL_R103'; Assunto = 'Confirmação de atendimento'; Mensagem = 'Seu atendimento esta confirmado (drill R103).' }
if ($rMsg2.Http -ne 200) { Falhar ('MSG_EMAIL_P1 http ' + $rMsg2.Http + ' ' + $rMsg2.Content) }
if ([string]$rMsg2.Json.data.status -ne 'ENVIADA') { Falhar ('MSG_EMAIL_P1 status=' + [string]$rMsg2.Json.data.status + ' esperado ENVIADA') }
$rMsg3 = HttpRequest 'POST' ($apiUrl + '/api/mensageria/enviar') $tokInt @{ PacienteId = [guid]$idPac2; Canal = 'WHATSAPP'; TipoEvento = 'DRILL_R103'; Mensagem = 'Teste sem destino.' }
if ($rMsg3.Http -ne 200) { Falhar ('MSG_WA_P2 http ' + $rMsg3.Http + ' ' + $rMsg3.Content) }
if ([string]$rMsg3.Json.data.status -ne 'SEM_DESTINO') { Falhar ('MSG_WA_P2 status=' + [string]$rMsg3.Json.data.status + ' esperado SEM_DESTINO') }
$rMsg4 = HttpRequest 'POST' ($apiUrl + '/api/mensageria/enviar') $tokInt @{ PacienteId = [guid]$idPac3; Canal = 'WHATSAPP'; TipoEvento = 'DRILL_R103'; Mensagem = 'Teste sem opt-in.' }
if ($rMsg4.Http -ne 200) { Falhar ('MSG_WA_P3 http ' + $rMsg4.Http + ' ' + $rMsg4.Content) }
if ([string]$rMsg4.Json.data.status -ne 'BLOQUEADA_SEM_CONSENTIMENTO') { Falhar ('MSG_WA_P3 status=' + [string]$rMsg4.Json.data.status + ' esperado BLOQUEADA_SEM_CONSENTIMENTO') }
$nCaixa = Psq ("select count(*) filter (where status='ENVIADA') || '|' || count(*) filter (where status='SEM_DESTINO') || '|' || count(*) filter (where status='BLOQUEADA_SEM_CONSENTIMENTO') from plantaopro.mensagens_saida where cliente_id=" + $cidInt)
if ($nCaixa -ne '2|1|1') { Falhar ('MSG_CAIXA enviados|sem_destino|bloqueados=' + $nCaixa + ' esperado 2|1|1') }
$nMsgAud = Psq ("select count(*) from plantaopro.auditoria_acoes_criticas where cliente_id=" + $cidInt + " and entidade='MENSAGENS_SAIDA'")
if ($nMsgAud -ne '4') { Falhar ('MSG_AUDITORIA linhas=' + $nMsgAud + ' esperado 4') }
Log 'MSG_OK enviada=2 sem_destino=1 bloqueada_sem_consentimento=1 provider=LOCAL auditoria=4'

# ---------------------------------------------------------------- CHAVE (ciclo da API publica)
$rKey = HttpRequest 'POST' ($apiUrl + '/api/developer/api-keys') $tokInt @{ Nome = 'R103 drill'; Escopos = @('agendamentos:read','pacientes:read') }
if ($rKey.Http -ne 200) { Falhar ('CHAVE_CRIAR http ' + $rKey.Http + ' ' + $rKey.Content) }
$keyId = [string]$rKey.Json.data.id
$chave = [string]$rKey.Json.data.chaveExibicaoUnica
if (-not ($chave -like 'pp_live_*')) { Falhar ('CHAVE_CRIAR formato=' + $chave + ' esperado pp_live_*') }
$hBanco = Psq ("select api_key_hash from plantaopro.api_chaves where id='" + $keyId + "'")
if ((Sha256Hex $chave) -ne $hBanco) { Falhar ('CHAVE_HASH banco=' + $hBanco + ' esperado sha256 da chave exibida') }
$escoposBanco = Psq ("select escopos from plantaopro.api_chaves where id='" + $keyId + "'")
if ($escoposBanco -ne 'agendamentos:read,pacientes:read') { Falhar ('CHAVE_ESCOPOS banco=' + $escoposBanco) }
$rateLinha = Psq ("select limite_minuto || '|' || limite_dia from plantaopro.api_rate_limits where api_chave_id='" + $keyId + "'")
if ($rateLinha -ne '120|10000') { Falhar ('CHAVE_RATE limites=' + $rateLinha + ' esperado 120|10000') }
Log ('CHAVE_CRIAR_OK id=' + $keyId + ' hash_sha256=OK limites=120min/10000dia')

$rBad = HttpRequestH 'GET' ($apiUrl + '/api/publico/identidade') '' @{ 'X-Api-Key' = 'pp_live_inexistente1234567890' } $null
if ($rBad.Http -ne 401) { Falhar ('CHAVE_401_INEXISTENTE http ' + $rBad.Http + ' esperado 401') }
Log 'CHAVE_401_OK chave_inexistente=http=401'

$rIdent = HttpRequestH 'GET' ($apiUrl + '/api/publico/identidade') '' @{ 'X-Api-Key' = $chave } $null
if ($rIdent.Http -ne 200) { Falhar ('CHAVE_IDENTIDADE http ' + $rIdent.Http + ' ' + $rIdent.Content) }
if ([string]$rIdent.Json.data.chaveId -ne $keyId) { Falhar ('CHAVE_IDENTIDADE chaveId=' + [string]$rIdent.Json.data.chaveId) }
$escoposIdent = @($rIdent.Json.data.escopos | Sort-Object) -join ','
if ($escoposIdent -ne 'agendamentos:read,pacientes:read') { Falhar ('CHAVE_IDENTIDADE escopos=' + $escoposIdent) }
Log 'CHAVE_IDENTIDADE_OK chaveId=confere escopos=2'

$rSit = HttpRequestH 'GET' ($apiUrl + '/api/publico/agendamentos/' + $idAgend + '/situacao') '' @{ 'X-Api-Key' = $chave } $null
if ($rSit.Http -ne 200) { Falhar ('CHAVE_SITUACAO http ' + $rSit.Http + ' ' + $rSit.Content) }
if ([string]$rSit.Json.data.status -ne 'AGENDADO') { Falhar ('CHAVE_SITUACAO status=' + [string]$rSit.Json.data.status + ' esperado AGENDADO') }
if (-not ([string]$rSit.Json.data.pacienteNome -like '*R103*')) { Falhar ('CHAVE_SITUACAO pacienteNome=' + [string]$rSit.Json.data.pacienteNome) }
[IO.File]::WriteAllText($t1arquivo, $rSit.Content, (New-Object System.Text.UTF8Encoding($false)))
Log 'T1_DUMP_OK arquivo=r103a_t1_publico_situacao.json status=AGENDADO'

$rPac = HttpRequestH 'GET' ($apiUrl + '/api/publico/pacientes/' + $idPac1) '' @{ 'X-Api-Key' = $chave } $null
if ($rPac.Http -ne 200) { Falhar ('CHAVE_PACIENTE http ' + $rPac.Http + ' ' + $rPac.Content) }
if (-not ([string]$rPac.Json.data.nome -like '*R103*')) { Falhar ('CHAVE_PACIENTE nome=' + [string]$rPac.Json.data.nome) }
Log 'CHAVE_PACIENTE_OK leitura_por_escopo_pacientes_read'

# 403: chave somente pacientes:read nao le agendamentos
$rKey2 = HttpRequest 'POST' ($apiUrl + '/api/developer/api-keys') $tokInt @{ Nome = 'R103 drill restrita'; Escopos = @('pacientes:read') }
if ($rKey2.Http -ne 200) { Falhar ('CHAVE_K2_CRIAR http ' + $rKey2.Http + ' ' + $rKey2.Content) }
$keyId2 = [string]$rKey2.Json.data.id
$chave2 = [string]$rKey2.Json.data.chaveExibicaoUnica
$rEscopo = HttpRequestH 'GET' ($apiUrl + '/api/publico/agendamentos/' + $idAgend + '/situacao') '' @{ 'X-Api-Key' = $chave2 } $null
if ($rEscopo.Http -ne 403) { Falhar ('CHAVE_403_ESCOPO http ' + $rEscopo.Http + ' esperado 403') }
if (-not ($rEscopo.Content -like '*escopo*')) { Falhar ('CHAVE_403_ESCOPO motivo ausente: ' + $rEscopo.Content) }
$rRev2 = HttpRequest 'POST' ($apiUrl + '/api/developer/api-keys/' + $keyId2 + '/revogar') $tokInt $null
if ($rRev2.Http -ne 200) { Falhar ('CHAVE_K2_REVOGAR http ' + $rRev2.Http + ' ' + $rRev2.Content) }
Log 'CHAVE_403_OK sem_escopo_agendamentos_http=403 chave_restrita_reogada'

# 429: janela de 1 minuto esgotada (3 usos ja registrados p/ esta chave)
Psq ("update plantaopro.api_rate_limits set limite_minuto=3 where api_chave_id='" + $keyId + "'") | Out-Null
$rRate = HttpRequestH 'GET' ($apiUrl + '/api/publico/identidade') '' @{ 'X-Api-Key' = $chave } $null
if ($rRate.Http -ne 429) { Falhar ('CHAVE_429_RATE http ' + $rRate.Http + ' esperado 429') }
if (-not ($rRate.Content -like '*Limite*')) { Falhar ('CHAVE_429_RATE motivo ausente: ' + $rRate.Content) }
$errRest = Psq ("update plantaopro.api_rate_limits set limite_minuto=120 where api_chave_id='" + $keyId + "'")
if ($errRest -match 'ERRO') { Falhar ('CHAVE_429_RESTORE ' + $errRest) }
Log 'CHAVE_429_OK janela_de_minuto_excedida_http=429 limite_restaurado=120'

$nUsos = Psq ("select count(*) || '|' || count(*) filter (where rota like '/api/publico/%') from plantaopro.api_chaves_usos where chave_id='" + $keyId + "'")
if ($nUsos -ne '3|3') { Falhar ('CHAVE_USOS total|publico=' + $nUsos + ' esperado 3|3') }
$rLogs = HttpRequest 'GET' ($apiUrl + '/api/developer/api-keys/' + $keyId + '/logs') $tokInt $null
if ($rLogs.Http -ne 200) { Falhar ('CHAVE_LOGS http ' + $rLogs.Http + ' ' + $rLogs.Content) }
if (-not [bool]$rLogs.Json.success) { Falhar ('CHAVE_LOGS sucesso=NAO ' + [string]$rLogs.Json.message) }
$nLogs = @($rLogs.Json.data).Count
if ($nLogs -ne 3) { Falhar ('CHAVE_LOGS itens=' + $nLogs + ' esperado 3') }
$rRev = HttpRequest 'POST' ($apiUrl + '/api/developer/api-keys/' + $keyId + '/revogar') $tokInt $null
if ($rRev.Http -ne 200) { Falhar ('CHAVE_REVOGAR http ' + $rRev.Http + ' ' + $rRev.Content) }
$rRevChec = HttpRequestH 'GET' ($apiUrl + '/api/publico/identidade') '' @{ 'X-Api-Key' = $chave } $null
if ($rRevChec.Http -ne 401) { Falhar ('CHAVE_REVOGADA_401 http ' + $rRevChec.Http + ' esperado 401') }
Log 'CHAVE_USOS_OK trilha=3 logs_portal=3 revogada_401=SIM'

# ---------------------------------------------------------------- CHECKIN (gatilho transacional no commit)
# Fluxo do kernel: AGENDADO -> (confirmar) CONFIRMADO -> (checkin) CHECKIN_REALIZADO.
$rConf = HttpRequest 'POST' ($apiUrl + '/api/agendamentos/' + $idAgend + '/confirmar') $tokInt @{ Observacoes = 'Drill R103 - confirmacao do agendamento' }
if ($rConf.Http -ne 200) { Falhar ('CHECKIN_CONFIRMAR http ' + $rConf.Http + ' ' + $rConf.Content) }
if (-not [bool]$rConf.Json.success) { Falhar ('CHECKIN_CONFIRMAR sucesso=NAO ' + [string]$rConf.Json.message) }
$rChk = HttpRequest 'POST' ($apiUrl + '/api/agendamentos/' + $idAgend + '/checkin') $tokInt @{ Observacoes = 'Drill R103 - check-in transacional' }
if ($rChk.Http -ne 200) { Falhar ('CHECKIN http ' + $rChk.Http + ' ' + $rChk.Content) }
if (-not [bool]$rChk.Json.success) { Falhar ('CHECKIN sucesso=NAO ' + [string]$rChk.Json.message) }
$stAg = Psq ("select coalesce(status,'') from plantaopro.agendamentos where id='" + $idAgend + "'")
if ($stAg -ne 'CHECKIN_REALIZADO') { Falhar ('CHECKIN agendamento.status=' + $stAg + ' esperado CHECKIN_REALIZADO') }
$nChkRow = Psq ("select count(*) from plantaopro.agendamento_checkins where agendamento_id='" + $idAgend + "'")
if ($nChkRow -ne '1') { Falhar ('CHECKIN registro checkins=' + $nChkRow + ' esperado 1') }
Log 'CHECKIN_OK http=200 status=CHECKIN_REALIZADO registro=1'

$nTrig = Psq ("select count(*) || '|' || count(*) filter (where status='ENVIADA') || '|' || count(*) filter (where canal='WHATSAPP') || '|' || count(*) filter (where canal='EMAIL' and assunto is not null) from plantaopro.mensagens_saida where cliente_id=" + $cidInt + " and tipo_evento='CHECKIN_REALIZADO'")
if ($nTrig -ne '2|2|1|1') { Falhar ('CHECKIN_TRIGGER linhas|enviadas|wa|email_assunto=' + $nTrig + ' esperado 2|2|1|1') }
$nTrigDestino = Psq ("select count(*) filter (where canal='WHATSAPP' and destino='+5581999990001') || '|' || count(*) filter (where canal='EMAIL' and destino='pac1.r103@plantaopro-local.test') from plantaopro.mensagens_saida where cliente_id=" + $cidInt + " and tipo_evento='CHECKIN_REALIZADO'")
if ($nTrigDestino -ne '1|1') { Falhar ('CHECKIN_TRIGGER destinos wa|email=' + $nTrigDestino + ' esperado 1|1') }
Log 'CHECKIN_TRIGGER_OK 2_canais_com_optin enviadas=2 destinos_confirmados=SIM'

# ---------------------------------------------------------------- WEB_OUTBOX (tela Preferencias renderiza o outbox)
$sessao = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$pg0 = Invoke-WebRequest -Uri ($webUrl + '/Account/Login') -WebSession $sessao -UseBasicParsing
$html0 = $pg0.Content
$tokPos = $html0.IndexOf('name="__RequestVerificationToken"')
if ($tokPos -lt 0) { Falhar 'WEB_OUTBOX_LOGIN token antiforgery nao encontrado na pagina de login' }
$valPos = $html0.IndexOf('value="', $tokPos)
$tokStart = $valPos + 'value="'.Length
$tokEnd = $html0.IndexOf('"', $tokStart)
$antiTok = $html0.Substring($tokStart, $tokEnd - $tokStart)
$bodyLogin = 'Email=' + [System.Uri]::EscapeDataString($emailInt) + '&Senha=' + [System.Uri]::EscapeDataString($senhaInt) + '&__RequestVerificationToken=' + [System.Uri]::EscapeDataString($antiTok)
Invoke-WebRequest -Uri ($webUrl + '/Account/Login') -Method Post -Body $bodyLogin -ContentType 'application/x-www-form-urlencoded' -WebSession $sessao -UseBasicParsing -MaximumRedirection 5 | Out-Null

$pgPrefer = WebObter '/Notificacoes/Preferencias' $sessao
if (-not $pgPrefer) { Falhar 'WEB_OUTBOX pagina Preferencias nao respondeu 200 apos 6 tentativas' }
$htmlP = [string]$pgPrefer.Content
if (-not ($htmlP -like '*data-mensageria-outbox*')) { Falhar 'WEB_OUTBOX secao data-mensageria-outbox ausente na Preferencias' }
if (-not ($htmlP -like '*data-mensageria-tabela*')) { Falhar 'WEB_OUTBOX tabela do outbox ausente (itens deveriam existir)' }
if (-not ($htmlP -like '*data-status="ENVIADA"*')) { Falhar 'WEB_OUTBOX linha ENVIADA ausente no outbox renderizado' }
if (-not ($htmlP -like '*data-status="SEM_DESTINO"*')) { Falhar 'WEB_OUTBOX linha SEM_DESTINO ausente no outbox renderizado' }
if (-not ($htmlP -like '*CHECKIN_REALIZADO*')) { Falhar 'WEB_OUTBOX evento CHECKIN_REALIZADO ausente no outbox renderizado' }
if ($htmlP -notmatch '<p data-resumo-sent>(\d+)</p>') { Falhar 'WEB_OUTBOX resumo-sent ausente' }
$resumoSent = [int]$Matches[1]
if ($resumoSent -ne 4) { Falhar ('WEB_OUTBOX resumo enviado=' + $resumoSent + ' esperado 4 (2 manual + 2 gatilho check-in)') }
Log 'WEB_OUTBOX_OK secao+tabela+linhas+resumo_enviado=4'

# ---------------------------------------------------------------- FINALIZACAO
PararApps
Start-Sleep 2
if (-not (PortaLivre $porta) -or (-not (PortaLivre $webPorta))) { Falhar 'PORTAS_LIVRES_FIM portas ainda ocupadas apos parar os apps' }
Log 'PORTAS_LIVRES_FIM=SIM'
Log 'RESULTADO=PASSEI'
EscreverSaida
exit 0
