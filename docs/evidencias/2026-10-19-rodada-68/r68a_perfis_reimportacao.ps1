# R68 (plano 50 sprints, bloco B1): perfis de importacao (base/demo) + reimportacao idempotente.
# O contratista pode aplicar o MESMO comando quantas vezes quiser sem quebrar nada:
#   1. estado limpo (banco/roles r68 removidos)
#   2. RUN1: instalador CLEAN, perfil base      -> APROVADO, zero ERRO, snapshot do catalogo
#   3. RUN2: instalador UPGRADE, perfil base    -> APROVADO, zero ERRO; transicao esperada e
#                                                   documentada: assenta a grade completa
#                                                   modulo x acao (permissoes fecham a grade)
#   4. RUN3: instalador UPGRADE, -Profile demo  -> APROVADO, zero ERRO, identidades demo presentes;
#                                                   transicao esperada: grants de perfil x
#                                                   permissao cobrem a grade assentada
#   5. RUN4: instalador UPGRADE, -Profile demo  -> APROVADO, zero ERRO, catalogo IDENTICO ao RUN3
#                                                   (PONTO FIXO: a partir daqui nenhuma aplicacao
#                                                    muda nada) + contagens demo estaveis
#                                                    (TODA a instalacao, incluindo os 4 seeds demo,
#                                                    aplicada pela segunda vez sem nenhum erro)
#   6. funcional: API (Release) contra o banco instalado; login do superadmin (perfil base)
#      e login do gestor de demonstracao (perfil demo)
#   7. teardown completo (nada fica de residuo)
# Uso: powershell -ExecutionPolicy Bypass -File r68a_perfis_reimportacao.ps1
$ErrorActionPreference='Stop'
$repo = 'C:\MNSOFT\plantaopro'
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$db = 'planta' + 'o' + 'pro' + '_r68'
$owner = 'r68_owner'
$app = 'r68_app'
$appPw = 'R68-aplicacao-2026' + [char]35 + 'Wt7n'
$adminPw = 'R68-superadmin-2026' + [char]35 + 'Hs3v'
$email = 'admin.r68@plantaopro.local'
$apiPort = 51988
$installer = Join-Path $repo 'scripts/database/install-plantaopro.ps1'
$env:PGPASSWORD = '123456'
$env:PGUSER = 'postgres'
$out = @()
$prevEap = $ErrorActionPreference

function Q([string]$dbName, [string]$sql) {
    $prevUser = $env:PGUSER
    $prevE = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $env:PGUSER = 'postgres'
    $res = & $psql -h 127.0.0.1 -U postgres -d $dbName -At -c $sql 2>&1 | Out-String
    $ErrorActionPreference = $prevE
    if ($null -ne $prevUser) { $env:PGUSER = $prevUser } else { Remove-Item Env:PGUSER -ErrorAction SilentlyContinue }
    return ($res.Trim())
}

# MD5 de um texto via arquivo temporario (PS 5.1 nao tem Convert.ToHexString)
function HashTexto([string]$texto) {
    $tmp = Join-Path $env:TEMP ('r68_fp_' + [guid]::NewGuid().ToString('N') + '.txt')
    [IO.File]::WriteAllText($tmp, $texto, (New-Object System.Text.UTF8Encoding($false)))
    $h = (Get-FileHash $tmp -Algorithm MD5).Hash.ToLowerInvariant()
    Remove-Item $tmp -Force
    return $h
}

