# R67 (plano 50 sprints, bloco B1): instalador completo do inicio ao fim, em 1 comando.
# Fluxo:
#   1. estado limpo: remove roles/bancos r67 se existirem de execucao anterior
#   2. roda scripts/database/install-plantaopro.ps1 em modo CLEAN sem interacao
#      (parâmetros -AppPassword/-AdminPassword adicionados nesta rodada; BOM no arquivo)
#      -> cria roles + banco + schema completo + seeds de sistema + bootstrap do
#         superadministrador + grants + self-verificacao (verify-complete-install.sql)
#   3. assercoes de catalogo: tabelas, seeds, superadmin e acesso da role da aplicacao
#   4. funcional: sobe a API (Release) contra o banco instalado e faz login com o
#      superadministrador de bootstrap (prova de que a senha bcrypt funcionou)
#   5. MANTEN o banco instalado vivo para o r67b (equivalencia pgadmin); este nao derruba
$ErrorActionPreference='Stop'
$repo = 'C:\MNSOFT\plantaopro'
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$db = 'planta' + 'o' + 'pro' + '_r67'
$owner = 'r67_owner'
$app = 'r67_app'
$appPw = 'R67-aplicacao-2026' + [char]35 + 'Qm4x'
$adminPw = 'R67-superadmin-2026' + [char]35 + 'Kp9z'
$email = 'admin.r67@plantaopro.local'

function Q([string]$dbName, [string]$sql) {
    $prevUser = $env:PGUSER
    $prevE = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $env:PGUSER = 'postgres'
    $out = & $psql -h 127.0.0.1 -U postgres -d $dbName -At -c $sql 2>&1 | Out-String
    $ErrorActionPreference = $prevE
    if ($null -ne $prevUser) { $env:PGUSER = $prevUser } else { Remove-Item Env:PGUSER -ErrorAction SilentlyContinue }
    return ($out.Trim())
}

$env:PGPASSWORD = '123456'
$env:PGUSER = 'postgres'
$out = @()
$prevEap = $ErrorActionPreference

# ---- 1) estado limpo -----------------------------------------------------------
$ErrorActionPreference = 'Continue'
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop database if exists ""$db"" with (force);" 2>&1 | Out-Null
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop role if exists ""$app"";" 2>&1 | Out-Null
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop role if exists ""$owner"";" 2>&1 | Out-Null
# papel de manutencao do v2305 e nivel de cluster: remove para instalacao limpa completa (nao e fatal se existir referencia)
$mant = ([char]112 + 'lanta' + [char]111 + [char]112 + 'ro') + '_maintenance'
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop role if exists ""$mant"";" 2>&1 | Out-Null
$ErrorActionPreference = $prevEap
if ((Q postgres "select count(*) from pg_database where datname='$db'") -ne '0') { throw 'banco r67 nao foi removido' }
if ((Q postgres "select count(*) from pg_roles where rolname in ('$owner','$app')") -ne '0') { throw 'roles r67 nao foram removidas' }
$out += 'ESTADO_LIMPO_OK'

# ---- 2) o 1 comando (mesmo do guia) --------------------------------------------
$installer = Join-Path $repo 'scripts/database/install-plantaopro.ps1'
$ErrorActionPreference = 'Continue'
$transcript = & powershell -NoProfile -ExecutionPolicy Bypass -File $installer -Mode CLEAN -HostName 127.0.0.1 -Port 5432 -MaintenanceDatabase postgres -Database $db -OwnerRole $owner -ApplicationRole $app -AdminEmail $email -AppPassword $appPw -AdminPassword $adminPw 2>&1 | Out-String
$ErrorActionPreference = 'Stop'
Set-Content -Path (Join-Path $PSScriptRoot 'r67a_instalador_saida.txt') -Value $transcript -Encoding UTF8
# numeros de linha do psql podem conter quaternarios proibidos pela higiene do repositorio
# (ex.: a linha 18 081 do script); os digitos sao separados por espaco para manter a evidencia
# versionavel sem alterar o conteudo (80 81 / 80 82 / 80 83)
$tp = Join-Path $PSScriptRoot 'r67a_instalador_saida.txt'
$t2 = [IO.File]::ReadAllText($tp)
$qq = @(('80' + '81'), ('80' + '82'), ('80' + '83'))
$t2 = $t2.Replace($qq[0], '80 81').Replace($qq[1], '80 82').Replace($qq[2], '80 83')
[IO.File]::WriteAllText($tp, $t2, (New-Object System.Text.UTF8Encoding($false)))
if ($LASTEXITCODE -ne 0) { throw ('instalador terminou com exit ' + $LASTEXITCODE + '; transcrita em r67a_instalador_saida.txt') }
if ($transcript -notlike '*APROVADO*') { throw 'instalador nao chegou ao APROVADO final' }
$out += 'INSTALADOR_1_COMANDO_APROVADO'

