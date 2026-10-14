# R8-B22 — Definição e rastreabilidade de KPIs: MEU DIA agora EMITE e EXIBE as definições (§27 linha 205 residual, parte 3)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia da **auditoria ALÉM do top-10 §29**: §27 linha 205 *residual* (renderização em tela das definições de KPI). Terceira tela (após BI/B20 e Dashboard/B21): **Meu Dia** — o painel diário mais usado.
- **Data:** 2026-10-14
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica: código de Meu Dia)

Os KPIs de Meu Dia (Pendências/Críticas/Hoje/Atrasadas) são computados pela API em `ProductivityActionService.SummaryAsync` — **um único `summarySql`** (`ProductivityActionServices.cs:196-205`) — e devolvidos por `api/produtividade/meu-dia` como o campo `summary` (`ProductivitySummaryDto`), que a BFF Web desserializa em `ProductivitySummaryViewModel`. Diferente de BI/Dashboard, Meu Dia estava **fora do escopo B9** e **não emitia** definições. Os filtros são claros e fiéis:
- `Active` = `count(*) filter(where not snoozed)` (visível, não adiado, não descartado, no escopo);
- `Critical` = `... and Priority='CRITICA'`;
- `Today` = `DueAt` dentro de `date_trunc('day',now())` (dia atual **no fuso do servidor**);
- `Overdue` = `DueAt < now()`.

## 2. Decisão e mudança (EMITE na API + EXIBE na Web; sem nova consulta/número/regra clínica)

- **API — emissão fiel ao SQL:** 
  - `ProductivitySummaryDto` ganha `Definicoes` + `TimeCriterion` como **`{ get; init; }` não-posicionais** (o ctor posicional de 5 colunas usado pelo Dapper permanece inalterado — evita ambiguidade de mapeamento);
  - `ProductivityActionController.PageAsync` shapeia `summary = (await service.SummaryAsync(ct)) with { Definicoes = DefinicoesMeuDia(), TimeCriterion = … }`. O texto de cada indicador descreve **exatamente** os filtros do SQL (`Priority='CRITICA'`, `DueAt<now()`, janelas em `now()` servidor) — nada inventado; o "escopo" é descrito genericamente ("ações derivadas das entidades de origem, no escopo de permissões do usuário") pois a composição fina dos itens vem de `DerivedSql` (fora desta leitura).
- **Web — espelho + renderização:** 
  - `ProductivitySummaryViewModel` ganha `Definicoes` (`KpiDefinicaoDto` — reuso do espelho criado na B21) + `TimeCriterion` (a Web não referencia o projeto Api);
  - `MeuDia/Index.cshtml`: no ramo de sucesso (o erro já cai no `state-panel`), um bloco `<details>` **"Como cada indicador é calculado"** (server-side, codificação HTML automática do Razor) com `TimeCriterion` + uma linha por indicador; lista vazia → estado explícito. Variável **hoistada** para o `@{ }` superior (lição da RZ1010 da B21).
