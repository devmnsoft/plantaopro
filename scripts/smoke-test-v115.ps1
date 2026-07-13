$BaseUrl = $env:BASE_URL
if (-not $BaseUrl) { $BaseUrl = "http://localhost:5000" }
$OutMd = "docs/homologacao/v115-smoke-result.md"
$OutJson = "docs/homologacao/v115-smoke-result.json"
New-Item -ItemType Directory -Force -Path "docs/homologacao" | Out-Null
$Endpoints = @("/api/health", "/api/v115/faturamento/regras", "/api/v115/faturamento/contas-receber", "/api/v115/repasses-medicos", "/api/v115/glosas", "/api/v115/financeiro/alertas", "/api/v114/mobile/medico/dashboard")
$Results = @()
"# Smoke v1.15`n" | Set-Content $OutMd
foreach ($Ep in $Endpoints) {
  try { $Resp = Invoke-WebRequest -Uri "$BaseUrl$Ep" -UseBasicParsing; $Code = [int]$Resp.StatusCode } catch { $Code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 } }
  $Status = if ($Code -eq 200) { "PASS" } elseif ($Code -eq 401 -or $Code -eq 403) { "PASS_AUTH" } else { "WARN" }
  "- $Status ``$Ep`` HTTP $Code" | Add-Content $OutMd
  $Results += @{ endpoint=$Ep; status=$Status; httpStatus=$Code }
}
@{ version="v1.15"; results=$Results } | ConvertTo-Json -Depth 5 | Set-Content $OutJson
