# R66: risk-scan dos arquivos candidatos ao anexo (backend/sql + schema/150_portal):
# comandos backslash, CREATE INDEX sem IF NOT EXISTS, INSERT sem ON CONFLICT/NOT EXISTS,
# DROP/TRUNCATE/LOCK, e linhas de conteudo por arquivo (visao geral).
$ErrorActionPreference = 'Continue'
$alvos = Get-ChildItem 'C:\MNSOFT\plantaopro\backend\sql' -Filter *.sql | Sort-Object Name
$alvos += Get-Item 'C:\MNSOFT\plantaopro\database\schema\150_portal_unidade_contratos_v2100.sql'
foreach ($f in $alvos) {
    $ls = @(Get-Content $f.FullName)
    $risco = @()
    $ins = 0; $oi = 0
    for ($i = 0; $i -lt $ls.Count; $i++) {
        $l = $ls[$i]
        if ($l -and $l.TrimStart().StartsWith('\')) { $risco += 'L' + ($i + 1) + ':BS[' + $l.Trim().Substring(0, [Math]::Min(60, $l.Trim().Length)) + ']' }
        if ($l -match '(?i)^\s*create\s+(unique\s+)?index\s+(?!if\s+not\s+exists\b|concurrently\b)\S') { $risco += 'L' + ($i + 1) + ':IDX' }
        if ($l -match '(?i)^\s*drop\s+(table|view|index|sequence|constraint|database)') { $risco += 'L' + ($i + 1) + ':DROP' }
        if ($l -match '(?i)^\s*truncate\b') { $risco += 'L' + ($i + 1) + ':TRUNC' }
        if ($l -match '(?i)\binsert\s+into\b') { $ins++ }
        if ($l -match '(?i)\bon\s+conflict\b') { $oi++ }
    }
    $tag = ''
    if ($ins -gt 0 -and $oi -eq 0) { $tag = 'INS_SEM_ONCONFLICT=' + $ins }
    if ($tag -ne '') { $risco += $tag }
    '{0} :: linhas={1} :: {2}' -f $f.Name, $ls.Count, ($(if ($risco.Count -gt 0) { $risco -join ' | ' } else { 'LIMPO' }))
}
