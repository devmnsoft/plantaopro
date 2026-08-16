# Build e runtime — v1.76.0

## Resultado neste ambiente

**BLOQUEADO:** `dotnet --info` retornou código 127 (`dotnet: command not found`) em 16/08/2026. Restore, build, testes .NET, inicialização do runtime e smoke navegado não foram declarados aprovados.

## Validação em Windows/Visual Studio

Abra `backend/PlantaoPro.sln`, restaure os pacotes, selecione Release e execute Build Solution e Test Explorer. Pela CLI com SDK compatível: `dotnet restore backend/PlantaoPro.sln`, `dotnet build backend/PlantaoPro.sln -c Release` e `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release`.
