# R8-B23 — Definição e rastreabilidade de KPIs: COMMAND CENTER agora EMITE e EXIBE as definições (§27 linha 205 residual, parte 4)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia da **auditoria ALÉM do top-10 §29**: §27 linha 205 *residual* (renderização em tela das definições de KPI). Quarta tela (após BI/B20, Dashboard/B21 e Meu Dia/B22): **Command Center** — a visão operacional do gestor (OPERACAO).
- **Data:** 2026-10-14
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica: código do Command Center)

Os **8 indicadores operacionais** exibidos como cards (Risco agora/Descobertos/Confirmação/Em Execução/Ocorrências/Profissionais/Financeiro/Alertas) são computados pela API em `ManagerCommandCenterService.GetAsync` — **um único `SqlCommandCenter`** (`ManagerCommandCenterService.cs:44-117`) — e devolvidos por `api/manager-command-center` como o campo `summary` (`CommandCenterSummary`), que a BFF Web desserializa em `ManagerSummary`. Como Meu Dia, Command Center estava **fora do escopo B9** e **não emitia** definições. Cada card tem um filtro claro e fiel:
- `Critical` (Risco agora) = plantões com `vagas_disponiveis > 0 and data_inicio <= now() + interval '6 hours'`;
- `Uncovered` (Descobertos) = `vagas_disponiveis > 0 and lower(status) not in ('cancelado','realizado')`;
- `PendingConfirmation` (Confirmação) = `lower(status) in ('aberto','pendente')`;
- `PendingCheckIns` (Em Execução) = `medico_checkins.checkout_em IS NULL and status_conferencia='REGISTRO_INCOMPLETO'` (estado atual do tenant);
- `OpenIncidents` (Ocorrências) = `ocorrencias_operacionais.situacao in ('ABERTA','EM_ATENDIMENTO') and reg_status='A'`;
- `AvailableProfessionals` (Profissionais) = `medicos.reg_status='A'` vinculados ao tenant/cliente;
- `FinancialPending` (Financeiro) = `fechamento_plantao.status in ('EM_CONFERENCIA','AGUARDANDO_APROVACAO','COM_DIVERGENCIA')`;
- `CriticalNotifications` (Alertas) = `notificacoes` ativas, não lidas, `tipo in ('critico','urgente','erro')`.

Obs.: `Today` e `PendingReplacements` existem no `CommandCenterSummary` **mas não viram card** → não são definidos aqui (critério "por indicador exibido", coerente com Meu Dia).

## 2. Decisão e mudança (EMITE na API + EXIBE na Web; sem nova consulta/número/regra)

- **API — emissão fiel ao SQL:**
  - `CommandCenterSummary` ganha `Definicoes` + `TimeCriterion` como **`{ get; init; }` não-posicionais** (o ctor posicional de 10 colunas usado por `grid.ReadSingleAsync<CommandCenterSummary>()` permanece inalterado — evita ambiguidade de mapeamento Dapper);
  - `GetAsync` shapeia `summary = (await grid.ReadSingleAsync<…>()) with { Definicoes = Definicoes(), TimeCriterion = TempoCritico }`. Cada texto descreve **exatamente** o filtro do SQL (`interval '6 hours'`, `REGISTRO_INCOMPLETO`, `AGUARDANDO_APROVACAO`, …) — nada inventado.
- **Web — espelho + renderização:**
  - `ManagerSummary` ganha `Definicoes` (`KpiDefinicaoDto` — reuso do espelho da B21) + `TimeCriterion` (a Web não referencia o projeto Api);
  - `CommandCenter/Index.cshtml`: **apenas no ramo operacional**, um bloco `<details>` **"Como cada indicador é calculado"** (server-side, codificação HTML automática do Razor) com `TimeCriterion` + uma linha por card; lista vazia → estado explícito. Variável **hoistada** para o `@{ }` superior usando `Array.Empty<…>()` (evita dependência de Linq na view; lição RZ1010 da B21).
