# A1 — Inventário real de funcionalidades visíveis ao cliente (Rodada 5)

> **⚠️ Este corpo é o retrato de `HEAD = 0ad2617`. A seção D (final do arquivo) é o adendo de
> reconfirmação sobre `HEAD = 4f0151b` (R6) e prevalece sobre qualquer linha abaixo quando divergir.**

Fonte: auditoria read-only do app Web (MVC/Razor, BFF) + API operacional em `C:\MNSOFT\plantaopro`,
executada sobre `HEAD = 0ad2617` (fim da Rodada 4). Nenhuma linha de código foi alterada; nada foi
compilado/executado nesta auditoria. Inventário: **~825 ações** em ~125 controladores web. Módulos
mapeados por `SaasRouteGuardFilter.cs` → dicionário `ControllerModules`. Sem homologação real em banco
(somente teste de contrato por conteúdo de arquivo), exceto onde indicado.

**Legenda**: `FV` = FUNCIONAL_VERIFICADA · `ISH` = IMPLEMENTADA_SEM_HOMOLOGACAO · `PAR` = PARCIAL ·
`SA` = SOMENTE_APRESENTACAO · `BI` = BLOQUEADA_INTEGRACAO · `FAKE` = sucesso falso (seção B).

> **Fato transversal crítico**: o app Web não tem rota nem proxy `/api/*` (`Program.cs` L168–170, só a
> rota MVC padrão; o único proxy é `bff/operacao` via `OperationBffController`). Qualquer `fetch('/api/…')`
> feita **no navegador** dentro de uma view é morta — mesmo que o endpoint equivalente exista e funcione
> na API. Padrão funcional usado aqui: controlador Web chama a API **server-side** (BFF) que faz SQL real.

## A) Inventário e classificação

### A.1 — Billing / Assinatura / Faturamento / Comercial / Onboarding / Self-service (exaustivo)

