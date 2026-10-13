# R8-B17 — `MeuDiaService` × `ProductivityActionService`: validação da facade (fonte única) + contrato anti-drift (§29 item 9)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia "`MeuDiaService` facade sobre `ProductivityActionService` (compatibilidade intencional)" (item 9 da fila §29, ordem quebrado→alto impacto: … → B14 → B15 → B16 → **B17**)
- **Data:** 2026-10-13
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica: código dos dois serviços + consumidores)

O varredura (C2.4) classificou `MeuDiaService` como "facade documentada sobre `ProductivityActionService` — candidata a consolidação segura". A validação por fonte canônica confirma que o **Meu Dia NÃO tem fonte própria** — é um adapter fino sobre o **único** serviço de produtividade:

| Camada Meu Dia (`MeuDiaServices.cs`) | Delega para (`IProductivityActionService`) | Observação |
| --- | --- | --- |
| `MeuDiaRepository.ReadAsync(q, ct)` | `productivity.ListAsync(q, ct)` | **passthrough puro** (sem SQL, sem conexão) |
| `MeuDiaService.ObterResumoAsync` / `PendenciasAsync` | `ReadAsync(Today)` | `Today = ProductivityQuery(DueTo: UtcNow+1d, PageSize 25)` |
| `MeuDiaService.AgendaAsync` | `ReadAsync(Module:"CLINICO", DueTo UtcNow+1d)` | módulo clínico fixo |
| `MeuDiaService.IndicadoresAsync` | `productivity.SummaryAsync(ct)` | mesmo resumo derivado |
| `MeuDiaService.AcoesRapidas()` | `productivity.QuickActions()` | mesmo conjunto |

**A fonte canônica real** é `IProductivityActionService` → `IProductivityActionRepository`, onde vive a **única** query derivada relevante: a CTE `with derived … visible … select count(*) filter(where not snoozed …)` que produz os indicadores Active/Critical/Today/Overdue/Snoozed (`ProductivityActionServices.cs:196-205`). Não existe segunda tabela, segunda query nem conexão no caminho Meu Dia.

**A ÚNICA coisa que a facade agrega** (não é lógica de dados): um **padrão de janela fixo** — "hoje" como `DateTimeOffset.UtcNow.AddDays(1)` (UTC, pág. 25; agenda com `Module:"CLINICO"`) — e o **mapeamento de nomes PT-BR** (ObterResumo/Indicadores/Pendencias/Agenda/AcoesRapidas) sobre ListAsync/SummaryAsync/QuickActions.

**Consumidores (comprovado):** o Meu Dia é alcançado SOMENTE por `MeuDiaController` (→ `IMeuDiaService`); a "Central de Ações" é alcançada por `ProductivityActionController` (→ `IProductivityActionService`). As duas superfícies convergem no **MESMO serviço canônico** — exatamente como declara o comentário no topo do arquivo ("Compatibility facade: Meu Dia and Central de Ações share one real, derived source").

**Conclusão:** canônico = `ProductivityActionService`; a facade é **adapter de compatibilidade intencional e documentado**, não uma duplicação de dados. Não há "dupla fonte" a reconciliar.

## 2. Decisão e mudança (sem regra inventada, sem deleção antecipada)

