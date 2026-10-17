# Rodada 14 — Relatório final consolidado (500 permanente `Npgsql 42P08` em faturas SaaS: cast duplo do `@competencia` + degradação honesta + homologação viva + achado/correção do banner "Sucesso")

- **Data:** 2026-10-16/17 · **Base:** `c778469` (`origin/main`, ponta do push da Rodada 13) · **Escopo:** topo da fila R13z — F13-A (`GET /api/faturamento-saas/faturas` sempre-500 com `42P08` em `@competencia` nulo, escopo não-global) + F13-B (Index de Faturamento SaaS degrada silenciosamente para vazio falso). Recon desta rodada (r14a0) ampliou: **existem exatamente 2 ocorrências sem cast no repo** (listagem comercial + relatório SaaS de faturamento, este chamado pelo Web sempre sem competência ⇒ sempre-500 para todo mundo), e a varredura fechou com prova de fonte. Fora de escopo e seguidos na fila §7: fantasmas do catálogo, drill-downs, exposição em menu, P1b `destino`, Meu Dia UTC, tile B18, especialidade, pg_dump.
- **Regra de push:** respeitada — **3 commits locais em `main`, sem push** (pendentes de autorização ao fim desta rodada): `5f30b0e` (r14a0) → `81ebb7a` (r14b1) → `07fb0a0` (r14b2) → este commit (r14z).

## Gate de suíte (Release)

| Ponto | Resultado |
|---|---|
| Baseline (`c778469`) | build EXIT 0 · **1447**, com 1 reprodução do flake conhecido da família ① (G3 `SaasGovernancaB6Rodada4Tests`, teardown 40P01) — isolado **5/5 green** pela regra de casa |
| Pós-r14b1 | build EXIT 0 · **1455/1455 limpo** (8 fatos novos: 2 source-scan da forma canônica, 2 banco real contra `plantaopro_test` — forma antiga ⇒ `PostgresException 42P08` permanente e forma corrigida executa — 4 web-bff de degradação honesta) |
| Pós-r14b2 (fim da rodada) | build EXIT 0 · **1456/1456 limpo** (1 fato novo: atribuição de erro quando o resumo falha; stubs de resumo passaram a usar o envelope real `"message":"Sucesso"`) |

Nenhum teste removido ou enfraquecido na rodada: 1447 → 1456 = +9 fatos.

## Slices entregues

- **r14a0** (`5f30b0e`): auditoria inicial + plano — raiz do 42P08 documentada contra a convenção de casa (`DateOnlyDapperTypeHandler`: "a proteção contra 42P08 vem dos casts nas consultas"), segunda ocorrência idêntica mapeada no relatório SaaS, plano do fix + degradação honesta + ciclo de homologação com grants reversíveis `HOMOLOG_R14`.
- **r14b1** (`81ebb7a`): fix nas 2 ocorrências (`(@competencia::date is null or f.competencia=@competencia::date)` — o walker de resolução do PG exige tipo **já na ocorrência `IS NULL`**; cast só na comparação posterior ainda dá 42P08, provado empiricamente no precheck), banners `<strong>Falha ao carregar.</strong>` de padrão de casa nas 2 views com supressão do vazio falso por `comErro`, contrato de 8 fatos.
- **r14b2** (`07fb0a0`): homologação viva completa (grants aplicados → capturas → rollback exato → negação restaurada, ver §5) + **achado e correção de segunda ordem**: o caminho de sucesso do parser simples expõe `message:"Sucesso"` do envelope como `Error` e o controller anunciava sucesso como falha — corrigido por atribuição de erro pelo status da chamada (F-R14-C).

## Causa-raiz e fix final (resumo)

Com `competencia` nulo (`DateOnly?`), o Dapper envia o parâmetro sem tipo (OID 0) e o PostgreSQL falha no parse com `42P08` — comportamento esperado pela própria convenção documentada da casa, que exige cast nos SQLs. As 2 ocorrências sem cast formavam: (a) 500 permanente em `/FaturamentoSaas/Index` para tenants não-globais (degradação silenciosa + vazio falso), (b) 500 permanente em `/Relatorios/SaasFaturamento` para todos (o Web nunca envia competência). O fix é o cast duplo na cláusula única de cada ação — sem DDL, sem mudança no handler, sem mudança de contrato de API; `GerarMensal` confirmada segura (constrói `DateOnly` não-nulo da request). Prova de raiz versionada: o SQL antigo exato continua reproduzindo `PostgresException 42P08` contra `plantaopro_test` com o mesmo shape de argumentos da ação (fato de contrato — se o PG resolver sozinho ou o cast sumir, o contrato falha de propósito).

## Contrato alterado na rodada