| Funcionalidade | Rota | Módulo | Cat. | Evidência (file:linha) | Observação |
|---|---|---|---|---|---|
| Billing: todas as 7 ações (Index, Assinaturas, AssinaturaDetails, Faturas, CreateFatura, UpgradeDowngrade, Inadimplencia) | `/Billing/*` | BILLING | **SA** | `BillingController.cs` (shell `SaasComercialPage`); view exibe `ApiPath` como `<code>` só (L9, L12–15), sem JS | API real existe mas é apenas leitura/fakes: `SaasComercialOperacaoController.cs` L35–54 (`/assinaturas`, `/faturas`, `/health`, `/erros`, `/endpoints-lentos`, `/auditoria-resumo` com payload fixo) |
| MinhaAssinatura: Index | `/MinhaAssinatura` | ASSINATURA | **ISH** | `MinhaAssinaturaController.cs` L20 (`ReadApiResponseAsync api/minha-assinatura`) | BFF server-side real |
| MinhaAssinatura: Modulos + RevisarModulos + ConfirmarModulos + CancelarSolicitacaoModulo | `/MinhaAssinatura/Modulos*` | ASSINATURA | **ISH** | `ModuleContractingService.cs` CatalogAsync L16–33, ReviewAsync L35–60, ConfirmAsync L62–83 (lock advisory + insert `solicitacoes_modulos`/`solicitacao_modulo_itens`), DecideAsync L94–132 (`tenant_modulos` L128–130) | Fluxo autoatendido de contratação de módulos é **real de ponta a ponta** (`api/portal-cliente/modulos`); contrato coberto em arquivo (`V2163PortalClienteModulosContractTests.cs`) |
| MinhaAssinatura: Faturas | `/MinhaAssinatura/Faturas` | ASSINATURA | **SA**+FAKE | `Views/MinhaAssinatura/Faturas.cshtml` L1 | "Atual \| Aberta \| R$ 899,00" **hardcoded** |
| MinhaAssinatura: Uso | `/MinhaAssinatura/Uso` | ASSINATURA | **SA**+FAKE | `Views/MinhaAssinatura/Uso.cshtml` L1 | Métricas e barras de 45% fixas |
| MinhaAssinatura: Limites | `/MinhaAssinatura/Limites` | ASSINATURA | **SA**+FAKE | `Views/MinhaAssinatura/Limites.cshtml` L6–9 | KPIs hardcoded |
| MinhaAssinatura: Upgrade / Downgrade | `/MinhaAssinatura/Upgrade|Downgrade` | ASSINATURA | **SA**+FAKE | `Upgrade.cshtml` L2; `Downgrade.cshtml` L2 | Botões mortos; Downgrade tem título errado ("Solicitar upgrade"). Endpoints reais de self-service existem sem serem chamados: `SelfServiceSaasController.cs` L389–437 + `SelfServiceServices.cs` L468–548 |
| MinhaAssinatura: Cancelamento | `/MinhaAssinatura/Cancelamento` | ASSINATURA | **SA**+FAKE | `Cancelamento.cshtml` L10 (form `method="post"` **sem action**); `MinhaAssinaturaController.cs` L122–123 (só GET) | Submeter o formulário não chega em handler de escrita |
| Planos (admin): CRUD + Recursos, Comparativo, AlterarStatus | `/Planos/*` | COMERCIAL | **ISH** | `PlanosController.cs` → `SaasCommercialController.cs` L94–221 (CRUD `planos`/`plano_recursos`) | Pipeline real, sem homologação |
| Assinaturas (admin): CRUD, Uso, Details, AlterarPlano, AlterarStatus | `/Assinaturas/*` | ASSINATURA | **ISH** | `AssinaturasController.cs` → `SaasCommercialController.cs` L248–472 (historico, suspender, reativar, cancelar, alterar-plano) | Idem |
| FaturamentoSaas: Index, Details, GerarMensal, Inadimplencia, MarcarPaga, Cancelar, Contestar, ResolverContestacao, Notificar | `/FaturamentoSaas/*` | FATURAMENTO | **ISH** | `FaturamentoSaasController.cs` → `SaasCommercialController.cs` L595+ (ger-mensal; marcar-paga→`pagamentos_saas` L742–748; notificar L802–818) | Teste de contrato existe (`FaturamentoSaasFunctionalContractTests.cs`), mas é assertion sobre conteúdo de arquivo |
| Clientes: Index, Details, Jornada, Inteligencia, AlterarStatus | `/Clientes/*` | COMERCIAL | **ISH** | API `ClientesController.cs` L83–88 (`api/clientes/{id}/situacao`), suspender/reativar/cancelar SQL L70–114 | |
| JornadaClientes: 9 ações (Avancar, Retroceder, RegistrarEvento, CriarTarefa, ConcluirTarefa…) | `/JornadaClientes/*` | JORNADA | **ISH** | `JornadaClienteService` (SaasEvolutionServices.cs L179+): upsert `jornada_cliente` L275, eventos L276/L322, auto-tarefas L277–288, `cliente_alertas` L307–315, concluir tarefa L335–338 | |
| Comercial: Leads, Oportunidades, Propostas, Index, Funil, PrevisaoReceita | `/Comercial/*` | COMERCIAL | **SA** | Views estáticas sem fetch (nenhum JS chamando API) | API é real e transacional (`ComercialController` API → `ComercialSaasService` L350+: leads L379, oportunidades tx L400–402, ganhar/perder L412–414/L426–428, propostas tx L444–456 com desconto>15% exigindo ADMINISTRADOR_GLOBAL, status L460–479) — **só não é exposta pelas views** |
| Comercial: **[P] CriarLead** | `/Comercial/CriarLead` | COMERCIAL | **ISH** | Único POST fiado no BFF real acima | |
| Onboarding: Index | `/Onboarding` | ONBOARDING | **SA** | `OnboardingController.cs` (bare `View()`) | |
| Onboarding: NovoCliente (GET+POST) + Sucesso | `/Onboarding/NovoCliente`, `/Sucesso` | ONBOARDING | **ISH** | `OnboardingService.CriarClienteCompletoAsync` — transação real (OnboardingService.cs L30–332); Sucesso busca resumo (L337) | |
| Onboarding: Pular Etapa (API) | `api/onboarding/…/pular` | ONBOARDING | **PAR**+FAKE | API `OnboardingController.cs` L125–130 | Só grava **audit log**; finta que a etapa foi pulada |
| Public Self-service: Cadastro.Confirmar **[P]** | `/Cadastro/Confirmar` | SELF_SERVICE | **SA**+FAKE | `PublicSelfServiceWebControllers.cs` L63–77; FAKE L75; `Sucesso.cshtml` alega provisionamento | **Nenhum call à API**; o real `FinalizarCadastroAsync` (L216–260+, L561–570+) está desconectado |
| Public Self-service: etapas do cadastro (Empresa, Plano, Usuario, Confirmacao, Sucesso) + PlanosPublicos | `/Cadastro/*`, `/PlanosPublicos/*` | SELF_SERVICE | **SA** | Views estáticas de wizard | Sem persistência |
| Public Self-service: WhiteLabel (5 ações) | `/WhiteLabel/*` | SELF_SERVICE | **SA** | VM vazias em `PublicSelfServiceWebControllers.cs` | |
| Public Self-service: Parametrizacoes (6) | `/Parametrizacoes/*` | SELF_SERVICE | **PAR** | Dicionários hardcoded no GET, mas os PUT persistem `tenant_parametros` (L361–384) | Escrita real, leitura inicial inventada |
| Public Self-service: Perfis (9) | `/Perfis/*` | SELF_SERVICE | **ISH** | BFF real `api/perfis` | |
| Landing/Simulador/CasosUso/Contato/Demo (`CommercialDemoWebController`, 7 ações) | `/Comercial-demo*` | DEMO_COMERCIAL | **SA** | `CommercialDemoWebController.cs`: Simulador = cálculo local L24–33 (sem persistir) | |
| **[P] EnviarContato** | `/…/EnviarContato` | DEMO_COMERCIAL | **SA**+FAKE | `CommercialDemoWebController.cs` L45–61; FAKE L59 | "Solicitação registrada" sem nenhuma chamada de API |
| AdminSaas: Index | `/AdminSaas` | ADMIN_SAAS | **PAR** | BFF real, mas aponta para `AdminSaasApi` sobre `CommercialDemoService.cs` (dicionários estáticos, zero SQL) | Dados morrem no restart |
| AdminSaas: 11 páginas (Clientes, Tenants, Planos, Assinaturas, Propostas, Leads, Parceiros, Billing, Alertas, Implantacoes, Monitoramento) | `/AdminSaas/*` | ADMIN_SAAS | **SA** | `CommercialPageFactory.Build` (estático) | |
| PropostasComerciais: Index GET, Create, Edit | `/PropostasComerciais/*` | COMERCIAL | **SA** | `PropostasComerciaisController.cs` | |
| PropostasComerciais: **Index [P]** | `/PropostasComerciais` POST | COMERCIAL | **SA**+FAKE | `CommercialDemoWebController.cs` L174–182; FAKE L179 | "Proposta salva para demonstração comercial" |
| ClientePortal (11) / ParceiroPortal (8) | `/ClientePortal/*`, `/ParceiroPortal/*` | PORTAIS | **SA** | `Fase2OperationalFlowService.Build` (L41–58) | Empty-state **honesto** (não finge dados) |
| Modulos (admin) — 7 ações | `/Modulos/*` | MODULOS | **ISH** | `ModulosController.cs` L185–280, BFF completo `api/modulos` | |
| FeatureFlags / DemoAdminController ([Route("Demo")]) | `/FeatureFlags`, `/Demo/*` | DEMO | **SA** | Estáticos | |
| B2B Launch: Developer (9, c/ **[P] CreateApiKey**), Contratos (6), SLA (3), Beta (5), Suporte (6), Monitoramento (6), GoToMarket (5), OperacionalLaunch (2) | `/Developer/*` etc. | B2B_LAUNCH | **SA** (CreateApiKey **FAKE**) | `B2BLaunchWebControllers.cs` L8–119; CreateApiKey L18–28 | "A criação efetiva deve ser concluída pela API do Developer Portal" |
| B2B Ops: Executivo (8), WhiteLabelTemplates (6), Treinamento (6), MedicoArea (11), Renovacoes (3), Expansoes (2) | `/Executivo/*` etc. | B2B_OPS | **SA** (AceitarConvite **FAKE**) | `B2BCommercialOpsWebControllers.cs` (35 ações, todas `B2BLaunchPages.Pagina`); `MedicoArea.AceitarConvite` L66–70 | APIs em memória correspondentes existindo mas sem SQL: `B2BCommercialOpsServices.cs` (ConcurrentDictionaries+seed: `api/executivo`, `api/treinamento`, `api/white-label-templates`, `api/piloto`…) |
| Piloto: Index/Checklist/Ocorrencias + ConcluirChecklist + CriarOcorrencia | `/Piloto/*` | PILOTO | **PAR** | API `PilotoController.cs`: Resumo L26–63 mistura contagens reais e memória; Checklist/Ocorrencias L65–101 = dicionários em memória | Escritas perdidas no restart; toast de sucesso na web |
| Piloto: Programas…Indicadores (7) | `/Piloto/*` | PILOTO | **SA** | Estáticos (L163–169) | |

