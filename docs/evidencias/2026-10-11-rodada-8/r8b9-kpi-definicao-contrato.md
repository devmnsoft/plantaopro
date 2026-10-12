# R8-B9 — Contrato de definição das 5 calculadoras de KPI (§27 — estender padrão Operacional)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia "5 calculadores de KPI sobrepostos" (item 1 da fila pós-R7-D, ordem quebrado→alto impacto: … → B7 → B8 → **B9**)
- **Data:** 2026-10-12
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (roteiro `r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada (prompt: "Não publique, faça push ou merge como consequência automática desta tarefa").

## 1. Achado (fonte canônica)

- Matriz `r8a0-auditoria-inicial-matriz-estado.md`, linha "Definição por indicador" (§27, linha 205): estado **parcial** — apenas Relatórios Operacionais expunha `Definition` por indicador; maioria dos painéis/KPIs sem definição. Ação = "fatia §27: estender padrão Operacional"; validação = "contrato de definição".
- Varredura `r8a0-varredura-indicadores-design-redundancias.md`, Área A/A1 (tabela painéis × definição): **apenas uma família** tinha definição explícita. E §29 item 1 = as **5 calculadoras de dashboard/KPI sobrepostas**: `BiService`, `DashboardPremiumService`, `Fase6BiIntegracoesService.DashboardAsync`, `DashboardService`/api-dashboard + v114/operacional (referência).

O padrão de referência já existia em HEAD (`OperationalReportService`): campo `Definition` textual por indicador + `TimeCriterion` no resultado ("intervalo semiaberto no fuso persistido") — único lugar onde fórmula/fonte/período/fuso era auto-descritiva e renderizada na UI.

Escopo desta fatia: **estender esse padrão às 5 calculadoras** (§29 item 1), tornando-as **auto-descritivas e comparáveis**, para que a futura consolidação (canônico → migrar consumidores → regressões) siga o rito §29 sobre uma base legível. **Nenhuma deleção nem merge nesta rodada** — só metadados aditivos/back-compat. As definições gravadas derivam do **SQL real** de cada serviço (nada inventado): KPIs de **período** marcam "(período) no fuso consultado", os de **estado** marcam "(estado atual)".

## 2. Decisões (sem regras inventadas)

1. **Unidade canônica compartilhada**: novo `record KpiDefinicao(string Codigo, string Label, string Definicao)` (`KpiDefinicao.cs`). `Codigo` = chave estável (nome da propriedade/alias do SELECT); `Label` = rótulo pt-BR; `Definicao` = fórmula/fonte/período/fuso/filtros fiéis ao SQL.
2. **Aditivo/back-compat por shape do modelo**: nos DTOs classe (mapeados por nome, nunca construídos por posição) → **coleção paralela** `IReadOnlyList<KpiDefinicao> Definicoes`; no record de lista (Premium) → **cauda opcional** `string Definicao=""` em `DashboardKpiDto`. Zero mudança de lógica SQL; consumo intacto (desserialização tolerante por nome; Dapper mapeia classes por nome, records por posição → cauda segura; a única construção posicional de `DashboardKpiDto` é a `AddCount`, que agora encaminha o texto).
3. **Critério de tempo explícito onde ausente**: BI e dashboard-API ganham `string TimeCriterion` (espelho de `OperationalReportResult.TimeCriterion`). BI já tinha `Periodo`+`Fuso` no resultado; `TimeCriterion` sintetiza "janela do mês {Periodo} no fuso {Fuso}". Dashboard-API não expunha critério algum (usa `now()` do servidor) → agora declarado. Premium declara o critério dentro de cada `Definicao` ("sem filtro de período").
4. **Referência e Fase6 sem mudança de código**: `OperationalReportService` (já tem `Definition`+`TimeCriterion`) e `Fase6BiIntegracoesService` (já tem `Explicacao` por KPI, populada) são apenas **verificados pelo contrato** (reflexão sobre a referência; comportamento na Fase6), não alterados.
5. **Fix de raiz do parâmetro `mesRef` (BI)**: o envio de `DateOnly?` via Dapper/Npgsql lançava `NotSupportedException: "The member mesRef of type System.DateOnly cannot be used as a parameter value"` de forma **determinística quando o fato era o 1º uso do pool num processo frio** (pool aquecido passava). Corrigido enviando `mesRef` como **string `yyyy-MM-dd`** — cast `::date` no SQL preserva a data exata (sem deslocamento de fuso: `null`→`NULL::date`, `"2026-09-01"`→`2026-09-01`). Semântica idêntica p/ todos os chamadores (validado pelos testes BI existentes nulo + mês + inválidos).
6. **Sem migration, sem nova tabela, sem regra de domínio nova, sem chave i18n.** Web inalterado (renderização em tela = dívida, §5).

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Api/KpiDefinicao.cs` | **Novo**: `record KpiDefinicao(string Codigo, string Label, string Definicao)` — unidade canônica de definição de indicador (padrão Operacional estendido). |
| `backend/PlantaoPro.Api/Models.cs` | `BiResumoExecutivoDto` + `DashboardDto` ganham `IReadOnlyList<KpiDefinicao> Definicoes` + `string TimeCriterion` (propriedades aditivas; Dapper mapeia por nome, construção nunca posicional). |
| `backend/PlantaoPro.Api/BiServices.cs` | `GetResumoExecutivoAsync` popula `row.Definicoes` (12 indicadores) + `row.TimeCriterion`; novo `private static Definicoes()`; **fix raiz**: parâmetro `mesRef` passa como `string yyyy-MM-dd` (era `DateOnly?`). |
| `backend/PlantaoPro.Api/Data.cs` | `DashboardService.GetAsync` popula `ind.Definicoes` (13 indicadores) + `ind.TimeCriterion`; novo `private static Definicoes()`. |
| `backend/PlantaoPro.Api/DashboardPremiumService.cs` | `DashboardKpiDto` ganha cauda `string Definicao=""`; `AddCount` ganha `string definicao` e a encaminha; os 4 KPIs passam o texto (contagem `reg_status='A'` por escopo, sem período). |
| `backend/PlantaoPro.Tests/R8B9KpiDefinicaoContratoTests.cs` | **Novo**: 5 fatos — BI (Success + Periodo/Fuso/TimeCriterion + cobertura de códigos + nenhuma definição vazia), dashboard-API (Success + TimeCriterion + 13 códigos + nenhuma vazia), Premium (todo KPI `Fonte=="real"` + `Definicao` não vazia), Fase6 (todo indicador `Explicacao` não vazia), Operacional (padrão de referência íntegro via reflexão). |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | Linha "Definição por indicador" (§27) → **corrigido (R8-B9)** (residual = renderização Web + consumidores fora do §29-1, dívida registrada); §27 linha 208 (drill-down) → cruzamento p/ a base das calculadoras. |

## 4. Evidência (executada)

- **Build sln completa:** EXIT 0 (0 erros) — record novo + propriedades aditivas + fix do parâmetro recompilaram todos os projetos/chamadores.
- **Alvo:** `R8B9KpiDefinicaoContratoTests` = **5/5 aprovados** (run isolado).
- **Prova do fix `mesRef` (cold-start):** antes do fix, o fato BI falhava com 500 (`mesRef … DateOnly … cannot be used as a parameter value`) quando era o **primeiro uso do pool num processo frio** (reproduzido de forma determinística); após o fix → **1/1 aprovado** no mesmo cenário isolado.
- **Regressão BI (mesmo parâmetro):** `Saude360R4B9PlantoesMeuDiaBiTests.Bi_*` (nulo + mês `2026-09` + meses inválidos) + `R8B9*` juntos = **8/8 aprovados** — o fix string preservou janela/fuso/validação existente.
- **Gate phaseA v2 (pipeline):** contratos Login/R7E1/R7A1/R7OB — **52/52, EXIT=0**.
- **Full ×3 (pipeline v2):** total agora **1340** (1335 baseline + 5 novos). full1 **1340/1340 EXIT=0**; full3 **1340/1340 EXIT=0**; full2 = **1 falha transitória** em `R7OBOnboardingPreferenciasIdiomaTests.O1_Iniciar_MaterializaEtapaDeIdioma_OpcionalNoNucleo_Idempotente` — classe **já do catálogo de flake ①**, **limpa em full1/full3 e 5/5 em execução isolada** (regra ①), alheia ao escopo desta fatia. **Sem flake novo — catálogo inalterado.**
- **Higiene do repositório:** `RepositoryHygieneContractTests` aprovada (nas full verdes).
- **Verificação byte-level:** tokens presentes nos arquivos novos/mudados — `record KpiDefinicao(string Codigo, string Label, string Definicao)`, `IReadOnlyList<KpiDefinicao> Definicoes` (Models ×2), `string TimeCriterion` (Models ×2), `private static IReadOnlyList<KpiDefinicao> Definicoes()` (Bi/Data), `record DashboardKpiDto(...,string Definicao="")`, `AddCount(...,string definicao)` + `new DashboardKpiDto(titulo,total,"real",definicao)`, `mesRef=mesRef?.ToString("yyyy-MM-dd")`; 12 definições no BI + 13 no dashboard-API.

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Renderização em tela (Web):** os painéis BI/Premium/Dashboard ainda **não exibem** `Definicoes` ao usuário na UI — a exposição hoje é no **contrato/API**. Faltaria o equivalente à exibição do Relatórios Operacionais (card `<p>@i.Definition</p>`). Mesma categoria de dívida UI da B8/B7 (passo na tela), sem novas chaves i18n por enquanto.
2. **Consumidores fora do §29-1:** Meu Dia (`MeuDiaServices`/`ProductivityActionServices`), Manager Command Center (`ManagerCommandCenterService`), Resumo Admin 360 — não fazem parte das 5 calculadoras (§29 item 1); suas definições seguem nas jornadas próprias (linhas §27 206/208 cobrem Meu Dia/Bi e drill-down).
3. **Consolidação das 5 calculadoras (§29 item 1, rito):** esta fatia as tornou comparáveis/auto-descritivas; o merge real (escolher canônico → migrar consumidores → regressões) segue o rito §29, sem deleção antecipada.
4. Base principal em v2339: lote de deploy segue incluindo **v2340–v2350** (invariado nesta fatia — sem migration).

## 6. Fila seguinte (R8)

Com §27 linha 205 entregue (contrato de definição das 5 calculadoras) e §29 item 1 tornado consolidável, o próximo item da ordem quebrado→alto impacto de `r8a0` §4 = **`cid_tabela` criada divergente em 6 arquivos** (item 2). Alternativas: janela viva de homologação (`r6a1b` §7), a dívida UI de renderização de definições (forma completa do passo na tela), ou o push da rodada (pendente).
