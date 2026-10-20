# R71E (pedido do usuario, fora do plano dos 50 sprints): valida a chave GLOBAL da Drael
# (IA da casa) ponta a ponta pelo sistema. Convencao do repo: segredo no .env (git-ignorado),
# lido aqui e injetado como variavel de ambiente — nunca em appsettings versionada.
# Fluxo: sobe a API (Release, banco de teste) -> login demo -> GET api/ai/config ->
# POST api/ai/test-conexao drael (chamada REAL a https://drael.sh/v1/chat/completions,
# modelo drael-v1) -> para a API. Espelho do r66i (Gemini) com a semantica de quota
# de testes (max 10 por tenant/provedor em 24h, auditados em ai_usos).
$ErrorActionPreference='Continue'
$repo = 'C:\MNSOFT\plantaopro'
$b = 'planta' + 'o' + 'pro'
$key = ''
$envFile = Join-Path $repo '.env'
if (Test-Path $envFile) {
    foreach ($l in Get-Content $envFile) {
        if ($l -like 'Ai__Providers__Drael__ApiKey=*') { $key = ($l -replace '^Ai__Providers__Drael__ApiKey=', '') }
    }
}
if ([string]::IsNullOrWhiteSpace($key)) { throw 'chave Drael ausente em ' + $envFile + ' (Ai__Providers__Drael__ApiKey)' }
$mascara = $key.Substring(0, 5) + '...' + $key.Substring($key.Length - 4)

$conns = Get-NetTCPConnection -State Listen -LocalPort 51976 -ErrorAction SilentlyContinue
foreach ($c in $conns) { Stop-Process -Id $c.OwningProcess -Force -ErrorAction SilentlyContinue }
Start-Sleep 2

$env:ConnectionStrings__Default = "Host=127.0.0.1;Port=5432;Database=" + $b + "_test;Username=postgres;Password=123456"
$env:Jwt__Key = 'r71e-drael-dev-jwt-key-0123456789-abcdefghijklmn'
$env:Ai__Providers__Drael__ApiKey = $key
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$apiDll = Join-Path $repo 'backend/PlantaoPro.Api/bin/Release/net10.0/PlantaoPro.Api.dll'
if (-not (Test-Path $apiDll)) { throw 'build Release ausente: ' + $apiDll }
$proc = Start-Process -FilePath 'dotnet' -ArgumentList ('"' + $apiDll + '" --urls http://localhost:51976') -WindowStyle Hidden -PassThru

$ok = $false
for ($i = 0; $i -lt 60; $i++) {
    Start-Sleep 1
    try { $h = Invoke-WebRequest 'http://localhost:51976/api/health' -UseBasicParsing -TimeoutSec 2; if ([int]$h.StatusCode -eq 200) { $ok = $true; break } } catch { }
}
if (-not $ok) { throw 'API nao respondeu no health' }
$out = @('API_OK ' + (Get-Date -Format 'HH:mm:ss') + ' chave=' + $mascara)

$body = @{ Email = 'gestor@santacasa-demo.example'; Senha = 'SantaCasa!Demo2026#Gestor' } | ConvertTo-Json
$r = Invoke-WebRequest 'http://localhost:51976/api/auth/login' -Method Post -Body $body -ContentType 'application/json' -UseBasicParsing
$j = $r.Content | ConvertFrom-Json
$tok = $j.data.token
$out += ('LOGIN_OK tenantRequired=' + [bool]$j.data.tenantContextRequired + ' roles=' + (($j.data.roles) -join ','))
if ([bool]$j.data.tenantContextRequired) {
    $r2 = Invoke-WebRequest 'http://localhost:51976/api/auth/refresh-context' -Method Post -Headers @{ Authorization = 'Bearer ' + $tok } -UseBasicParsing
    $tok = (($r2.Content | ConvertFrom-Json).data).token
    $out += 'REFRESH_CTX_OK'
}

$rc = Invoke-WebRequest 'http://localhost:51976/api/ai/config' -Headers @{ Authorization = 'Bearer ' + $tok } -UseBasicParsing
$cfg = $rc.Content | ConvertFrom-Json
foreach ($c in $cfg.configuracoes) {
    $out += ('CFG {0} prov={1} hab={2} chaveTenant={3} chaveGlobal={4}' -f $c.taskCode, $c.provedor, $c.habilitada, $c.chaveDoTenantConfigurada, $c.chaveGlobalDisponivel)
}
$out += ('CHAVE_MESTRA_SERVIDOR=' + [bool]$cfg.chaveMestraDoServidorConfigurada)

$rt = Invoke-WebRequest 'http://localhost:51976/api/ai/test-conexao' -Method Post -Body '{"provedor":"drael"}' -ContentType 'application/json' -Headers @{ Authorization = 'Bearer ' + $tok } -UseBasicParsing -TimeoutSec 90
$out += ('TEST_CONEXAO_STATUS=' + [int]$rt.StatusCode)
$tj = $rt.Content | ConvertFrom-Json
$out += ('TEST_RESULTADO success=' + $tj.success + ' kind=' + $tj.statusKind + ' provedor=' + $tj.provedor + ' modelo=' + $tj.modelo + ' duracaoMs=' + $tj.duracaoMs)
$out += ('TEST_MENSAGEM ' + $tj.mensagem)

Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
$out += 'API_PARADA'
$out | ForEach-Object { Write-Output $_ }
Set-Content -Path (Join-Path $PSScriptRoot 'r71e_drael_e2e_saida.txt') -Value ($out -join "`n") -Encoding UTF8
if (-not $tj.success) { Write-Output 'E2E_RESULTADO=FALHA'; exit 1 }
Write-Output 'E2E_RESULTADO=SUCESSO'