### A.2 — Demais módulos (resumo por módulo)

| Módulo (controlador web) | Ações | Cat. | Evidência (file:linha) |
|---|---|---|---|
| Administrativo360 (`Administrativo360Controller`, `Adm360Cotacoes/DocumentosXml/Fiscal/Gestao`) | 155/156 | **FV** (1 ação BI + 1 PAR abaixo) | CRUD + finanças + estoque real via Dapper; ~18 arquivos de teste com banco (`Administrativo360*Tests`); exceções: `Adm360Cotacoes.TransmitirResposta` = **BI** (L185–200, toast de sucesso L200 mascara `CONFIGURACAO_PENDENTE`; `PortalConnectors.cs`: Opmenexo L6–50 e Inpart L52–106 sempre `Conectado:false`); `Adm360Fiscal.Emitir` = **PAR** (L336, conector externo P1 pendente; interno funciona) |
| Saúde 360 (ClinicaDashboard, PendenciasClinicas, PainelChamada, Agendamentos, Triagem, Consultas, Cid, Prescricoes, ClinicaFinanceiro, Convenios, PlanosSaude, Pacientes, WorkflowSaude360, GestorDashboard, Saude360Web) | 92 | **FV** | BFF uniforme → `Saude360ClinicalControllers.cs` L11–282 + `Saude360SupportControllers.cs` L73 + `WorkflowSaude360Controller` L9 + `ValorExecutivoController` L28; persistência provada em `Saude360ClinicalService.cs` (inserts L401, 406, 447, 469, 617–631: pacientes/agendamentos/triagens/consultas/cid_tabela/prescricoes/convenios/planos_saude); testes reais em DB (`Saude360*Tests`) |
| Governança SaaS B6 | (rodada 4) | **FV** | `SaasGovernancaB6Rodada4Tests.cs` (banco real) |
| Ocorrências (`Ocorrencias` web, 2) | 2 | **FV** | GET `api/ocorrencias/{id}`+`/historico`, POST `/situacao` c/ versão (OcorrenciasController L16–62); `OcorrenciaWorkflowTests` |
| Central de Escala — Index, Plantao, Substituicoes(+Details), DecidirSubstituicao, Convidar | 6 | **ISH** | `OperacaoService.GetResumoAsync` (OperacaoServices.cs L38–82, SQL real); Decidir via `SubstituicoesFase4Controller` L92–155 (otimista `VersaoEsperado`); Convidar `PlantoesController.cs` L228–250 (guard de assinatura + uso registrado) |
| Central de Escala — PlantaoDescoberto, Risco, MedicosDisponiveis, Sugestoes, MedicosSugeridos, ConvitesPendentes, Calendario | 7 | **SA** | Web `CentralEscalaController.cs` L48–52/L98–99/L150 → empty-state honesto de `Fase2OperationalFlowService`; API de sugestões real existe sem ser chamada (`CentralEscalaSugestoesFase4Controller` L77–87) |
| Operação Assistida (10 ações) | 10 | **ISH** | `OperacaoAssistidaController.cs` (API): upsert `operacao_assistida_checklist` L428–430, ocorrências L444–446, treinamentos L459–460, clientes+recálculo L473–477, `alertas_operacionais` em CRÍTICA L485–486; ConcurrentDictionaries L16–18 são cache/override |
| Fechamentos (V1420) | 2 | **ISH** | BFF: list/detail/timeline/6 ações de conferência + export CSV (L20–29) |
| Cobertura (V1420, 4 ações fora do índice regex) + V1420Bff ([Route("bff")]) | 4+2 | **SA** | Empty-state honesto ("Aguardando integração com a fonte operacional", L57); BFF retorna JSON com zeros hardcoded |
| BI (11 ações) | 11 | **SA** | Controlador = bare `View()`; `Views/Bi/Index.cshtml` L19 `fetch('/api/bi/resumo-executivo')` **sem token e sem rota/proxy `/api` no Web** → KPIs ficam "-"; `Financeiro.cshtml` L2 "em evolução". Lado API é real (`BiController` L26–35 c/ gate de plano; painéis L37–56 retornam string "em evolução") |
| Inteligência (1) | 1 | **SA** | Dashboard com dados demo hardcoded (GUIDs fixos L27–28, "Hospital A" L31–33) |
| MeuDia, Pendências, Dashboards Premium, Dashboard, CommandCenter, GlobalSearch, Relatórios (26), SaasDashboard, Configuracoes | 43 | **ISH** | BFFs reais: `ProductivityWebService` L19–29; `DashboardsPremiumController`→API L15–21→`DashboardPremiumService.cs` L40 (SQL confirmado); `api/dashboard`; `api/operacao-inteligente/resumo`; `api/global-search`; `api/saas-dashboard/resumo` |
| Customer Success (web): resumo/saúde/alertas/recomendações/recalcular | 5 | **ISH** | `CustomerSuccessController.cs` L12–58 → `api/saas-inteligencia/*` (não confundir com a in-memory `api/customer-success`) |
| Customer Success (web): Contas, ContaDetails, Riscos, Oportunidades, Nps, Playbooks, PlanosAcao | 7 | **SA** | Páginas `B2BLaunch` estáticas (L60–66); **forms caem no handler do Developer** (ver B-5) |
| Faturamento Clínico: Index, ContasReceber, Recebimentos | 3 | **ISH** | BFF `api/v115/faturamento/contas-receber` (FaturamentoClinicoController L16–36) |
| Faturamento Clínico: Titulos, RepassesMedicos, Glosas, DemoBoleto, Regras, Configuracoes | 6 | **SA** | Shells `Views/V114/Produto.cshtml` (L38–57) |
| Cadastros base: Hospitais, Médicos, Especialidades (7 cada), Usuários, Perfis, Usuario(7) | 34 | **ISH** | BFFs CRUD com tratamento de erro real (ex.: Hospitais L31/L57/L76; Usuarios L20–117) |
| Operação core: Plantões (9/10), Escalas, Financeiro (12), Comunicação (6/7), Minha Agenda (13), Convites, Pagamentos, Agenda(BFF+web), Conferência Execução, Central Atendimento | 62 | **ISH** | BFF→API→SQL com sucesso pós-POST condicionado a resposta (ex.: Plantões L109/L172/L210; Escalas L94; Financeiro L48–129; MinhaAgenda L80–198). Exceções SA: `Plantoes.Calendario` (bare View L39) e `Comunicacao.Templates` (L220) |
| LGPD: 3 escritas (CriarSolicitacao, RegistrarConsentimento, Exportar) | 3 | **ISH** | BFF `api/lgpd/*` (LgpdController L29/L48/L66) |
| LGPD: 8 páginas GET | 8 | **SA** | Bare `View()` (L11–18) |
| Observabilidade: Index | 1 | **ISH** | Busca `api/observabilidade/*` L18–20 |
| Observabilidade: Erros/Performance/Acessos/Eventos | 4 | **SA** | Bare `View()` L34–40 |
| Ajuda (9), Manual (11), PrimeirosPassos, Implantacao, Integracoes (5), Notificacoes (2), HospitalArea (4), Seguranca (12), FaturamentoRegras/Shells V114+V115 (ItensFaturaveis, Jornada, Templates, Favoritos, HistoricoAcoes), V112WebController (10) | 70+ | **SA** | Todos estáticos: bare `View()`, listas hardcoded (`AjudaController` L167+/L180+, `ManualController` L9–19) ou shells que prometem "Dados carregados por APIs" **sem nenhum JS** (`V112WebController` L7–19; `Views/V114/Produto.cshtml`). `Feedback` da Ajuda é FAKE (L128). `Operacoes/Index.cshtml` L4 tem fetch morto igual ao BI |
| Permissoes (matriz estática, 6), Coordenacao, Marketplace, PlanosPublicos | 12 | **SA** | `ProfessionalSaasControllers.cs`: matriz criada em código (L50–85), `SimularPerfil` L43–48 só simula; `CriarMatriz`/`Modulo` sem persistência |
| SaasClientes, SaasTenants, SaasPlanos, SaasUso, OperacaoSaas (shell `SaasComercialPage`) | 19 | **SA** | Mesma família do Billing: `SaasComercialOperacaoWebControllers.cs` L11–14 mostra `ApiPath` como texto, sem chamá-la |

