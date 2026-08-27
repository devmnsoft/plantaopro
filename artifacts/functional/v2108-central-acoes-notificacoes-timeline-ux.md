# v2.10.8 — Central de Ações, notificações, timeline e UX

Data da execução: 2026-08-27 (UTC).

## Resultado executivo

**Implementação funcional bloqueada antes de qualquer alteração.** O executável
`dotnet` não está instalado nem disponível no `PATH` deste contêiner. A validação
inicial obrigatória falhou com `dotnet: command not found` (exit code 127) tanto no
restore quanto no build Debug. Conforme a regra explícita desta rodada, nenhuma
alteração funcional, visual, de banco de dados ou de testes foi realizada.

O clone também não possui remoto Git configurado e somente continha a branch local
`work`, no commit `d0f364c`. Por isso, `git fetch origin main` falhou e não foi possível
atualizar ou confirmar a `main` contra o GitHub. A branch solicitada
`codex/v2108-central-acoes-notificacoes-timeline-ux` foi criada a partir do único estado
local disponibilizado pelo ambiente; este relatório não apresenta essa base como uma
`main` remotamente sincronizada.

## Confirmação do SDK exigido

Não há `global.json`. A inspeção dos arquivos de projeto confirmou que API, Web,
Tests, Application, Domain, Infrastructure, CrossCutting e Tools usam `net10.0`.
`backend/Directory.Build.props` fixa `LangVersion` em `10.0`. Logo, a retomada requer
um SDK .NET 10 compatível; o projeto não foi rebaixado para contornar o ambiente.

## Funcionalidades implementadas

Nenhuma. Central de Ações Operacionais, notificações acionáveis e timeline unificada
não foram iniciadas sem restore/build inicial verde. Também não foram duplicados ou
alterados AppShell, busca global, financeiro e Central Meu Dia existentes. Nenhuma
funcionalidade foi removida e nenhum mock ou dado fake foi acrescentado.

## Telas alteradas

Nenhuma. Sem aplicação compilável, não seria possível validar com segurança estados de
loading, erro e vazio, drawer/modal, filtros, kanban/lista, responsividade mobile,
acessibilidade ou integração com dados reais. Consequentemente, não há screenshot desta
rodada documental.

## Scripts criados

Nenhum script SQL ou de aplicação foi criado. Em particular, nenhum DDL foi adicionado
ao runtime de requisição e nenhuma estrutura de ações, timeline, preferências ou
histórico de leitura foi criada sem a possibilidade de validar migrações e testes.

## Regras de permissão aplicadas

Nenhuma regra nova foi aplicada. A implementação futura ainda deve comprovar isolamento
por `tenant_id`, autorização por perfil/permissão, minimização de dados sensíveis,
links seguros e validação server-side de cada ação. Este documento não afirma que esses
critérios foram atendidos para funcionalidades que não foram implementadas.

## Decisões de UX e melhorias de design

Nenhuma mudança de UX ou design foi feita. A decisão segura foi preservar a interface
existente em vez de produzir uma alteração não compilada. Na retomada, a Central deverá
usar seletores com dados reais (nunca IDs manuais), hierarquia visual calma, badges
comedidos, ações claras e estados responsivos e acessíveis, sem `alert()`, `confirm()`
ou `href="#"`.

## Validação inicial obrigatória

| Comando | Resultado |
|---|---|
| `git status --short` | sucesso; árvore inicialmente limpa |
| `git branch --show-current` | sucesso; branch inicial `work` |
| `dotnet --info \|\| true` | bloqueado; `dotnet: command not found` |
| `dotnet restore backend/PlantaoPro.sln` | bloqueado; exit code 127 |
| `dotnet build backend/PlantaoPro.sln -c Debug --no-restore` | bloqueado; exit code 127 |
| `git fetch origin main` | bloqueado; não há remoto `origin` configurado |
| `git switch -c codex/v2108-central-acoes-notificacoes-timeline-ux` | sucesso |
| `find . -name global.json -print` | sucesso; nenhuma ocorrência |
| `rg -n '<TargetFramework\|<TargetFrameworks\|<LangVersion' backend --glob '*.csproj' --glob '*.props'` | sucesso; confirmou `net10.0` e C# 10 |

## Busca de padrões proibidos

A busca obrigatória foi executada antes de qualquer mudança funcional:

```text
rg -n 'href="#"|alert\(|confirm\(|Digite.*Id|Digite.*ID|placeholder=.*Id|placeholder=.*ID|SELECT \*' backend/PlantaoPro.Api backend/PlantaoPro.Web backend/PlantaoPro.Tests
```

Ela retornou dez correspondências preexistentes. Oito são textos explicativos ou
assertivas/fixtures de segurança em testes. Duas são violações reais preexistentes em
`backend/PlantaoPro.Web/Views/Assinaturas/_Form.cshtml`: os placeholders de
`ClienteId` e `PlanoId` solicitam IDs manuais. Esses campos não foram modificados porque
a correção correta exige lookups reais, autorização, isolamento por tenant e testes —
alteração funcional proibida enquanto o SDK estiver ausente. Não foi identificado
`SELECT *`, `href="#"` ou `confirm()` em código de produção pela busca indicada.

## Validações finais

Os comandos finais que dependem do SDK foram executados e permanecem bloqueados pela
mesma ausência de `dotnet`; isso não representa aprovação de build ou testes.

| Comando | Resultado |
|---|---|
| `dotnet clean backend/PlantaoPro.sln` | bloqueado; exit code 127 |
| `dotnet restore backend/PlantaoPro.sln` | bloqueado; exit code 127 |
| `dotnet build backend/PlantaoPro.sln -c Debug --no-restore` | bloqueado; exit code 127 |
| `dotnet build backend/PlantaoPro.sln -c Release --no-restore` | bloqueado; exit code 127 |
| `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build` | bloqueado; exit code 127 |
| `git diff --check` | sucesso; exit code 0 |

## Limitações reais restantes

1. Configurar um remoto `origin`, buscar o GitHub e atualizar a `main` de verdade.
2. Disponibilizar o SDK oficial .NET 10 e obter restore e build inicial Debug verdes.
3. Inventariar e reutilizar as estruturas reais existentes, sem duplicar AppShell,
   busca global, financeiro ou Central Meu Dia.
4. Implementar a Central de Ações com dados reais, isolamento por tenant, RBAC, SQL
   explícito e parametrizado, DTOs Dapper materializáveis e logging relevante.
5. Evoluir notificações e timeline nos fluxos principais, com migrações idempotentes e
   sem DDL em runtime.
6. Criar os testes funcionais e de contrato solicitados e executar toda a matriz
   Debug/Release/testes.
7. Validar visualmente desktop, tablet e mobile e produzir screenshots representativos.
8. Corrigir os dois campos manuais preexistentes de assinatura com seletores abastecidos
   por dados reais e autorizados.
