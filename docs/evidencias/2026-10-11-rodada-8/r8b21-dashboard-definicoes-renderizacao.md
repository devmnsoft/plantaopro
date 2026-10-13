# R8-B21 — Definição e rastreabilidade de KPIs: o PAINEL OPERACIONAL (Home/Dashboard) agora EXIBE as definições (§27 linha 205 residual, parte 2)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia da **auditoria ALÉM do top-10 §29**: §27 linha 205 *residual* (renderização em tela das definições de KPI). Segunda tela após o BI (R8-B20): o painel principal `Home/Dashboard`.
- **Data:** 2026-10-13
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica: código do dashboard + B9/B11)

R8-B9 fez o serviço de dashboard **EMITIR** `Definicoes[]` (`Codigo`/`Label`/`Definicao`) + `TimeCriterion` na API (`Data.cs` `DashboardService.GetAsync:2407-2427`, 13 entradas). O painel principal consome `/api/dashboard` via `HomeController`, mas a Web usa **mirror records** (não referencia o projeto `PlantaoPro.Api`) e o mirror `DashboardDto` (13 parâmetros posicionais) **derrubava** `Definicoes`/`TimeCriterion` no deserialize — o usuário via os números sem a fórmula/fonte/período/fuso. Mesma classe de dívida que a B11 registrou ("VM Web não exibe `Definicoes`") e que a B20 tratou no BI.

## 2. Decisão e mudança (só apresentação + espelho do DTO; sem nova consulta/número/regra clínica)

- **`ViewModels.cs`** (Web Models): 
  - novo mirror `KpiDefinicaoDto(string Codigo, string Label, string Definicao)` — mesmo shape de JSON (`codigo`/`label`/`definicao`) do `KpiDefinicao` da API; necessário porque a Web MVC **não referencia** `PlantaoPro.Api`;
  - `DashboardDto` ganhou dois parâmetros **com padrão** no fim: `IReadOnlyList<KpiDefinicaoDto>? Definicoes = null` e `string TimeCriterion = ""`. Com padrão para **não quebrar** a única construção posicional (`HomeController:87`, 13 args) nem a expressão `with` do filtro (`HomeController:125`). O `System.Text.Json` popula os campos do payload durante `ReadApiResponse<DashboardOverviewDto>`; a `with {...}` do filtro preserva os membros não sobrescritos → chegam à view.