### A.3 — Infraestrutura / não-produto

`Account` (10, **ISH** — autenticação/reset), `Error`, `TestSignin`, `MvcSeguroTest*`, `OperationBff`
(proxy `bff/operacao`), helpers de `BaseWebController` (tratam qualquer 2xx como sucesso — relevante
para avaliação de validação no lado API).

## B) Lista detalhada de no-op / sucesso falso (fake success)

| # | Funcionalidade | O que o usuário vê | O que realmente acontece | Evidência |
|---|---|---|---|---|
| 1 | Contato comercial (landing) | "Solicitação registrada!" | Zero chamada de API; nada é salvo | `CommercialDemoWebController.cs` L45–61, fake em **L59** |
| 2 | Salvar proposta comercial | "Proposta salva para demonstração comercial" | Apenas `TempData["Success"]` | `CommercialDemoWebController.cs` L174–182, fake em **L179** |
| 3 | Cadastro público → Confirmar | Tela de sucesso alegando provisionamento do tenant | POST **não chama a API**; o fluxo real `FinalizarCadastroAsync` nunca é invocado | `PublicSelfServiceWebControllers.cs` L63–77, fake em **L75**; real desconectado L216–260+ e L561–570+ |
| 4 | Criar API Key (Developer) | Toast de sucesso | "A criação efetiva deve ser concluída pela API do Developer Portal" — nada criado | `B2BLaunchWebControllers.cs` (Developer.CreateApiKey) L18–28 |
| 5 | **Qualquer** página de formulário B2B (NPS, Playbooks CS, Planos de Ação, etc.) | Form submete e "salva" | O form é hardcoded para postar em `Developer/CreateApiKey` (o item 4 acima) | `Views/B2BLaunch/Form.cshtml` **L38** (`asp-controller="Developer" asp-action="CreateApiKey"`); afetado por `CustomerSuccessController.cs` L64–66 (Nps/Playbooks/PlanosAcao) |
| 6 | Aceitar convite (Área do Médico B2B) | Botão responde | "Nenhuma alteração foi realizada" | `B2BCommercialOpsWebControllers.cs` (MedicoArea.AceitarConvite) L66–70 |
| 7 | Feedback do artigo de ajuda | "Obrigado! Feedback … registrado" | Só um `logger.LogInformation` | `AjudaController.cs` **L128** |
| 8 | Pular etapa de onboarding | Etapa marcada como pulada | Apenas registro de auditoria; a etapa segue tecnicamente pendente | API `OnboardingController.cs` L125–130 |
| 9 | Minha Assinatura → Faturas | Fatura "Atual \| Aberta \| R$ 899,00" | Valores hardcoded na view | `Views/MinhaAssinatura/Faturas.cshtml` **L1** |
| 10 | Minha Assinatura → Uso | Barras de consumo em 45% | Percentuais hardcoded | `Views/MinhaAssinatura/Uso.cshtml` **L1** |
| 11 | Minha Assinatura → Limites | KPIs de uso dos limites | Hardcoded | `Views/MinhaAssinatura/Limites.cshtml` **L6–9** |
| 12 | Minha Assinatura → Upgrade/Downgrade | Botões de ação | Botões mortos; título de Downgrade diz "Solicitar upgrade". Endpoints reais de upgrade/downgrade existem sem UI ligada | `Upgrade.cshtml` L2; `Downgrade.cshtml` L2; reais: `SelfServiceSaasController.cs` L389–437, `SelfServiceServices.cs` L468–548 |
| 13 | Minha Assinatura → Cancelamento | Formulário "Cancelar assinatura" | `form method="post"` sem action; controller só tem GET | `Cancelamento.cshtml` **L10**; `MinhaAssinaturaController.cs` L122–123 |
| 14 | Piloto: checklist e ocorrências (concluir/criar) | Toast de sucesso | Escrita em `ConcurrentDictionary` — some no restart | API `PilotoController.cs` L65–101 (dicionários); resumo misto L26–63 |
| 15 | Pisos in-memory comerciais (AdminSaas index, Executivo, Treinamento, WhiteLabel templates, PilotoBeta, Customer-success-API, OperacaoAssistida planos, Renovações, Expansões) | Telas com dados e "sucessos" | `Dictionary`/`ConcurrentDictionary` estáticos; **zero insert/update SQL** | `CommercialDemoService.cs`; `B2BCommercialOpsServices.cs` |
| 16 | Cotações → Transmitir Resposta (OPMENEXO/INPART) | Toast de sucesso em transmissão | `Sucesso:false`/`Conectado:false` permanentes; toast de sucesso mascara `CONFIGURACAO_PENDENTE` | `Adm360CotacoesWebController.TransmitirResposta` L185–200 (toast L200); `PortalConnectors.cs` Opmenexo L6–50, Inpart L52–106; testes: `Administrativo360CotacoesTransmissaoB5Tests` |
| 17 | Importação manual de documentos | "EXPORTADA_MANUALMENTE" como sucesso | Conector manual devolve `Sucesso:true` sem transmissão | `PortalConnectors.cs` ImportacaoManualConnector L108+ |
| 18 | Emissão de nota fiscal | Nota "emitida" | Interno OK; **transmissão ao emissor externo pendente (P1)** | `Adm360FiscalWebController.Emitir` L336; `Administrative360R4F1FiscalPreEmissoesTests` |
| 19 | Painel BI (e /Operacoes) | KPIs que deveriam carregar | `fetch('/api/bi/resumo-executivo')` sem token **e sem rota/proxy `/api` no Web** → KPIs eternamente "-" | `Views/Bi/Index.cshtml` **L19**; `Program.cs` **L168–170** (só rota MVC default); mesmo padrão em `Views/Operacoes/Index.cshtml` L4 |
| 20 | Dashboard de Inteligência | Números de operação | Dados demo fixos ("Hospital A", GUIDs constantes) | `InteligenciaController.Dashboard` L27–33 |
| 21 | BFF `/bff` (cobertura/fechamentos) | JSON com métricas | Retorna zeros hardcoded em 7 GETs | `V1420BffController.cs` |
| 22 | Shells `SaasComercialPage` (Billing e 5 controllers SaaS) | Lista de "ações" e endpoint | `ApiPath` exibida como `<code>`; nada é chamado pela página | `SaasComercialOperacaoWebControllers.cs` L11–14; view L9/L12–15; `BillingController` |

