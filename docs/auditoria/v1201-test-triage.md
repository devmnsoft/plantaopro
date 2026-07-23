# Triagem de testes v1.20.1 → v1.20.2

Data: 2026-07-23

## Diagnóstico

A execução local dos gates .NET ficou bloqueada porque o container não possui o comando `dotnet` instalado. O build e os testes retornaram código 127 antes de compilar ou executar qualquer teste.

## Comandos executados

- `dotnet restore backend/PlantaoPro.sln`
- `dotnet build backend/PlantaoPro.sln -c Release --no-restore`
- `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build --logger "trx;LogFileName=tests.trx" --results-directory artifacts/TestResults --logger "console;verbosity=detailed"`

## Ações realizadas

- Consolidado `EmptyStateViewModel` em um único modelo canônico.
- Adicionado teste de contrato para falhar quando houver múltiplas declarações do modelo.
- Regenerado `scrpt_completo.sql` com schema 140 preservado no manifesto.
- Corrigida a proteção de colunas legadas antes da leitura de `reg_date`/`reg_status`.
- Atualizado o workflow de equivalência para comparar instalação limpa com upgrade real e dumps normalizados.

## Pendência honesta

Reexecutar os gates em ambiente com .NET SDK e PostgreSQL 16 disponíveis. Nenhum resultado local foi declarado como verde sem execução real.
