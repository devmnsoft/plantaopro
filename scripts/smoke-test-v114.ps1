$BaseUrl = $env:BASE_URL
if ([string]::IsNullOrWhiteSpace($BaseUrl)) { $BaseUrl = "http://localhost:5000" }
$Token = $env:JWT_TOKEN
$Headers = @{ "Content-Type" = "application/json" }
if (-not [string]::IsNullOrWhiteSpace($Token)) { $Headers["Authorization"] = "Bearer $Token" }
$Endpoints = @("/api/health","/api/v114/dashboard","/api/v114/operacao/central","/api/v114/itens-faturaveis","/api/v114/faturamento/contas-receber","/api/v114/faturamento/titulos","/api/v114/faturamento/repasses-medicos","/api/v114/faturamento/glosas","/api/v114/jornada/progresso","/api/v114/templates-operacionais","/api/v114/mobile/medico/dashboard")
$Results = @()
foreach ($Endpoint in $Endpoints) {
    try { $Response = Invoke-WebRequest -Uri "$BaseUrl$Endpoint" -Headers $Headers -UseBasicParsing; $Code = [int]$Response.StatusCode; $Status = if ($Code -eq 200) { "PASS" } else { "WARN" } }
    catch { $Code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }; $Status = "WARN" }
    $Results += [pscustomobject]@{ endpoint = $Endpoint; status = $Status; httpStatus = $Code }
}
New-Item -ItemType Directory -Force -Path "docs/homologacao" | Out-Null
$Results | ConvertTo-Json | Set-Content "docs/homologacao/v114-smoke-result.json"
"# Smoke v1.14 PlantãoPro`n`nBoleto demonstrativo; endpoints protegidos podem retornar 401/403 sem JWT.`n" + (($Results | ForEach-Object { "- $($_.status) ``$($_.endpoint)`` HTTP $($_.httpStatus)" }) -join "`n") | Set-Content "docs/homologacao/v114-smoke-result.md"