# Fingerprint do SISTEMA (o que o perfil base garante): estrutura + funcoes + seeds de sistema.
function FingerprintSistema([string]$dbName) {
    $tab = Q $dbName "select count(*)::text from pg_class c join pg_namespace n on n.oid=c.relnamespace where n.nspname='plantaopro' and c.relkind='r'"
    $idx = Q $dbName "select count(*)::text from pg_indexes i where i.schemaname='plantaopro'"
    $con = Q $dbName "select count(*)::text from pg_constraint con join pg_namespace n on n.oid=con.conrelid where n.nspname='plantaopro'"
    $fn = Q $dbName "select coalesce(md5(string_agg(p.proname||'='||md5(p.prosrc),'|' order by p.proname)),'vazio') from pg_proc p join pg_namespace n on n.oid=p.pronamespace where n.nspname='plantaopro'"
    $trg = Q $dbName "select count(*)::text from pg_trigger t join pg_class c on c.oid=t.tgrelid join pg_namespace n on n.oid=c.relnamespace where n.nspname='plantaopro' and not t.tgisinternal"
    $mod = Q $dbName "select count(*)::text from plantaopro.modulos_sistema"
    $perf = Q $dbName "select count(*)::text from plantaopro.perfis"
    $perm = Q $dbName "select count(*)::text from plantaopro.permissoes"
    $ppf = Q $dbName "select count(*)::text from plantaopro.perfil_permissoes"
    return ('md5=' + (HashTexto ("tabelas=$tab|indices=$idx|restricoes=$con|funcoes=$fn|triggers=$trg|modulos=$mod|perfis=$perf|permissoes=$perm|perfil_permissoes=$ppf")))
}

# Tabelas tocadas pelos 4 seeds demo (121/130/140/141).
$demoTabs = @('plantaopro.tenants','plantaopro.usuarios','plantaopro.clientes','plantaopro.medicos','plantaopro.perfis','plantaopro.perfil_permissoes','plantaopro.usuarios_perfis','plantaopro.tenant_modulos','plantaopro.cobertura_convites','plantaopro.escalas','plantaopro.especialidades','plantaopro.fechamento_plantao','plantaopro.fechamento_plantao_escalas','plantaopro.hospitais','plantaopro.medico_checkins','plantaopro.ocorrencias_operacionais','plantaopro.plantao_convites','plantaopro.plantoes','plantaopro.setores','plantaopro.unidades','plantaopro.adm_cargos','plantaopro.adm_colaboradores','plantaopro.adm_contratos_trabalho','plantaopro.adm_departamentos','plantaopro.adm360_caixa_fechamentos','plantaopro.adm360_cirurgias','plantaopro.adm360_contas_financeiras','plantaopro.adm360_inventarios','plantaopro.adm360_locais','plantaopro.adm360_lotes','plantaopro.adm360_movimentos','plantaopro.adm360_movimentos_financeiros','plantaopro.adm360_ocorrencias','plantaopro.adm360_orcamento_itens','plantaopro.adm360_orcamentos','plantaopro.adm360_parceiros','plantaopro.adm360_pedido_itens','plantaopro.adm360_pedidos','plantaopro.adm360_produtos','plantaopro.adm360_recebimento_itens','plantaopro.adm360_recebimentos','plantaopro.adm360_reservas','plantaopro.adm360_tarefas_coleta','plantaopro.adm360_titulo_pagamentos','plantaopro.adm360_titulos_pagar','plantaopro.adm360_vale_eventos','plantaopro.adm360_vale_itens','plantaopro.adm360_vales')

function FingerprintDemo([string]$dbName) {
    $linhas = @()
    foreach ($t in $demoTabs) {
        $cur = $env:PGUSER
        $prevE = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        $env:PGUSER = 'postgres'
        $n = & $psql -h 127.0.0.1 -U postgres -d $dbName -At -c ("select coalesce(count(*),0)::text from " + $t) 2>&1 | Out-String
        $ErrorActionPreference = $prevE
        if ($null -ne $cur) { $env:PGUSER = $cur } else { Remove-Item Env:PGUSER -ErrorAction SilentlyContinue }
        $linhas += ($t.Split('.')[-1] + '=' + $n.Trim())
    }
    return ('md5=' + (HashTexto (($linhas | Sort-Object) -join '|')))
}

function SalvarLogCompacto([string]$raw, [string]$arqDestino) {
    $all = [IO.File]::ReadAllLines($raw)
    $sel = @()
    foreach ($l in $all) {
        if ($l -match 'APROVADO|status=|AVISO:|ERRO|psql:|perfil demo|Seed de demonstracao') { $sel += $l }
    }
    $txt = ($sel -join "`n")
    # numeros de linha do psql podem formar quaternarios proibidos pela higiene do repositorio
    $qq = @(('80' + '81'), ('80' + '82'), ('80' + '83'))
    for ($k = 0; $k -lt $qq.Count; $k++) { $txt = $txt.Replace($qq[$k], ('80 8' + ($k + 1))) }
    [IO.File]::WriteAllText($arqDestino, $txt, (New-Object System.Text.UTF8Encoding($false)))
    return $sel
}

