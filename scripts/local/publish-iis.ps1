# A2 (rodada 4): publica API e Web em Release para implantacao IIS inprocess.
# Remove appsettings de ambiente especifico da saida: o SDK Web embarca TODO appsettings*.json
# na publicacao e os projetos versionam appsettings.Development.json (DemoSeed, TestSignin,
# portas de dev). Na producao/aceite esses arquivos devem estar ausentes do diretorio.
param(
    [string]$OutDir = "C:\inetpub\plantao",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$targets = @(
    @{ Name = "Web"; Project = (Join-Path $root "backend\PlantaoPro.Web\PlantaoPro.Web.csproj") },
    @{ Name = "API"; Project = (Join-Path $root "backend\PlantaoPro.Api\PlantaoPro.Api.csproj") }
)

foreach ($t in $targets) {
    $out = Join-Path $OutDir $t.Name
    Write-Host "Publicando $($t.Name) -> $out"
    dotnet publish $t.Project -c $Configuration --nologo -o $out
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish $($t.Name) falhou com codigo $LASTEXITCODE." }

    foreach ($f in @("appsettings.Development.json", "appsettings.Testing.json")) {
        $p = Join-Path $out $f
        if (Test-Path $p) {
            Remove-Item $p -Force
            Write-Host "  Removido da saida: $f (config de ambiente nao sai para producao)."
        }
    }
}

Write-Host ""
Write-Host "Etapas seguintes (detalhe em docs\deploy\guia-implantacao-iis.md):"
Write-Host "  1. Criar pools PlantaoProWebAppPool / PlantaoProApiAppPool (No Managed Code)."
Write-Host "  2. Criar site plantao-api (http/127.0.0.1:8197) e plantao-web (https/+:443)."
Write-Host "  3. Definir variaveis de ambiente por site (ASPNETCORE_ENVIRONMENT=Production,"
Write-Host "     ConnectionStrings__Default + Jwt__* na API; PlantaoProApi__BaseUrl e"
Write-Host "     DataProtection__KeysDirectory=C:\ProgramData\PlantaoPro\DataProtection no Web)."
Write-Host "  4. Executar o roteiro de validacao da secao 9 do guia."
