# Rodada 14 — Auditoria inicial & plano de sprint (42P08 na listagem de faturas SaaS + degradacao honesta; segunda ocorrencia identica no relatorio SaaS sempre em 500)

Data: 2026-10-16 · Base: `c778469` (ponta do push da Rodada 13) · Itens da fila R14 consumidos: **F13-A** (r13z §7.1 — `GET /api/faturamento-saas/faturas` responde 500 com `Npgsql 42P08`) e **F13-B** (r13z §7.2 — banner de erro do Index de Faturamento SaaS nunca renderizado). Fora de escopo e seguidos na fila: fantasmas do catálogo guard (§7.3), drill-down gaps, exposição em menu, P1b `destino`, Meu Dia UTC, tiles B18, especialidade B16, pg_dump P5.

## 0. Contexto e cadeia do achado

- **r13b2** (janela viva, 20:58–21:00 UTC): com o guard corrigido e telas liberadas, o endpoint `/resumo` respondeu 200 (zeros honestos) mas a lista paginada `/faturas` respondeu **500** — `Npgsql.PostgresException 42P08: não foi possível determinar o tipo de dados do parâmetro $5`, no `ExecuteScalarAsync` (count) em `SaasCommercialController.cs` L605. Reproduzido 3×, com e sem `?status=VENCIDA`; `/inadimplencia` 200.
- Este recon (r14a0) localiza a raiz e revela que **o defeito não é um só**: a mesma violação existe num segundo endpoint, atingido por um caminho que nunca envia o parâmetro ⇒ quebrado para todo mundo (F3).

## 1. Recon da fonte canônica

### 1.1 Fatos medidos

| # | Fato | Evidência |
|---|---|---|
| F1 | Reprodução do 500 com assinatura exata: `42P08` parâmetro `$5`, POSITION 172, na query de count da ação `Faturas` | Logs da Api na janela r13b2 (shell capture); `SaasCommercialController.cs` L605 |
| F2 | Raiz: o handler de casa documenta a regra — `DateOnlyDapperTypeHandler.cs` L23-24: *"Quando o valor é nulo (DateOnly?), o Dapper 2.1.35 envia o parâmetro sem tipo; a proteção contra 42P08 vem dos casts no lado do parâmetro nas consultas (`"@param::date"`)"* | Fonte (comentário de casa) + registro global idempotente em `Program.cs` L25 |
| F3 | **Segunda ocorrência (ACHADO NOVO F-R14-A)**: `RelatoriosSaasController.Faturamento()` (L51-57) usa exatamente o mesmo padrão `(@competencia is null or f.competencia=@competencia)` com `[FromQuery] DateOnly? competencia`; o chamador Web `RelatoriosController.SaasFaturamento` (L45/L67) envia **somente** `page/pageSize` ⇒ `GET /api/relatorios/saas/faturamento` chega sempre com `competencia=null` ⇒ a tela `/Relatorios/SaasFaturamento` responde **500 sempre** (envelope "Não foi possível carregar relatório SaaS." via `ExecutarRelatorioAsync` L141-148). O caminho de **exportação** (`{tipo}/exportar` L98-113 → `ObterDadosAsync` L152-160) usa SQL própria **sem** `@competencia` ⇒ exporta sem esse modo de falha | Fonte |
| F4 | Varredura sistemática do padrão `(@x is null or …=@x)` em `PlantaoPro.Api` + `PlantaoPro.Infrastructure`: todas as demais ocorrências usam `Guid?`/`string?`/`DateTime?` (tipados pelo protocolo mesmo nulos) ou já trazem cast nos precedentes (`ExecutionConferenceService` L72-73 e `FechamentoOperacionalService` L39: `p.data_inicio::date>=@inicio`) ⇒ **exatamente 2 violações** no repo, ambas em filtros de data de `faturas_saas` (L603 do controller comercial e L56 do relatorio) | Grep versionado desta sessão |
| F5 | Degradacao desonesta (F13-B confirmado e dimensionado): `FaturamentoSaasController.Index` seta `ViewBag.ErrorMessage` (L26), mas `Views/FaturamentoSaas/Index.cshtml` **nunca o lê** — com a falha da lista, a view renderiza `_EmptyState` *"Nenhuma fatura encontrada / Ajuste os filtros ou gere faturas mensais"* (L52-55): **vazio falso orientando o usuário a gerar faturas quando a query errou**. Padrão de casa: os outros 18 casos leem `ViewBag.ErrorMessage` e renderizam alert com `role="alert"`. `Views/Relatorios/Saas.cshtml` também não lê o ViewBag setado em L68 do controller (mesma doença; resíduo baixo após o F3 consertar o 500, mas fora do princípio "sem vazio falso") | Fonte (grep + leitura das views) |
| F6 | Nota lateral (novo achado F-R14-B): `ExecutarRelatorioAsync` calcula `total = items.LongCount()` (L142) — contagem da **página atual**, não do total geral ⇒ paginação do relatório exibe total truncado em `pageSize` quando há mais linhas. Classe distinta (cálculo, não tipagem) ⇒ fila, não entra nesta rodada | Fonte |

