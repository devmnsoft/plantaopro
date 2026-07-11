param([string]$BaseUrl = "http://localhost:5000")
$ErrorActionPreference = "Stop"
function Invoke-Step($Name, $Script) { Write-Host "==> $Name"; & $Script }
Invoke-Step "API health" { Invoke-RestMethod "$BaseUrl/api/dashboard" | Out-Null }
Invoke-Step "Homologation" { Invoke-RestMethod "$BaseUrl/api/homologation/status" | Out-Null }
Invoke-Step "Demo run all" { Invoke-RestMethod -Method Post "$BaseUrl/api/demo/run-all" | Out-Null }
Write-Host "Smoke v1.12 concluído. Para fluxo mutável completo, execute V112HomologationFlowTests no CI com SDK .NET."
