# R98 (assistente Drael em linguagem simples: resumo do pendente + orientacao ao
# usuario final): drill ponta a ponta com stub local do provedor Drael.
# Fluxo deterministico sobre o banco vivo:
#   ESTAGEM_1        stub Drael (HttpListener local) + API no ar (health)
#   PRE_LIMPEZA      purga por CNPJ dos dois mundos do drill + linhas ai_* do mes
#                    anterior (crash-idempotencia; rerun previsivel)
#   PA_MUNDOS        plano drill criado pelo admin + mundo TRIAL (5 dias) e mundo
#                    ATIVA MENSAL via funil self-service
#   AI_NAO_HAB       tarefa ainda nao configurada -> 200 NAO_HABILITADO (sem sucesso
#                    registrado em ai_usos)
#   AI_CONFIG        dono (ADMINISTRADOR_CLIENTE) habilita RESUMO_PENDENTE com
#                    provedor drael, chave do tenant e cota mensal de 1 uso
#   AI_TESTE         test-conexao usa a chave do tenant contra o stub -> OK
#   AI_GERACAO       POST tarefas/resumo-pendente -> 200 sucesso, provedor=drael,
#                    texto presente (jornada TRIAL: contrato + setup + operacao)
#   AI_USO           auditoria imutavel registra exatamente 1 SUCESSO com o novo
#                    codigo de tarefa RESUMO_PENDENTE
#   AI_COTA          segunda geracao no mesmo mes -> COTA_EXCEDIDA (cota 1) sem
#                    novo sucesso auditado
#   AI_VAZIO         mundo ATIVA limpo (etapas forcadas para CONCLUIDA MANUAL,
#                    nada operacional) -> 200 VAZIO com sucesso, sem gastar provedor
# A limpeza no inicio garante rerun previsivel; os dois tenants permanecem ao final.
$ErrorActionPreference = 'Stop'

$repo  = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$db    = 'plantaopro_test'
$porta = 52079
$stubPort = 52078
$apiUrl = 'http://localhost:' + $porta
$apiDll = Join-Path $repo 'backend/PlantaoPro.Api/bin/Release/net10.0/PlantaoPro.Api.dll'
$psql  = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$env:PGPASSWORD = '123456'
$env:PGCLIENTENCODING = 'UTF8'
$saida = Join-Path $PSScriptRoot 'r98a_drill_saida.txt'
$cnpjTrial = '98001001000111'
$cnpjAtiva = '98002002000122'

$linhas = New-Object System.Collections.Generic.List[string]
function Log([string]$l) { $linhas.Add($l); Write-Host $l }
function EscreverSaida() { Set-Content -Path $saida -Value $linhas -Encoding UTF8 }

