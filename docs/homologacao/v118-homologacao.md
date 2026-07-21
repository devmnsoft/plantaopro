# v1.18 — Homologação

Data UTC: 2026-07-21.

## Implementado

- Workflow reorganizado em `build-test`, `database-clean-install`, `database-upgrade`, `runtime-e2e` e `mobile-check`.
- Pipeline de migrations canônico criado para banco vazio e atualização.
- Dependências NuGet revisadas nos pontos identificados: Npgsql, JwtBearer e Swashbuckle no Web.

## Validado

- Validação local de Git executada: branch criada a partir do snapshot local e árvore inicialmente limpa.
- Busca estática por DDL em código C# operacional não encontrou `CREATE TABLE`, `ALTER TABLE`, `CREATE SCHEMA`, `CREATE EXTENSION` ou `CREATE INDEX` em controllers, middlewares ou services C#; ocorrências restantes são arquivos SQL versionados.

## Parcial

- Testes xUnit, build, restore, format e smokes não foram executados localmente por ausência do SDK `dotnet` no contêiner.
- Logs completos do PR #254 não estavam disponíveis no ambiente local porque não há `gh` CLI nem artefatos do Actions expostos.
- As cinco jornadas E2E permanecem dependentes da execução real do `runtime-e2e` no GitHub Actions.
- Mobile check documenta a ausência de `mobile/package.json` no snapshot quando aplicável.

## Futuro

- Completar a matriz comportamental de autorização e integração após o CI fornecer falhas específicas reproduzíveis.
- Evoluir contratos de Application/Infrastructure de forma incremental por fluxo real, sem criar abstrações artificiais.