- **Escopo:** só os **8 cards operacionais** (tenant `GetAsync`). A **view GLOBAL** (governança — métricas de plataforma via `GetGlobalAsync`) exibe métricas *diferentes* e segue fora desta fatia (ver §5).

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Api/ManagerCommandCenterService.cs` | **Produção:** `CommandCenterSummary` + `Definicoes`/`TimeCriterion` (`{ get; init; }`); `GetAsync` anexa as definições + `Definicoes()`/const fiéis ao SQL. |
| `backend/PlantaoPro.Web/Models/ManagerCommandCenterViewModel.cs` | **Produção:** `ManagerSummary` + `Definicoes`/`TimeCriterion` (espelho). |
| `backend/PlantaoPro.Web/Views/CommandCenter/Index.cshtml` | **Produção:** bloco `<details>` "Como cada indicador é calculado" (ramo operacional). |
| `backend/PlantaoPro.Tests/R8B23CommandCenterDefinicoesContratoTests.cs` | **Novo:** 3 fatos source-read — API emite (fiel ao SQL) · VM Web carrega · view renderiza. |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §27 linha 205 → + Command Center (B23, com emissão); residual = Admin360 (+ view global CC). |

## 4. Evidência (executada)

- **Build sln completa:** EXIT 0 (compilação com êxito — ambos os projetos e a view Razor do Command Center compilam limpo).
- **Alvo R8B23:** `R8B23CommandCenterDefinicoesContratoTests` = **3/3 aprovados** (`--filter FullyQualifiedName~R8B23`).
- **Gate phaseA v2:** EXIT 0 (nas duas execuções).
- **Full suite v2 — DUAS passes completas LIMPAS de 1382** (baseline 1379 + 3 novos): `pipeline1-full3` (EXIT 0) e `pipeline2-full1` (EXIT 0), ambas com `Total de testes: 1382`.
- **Rodadas "barulhentas" — todas ambientais, nenhuma no caminho CC/Produtividade** (box compartilhado, RAM livre 0,5–1,0 GB):
  - `pipeline1-full1`: `SaasGovernancaB6Rodada4Tests.G3_…` com `Npgsql 40P01: impasse detectado` **dentro de `PurgarAsync()`→`Dispose()`** (teardown, `SaasGovernancaB6Rodada4Tests.cs:481/78`) — deadlock transiente sob carga paralela; classe flake **conhecida** (catálogo ①). **Isolado = 4/5** (o único erro foi um *process kill* por OOM, sem resultado de teste — não falha de assert/banco).
  - `pipeline1-full2` e `pipeline2-full2`: **host dotnet test morto por OOM** (EXIT=−1, sem linha `Total de testes`) — abortou cedo (~0,5–1,4 min), não falha de teste.
  - `pipeline2-full3`: `R7OBOnboardingPreferenciasIdiomaTests.O1_…` com `40P01: impasse detectado` — mesma **classe** e **assinatura** do R7OB O2 da B22 (já isolado 5×=5/5 ali). **Isolado = 5/5.**
- **Observação de padrão (box sob pressão sustentada):** em cada passe paralelo sob RAM baixa surge ~1 deadlock (40P01) aleatório em teste de escrita DB + ocasionalmente um kill do host inteiro por OOM (agora mesmo um passe de teste **único** foi matado — G3 RUN3). Confirmar transiente: isolar (sem concorrência) → limpo; e a suíte completa passa limpa ≥2× com o mesmo binário. **Não** regressão.
- **Verificação byte-level:** `git status` = 2 arquivos Api + 2 arquivos Web alterados + teste novo + docs; nenhum outro arquivo de produção tocado.

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Renderizar `Definicoes` em ADMIN360** (§27-205 residual remanescente): várias áreas; como Meu Dia/CC, exige **emitir** definições no backend de cada área (fórmulas fiéis ao SQL) antes de exibir. Segue pendente junto à consolidação de métricas (B9/B18).
2. **View GLOBAL do Command Center** (`GetGlobalAsync` — métricas de plataforma: Tenants/Clientes/Módulos Ativos): exibe KPIs *diferentes* das operacionais; seguiria exigindo emitir suas próprias definições nesse endpoint. Pendente (menor; visão de governança).
3. **Drill-down §27-208:** segue proposta/pendente (B19) — agora **quatro** telas (BI + Dashboard + Meu Dia + Command Center) exibem definições (base para linkar "ver registros").
4. **Caveat honesto (documentado, sem mudança):** `Today` e `PendingReplacements` existem no DTO mas não são exibidos como card → não definidos nesta fatia (critério "por indicador exibido"). Janelas "hoje/crítico" usam `now()`/`current_date` no fuso do servidor; os cards de plantões respeitam o período selecionado, os demais são estado-atual do tenant (detalhes no `TimeCriterion`). Nenhuma regra inventada.

## 6. Fila seguinte (R8) — auditoria além do top-10

§27-205: **BI (B20) + Dashboard (B21) + Meu Dia (B22) + Command Center (B23) entregues**; **Admin360 pendente** (+ view global CC). §27-208: pendente (base em 4 telas p/ linkar). Próximos: emitir+exibir definições em Admin360 (ou na view global do CC), ou seguir §27/§28 pela matriz, ou janela viva de homologação / push.

## 7. Gotchas aprendidos (reutilizáveis)

- **Record DTO consumido pelo Dapper + campos novos:** `{ get; init; }` não-posicionais (corpo do record) mantêm o ctor posicional que o Dapper resolve por colunas inalterado; popula-se com `with { … }`. (Mesma regra da B22.)
- **Tela com vários KPIs → definir SÓ os exibidos:** o `CommandCenterSummary` tem 10 campos, mas só 8 viram card. Definir os exibidos (fiel ao "por indicador"); listar explicitamente no doc os campos NÃO exibidos (hoje/atraso) p/ deixar claro que a omissão é intencional, não esquecimento.
- **Kill por OOM ≠ falha de teste:** sob RAM baixa até um passe de **teste único** pode ser matado (EXIT=−1, log sem resultado). Distinguir: se o log **não tem** linha de resultado (Aprovado!/Total), é kill do processo, não falha. Re-isolar confirma.
- **Fatiamento 2-projetos (emitir + exibir):** mesmo encadeamento da B20/B21/B22 — campo no DTO da API (init) → popular no serviço/controller → campo no mirror Web → renderizar na view; mesmo nome de campo (case-insensitive) para o STJ mapear.
