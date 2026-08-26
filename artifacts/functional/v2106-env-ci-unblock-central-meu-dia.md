# v2.10.6 — diagnóstico de ambiente, SDK e CI

Data da execução: 2026-08-26 (UTC).

## Resultado executivo

- O repositório exige o SDK **.NET 10**: todos os projetos usam `net10.0` e `backend/Directory.Build.props` fixa `LangVersion` em `10.0`.
- Não existe `global.json` no repositório.
- O contêiner não possui `dotnet` no `PATH` nem uma instalação localizável no sistema de arquivos.
- Nenhum download alternativo ou bypass inseguro foi tentado. A instalação anterior pelo endpoint oficial já havia retornado HTTP 403 e a busca local não encontrou SDK reutilizável.
- O remoto `origin` foi configurado como `https://github.com/devmnsoft/plantaopro.git`, mas `git fetch --all --prune` foi bloqueado pelo proxy com HTTP 403.
- Já existe CI em `.github/workflows/dotnet-ci.yml`, acionada em push e pull request. Ela usa `actions/setup-dotnet@v4` com `dotnet-version: '10.0.x'` e executa diagnóstico, restore, build Release e testes da solução. Portanto, não foi necessário alterar o workflow.
- O executável `gh` está instalado, porém não há autenticação em nenhum host GitHub. Não existe comando `make_pr` disponível no contêiner.
- A implementação funcional da Central Meu Dia, busca global e design premium **não foi iniciada**, pois não foi possível obter uma validação inicial executável de restore/build/test. Isto cumpre a condição de não produzir mudanças funcionais sem build inicial verde.

## Estado inicial

| Verificação | Resultado |
| --- | --- |
| Diretório | `/workspace/plantaopro` |
| Branch inicial | `work` |
| Árvore de trabalho | limpa |
| Remotos iniciais | nenhum |
| Sistema | Linux x86_64, kernel `6.18.35` |
| `dotnet --info` | `dotnet: command not found` |
| `dotnet --list-sdks` | `dotnet: command not found` |
| `which dotnet` | nenhuma saída |
| `global.json` | inexistente |
| Workflow | `.github/workflows/dotnet-ci.yml` e `.github/workflows/database-one-click.yml` |

A branch `codex/v2106-env-ci-unblock-central-meu-dia` foi criada antes das mudanças desta rodada.

## Versão exigida

O levantamento com `find` encontrou a solução e os projetos em `backend/`. A busca com `rg` confirmou `TargetFramework` `net10.0` nos projetos API, Web, Tests, Application, Domain, Infrastructure, CrossCutting e Tools. O projeto não deve ser rebaixado para contornar o ambiente. Como não há `global.json`, o CI existente seleciona corretamente a linha `10.0.x` do SDK.

## SDK local e bloqueio de execução

Foi executado:

```text
find / -type f -name dotnet 2>/dev/null | head -20
```

O comando não retornou arquivo algum. Assim, não havia diretório que pudesse ser acrescentado temporariamente ao `PATH`. Os comandos `dotnet restore`, `dotnet build` e `dotnet test` não foram repetidos: sem executável, repetição produziria apenas o mesmo `command not found` e não constituiria validação real.

Não foi feita instalação por fonte não oficial, tentativa repetitiva de download ou alteração de `TargetFramework`. O bloqueio técnico que permanece é a ausência do SDK .NET 10 no contêiner.

## Remoto, CI e abertura de PR

O remoto ausente foi configurado com:

```text
git remote add origin https://github.com/devmnsoft/plantaopro.git
```

Depois disso, `git remote -v` exibiu `origin` para fetch e push. A tentativa de sincronização retornou:

```text
fatal: unable to access 'https://github.com/devmnsoft/plantaopro.git/': CONNECT tunnel failed, response 403
```

O workflow existente foi inspecionado com `rg` e já contém:

- checkout por `actions/checkout@v4`;
- SDK por `actions/setup-dotnet@v4` e `dotnet-version: '10.0.x'`;
- `dotnet --info`;
- `dotnet restore backend/PlantaoPro.sln`;
- `dotnet build backend/PlantaoPro.sln -c Release --no-restore`;
- `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build`;
- verificações adicionais e publicação de evidências.

O comando `command -v make_pr` não retornou resultado. `command -v gh` encontrou o GitHub CLI, mas `gh auth status` informou que não há login. Consequentemente, o CI não pôde ser disparado remotamente e a abertura do PR depende de credencial/conectividade externa disponível ao orquestrador.

## Comandos e resultados

### Diagnóstico

- `pwd`: sucesso; `/workspace/plantaopro`.
- `git status --short`: sucesso; sem alterações.
- `git branch --show-current`: sucesso; `work` antes da criação da branch solicitada.
- `git remote -v`: sucesso; nenhuma saída inicialmente.
- `dotnet --info`: falha ambiental; comando inexistente.
- `dotnet --list-sdks`: falha ambiental; comando inexistente.
- `which dotnet`: nenhuma instalação no `PATH`.
- `uname -a`: sucesso.
- `ls -la`, `ls -la backend`, `ls -la .github/workflows`: sucesso.
- `find . -name "global.json" -o -name "*.csproj" -o -name "*.sln"`: sucesso; nenhum `global.json`.
- `rg -n "TargetFramework|TargetFrameworks|LangVersion|net10.0|net8.0|global.json" .`: sucesso; confirmou `net10.0` e C# 10.
- `find / -type f -name dotnet 2>/dev/null | head -20`: sucesso; nenhuma ocorrência.

### CI e verificações independentes do SDK

- `git fetch --all --prune`: falhou por limitação de rede (`CONNECT tunnel failed`, HTTP 403).
- `rg -n "setup-dotnet|dotnet-version|global-json-file|dotnet restore|dotnet build|dotnet test" .github/workflows`: sucesso; CI .NET 10 já adequada.
- `python3 scripts/repository-security-check.py`: sucesso; `repository-security ok`.
- `python3 scripts/check-csharp10-compatibility.py`: sucesso; compatibilidade C# 10 e CSS Razor validada.
- `python3 scripts/validate-scrpt-completo.py`: sucesso; cobertura declarada de 100%.
- `gh auth status`: falha ambiental; GitHub CLI sem autenticação.
- `rg -n <padrões-proibidos> backend/PlantaoPro.Api backend/PlantaoPro.Web backend/PlantaoPro.Tests scripts docs README.md .env.example`: executado; encontrou ocorrências preexistentes, inclusive `SELECT *`, SQL interpolado, placeholders de IDs e credenciais locais de teste. Nenhuma ocorrência foi introduzida por esta rodada documental; os achados devem ser tratados em uma rodada própria com build e testes disponíveis.

## Escopo funcional

Nenhum arquivo de aplicação, banco de dados ou teste foi alterado. Central Meu Dia, busca global, favoritos, páginas recentes, widgets por perfil, preferências e polimento visual permanecem fora desta rodada. A retomada exige, nesta ordem:

1. disponibilizar SDK .NET 10 oficial;
2. obter sucesso em restore, builds Debug da API e da solução e testes iniciais;
3. corrigir qualquer falha real encontrada;
4. somente então iniciar a implementação funcional;
5. executar toda a matriz final Debug/Release/testes e os gates de segurança.