Observação: "FUNCIONAL" aqui significa pipeline real Web→API→PostgreSQL confirmado por leitura de código.
Como a convenção predominante de teste é *contract* por conteúdo de arquivo (`File.ReadAllText` +
`Assert.Contains`), `FV` foi marcado apenas onde testes contra banco real foram mapeados
(Administrativo360, Saúde 360, Governança B6, Ocorrências).

## C) Contagens por categoria

| Categoria | Ações classificadas | Observação |
|---|---:|---|
| FUNCIONAL_VERIFICADA (pipeline real + teste em banco real) | **249** | Administrativo360 155 · Saúde 360 92 · Ocorrências 2 |
| IMPLEMENTADA_SEM_HOMOLOGACAO (pipeline real, homologação pendente) | **211** | Assinaturas/Planos/FaturamentoSaas/Clientes/Jornada, Operação core, BFFs administrativos, Meu Dia/Pendências, Relatórios, Auth etc. |
| SOMENTE_APRESENTACAO (view estática / fetch morto / estado honesto vazio) | **≈ 302** | Inclui 22+ flags FAKE da seção B; B2B 78 · Bi 11 · Billing+SaaS-page 26 · Portais 19 · Cadastro/WL/LGPD-GET 20 · Shells V112/V114/V115 25 · Help/Docs 26 · Outros ~105 |
| PARCIAL (funciona em parte / persistência em memória / integração pendente) | **13** | Piloto 5 · Parametrizacoes 6 · AdminSaas-Index 1 · Fiscal.Emitir 1 |
| BLOQUEADA_INTEGRACAO | **1** | Adm360Cotacoes.TransmitirResposta (conectores OPMENEXO/INPART desligados) |
| INDISPONIVEL | **0** | Não há rota que retorne indisponibilidade pura; bloqueios de integração aparecem como BI/PARCIAL acima |
| Infra/testes (fora do escopo de produto) | ~17+22 | Helpers de `BaseWebController`, Error, TestSignin, MvcSeguroTest*, proxy; ~22 = artefatos do índice (rotas duplicadas sobre 1 ação, declarações em linha única) |

