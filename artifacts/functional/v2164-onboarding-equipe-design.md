# PlantãoPro v2.16.4 — compilação, onboarding, equipe e design

## Baseline e causa-raiz

- **Commit inicial:** `3092629`, merge da v2.16.3, com árvore de trabalho limpa. A entrega foi preparada na branch `codex/v2164-fix-compilacao-onboarding-equipe-design`.
- A causa primária dos diagnósticos `CS1010`, `CS1003`, `CS1026`, `CS1002`, `CS0742`, `CS0744` e `CS1525` foi confirmada no arquivo real: o alias PostgreSQL `"DependenciasArray"` aparecia dentro de um literal verbatim C# sem duplicar as aspas. O compilador encerrava a string antes do alias e interpretava `from`, `on`, `equals`/demais tokens da consulta como C#.
- A correção é deliberadamente unitária: no literal `@"..."`, o alias passou a ser `""DependenciasArray""`. Colunas, `array_agg`, joins, filtro por tenant, parâmetros, agrupamento e ordenação foram preservados. Não foi usada interpolação nem raw string de C# 11.
- `PlantaoPro.Tests.csproj` já contém `ProjectReference` para a API e não contém referência a DLL/`obj`. Portanto o `CS0006` relatado é consistente com erro em cascata enquanto a API não produz o assembly; nenhuma DLL foi copiada e a referência não foi alterada.

## Matriz funcional verificada

| Funcionalidade | Estado encontrado no código/persistência | Evidência canônica | Lacuna / risco que continua aberto | Teste necessário |
|---|---|---|---|---|
| Contratação modular | Persistente, revisão versionada, snapshot, idempotência e aprovação global | `ModuleContractingService`, controller e migration `390` | Ativação futura requer reconciliador operacional | PostgreSQL: vazio, válido, dois tenants e vigência futura |
| Login | POST MVC com antiforgery; handler chama API; botão possui timeout, `pageshow`, offline e recuperação | `Account/Login.cshtml`, `auth-login.js`, controllers de Account/Auth | Runtime não iniciado neste executor | Credencial válida/inválida, cookie, contexto, redirecionamento, console |
| Contexto/autorização | JWT, sessão revogável, tenant atual e permissões persistidas já existem | `TenantContextService`, middleware e serviços de segurança | Revalidar todas as rotas administrativas manipuladas por payload | Dois clientes, troca de contexto e revogação com sessão aberta |
| Onboarding | Há `tenant_onboarding` e checklist persistente; criação self-service é transacional | `SelfServiceServices.CriarOnboardingAsync`, `OnboardingController` | O endpoint `concluir` ainda aceita marcação manual e o checklist legado exige etapas clínicas; não satisfaz progresso derivado do contrato | Retomada, reconciliação por dados reais, cliente apenas de plantões |
| Organização/unidades | CRUDs e tabelas existentes | controllers/serviços de cliente, hospitais e unidades | Concorrência otimista, fuso e diagnóstico de dependências de inativação não foram comprovados | ETag/versão, unidade de outro tenant, histórico UTC |
| Equipe/perfis | Usuários, vínculos/perfis e matriz de permissões existem | serviços de segurança, usuários e `perfil_permissoes` | Há rotas legadas sobrepostas; proteção concorrente do último admin precisa de teste integrado | papel global manipulado, último admin, bloqueio só do vínculo |
| Convite de funcionário | Não foi encontrado um fluxo canônico completo; `cadastro_cliente_convites` é de cadastro e convites existentes são majoritariamente de plantão | schemas e serviços de convites | Ausentes estados de entrega, aceite comprovado, token de uso único completo e transação de capacidade/vínculo | duplicado, concorrência, expiração, revogação, reenvio, provedor e dois clientes |
| Interface | Login já tem foco, mensagens textuais, toggle correto e loading recuperável; onboarding administrativo ainda é uma lista fixa | views e JavaScript reais | Equipe/onboarding responsivos e filtros no servidor não foram homologados | desktop/mobile/teclado/console e screenshots atuais |

## Correção e testes adicionados

- Foi corrigido somente o escape do alias Dapper/PostgreSQL no catálogo. `ModuleRow.DependenciasArray` continua sendo o destino da materialização e o fallback JSON continua intacto.
- O contrato automatizado v2.16.4 fixa: abertura do literal verbatim; alias com escape compatível com C# 10; presença dos joins, escopo tenant, agrupamento e ordenação; `ProjectReference` da API sem referência binária; e recuperação do formulário de login sem `alert()`/`confirm()`.
- Não houve mudança SQL nesta rodada, portanto não foi criada migration nem regenerado o consolidado.

## Resultados locais

- `dotnet --info` / restore / build: **não executáveis**; `dotnet` não está instalado (`command not found`). Assim, esta entrega não declara a compilação aprovada localmente, embora a causa sintática tenha sido corrigida e protegida por contrato de fonte.
- PostgreSQL, E2E autenticado e screenshots da aplicação real: **não executados**, pois o executor não possui SDK/runtime e não foi fornecido banco descartável. A validade e a materialização da consulta ainda são gates obrigatórios.
- Os checks estáticos Python e `git diff --check` foram executados localmente; seus resultados são registrados no commit/PR.

## Escopo honesto e gates de homologação

A rodada corrige o bloqueio de compilação comprovado e acrescenta proteção contra regressão. A inspeção não encontrou evidência suficiente para afirmar que a jornada completa de convite de funcionário já seja segura; implementar superficialmente um segundo fluxo de convite criaria duplicidade e risco de autorização. Por isso as lacunas acima são explicitamente mantidas como abertas, em vez de apresentar títulos/prompts como funcionalidade entregue.

Antes de homologar v2.16.4, o CI deve executar os builds Debug/Release e xUnit; um PostgreSQL descartável deve validar catálogo vazio/válido e isolamento; e a aplicação real deve comprovar login → organização → convite seguro → aceite pelo destinatário → vínculo/perfil → acesso → revogação com sessão aberta, incluindo concorrência e limites. Desktop/mobile, teclado, mensagens, console e screenshots sem dados pessoais também permanecem obrigatórios. Não há recomendação de merge automático.
