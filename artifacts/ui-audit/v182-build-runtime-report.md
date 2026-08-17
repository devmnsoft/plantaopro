# Build e runtime — v1.82.0

## Ambiente e resultado

- SDK requerido: **.NET 10** (`TargetFramework` `net10.0`); não há `global.json`; `LangVersion` permanece 10.
- SDK encontrado: **BLOQUEADO — `dotnet` não está instalado** (`/bin/bash: dotnet: command not found`).
- `dotnet --info`, restore, build, testes e startup foram realmente tentados em 17/08/2026 e ficaram **BLOQUEADOS** antes da execução pelo mesmo motivo.
- Nenhum erro de C#, Razor, DI, banco ou middleware pode ser inferido sem compilador/runtime. Nenhum arquivo C#/Razor foi alterado.

| Comando | Resultado |
|---|---|
| `dotnet --info` | BLOQUEADO: executável ausente |
| `dotnet restore backend/PlantaoPro.sln` | BLOQUEADO: executável ausente |
| `dotnet build backend/PlantaoPro.sln -c Release` | BLOQUEADO: executável ausente |
| `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release` | BLOQUEADO: executável ausente |
| `dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj` | BLOQUEADO: executável ausente; porta/startup não determinados |

## Reprodução Windows / Visual Studio

1. Instalar o SDK .NET 10 e confirmar com `dotnet --info` (ou abrir `backend/PlantaoPro.sln` no Visual Studio compatível com .NET 10).
2. Executar, na raiz: `dotnet restore backend/PlantaoPro.sln`.
3. Executar: `dotnet build backend/PlantaoPro.sln -c Release`.
4. Executar: `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release`.
5. Configurar os secrets/conexões descritos em `.env.example` e iniciar com `dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj`.

## Verificações executadas

Os gates Python, sintaxe JS/shell, geração/validação do PostgreSQL e suíte mobile foram executados. O script completo obteve 100% de cobertura no validador. A primeira execução de `check-layout-regression.py` revelou referências v1.81 e dois viewports legados ausentes; o contrato foi corrigido e o gate passou na repetição.
