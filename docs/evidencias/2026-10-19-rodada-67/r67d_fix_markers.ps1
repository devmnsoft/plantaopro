# R67: atualiza o marcador -- SOURCE-SHA256 da secao v2305 nos dois scripts completos e
# nos dois backups do pipeline R66 (a migration mudou no r67c; o hash do JSON ja foi
# atualizado, falta o marcador embutido no corpo das secoes canonicas).
$ErrorActionPreference = 'Stop'
$root = 'C:\MNSOFT\plantaopro'
$enc = New-Object System.Text.UTF8Encoding($false)
$mk = '2026_10_v2305_administrativo360_imutabilidade_por_papel.sql'

$json = [IO.File]::ReadAllText((Join-Path $root 'database/source-checksums.json'), $enc)
$rx = [regex]('"' + [regex]::Escape('database/migrations/' + $mk) + '":\s*"[0-9a-f]{64}"')
if (-not $rx.IsMatch($json)) { throw 'hash novo ausente no source-checksums.json' }
$hashNovo = $rx.Match($json).Groups[0].Value -replace '^.*:\s*"', '' -replace '"$', ''
Write-Output ('HASH_NOVO=' + $hashNovo)

$alvos = @(
    (Join-Path $root 'database/scrpt_completo.sql'),
    (Join-Path $root 'database/pgadmin/instalar_no_banco_atual.sql'),
    (Join-Path $root 'docs/evidencias/2026-10-19-rodada-66/_backup_scrpt_completo.sql'),
    (Join-Path $root 'docs/evidencias/2026-10-19-rodada-66/_backup_instalar_no_banco_atual.sql')
)

foreach ($p in $alvos) {
    $t = [IO.File]::ReadAllText($p, $enc)
    $iSrc = $t.IndexOf('-- SOURCE: database/migrations/' + $mk)
    if ($iSrc -lt 0) { throw ('marcador SOURCE ausente em ' + $p) }
    $iSha = $t.IndexOf('-- SOURCE-SHA256: ', $iSrc)
    if ($iSha -lt 0) { throw ('marcador SOURCE-SHA256 ausente em ' + $p) }
    $iniHash = $iSha + '-- SOURCE-SHA256: '.Length
    $hashAtual = $t.Substring($iniHash, 64)
    if ($hashAtual -eq $hashNovo) { Write-Output ('JAOCTUALIZADO=' + (Split-Path $p -Leaf)); continue }
    $t = $t.Substring(0, $iniHash) + $hashNovo + $t.Substring($iniHash + 64)
    [IO.File]::WriteAllText($p, $t, $enc)
    Write-Output ('MARCADOR_ATUALIZADO=' + (Split-Path $p -Leaf) + ' ' + $hashAtual.Substring(0,8) + ' -> ' + $hashNovo.Substring(0,8))
}
Write-Output 'R67D_OK'
