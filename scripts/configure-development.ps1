param(
    [Parameter(Mandatory = $true)] [string] $ConnectionString,
    [Parameter(Mandatory = $true)] [string] $JwtKey
)

$ErrorActionPreference = 'Stop'
$apiDir = Join-Path $PSScriptRoot '..' 'backend' 'PlantaoPro.Api'
Push-Location $apiDir
try {
    dotnet user-secrets set "ConnectionStrings:Default" $ConnectionString
    dotnet user-secrets set "Jwt:Key" $JwtKey
}
finally {
    Pop-Location
}