$global:Falhou = $false
function Falhar([string]$msg) {
  $global:Falhou = $true
  Log ('FALHA ' + $msg)
  Log 'RESULTADO=FALHOU'
  EscreverSaida
  try { if ($global:JobStub) { Stop-Job $global:JobStub -ErrorAction SilentlyContinue; Remove-Job $global:JobStub -Force -ErrorAction SilentlyContinue } } catch {}
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

Log ('R98A_DRILL data=' + (Get-Date -Format 'yyyyMMdd-HHmmss') + ' db=' + $db + ' porta=' + $porta + ' stub=' + $stubPort)
$global:ApiProc = $null
$global:PidApi = $null
$global:JobStub = $null

# ---------------------------------------------------------------- ESTAGEM 1: stub Drael
if (-not (PortaLivre $porta) -or (-not (PortaLivre $stubPort))) { Falhar ('portas ocupadas antes do inicio (api=' + $porta + ' stub=' + $stubPort + ')') }
Log 'PORTAS_LIVRES_INICIO=SIM'
$stubFlag = Join-Path $env:TEMP ('r98a_stub_' + $stubPort + '.run')
if (Test-Path $stubFlag) { Remove-Item $stubFlag -Force }
Set-Content -Path $stubFlag -Value '1'
$global:JobStub = Start-Job -ArgumentList $stubPort, $stubFlag -ScriptBlock {
  param($port, $flag)
  $ErrorActionPreference = 'Stop'
  $l = New-Object System.Net.HttpListener
  $l.Prefixes.Add('http://127.0.0.1:' + $port + '/')
  $l.Start()
  $json = '{"id":"gen-r98","object":"chat.completion","created":1,"model":"drael-v1","choices":[{"index":0,"message":{"role":"assistant","content":"Resumo da sua organizacao: o teste gratuito segue em andamento. Primeiro, complete a configuracao inicial indicada nas telas; em seguida, trate as pendencias operacionais na ordem sugerida. Cada acao e feita na tela indicada."},"finish_reason":"stop"}],"usage":{"prompt_tokens":42,"completion_tokens":28,"total_tokens":70}}'
  while ((Test-Path $flag)) {
    try {
      $ctx = $l.GetContext()
      $buf = [System.Text.Encoding]::UTF8.GetBytes($json)
      $ctx.Response.StatusCode = 200
      $ctx.Response.ContentType = 'application/json'
      $ctx.Response.ContentLength64 = $buf.Length
      $ctx.Response.OutputStream.Write($buf, 0, $buf.Length)
      $ctx.Response.Close()
    } catch { break }
  }
  try { $l.Stop() } catch {}
}
Start-Sleep 3
$probe = HttpRequest 'POST' ('http://127.0.0.1:' + $stubPort + '/v1/chat/completions') '' @{ model = 'drael-v1'; messages = @() }
if ($probe.Http -ne 200 -or [string]$probe.Json.model -ne 'drael-v1') { Falhar ('stub drael sem resposta OpenAI-compativel http=' + $probe.Http) }
Log ('ESTAGEM_1_STUB_DRAEL ok=SIM porta=' + $stubPort)

# ---------------------------------------------------------------- ESTAGEM 1: API
if (-not (Test-Path $apiDll)) { Falhar ('DLL da API ausente: repo=' + $repo + ' dll=' + $apiDll) }
$env:ConnectionStrings__Default = 'Host=127.0.0.1;Port=5432;Database=' + $db + ';Username=postgres;Password=123456'
$env:Jwt__Key = 'r98-drill-dev-jwt-key-0123456789-abcd'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ApiSettings__BaseUrl = $apiUrl
$env:PlantaoProApi__BaseUrl = $apiUrl
# IA: cifra das chaves por tenant + base do Drael apontando para o stub local.
$env:Ai__EncryptionKey = 'a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2'
# Nome com dois-pontos nao aceita o sintaxe $env:; SetEnvironmentVariable cobre o mesmo escopo (processo).
[System.Environment]::SetEnvironmentVariable('Ai__Providers:Drael__BaseUrl', 'http://127.0.0.1:' + $stubPort + '/v1/')
$global:ApiProc = Start-Process -FilePath 'dotnet' -ArgumentList ('"' + $apiDll + '" --urls http://localhost:' + $porta) -WorkingDirectory (Join-Path $repo 'backend/PlantaoPro.Api') -WindowStyle Hidden -PassThru
if (-not (ApiAguardarNoAr 60)) { Falhar 'API nao ficou no ar em 60s' }
$owner = Get-NetTCPConnection -State Listen -LocalPort $porta -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $owner) { Falhar 'API no ar mas sem dono da porta' }
$global:PidApi = [int]$owner.OwningProcess
Log ('ESTAGEM_1_API_NO_AR ok=SIM pid=' + $global:PidApi)

# -------------------------------------------------- limpeza para rerun previsivel
LimparAiPorCnpjs @($cnpjTrial, $cnpjAtiva) | Out-Null
LimparMundoPorCnpj $cnpjTrial | Out-Null
LimparMundoPorCnpj $cnpjAtiva | Out-Null
LimparPlanoPorCodigo 'R98_PLANO_DRILL' | Out-Null
Log 'PRE_LIMPEZA_RERUN ok=SIM'

$tokAdm = FazerLogin 'superadmin@plantaopro.local' 'Super@123456'

# ---------------------------------------------------------------- PA_PLANO_DRILL
function PayloadPlano([double]$anual, [double]$dias, [bool]$permiteTrial) {
  return @{
    nome = 'R98 Plano Drill'
    descricao = 'Plano do drill r98'
    valorMensal = 100.00
    limiteMedicos = 0; limiteHospitais = 0; limitePlantoesMes = 0
    permiteMobile = $false; permiteBi = $false; permiteRelatoriosAvancados = $false; permiteIntegracoes = $false
    codigo = 'r98_plano_drill'
    slug = 'r98-plano-drill'
    valorAnual = $anual
    publico = $true; destaque = $false; ordem = 951; permiteWhiteLabel = $false
    permiteTrial = $permiteTrial
    diasTrial = $dias
  }
}
$rPlano = HttpRequest 'POST' ($apiUrl + '/api/planos') $tokAdm (PayloadPlano 1250.00 5 $true)
if ($rPlano.Http -ne 200) { Falhar ('PA_PLANO_DRILL http ' + $rPlano.Http + ' ' + $(if ($rPlano.Json) { $rPlano.Json.message } else { 'sem corpo' })) }
$idPlano = [string]$rPlano.Json.data

