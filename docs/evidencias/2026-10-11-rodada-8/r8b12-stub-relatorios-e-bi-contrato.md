# R8-B12 — Placeholder de relatório duplicado ×5 + views-fantasma do BiController: canônico parcial + contrato (§29 item 4)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia "Placeholder 'Sem dados no período' duplicado em 5 views + BiController (views-fantasma)" (item 4 da fila §29, ordem quebrado→alto impacto: … → B10 → B11 → **B12**)
- **Data:** 2026-10-13
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica)

- Matriz `r8a0-auditoria-inicial-matriz-estado.md` §29 item 4 (linha 230): **"Placeholder 'Sem dados no período' duplicado em 5 views + `BiController` 8 ações/4 views (views-fantasma)."** Varredura `r8a0-varredura-indicadores-design-redundancias.md` (C4/C7.2/C7.3).
- **Sub-partida A — 5 relatórios-stub idênticos:** `Views/Relatorios/{Cobertura,Convites,ProdutividadeMedica,Sla,FaturamentoSaas}.cshtml` reproduziam **byte-a-byte** o mesmo markup de 6 linhas: `_PageHeader` (mesma descrição/icone; só o título varia) + alert de filtros + notice "Indicadores não disponíveis" + tabela vazia `Sem dados no período selecionado.` + botão "Baixar Excel" bloqueado. Único diferencial entre eles = o literal do título.
- **Estas rotas são VIVAS (não código morto):** ligadas dinamicamente pelo catálogo `Views/Relatorios/Index.cshtml` (`asp-action="@report.Action"`) e por `SaasDashboard/Index.cshtml` (`FaturamentoSaas`). São relatórios **ainda sem fonte de dados** (stubs intencionais).
- **Sub-partida B — `BiController` (Web):** a leitura real do controller (não "8/4") = **11 ações → 5 views**: 5 com view própria (`Index`, `Financeiro`, `Medicos`, `Clientes`, `Sla`) e **6 aliasando `View("Index")`** (`Operacional`, `Clinico`, `Convenios`, `Saas`, `Widgets`, `Alertas`). `Bi.Index` **NÃO é fantasma** — é o dashboard "BI Executivo" funcional (fetch `/api/bi/resumo-executivo`). Os outros 4 são stubs de 1 linha "em evolução".
- **Wiring:** `grep asp-controller="Bi"` / `href="/Bi` / JS = **0** referências → as rotas BI não estão ligadas em nenhum menu/link (só por URL direta). (Há um `BiController` **distinto na Api** — `ControllerBase`; fora do escopo. O item 4 mira o da **Web**.)

## 2. O que foi feito nesta fatia

- **(A) Canônico + migração (consolidação concreta):** extraído o markup repetido num partial canônico `Views/Shared/_RelatorioStub.cshtml` (fonte **única** do placeholder de vazio + notice + botão bloqueado); as 5 views migradas para ele (cada uma = definir `ViewData["Title"]` + `<partial name="_RelatorioStub" />`). O título vem de `ViewData["Title"]` — o **mesmo** string que o `_PageHeader` original usava → **output byte-equivalente**. O marker `Sem dados no período selecionado` agora ocorre **uma única vez** (no partial) e some das 5 views.
- **(B) Inventário classificado + contrato (sem remoção física):** contratou-se que toda ação do `BiController` resolve para uma view `.cshtml` **existente** (integridade de rota — view renomeada/errada vira rota fantasma) e fixou-se o conjunto de views distintas `{Index, Financeiro, Medicos, Clientes, Sla}` + os 6 aliases para `Index` + o "em evolução" dos 4 stubs. A remoção física dos ghosts/aliases = pendência (roadmap de sub-páginas BI + wiring de menu), documentada.

## 3. Decisões (sem regras inventadas)

1. **Por que consolidar fisicamente aqui (vs. adiar como B10/B11):** o rito §29 manda "comparar consumidores → canônico → migrar → regressões". Estas 5 views são **100% idênticas exceto o título** — há uma forma canônica mecânica e inequívoca (partial), com risco baixo e verificado pelo build (Razor compila o partial + as views). Diferente de B10 (merge de DDL em 6 blocos, produto-shaped) e B11 (merge físico de tipos BFF) — onde a forma canônica ainda depende de decisão — aqui ela é óbvia.
2. **Por que NÃO remover as rotas/views BI fantasmas:** remover rota é mudança de comportamento; os aliases fazem `/Bi/X` cair no dashboard (inócuos) e os stubs comunicam "em evolução". Nenhuma está ligada em menu hoje, mas o wire/remoção depende do roadmap das sub-páginas BI → segue pendência, **guardada por contrato** (não por deleção antecipada).
3. **Título via `ViewData["Title"]` no partial** (não por `model=`): evita a ambiguidade do Razor de tratar um literal `model="…" com espaço` como expressão C# (quebrou o build no 1º attempt — ver gotcha) e preserva exatamente o valor que o markup original já usava.
4. **Sem DDL, sem novo tipo, sem mudança de rota.** Mudanças restritas a: 1 novo partial + 5 views migradas + 1 novo arquivo de teste (+ matriz/doc).

