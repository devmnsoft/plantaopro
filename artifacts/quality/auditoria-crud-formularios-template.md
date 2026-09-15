# Auditoria de CRUD, formulários e template — evolução incremental

- **Repositório:** `devmnsoft/plantaopro`
- **Commit inicial:** `69b55ce60dea54ff8824dd3ee700792fcc6a58dc`
- **Data:** 2026-09-15
- **Escopo desta entrega:** inventário estático completo dos formulários Razor detectáveis e auditoria aprofundada da jornada de perfis de acesso.

> Este relatório não afirma ausência de bugs. “Aprovado” só é usado quando houve execução. O ambiente desta sessão não possui o SDK .NET nem Docker/PostgreSQL; portanto a persistência e o navegador não foram executados localmente. Os gates de CI existentes permanecem como caminho de execução real.

## Método, base e limitações

Foram lidos o histórico recente, `.github/workflows/dotnet-ci.yml`, `README.md`, documentação de arquitetura/homologação e o fluxo compartilhado `wwwroot/js/form-experience.js`. Não havia `AGENTS.md` no repositório nem em seus diretórios ascendentes. O inventário pesquisou `*.cshtml` por `<form`/`Html.BeginForm` e JavaScript por formulários criados em template/DOM. O resultado é **99 arquivos com 125 ocorrências de formulário**. Modais e ações de linha embutidos contam dentro do arquivo hospedeiro.

A classificação automática “mutação/ação” é conservadora: `asp-action` pode existir também em uma consulta. Ela localiza a superfície, mas não equivale a aprovação funcional. Uploads foram marcados separadamente. Formulários construídos indiretamente por bibliotecas/componentes sem literal de formulário continuam como risco residual.

## Cobertura comprovada

| Métrica | Quantidade |
|---|---:|
| Arquivos inventariados com formulário | 99 |
| Ocorrências de formulário | 125 |
| Jornadas rastreadas em profundidade nesta evolução | 1 (perfis) |
| Jornadas executadas contra PostgreSQL nesta sessão | 0 |
| Cobertura aprofundada por arquivo | 1/99 (1,01%) |
| Arquivos pendentes de execução individual | 98 |

## Matriz rastreável da jornada auditada

| módulo | tela/formulário | operação | endpoint Web | cliente HTTP | endpoint API | serviço | persistência | permissão | regra | teste/evidência | resultado |
|---|---|---|---|---|---|---|---|---|---|---|---|
| A — Perfis | `Views/Perfis/Index.cshtml` | listar/consultar | `GET /Perfis` | `ReadApiListResponseAsync` | `GET /api/perfis` | `SelfServiceSaasService.ListarPerfisAsync` | `SELECT` explícito em `plantaopro.perfis`, escopo tenant/global | autenticado; tela limitada a administradores | perfil global/base pode ser visível, sem se tornar editável | inspeção de rota, DTO e SQL | não executado (SDK/DB indisponíveis) |
| A — Perfis | `Views/Perfis/Form.cshtml` | cadastrar | `POST /Perfis/Create` + antiforgery | `SendApiAsync` | `POST /api/perfis` | `SalvarPerfilAsync` | transação; advisory lock; `INSERT plantaopro.perfis`; auditoria após commit | **corrigido:** API agora exige administrador global/administrador/administrador do cliente | valida tamanho, normaliza código, rejeita duplicidade no tenant com 409 | `PerfilCrudAuditTests`; build pendente | corrigido; reteste de persistência pendente |
| A — Perfis | `Views/Perfis/Form.cshtml` | editar | `POST /Perfis/Edit/{id}` + antiforgery | `SendApiAsync(PUT)` | `PUT /api/perfis/{id}` | `SalvarPerfilAsync` | `UPDATE` tenant-scoped, somente nome/descrição; exige 1 linha; autoria/data | **corrigido:** papel administrativo revalidado na API | perfil base, fora do tenant ou inexistente retorna 404; payload não altera tenant/status/base | `PerfilCrudAuditTests`; build pendente | corrigido; reteste de persistência pendente |
| A — Perfis | `Views/Perfis/Index.cshtml` | inativar | `POST /Perfis/Inativar/{id}` + antiforgery | `SendApiAsync` | `POST /api/perfis/{id}/inativar` | comando Dapper no controller API | soft delete (`status=INATIVO`, `reg_status=I`), zero linhas = 404 | **corrigido:** papel administrativo na API | perfil base protegido; confirmação agora informa nome e consequência; histórico preservado | teste de autorização por reflexão; persistência pendente | corrigido parcialmente; auditoria/autoria da inativação ainda pendente |
| A — Perfis | `Views/Perfis/Permissoes.cshtml` | transição de permissões | `POST /Perfis/Permissoes/{id}` + antiforgery | `SendApiAsync` | `POST /api/perfis/{id}/permissoes` | `AtualizarPermissoesPerfilAsync` | transação em `perfil_permissoes`; valida módulo contratado; auditoria | **corrigido:** papel administrativo na API | perfil customizado do tenant; não concede módulo não contratado | teste de autorização por reflexão; execução pendente | corrigido; reteste de persistência pendente |

