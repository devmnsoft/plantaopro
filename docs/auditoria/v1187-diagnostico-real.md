# Diagnóstico real v1.18.7 — runtime homologável e Central de Segurança

## Execuções iniciais

- `git status --short --branch`: branch de trabalho criada a partir do estado local.
- `git log -10 --oneline`: último commit observado `f79e9d5 Merge pull request #262 from devmnsoft/codex/corrigir-schema-de-permissoes-e-build`.
- `dotnet --info`: **falhou no ambiente local** porque o executável `dotnet` não está instalado no container (`/bin/bash: dotnet: command not found`).
- `dotnet restore`, `dotnet build` e `dotnet test`: não executados localmente pelo mesmo bloqueio de runtime .NET ausente.
- `psql -v ON_ERROR_STOP=1 -d plantaopro -f database/scrpt_completo.sql`: não executado localmente porque não há serviço PostgreSQL 16 provisionado neste container.

## Primeira falha confirmada

- Falha empírica local: ausência do SDK/runtime `dotnet` no container.
- Falha de banco esperada a partir do script v1.18.6: `function uuid_generate_v4() does not exist` quando `SET search_path TO plantaopro;` remove `public` da resolução de funções.
- SQLSTATE esperado no PostgreSQL para função inexistente: `42883`.
- Arquivo envolvido antes da correção: `database/scrpt_completo.sql` gerado por `scripts/generate-scrpt-completo.py`.
- Objeto/função envolvido: `uuid_generate_v4()` não qualificado em tabelas históricas.

## Causa raiz

O gerador continuava incorporando fontes históricas com `SET search_path TO plantaopro;` e chamadas não qualificadas a funções instaladas em `public`, especialmente `uuid_generate_v4()`. Assim, uma instalação limpa podia falhar antes dos gates de runtime, e o CI também coletava TRX em caminho instável.

## Correção aplicada

- O gerador passou a normalizar `SET search_path TO plantaopro;` para `SET search_path TO plantaopro, public;`.
- O gerador passou a converter `uuid_generate_v4()` não qualificado para `gen_random_uuid()`.
- O gerador passou a qualificar `unaccent()` como `public.unaccent()`.
- Foram adicionados arquivos canônicos por domínio em `database/schema/`.
- A Central de Segurança v1.18.7 adicionou estruturas persistidas para sessões, refresh tokens, revogações, histórico de senha e políticas de senha.
- O CI passou a gravar TRX em `artifacts/TestResults/tests.trx` e validar counters reais.

## Resultado após correção local possível

- `python3 scripts/generate-scrpt-completo.py`: executado com sucesso e determinístico em duas execuções consecutivas.
- Validação textual: não restaram usos não qualificados de `uuid_generate_v4()`, `unaccent()` ou `SET search_path TO plantaopro;` em `database/scrpt_completo.sql` e nos novos arquivos canônicos.

## Quantidade real de testes

Não foi possível coletar a quantidade real neste container porque `dotnet` não está instalado. O workflow foi corrigido para extrair `total`, `executed` e `failed` diretamente do TRX real do `dotnet test` no CI, sem TRX sintético.

## Warnings

- Ambiente local sem .NET SDK.
- Ambiente local sem PostgreSQL 16 ativo para execução real de instalação limpa/idempotência.

## Jobs do CI atualizados

- `build-test`
- `database-complete-script-clean-install`
- `database-complete-script-idempotency`
- `database-legacy-compatibility`
- `database-upgrade`
- `database-schema-equivalence`
- `runtime-from-complete-script`
- `auth-e2e`
- `security-access-e2e`
- `swagger-contract`
- `mobile-check`
- `repository-security`

## Pendências honestas

Este PR não deve ser declarado homologado em produção até os jobs remotos executarem com PostgreSQL 16 e SDK .NET disponíveis e todos ficarem verdes.
