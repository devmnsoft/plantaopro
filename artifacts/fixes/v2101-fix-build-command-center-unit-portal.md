# v2.10.1 — correção de build do Command Center e Portal da Unidade

## Causa-raiz

O SQL do `UnitDashboardService` estava em uma string verbatim (`@"..."`), mas os
aliases SQL usavam escape de string comum (`\"Today\"`, `\"Future\"`, etc.). Em
uma string verbatim, a barra invertida não escapa aspas. Com isso, a primeira
ocorrência de `\"` encerrava a string C# e o restante do SQL era interpretado
como código, produzindo em cascata `CS1002`, `CS1026`, `CS1056`, `CS1010` e
`CS0742`.

## Arquivos revisados e corrigidos

- `backend/PlantaoPro.Api/ManagerCommandCenterService.cs`: revisado integralmente,
  com atenção especial ao SQL inicial. As aspas de aliases já estavam no formato
  correto para string verbatim (`""Alias""`), os parâmetros Dapper estavam
  preservados e não havia alteração necessária.
- `backend/PlantaoPro.Api/UnitPortalServices.cs`: o SQL do dashboard foi convertido
  em string verbatim multilinha válida, usando aspas duplicadas nos aliases e
  mantendo `@tenantId` e `@unitId` como parâmetros Dapper. A consulta de aprovação
  também passou a enumerar somente as seis colunas consumidas, eliminando
  `select *` sem alterar o filtro de tenant, o bloqueio transacional ou a auditoria.

## Erros eliminados

A correção remove a origem sintática dos erros `CS1002`, `CS1026`, `CS1056`,
`CS1010` e `CS0742`. Não foram usados raw string literals, interpolação SQL,
mudança de `TargetFramework` ou mudança de `LangVersion`.

O `CS0006` reportado nos testes era uma consequência esperada de
`PlantaoPro.Api.dll` não ser gerada quando a compilação da API parava nos erros
de sintaxe; não foi aplicado tratamento independente para esse erro derivado.

## Validações executadas

| Comando | Resultado |
| --- | --- |
| `dotnet clean backend/PlantaoPro.sln` | Bloqueado pelo ambiente: `/bin/bash: dotnet: command not found` (exit 127). |
| `dotnet restore backend/PlantaoPro.sln` | Bloqueado pelo ambiente: `/bin/bash: dotnet: command not found` (exit 127). |
| `dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj -c Debug` | Bloqueado pelo ambiente: `/bin/bash: dotnet: command not found`. |
| `dotnet build backend/PlantaoPro.sln -c Debug --no-restore` | Bloqueado pelo ambiente: `/bin/bash: dotnet: command not found` (exit 127). |
| `dotnet build backend/PlantaoPro.sln -c Release --no-restore` | Bloqueado pelo ambiente: `/bin/bash: dotnet: command not found` (exit 127). |
| `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build` | Bloqueado pelo ambiente: `/bin/bash: dotnet: command not found` (exit 127). |
| `python3 scripts/repository-security-check.py` | Aprovado: `repository-security ok`. |
| `python3 scripts/check-csharp10-compatibility.py` | Aprovado: compatibilidade C# 10 e CSS Razor validada. |
| `python3 scripts/validate-scrpt-completo.py` | Aprovado: `ok: true`, cobertura `100.0%`. |
| Busca obrigatória com `rg` | Executada. Encontrou apenas ocorrências preexistentes fora dos services corrigidos (testes, documentação, scripts e dois placeholders antigos); nenhum novo secret foi introduzido. O `select *` do Portal da Unidade foi removido. |
| `git diff --check` | Aprovado, sem erros de whitespace. |

Também foi tentada a instalação local do SDK 10 com o instalador oficial, mas o
download de `https://dot.net/v1/dotnet-install.sh` retornou HTTP 403.

## Limitação existente

A limitação real desta execução é a ausência do SDK .NET no contêiner e o bloqueio
HTTP ao instalador, portanto os builds e testes binários precisam ser confirmados
pela CI ou por um ambiente com .NET 10. As verificações estáticas disponíveis
foram executadas com sucesso.