**Total classificado ≈ 787/825** (diferença = infra + artefatos de indexação; totais arredondados por
ação, não por rota duplicada).

## Síntese executiva

1. **Onde o cliente de verdade (tenant) pode operar hoje de ponta a ponta**: Saúde 360,
   Administrativo360, plantões/escalas/financeiro/minha agenda/relatórios, jornada, módulos do portal
   cliente, operação assistida e conferências — com persistência SQL real (249 + 211 ações).
2. **Onde a fachada engana**: todo o eixo *assinatura do cliente* (faturas/usos/limites/upgrade/
   cancelamento hardcoded), o *cadastro público* (sucesso sem provisionar), os *contatos/propostas de
   demonstração comercial* e todos os *forms B2B* (que caem no fake de API key).
3. **Risco de dado volátil**: qualquer fluxo que toque `CommercialDemoService`/`B2BCommercialOpsServices`/
   Piloto-checklist perde dados no restart.
4. **Pontos mais baratos de corrigir**: ligar o form de Cancelamento/Upgrade/Downgrade aos endpoints reais
   já existentes em `SelfServiceSaasController`; chamar `FinalizarCadastroAsync` em `Cadastro.Confirmar`;
   corrigir o `asp-action` de `Views/B2BLaunch/Form.cshtml` ou remover o form; adicionar proxy/token para
   os `fetch('/api/…')` de BI e Operações (ou trocar por BFF server-side como no resto do app).


## D) R6 — Reconfirmação da base sobre `HEAD = 4f0151b` (2026-10-09)

Método: as 22 falsas-promessas da seção B e as linhas comerciais/onboarding da seção A foram
**reverificadas uma a uma no código atual** (auditoria read-only segunda passagem) e confrontadas com
as evidências executadas da própria Rodada 5 (`r5b4`, `r5b5`, `r5b6`, `r5c7`, `r5d9`–`r5d12`,
`r5-e13-*`, `r5-p0-ensaio-upgrade-clone.md`) e com a suíte em banco real (1183/1183).
Classificação na legenda exigida pela diretriz R6:

