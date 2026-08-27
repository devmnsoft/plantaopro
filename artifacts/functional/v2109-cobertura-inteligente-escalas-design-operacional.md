# v2.10.9 — Cobertura inteligente de escalas e design operacional

Data da execução: 2026-08-27 (UTC).

## Resultado executivo

**Implementação funcional bloqueada antes de qualquer alteração.** O executável
`dotnet` não está instalado ou disponível no `PATH` do contêiner. O restore e o build
Debug iniciais obrigatórios falharam com `dotnet: command not found` (exit code 127).
Conforme a regra explícita desta rodada, nenhuma alteração funcional, visual, de banco
de dados ou de testes foi realizada.

O clone foi entregue sem remoto Git configurado e a única branch inicialmente
disponível era `work`, no commit `c00aa0e`. O remoto `origin` foi então configurado com
o endereço informado na solicitação, mas o acesso ao GitHub foi recusado pelo proxy do
ambiente com HTTP 403. Portanto, não foi possível buscar nem atualizar a `main`. A branch
`codex/v2109-cobertura-inteligente-escalas-design-operacional` foi criada a partir do
único estado local fornecido pelo ambiente, sem afirmar que essa base corresponde à
`main` remota atualizada.

## SDK exigido pelo projeto

Não existe `global.json` no repositório. A inspeção dos arquivos `.csproj` confirmou
que API, Web, testes, camadas Application, Domain, Infrastructure e CrossCutting e as
ferramentas usam `net10.0`. O arquivo `backend/Directory.Build.props` fixa
`LangVersion` em `10.0`. A retomada exige um SDK .NET 10 compatível; o projeto não foi
rebaixado para contornar a limitação do ambiente.

## Funcionalidades implementadas

Nenhuma. O Mapa de Cobertura, os riscos operacionais, as recomendações de profissionais
e o fluxo de substituição não foram iniciados sem restore/build inicial verde. Não
foram repetidos ou alterados AppShell, Central Meu Dia, busca global, financeiro ou
notificações existentes. Nenhuma funcionalidade existente foi removida, e nenhum mock,
dado fake ou atalho visual foi adicionado.

## Regras de cobertura aplicadas

Nenhuma regra nova foi aplicada. A retomada ainda deve mapear quais dados reais existem
para profissional ausente, confirmação pendente, recusa, solicitação de troca,
documentação, especialidade, conflito de horário, descanso, carga horária e perfil
exigido pela unidade. Este relatório não apresenta como atendida uma regra que não pôde
ser implementada e validada.

## Critérios de elegibilidade e recomendação

Nenhum serviço de elegibilidade ou recomendação foi criado. A futura implementação
deve usar somente dados reais para tenant, perfil ativo, especialidade, disponibilidade,
conflitos, bloqueio cadastral, documentação e vínculo com unidade, ignorando e
documentando critérios para os quais não exista fonte persistida. Nenhuma pontuação ou
ordenação simulada foi introduzida.

## Telas alteradas

Nenhuma. Agenda, escalas, plantões, convites, trocas, ocorrências, detalhes, modais,
drawers, tabelas, filtros, badges e botões foram preservados. Sem aplicação compilável,
não seria seguro alterar ou afirmar validados os estados carregando, vazio, erro, sem
permissão, sem dados, coberto, descoberto, risco, pendência, substituição ou conflito.
Consequentemente, não há screenshot desta rodada documental.

## Melhorias de design

Nenhuma mudança visual foi feita. A decisão foi preservar a interface existente em vez
de produzir uma alteração não compilada. A retomada deve entregar o visual premium e
responsivo solicitado, com status que não dependam apenas de cor, seletores abastecidos
por dados reais e confirmações em modal próprio, sem `alert()`, `confirm()` ou
`href="#"`.

## Scripts criados

Nenhum script SQL ou de aplicação foi criado. Em particular, nenhuma tabela de alerta,
histórico de sugestão/substituição, justificativa ou auditoria foi inventada sem antes
confirmar o modelo real e sem a possibilidade de validar idempotência e integração.

## Testes adicionados

