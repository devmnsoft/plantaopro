# v1.18 — Diagnóstico inicial

Data UTC: 2026-07-21.

## Comandos executados antes de alterar código

| Comando | Resultado real |
|---|---|
| `git status` | Branch inicial `work`, árvore limpa. |
| `git branch --show-current` | `work`. |
| `git log -10 --oneline` | Últimos commits: `aa3c7d4`, `85157c6`, `87f0229`, `169a4c6`, `241c4be`, `6c197b1`, `da45d75`, `fd1bebc`, `734307e`, `49cb015`. |
| `dotnet --info` | Falhou no ambiente local: `/bin/bash: line 1: dotnet: command not found`. |
| `dotnet restore backend/PlantaoPro.sln` | Falhou no ambiente local: `/bin/bash: line 1: dotnet: command not found`. |
| `dotnet build backend/PlantaoPro.sln -c Release` | Falhou no ambiente local: `/bin/bash: line 1: dotnet: command not found`. |
| `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --logger "console;verbosity=detailed"` | Falhou no ambiente local: `/bin/bash: line 1: dotnet: command not found`. |

## Workflow PR #254

O ambiente local não possui `gh` CLI (`/bin/bash: line 1: gh: command not found`) e não expôs artefatos/logs do GitHub Actions. Portanto, não houve evidência local suficiente para listar nomes e mensagens completas dos testes xUnit que falharam no PR #254.

Evidência disponível no repositório: `.github/workflows/dotnet-ci.yml` executava os jobs `runtime-e2e-v113` a `runtime-e2e-v117` chamando `psql "$ConnectionStrings__Default" -f ...`, passando uma connection string Npgsql com ponto e vírgula diretamente para o `psql`. Esse padrão é incompatível com a recomendação da rodada e foi substituído por variáveis `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD` e `psql -v ON_ERROR_STOP=1 -f ...`.
