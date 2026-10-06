# ws-a2-list-user-secrets.ps1 -- lista chaves e prefixos dos user-secrets locais
$ids = @('plantaopro-api-development', 'plantaopro-web-development')
foreach ($id in $ids) {
    $p = Join-Path $env:APPDATA ("Microsoft\UserSecrets\" + $id + "\secrets.json")
    if (-not (Test-Path $p)) { $p = Join-Path $env:LOCALAPPDATA ("Microsoft\UserSecrets\" + $id + "\secrets.json") }
    Write-Host ('== ' + $id + ' -> ' + $p + ' existe=' + (Test-Path $p))
    if (Test-Path $p) {
        $j = Get-Content $p -Raw | ConvertFrom-Json
        foreach ($prop in $j.PSObject.Properties) {
            $v = [string]$prop.Value
            $prefix = if ($v.Length -gt 70) { $v.Substring(0, 70) + '...' } else { $v }
            Write-Host ('   ' + $prop.Name + ' = ' + $prefix)
        }
    }
}
exit 0
