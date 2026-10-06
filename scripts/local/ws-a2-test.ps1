Set-Location C:\MNSOFT\plantaopro
& 'scripts\local\run-dev-stop.ps1' | Out-Null
dotnet test 'backend\PlantaoPro.Tests\PlantaoPro.Tests.csproj' --nologo -v q
exit $LASTEXITCODE