Mapeamento do `$5` (F1): o args object da ação `Faturas` declara na ordem `global, scopedClienteId, requestedClienteId, status, competencia, s, offset`; o Dapper indexa os nomes por ordem e a query de count referencia apenas os primeiros cinco ⇒ `$5 = @competencia` (o único sem tipo resolvido quando nulo).

### 1.2 Fluxo de falha observado (janela r13b2)

1. Guard (corrigido na R13) libera `FaturamentoSaas` com contrato+permissão.
2. Web → `api/faturamento-saas/resumo` **200** (zeros reais do tenant).
3. Web → `api/faturamento-saas/faturas?page=1&pageSize=20` **500** (42P08 no parse do count).
4. Web loga `warn: Falha ao consultar API paginada … Status:500` e degrada: KPIs de resumo (200) + tabela vazia silenciosa + empty-state enganoso (F5).

## 2. Decisões versionadas

- **D1 — Correção pelo cast, convenção documentada de casa.** Nas duas ocorrências (F2/F4), tipar a ocorrência de comparação: `f.competencia=@competencia::date` (cast no lado do parâmetro, como o comentário do handler manda). O lado `@competencia is null` não precisa de tipo (a resolução global do parâmetro sai da ocorrência com cast — precedentes operacionais em produção do próprio repo, F4). **Rejeitado:** alterar `DateOnlyDapperTypeHandler` para sobrepor `DbType` no null — mudaria o comportamento global de todo parâmetro `DateOnly` do processo, quando a convenção já existe, é documentada e tem precedentes.
- **D2 — Degradacao honesta nas duas views.** Renderizar `ViewBag.ErrorMessage` no padrão de casa (alert `role="alert"`) e **suprimir o vazio falso** quando houve erro (lista vazia por falha ≠ "nenhuma fatura encontrada"). Afeta `Views/FaturamentoSaas/Index.cshtml` e, pela mesma classe, `Views/Relatorios/Saas.cshtml` (F5). As mensagens vêm do envelope da API existente — **nenhuma literal nova de UI**, resx ×4 (650 chaves) intocados.
- **D3 — Contrato anti-regressão (classe nova `R14…ContratoTests`, ~5 fatos):**
  1. fonte: as 2 ocorrências do filtro de data de `faturas_saas` tipadas com `::date` (anti-regressão da convenção);
  2. integração (banco real, via factory de host usado pelos testes DB da suíte): `GET /api/faturamento-saas/faturas` **sem** `competencia` (escopo não-global) → **200**;
  3. integração: `GET /api/relatorios/saas/faturamento` (sem `competencia`) → **200** (antes: sempre 500);
  4. view: Index de FaturamentoSaaS lê `ViewBag.ErrorMessage` e não exibe o empty-state enganoso em estado de erro;
  5. view: mesma verificação para `Views/Relatorios/Saas.cshtml`.
