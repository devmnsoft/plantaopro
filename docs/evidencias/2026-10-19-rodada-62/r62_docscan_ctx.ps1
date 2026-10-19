$targets = @(
  'backend/PlantaoPro.Api/Ai/AiModeloCompatibilidade.cs',
  'backend/PlantaoPro.Api/Ai/AiProviderAdapters.cs',
  'database/migrations/2026_10_v2356_r62_ai_provedor_drael.sql',
  'database/pgadmin/instalar_no_banco_atual.sql',
  'database/scrpt_completo.sql')
$root = (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))
foreach ($t in $targets) {
    $p = Join-Path $root $t
    $lines = [IO.File]::ReadAllLines($p, [Text.Encoding]::UTF8)
    $hits = @()
    for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i].Contains([string][char]195)) { $hits += (($i + 1).ToString() + ': ' + $lines[$i].Trim()) } }
    Write-Output ('=== ' + $t + ' (' + $hits.Count + ')')
    $hits | Select-Object -First 5 | ForEach-Object { Write-Output ($_ ) }
}