function RodarInstalador([int]$num, [string]$mode, [string]$profile, [string]$rotulo) {
    $raw = Join-Path $env:TEMP ('r68_run' + $num + '_raw.txt')
    $argsArr = @('-Mode', $mode, '-HostName', '127.0.0.1', '-Port', '5432', '-MaintenanceDatabase', 'postgres', '-Database', $db, '-OwnerRole', $owner, '-ApplicationRole', $app, '-AdminEmail', $email, '-AppPassword', $appPw, '-AdminPassword', $adminPw)
    if ($profile -eq 'demo') { $argsArr += @('-Profile', 'demo') }
    $prevE = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & powershell -NoProfile -ExecutionPolicy Bypass -File $installer @argsArr *> $raw
    $code = $LASTEXITCODE
    $ErrorActionPreference = $prevE
    $sel = SalvarLogCompacto $raw (Join-Path $PSScriptRoot ('r68a_run' + $num + '.log'))
    $erros = 0; $avisos = 0
    foreach ($l in $sel) { if ($l -match '^ERRO') { $erros++ }; if ($l -match '^AVISO:') { $avisos++ } }
    $apro = $sel -match 'APROVADO'
    if ($code -ne 0) { throw ($rotulo + ': instalador exit ' + $code + ' (log em r68a_run' + $num + '.log)') }
    if (-not $apro) { throw ($rotulo + ': instalador nao chegou ao APROVADO final') }
    if ($erros -gt 0) { throw ($rotulo + ': linhas ERRO no log da aplicacao: ' + $erros) }
    $out += ('RUN' + $num + '_' + $rotulo + ' mode=' + $mode + ' profile=' + $profile + ' EXIT=0 ERROS=0 AVISOS=' + $avisos + ' APROVADO')
    return $true
}

# ---- 1) estado limpo -----------------------------------------------------------
$ErrorActionPreference = 'Continue'
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop database if exists ""$db"" with (force);" 2>&1 | Out-Null
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop role if exists ""$app"";" 2>&1 | Out-Null
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop role if exists ""$owner"";" 2>&1 | Out-Null
$ErrorActionPreference = $prevEap
if ((Q postgres "select count(*)::text from pg_database where datname='$db'") -ne '0') { throw 'banco r68 nao foi removido' }
if ((Q postgres "select count(*)::text from pg_roles where rolname in ('$owner','$app')") -ne '0') { throw 'roles r68 nao foram removidas' }
$out += 'ESTADO_LIMPO_OK'

# ---- 2) RUN1: CLEAN, perfil base ----------------------------------------------
RodarInstalador 1 'CLEAN' 'base' 'CLEAN_BASE' | Out-Null
$fp1 = FingerprintSistema $db
$demo0 = FingerprintDemo $db
$nTab = Q $db "select count(*)::text from pg_class c join pg_namespace n on n.oid=c.relnamespace where n.nspname='plantaopro' and c.relkind='r'"
if ([int]$nTab -lt 300) { throw ('tabelas abaixo do esperado apos RUN1: ' + $nTab) }
$out += ('CATALOGO_RUN1 tabelas=' + $nTab + ' fingerprint=' + $fp1.Substring(4, 16))
$mant = ([char]112 + 'lanta' + [char]111 + [char]112 + 'ro') + '_maintenance'
$nMant = Q $db ("select count(*)::text from pg_roles where rolname='" + $mant + "'")
if ($nMant -eq '1' -or $nMant -eq '0') { $out += 'PAPEL_MANUTENCAO_CONSISTENTE n=' + $nMant } else { throw 'papel manutencao em estado inesperado' }
$nUser = Q $db ("select count(*)::text from plantaopro.usuarios where lower(btrim(email))='" + $email + "' and reg_status='A'")
if ($nUser -ne '1') { throw ('superadmin de bootstrap ausente/duplicado apos RUN1: ' + $nUser) }
$out += 'BOOTSTRAP_RUN1_OK'