- **`Home/Dashboard.cshtml`**: dentro do ramo onde `indicadores != null` (a ramificação nula já cai no `_EmptyState`), um bloco `<details>` **"Como cada indicador é calculado"** (server-side, com codificação HTML automática do Razor) que exibe `indicadores.TimeCriterion` + uma linha por indicador (`Label`: `Definicao`); lista vazia → "Sem definições informadas pela fonte".
- **Escopo:** só o **painel `Home/Dashboard`**. Meu Dia, CommandCenter e Admin360 seguem como dívida registrada (B9 os colocou fora do §29-1; exibi-los exigiria primeiro **emitir** definições no backend — mudança maior, fora desta fatia).

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Web/Models/ViewModels.cs` | **Produção:** novo mirror `KpiDefinicaoDto`; `DashboardDto` ganha `Definicoes` + `TimeCriterion` (parâmetros com padrão). |
| `backend/PlantaoPro.Web/Views/Home/Dashboard.cshtml` | **Produção:** bloco `<details>` "Como cada indicador é calculado" (TimeCriterion + Label/Definicao por indicador + estado vazio). |
| `backend/PlantaoPro.Tests/R8B21DashboardDefinicoesContratoTests.cs` | **Novo:** 3 fatos source-read — (1) o mirror Web carrega `Definicoes`+`TimeCriterion` (+ `KpiDefinicaoDto` existe); (2) a view as renderiza; (3) a fonte (`Data.cs`) continua emitindo. |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §27 linha 205 → BI **+ Dashboard** em tela entregues; residual = MeuDia/CommandCenter/Admin360. |

## 4. Evidência (executada)

- **Build sln completa:** EXIT 0 (**compilação com êxito** — a view Razor e o record mudado compilam limpo). *Nota:* a 1ª tentativa falhou com `RZ1010` (um `@{ var ... }` escrito **dentro** do bloco transição `else { }`); corrigido ao **hoistar** a variável para o `@{ }` superior; 2ª tentativa = êxito.
- **Alvo R8B21:** `R8B21DashboardDefinicoesContratoTests` = **3/3 aprovados** (`--filter FullyQualifiedName~R8B21`).
- **Gate phaseA v2:** **EXIT 0** (RAM livre 0,83 GB ao iniciar).
- **Full suite v2:** **Total de testes: 1376** (baseline 1373 + 3 novos) em TODAS as 3 rodadas. full1 + full2 **limpos**; full3 teve **1** falha — `Saude360R4B8FinanceiroCanonicidadeTests.T09_EstornoComCaixaFechado_NaoReabreECaixaComAvisoDeConciliacao` — com `Npgsql … 40P01: impasse detectado` (**deadlock**), sem ligação com a fatia:
  - é erro de **banco** (impasse), não de assert/valor; full1+full2 passaram com o **mesmo binário**;
  - **isolado = 5× 5/5** → **transiente** (contention sob carga paralela em box de RAM baixa compartilhado).
- **Verificação byte-level:** `git status` = `ViewModels.cs` + `Home/Dashboard.cshtml` alterados + teste novo (+ docs); nenhum outro arquivo de produção tocado.

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Renderizar `Definicoes` nas demais telas** (§27-205 residual remanescente): **Meu Dia, CommandCenter, Admin360**. Diferentemente de BI/dashboard (que já **emitem** definições no backend), estes estão **fora do escopo B9** — exigiriam primeiro *emitir* definições (definir as fórmulas fielmente ao SQL), então são uma mudança maior; seguem pendentes junto à consolidação de métricas (B9/B18).
2. **Drill-down §27-208:** segue proposta/pendente (B19) — agora **duas** telas (BI + Dashboard) exibem as definições (base para linkar "ver registros").
3. **Caveat honesto (documentado, sem mudança):** o `HomeController` **refiltra** no front alguns KPIs exibidos (`TotalPlantoes`, `PlantoesConfirmados`, `PagamentosPendentes`, `ValorPendente`, `ValorPagoMes`) por período/hospital/especialidade, enquanto o `TimeCriterion` emitido pela API descreve o cálculo **estado-atual** da fonte. Exibimos as definições **como a fonte as reporta**; alinhar texto de período quando houver consolidação de métricas (B9/B18). Nenhuma regra inventada nesta fatia.
4. **Meu Dia UTC "today" + facade (B17):** invariado.

## 6. Fila seguinte (R8) — auditoria além do top-10

§27-205: **BI (B20) + Dashboard (B21) em tela entregues**; MeuDia/CommandCenter/Admin360 pendentes. §27-208: pendente (agora com base em 2 telas p/ linkar). Próximos: emitir+exibir definições nas 3 telas restantes, ou seguir §27/§28 pela matriz, ou janela viva de homologação / push.

## 7. Gotchas aprendidos (reutilizáveis)

- **RZ1010 — `@{ }` dentro de um bloco transição:** escrever `@{ var x = ...; }` **dentro** de `{ }` já aberto por `@if {}`/`else { }`/etc. dá `error RZ1010: Unexpected "{" after "@"`. Dentro de um bloco de código já aberto, usa-se o C# puro (sem `@`). Padrão mais seguro: **hoistar** a variável para o `@{ }` superior do topo da view (onde as demais variáveis locais vivem) e só usar o nome no markup.
- **Mirror record posicional + campos novos:** para estender um `record` posicional consumido pelo `System.Text.Json` sem quebrar construções existentes, adicione parâmetros **com valor padrão** no fim (default `null`/const). `Array.Empty<T>()`/`new List<T>()` **não** são válidos como default de parâmetro opcional (não são constante de compilação) — use `null` (nullable) ou const.
- **Web não referencia Api:** ao precisar de um tipo da API na Web MVC, confirme o `<ProjectReference>`; se ausente, crie um **mirror** com o mesmo shape de JSON (padrão "EspelhoDtosApiWeb", dívida B11) em vez de referenciar o assembly.
- **Deadlock (40P01) em teste de escrita ≠ regressão:** "impasse detectado" é contention de banco sob carga paralela (RAM baixa, box compartilhado). Diagnóstico: passou nas outras passes com o mesmo binário? + isolar 5× (sem concorrência) antes de concluir.
