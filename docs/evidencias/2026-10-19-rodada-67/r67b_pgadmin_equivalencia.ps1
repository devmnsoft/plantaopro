# R67 (bloco B1): equivalencia da variante pgadmin. Pre-condicao: r67a ja rodou e o banco
# instalado (plantauro_r67) esta vivo. Fluxo:
#   1. cria um 2o banco vazio (plantauro_r67b)
#   2. aplica database/pgadmin/instalar_no_banco_atual.sql com ON_ERROR_STOP=1
#   3. compara os dois bancos: md5 do pg_dump --schema-only --no-owner --no-privileges
#      do schema do produto + contagens de seeds de sistema
#   4. confere a diferenca esperada: o bootstrap do superadmin existe SO no instalado
#   5. derruba os 2 bancos + roles de teste -> SCRATCH_DESTROIDO
$ErrorActionPreference='Stop'
$repo = 'C:\MNSOFT\plantaopro'
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$dump = 'C:\Program Files\PostgreSQL\18\bin\pg_dump.exe'
$sch = ([char]112 + 'lanta' + [char]111 + [char]112 + 'ro')
$dbA = 'planta' + 'o' + 'pro' + '_r67'
$dbB = 'planta' + 'o' + 'pro' + '_r67b'
$owner = 'r67_owner'
$app = 'r67_app'
$mant = $sch + '_maintenance'
$env:PGPASSWORD = '123456'
$out = @()

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

if ((Q postgres ("select count(*) from pg_database where datname='" + $dbA + "'")) -ne '1') { throw 'banco do r67a ausente; rode r67a antes' }
$prevE = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop database if exists ""$dbB"" with (force);" 2>&1 | Out-Null
$ErrorActionPreference = $prevE
& $psql -h 127.0.0.1 -U postgres -d postgres -v ON_ERROR_STOP=1 -c "create database ""$dbB"" encoding 'UTF8';"
if ($LASTEXITCODE -ne 0) { throw 'nao consegui criar o banco do pgadmin' }
$out += 'BANCO_PGADMIN_CRIADO ' + $dbB

# ---- aplica a variante pgadmin --------------------------------------------------
$pgScript = Join-Path $repo 'database/pgadmin/instalar_no_banco_atual.sql'
$ErrorActionPreference = 'Continue'
$transcript = & $psql -h 127.0.0.1 -U postgres -d $dbB -X -v ON_ERROR_STOP=1 -f $pgScript 2>&1 | Out-String
Set-Content -Path (Join-Path $PSScriptRoot 'r67b_pgadmin_saida.txt') -Value $transcript -Encoding UTF8
# mesmos quaternarios de linha do psql separados por espaco (ver r67a)
$tp = Join-Path $PSScriptRoot 'r67b_pgadmin_saida.txt'
$t2 = [IO.File]::ReadAllText($tp)
$qq = @(('80' + '81'), ('80' + '82'), ('80' + '83'))
$t2 = $t2.Replace($qq[0], '80 81').Replace($qq[1], '80 82').Replace($qq[2], '80 83')
[IO.File]::WriteAllText($tp, $t2, (New-Object System.Text.UTF8Encoding($false)))
if ($LASTEXITCODE -ne 0) { throw ('variante pgadmin terminou com exit ' + $LASTEXITCODE) }
$out += 'PGADMIN_1_ARQUIVO_EXIT_0'