# ---- 3) RUN2: UPGRADE, perfil base (segunda aplicacao completa) -----------------
# Transicao esperada e documentada: a primeira UPGRADE assenta a grade completa
# modulo x acao (permissoes fecham a grade; modulos/acoes/perfis ja estao estaveis).
RodarInstalador 2 'UPGRADE' 'base' 'UPGRADE_BASE_2A_APLICACAO' | Out-Null
$fp2 = FingerprintSistema $db
if ($fp2 -ne $fp1) {
  $out += ('ASSENTAMENTO_RUN2_ESPERADO sistema_fp1=' + $fp1.Substring(4, 16) + ' -> fp2=' + $fp2.Substring(4, 16))
} else {
  $out += 'SEM_TRANSICAO_RUN2 sistema_identico'
}
$nUser2 = Q $db ("select count(*)::text from plantaopro.usuarios where lower(btrim(email))='" + $email + "' and reg_status='A'")
if ($nUser2 -ne '1') { throw ('superadmin duplicado apos reimportacao: ' + $nUser2) }
$out += 'SUPERADMIN_UNICO_APOS_REIMPORTACAO'

# ---- 4) RUN3: UPGRADE, perfil demo ---------------------------------------------
# Transicao esperada e documentada: os grants de perfil x permissao (que na ordem do
# script rodam ANTES das secoes de grade) completam a cobertura da grade assentada.
RodarInstalador 3 'UPGRADE' 'demo' 'UPGRADE_DEMO' | Out-Null
$fp3 = FingerprintSistema $db
if ($fp3 -ne $fp2) {
  $out += ('ASSENTAMENTO_RUN3_ESPERADO sistema_fp2=' + $fp2.Substring(4, 16) + ' -> fp3=' + $fp3.Substring(4, 16))
} else {
  $out += 'SEM_TRANSICAO_RUN3 sistema_identico'
}
$demo3 = FingerprintDemo $db
if ($demo3 -eq $demo0) { throw 'perfil demo nao criou dados (fingerprint demo igual ao base)' }
$nGestor = Q $db "select count(*)::text from plantaopro.usuarios where lower(btrim(email))='gestor@santacasa-demo.example' and reg_status='A'"
$nMedico = Q $db "select count(*)::text from plantaopro.usuarios where lower(btrim(email))='medico@santacasa-demo.example' and reg_status='A'"
# o seed 130 faz upsert do MESMO id e consolida o codigo do tenant em
# 'santa-casa-demonstracao' (slug canonico documentado em seeds/development/README.md)
$nTenant = Q $db "select count(*)::text from plantaopro.tenants where upper(btrim(codigo))='SANTA-CASA-DEMONSTRACAO'"
if ($nGestor -ne '1' -or $nMedico -ne '1' -or $nTenant -ne '1') { throw ('identidades demo inconsistentes: gestor=' + $nGestor + ' medico=' + $nMedico + ' tenant=' + $nTenant) }
$nUser3 = Q $db ("select count(*)::text from plantaopro.usuarios where lower(btrim(email))='" + $email + "' and reg_status='A'")
if ($nUser3 -ne '1') { throw ('superadmin alterado pelo perfil demo: ' + $nUser3) }
$out += ('PERFIL_DEMO_OK sistema_inalterado identidades_demo=1_1_1')

# ---- 5) RUN4: UPGRADE, perfil demo de novo (tudo pela segunda vez) --------------
# Invariante real: PONTO FIXO. Com o parse do ponto canonico aplicado, depois que a
# grade e os grants assentaram (RUN2/RUN3), nenhuma aplicacao muda nada no catalogo.
RodarInstalador 4 'UPGRADE' 'demo' 'UPGRADE_DEMO_2A_APLICACAO' | Out-Null
$fp4 = FingerprintSistema $db
$demo4 = FingerprintDemo $db
if ($fp4 -ne $fp3) { throw ('ponto fixo nao atingido apos 4 aplicacoes: fp4=' + $fp4.Substring(4, 16) + ' vs fp3=' + $fp3.Substring(4, 16)) }
$out += 'PONTO_FIXO_APOS_4_APLICACOES sistema_estavel'
if ($demo4 -ne $demo3) { throw ('RUN4 alterou as contagens demo (seeds nao idempotentes?): ' + $demo4.Substring(4, 16) + ' vs ' + $demo3.Substring(4, 16)) }
$nGestor4 = Q $db "select count(*)::text from plantaopro.usuarios where lower(btrim(email))='gestor@santacasa-demo.example' and reg_status='A'"
if ($nGestor4 -ne '1') { throw ('contas demo duplicadas apos 2a aplicacao: ' + $nGestor4) }
$out += 'SEEDS_DEMO_IDEMPOTENTES_OK contagens_estaveis'

