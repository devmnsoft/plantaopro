<#
.SYNOPSIS
  Bloco E (Homologacao Administrativo 360) - Matriz API reprodutivel nos cenarios M1..M7b.

.DESCRIPTION
  Script autocontido (sem dependencias externas) que valida contra a API no ar:
    M1  login GESTOR            -> 200 + permissoes/modules ADM360
    M2  READ gestor (dashboard) -> 200
    M2b READ gestor (cotacoes)  -> 200
    M3  WRITE gestor (mapeamento) -> 200
    M4  login CONSULTA/AUDITOR  -> 200 + permissoes auditadas
    M5  READ consulta (dashboard) -> 200
    M5b READ consulta (cotacoes)  -> 200
    M6  WRITE consulta (mapeamento) -> 403 (negacao por falta de permissao)
    M7  REVOKE live da permissao ADM360:VER do perfil Auditor (direto no banco)
        -> dashboard do MESMO token vira 403 SEM re-login (revogacao efetiva)
    M7b RESTORE da permissao     -> dashboard volta a 200 com o MESMO token

  A linha de perfil_permissoes do M7 e identificada DINAMICAMENTE (perfil do usuario
  consulta@santacasa-demo.example x permissao ADM360:VER), portanto o script funciona
  em qualquer banco instalado pelo caminho oficial com o seed de demo (padrao:
  plantaopro_test, o banco unico de testes). O restore e executado mesmo em falha
  (try/finally), deixando o banco como estava.

  Contrato de saida: exit code = quantidade de checks FALHOU (0 = matriz 10/10 PASS).
  Os corpos de resposta ficam em $env:TEMP (arquivos pp_api_*.txt / pp_api_out_*.body).

