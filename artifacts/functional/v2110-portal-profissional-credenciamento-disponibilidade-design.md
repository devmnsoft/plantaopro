# v2.11.0 — Portal do Profissional, credenciamento e disponibilidade

Data da execução: 2026-08-27 (UTC).

## Resultado executivo

**Implementação funcional bloqueada antes de qualquer alteração.** O executável
`dotnet` não está instalado nem disponível no `PATH` do contêiner. O restore e o build
Debug iniciais obrigatórios falharam com `dotnet: command not found` (exit code 127).
Conforme a regra explícita desta rodada, nenhuma alteração funcional, visual, de banco
de dados ou de testes foi realizada.

O clone foi entregue sem remoto Git configurado, sem branch local `main` e apenas com a
branch `work`, no commit `a8a9ce6`. O remoto `origin` foi configurado com o endereço
informado na solicitação, mas o proxy do ambiente recusou o acesso ao GitHub com HTTP
403. Assim, não foi possível buscar nem atualizar a `main`. A branch solicitada foi
criada a partir do único estado local disponível, sem afirmar que essa base corresponda
à `main` remota atualizada.

## SDK exigido pelo projeto

Não há `global.json` no repositório. Todos os projetos da solução — API, Web, testes,
Application, Domain, Infrastructure, CrossCutting e ferramentas — usam `net10.0`.
`backend/Directory.Build.props` fixa `LangVersion` em `10.0`. A retomada exige um SDK
.NET 10 compatível; nenhum framework ou nível da linguagem foi alterado para contornar
a limitação do ambiente.

## Funcionalidades implementadas

Nenhuma. Portal, perfil profissional, documentos, especialidades, vínculos,
disponibilidade, preferências, contato, dados bancários/Pix, histórico e painel
administrativo de credenciamento não foram alterados sem restore/build inicial verde.
Nenhuma funcionalidade existente foi removida ou repetida e nenhum mock, dado fake
fixo ou tela apenas visual foi criado.

## Regras de negócio aplicadas

Nenhuma regra nova foi aplicada. Os status de credenciamento, os requisitos de aptidão
para escala, a documentação obrigatória e as transições administrativas continuam
pendentes de implementação e validação sobre o catálogo e os fluxos reais do projeto.
Este relatório não apresenta nenhum desses critérios como atendido.

## Telas criadas ou evoluídas

Nenhuma. A interface existente foi preservada. Sem uma aplicação compilável não seria
seguro alterar nem validar portal desktop/mobile, perfil, documentos, disponibilidade,
painel administrativo, modais, estados vazio/carregando/erro ou acessibilidade. Por
isso, não foi produzida screenshot nesta rodada estritamente documental.

## Scripts SQL criados

Nenhum. Não foram inventados catálogo de documentos, status, histórico,
disponibilidades ou preferências sem confirmar o modelo real e sem poder validar
idempotência, Dapper, PostgreSQL e integração. Nenhum DDL foi adicionado ao runtime.

## Validações adicionadas

Nenhuma validação funcional foi adicionada. Permanecem pendentes formato de CRM e UF,
CPF conforme padrão existente, e-mail, telefone, upload, datas, horários, sobreposição,
motivos administrativos, tenant e permissões.

## Decisões de design e melhorias mobile

A decisão segura foi preservar a experiência atual, em vez de entregar mudanças não
compiladas. A retomada deverá aplicar hierarquia visual premium e calma, responsividade
mobile-first, labels visíveis, badges acessíveis, feedback humano e modais próprios,
sem `confirm()`, `alert()` ou `href="#"`, sempre abastecidos por dados reais.

## Testes adicionados

Nenhum teste foi adicionado, pois isso constituiria alteração funcional após o bloqueio
inicial. Todos os cenários obrigatórios — isolamento do profissional e tenant,
aprovação/reprovação, vencimento, sobreposição, aptidão, suspensão, upload, RBAC e DTOs
Dapper — continuam pendentes.

## Comandos executados e resultados

### Validação inicial e preparação Git