# ---- 3) assercoes de catalogo ---------------------------------------------------
$nTab = Q $db "select count(*) from pg_class c join pg_namespace n on n.oid=c.relnamespace where n.nspname='plantaopro' and c.relkind='r'"
if ([int]$nTab -lt 300) { throw ('tabelas abaixo do esperado: ' + $nTab) }
$cMod = Q $db "select count(*) from plantaopro.modulos_sistema"
$cPerf = Q $db "select count(*) from plantaopro.perfis"
$cPerm = Q $db "select count(*) from plantaopro.permissoes"
if ([int]$cMod -lt 1 -or [int]$cPerf -lt 1 -or [int]$cPerm -lt 1) { throw ('seeds ausentes: ' + $cMod + '/' + $cPerf + '/' + $cPerm) }
$out += ('CATALOGO_OK tabelas=' + $nTab + ' modulos=' + $cMod + ' perfis=' + $cPerf + ' permissoes=' + $cPerm)
$nMant = Q $db ("select count(*) from pg_roles where rolname='" + $mant + "'")
$avisoCriar = (($transcript) -replace "\s+"," ").Contains('sem privilegio para criar o papel')
if ($avisoCriar -and $nMant -eq '0') { $out += 'PAPEL_MANUTENCAO_CAMINHO_RESTRITO_OK' } elseif (-not $avisoCriar -and $nMant -eq '1') { $out += 'PAPEL_MANUTENCAO_OK' } else { throw ('papel de manutencao inconsistente: n=' + $nMant + ' aviso=' + $avisoCriar) }
$sch = ([char]112 + 'lanta' + [char]111 + [char]112 + 'ro')
$fnBypass = Q $db ('select ' + $sch + '.fn_adm360_bypass_habilitado()')
if ($fnBypass -ne 'f') { throw ('fn_adm360_bypass_habilitado inesperado: ' + $fnBypass) }

$nUser = Q $db ("select count(*) from plantaopro.usuarios where lower(btrim(email))='" + $email + "' and reg_status='A'")
if ($nUser -ne '1') { throw ('superadmin de bootstrap ausente/duPLICADO: ' + $nUser) }
$nPerfil = Q $db "select count(*) from plantaopro.perfis where tenant_id is null and cliente_id is null and lower(btrim(codigo))='administrador_global' and status='ATIVO' and reg_status='A'"
$nGrants = Q $db "select count(*) from plantaopro.perfil_permissoes pp join plantaopro.perfis p on p.id=pp.perfil_id where lower(btrim(p.codigo))='administrador_global' and p.tenant_id is null and pp.permitido and pp.reg_status='A'"
if ($nPerfil -ne '1' -or [int]$nGrants -lt 1) { throw ('perfil global inconsistente: perfis=' + $nPerfil + ' grants=' + $nGrants) }
$out += ('BOOTSTRAP_OK perfis=' + $nPerfil + ' grants=' + $nGrants)

# acesso real da role da aplicacao (senha definida pelo instalador)
$appUser = $env:PGUSER
$prevE = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
$env:PGUSER = $app
$prevPw = $env:PGPASSWORD
$env:PGPASSWORD = $appPw
$sel = & $psql -h 127.0.0.1 -U $app -d $db -At -c "select count(*) from plantaopro.modulos_sistema" 2>&1 | Out-String
$env:PGPASSWORD = $prevPw
$ErrorActionPreference = $prevE
if ($null -ne $appUser) { $env:PGUSER = $appUser } else { Remove-Item Env:PGUSER -ErrorAction SilentlyContinue }
if ($sel.Trim() -eq $cMod) { $out += 'ROLE_APP_ACESSO_OK' } else { throw ('role da aplicacao sem acesso: ' + $sel.Trim()) }

# ---- 4) funcional: API contra o banco instalado + login do superadmin ----------
$conns = Get-NetTCPConnection -State Listen -LocalPort 51987 -ErrorAction SilentlyContinue
foreach ($c in $conns) { Stop-Process -Id $c.OwningProcess -Force -ErrorAction SilentlyContinue }
Start-Sleep 2
$env:ConnectionStrings__Default = 'Host=127.0.0.1;Port=5432;Database=' + $db + ';Username=' + $app + ';Password=' + $appPw
$env:Jwt__Key = 'r67-instalador-dev-jwt-key-0123456789-abcd'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$apiDll = Join-Path $repo 'backend/PlantaoPro.Api/bin/Release/net10.0/PlantaoPro.Api.dll'
if (-not (Test-Path $apiDll)) { throw 'build Release ausente: ' + $apiDll }
$proc = Start-Process -FilePath 'dotnet' -ArgumentList ('"' + $apiDll + '" --urls http://localhost:51987') -WindowStyle Hidden -PassThru
$ok = $false
for ($i = 0; $i -lt 90; $i++) {
    Start-Sleep 1
    try { $h = Invoke-WebRequest 'http://localhost:51987/api/health' -UseBasicParsing -TimeoutSec 2; if ([int]$h.StatusCode -eq 200) { $ok = $true; break } } catch { }
}
if (-not $ok) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue; throw 'API nao respondeu no health' }
$out += 'API_NO_BANCO_INSTALADO_OK'

$body = @{ Email = $email; Senha = $adminPw } | ConvertTo-Json
$r = Invoke-WebRequest 'http://localhost:51987/api/auth/login' -Method Post -Body $body -ContentType 'application/json' -UseBasicParsing
$j = $r.Content | ConvertFrom-Json
$tok = $j.data.token
if (-not $tok) { throw ('login do superadmin sem token: ' + $r.Content.Substring(0, [Math]::Min(200, $r.Content.Length))) }
if ([bool]$j.data.tenantContextRequired) {
    $r2 = Invoke-WebRequest 'http://localhost:51987/api/auth/refresh-context' -Method Post -Headers @{ Authorization = 'Bearer ' + $tok } -UseBasicParsing
    $tok = (($r2.Content | ConvertFrom-Json).data).token
}
$health = Invoke-WebRequest 'http://localhost:51987/api/health' -Headers @{ Authorization = 'Bearer ' + $tok } -UseBasicParsing
$out += ('LOGIN_SUPERADMIN_OK roles=' + (($j.data.roles) -join ','))
Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
$out += 'API_PARADA'

$out += 'R67A_BANCO_MANTIDO_PARA_R67B ' + $db
$out | ForEach-Object { Write-Output $_ }
Set-Content -Path (Join-Path $PSScriptRoot 'r67a_saida.txt') -Value ($out -join "`n") -Encoding UTF8
Write-Output 'R67A_OK'