# ---------------------------------------------------------------- PA_MUNDOS (funil self-service)
$rT = HttpRequest 'POST' ($apiUrl + '/api/public/cadastro/finalizar') '' @{
  empresa      = @{ nomeFantasia = 'R98 Drill Trial'; razaoSocial = 'R98 Drill Trial LTDA'; cnpj = $cnpjTrial; segmento = 'Clinica'; cidade = 'Recife'; uf = 'PE'; emailCorporativo = 'r98-drill-trial@plantaopro-local.test' }
  plano        = @{ planoId = $idPlano; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
  usuarioAdmin = @{ nome = 'R98 Trial Admin'; email = 'r98-drill-trial@plantaopro-local.test'; cargo = 'Gestora'; senha = 'R98-trial-2026#Kp9z' }
  statusAssinatura = 'TRIAL'
  diasTrial      = 5
}
if ($rT.Http -ne 200) { Falhar ('PA_MUNDOS trial http ' + $rT.Http + ' ' + $(if ($rT.Json) { $rT.Json.message } else { 'sem corpo' })) }
$tTenantId = [string]$rT.Json.data.tenantId
$tStatus = Psq ("select coalesce(status,'') from plantaopro.assinaturas where id='" + $rT.Json.data.assinaturaId + "'")
if ($tStatus -ne 'TRIAL') { Falhar ('PA_MUNDOS trial status=' + $tStatus) }

$rA = HttpRequest 'POST' ($apiUrl + '/api/public/cadastro/finalizar') '' @{
  empresa      = @{ nomeFantasia = 'R98 Drill Ativa'; razaoSocial = 'R98 Drill Ativa LTDA'; cnpj = $cnpjAtiva; segmento = 'Clinica'; cidade = 'Olinda'; uf = 'PE'; emailCorporativo = 'r98-drill-ativa@plantaopro-local.test' }
  plano        = @{ planoId = $idPlano; periodicidade = 'MENSAL'; aceiteTermos = $true; aceitePrivacidade = $true; consentimentoLgpd = $true }
  usuarioAdmin = @{ nome = 'R98 Ativa Admin'; email = 'r98-drill-ativa@plantaopro-local.test'; cargo = 'Gestor'; senha = 'R98-ativa-2026#Kp9z' }
}
if ($rA.Http -ne 200) { Falhar ('PA_MUNDOS ativa http ' + $rA.Http + ' ' + $(if ($rA.Json) { $rA.Json.message } else { 'sem corpo' })) }
$aTenantId = [string]$rA.Json.data.tenantId
$aStatus = Psq ("select coalesce(status,'') from plantaopro.assinaturas where id='" + $rA.Json.data.assinaturaId + "'")
if ($aStatus -ne 'ATIVA') { Falhar ('PA_MUNDOS ativa status=' + $aStatus) }
Log ('PA_MUNDOS_OK trial_tenant=' + $tTenantId + ' ativa_tenant=' + $aTenantId)

$tokTrial = FazerLogin 'r98-drill-trial@plantaopro-local.test' 'R98-trial-2026#Kp9z'

# ---------------------------------------------------------------- AI_NAO_HABILITADA
$r0 = HttpRequest 'POST' ($apiUrl + '/api/ai/tarefas/resumo-pendente') $tokTrial $null
$s0 = ''; if ($r0.Json) { $s0 = [string]$r0.Json.statusKind }
$u0 = Psq ("select count(*) from plantaopro.ai_usos where tenant_id='" + $tTenantId + "' and status='SUCESSO'")
Log ('AI_TAREFA_NAO_HABILITADA_OK http=' + $r0.Http + ' status=' + $s0)
if ($r0.Http -ne 200) { Falhar ('AI_NAO_HABILITADA http ' + $r0.Http + ' esperado 200') }
if ($s0 -ne 'NAO_HABILITADO') { Falhar ('AI_NAO_HABILITADA status=' + $s0) }
if ($u0 -ne '0') { Falhar ('AI_NAO_HABILITADA usos_sucesso=' + $u0) }

# ---------------------------------------------------------------- AI_CONFIG (chave do tenant + cota 1)
$rCfg = HttpRequest 'PUT' ($apiUrl + '/api/ai/config/RESUMO_PENDENTE') $tokTrial @{
  habilitada          = $true
  provedor            = 'drael'
  modelo              = 'drael-v1'
  fallbackProvedor    = $null
  apiKey              = 'dk-r98-drill-local'
  limiteTokensEntrada = 4000
  limiteTokensSaida   = 1200
  timeoutS            = 30
  cotaMensalUsos      = 1
  orcamentoMensal     = $null
}
$cfgTask = ''; $cfgChave = $false
if ($rCfg.Json) { $cfgTask = [string]$rCfg.Json.taskCode; $cfgChave = [bool]$rCfg.Json.chaveDoTenantConfigurada }
Log ('AI_CONFIG_OK http=' + $rCfg.Http + ' task=' + $cfgTask + ' chave=' + ($(if ($cfgChave) { 'SIM' } else { 'NAO' })))
if ($rCfg.Http -ne 200) { Falhar ('AI_CONFIG http ' + $rCfg.Http + ' ' + $(if ($rCfg.Json) { $rCfg.Json.message } else { 'sem corpo' })) }
if ($cfgTask -ne 'RESUMO_PENDENTE') { Falhar ('AI_CONFIG task=' + $cfgTask) }
if (-not $cfgChave) { Falhar 'AI_CONFIG chave do tenant nao confirmada' }

# ---------------------------------------------------------------- AI_TESTE_CONEXAO (chave do tenant -> stub)
$rTc = HttpRequest 'POST' ($apiUrl + '/api/ai/test-conexao') $tokTrial @{ provedor = 'drael' }
$tcKind = ''; if ($rTc.Json) { $tcKind = [string]$rTc.Json.statusKind }
Log ('AI_TESTE_CONEXAO_OK http=' + $rTc.Http + ' status=' + $tcKind)
if ($rTc.Http -ne 200 -or $tcKind -ne 'OK') { Falhar ('AI_TESTE_CONEXAO http ' + $rTc.Http + ' status=' + $tcKind) }

# ---------------------------------------------------------------- AI_GERACAO (jornada TRIAL -> stub)
$rG = HttpRequest 'POST' ($apiUrl + '/api/ai/tarefas/resumo-pendente') $tokTrial $null
$gOk = $false; $gProv = ''; $gTxt = ''; $gEscopo = ''
if ($rG.Json) { $gOk = [bool]$rG.Json.success; $gProv = [string]$rG.Json.provedor; $gTxt = ([string]$rG.Json.texto).Trim(); $gEscopo = [string]$rG.Json.escopo }
Log ('AI_GERACAO_OK http=' + $rG.Http + ' sucesso=' + ($(if ($gOk) { 'SIM' } else { 'NAO' })) + ' provedor=' + $gProv + ' texto=' + ($(if ($gTxt.Length -gt 0) { 'SIM' } else { 'NAO' })))
if ($rG.Http -ne 200 -or -not $gOk) { Falhar ('AI_GERACAO http ' + $rG.Http + ' success=' + $gOk) }
if ($gProv -ne 'drael') { Falhar ('AI_GERACAO provedor=' + $gProv) }
if ($gTxt.Length -le 0) { Falhar 'AI_GERACAO texto vazio' }
if ($gEscopo -notlike '*America/Sao_Paulo*') { Falhar ('AI_GERACAO escopo sem fuso: ' + $gEscopo) }

# ---------------------------------------------------------------- AI_USO_REGISTRADO
$u1 = Psq ("select count(*) from plantaopro.ai_usos where tenant_id='" + $tTenantId + "' and task_code='RESUMO_PENDENTE' and status='SUCESSO'")
$uProv = Psq ("select coalesce(provedor,'') from plantaopro.ai_usos where tenant_id='" + $tTenantId + "' and task_code='RESUMO_PENDENTE' and status='SUCESSO' order by created_at desc limit 1")
Log ('AI_USO_REGISTRADO_OK task=RESUMO_PENDENTE sucesso=' + $u1 + ' provedor=' + $uProv)
if ($u1 -ne '1') { Falhar ('AI_USO_REGISTRADO sucesso=' + $u1 + ' esperado 1') }
if ($uProv -ne 'drael') { Falhar ('AI_USO_REGISTRADO provedor=' + $uProv) }

# ---------------------------------------------------------------- AI_COTA_EXCEDIDA (segunda geracao no mes)
$rG2 = HttpRequest 'POST' ($apiUrl + '/api/ai/tarefas/resumo-pendente') $tokTrial $null
$g2Kind = ''; if ($rG2.Json) { $g2Kind = [string]$rG2.Json.statusKind }
$u2 = Psq ("select count(*) from plantaopro.ai_usos where tenant_id='" + $tTenantId + "' and task_code='RESUMO_PENDENTE' and status='SUCESSO'")
Log ('AI_COTA_EXCEDIDA_OK http=' + $rG2.Http + ' status=' + $g2Kind + ' sucesso=' + $u2)
if ($rG2.Http -ne 200) { Falhar ('AI_COTA_EXCEDIDA http ' + $rG2.Http) }
if ($g2Kind -ne 'COTA_EXCEDIDA') { Falhar ('AI_COTA_EXCEDIDA status=' + $g2Kind) }
if ($u2 -ne '1') { Falhar ('AI_COTA_EXCEDIDA sucesso=' + $u2 + ' esperado 1') }

# ---------------------------------------------------------------- AI_VAZIO (ATIVA limpa, sem gastar provedor)
# Etapas com criterio automatico seguem os DADOS na reavaliacao (concluir por SQL so
# persiste, nao atende). Remover o criterio + concluir MANUAL deixa a etapa na branca
# humana (atendida = concluido), deterministica para o mundo limpo do VAZIO.
Psq ("update plantaopro.tenant_onboarding_checklist set criterio_codigo=null, concluido=true, status='CONCLUIDO', origem_conclusao='MANUAL', concluido_em=now(), reg_update=now() where tenant_id='" + $aTenantId + "'") | Out-Null
$tokAtiva = FazerLogin 'r98-drill-ativa@plantaopro-local.test' 'R98-ativa-2026#Kp9z'
$rV = HttpRequest 'POST' ($apiUrl + '/api/ai/tarefas/resumo-pendente') $tokAtiva $null
$vKind = ''; $vOk = $false
if ($rV.Json) { $vKind = [string]$rV.Json.statusKind; $vOk = [bool]$rV.Json.success }
$uAtiva = Psq ("select count(*) from plantaopro.ai_usos where tenant_id='" + $aTenantId + "' and status='SUCESSO'")
Log ('AI_VAZIO_OK http=' + $rV.Http + ' status=' + $vKind)
if ($rV.Http -ne 200 -or $vKind -ne 'VAZIO') { Falhar ('AI_VAZIO http ' + $rV.Http + ' status=' + $vKind) }
if (-not $vOk) { Falhar 'AI_VAZIO deveria vir com sucesso=true' }
if ($uAtiva -ne '0') { Falhar ('AI_VAZIO usou provedor (sucesso=' + $uAtiva + ')') }

# ---------------------------------------------------------------- teardown
try { if ($global:PidApi) { Stop-Process -Id $global:PidApi -Force -ErrorAction SilentlyContinue } } catch {}
try { if (Test-Path $stubFlag) { Remove-Item $stubFlag -Force } } catch {}
try { if ($global:JobStub) { Stop-Job $global:JobStub -ErrorAction SilentlyContinue; Remove-Job $global:JobStub -Force -ErrorAction SilentlyContinue } } catch {}
$global:JobStub = $null
Start-Sleep 2
$resto = Get-NetTCPConnection -State Listen -LocalPort $porta -ErrorAction SilentlyContinue | Select-Object -First 1
if ($resto) { try { Stop-Process -Id ([int]$resto.OwningProcess) -Force -ErrorAction SilentlyContinue } catch {}; Start-Sleep 2 }
if (-not (PortaLivre $porta) -or (-not (PortaLivre $stubPort))) { Falhar 'portas ainda ocupadas no teardown' }
Log 'PORTAS_LIVRES=SIM'
if ($global:Falhou) { Falhar 'resultado final negativo' }
Log 'RESULTADO=PASSEI'
EscreverSaida
exit 0