- `backend/PlantaoPro.Api/Controllers/SaasCommercialController.cs` — cast duplo no `where` único da ação `Faturas` (cobre count+list) + comentário de convenção.
- `backend/PlantaoPro.Api/Controllers/RelatoriosSaasController.cs` — cast duplo na ação `Faturamento` + comentário acima do endpoint.
- `backend/PlantaoPro.Web/Views/FaturamentoSaas/Index.cshtml` e `Views/Relatorios/Saas.cshtml` — variáveis `erroCarregamento`/`comErro` (cobre `ViewBag.ErrorMessage` **e** `TempData["Error"]` do path catch), banner `alert-danger role=alert` com o literal de casa, empty state/vazio suprimidos quando `comErro`.
- `backend/PlantaoPro.Web/Controllers/FaturamentoSaasController.cs` — atribuição do erro pelo status da chamada do resumo (não pelo primeiro valor não-nulo), anulando o vazamento "Sucesso" (F-R14-C) com comentário do achado.
- `backend/PlantaoPro.Tests/R14FaturasCompetenciaNullContratoTests.cs` — 9 fatos em 3 classes (source-scan com as strings canônicas + extinto o `f.competencia=@competencia)`; banco real com handlers registrados via static ctor; web-bff da coleção dedicada).
- Sem DDL, sem migration, `access_catalog_version` permanece **`v2149`**.

## Homologação viva — veredicto

**APROVADA**, ciclo completo com grants reversíveis `HOMOLOG_R14` (só `FATURAMENTO_SAAS` em `tenant_modulos` + reativação dormente `FATURAMENTO_SAAS.VER`; `RELATORIOS` já efetivo no SV):

| Etapa | Evidência |
|---|---|
| Pré-grant | `/FaturamentoSaas/Index` → `AccessDenied?...MODULO_NAO_CONTRATADO` (guard honesto) · `/Relatorios/SaasFaturamento` → **200 com as 4 linhas reais** do dev DB (prova viva de que o F13-A morreu — antes: sempre-500) |
| Pós-grant (build final) | Index 200 com zeros honestos (escopo SV), sem banner, empty state legítimo; `?status=VENCIDA` refletido na API; **`?competencia=2026-10-01` round-trip DateOnly não-nulo 200**; `/Inadimplencia` 200; relatório 200 com 4 linhas/“R$ 676,00” · toda chamada `faturas`/relatório **200** nos logs da Api |
| Rollback (net-zero) | `DELETE 1` + `UPDATE 1` num único bloco; resíduo `HOMOLOG_R14`=0, `VER`→`I|permitido=true`, efetivos de volta aos **18 exatos**; re-login #2 ⇒ `MODULO_NAO_CONTRATADO` de novo (e `warn` do guard nos logs do Web) · Postgres segue de pé, dev DB intocado além do ciclo |

Detalhe completo, capturas verbais e artefatos em `r14b2-homologacao-viva-fix-competencia-e-banner.md`.

## Decisões versionadas nesta rodada (resumo do plano r14a0)

1. **D1 — Cast duplo nas 2 ocorrências, nada além.** Handler global rejeitado (blast radius); fallback do plano materializado pelo precheck empírico (cast só na comparação não basta — o tipo precisa existir já na ocorrência `IS NULL`).
2. **D2 — Degradação honesta de padrão de casa:** views leem `(ViewBag.ErrorMessage as string) ?? (TempData["Error"] as string)` (cobre o path catch), banner com o literal `<strong>Falha ao carregar.</strong>` idêntico ao de `Clientes/Index.cshtml`, vazio falso suprimido por `comErro` (sem over-supressão: o vazio verdadeiro continua anunciando o estado real).
3. **D3 — Contrato de 8 fatos** (não os ~5 estimados): a dupla ocorrência exigiu facts em triplicata — fonte, banco real e web-bff.
4. **D4 — Grants mínimos `HOMOLOG_R14`** espelhando R12/R13: só o módulo em jogo + permissão dormente, re-login obrigatório nos dois lados (claims materializam no login), rollback exato e verificado.
5. **D5 — Sem DDL/handler/contrato de API:** `access_catalog_version` intacto; `GerarMensal` auditada como não-afetada.
6. **D6 (novo, b2) — Atribuição de erro pelo status, não pelo parser:** o vazamento "Sucesso" foi corrigido no action afetado (escolha entre `resumo.Error`/`faturas.Error` guiada pelo `StatusCode` do resumo) sem tocar `BaseWebController` — uniformizar os dois parsers exige varredura prévia dos consumidores de `.Error` em 2xx (fila, F-R14-C). Nenhum literal acoplado no controller.

## Achados da rodada

