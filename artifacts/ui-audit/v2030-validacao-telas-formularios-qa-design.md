# v2.03.0 — Validação de telas, formulários e QA visual

Data: 2026-08-25  
Escopo: continuidade da v2.02.0, sem refazer dashboard, calendário ou visualizações operacionais.

## Método e cobertura

A rodada combinou revisão estática das views, ViewModels e infraestrutura compartilhada de formulários com os verificadores automatizados do repositório. Foram auditados os fluxos principais abaixo, conferindo layout compartilhado, labels, feedback, estado vazio/loading, ações, filtros, permissões, teclado e comportamento responsivo já coberto pelo design system.

| Área | Rotas/views revisadas | Resultado |
|---|---|---|
| Login e recuperação | `Account/Login`, `ForgotPassword`, `ResetPassword`, `AccessDenied` | Labels e ajuda visíveis, resumo acessível, foco de erro e submit protegido já presentes. Limites server-side reforçados no login. |
| Dashboard | `Dashboard`, `SaasDashboard` | Apenas regressão estrutural; visualizações da v2.02.0 não foram alteradas. |
| Escalas e plantões | `Escalas`, `Plantoes` | Tabelas, filtros, empty states, ações por permissão e formulário de plantão revisados; período, contexto, tipo, vagas e valor têm validação server-side. |
| Profissionais | `Medicos`, área profissional (`MeuDia`, `MinhaAgenda`, `MinhaCentral`) | Cadastro recebeu contrato mais estrito para CPF, CRM, nome e status; resumo/foco de erros e alterações não salvas padronizados. |
| Unidades | `Hospitais` e seletores de unidade | Cadastro recebeu limites, formato de CNPJ e status permitido; feedback acessível padronizado. |
| Usuários e permissões | `Usuarios`, `Usuario`, `Seguranca` | Revisão de labels, feedback e ações condicionadas ao acesso, sem alteração do RBAC existente. |
| Planos e assinaturas | `Planos`, `Assinaturas`, `MinhaAssinatura` | Formulário de assinatura reformulado; tenant/plano não vazios, datas, vencimento, valor e observações validados no servidor e no cliente. |
| White label | fluxos cobertos por `SelfServiceWhiteLabelContractTests` | Revisão estática de estados e mensagens; sem regressão do contrato existente. |
| Relatórios | `Relatorios`, `Bi` | Filtros, resultados vazios, exportações e restrição visual revisados estaticamente. |
| Configurações e auditoria | `Configuracoes`, `Auditoria` | Hierarquia, filtros, tabelas responsivas, status textual e acesso revisados. |

## Correções aplicadas

1. **Assinaturas:** adicionadas validações server-side de cliente e plano não vazios, vigência coerente, dia 1–31, valor permitido e limite de observações. A view agora exibe resumo de todos os erros, mensagens por campo, texto de ajuda, limites HTML equivalentes, foco no primeiro inválido, aviso de alterações não salvas e bloqueio visual contra duplo envio.
2. **Profissionais:** CPF, CRM, nome e status passaram a rejeitar formatos, comprimentos e valores fora do contrato. O formulário ganhou o padrão `pp-form`, resumo completo acessível, foco no primeiro inválido e dirty state.
3. **Unidades:** razão social/nome fantasia, CNPJ e status passaram a ter contrato server-side explícito. O formulário recebeu o mesmo padrão acessível de erro e foco.
4. **Login:** limites explícitos para e-mail e senha evitam payloads excessivos no servidor, mantendo mensagens humanas.
5. **Regressão:** testes de unidade cobrem assinatura inválida e identificadores/status inválidos dos cadastros centrais.

## UX, acessibilidade e responsividade

- O resumo de erro usa `role="alert"`, `aria-live="assertive"` e é focável; mensagens de campo permanecem associadas automaticamente por `form-experience.js`.
- Os formulários alterados possuem labels persistentes; placeholders somente dão exemplo/contexto e não substituem labels.
- Datas recebem validação cruzada no navegador e no servidor; botões de envio anunciam busy/loading e bloqueiam novo submit.
- Grids Bootstrap mantêm empilhamento em 360/390 px e distribuição em 768/1024/1366/1440/1920 px; rodapés usam wrap e os controles preservam alvos adequados.
- Não foram introduzidos `alert()`, `confirm()`, `href="#"`, menu ou logo duplicados.

## Problemas encontrados

- O formulário de assinatura era visualmente cru, não mostrava mensagens por campo e confiava em tipos primitivos sem validação de escopo, período ou limites.
- Cadastros de médico e hospital exibiam somente erros de modelo no resumo e não ativavam foco inicial/dirty state.
- CPF/CNPJ e status de cadastro podiam chegar ao controller em formatos inválidos.
- Login não possuía limite server-side explícito de tamanho.

## Comandos executados

- `python3 scripts/repository-security-check.py` — aprovado.
- `python3 scripts/check-csharp10-compatibility.py` — aprovado.
- `python3 scripts/validate-scrpt-completo.py` — aprovado, cobertura 100%.
- `python3 scripts/check-form-experience.py` — aprovado.
- `python3 scripts/check-feedback-ui.py` — aprovado.
- `git diff --check` — aprovado.
- `dotnet restore backend/PlantaoPro.sln` — não executável: SDK `dotnet` ausente no container.
- `dotnet build backend/PlantaoPro.sln -c Release --no-restore` — não executável pela mesma limitação.
- `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build` — não executável pela mesma limitação.
- `rg -n "href=\"#\"|alert\\(|confirm\\(|Password=123456|Username=postgres;Password=|CHANGE_ME_WITH_32|Host=.*Password=|Server=.*Password=" backend/PlantaoPro.Api backend/PlantaoPro.Web backend/PlantaoPro.Tests scripts docs README.md .env.example` — executado; retornou referências documentais/fixtures e strings maliciosas deliberadas de testes, não uso de APIs nativas nas views. Também apontou duas credenciais locais de fixture PostgreSQL já aceitas pelo verificador de segurança.

## Pendências e limitações conhecidas

- O container não contém o SDK .NET; restore/build/test devem ser repetidos no CI ou em ambiente com o SDK definido pelo projeto.
- Sem runtime ASP.NET disponível não foi possível produzir screenshots navegadas nesta rodada. A validação de breakpoints é estrutural/estática e deve ser complementada pelo smoke Playwright autenticado em CI para evidência pixel a pixel.
- Os IDs de cliente e plano em assinatura ainda são campos GUID. A troca por combobox pesquisável depende de endpoint de lookup autorizado e fica registrada como melhoria, sem relaxar a validação atual.