Rito §29 (comparar consumidores → canônico → migrar → regressões):
- **Comparar:** feito (tabela acima). **Canônico:** `ProductivityActionService` (fonte derivada única).
- **Migrar/consolidar:** a varredura chamou de "consolidação segura" — estruturalmente é de baixo risco (a facade delega tudo). Mas **adiada nesta rodada** porque removê-la exigiria re-ligar `MeuDiaController` a `IProductivityActionService` e **relocar o padrão de janela "hoje"** (hoje fica no adapter; sem ele, o controller precisaria montar o próprio `ProductivityQuery`) — um refactor acoplado a comportamento, não apenas deleção de símbolo redundante. O rito desta rodada veda **deleção antecipada**.
- **Regressões/contrato:** entregue **CONTRATO anti-drift** `R8B17MeuDiaFacadeContratoTests` (3 fatos): (1) `MeuDiaRepository` é passthrough do `IProductivityActionService`; (2) `MeuDiaService` só delega — sem SQL inline, sem `NpgsqlConnection`, sem `IProductivityActionRepository` direto; (3) Meu Dia (`MeuDiaController→IMeuDiaService`) e Central de Ações (`ProductivityActionController→IProductivityActionService`) rosam no MESMO serviço canônico. Se alguém der ao Meu Dia uma fonte própria (nova query/tabela/conexão), o contrato falha e força decisão consciente.
- **Zero mudança em produção.**

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Tests/R8B17MeuDiaFacadeContratoTests.cs` | **Novo**: 3 fatos — (1) repositório Meu Dia passthrough do serviço canônico; (2) serviço Meu Dia só delega (sem SQL/conexão/repositório próprio); (3) Meu Dia e Central de Ações no mesmo serviço canônico. |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §29 item 9 (linha 235) → **R8-B17**; resumo "Estado" (§29) refletindo itens 1–9. |

## 4. Evidência (executada)

- **Build sln completa:** EXIT 0 (`BUILD_R8B17_EXIT:0`; apenas avisos pré-existentes).
- **Alvo:** `R8B17MeuDiaFacadeContratoTests` = **3/3 aprovados** (`--filter FullyQualifiedName~R8B17`).
- **Gate phaseA v2:** **52/52, EXIT 0** (RAM livre 0,72 GB ao iniciar).
- **Full suite v2:** **`Total de testes: 1364`** (1361 baseline B16 + 3 novos) em **todas** as 3 rodadas. **full1** e **full3** = limpos 1364/1364; **full2** = 1 falha `SaasComercialB4MatrizTests.B1_Matriz_PlanoInexistente_404Honesto` = **`Npgsql 40P01: impasse detectado`** (deadlock) no **`PurgarAsync()`** (cleanup em `Dispose`) da classe de flake do **catálogo ①** sob pressão de memória/concorrência da suíte (RAM 0,72 GB — a rodada mais apertada) — **isolada e aprovada 5/5** sob memória normal (não regressão).
- **Verificação byte-level:** `git status` = matriz + teste novo (+ doc selado); **nenhum arquivo de produção alterado**.

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Consolidação opcional da facade:** apagar `MeuDiaService`/`IMeuDiaService`/`MeuDiaRepository`/`MeuDiaRepository` e apontar `MeuDiaController` direto para `IProductivityActionService` (montando o mesmo `ProductivityQuery`) é de baixo risco estrutural; fazer junto com uma decisão sobre a janela "hoje" (item 2). Contrato `R8B17…` (fatos 1/2) exige decisão consciente se o Meu Dia vier a ganhar fonte própria.
2. **Janela "hoje" em UTC:** o padrão de período do Meu Dia é `DateTimeOffset.UtcNow.AddDays(1)` — **fuso UTC, não o do usuário** (já sinalizado pela varredura, linha Meu Dia). Corrigir = mudança de comportamento/regra (usar fuso do tenant/usuário), fora do rito desta rodada.
3. **§29 item 10 (próximo na fila):** Cards KPI manuais × partials `_KpiCard`/`_CoverageCard` subutilizados.
4. **Main DB v2339 / homologação viva:** invariado — janela segue em `r6a1b` §7 (+ lote v2340–v2351 + IIS).

## 6. Fila seguinte (R8)

Com §29 item 9 entregue (validação da facade de fonte única + contrato anti-drift), o próximo item da fila §29 = **item 10: Cards KPI manuais × partials `_KpiCard`/`_CoverageCard` subutilizados.** Alternativas: janela viva de homologação (`r6a1b` §7), ou o push da rodada (pendente).

## 7. Gotchas aprendidos (reutilizáveis)

- **"Facade" nem sempre é duplicação de dados — pode ser mapeamento de vocabulário/janela:** confirmar se o wrapper agrega **lógica de fonte** (query/tabela/conexão própria) vs. só **re-nomeia + aplica um padrão** de consulta. Aqui o Meu Dia só re-nomeia PT-BR + fixa a janela "hoje"; a fonte é única. Só há redundância a consolidar se houver segunda fonte.
- **Provar "sem fonte própria" por ausência + delegação:** varrer o arquivo da facade por `SELECT`/`NpgsqlConnection`/repositório-alvo (ausentes) E pelas chamadas de delegação (presentes) — as duas direções juntas fecham o invariante.
- **Catálogo ① insiste sob RAM baixa (0,72 GB):** a mesma classe `SaasComercialB4MatrizTests` falhou em 3 rodadas seguidas com métodos DIFERENTES (`B3_DecidirDowngrade_…409`, agora `B1_Matriz_PlanoInexistente_404Honesto`), sempre `40P01` em `PurgarAsync()`/`Dispose` — assinatura de flaky de concorrência/memória; isolar (5×) prova não-regressão.