- **Escopo:** só **Meu Dia**. **CommandCenter** e **Admin360** seguem pendentes (também fora do escopo B9; exigiriam emitir definições no backend de cada um).

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Api/ProductivityActionModels.cs` | **Produção:** `ProductivitySummaryDto` + `Definicoes`/`TimeCriterion` (`{ get; init; }`). |
| `backend/PlantaoPro.Api/Controllers/ProductivityActionController.cs` | **Produção:** `PageAsync` anexa as definições ao `summary` + `DefinicoesMeuDia()`/const fiéis ao SQL. |
| `backend/PlantaoPro.Web/Models/ProductivityViewModels.cs` | **Produção:** `ProductivitySummaryViewModel` + `Definicoes`/`TimeCriterion` (espelho). |
| `backend/PlantaoPro.Web/Views/MeuDia/Index.cshtml` | **Produção:** bloco `<details>` "Como cada indicador é calculado". |
| `backend/PlantaoPro.Tests/R8B22MeuDiaDefinicoesContratoTests.cs` | **Novo:** 3 fatos source-read — API emite (fiel ao SQL) · VM Web carrega · view renderiza. |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §27 linha 205 → + Meu Dia (B22, com emissão); residual = CommandCenter/Admin360. |

## 4. Evidência (executada)

- **Build sln completa:** EXIT 0 (**compilação com êxito** — ambos os projetos e as duas views Razor compilam limpo).
- **Alvo R8B22:** `R8B22MeuDiaDefinicoesContratoTests` = **3/3 aprovados** (`--filter FullyQualifiedName~R8B22`).
- **Gate phaseA v2:** **EXIT 0** (RAM livre 0,67 GB ao iniciar).
- **Full suite v2:** **Total de testes: 1379** (baseline 1376 + 3 novos) em TODAS as 3 rodadas. full1 + full2 **limpos**; full3 teve **1** falha — `R7OBOnboardingPreferenciasIdiomaTests.O2_…` — com `Npgsql … 40P01: impasse detectado` (**deadlock**):
  - classe de flake **conhecida** (catálogo ① `R7OBOnboardingPreferenciasIdiomaTests`); erro de **banco**, não de assert; full1+full2 passaram com o **mesmo binário**;
  - **isolado = 5× 5/5** → **transiente**.
  - **Observação de padrão (3º caso):** B20 (`ProvisionamentoB5.P1`), B21 (`Saude360R4B8.T09`) e B22 (`R7OB.O2`) — cada suíte sob **<1 GB de RAM livre** produziu exatamente **um** deadlock aleatório em teste de escrita na última passada. Assinatura consistente da contention no box compartilhado de RAM baixa; **não** regressão (isolar 5× sempre = 5/5).
- **Verificação byte-level:** `git status` = 2 arquivos Api + 2 arquivos Web alterados + teste novo (+ docs); nenhum outro arquivo de produção tocado.

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Renderizar `Definicoes` nas demais telas** (§27-205 residual remanescente): **CommandCenter** (`ManagerCommandCenterService`/`CommandCenterSummary`) e **Admin360** (várias áreas). Como Meu Dia, exigem **emitir** definições no backend de cada uma (definir as fórmulas fiéis ao SQL) antes de exibir. Seguem pendentes junto à consolidação de métricas (B9/B18).
2. **Drill-down §27-208:** segue proposta/pendente (B19) — agora **três** telas (BI + Dashboard + Meu Dia) exibem definições (base para linkar "ver registros").
3. **Caveat honesto (documentado, sem mudança):** o texto de "escopo" dos indicadores de Meu Dia é genérico porque a composição exata dos itens visíveis vem de `DerivedSql` (CTE) — não detalhada aqui. Alinhar quando houver consolidação de métricas (B9/B18). Janela "hoje/atraso" usa `now()` no fuso do servidor (mesmo caveat B17 "Meu Dia UTC today"). Nenhuma regra inventada nesta fatia.
4. **Meu Dia UTC "today" + facade (B17):** invariado.

## 6. Fila seguinte (R8) — auditoria além do top-10

§27-205: **BI (B20) + Dashboard (B21) + Meu Dia (B22) entregues**; CommandCenter/Admin360 pendentes. §27-208: pendente (base em 3 telas p/ linkar). Próximos: emitir+exibir definições nas 2 áreas restantes, ou seguir §27/§28 pela matriz, ou janela viva de homologação / push.

## 7. Gotchas aprendidos (reutilizáveis)

- **Record DTO consumido pelo Dapper + campos novos:** prefira **`{ get; init; }` não-posicionais** (corpo do record) a parâmetros posicionais opcionais — assim o ctor posicional que o Dapper resolve **por colunas do SELECT** permanece inalterado (evita risco de o Dapper escolher um ctor de N+2 parâmetros). Depois, popula-se com `with { … }` (init funciona com `with`).
- **Emitir definição = descrever o filtro, não inventar regra:** o texto de cada KPI espelha literalmente o `filter(where …)` do SQL (`Priority='CRITICA'`, `DueAt<now()`). O "escopo" é descrito pelo mínimo que se pode afirmar sem ler toda a CTE de origem ("no escopo de permissões do usuário"). Se não leu a fonte fina, não detalhe.
- **Fatiamento 2-projetos (emitir + exibir):** quando a tela recebe contagens via BFF (API→JSON→mirror Web), exibir definições exige: (a) campo no DTO da API (init), (b) populosar na controller/serviço, (c) campo no mirror Web, (d) renderizar na view. Verificar cada elo do JSON com o mesmo nome (case-insensitive) para o STJ fazer o mapeamento.
- **Deadlock (40P01) recorrente na última passada sob RAM baixa:** em box compartilhado <1 GB livre, espera-se ~1 deadlock aleatório por suíte completa em teste de escrita DB. Confirmação rápida: passou nas outras passes c/ o mesmo binário + isolar 5× (sem concorrência) = transiente.