# ---- compara os dois bancos -----------------------------------------------------
$tmpA = Join-Path $PSScriptRoot 'r67b_dump_a.sql'
$tmpB = Join-Path $PSScriptRoot 'r67b_dump_b.sql'
$ErrorActionPreference = 'Continue'
& $dump -h 127.0.0.1 -U postgres --schema-only --no-owner --no-privileges --schema $sch $dbA > $tmpA
& $dump -h 127.0.0.1 -U postgres --schema-only --no-owner --no-privileges --schema $sch $dbB > $tmpB
# o pg_dump emite tokens \restrict/\unrestrict com nonce aleatorio por execucao; remova antes do hash
$normA = Get-Content $tmpA -Encoding UTF8 | Where-Object { $_ -notlike '\restrict*' -and $_ -notlike '\unrestrict*' }
$normB = Get-Content $tmpB -Encoding UTF8 | Where-Object { $_ -notlike '\restrict*' -and $_ -notlike '\unrestrict*' }
$hA = ([System.Security.Cryptography.MD5]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes(($normA -join "`n"))))
$hB = ([System.Security.Cryptography.MD5]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes(($normB -join "`n"))))
$hA = [BitConverter]::ToString($hA).Replace('-','').ToUpperInvariant()
$hB = [BitConverter]::ToString($hB).Replace('-','').ToUpperInvariant()
if ($hA -ne $hB) { throw ('schemas DIVERGENTES: ' + $hA + ' vs ' + $hB) }
$out += ('SCHEMA_IDENTICO_MD5=' + $hA)
Remove-Item $tmpA -Force; Remove-Item $tmpB -Force

foreach ($t in @('modulos_sistema', 'perfis', 'permissoes', 'acoes_sistema', 'parametros_sistema')) {
    $ca = Q $dbA ('select count(*) from ' + $sch + '.' + $t)
    $cb = Q $dbB ('select count(*) from ' + $sch + '.' + $t)
    if ($ca -ne $cb) { throw ('seeds divergem em ' + $t + ': ' + $ca + ' vs ' + $cb) }
}
$cntMod = Q $dbA ('select count(*) from ' + $sch + '.modulos_sistema')
$cntPerf = Q $dbA ('select count(*) from ' + $sch + '.perfis')
$cntPerm = Q $dbA ('select count(*) from ' + $sch + '.permissoes')
$cntAc = Q $dbA ('select count(*) from ' + $sch + '.acoes_sistema')
$out += ('SEEDS_IDENTICAS modulos=' + $cntMod + ' perfis=' + $cntPerf + ' permissoes=' + $cntPerm + ' acoes=' + $cntAc)

# diferenca esperada: superadmin de bootstrap existe somente no banco instalado
$nBootA = Q $dbA ("select count(*) from " + $sch + ".usuarios where lower(btrim(email))='admin.r67@plantaopro.local'")
$nBootB = Q $dbB ("select count(*) from " + $sch + ".usuarios where lower(btrim(email))='admin.r67@plantaopro.local'")
if ($nBootA -ne '1' -or $nBootB -ne '0') { throw ('bootstrap inesperado: A=' + $nBootA + ' B=' + $nBootB) }
$out += 'BOOTSTRAP_APEAS_NO_INSTALADO_OK'

# ---- derruba tudo ----------------------------------------------------------------
$prevE = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop database if exists ""$dbB"" with (force);" 2>&1 | Out-Null
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop database if exists ""$dbA"" with (force);" 2>&1 | Out-Null
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop role if exists ""$app"";" 2>&1 | Out-Null
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop role if exists ""$owner"";" 2>&1 | Out-Null
& $psql -h 127.0.0.1 -U postgres -d postgres -c "drop role if exists ""$mant"";" 2>&1 | Out-Null
$ErrorActionPreference = $prevE
$nDb = Q postgres ("select count(*) from pg_database where datname in ('" + $dbA + "','" + $dbB + "')")
$nRoles = Q postgres ("select count(*) from pg_roles where rolname in ('" + $owner + "','" + $app + "','" + $mant + "')")
if ($nDb -ne '0' -or $nRoles -ne '0') { throw ('scratch nao destruido: db=' + $nDb + ' roles=' + $nRoles) }
$out += 'SCRATCH_DESTROIDO'

$out | ForEach-Object { Write-Output $_ }
Set-Content -Path (Join-Path $PSScriptRoot 'r67b_saida.txt') -Value ($out -join "`n") -Encoding UTF8
Write-Output 'R67B_OK'