## 4. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Web/Views/Shared/_RelatorioStub.cshtml` | **Novo**: partial canônico — único lugar do placeholder "Sem dados no período selecionado" + notice + botão bloqueado; lê o título de `ViewData["Title"]`. |
| `backend/PlantaoPro.Web/Views/Relatorios/{Cobertura,Convites,ProdutividadeMedica,Sla,FaturamentoSaas}.cshtml` | Migrados: 6 linhas → 2 (definir título + `<partial name="_RelatorioStub" />`). Output byte-equivalente. |
| `backend/PlantaoPro.Tests/R8B12StubRelatoriosEBiContratoTests.cs` | **Novo**: 3 fatos — (1) o marker de vazio vive **só** no partial canônico e as 5 views delegam a ele (DRY, single-source); (2) toda ação do `BiController` resolve para view `.cshtml` existente (integridade de rota); (3) inventário BI fixo (5 views distintas + 6 aliases + 4 stubs "em evolução"). |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §29 item 4 (linha 230) → **R8-B12**; resumo "Estado" (§29) refletindo itens 1+2+3+4. |

## 5. Evidência (executada)

- **Build sln completa:** EXIT 0 (0 erros; apenas avisos pré-existentes) — compila o novo partial + as 5 views migradas (Razor) + o teste.
- **Alvo:** `R8B12StubRelatoriosEBiContratoTests` = **3/3 aprovados** (run isolado, `--filter FullyQualifiedName~R8B12`).
- **Gate phaseA v2 (pipeline):** **52 testes, EXIT=0**.
- **Full ×3 (pipeline v2):** total agora **1349** (1346 baseline + 3 novos). full1 **1349/1349 EXIT=0**; full2 **1349/1349 EXIT=0**; full3 **1349/1349 EXIT=0**. **Sem flake esta rodada** (catálogo ① inalterado; o msg "Falha ao registrar auditoria legada" é log benigno presente também nas execuções de referência, não um teste falho).
- **Verificação byte-level:** o marker "Sem dados no período selecionado" ocorre **uma única vez** (em `_RelatorioStub.cshtml`); **nenhuma** das 5 views o contém e cada uma contém `_RelatorioStub`; `git status` = exatamente os 3 artefatos acima alterados/criados (+ matriz/doc).

## 6. Dívida registrada (fora do escopo desta fatia)

1. **Remoção física / wiring do `BiController` (rito §29):** inventário contratado; remoção pendente do roadmap das sub-páginas BI + decisão de menu (hoje 0 links). Os 6 aliases para `Index` e os 4 stubs "em evolução" seguem até lá.
2. **Os 3 relatórios restantes do catálogo** (`Execucao`, `Apuracao`, `Saas`) — não estão entre os 5 byte-idênticos; cada um é tratado quando ganhar fonte real (este partial cobre só os stubs idênticos).
3. **Main DB em v2339 / homologação viva:** invariado — janela segue em `r6a1b` §7 (+ lote v2340–v2351 + IIS).

## 7. Fila seguinte (R8)

Com §29 item 4 entregue (canônico + migração dos 5 stubs + contrato do `BiController`), o próximo item da fila §29 = **item 5: Menu `_AppSidebar` com `Usuarios.Index` ×2 e `Perfis.Index` ×2 (:75/:76 vs :89/:90) — duplicação visível para super-admin.** Alternativas: janela viva de homologação (`r6a1b` §7), ou o push da rodada (pendente).

## 8. Gotchas aprendidos (reutilizáveis)

- **`<partial … model="literal com espaço">` no Razor:** atributo `model` sem `@` cujo valor é um literal com espaço (ex.: `"Relatório Sla"`) é interpretado como **expressão C#** (`Relatório Sla`) → erro CS1002/CS0103. Solução robusta: não passar `model` — ler de `ViewData["Title"]` dentro do partial (ou usar expressão explícita `model='@("…")'`).
- **String verbatim C# + aspas no regex:** numa string `@"…"`, cada `"` literal vira `""`; ao codificar `(?:"(\w+)")?` o **segundo** `"` também precisa ser duplo → `(?:""(\w+)""?)`. Um `"` único seguido de `)` encerra a string cedo (CS1525/CS1010).
- **Dois `BiController`:** Web (`: BaseWebController`, rotas/views) vs Api (`: ControllerBase`, endpoints). Não confundir — o item 4 é o da **Web**.
- **"8 ações/4 views" da varredura era aproximado:** leitura real = **11 ações/5 views** (6 aliasando `Index`). Sempre recontar no fonte antes de concluir "fantasma".
