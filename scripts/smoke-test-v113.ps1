param([string]$BaseUrl = $env:BASE_URL)
if ([string]::IsNullOrWhiteSpace($BaseUrl)) { $BaseUrl = "http://localhost:5000" }
$headers = @{ Accept = "application/json" }
if (-not [string]::IsNullOrWhiteSpace($env:PLANTAOPRO_TOKEN)) { $headers.Authorization = "Bearer $($env:PLANTAOPRO_TOKEN)" }
$paths = @('/api/health','/api/v113/customers','/api/v113/products','/api/v113/inventory/balance','/api/v113/orders','/api/v113/tasks/my','/api/v113/billing/invoices','/api/v113/billing/titles','/api/v113/outbox','/api/v113/templates','/api/v113/journey/what-to-do-now','/api/v113/dashboard','/api/v113/homologation/status')
$checks = @(); $passed = 0; $failed = 0
foreach ($path in $paths) { try { $r = Invoke-WebRequest -Uri "$BaseUrl$path" -Headers $headers -Method GET -UseBasicParsing; $passed++; $checks += @{ name=$path; status='checked'; http=[int]$r.StatusCode } } catch { $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }; if ($code -eq 401) { $passed++ } else { $failed++ }; $checks += @{ name=$path; status= if ($code -eq 401) { 'checked' } else { 'failed' }; http=$code } } }
New-Item -ItemType Directory -Force docs/homologacao | Out-Null
@{ version='v1.13'; passed=$passed; failed=$failed; checks=$checks } | ConvertTo-Json -Depth 5 | Set-Content docs/homologacao/v113-smoke-result.json
"# Resultado smoke v1.13`n`n- Base URL: $BaseUrl`n- Passou: $passed`n- Falhou: $failed`n- Observação: token não é impresso pelo script." | Set-Content docs/homologacao/v113-smoke-result.md
if ($failed -gt 0) { exit 1 }