Nenhum teste foi adicionado, pois isso constituiria implementação após o bloqueio
inicial explícito. Os cenários de plantão coberto/descoberto, elegibilidade, conflito,
especialidade, confirmação, substituição, permissão, tenant, DTO Dapper e formulário
sem ID manual continuam pendentes.

## Comandos executados e resultados

### Validação inicial

| Comando | Resultado |
|---|---|
| `git status --short` | sucesso; árvore inicialmente limpa |
| `git branch --show-current` | sucesso; branch inicial `work` |
| `git remote -v` | sucesso; confirmou inicialmente que não havia remoto configurado |
| `git remote add origin https://github.com/devmnsoft/plantaopro.git` | sucesso; remoto configurado localmente |
| `git fetch origin main` | bloqueado; proxy recusou o túnel HTTPS com HTTP 403 (exit code 128) |
| `dotnet --info \|\| true` | bloqueado; `dotnet: command not found` |
| `dotnet restore backend/PlantaoPro.sln` | bloqueado; exit code 127 |
| `dotnet build backend/PlantaoPro.sln -c Debug --no-restore` | bloqueado; exit code 127 |
| `git switch -c codex/v2109-cobertura-inteligente-escalas-design-operacional` | sucesso |
| `find . -maxdepth 3 -name global.json -o -name '*.sln'` | sucesso; nenhuma fixação por `global.json` |
| `rg -n '<TargetFramework\|<TargetFrameworks\|RollForward\|LangVersion' backend --glob '*.csproj' --glob 'Directory.Build.*' --glob 'global.json'` | sucesso; confirmou `net10.0` e C# 10 |

### Validações finais

Os comandos dependentes do SDK foram executados e permanecem bloqueados pela ausência
de `dotnet`; seus resultados não representam aprovação de build ou testes.

| Comando | Resultado |
|---|---|
| `dotnet clean backend/PlantaoPro.sln` | bloqueado; exit code 127 |
| `dotnet restore backend/PlantaoPro.sln` | bloqueado; exit code 127 |
| `dotnet build backend/PlantaoPro.sln -c Debug --no-restore` | bloqueado; exit code 127 |
| `dotnet build backend/PlantaoPro.sln -c Release --no-restore` | bloqueado; exit code 127 |
| `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build` | bloqueado; exit code 127 |
| `python3 scripts/repository-security-check.py` | sucesso; `repository-security ok` (exit code 0) |
| `python3 scripts/check-csharp10-compatibility.py` | sucesso; compatibilidade C# 10 e CSS Razor validada (exit code 0) |
| `python3 scripts/validate-scrpt-completo.py` | sucesso; cobertura de 100% (exit code 0) |
| busca obrigatória de padrões proibidos com `rg` | executada; retornou correspondências preexistentes (exit code 0) |
| `git diff --check` | sucesso; exit code 0 |

## Limitações reais restantes

1. Liberar acesso HTTPS ao remoto `origin`, buscar o GitHub e atualizar a `main` de verdade.
2. Disponibilizar o SDK oficial .NET 10 e obter restore e build Debug iniciais verdes.
3. Inventariar o modelo, os serviços e as telas existentes antes de evoluí-los, sem
   duplicar módulos já entregues.
4. Implementar o Mapa de Cobertura com período e filtros, dados reais, estados completos
   e responsividade entre 360 px e desktop grande.
5. Implementar riscos somente para regras suportadas pelo banco e documentar as demais.
6. Implementar elegibilidade, recomendação e substituição com SQL parametrizado,
   isolamento por `tenant_id`, permissões, auditoria e seletores reais sem IDs manuais.
7. Criar scripts idempotentes apenas se o modelo confirmado exigir novas estruturas.
8. Adicionar e executar os testes obrigatórios, incluindo DTOs Dapper, RBAC e isolamento
   entre tenants.
9. Executar validação visual desktop/mobile, drawer, substituição, vazio e erro, e
   produzir screenshots representativas.
10. Obter builds Debug e Release verdes e executar toda a suíte de testes antes de
    considerar qualquer critério funcional aceito.