### Cadeia do cadastro/edição de perfil

`Views/Perfis/Form.cshtml` → `PlantaoPro.Web.Controllers.PerfisController` → cliente HTTP autenticado de `BaseWebController` → `PlantaoPro.Api.Controllers.PerfisController` → `SelfServiceSaasService.SalvarPerfilAsync` → Dapper/Npgsql → `plantaopro.perfis` → `IAuditService` → redirect-after-post para permissões → nova consulta da API.

## Defeitos reproduzidos por inspeção e correções

1. **Autorização insuficiente na API:** a classe exigia apenas usuário autenticado; chamadas diretas de criação, edição, inativação e permissões não repetiam o papel administrativo já exigido no MVC. Corrigido em cada mutação, preservando leitura autenticada.
2. **Falso sucesso no UPDATE:** o retorno era sucesso mesmo com zero linhas, inclusive ID inexistente, perfil de outro tenant ou perfil já inativo. O comando agora verifica linhas afetadas e retorna 404 sem auditar sucesso.
3. **Escopo incompleto na proteção de perfil base:** a consulta anterior ao update não filtrava tenant. Substituída por um único UPDATE tenant-scoped e protegido contra `base_sistema`, reduzindo TOCTOU.
4. **Duplicidade por clique concorrente:** cadastro fazia INSERT sem serialização própria da chave lógica. A transação agora usa advisory lock por tenant, verifica nome/código ativos dentro do mesmo lock e retorna conflito 409.
5. **Validação divergente Web/API:** API só exigia nome não vazio. Agora replica limites relevantes de nome, descrição e formato do código, sem aceitar propriedades extras para tenant, autoria, status ou perfil base.
6. **Experiência inconsistente:** formulário de perfil não ativava foco no erro, aviso de mudanças nem bloqueio de duplo submit do script compartilhado. O template agora usa esses comportamentos e ajuda “Como usar esta página”.
7. **Confirmação genérica:** inativação não identificava o perfil. O diálogo agora informa nome, consequência e preservação histórica.

## Blocos funcionais e situação

- **A. Login, contexto, usuários, vínculos e perfis:** perfis auditados e corrigidos; login/contexto/usuários/vínculos inventariados, sem nova execução nesta sessão.
- **B. Organizações, unidades, profissionais e auxiliares:** inventariado; não executado.
- **C. Escalas, plantões, atribuições e confirmações:** inventariado; não executado.
- **D. Presenças, correções e conferência:** inventariado; não executado.
- **E. Financeiro e contratação modular:** inventariado; não executado.
- **F. Demais formulários:** inventariado; não executado.

Nenhum formulário pendente foi aprovado por semelhança. Operações de domínio continuam sendo inativação, cancelamento, ajuste ou estorno quando já definidas; esta entrega não introduz DELETE genérico.

## Evidência e plano de reteste

Os testes adicionados verificam por reflexão que todas as mutações de perfil exigem papéis administrativos e cobrem o contrato da experiência do formulário e as guardas de persistência. Eles são regressões úteis, mas **não substituem** o teste PostgreSQL/E2E solicitado.

Com SDK, PostgreSQL sintético e navegador disponíveis, executar:

