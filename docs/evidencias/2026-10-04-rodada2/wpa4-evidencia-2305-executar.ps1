# ============================================================================
# PlantaoPro | Runner da evidência WP-A4 — imutabilidade v2305 por papel
# (rodada 2, 2026-10-04)
#
# Uso (a partir da raiz do repositório ou de qualquer pasta):
#   $env:PGPASSWORD = '<senha-local-postgres>'
#   powershell -ExecutionPolicy Bypass -File docs\evidencias\2026-10-04-rodada2\wpa4-evidencia-2305-executar.ps1
#
# Executa os 4 passos em conexões REAIS como cada papel (session_user muda de
# fato — requisito para a função restrita fn_adm360_bypass_habilitado()):
#   1. postgres    → prep (papéis temporários + linha-base)
#   2. gate_usr    → cenário usuário comum (B/C/D)
#   3. gate_maint  → cenário maintenance (E/F/G/H)
#   4. postgres    → estado final + limpeza (I)
# Saída consolidada (sanitizável): wpa4-evidencia-2305-saida.log (UTF-8 sem BOM)
# ============================================================================
$ErrorActionPreference = 'Stop'
$psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$base = Split-Path -Parent $MyInvocation.MyCommand.Path
$db = 'plantaopro_test'
$log = Join-Path $base 'wpa4-evidencia-2305-saida.log'
$utf8SemBom = New-Object System.Text.UTF8Encoding($false)

if (-not $env:PGPASSWORD) { throw 'Defina $env:PGPASSWORD com a senha local do postgres antes de executar.' }
# Decodificação correta da saída nativa do psql (UTF-8): sem isto, o PowerShell 5.1
# usa a codepage da console e os acentos saem corrompidos no log.
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$passPostgres = $env:PGPASSWORD
$passGate = 'gate-evidencia' # mesma senha definida em wpa4-evidencia-2305-prep.sql (papel descartável)

function Adicionar-AoLog([string[]]$linhas) {
    [System.IO.File]::AppendAllLines($log, $linhas, $utf8SemBom)
}

Adicionar-AoLog @("Evidencia WPA4 v2305 por papel | $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz') | banco: $db")

function Executar-Passo([string]$user, [string]$arquivoSql) {
    $env:PGPASSWORD = $(if ($user -eq 'postgres') { $passPostgres } else { $passGate })
    $caminho = Join-Path $base $arquivoSql
    Adicionar-AoLog @('', "=== [$user] psql -v ON_ERROR_STOP=1 -h 127.0.0.1 -p 5432 -U $user -d $db -f $arquivoSql ===")
    # stderr do psql (NOTICE/DETAIL) vira ErrorRecord no PowerShell; com EAP='Stop' cada linha
    # terminaria o runner — por isso 'Continue' local e verificação explícita de $LASTEXITCODE.
    $eapAnterior = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $env:PGCLIENTENCODING = 'UTF8' # encoding do cliente (psql 18: -E e echo-queries; não usar -E UTF8)
    $saida = & $psql '-v' 'ON_ERROR_STOP=1' '-h' '127.0.0.1' '-p' '5432' '-U' $user '-d' $db '-f' $caminho 2>&1 |
        ForEach-Object { $_.ToString() }
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $eapAnterior
    Adicionar-AoLog ($saida | ForEach-Object { $_ })
    if ($exitCode -ne 0) {
        $env:PGPASSWORD = $passPostgres
        throw "Passo $arquivoSql falhou (exit code $exitCode). Log: $log"
    }
}

try {
    Executar-Passo 'postgres'   'wpa4-evidencia-2305-prep.sql'
    Executar-Passo 'gate_usr'   'wpa4-evidencia-2305-cenario-usuario-comum.sql'
    Executar-Passo 'gate_maint' 'wpa4-evidencia-2305-cenario-maintenance.sql'
    Executar-Passo 'postgres'   'wpa4-evidencia-2305-final.sql'
}
finally {
    $env:PGPASSWORD = $passPostgres
}

Adicionar-AoLog @('', 'EVIDENCIA WPA4 V2305 COMPLETA SEM ERROS')
Write-Host "OK: log consolidado em $log"
