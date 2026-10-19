$t = [IO.File]::ReadAllText('C:\MNSOFT\plantaopro\docs\evidencias\2026-10-19-rodada-66\r66c_gen_convergencia.ps1', (New-Object System.Text.UTF8Encoding($false)))
$i = $t.IndexOf('Sec[')
$seg = $t.Substring($i - 6, 60)
Write-Output ('codes=' + (($seg.ToCharArray() | ForEach-Object { [int][char]$_ }) -join ','))
$linhas = @(Get-Content 'C:\MNSOFT\plantaopro\database\scrpt_completo.sql' -Encoding UTF8)
$hit = 0
for ($j = 0; $j -lt $linhas.Count; $j++) { if ($linhas[$j] -match '^-- Sec[çc][aã]o (\d+) — R66 gap instalacao:') { $hit++; if ($hit -le 2) { Write-Output ('MATCH ' + ($j + 1) + ' num=' + $Matches[1]) } } }
Write-Output ('TOTAL_MATCH=' + $hit)
