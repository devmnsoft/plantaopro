# ============================================================================
# PlantaoPro | Runner do GATE da rodada 2 - ciclo de vida do banco canonico
# apos o WP-A3 (v2309 + v2310) (rodada 2, 2026-10-04)
#
# Banco descartavel explicitamente autorizado: plantaopro_gate_r2 (criado e
# derrubado por este script).
#
# Uso (a partir da raiz do repositório ou de qualquer pasta):
#   $env:PGPASSWORD = '<senha-local-postgres>'
#   powershell -ExecutionPolicy Bypass -File docs\evidencias\2026-10-04-rodada2\gate-r2-banco-executar.ps1
#
# Passos (ferramentas canônicas — PlantaoPro.Tools.Database):
#   1. create-database  → cria o banco descartável (UTF8, template0)
#   2. install          → install-manifest.json em transação única
#                         (+ install_manifest_runs + verificação)
#   3. upgrade          → migration-manifest.json (aplica pendentes, checksums
#                         SHA-256 normalizados)
#   4. upgrade de novo  → idempotência (nenhuma nova aplicação esperada)
#   5. status           → pendências/estado
#   6. verify           → IDENTITY_SCHEMA_READY esperado
#   7. limpeza          → DROP DATABASE do banco descartável
# Saída consolidada: gate-r2-banco-saida.log (UTF-8 sem BOM)
# ============================================================================
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$db = 'plantaopro_gate_r2'
$log = Join-Path $PSScriptRoot 'gate-r2-banco-saida.log'
$utf8SemBom = New-Object System.Text.UTF8Encoding($false)

if (-not $env:PGPASSWORD) { throw 'Defina $env:PGPASSWORD com a senha local do postgres antes de executar.' }
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# Ambiente das ferramentas: Development explícito (create-database) +
# autorização do auto-create de dev. Sem expor segredo fora de variáveis locais.
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:PLANTAOPRO_ALLOW_DEVELOPMENT_AUTO_CREATE = 'true'
$env:PLANTAOPRO_CONNECTION_STRING = "Host=127.0.0.1;Port=5432;Database=$db;Username=postgres;Password=$env:PGPASSWORD"

function Adicionar-AoLog([string[]]$linhas) {
    [System.IO.File]::AppendAllLines($log, $linhas, $utf8SemBom)
}

function Executar-Tool([string]$comando, [string]$descricao) {
    Adicionar-AoLog @('', "=== [$descricao] dotnet run --project backend/PlantaoPro.Tools.Database -- $comando ===")
    $eapAnterior = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    Push-Location $root
    try {
        $saida = & dotnet run --project 'backend/PlantaoPro.Tools.Database' -- $comando 2>&1 | ForEach-Object { $_.ToString() }
    } finally {
        Pop-Location
    }
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $eapAnterior
    Adicionar-AoLog $saida
    if ($exitCode -ne 0) { throw "Passo '$comando' falhou (exit code $exitCode). Log: $log" }
}

[System.IO.File]::Delete($log)
Adicionar-AoLog @("Ciclo de vida do banco (canônico) | $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz') | banco descartável: $db")

try {
    Executar-Tool 'create-database' 'passo 1: criação do banco descartável'
    Executar-Tool 'install'         'passo 2: instalação limpa (install-manifest, transação única)'
    Executar-Tool 'upgrade'         'passo 3: upgrade (migration-manifest, aplica pendentes)'
    Executar-Tool 'upgrade'         'passo 4: re-aplicação do upgrade (idempotência esperada)'
    Executar-Tool 'status'          'passo 5: status (pendências)'
    Executar-Tool 'verify'          'passo 6: verificação do schema identitário'
}
finally {
    # Limpeza garantida: derrubar o banco descartável (mesmo em caso de falha).
    Adicionar-AoLog @('', "=== [limpeza] DROP DATABASE $db (banco descartável) ===")
    $eapAnterior = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $saida = & $psql '-v' 'ON_ERROR_STOP=1' '-h' '127.0.0.1' '-p' '5432' '-U' 'postgres' '-d' 'postgres' '-c' "DROP DATABASE IF EXISTS $db WITH (FORCE)" 2>&1 | ForEach-Object { $_.ToString() }
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $eapAnterior
    Adicionar-AoLog $saida
    if ($exitCode -ne 0) { throw "Limpeza falhou (exit code $exitCode). Log: $log" }
}

Adicionar-AoLog @('', 'CICLO DE VIDA DO BANCO GATE R2 COMPLETO SEM ERROS')
Write-Host "OK: log consolidado em $log"
