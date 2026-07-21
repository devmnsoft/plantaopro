# Diagnóstico CI v1.18.1

Data: 2026-07-21.

## Comandos executados localmente

- `git status`: árvore limpa na branch inicial `work` antes das alterações.
- `git branch --show-current`: `work`; criada a branch `codex/v1181-ci-verde-jornada-clinica-real`.
- `git log -10 --oneline`: HEAD `df4bc67 Merge pull request #255 ... v1.18`.
- `dotnet --info`: falhou no ambiente local com `/bin/bash: dotnet: command not found`.
- `dotnet restore backend/PlantaoPro.sln`: não executou porque o SDK .NET não está instalado no container.
- `dotnet build backend/PlantaoPro.sln -c Release --no-restore`: não executou porque o SDK .NET não está instalado no container.

## Erros reais identificados no repositório

### Build

O container local não possui `dotnet`; por isso o erro reproduzido foi ambiental, não de compilação de código: `/bin/bash: dotnet: command not found`.

### Migrations canônicas

Arquivo: `scripts/apply-canonical-migrations.sh`.

Causa raiz encontrada por inspeção:

1. A migration era registrada em `plantaopro.schema_migrations` somente depois de executar `psql -f`, mas fora da mesma transação; uma falha entre DDL e registro deixava banco parcialmente atualizado sem controle transacional.
2. Migrations já aplicadas eram ignoradas sem comparar checksum; alterações posteriores em SQL canônico não geravam falha controlada.
3. Seeds de demonstração eram aplicados como parte obrigatória da cadeia estrutural, contrariando a separação entre DDL canônico e dados opcionais.
4. O cenário de upgrade no workflow inseria registros artificiais em `schema_migrations` com checksum `pre-v118-upgrade-baseline`, o que mascarava a execução física real e impediria uma verificação de checksum confiável.

Primeira migration candidata a bloquear a sequência em upgrade real: `000_base_schema`, porque o workflow anterior pré-marcava essa migration com checksum artificial e a nova validação precisa comparar o checksum real do arquivo `database/PlantaoPro_PostgreSQL_Completo.sql`.

## PostgreSQL

`psql`/PostgreSQL não foram executados localmente nesta imagem porque o diagnóstico inicial já foi bloqueado pela ausência do SDK .NET e não há serviço PostgreSQL ativo configurado no container. Os scripts corrigidos executam com `psql -v ON_ERROR_STOP=1` e serão validados no GitHub Actions com PostgreSQL 16.
