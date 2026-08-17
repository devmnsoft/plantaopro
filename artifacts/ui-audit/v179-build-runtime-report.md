# Build e runtime — v1.79.0

## Ambiente
Os comandos .NET, scripts, JavaScript e mobile devem ser registrados após a validação final desta entrega. O smoke autenticado requer aplicação, API, banco, tenant e storage state válidos; ausência desses elementos é bloqueio, nunca aprovação.

## Validação no Windows / Visual Studio
1. Abrir `backend/PlantaoPro.sln`; restaurar NuGet e compilar em Release.
2. Iniciar API e Web com configuração de tenant real.
3. Executar `PlantaoPro.Tests` no Test Explorer.
4. Gerar storage state autenticado e executar `scripts/ui/run-visual-smoke.ps1`.

## Resultado neste ambiente (2026-08-17)
- `dotnet --info`: bloqueado — executável `dotnet` ausente (código 127).
- restore/build/test: bloqueados pela mesma ausência; **não aprovados**.
- O trabalho prosseguiu somente com validações estáticas. A homologação deve seguir o roteiro acima.