1. `dotnet restore backend/PlantaoPro.sln`;
2. builds Debug e Release e `dotnet test`;
3. instalar `database/scrpt_completo.sql` em banco descartável;
4. login real como administrador do cliente e criar perfil; confirmar via conexão SQL independente e detalhe;
5. reenviar simultaneamente duas criações iguais; esperar um sucesso e um 409, com um único registro;
6. editar e recarregar; adulterar `tenantId`, `status` e `baseSistema` no payload e confirmar que não mudam;
7. repetir com outro tenant, perfil não administrativo e ID inexistente;
8. configurar permissões contratadas/não contratadas e inativar; confirmar histórico, sessões/vínculos e ausência em novos seletores;
9. capturar screenshots em celular, tablet e desktop.

**Screenshots:** não produzidos, pois a aplicação real não pôde ser compilada/iniciada sem SDK. Não foi criada imagem simulada.

## Inventário completo de arquivos com formulário

| módulo/tela | arquivo | ocorrências | natureza detectada | resultado |
|---|---|---:|---|---|
| Account | `backend/PlantaoPro.Web/Views/Account/ForgotPassword.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Account | `backend/PlantaoPro.Web/Views/Account/Login.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Account | `backend/PlantaoPro.Web/Views/Account/ResetPassword.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Agenda | `backend/PlantaoPro.Web/Views/Agenda/Index.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Agendamentos | `backend/PlantaoPro.Web/Views/Agendamentos/AgendaPremium.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Agendamentos | `backend/PlantaoPro.Web/Views/Agendamentos/_Form.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Ajuda | `backend/PlantaoPro.Web/Views/Ajuda/Artigo.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Ajuda | `backend/PlantaoPro.Web/Views/Ajuda/Busca.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Ajuda | `backend/PlantaoPro.Web/Views/Ajuda/Index.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Assinaturas | `backend/PlantaoPro.Web/Views/Assinaturas/AlterarPlano.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Assinaturas | `backend/PlantaoPro.Web/Views/Assinaturas/Create.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Assinaturas | `backend/PlantaoPro.Web/Views/Assinaturas/Details.cshtml` | 3 | mutação/ação | não executado — requer jornada dedicada |
| Assinaturas | `backend/PlantaoPro.Web/Views/Assinaturas/Edit.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Assinaturas | `backend/PlantaoPro.Web/Views/Assinaturas/Index.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Auditoria | `backend/PlantaoPro.Web/Views/Auditoria/Index.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| B2BLaunch | `backend/PlantaoPro.Web/Views/B2BLaunch/Form.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Cadastro | `backend/PlantaoPro.Web/Views/Cadastro/Cadastro.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| CentralAtendimento | `backend/PlantaoPro.Web/Views/CentralAtendimento/Index.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Clientes | `backend/PlantaoPro.Web/Views/Clientes/Index.cshtml` | 2 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Comercial | `backend/PlantaoPro.Web/Views/Comercial/Leads.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| CommandCenter | `backend/PlantaoPro.Web/Views/CommandCenter/Index.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| CommercialDemoWeb | `backend/PlantaoPro.Web/Views/CommercialDemoWeb/Contato.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| CommercialDemoWeb | `backend/PlantaoPro.Web/Views/CommercialDemoWeb/Simulador.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Comunicacao | `backend/PlantaoPro.Web/Views/Comunicacao/Details.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Comunicacao | `backend/PlantaoPro.Web/Views/Comunicacao/Index.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Comunicacao | `backend/PlantaoPro.Web/Views/Comunicacao/NovaConversa.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| ConferenciaExecucao | `backend/PlantaoPro.Web/Views/ConferenciaExecucao/Index.cshtml` | 2 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Consultas | `backend/PlantaoPro.Web/Views/Consultas/Atendimento.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Convites | `backend/PlantaoPro.Web/Views/Convites/Index.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| CustomerSuccess | `backend/PlantaoPro.Web/Views/CustomerSuccess/Details.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Escalas | `backend/PlantaoPro.Web/Views/Escalas/Details.cshtml` | 3 | mutação/ação | não executado — requer jornada dedicada |
| Escalas | `backend/PlantaoPro.Web/Views/Escalas/Index.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Escalas | `backend/PlantaoPro.Web/Views/Escalas/Substituir.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Especialidades | `backend/PlantaoPro.Web/Views/Especialidades/Index.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Especialidades | `backend/PlantaoPro.Web/Views/Especialidades/_EspecialidadeForm.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| FaturamentoClinico | `backend/PlantaoPro.Web/Views/FaturamentoClinico/Index.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| FaturamentoSaas | `backend/PlantaoPro.Web/Views/FaturamentoSaas/Details.cshtml` | 5 | mutação/ação | não executado — requer jornada dedicada |
| FaturamentoSaas | `backend/PlantaoPro.Web/Views/FaturamentoSaas/GerarMensal.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| FaturamentoSaas | `backend/PlantaoPro.Web/Views/FaturamentoSaas/Index.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Financeiro | `backend/PlantaoPro.Web/Views/Financeiro/Details.cshtml` | 3 | mutação/ação | não executado — requer jornada dedicada |
| Financeiro | `backend/PlantaoPro.Web/Views/Financeiro/Index.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Home | `backend/PlantaoPro.Web/Views/Home/Dashboard.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Hospitais | `backend/PlantaoPro.Web/Views/Hospitais/Index.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Hospitais | `backend/PlantaoPro.Web/Views/Hospitais/_HospitalForm.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| HospitalArea | `backend/PlantaoPro.Web/Views/HospitalArea/NovaSolicitacao.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Inteligencia | `backend/PlantaoPro.Web/Views/Inteligencia/Dashboard.cshtml` | 1 | consulta/filtro | não executado — requer jornada dedicada |
| JornadaClientes | `backend/PlantaoPro.Web/Views/JornadaClientes/Details.cshtml` | 5 | mutação/ação | não executado — requer jornada dedicada |
| Lgpd | `backend/PlantaoPro.Web/Views/Lgpd/Consentimentos.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Lgpd | `backend/PlantaoPro.Web/Views/Lgpd/ExportarDados.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Lgpd | `backend/PlantaoPro.Web/Views/Lgpd/Solicitacoes.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Medicos | `backend/PlantaoPro.Web/Views/Medicos/Index.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Medicos | `backend/PlantaoPro.Web/Views/Medicos/_MedicoForm.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| MinhaAgenda | `backend/PlantaoPro.Web/Views/MinhaAgenda/PlantoesDisponiveis.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| MinhaAgenda | `backend/PlantaoPro.Web/Views/MinhaAgenda/Presencas.cshtml` | 2 | mutação/ação | não executado — requer jornada dedicada |
| MinhaAssinatura | `backend/PlantaoPro.Web/Views/MinhaAssinatura/Cancelamento.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| MinhaCentral | `backend/PlantaoPro.Web/Views/MinhaCentral/Index.cshtml` | 1 | a classificar | não executado — requer jornada dedicada |
| MinhaCentral | `backend/PlantaoPro.Web/Views/MinhaCentral/_WorkItemDrawer.cshtml` | 1 | a classificar | não executado — requer jornada dedicada |
| Modulos | `backend/PlantaoPro.Web/Views/Modulos/Form.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Modulos | `backend/PlantaoPro.Web/Views/Modulos/Tenant.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Notificacoes | `backend/PlantaoPro.Web/Views/Notificacoes/Index.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Notificacoes | `backend/PlantaoPro.Web/Views/Notificacoes/Preferencias.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Onboarding | `backend/PlantaoPro.Web/Views/Onboarding/NovoCliente.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| OperacaoAssistida | `backend/PlantaoPro.Web/Views/OperacaoAssistida/Checklist.cshtml` | 2 | mutação/ação | não executado — requer jornada dedicada |
| OperacaoAssistida | `backend/PlantaoPro.Web/Views/OperacaoAssistida/Index.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| OperacaoAssistida | `backend/PlantaoPro.Web/Views/OperacaoAssistida/Ocorrencias.cshtml` | 3 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| OperacaoAssistida | `backend/PlantaoPro.Web/Views/OperacaoAssistida/Treinamentos.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| OperacaoPremium | `backend/PlantaoPro.Web/Views/OperacaoPremium/Fechamentos.cshtml` | 2 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Pacientes | `backend/PlantaoPro.Web/Views/Pacientes/_Form.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Pagamentos | `backend/PlantaoPro.Web/Views/Pagamentos/Index.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Pendencias | `backend/PlantaoPro.Web/Views/Pendencias/Index.cshtml` | 2 | consulta/filtro | não executado — requer jornada dedicada |
| Perfis | `backend/PlantaoPro.Web/Views/Perfis/Create.cshtml` | 1 | a classificar | não executado — requer jornada dedicada |
| Perfis | `backend/PlantaoPro.Web/Views/Perfis/Edit.cshtml` | 1 | a classificar | não executado — requer jornada dedicada |
| Perfis | `backend/PlantaoPro.Web/Views/Perfis/Form.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Perfis | `backend/PlantaoPro.Web/Views/Perfis/Index.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Perfis | `backend/PlantaoPro.Web/Views/Perfis/Permissoes.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Permissoes | `backend/PlantaoPro.Web/Views/Permissoes/Matriz.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Permissoes | `backend/PlantaoPro.Web/Views/Permissoes/TestarAcesso.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Piloto | `backend/PlantaoPro.Web/Views/Piloto/Checklist.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Piloto | `backend/PlantaoPro.Web/Views/Piloto/Ocorrencias.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Planos | `backend/PlantaoPro.Web/Views/Planos/Create.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Planos | `backend/PlantaoPro.Web/Views/Planos/Edit.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Planos | `backend/PlantaoPro.Web/Views/Planos/Index.cshtml` | 2 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Plantoes | `backend/PlantaoPro.Web/Views/Plantoes/Details.cshtml` | 2 | mutação/ação | não executado — requer jornada dedicada |
| Plantoes | `backend/PlantaoPro.Web/Views/Plantoes/Index.cshtml` | 1 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Plantoes | `backend/PlantaoPro.Web/Views/Plantoes/_PlantaoForm.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| PropostasComerciais | `backend/PlantaoPro.Web/Views/PropostasComerciais/PropostaForm.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Saude360 | `backend/PlantaoPro.Web/Views/Saude360/Formulario.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Shared | `backend/PlantaoPro.Web/Views/Shared/_CommandPalette.cshtml` | 1 | a classificar | não executado — requer jornada dedicada |
| Usuario | `backend/PlantaoPro.Web/Views/Usuario/Admin.cshtml` | 2 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| Usuario | `backend/PlantaoPro.Web/Views/Usuario/AlterarSenha.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Usuario | `backend/PlantaoPro.Web/Views/Usuario/Edit.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Usuarios | `backend/PlantaoPro.Web/Views/Usuarios/Form.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| Usuarios | `backend/PlantaoPro.Web/Views/Usuarios/Index.cshtml` | 2 | mutação/ação, consulta/filtro | não executado — requer jornada dedicada |
| V114 | `backend/PlantaoPro.Web/Views/V114/Form.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| WhiteLabel | `backend/PlantaoPro.Web/Views/WhiteLabel/Assets.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| WhiteLabel | `backend/PlantaoPro.Web/Views/WhiteLabel/Edit.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| WhiteLabel | `backend/PlantaoPro.Web/Views/WhiteLabel/Emails.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| WhiteLabel | `backend/PlantaoPro.Web/Views/WhiteLabel/Index.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
| WhiteLabel | `backend/PlantaoPro.Web/Views/WhiteLabel/Preview.cshtml` | 1 | mutação/ação | não executado — requer jornada dedicada |
## Pendências e ambiguidades

- A inativação de perfil preserva o registro, mas ainda deve registrar `updated_by`, evento de auditoria e decidir explicitamente o efeito imediato sobre sessões já abertas; não foi inventada uma regra sem confirmação do produto.
- A unicidade lógica está serializada pelo serviço auditado, porém outros endpoints legados de perfil continuam existindo. Consolidar os dois conjuntos de endpoints/serviços é pendência para evitar regras divergentes.
- Os 98 arquivos fora da jornada aprofundada precisam de rastreamento individual controller/serviço/SQL e execução real; este inventário não os declara funcionais.
- Filtros e formulários JavaScript sem literal `<form>` podem exigir instrumentação do DOM em E2E para cobertura absoluta.