.PARAMETER ApiBase
  Base da API local (padrao https://localhost:51977).
.PARAMETER DatabaseName
  Banco alvo para revoke/restore (padrao plantaopro_test - banco unico de testes).
.PARAMETER PgHost / PgPort / PgUser / PgPassword
  Conexao psql (senha lida de $env:PGPASSWORD quando nao passada).
.PARAMETER LogPath
  Quando informado, grava transcript completo no arquivo indicado.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts\homologacao\api_matrix.ps1 -LogPath .\api-matrix.log
#>
param(
  [string]$ApiBase = 'https://localhost:51977',
  [string]$DatabaseName = 'plantaopro_test',
  [string]$PgHost = '127.0.0.1',
  [int]$PgPort = 5432,
  [string]$PgUser = 'postgres',
  [string]$PgPassword = $(if ($env:PGPASSWORD) { $env:PGPASSWORD } else { '123456' }),
  [string]$LogPath = ''
)

$ErrorActionPreference = 'Continue'
$fails = 0
$tmp = $env:TEMP
$revoked = $false
$PsqlBin = $null

if (-not $env:PGPASSWORD) { Write-Host 'AVISO: $env:PGPASSWORD vazio - usando senha local padrao "123456" (padrao dos defaults versionados).' }

if ($LogPath) { Start-Transcript -Path $LogPath -Append | Out-Null }

function Find-Psql {
  $cmd = Get-Command psql -ErrorAction SilentlyContinue
  if ($cmd) { return $cmd.Source }
  $found = Get-ChildItem 'C:\Program Files\PostgreSQL' -Directory -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending |
    ForEach-Object { Join-Path $_.FullName 'bin\psql.exe' } |
    Where-Object { Test-Path $_ } |
    Select-Object -First 1
  if ($found) { return $found }
  throw "psql nao encontrado (PATH nem C:\Program Files\PostgreSQL\*)."
}

function Invoke-Pg {
  param([string]$Sql, [switch]$Tabular)
  $env:PGPASSWORD = $PgPassword
  $a = @('-h', $PgHost, '-p', ([string]$PgPort), '-U', $PgUser, '-d', $DatabaseName, '-X', '-w', '-v', 'ON_ERROR_STOP=1')
  if ($Tabular) { $a += @('-At', '-F', ' | ') } else { $a += @('-At') }
  $a += @('-c', $Sql)
  $out = (& $PsqlBin @a 2>&1 | Out-String).Trim()
  if ($LASTEXITCODE -ne 0) { throw "psql falhou (exit $LASTEXITCODE) para: $Sql`n$out" }
  return $out
}

function Write-Save([string]$Label, [string]$Body) {
  $p = Join-Path $tmp ("pp_api_{0}.txt" -f $Label)
  [System.IO.File]::WriteAllText($p, $Body, (New-Object System.Text.UTF8Encoding($false)))
  Write-Host ("  resp-file -> {0}" -f $p)
}

$callNo = 0
function Invoke-Curl {
  param([string]$Method, [string]$Url, [string]$Token, [string]$BodyFile)
  $script:callNo++
  $bodyOut = Join-Path $tmp ("pp_api_out_{0:D2}.body" -f $script:callNo)
  $a = @('-s', '-k', '-X', $Method, '-o', $bodyOut, '-w', '%{http_code}')
  if ($Token) { $a += @('-H', ("Authorization: Bearer {0}" -f $Token)) }
  else { $a += @('-H', 'Accept: application/json') }
  if ($BodyFile) { $a += @('-H', 'Content-Type: application/json', '--data-binary', "@$BodyFile") }
  $a += $Url
  $codeStr = (& curl.exe @a 2>$null | Out-String).Trim()
  $code = 0
  [int]::TryParse($codeStr, [ref]$code) | Out-Null
  $body = ''
  if (Test-Path $bodyOut) { $body = [System.IO.File]::ReadAllText($bodyOut) }
  return [pscustomobject]@{ Code = $code; Body = $body }
}

function Check([string]$Step, $R, [int]$Expected) {
  $ok = ($R.Code -eq $Expected)
  if (-not $ok) { $script:fails++ }
  Write-Host ("{0}: HTTP {1} (esperado {2}) => {3}" -f $Step, $R.Code, $Expected, $(if ($ok) { 'PASS' } else { 'FAIL' }))
}

function New-JsonFile([string]$Name, [string]$Json) {
  $p = Join-Path $tmp ("pp_api_{0}.json" -f $Name)
  [System.IO.File]::WriteAllText($p, $Json, (New-Object System.Text.UTF8Encoding($false)))
  return $p
}

# ---------- metadados de reproducao ----------
$PsqlBin = Find-Psql
Write-Host '=== MATRIZ API ADMINISTRATIVO 360 (Bloco E) ==='
Write-Host ("data={0}  api={1}  db={2}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz'), $ApiBase, $DatabaseName)
Write-Host ("psql={0}" -f $PsqlBin)

# ---------- M1: login gestor ----------
Write-Host '=== M1: login GESTOR (API) ==='
$g = New-JsonFile 'login_gestor_req' '{"email":"gestor@santacasa-demo.example","senha":"SantaCasa!Demo2026#Gestor"}'
$r = Invoke-Curl POST "$ApiBase/api/auth/login" -BodyFile $g
Write-Save 'login_gestor_resp' $r.Body
Check 'M1 login gestor' $r 200
$tokG = $null
try { $j = $r.Body | ConvertFrom-Json; $tokG = $j.data.token } catch { Write-Host "  parse falhou: $($_.Exception.Message)" }
if ($tokG) {
  Write-Host ("  tenantId={0} sessionId={1}" -f $j.data.tenantId, $j.data.sessionId)
  $perms = @($j.data.permissions)
  Write-Host ("  permissions n={0}" -f $perms.Count)
  $adm = $perms | Where-Object { $_ -like 'ADM360*' }
  Write-Host ("  ADM360 perms n={0} (amostra): {1}" -f @($adm).Count, ((@($adm) | Select-Object -First 6) -join ', '))
  Write-Host ("  tem ADM360.MAPEAR_CADASTROS: {0}; tem ADM360.VER: {1}" -f (@($perms) -contains 'ADM360.MAPEAR_CADASTROS'), (@($perms) -contains 'ADM360.VER'))
  $mods = @($j.data.modules)
  $hasMod = @($mods | Where-Object { $_ -eq 'ADM360' -or "$_" -match 'a3600000' }).Count -gt 0
  Write-Host ("  modules n={0}; tem ADM360: {1}" -f $mods.Count, $hasMod)
}

# ---------- M2: read gestor ----------
Write-Host '=== M2: READ gestor (gestao/dashboard) ==='
$r2 = Invoke-Curl GET "$ApiBase/api/administrativo360/gestao/dashboard" -Token $tokG
Write-Save 'read_dashboard_gestor' $r2.Body
Check 'M2 read dashboard' $r2 200

Write-Host '=== M2b: READ gestor (cotacoes list) ==='
$r2b = Invoke-Curl GET "$ApiBase/api/administrativo360/cotacoes" -Token $tokG
Write-Save 'read_cotacoes_gestor' $r2b.Body
Check 'M2b read cotacoes' $r2b 200

# ---------- M3: write gestor ----------
Write-Host '=== M3: WRITE gestor (POST mapeamentos MAT9001-HOMOLOG) ==='
$m = New-JsonFile 'mapeamento_gestor_req' '{"portalContaId":null,"provedor":"PORTAL_DEMO","tipoEntidade":"PRODUTO","codigoExterno":"MAT9001-HOMOLOG","descricaoExterna":"Mapeamento homologacao Drael","entidadeInternaId":null,"entidadeInternaDescricao":"Produto interno homologacao","fatorConversao":1.0}'
$r3 = Invoke-Curl POST "$ApiBase/api/administrativo360/cotacoes/mapeamentos" -Token $tokG -BodyFile $m
Write-Save 'write_mapeamento_gestor' $r3.Body
Check 'M3 write mapeamentos' $r3 200

# ---------- M4: login consulta ----------
Write-Host '=== M4: login CONSULTA/AUDITOR (API) ==='
$c = New-JsonFile 'login_consulta_req' '{"email":"consulta@santacasa-demo.example","senha":"SantaCasa!Demo2026#Gestor"}'
$r4 = Invoke-Curl POST "$ApiBase/api/auth/login" -BodyFile $c
Write-Save 'login_consulta_resp' $r4.Body
Check 'M4 login consulta' $r4 200
$tokC = $null
try { $jc = $r4.Body | ConvertFrom-Json; $tokC = $jc.data.token } catch { Write-Host "  parse falhou: $($_.Exception.Message)" }
if ($tokC) {
  $pc = @($jc.data.permissions)
  Write-Host ("  permissions n={0}" -f $pc.Count)
  Write-Host ("  tem ADM360.VER: {0}; tem ADM360.COTACAO_CONSULTAR: {1}; tem ADM360.MAPEAR_CADASTROS: {2}" -f (@($pc) -contains 'ADM360.VER'), (@($pc) -contains 'ADM360.COTACAO_CONSULTAR'), (@($pc) -contains 'ADM360.MAPEAR_CADASTROS'))
}

# ---------- M5: read consulta ----------
Write-Host '=== M5: READ consulta (dashboard + cotacoes) ==='
$r5 = Invoke-Curl GET "$ApiBase/api/administrativo360/gestao/dashboard" -Token $tokC
Write-Save 'read_dashboard_consulta' $r5.Body
Check 'M5 read dashboard (antes do revoke)' $r5 200

$r5b = Invoke-Curl GET "$ApiBase/api/administrativo360/cotacoes" -Token $tokC
Write-Save 'read_cotacoes_consulta' $r5b.Body
Check 'M5b read cotacoes' $r5b 200

# ---------- M6: write consulta (deve negar) ----------
Write-Host '=== M6: WRITE consulta (POST mapeamentos MAT9002-AUDITOR) -> deve negar ==='
$m2 = New-JsonFile 'mapeamento_consulta_req' '{"portalContaId":null,"provedor":"PORTAL_DEMO","tipoEntidade":"PRODUTO","codigoExterno":"MAT9002-AUDITOR","descricaoExterna":"Tentativa escrita auditoria","entidadeInternaId":null,"entidadeInternaDescricao":"Produto interno auditoria","fatorConversao":1.0}'
$r6 = Invoke-Curl POST "$ApiBase/api/administrativo360/cotacoes/mapeamentos" -Token $tokC -BodyFile $m2
Write-Save 'write_mapeamento_consulta' $r6.Body
Check 'M6 write consulta (negacao)' $r6 403

# ---------- M7/M7b: revogacao live + restore (identificacao dinamica, restore garantido) ----------
Write-Host '=== M7: REVOKE Auditor ADM360:VER (DB) -> dashboard deve negar SEM re-login ==='
$targetQuery = "SELECT pp.id FROM plantaopro.perfil_permissoes pp JOIN plantaopro.permissoes p ON p.id=pp.permissao_id WHERE p.codigo='ADM360:VER' AND pp.perfil_id IN (SELECT up.perfil_id FROM plantaopro.usuarios_perfis up JOIN plantaopro.usuarios u ON u.id=up.usuario_id WHERE u.email='consulta@santacasa-demo.example')"
$cleanIds = @()
try {
  $rawTarget = Invoke-Pg -Sql $targetQuery -Tabular
  $cleanIds = @(($rawTarget -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -match '^[0-9a-f-]{36}$' }))
} catch {
  Write-Host ("  PRE-CHECK PSQL FALHOU: {0}" -f $_.Exception.Message)
}
Write-Host ("  linha(s) alvo encontrada(s): {0}" -f $cleanIds.Count)
if ($cleanIds.Count -eq 1) {
  try {
    Invoke-Pg -Sql ("UPDATE plantaopro.perfil_permissoes SET reg_status='I' WHERE id='{0}'" -f $cleanIds[0]) | Out-Null
    Write-Host ("  REVOKE ok: {0} -> reg_status I" -f $cleanIds[0])
    $revoked = $true
  } catch {
    Write-Host ("  REVOKE FALHOU: {0}" -f $_.Exception.Message)
  }
} else {
  Write-Host "  PRE-CHECK FALHOU: esperada exatamente 1 linha, encontradas $($cleanIds.Count)."
}

$r7 = Invoke-Curl GET "$ApiBase/api/administrativo360/gestao/dashboard" -Token $tokC
Write-Save 'read_dashboard_consulta_revoke' $r7.Body
Check 'M7 dashboard apos revoke (sem relogin)' $r7 403

Write-Host '=== M7b: RESTORE grant -> dashboard volta a 200 (mesmo token) ==='
if ($revoked) {
  try {
    Invoke-Pg -Sql ("UPDATE plantaopro.perfil_permissoes SET reg_status='A' WHERE id='{0}'" -f $cleanIds[0]) | Out-Null
    Write-Host ("  RESTORE ok: {0} -> reg_status A" -f $cleanIds[0])
  } catch {
    Write-Host ("  RESTORE FALHOU: {0}" -f $_.Exception.Message)
    $fails++
  }
} else {
  Write-Host "  RESTORE pulado (nenhuma linha foi revogada)."
}
$r7b = Invoke-Curl GET "$ApiBase/api/administrativo360/gestao/dashboard" -Token $tokC
Write-Save 'read_dashboard_consulta_restore' $r7b.Body
Check 'M7b dashboard apos restore' $r7b 200

# ---------- resumo ----------
Write-Host ''
Write-Host ("MATRIZ FINAL: fails={0}" -f $fails)
if ($LogPath) { Stop-Transcript | Out-Null }
exit $fails