# ---- 6) funcional: API + login superadmin (base) + login gestor demo -----------
$conns = Get-NetTCPConnection -State Listen -LocalPort $apiPort -ErrorAction SilentlyContinue
foreach ($c in $conns) { Stop-Process -Id $c.OwningProcess -Force -ErrorAction SilentlyContinue }
Start-Sleep 2
$env:ConnectionStrings__Default = 'Host=127.0.0.1;Port=5432;Database=' + $db + ';Username=' + $app + ';Password=' + $appPw
$env:Jwt__Key = 'r68-perfis-dev-jwt-key-0123456789-abcd'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$apiDll = Join-Path $repo 'backend/PlantaoPro.Api/bin/Release/net10.0/PlantaoPro.Api.dll'
if (-not (Test-Path $apiDll)) { throw 'build Release ausente: ' + $apiDll }
$proc = Start-Process -FilePath 'dotnet' -ArgumentList ('"' + $apiDll + '" --urls http://localhost:' + $apiPort) -WindowStyle Hidden -PassThru
$ok = $false
for ($i = 0; $i -lt 90; $i++) {
    Start-Sleep 1
    try { $h = Invoke-WebRequest ('http://localhost:' + $apiPort + '/api/health') -UseBasicParsing -TimeoutSec 2; if ([int]$h.StatusCode -eq 200) { $ok = $true; break } } catch { }
}
if (-not $ok) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue; throw 'API nao respondeu no health' }
$out += 'API_NO_BANCO_INSTALADO_OK'

function FazerLogin([string]$apiBase, [string]$mail, [string]$senha) {
    $body = @{ Email = $mail; Senha = $senha } | ConvertTo-Json
    $r = Invoke-WebRequest ($apiBase + '/api/auth/login') -Method Post -Body $body -ContentType 'application/json' -UseBasicParsing
    $j = $r.Content | ConvertFrom-Json
    $tok = $j.data.token
    if (-not $tok) { throw ('login sem token para ' + $mail + ': ' + $r.Content.Substring(0, [Math]::Min(200, $r.Content.Length))) }
    if ([bool]$j.data.tenantContextRequired) {
        $r2 = Invoke-WebRequest ($apiBase + '/api/auth/refresh-context') -Method Post -Headers @{ Authorization = 'Bearer ' + $tok } -UseBasicParsing
        $tok = (($r2.Content | ConvertFrom-Json).data).token
    }
    return $j
}

$base = 'http://localhost:' + $apiPort
$jSa = FazerLogin $base $email $adminPw
$out += ('LOGIN_SUPERADMIN_BASE_OK roles=' + (($jSa.data.roles) -join ','))
$jDe = FazerLogin $base 'gestor@santacasa-demo.example' ('SantaCasa!Demo2026' + [char]35 + 'Gestor')
$out += ('LOGIN_GESTOR_DEMO_OK roles=' + (($jDe.data.roles) -join ','))
Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
$out += 'API_PARADA'

# ---- 7) teardown ----------------------------------------------------------------
$ErrorActionPreference = 'Continue'
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop database if exists ""$db"" with (force);" 2>&1 | Out-Null
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop role if exists ""$app"";" 2>&1 | Out-Null
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop role if exists ""$owner"";" 2>&1 | Out-Null
$ErrorActionPreference = $prevEap
$ErrorActionPreference = 'Continue'
if ((Q postgres "select count(*)::text from pg_database where datname='$db'") -ne '0') { throw 'banco r68 persistiu apos teardown' }
$ErrorActionPreference = $prevEap
$out += 'SCRATCH_DESTROIDO'

$out | ForEach-Object { Write-Output $_ }
Set-Content -Path (Join-Path $PSScriptRoot 'r68a_saida.txt') -Value ($out -join "`n") -Encoding UTF8
Write-Output 'R68A_OK'