| Legenda R6 | Equivalente antigo | Critério |
|---|---|---|
| **FV** funcional e verificada | FV | pipeline Web→API→SQL + teste em banco real **ou** probe ao vivo registrado em evidência |
| **PAR** parcial | PAR/BI | funciona em parte / persistência volátil / integração externa pendente |
| **BLOQ** bloqueada | BI/SA com dependência externa | depende de credencial/provedor/decisão que não existe no ambiente |
| **FAKE** falhou (fachada) | SA+FAKE | UI apresenta resultado que o sistema não produz — corrigível internamente, sem bloqueio externo |
| **NE** não executada | ISH/FV-partial | pipeline real existe, mas a jornada nunca foi executada/homologada ponta a ponta neste ambiente |

### D.1 — O que a Rodada 5 mudou no inventário (verificado no código atual)

| Linha original | Estado em `4f0151b` | Evidência atual |
|---|---|---|
| Seção B #3 Cadastro público → Confirmar "sucesso sem provisionar" | **FV** — POST chama `api/public/cadastro/finalizar` → provisionamento transacional pelo núcleo B5 `TentarProvisionarAsync` | `PublicSelfServiceWebControllers.cs:172` → `SelfServiceServices.cs:216-299`; ao vivo em `r5b5-provisionamento-convites.md` |
| Seção A.1 "Onboarding: Pular Etapa PAR+FAKE (só audit log)" | **FV** — skip persistido (`status='PULADA'`, `motivo_pular`, 409 se obrigatória); concluir-no-clique só com critério atendido; reavaliar derivado de dados | `OnboardingJornadaService.cs:453-479`; rotas `OnboardingController.cs(API):115-168`; loop completo ao vivo `r5d10-onboarding-loop-ao-vivo.md` |
| Seção B #9–#13 MinhaAssinatura Faturas/Uso/Limites/Upgrade/Downgrade/Cancelamento hardcoded/mortos | **FV** — views model-bound; POSTs reais com antiforgery gravando `upgrade_solicitacoes`/`downgrade_solicitacoes`; leitura real (`uso`, faturas por SQL) | `MinhaAssinaturaController.cs:146-266`; `SelfServiceServices.cs:580-652`; matriz B4 `r5b4-matriz-comercial.md` |
| Seção B #16 TransmitirResposta mascarando sucesso | **PAR/BLOQ honesto** — toast neutro ("Tentativa de transmissão executada"), status real `CONFIGURACAO_PENDENTE` persistido; conectores OPMENEXO/INPART continuam sem credenciais (bloqueio externo legítimo) | `Adm360CotacoesWebController.cs:185-204`; `PortalConnectors.cs:41-49,97-105` |
| Seção B #18 Emissão fiscal "nota emitida" | **FV-interna / BLOQ-externa** — `ENVIANDO` só com `IFiscalTransmissor` real; catálogo DI vazio → recusa honesta 400; falha reverte `ENVIANDO→PRONTA` | `Administrativo360FiscalService.cs:167-224`; `Program.cs:53`; `r5a2-fiscal-honesto.md` |
| Billing (7 ações SA) | **substituído por redirect** para páginas canônicas Assinaturas/FaturamentoSaas | `BillingController.cs` (R5-B6) |
| Eixo comercial admin | API decisória real e testada (`SolicitacoesPlanosController` aprovar/recusar idempotente); **UI Web de decisão ainda inexistente** (zero referências a `solicitacoes-planos` no Web) | `API\Controllers\SolicitacoesPlanosController.cs`; testes `SaasComercialB4MatrizTests` |
| Unidades de atendimento (Saúde 360) | **FV** — rota de escrita + página BFF provadas ao vivo; checklist demo 12/12 AUTOMATICO | `r5-e13-unidades-atendimento.md` |
| Cobrança SaaS | **FV (sandbox)** — máquina de estados + webhook HMAC + dedupe; fatura PAGA ao vivo via PIX_SANDBOX. Provedor externo: **BLOQ** (não existe contrato comercial) | `r5b6-cobranca-sandbox.md` (v2332) |
| Scheduler `ativar-agendados` | **FV** — kernel B4 único, BackgroundService, ator do sistema | `r5-e13-scheduler-agendados.md` |
| Upgrade do banco principal | **ENSAIO FV em clone** (upgrade oficial verde fim-a-fim); banco principal ainda **não atualizado** → Pendência P0 do usuário | `r5-p0-ensaio-upgrade-clone.md` |

### D.2 — Persistem como FAKE (falhou: fachada sobre nada) em `4f0151b`

| # | Item | Prova atual |
|---|---|---|
| 1 | Contato comercial da landing — "Solicitação registrada" sem salvar | `CommercialDemoWebController.cs:45-61` (TempData only) |
| 2 | PropostasComerciais POST "salva para demonstração" | `CommercialDemoWebController.cs:177-179` |
| 14 | Piloto checklist/ocorrências — escrita em `ConcurrentDictionary`, perde no restart | `PilotoController.cs:17-18` |
| 19 | BI `/Bi` e `/Operacoes`: `fetch('/api/…')` sem proxy `/api` no Web → KPIs eternamente "-" | `Views/Bi/Index.cshtml:19`, `Views/Operacoes/Index.cshtml:4`, `Web\Program.cs:168-170` |
| 20 | `/Inteligencia` dashboard com GUIDs/"Hospital A" hardcoded como dados de operação | `InteligenciaController.cs:27-40` |
| 21 | BFF `V1420` (cobertura/fechamentos) retorna zeros estáticos em 7 GETs | `V1420OperationalControllers.cs:33-49` |
| 15/22 | 11 subpáginas AdminSaas + shells `SaasComercialPage` estáticos apesar do caminho canônico em banco existir (`comercial_leads`/`comercial_propostas`; AdminSaas.Index já é real) | `CommercialDemoWebController.cs:112-122`, `Views/Shared/SaasComercialPage.cshtml:9`, `CommercialPageFactory` interno L298-312 |
| (inertes honestos) | Developer.CreateApiKey, MedicoArea.AceitarConvite, feedback da Ajuda, exportação manual de documentos | mensagens honestas mas função não faz o que a UI sugere: `B2BLaunchWebControllers.cs:18-28` (+ `Form.cshtml:38` posts tudo para lá), `B2BCommercialOpsWebControllers.cs:66-69`, `AjudaController.cs:119-130`, `PortalConnectors.cs:127-135` |