- **F13-A/F13-B (herdadas): RESOLVIDAS** e homologadas ao vivo (§5) — era o alvo da rodada.
- **F-R14-A:** nome dado à segunda ocorrência do sempre-500 (relatório SaaS); resolvida pelo mesmo cast (prova viva no relatório com dados reais).
- **F-R14-C (novo, P2, Web, sistêmico):** caminho de sucesso de `ReadApiResponsePayloadAsync` expõe `message` do envelope como `Error` (variante paginada devolve `null`) — telas que leiam `.Error` sobre 2xx podem anunciar "Sucesso" como falha. Fix local nesta rodada; uniformização de casa fica na fila com varredura de consumidores.
- **F-R14-D (novo, P3):** assimetria nos 500 — chamada simples usa literal fixo descartando a mensagem da API; chamada paginada repassa a mensagem parseada. Documentada, intenção a alinhar.
- **F-R14-B (novo, P3):** `ExecutarRelatorioAsync` calcula `total = items.LongCount()` (total de paginação truncado ao pageSize) e `RelatoriosSaasController.Faturamento` roda global sem scoping de tenant (design pré-existente do relatório).

## Dívidas abertas → Rodada 15 (ordenadas)

1. **P1b (herdada) — A12 baixa/devolução dirigida por `destino`:** encerrar com destino=descarte ainda devolve tudo a LIBERADO no resolver (sem consumo físico pelo destino).
2. **P2 (novo, F-R14-C) — Uniformizar a exposição de erro dos parsers Web:** varrer os consumidores de `.Error` em respostas 2xx e decidir a correção de casa (hoje o leak "Sucesso" existe em todo parser simples; o action de FaturamentoSaas já está imune pela D6).
3. **P2 (herdada R13, auditoria r13a0 §1.3) — Fantasmas atingíveis do catálogo guard** (cada um com decisão canônica própria; evidência de alcance medida): `CENTRAL_ESCALA` (checar gêmeo `ESCALAS`), `MINHA_AGENDA` (controller só papel `MEDICO`), `MEDICO_AREA`.
4. **P2 (herdada R12z §7.2) — Gaps de drill-down (r10b3 §6):** lista de pagamentos filtrável por `data_pagamento`; scoping de tenant em `api/medicos`; rota de "todos os ativos" de agendamentos; contexto de escopo visível no resumo executivo BI (link global × KPI tenant-scoped).
5. **P2 (herdada R12z §7.3, ampliada) — Exposição em menu:** decidir onde expor `Bi`/`DashboardsPremium` e **`FaturamentoSaas`** (rota agora funcional e homologada; sidebar "Cobranças" só existe na seção global-admin) — decisão de produto.
6. **P3 (novo, F-R14-D) — Alinhar a intenção dos 500** entre chamada simples (literal fixo) e paginada (mensagem da API).
7. **P3 (novo, F-R14-B) — Total de paginação do wrapper de relatórios** (truncado ao pageSize) + decidir scoping de tenant do relatório SaaS de faturamento (global hoje por design).
8. **P3 (herdada) — Fantasmas residuais do catálogo:** `HOSPITAL_AREA`, `MARKETPLACE`, `PROPOSTAS` (`[Authorize]` simples), `PARCEIRO`, e o mismatch dormente `OBSERVABILIDADE_GLOBAL`→`OBSERVABILIDADE` (mesmo padrão desta R13/R14; zero mudança comportamental até lá).
9. **P3 (herdada) — Janela "hoje" Meu Dia em fuso do tenant/usuário** (decisão de produto).
10. **P3 (herdada) — Spec de design do tile KPI** (pré-requisito da unificação B18, incluindo o action-link do r10b3).
11. **P3 (herdada) — Normalização especialidade clínica texto→id.**
12. **P5 (herdada) — `pg_dump` 18 quebrado neste host** (bloqueio ambiental de snapshot).

## Artefatos desta pasta

`r14a0-auditoria-inicial-plano-sprint.md` (recon F1–F8, varredura sistemática das ocorrências, plano, baseline com flake ①) · `r14b2-homologacao-viva-fix-competencia-e-banner.md` (ciclo completo de grants/rollback, capturas pré/pós com o build final, achado F-R14-C e sua correção) · este relatório. Scripts e logs reexecutáveis em `%TEMP%\opencode\`: `r14b2_pre_grant.sql(.bat)`, `r14b2_grants_sv.sql(.bat)`, `r14b2_rollback_sv.sql(.bat)`, `r14b2_health.ps1`, `r14b1_precheck.bat`, `r14b1_gate.bat`, `r14_probe_testdb.bat`, logs `r14b1_build.log`/`r14b1_newtests.log` (artefato de raiz do 42P08)/`r14b1_suite.log`/`r14b1_iso.log`, subida `r12_api.cmd`/`r12_web.cmd` (reutilizada das R12/R13).