- **D4 — Homologação b2 viva (mesma disciplina r12/r13):** subida Api/Web; captura pré-grant (estado atual); grant mínimo reversível tag **`HOMOLOG_R14`** = 1 `tenant_modulos` (`FATURAMENTO_SAAS`) + reativação `FATURAMENTO_SAAS.VER` — **sem** `BI` (caminho do card já provado na R13 e fora do escopo desta rodada) e **sem** novo módulo de relatórios (`RELATORIOS` já está contratado/habilitado no SV — constará no snapshot pré-grant); ciclo pós-grant: `/FaturamentoSaas/Index` (vazio **verdadeiro**: banner ausente + empty-state legítimo ou tabela), `?status=VENCIDA`, `?competencia=YYYY-MM` (filtro efetivamente filtrando), `/Relatorios/SaasFaturamento` **200** (antes: sempre 500); artefato de prova de raiz: script SQL que roda a query de count antiga (parâmetro nulo sem cast → 42P08) e a nova (→ ok); rollback verbatim + net-zero (efetivos SV 18→19→18).
- **D5 — Sem DDL, sem mudança no handler, sem mudança de contrato de API** (mesma assinatura de endpoint; apenas SQL interna + views). `access_catalog_version` permanece `v2149`.
- **Fila:** F-R14-A (segunda ocorrência) entra nesta rodada (D1/D3); F-R14-B (total de paginação = count da página) vai para a fila como P3 novo.

## 3. Slices planejados

| Slice | Entregável | Gate |
|---|---|---|
| **r14b1** | 2 casts (`SaasCommercialController.Faturas` L603, `RelatoriosSaasController.Faturamento` L56) + 2 views (banner + supressão de vazio falso) + classe de contrato ~5 fatos | build Release EXIT 0 + suíte full ≥ 1447 + novos fatos, 0 falha (flake família ① ⇒ isolate 5×) |
| **r14b2** | Homologação viva: capturas DOM (Index vazio verdadeiro, filtros VENCIDA + competência, tela do relatório antes-sempre-500 agora 200), artefato de prova de raiz (42P08 antigo vs cast novo), grants `HOMOLOG_R14` + rollback net-zero | Capturas DOM + verificação SQL final (efetivos pré/pós/rollback) |
| **r14z** | Relatório final consolidado + fila R15 | — |

## 4. Riscos e mitigações

| Risco | Mitigação |
|---|---|
| Resolução de tipo no PG: se o walker visitar a ocorrência `is null` antes da com cast e ainda assim 42P08 | Fallback pronto: cast também na ocorrência de `is null` (ou `coalesce`); os fatos de integração contra banco real (D3.2/D3.3) validam **antes** do commit |
| Flake família ① (nesta rodada já pegou o baseline em `G3`) | Isolate 5× antes de tratar como regressão (regra de casa) |
| Sessão in-memory do Web (`VERIFICACAO_INDISPONIVEL` pós-restart) | Re-login no fluxo da b2 (lição R12/R13) |
| Lock de DLLs Release em build com servidores no ar | Matar Api/Web antes de qualquer build Release (lição R13) |

## 5. Critérios de fechamento da rodada

1. r14b1 commitado com gate limpo (base 1447 + fatos novos, 0 falha além da família ① classificada).
2. Ao vivo: relatório SaaS de faturamento (antes sempre 500) responde 200; `/FaturamentoSaas/Index` em vazio **verdadeiro**; filtros `status` e `competencia` funcionando; artefato de raiz (antiga 42P08 × nova ok).
3. Banco devolvido ao estado pré-rodada (efetivos SV = 18; 0 resíduo `HOMOLOG_R14`).
4. r14z com fila R15 priorizada (incluindo F-R14-B novo).
5. Push somente com autorização explícita ao fim da rodada.

## 6. Baseline (c778469)

- **Build Release**: EXIT 0, 0 erros (tempo ~47 s incremental).
- **Suíte full (Release, --no-build)**: `Total de testes: 1447` · **Aprovados: 1446** · **Falhas: 1** →
  `SaasGovernancaB6Rodada4Tests.G3_Trial_ValidoOpera_E_VencidoBloqueiaComRegistro` com
  `Npgsql.PostgresException 40P01: impasse detectado` em `PurgarAsync()` no teardown (`Dispose`).
  Assinatura exata da família de flake conhecida ① (em R13 o mesmo padrão pegou `G4b_…` da
  **mesma classe**; a assinatura — 40P01 em `PurgarAsync()` — é invariante entre classes/fatos).
  Regra de casa aplicada: isolate 5× → **5/5 EXIT 0 (8/8 aprovados por passada, zero falhas)** ⇒
  flake transitório inter-classe, **não regressão**. Baseline aceito: 1447 com flake ① classificado.
- **Ambiente**: servidores Api/Web encerrados; Postgres **em execução** (127.0.0.1:5432) durante
  baseline e suíte.
- Logs: `r14_baseline_suite.log`, `r14_isolate_pass1..5.log` (em `%TEMP%\opencode\`).
