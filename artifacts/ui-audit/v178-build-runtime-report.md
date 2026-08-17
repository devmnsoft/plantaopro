# Build e runtime — v1.78.0

## Ambiente Codex
`dotnet --info` foi executado em 17/08/2026 e retornou `dotnet: command not found`. Por isso restore, build e testes .NET ficaram bloqueados pelo ambiente e **não estão aprovados**. As validações estáticas continuaram normalmente.

## Homologação no Visual Studio/Windows
1. Abrir `backend/PlantaoPro.sln`; 2. restaurar pacotes; 3. compilar em Release; 4. executar `PlantaoPro.Tests`; 5. iniciar Web/API com tenant e usuários reais; 6. capturar storage state autenticado; 7. executar `scripts/ui/run-visual-smoke.ps1`.
