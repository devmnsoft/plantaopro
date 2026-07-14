# Evidência de homologação runtime real — PlantãoPro

Data: 2026-07-09.
Branch alvo: `codex/homologacao-real-runtime-bugs-fluxos-criticos`.

## Comandos executados

| Comando | Resultado |
| --- | --- |
| `git checkout main` | Falhou: repositório local contém apenas a branch `work`, sem branch/remoto `main`. |
| `git pull --ff-only origin main` | Não executável neste checkout, pois não há remoto `origin` configurado. |
| `git status --short --branch` | OK, branch local `work`. |
| `gh pr list --state open` | Bloqueado por ambiente: `gh` não instalado. |
| `dotnet --info` | Bloqueado por ambiente: SDK .NET não instalado. |
| `docker --version` | Bloqueado por ambiente: Docker não instalado. |

## Resultado

Homologação runtime preparada, mas pendente de validação em ambiente com SDK .NET, Docker e PostgreSQL.

## Erros encontrados

- Checkout não possui `main` nem remoto `origin` para sincronização.
- CLI `gh`, SDK .NET, Docker e cliente PostgreSQL não estão disponíveis no executor.
- Defaults de senha demo/local apareciam em scripts de apoio e foram substituídos por placeholders explícitos via variáveis de ambiente.

## Correções feitas

- `docker-compose.yml` passou a exigir/usar placeholder local `CHANGE_ME_LOCAL_POSTGRES` quando `PLANTAOPRO_POSTGRES_PASSWORD` não for informado.
- `scripts/database/apply-local-postgres.sh` passou a usar o mesmo placeholder local por padrão.
- `scripts/smoke/smoke-api.sh` passou a usar `CHANGE_ME_DEMO_PASSWORD` por padrão, sem versionar senha real/demo fixa.
- Testes contratuais de runtime foram reforçados para garantir evidências, cobertura de smoke, rotas Web e appsettings sem segredos triviais.

## Pendências reais

- Executar `dotnet restore`, `dotnet build` e `dotnet test` em executor com SDK .NET.
- Executar `docker compose up -d`, aplicar banco e rodar smoke real contra API/Web.
- Validar manualmente CRUDs, RBAC e jornadas críticas com usuários demo configurados por segredo de ambiente.

## Status final

**Bloqueado por ambiente** para runtime real. O repositório foi preparado para homologação sem declarar produção.

## Configuração JWT

A execução local, CI, Docker e IIS exige `Jwt__Key` com pelo menos 32 caracteres, além de `Jwt__Issuer` e `Jwt__Audience`. O erro `Configuração Jwt:Key não encontrada ou inválida` indica chave ausente, vazia ou curta. Use user-secrets ou variáveis de ambiente e nunca versione segredo real. Guia completo: `docs/configuracao-jwt-local-ci.md`.
