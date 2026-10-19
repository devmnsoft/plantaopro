# R66 (bloco B1): valida a chave Gemini ponta a ponta. Convencao do repo: segredo por
# variavel de ambiente (Ai__Providers__Gemini__ApiKey), nunca em appsettings versionada.
# Fluxo: sobe a API (Release, banco de teste) -> login demo -> GET api/ai/config ->
# POST api/ai/test-conexao gemini (chamada REAL ao provedor) -> para a API.
$ErrorActionPreference='Continue'
$repo = 'C:\MNSOFT\plantaopro'
$b = 'planta' + 'o' + 'pro'
$key = ''
$envFile = Join-Path $repo '.env'
if (Test-Path $envFile) {
    foreach ($l in Get-Content $envFile) {
        if ($l -like 'Ai__Providers__Gemini__ApiKey=*') { $key = ($l -replace '^Ai__Providers__Gemini__ApiKey=', '') }
    }
}
if ([string]::IsNullOrWhiteSpace($key)) { throw 'chave Gemini ausente em ' + $envFile + ' (Ai__Providers__Gemini__ApiKey)' }

$conns = Get-NetTCPConnection -State Listen -LocalPort 51976 -ErrorAction SilentlyContinue
foreach ($c in $conns) { Stop-Process -Id $c.OwningProcess -Force -ErrorAction SilentlyContinue }
Start-Sleep 2

$env:ConnectionStrings__Default = "Host=127.0.0.1;Port=5432;Database=" + $b + "_test;Username=postgres;Password=123456"
$env:Jwt__Key = 'r66-gemini-dev-jwt-key-0123456789-abcdefghijklmn'
$env:Ai__Providers__Gemini__ApiKey = $key
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
$out = @('API_OK ' + (Get-Date -Format 'HH:mm:ss'))

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

$rt = Invoke-WebRequest 'http://localhost:51976/api/ai/test-conexao' -Method Post -Body '{"provedor":"gemini"}' -ContentType 'application/json' -Headers @{ Authorization = 'Bearer ' + $tok } -UseBasicParsing -TimeoutSec 60
$out += ('TEST_CONEXAO_STATUS=' + [int]$rt.StatusCode)
$out += ('TEST_BODY ' + $rt.Content.Substring(0, [Math]::Min(400, $rt.Content.Length)))

Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
$out += 'API_PARADA'
$out | ForEach-Object { Write-Output $_ }
Set-Content -Path (Join-Path $PSScriptRoot 'r66i_gemini_e2e_saida.txt') -Value ($out -join "`n") -Encoding UTF8