| Comando | Resultado |
|---|---|
| `git status --short` | sucesso; árvore inicialmente limpa |
| `git branch --show-current` | sucesso; branch inicial `work` |
| `git remote -v \|\| true` | sucesso; confirmou ausência inicial de remoto |
| `git checkout main` | bloqueado; branch `main` não existe localmente |
| `git remote add origin https://github.com/devmnsoft/plantaopro.git` | sucesso; remoto configurado localmente |
| `git fetch origin main` | bloqueado; proxy recusou túnel HTTPS com HTTP 403 |
| `git pull --ff-only origin main \|\| true` | bloqueado; o remoto ainda não estava configurado nessa primeira tentativa |
| `git checkout -b codex/v2110-portal-profissional-credenciamento-disponibilidade-design` | sucesso |
| `dotnet --info \|\| true` | bloqueado; `dotnet: command not found` |
| `dotnet restore backend/PlantaoPro.sln` | bloqueado; exit code 127 |
| `dotnet build backend/PlantaoPro.sln -c Debug --no-restore` | bloqueado; exit code 127 |
| `find . -maxdepth 3 -name global.json -print` | sucesso; nenhuma ocorrência |
| `rg -n '<TargetFramework\|<TargetFrameworks\|<LangVersion\|RollForward' backend --glob '*.csproj' --glob 'Directory.Build.*' --glob 'global.json'` | sucesso; confirmou `net10.0` e C# 10 |

### Validações finais

Os comandos dependentes do SDK foram executados e permanecem bloqueados pela ausência
de `dotnet`; isso não representa aprovação de build ou testes.

| Comando | Resultado |
|---|---|
| `dotnet clean backend/PlantaoPro.sln` | bloqueado; exit code 127 |
| `dotnet restore backend/PlantaoPro.sln` | bloqueado; exit code 127 |
| `dotnet build backend/PlantaoPro.sln -c Debug --no-restore` | bloqueado; exit code 127 |
| `dotnet build backend/PlantaoPro.sln -c Release --no-restore` | bloqueado; exit code 127 |
| `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build` | bloqueado; exit code 127 |
| `python3 scripts/repository-security-check.py` | sucesso; `repository-security ok` |
| `python3 scripts/check-csharp10-compatibility.py` | sucesso; compatibilidade C# 10 e CSS Razor validada |
| `python3 scripts/validate-scrpt-completo.py` | sucesso; cobertura de 100% |
| busca obrigatória de padrões proibidos com `rg` | executada; retornou correspondências preexistentes; a expressão ampla também encontra SQL parametrizado, textos e scripts de validação |
| `git diff --check` | sucesso; exit code 0 |

## Conflitos encontrados e resolução

Nenhum conflito de conteúdo ocorreu. Não foi possível executar rebase contra `main`
porque não há `main` local e o acesso ao remoto foi bloqueado pelo proxy. Portanto,
nenhuma resolução de conflito ou exclusão de alteração de usuário foi necessária.

## Limitações reais restantes

1. Liberar acesso HTTPS ao GitHub, buscar o `origin` e atualizar a `main` real.
2. Disponibilizar o SDK oficial .NET 10 e obter restore/build Debug iniciais verdes.
3. Inventariar e reutilizar catálogo documental, especialidades, vínculos, dados
   bancários/Pix e fluxos existentes antes de criar qualquer estrutura.
4. Implementar portal e painel administrativo com dados reais, RBAC, isolamento por
   tenant, SQL explícito e parametrizado, DTOs Dapper materializáveis e logs relevantes.
5. Implementar status, histórico, upload/substituição documental e motivos obrigatórios.
6. Implementar disponibilidade semanal e pontual com validação de períodos e conflitos.
7. Integrar elegibilidade de escala sem enfraquecer regras existentes de carga horária.
8. Adicionar e executar todos os testes obrigatórios e a matriz Debug/Release.
9. Executar validação visual desktop/mobile e produzir screenshots representativas.
10. Fazer rebase, push e abrir o PR somente após um ambiente apto concluir a
    implementação e as validações; este ambiente não disponibiliza ferramenta
    `make_pr` e o proxy impede push e abertura remota do PR.
