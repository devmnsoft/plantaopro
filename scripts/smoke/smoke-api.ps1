param(
  [string]$BaseUrl = $env:PLANTAOPRO_API_BASE_URL,
  [string]$AdminEmail = $env:PLANTAOPRO_ADMIN_EMAIL,
  [string]$AdminPassword = $env:PLANTAOPRO_ADMIN_PASSWORD
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($BaseUrl)) { $BaseUrl = 'http://localhost:5000' }
if ([string]::IsNullOrWhiteSpace($AdminEmail)) { $AdminEmail = 'admin@plantaopro.local' }
if ([string]::IsNullOrWhiteSpace($AdminPassword)) { $AdminPassword = 'admin123' }
Write-Host "Smoke API PlantãoPro em $BaseUrl"
function Test-Endpoint($Method, $Path, $Expected) {
  $response = Invoke-WebRequest -Method $Method -Uri "$BaseUrl$Path" -UseBasicParsing -SkipHttpErrorCheck
  if ([int]$response.StatusCode -ne $Expected) { throw "Falha em $Method $Path: HTTP $($response.StatusCode), esperado $Expected" }
  Write-Host "OK $Method $Path -> $($response.StatusCode)"
}
Test-Endpoint GET '/' 200
Test-Endpoint GET '/api/health' 200
Test-Endpoint GET '/api/health/db' 200
Test-Endpoint GET '/swagger' 200
$login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" -ContentType 'application/json' -Body (@{ email=$AdminEmail; password=$AdminPassword } | ConvertTo-Json)
$token = $login.token
if (-not $token -and $login.data) { $token = $login.data.token }
if (-not $token) { throw 'Login respondeu sem token em campo conhecido.' }
$response = Invoke-WebRequest -Method GET -Uri "$BaseUrl/api/usuarios/me" -Headers @{ Authorization = "Bearer $token" } -UseBasicParsing -SkipHttpErrorCheck
if ([int]$response.StatusCode -notin @(200,204)) { throw "Falha em /api/usuarios/me autenticado: HTTP $($response.StatusCode)" }
Write-Host "Smoke API concluído sem expor token."
