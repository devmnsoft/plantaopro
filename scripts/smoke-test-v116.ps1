$BaseUrl = $env:BASE_URL
if ([string]::IsNullOrWhiteSpace($BaseUrl)) { $BaseUrl = "http://localhost:5000" }
$paths = @('/api/v116/convenios/autorizacoes','/api/v116/convenios/guias','/api/v116/faturamento/lotes','/api/v116/caixa/status','/api/v116/notificacoes-operacionais','/api/v116/relatorios/faturamento')
foreach ($path in $paths) { Write-Host "$path -> smoke v1.16" }