### D.3 — Fontes de verdade concorrentes para "módulo contratado/permitido" (raiz do item 2 R6)

Uma única pergunta — "este tenant tem este módulo e este usuário esta ação?" — é respondida hoje por
**11 resoluções paralelas**, apenas uma das quais canônica:

1. **Canônica**: `ModuleContractVigencia`/`ModuleContractingService.ContratadoAsync` (habilitado+status+vigência) — usada em login (`Data.cs:519`), política por request da API (`SecurityAdministrationServices.cs:126`), catálogo do portal (`ModuleContractingService.cs:26`) e onboarding (`OnboardingJornadaService.cs:95`).
2. `Saude360ModuleFilter.cs:47-50` — SQL próprio **ignorando vigência/agendamento e fail-open** em erro de banco.
3. `SelfServiceServices.cs:58` — somente `habilitado=true` (sem status/vigência).
4. `CobrancaSaasServices.cs:54-58` — somente vigência (escopo de faturamento, sem habilitado/status).
5. Web `SaasRouteGuardFilter.ControllerModules` (~70 entradas) + `PermissionActionOverrides` — dicionário de código, sem vínculo com `modulos_sistema`.
6. `_AppSidebar.cshtml:12-14,50-56` e `_SuprimentosNav.cshtml:18-35` — listas hardcoded de módulos/permissões duplicando o catálogo.
7. `FeatureCatalogService.cs:16-52` — terceira cópia dos códigos exigidos (usa família `SAUDE360_*`, divergindo dos códigos finos do guard: `PACIENTES` vs `SAUDE360_PACIENTES`).
8. Strings de papel composto `RolesConstants.Saude360*` — Web e API são **quase-duplicados divergentes** (Web não tem Triagem/Clínico/Cid/Repasses).
9. `AccessServices.cs:128-190` — fallbacks de papel pré-v2149 hardcoded.
10. Guard do módulo no onboarding — 5ª variante SQL + filtro C#.
11. `[RequireModule]` da API lê claims JWT (minted no login) — correto, mas congela mudanças até refresh (JWT 8 h; cookie sliding).

Consequência medida (motivo do item 2): controllers clínicos mapeados no guard a códigos finos
(`PACIENTES`, `AGENDAMENTOS`, `TRIAGEM`, `CONSULTAS`, `CLINICA_DASHBOARD`) nunca recebem claim desses
módulos porque `tenant_modulos` só registra o pacote `SAUDE360` → páginas legítimas caem em
`MODULO_NAO_CONTRATADO` com contrato ativo. O pacote vendido e suas capacidades não têm relação
explícita no catálogo canônico.

### D.4 — Classificação global reconfirmada (por nível R6)

| Nível R6 | Situação em `4f0151b` |
|---|---|
| **Funcional e verificada** | Núcleo Administrativo360 (~155 ações, testes em banco), Saúde 360 clínico (~92), Ocorrências, onboarding-contrato completo (materialização/skip/conclusão/reavaliação ao vivo), provisionamento self-service+admin, convites com hash/expiração, cobrança sandbox, matriz comercial (API + decisão idempotente), MinhaAssinatura real, scheduler AGENDADO, rota de unidades, fiscal interno honesto, escopo tenant_id canonizado (v2336), upgrade DB ensaiado em clone |
| **Parcial** | Fiscal externa (sem credenciais — BLOQ abaixo na transmissão), conectores cotação, Piloto-checklist (volátil), Parametrizacoes (leitura inicial inventada), AdminSaas.Index (misto), white-label via self-service (rota real, homologação visual parcial) |
| **Bloqueada (externa)** | Transmissão NF-e/NFS-e (credenciais P1), provedores OPMENEXO/INPART, provedor de cobrança externo (inexistente — sandbox é a entrega), deploy IIS + upgrade do banco principal (decisão de agenda do usuário) |
| **Falhou (fachada)** | Lista D.2 integral (contato landing, propostas demo, Piloto, BI/Operações fetch morto, Inteligência hardcoded, V1420 zeros, shells estáticos, forms B2B→CreateApiKey) |
| **Não executada** | ~211 ações ISH operacionais core (plantões/escalas/financeiro/agenda/relatórios — pipeline real sem homologação viva), portais ClientePortal/ParceiroPortal (empty-state honesto, jornada não andada), B2B Launch/Ops inteiras, ajuda/manual, LGPD GETs, jornadas completas-desktop/mobile no navegador, smoke pós-IIS |

Contagens por ação permanecem as da seção C como fotografia de superfície; o que muda em R6 é o
**nível de prova** de cada bloco, registrado acima com arquivo/linha e evidência executada.
