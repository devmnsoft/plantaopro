# R7-D0 — Varredura de escopo design/textos/i18n por jornada (relatório de exploração)

Data: 2026-10-11 · Método: exploração read-only (subagent `explore`, very thorough). Caminhos
com prefixo `Web/` são relativos a `C:\MNSOFT\plantaopro\backend\PlantaoPro.Web\`.

---

## 1. Inventário do mecanismo atual de i18n (estado verificado)

### 1.1 Como o resx é consumido
Não há tag helper, nem `@Localizer`/`IStringLocalizer`/`HtmlLocalizer`. O mecanismo é uma
**classe estática `UiText`** que encapsula um `ResourceManager`:

- `Web/Localization/UiText.cs:18-31` — `public static class UiText`; `L(string key)` =
  `Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key` (linha 23-24). Recurso base:
  `"PlantaoPro.Web.Localization.UiTexts"` (linha 20).
- Exposição aos views via `_ViewImports.cshtml:3` → `@using PlantaoPro.Web.Localization`,
  permitindo `@UiText.L("...")` direto em qualquer cshtml.
- Não há nada em `BaseController`: os controllers chamam
  `PlantaoPro.Web.Localization.UiText.L(...)` com namespace completo (único caso:
  `Web/Controllers/UsuarioController.cs:141,151,161,162,167,174`).

### 1.2 Infraestrutura de cultura (Program.cs)
- `Web/Program.cs:48-58` — `AddRequestLocalization`: cultura de **formatação fixa em pt-BR**
  (`UiLanguageCatalog.FormatCultureName`) e UICultures suportadas = pt-BR/en/es/fr; provider
  customizado.
- `Web/Services/Localization/UiLanguageCultureProvider.cs:20-29` — precedência USUÁRIO
  (claim `idioma`) > ORGANIZAÇÃO (claim `org_idioma`) > `Accept-Language` > pt-BR.
  `CurrentCulture` sempre pt-BR (moeda/datas não mudam com o idioma).
- Catálogo canônico compartilhado:
  `backend/PlantaoPro.CrossCutting/Localization/UiLanguageCatalog.cs` (arquivo não aberto
  linha a linha — "não verificado" além das referências de uso acima).
- `Web/Localization/UiText.cs:27-30` — `HtmlLang(HttpContext)` usado em
  `_Layout.cshtml:21` (`<html lang="...">`).

### 1.3 Catálogo de chaves — **59 chaves** no arquivo-mestre (contagem verificada: 59 `<data>`
em `UiTexts.resx`; idêntico 59/59/59 em `.en/.es/.fr.resx`)

| Grupo | Chaves |
|---|---|
| Common (2) | `Common.SkipLink`, `Common.CloseMenuAria` |
| Auth (2) | `Auth.DemoNotice`, `Auth.LoginTitle` |
| Login (32) | `Login.StoryTitle`, `Login.StorySubtitle`, `Login.TrustAcessoProtegido`, `Login.TrustContextoIsolado`, `Login.TrustAuditoriaAtiva`, `Login.Eyebrow`, `Login.Title`, `Login.Subtitle`, `Login.SessionAttention`, `Login.ReasonExpired`, `Login.ReasonTenantRequired`, `Login.ReasonUserBlocked`, `Login.ReasonTenantBlocked`, `Login.ReasonFeatureBlocked`, `Login.ReasonAccessDenied`, `Login.DelayTitle`, `Login.DelayDetail`, `Login.ConnectionLost`, `Login.ProgressTitle`, `Login.ProgressDetail`, `Login.EmailLabel`, `Login.EmailHelp`, `Login.PasswordLabel`, `Login.ShowPasswordAria`, `Login.PasswordHelp`, `Login.CapsLock`, `Login.Button`, `Login.ForgotPassword`, `Login.HelpSummary`, `Login.HelpBody`, `Login.SecurityNote`, `Login.PrivacyLink` |
| Profile (19) | `Profile.Title`, `Profile.Header`, `Profile.AccountActive`, `Profile.Nome`, `Profile.Email`, `Profile.Telefone`, `Profile.Notificacoes`, `Profile.NotInformado`, `Profile.EditarDados`, `Profile.AlterarSenha`, `Profile.LanguageSection`, `Profile.LanguageHint`, `Profile.Save`, `Profile.LanguageSaved`, `Profile.LanguageInvalid`, `Profile.LanguageSaveFailed`, `Profile.SessionExpiredFallback`, `Profile.LanguageApplyFailedFallback`, `Profile.UnexpectedSaveError` |
| Lang (4) | `Lang.pt-BR`, `Lang.en`, `Lang.es`, `Lang.fr` |

Observação de nomenclatura: 5 chaves usam nome em português (`Profile.Nome/Email/Telefone/Notificacoes`,
e valores `Lang.*`) — as demais em inglês. Padrão de grupo inconsistente dentro do próprio catálogo.

### 1.4 Views/controllers que JÁ usam (comprovado por grep de `UiText.L(`)
- `Web/Views/Account/Login.cshtml:4,9-14,25-33,36,51,55,59,62-64,68-74,78-79,81,85-90` (~30 pontos)
- `Web/Views/Usuario/Index.cshtml:4,11-12,16-19,23-24,30-31,37,41,46`
- `Web/Views/Shared/_Layout.cshtml:21,100,121`
- `Web/Views/Shared/_AuthLayout.cshtml:8,31,34`
- `Web/Controllers/UsuarioController.cs:141,151,161,162,167,174`
- Testes de contrato que travam esse uso: `backend/PlantaoPro.Tests/V2150SaasAccessFoundationTests.cs:153`,
  `V2164CompilationAndJourneyContractTests.cs:113`.

**Fora disso, toda a UI usa literais pt-BR hardcoded** (ver seção 2). O texto de ajuda
contextual por tela vem de outro mecanismo hardcoded em cshtml
(`Web/Views/Shared/_ScreenGuide.cshtml`, ex.: linha 10 com strings por controller) — não passa pelo resx.

## 2. Textos expostos ao usuário FORA do mecanismo (amostragem sistemática por jornada)

### Jornada 1 — Onboarding/Implantação
| # | Literal (resumo) | Local |
|---|---|---|
| 1 | Título da página "Onboarding SaaS"; hero "Implantação orientada ao contrato"/"Onboarding de clientes"; botões "Reavaliar agora", "Novo cliente com plano" | `Web/Views/Onboarding/Index.cshtml:2,4` |
| 2 | Estado "Sem cliente selecionado"; "Onboarding ainda não iniciado"; botão "Iniciar onboarding" | `Web/Views/Onboarding/Index.cshtml:12,16-17` |
| 3 | Legenda de status (Concluída/Pendente/Bloqueada/Pulada), "Concluir", "Pular", "Restaurar", "Justificativa" | `Web/Views/Onboarding/Index.cshtml:27,52-61` |
| 4 | Wizard de 5 passos + "Plano e unidade inicial" | `Web/Views/Onboarding/NovoCliente.cshtml:2,9,28,50` |
| 5 | "Cliente criado com sucesso.", "Ir para clientes" | `Web/Views/Onboarding/Sucesso.cshtml:6,16` |
| 6 | "Central de Implantação, Diagnóstico e Go-Live"; "Validar tudo"; loading "Carregando informações reais da API..." | `Web/Views/Implantacao/Index.cshtml:2,7,10,15` |
| 7 | **Mensagens de sucesso SEM acento no controller** (inconsistência de design): "…as etapas refletem os modulos contratados…", "Etapa concluida." | `Web/Controllers/OnboardingController.cs:96,105,115,124,133` |

### Jornada 2 — Plantões/Escala central
| # | Literal | Local |
|---|---|---|
| 1 | Título "Central de Operação"; hero "Central de cobertura"; KPIs "Cobertura confirmada"/"Aguardando resposta"/"Turnos em aberto"/"Conflitos" | `Web/Views/CentralEscala/Index.cshtml:3,8-11,25` |
| 2 | Botões "Novo plantão", "Abrir calendário"; help "Como usar esta página" | `Web/Views/CentralEscala/Index.html:30-31,41`.cshtml correção: `Web/Views/CentralEscala/Index.cshtml:30-31,41` |
| 3 | Select de situação com 8 rótulos hardcoded ("Vaga descoberta", "Atribuída sem confirmação"…) + "Aplicar filtros" | `Web/Views/CentralEscala/Index.cshtml:57,59` |
| 4 | Cartões de cobertura "Plantões hoje/Confirmados/Aguardando/Em risco/Conflitos" | `Web/Views/CentralEscala/Index.cshtml:64-68` |
| 5 | Painéis "Atenção necessária", "Linha do tempo operacional", "Ações rápidas"; estados vazios | `Web/Views/CentralEscala/Index.cshtml:88,95-97,101-106` |
| 6 | Tela de detalhe: "Voltar", cabeçalhos "Início/Fim/Vagas/Valor", badges com `@Model.Status` cru | `Web/Views/CentralEscala/Plantao.cshtml:3,7,20,26-29,33-35` |
| 7 | Lançes vazios "Nenhum plantão nesta etapa" / "Nenhum plantão neste recorte" | `Web/Views/Plantoes/Index.cshtml:79,99` |
| 8 | "Escolha o plantão"/"Localizar pela cobertura" | `Web/Views/Convites/Index.cshtml:15` |

### Jornada 3 — Saúde 360 (consultas/triagem/painel de chamada/pacientes)
Estrutura central: **uma única view genérica** `Web/Views/Saude360/Modulo.cshtml` renderizada
por **49 views-fachada** (ex.: `Web/Views/Pacientes/Index.cshtml:5`, `Web/Views/Triagem/Index.cshtml:5`,
`Web/Views/PainelChamada/Index.cshtml:5` e todas as 8 views de `PainelChamada/*`).
| # | Literal | Local |
|---|---|---|
| 1 | Eyebrow "PlantãoPro Saúde 360 · jornada assistencial"; etapa-jornada com 7 passos ("Paciente→…→Financeiro") | `Web/Views/Saude360/Modulo.cshtml:29,43-52` |
| 2 | Blocos do paciente "Selecione um paciente retornado pela API… Nenhum histórico demonstrativo é exibido." | `Web/Views/Saude360/Modulo.cshtml:65-68` |
| 3 | Cabeçalhos de tabela "Identificação/Código/Descrição/Status/Registro"; banner LGPD | `Web/Views/Saude360/Modulo.cshtml:96,121-122` |
| 4 | Estado vazio genérico duplicado em duas camadas | `Web/Views/Saude360/Modulo.cshtml:80` e `:100` |
| 5 | `Consultas/Index.cshtml` = **stub sem lista, sem CTA de dados, sem estado vazio** (só hero + 4 badges) | `Web/Views/Consultas/Index.cshtml:2,7-14` |
| 6 | Modal de finalização clínica: "Destino financeiro", opções "Particular/Convênio/Plano de saúde/Cortesia/Isento", "Confirmar finalização" | `Web/Views/Consultas/Atendimento.cshtml:23` |
| 7 | Contexto clínico "Alergias relatadas/Plano/Unidade" | `Web/Views/Consultas/Atendimento.cshtml:11` |
| 8 | Modal de boas-vindas todo em JS string pt-BR | `Web/wwwroot/js/saude360-tour.js:1` |
| 9 | Textos de ajuda por perfil hardcoded em `_PageHelp` (linhas 18-22 de `Modulo.cshtml`) | `Web/Views/Saude360/Modulo.cshtml:18-22` |

### Jornada 4 — Administrativo 360 (financeiro/cotações/dashboard)
| # | Literal | Local |
|---|---|---|
| 1 | "Gestão Organizacional"; cartões "Departamentos/Cargos/Colaboradores/Contratos de Trabalho" | `Web/Views/Administrativo360/Index.cshtml:3,18-19,39,50,61,72,126` |
| 2 | "Central de Cotações Pré-Cirúrgicas"; guia "Como Funciona o Ciclo…" (4 blocos) | `Web/Views/Administrativo360/Cotacoes/Index.cshtml:3,12,29-46` |
| 3 | Rótulos de status de cotação em duplo lugar (select + badges switch) | `Web/Views/Administrativo360/Cotacoes/Index.cshtml:75-82` e `166-192` |
| 4 | Cabeçalhos de tabela "Provedor/ID Externo/Hospital/Prazo/Status Interno/Orçamento/Ações" | `Web/Views/Administrativo360/Cotacoes/Index.cshtml:107-115` |
| 5 | "Dashboard Gerencial & Estatístico 360"; "Exportar Cotações (CSV)"; "Capacidades Contratadas no Tenant" | `Web/Views/Administrativo360/Gestao/Dashboard.cshtml:3,15,21-24,65` |
| 6 | **Mensagens de sucesso/falha pt-BR hardcoded nos controllers**: 8 em `Web/Controllers/Adm360CotacoesWebController.cs:106,139,159,179,200,222,270,314`; mais em `Adm360FiscalWebController.cs:149,236,307,363,415`; `Adm360DocumentosXmlWebController.cs:122-294`; `Administrativo360Controller.Cadastros.cs:171-249`; `Administrativo360Controller.Cirurgias.cs:89`; `Adm360GestaoWebController.cs:88` (amostra de grep limitada a 30 — há mais) |
| 7 | Alerta de erro genérico `@Model.Erro` com botão "Fechar" | `Web/Views/Administrativo360/Index.cshtml:25-28`; `Cotacoes/Index.cshtml:53-56` |

### Jornada 5 — Navegação global/sidebar
| # | Literal | Local |
|---|---|---|
| 1 | Defaults de fallback do tenant/perfil: "Organização", "Profissional" | `Web/Views/Shared/_AppSidebar.cshtml:6-7` |
| 2 | Labels da busca rápida/colapso: "Abrir busca rápida", "Busca rápida", "Recolher menu lateral" | `Web/Views/Shared/_AppSidebar.cshtml:23,25` |
| 3 | **Todos** os títulos de seção e itens de menu hardcoded: "Visão geral", "Operação", "Saúde 360", "Financeiro", "Administração do Cliente", "Administração MNSOFT", "Acompanhamento", "Suporte" + ~30 links ("Minha Central", "Meu dia", "Central de Pendências", "Cobertura", "Jornada clínica", "Visão financeira", "Logs e saúde"…) | `Web/Views/Shared/_AppSidebar.cshtml:20-97` (linhas 28-97) |
| 4 | Topbar: "Abrir menu", "Você está em" (breadcrumb aria), "Buscar no PlantãoPro", "Ajuda contextual" | `Web/Views/Shared/_AppTopbar.cshtml:18,22,52-54,59` |
| 5 | Title padrão do documento: "PlantãoPro" | `Web/Views/Shared/_Layout.cshtml:28` |
| 6 | Descrições de módulos por controller em JS (textos longos pt-BR) | `Web/wwwroot/js/form-experience.js:135` (amostra) |
| 7 | Guias de tela por controller hardcoded (ex.: "Hospitais"/"Especialidades") | `Web/Views/Shared/_ScreenGuide.cshtml:10` |

## 3. Chaves expostas cruas ao usuário (evidência de "key leak")

Buscas realizadas (todas com 0 ocorrências relevantes):
- `UiTexts\.` e `Resources\.` em `*.cshtml` de `Web/Views` → **0 ocorrências**.
- `@Localizer`, `IStringLocalizer`, `HtmlLocalizer` em `Web/` → **0 ocorrências**.
- Configuração de cultura/fallback em `appsettings.json` / `appsettings.Development.json` /
  `appsettings.example.json` (padrões `ulture`, `ocaliz`) → **0 ocorrências** (o fallback vive
  em código: `UiLanguageCultureProvider.cs:28` e `UiText.cs:24`).

Único caminho teórico de chave crua: `Web/Localization/UiText.cs:24` — `?? key` devolve a
própria chave se ela não existir no master. **Mitigação existente:** os 4 arquivos têm
exatamente 59 chaves idênticas (verificado por contagem) e existe teste de contrato de
paridade citado no comentário de `UiText.cs:12-14` ("não verificado" o corpo do teste em si).
**Conclusão: busca feita, 0 chaves cruas encontradas em tela.**

## 4. Estados vazios e feedback de sucesso/falha

### Sem estado vazio ou com estado vazio inadequado
| Tela | Problema | Local responsável |
|---|---|---|
| `Consultas/Index` (link direto do sidebar) | Stub: não tem lista, nem estado vazio, nem ação de dados — usuário cai numa página "morta" | `Web/Views/Consultas/Index.cshtml:4-17` |
| 49 telas do cluster Saúde 360 (Pacientes, Triagem, PainelChamada×8, Agendamentos×7, Convenios×9, ClinicaFinanceiro×7, PlanosSaude×5, PendenciasClinicas, ClinicaDashboard, ClinicaUnidades…) | Estado vazio **genérico idêntico em todos** ("Nenhum registro encontrado / A API não retornou registros reais para este módulo") — não diz *o que* é vazio nem *como começar* | `Web/Views/Saude360/Modulo.cshtml:80,100` |
| Detalhe de plantão na Central | Se `Model == null` e sem `ErrorMessage`, a tela renderiza só o botão "Voltar" — área totalmente em branco, sem explicação | `Web/Views/CentralEscala/Plantao.cshtml:15-39` |
| Implantação (SPA parcial) | Lista de etapas e timeline vaziam começam como `--`/vazio até o fetch; tem div de empty-state mas dependente de JS | `Web/Views/Implantacao/Index.cshtml:15,19-22,30-31` |
| Admin 360 – Gestão Organizacional | Sem estado vazio: com 0 registros os cartões apenas mostram "0 cadastrados" | `Web/Views/Administrativo360/Index.cshtml:32-130` |

### Com estado vazio adequado (referência positiva)
`Web/Views/Plantoes/Index.cshtml:99`, `Web/Views/Home/Dashboard.cshtml:67,97`,
`Web/Views/Administrativo360/Cotacoes/Index.cshtml:120-126` (msg condizente com filtros),
`Web/Views/CentralEscala/Index.cshtml:97` (parcial `_DemoEmptyState`),
`Web/Views/Onboarding/Index.cshtml:10-18` (estados "sem contexto"/"não iniciado").

### Feedback de ações
- **Com feedback (hardcoded pt-BR, fora do resx):** `Web/Controllers/CentralEscalaController.cs:86,91,107,135,139,145`;
  `Web/Controllers/OnboardingController.cs:96-133` (com defeito de acentuação);
  `Web/Controllers/Adm360CotacoesWebController.cs:106-314`; `Web/Controllers/AccountController.cs:328,401,423`.
  Renderização canônica: `Web/Views/Shared/_ToastMessages.cshtml:24-46`
  (chaves `Success/Error/Warning/Info` + aliases `SuccessMessage/ErrorMessage`).
- **Mensagens escritas em 3 camadas diferentes do mesmo fluxo**: controller (TempData),
  view (alert inline `@Model.Erro`/`ViewBag.ErrorMessage`) e JS — nenhuma delas localizada;
  risco de tradução divergente quando as chaves forem introduzidas.

## 5. Terminologia inconsistente

### 5.1 "plano" com 3 significados distintos nas views principais (top 10)
1. **SaaS/contrato:** "Plano contratado" — `Web/Views/Assinaturas/Details.cshtml:9`
2. **SaaS/contrato:** "Selecione o plano contratado" — `Web/Views/Assinaturas/_Form.cshtml:12`
3. **SaaS/contrato:** "Plano e unidade inicial / Vincule o plano contratado" — `Web/Views/Onboarding/NovoCliente.cshtml:28`
4. **SaaS/contrato:** "Plano: {nome}" — `Web/Views/Onboarding/Sucesso.cshtml:10`
5. **SaaS/contrato:** "plano contratado" — `Web/Views/Shared/_ActionBar.cshtml:3`
6. **SaaS/contrato:** "Planos comerciais" (menu global) — `Web/Views/Shared/_AppSidebar.cshtml:85`
7. **SaaS/contrato:** seletor "Plano" no cadastro público — `Web/Views/Cadastro/Cadastro.cshtml:46`
8. **Plano de saúde (convênio):** `<dt>Plano</dt>` no contexto clínico — `Web/Views/Consultas/Atendimento.cshtml:11`
9. **Plano de saúde (convênio):** option "Plano de saúde" no destino financeiro — `Web/Views/Consultas/Atendimento.cshtml:23`
10. **SaaS dentro de tela clínica:** cartão "Plano mínimo … requisito do módulo" — `Web/Views/Saude360/Modulo.cshtml:84`; módulo inteiro de plano de convênio: `Web/Views/Convenios/Planos.cshtml` + pasta `Web/Views/PlanosSaude/` (5 views) contra `Web/Views/Planos/` (SaaS) e `Web/Views/PlanosPublicos/`.

Verificação extra: **"plano de cuidados" / "plano de escala" → 0 ocorrências** em cshtml. O
conflito real é **plano-SaaS × plano-de-saúde**, não plano-de-cuidados.

### 5.2 "sistema" vs "saúde"
- **"Sistema de Saúde" / "sistema de saude": 0 ocorrências** em cshtml.
- Forma canônica em texto exibido: **"Saúde 360"** (acentuado, com espaço) — top ocorrências
  em `Web/Views/Cid/*.cshtml` (14 pontos), `Agendamentos/AgendaPremium.cshtml:8`,
  `Consultas/Index.cshtml:2,7`, `Saude360/Modulo.cshtml:29,43`, `_AppSidebar.cshtml:49`,
  `wwwroot/js/saude360-tour.js:1`.
- **"Saude360"** (sem acento, CamelCase) aparece massivamente como identificador/caminho em
  49+ views — visível apenas em código; porém o fallback `"Saude360Assistencial"` e claims
  podem vazar via `profile.Replace('_',' ')` do sidebar (`_AppSidebar.cshtml:24`).

## 6. Priorização sugerida (≤6 fatias pequenas e entregáveis)

| Fatia | Escopo concreto | Impacto | Risco |
|---|---|---|---|
| **F1 — Sidebar + topbar (navegação global)** | Grupo `Nav.*` (~35 chaves) no `UiTexts.resx`+trad; substituir literais de `_AppSidebar.cshtml:20-97`, `_AppTopbar.cshtml:18-59`, `_Layout.cshtml:28`; tratar acento do fallback "Organização/Profissional" | Altíssimo (todo usuário logado, todo idioma) | Baixo (labels só; guarda de permissão intocada) |
| **F2 — Login/Perfil: fechar lacunas vizinhas** | `Account/ForgotPassword`/reset; textos de sessão de `AccountController.cs:328,401,423` para `Auth.*`/`Session.*`; padronizar nomes de chaves `Profile.Nome/Telefone/Notificacoes` para inglês + atualizar os 4 resx e usages | Alto | Muito baixo |
| **F3 — Central de Escala** | Grupo `Coverage.*`/`Shift.*` (~25 chaves): `CentralEscala/Index.cshtml:3-106`, `CentralEscala/Plantao.cshtml:3-35` (inclui estado vazio/erro quando `Model==null`), `Plantoes/Index.cshtml:79,99`, mensagens de `CentralEscalaController.cs:86-145` | Alto | Baixo |
| **F4 — Saúde 360: des-genericizar o vazio + Consultas stub** | `Saude360/Modulo.cshtml:29-100`: empty-state por módulo via ViewModel existente; transformar `Consultas/Index.cshtml` em entrada com estado vazio + CTA real; localizar etapa-jornada | Alto (49 telas) | Baixo/médio |
| **F5 — Onboarding/Implantação: rotas + acentuação** | Grupo `Onboarding.*`/`Implantacao.*` (~20 chaves) + corrigir acentos em `OnboardingController.cs:96,115,124` | Médio | Muito baixo |
| **F6 — Admin 360: cotações + dashboard + convenção de TempData** | Grupo `Adm360.*` (~30 chaves) para `Cotacoes/Index.cshtml`, `Gestao/Dashboard.cshtml`, `Index.cshtml`; convenção de que TODO `TempData["Success"/"Error"]` de `Adm360*` passe por `UiText.L` | Médio | Baixo |

## Pendências explícitas (não verificado)
Corpo dos testes de paridade de resx em `backend/PlantaoPro.Tests/*` (citados, não abertos);
`UiLanguageCatalog.cs` linha a linha; `Administrativo360/{Orcamentos,TitulosReceber,…}.cshtml`
em profundidade; `Escalas/*.cshtml` (assumidos do padrão de `Plantoes/Index.cshtml`); views
`V112/V114/V116` e pastas demo (`CommercialDemoWeb`, `B2BLaunch`, `Piloto`) — fora das 5 jornadas.
