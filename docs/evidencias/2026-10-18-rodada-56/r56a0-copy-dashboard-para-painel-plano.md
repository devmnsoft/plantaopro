# R56 · r56a0 — Plano: passada de copy "Dashboard" → "Painel" (revisão de menus)

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R55 (`24199a4`, suíte 1614).

## Origem

Fila consolidada `r53b1 §7`, item 2: **decisão de produto "Dashboard"→"Painel"** pendente desde a
D-R50-1 ("termo mantido por ser estabelecido; mudança global fica para revisão de menus"). Este item é
a materialização dessa decisão. Decisão tomada nesta rodada (autônoma, documentada em `r56z`): adotar
**"Painel"** como termo canônico em pt-BR.

Fato que fecha o raciocínio de produto: as próprias traduções idiomáticas já usavam o equivalente —
es "Panel de control"/"Panel Gerencial", fr "Tableau de bord"/"Tableau de Bord de Gestion", en
"Dashboard" (idiomático em inglês). Só o pt-BR mantinha o empréstimo inglês.

## Decisões

### D-R56-1 — Termo canônico pt-BR: "Painel"
- Sidebar/tela principal: "Dashboard" → **"Painel"** (aba do navegador já era "Painel operacional").
- Adm360: "Dashboard Gerencial…" → **"Painel Gerencial…"** (title, heading, card de capacidade).
- Demais telas: "Dashboard executivo"→"Painel executivo", "Dashboard Inteligente"→"Painel
  Inteligente", "Dashboard SaaS (executivo)"→"Painel SaaS (executivo)", "Dashboard clínico"→
  "Painel clínico", badges de seção `pp-badge` → "Painel", "Dashboard 360" (nav suprimentos) →
  "Painel 360", mock white label → "Painel".
- Continuidade de D-R50-1: **nomes de código invariáveis** — rotas (`/Home/Dashboard`),
  controllers, ícones (`app-icon name="Dashboard"`), classes CSS (`pp-*-dashboard`), chaves resx
  (`Nav.Dashboard`, `Adm360.Dash.CapDashboardTitle`), códigos de papel/capacidade/módulo
  (`SAUDE360_DASHBOARD`, `CLINICA_DASHBOARD`, `DASHBOARD_GERENCIAL`).
- en/es/fr: **sem mudança** (já idiomáticos).

### D-R56-2 — Zonas preservadas (mantêm "Dashboard")
- `CommercialDemoWeb/Landing.cshtml` `<h2>Dashboards</h2>` — zona comercial pública whitelista (R39).
- Breadcrumb da área compat V112 (rotulo derivado da ação; contrato `R4C11BreadcrumbTrailTests`).
- Catálogo comercial de planos no banco: `'DASHBOARD_GESTOR','Dashboard Gestor'` — zona comercial/governança.
- Seeds históricos (v113 atividades tipo 'Dashboard'; migrations legacy) — dados imutáveis.
- URLs/APIs (`/api/operacoes/dashboard`, `/api/dashboard`), comentários, docs internas, códigos de
  permissão exibidos como código no fluxo de contratação (`DASHBOARD.VER` e afins).

### D-R56-3 — v2355: nomes de exibição do módulo DASHBOARD e da capacidade CLINICA_DASHBOARD → "Painel"
O catálogo `modulos_sistema` (módulos **e** capacidades-filhas, tabela única desde v2339) é o que o
**tenant vê em Minha Assinatura (Módulos)**, no fluxo de contratação e na gestão de permissões
(`SelfServiceSaasController` lê `nome/descricao`). Renomear só o `nome` de duas linhas — o código
permanece estável em ambas (grants/permissões referenciam `modulo_id`/`codigo`; as permissões
`DASHBOARD.*` exibidas como código no fluxo de oferta são identificadores, não copy):
- `DASHBOARD` 'Dashboard' → **'Painel'** ('Indicadores e visão geral' já estava limpo).
- `CLINICA_DASHBOARD` 'Dashboard Clinico' → **'Painel Clinico'** (capacidade do pacote SAUDE360;
  ASCII, convenção das linhas da tabela).

Migration idempotente `database/migrations/2026_10_v2355_r56_nome_modulo_painel.sql` + sincronização
dos dois scripts canônicos (marcador SOURCE/SOURCE-SHA256, seção 126) + `source-checksums.json` +
`migration-manifest.json` (`dependsOn: [v2354]`, `installRequired: false`) — convenção R55.

### D-R56-4 — Contrato permanente + prova e2e
- `R56PainelCopyContratoTests` (7 fatos): resx pt-BR sem valor visível com "Dashboard"; valores exatos
  de `Nav.Dashboard` nos 4 locales; títulos Adm360 = "Painel Gerencial…"; views sem frases antigas
  (exceção ComercialDemoWeb) + 4 badges "Painel" + sidebar ligada à mesma chave; migration v2355 com
  upsert exato; scripts canônicos sincronizados; fato de banco auto-curado (advisory lock +
  reaplicação + retry 40P01) afirmando `nome='Painel'` para `codigo='DASHBOARD'`.
- `r56_painel_e2e.ps1`: com apps no ar, texto visível (tags/scripts removidos, entidades decodificadas)
  de Home, Gestao/Dashboard e Estoque **sem a palavra "dashboard"**, com os positivos "Painel" e
  "Painel Gerencial" presentes. Smoke R52 canônica roda inalterada como regressão geral.
- Suíte base 1614 → **1621** (+7 fatos).

## Inventário (recon desta rodada)

| Local | Antes | Depois |
|---|---|---|
| `UiTexts.resx` `Nav.Dashboard` | Dashboard | Painel |
| `UiTexts.resx` `Adm360.Dash.PageTitle/Heading/CapDashboardTitle` | Dashboard Gerencial… | Painel Gerencial… |
| `ClientePortal/ParceiroPortal/Demo/FeatureFlags` badges | >Dashboard< | >Painel< |
| `DashboardsPremium/Perfil` (Title + eyebrow + h1) | Dashboard … | Painel … |
| `Inteligencia/Dashboard` (Title + h1) | Dashboard … | Painel … |
| `SaasDashboard/Index` (Title + h1) | Dashboard SaaS … | Painel SaaS … |
| `Administrativo360/Gestao/Monitor` botão voltar | Dashboard | Painel |
| `_SuprimentosNav` item | Dashboard 360 | Painel 360 |
| `ClinicaDashboard/FluxoAtendimento` card CTA | Dashboard clínico | Painel clínico |
| `WhiteLabel Index/Preview` mock de menu | Dashboard | Painel |
| `modulos_sistema` módulo `DASHBOARD` (v2355) | 'Dashboard' | 'Painel' |
| `modulos_sistema` capacidade `CLINICA_DASHBOARD` (v2355) | 'Dashboard Clinico' | 'Painel Clinico' |

## Gates de fechamento
Rebuild Release `--no-incremental` → gate 160/160 DIFF=NENHUMA; testes dirigidos (R56 + R7DF6 +
R7DF1 + R4C11 + R50 + DatabaseGeneratorIntegrity); suíte integral; smoke R52 inalterada; `r56_painel_e2e.ps1`;
teardown de portas; docscan dos docs da rodada (bad=0); commits `<secoes> (rodada 56 r56): …` sem acentos.
