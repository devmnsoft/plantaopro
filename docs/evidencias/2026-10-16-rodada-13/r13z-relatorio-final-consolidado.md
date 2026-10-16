# Rodada 13 — Relatório final consolidado (guard do `FaturamentoSaas`/`Billing`: remapeio do fantasma `BILLING` para o módulo canônico `FATURAMENTO_SAAS` + homologação viva)

- **Data:** 2026-10-16 · **Base:** `e3b7baa` (ponta do push da Rodada 12) · **Escopo:** P3 da fila R12z §7 item 1 — o card "Receita Vencida" do BI Executivo aponta para `/FaturamentoSaas`, cuja porta cobrava o código **`BILLING` inexistente no catálogo** ⇒ negação permanente para tenants (achado vivo do r12b2). Recon desta rodada refinou a natureza: **não é DDL ausente — é mismatch de código no guard** (módulo canônico `FATURAMENTO_SAAS` já existe com suíte completa de 31 e grants dormentes nos perfis admin). Fora de escopo e seguidos na fila §7: os demais fantasmas de catálogo (agora classificados), drill-down gaps da R10/R12, exposição em menu, P1b `destino`, Meu Dia UTC, tiles B18, especialidade B16, pg_dump P5.
- **Regra de push:** respeitada — **4 commits locais em `main`, sem push** (pendente de autorização ao fim desta rodada): `59841dc` → `41bdd55` → `e74c25b` → este commit.

## Gate de suíte (Release)

| Porta | Resultado | Nota |
|---|---|---|
| Baseline (`e3b7baa`) | **Total 1437 · Aprovados 1436 · 1 falha = flake família ① conhecida** (`SaasGovernancaB6Rodada4Tests.G4b_…`, `Npgsql 40P01 impasse` em `PurgarAsync()` no teardown) — **isolate 5× → verde em cada passada (8/8 aprovados)** | não-regressão pela regra de casa (mesma assinatura de R8/R11/R12 — em R12 foi `ProvisionamentoB5Tests.P1_Finalizar…`); log `r13b1_suite.log` + `r13_isolate_passi.log` |
| Pós-r13b1 (estado final) | **BUILD_EXIT 0 · Total 1447 · Aprovados 1447 · 0 falhas** | nenhum flake na passada; os **+8 fatos novos** passaram na full |
| Pós-r13b2 (estado final) | suíte do r13b1 vigente | slice só ambiente/dados/docs — nenhum binário alterado |

Δ testes da rodada: **+8** (classe nova `R13GuardFaturamentoSaasCatalogTests`: 2 fatos source-scan anti-regressão — entradas mapeadas para `FATURAMENTO_SAAS` e literal `"BILLING"` extinto do dicionário, com seed canônico citado no repo — e 6 fatos comportamentais web-bff: `MODULO_NAO_CONTRATADO` sem contrato, `PERMISSAO_NEGADA` com contrato sem permissão, 200 estado vazio honesto com contrato+permissão, filtro `?status=VENCIDA` refletido, shell `/Billing/Faturas` redirecionando quando liberado, denegação do mesmo módulo via rota do shell, teoria global admin bypass).

## Slices entregues

| Commit | Slice | Entrega |
|---|---|---|
| `59841dc` | r13a0 | Auditoria inicial / plano: baseline acima; recon por fonte canônica (F1–F8: `FATURAMENTO_SAAS` existe ATIVO ordem 200, suíte 31, 0 referências a `BILLING` fora das 2 entradas do guard, 31 grants dormentes `reg_status='I'` no perfil admin SV, sem nenhum tenant contratado); **auditoria sistemática 55 códigos do dicionário × `modulos_sistema` → 14 ausentes classificados** (4 bypass de construção, 1 corrigido aqui, 1 dormente, 7 fantasmas); decisões D1–D5 |
| `41bdd55` | r13b1 — fix | `SaasRouteGuardFilter.ControllerModules`: `["Billing"]="FATURAMENTO_SAAS"` + `["FaturamentoSaas"]="FATURAMENTO_SAAS"` (comentários de casa citando o achado r12b2/r13a0) + classe de contrato **+8 fatos** + gate conforme acima. Sem DDL, sem API, sem resx, `access_catalog_version` intacto em `v2149` |
| `e74c25b` | r13b2 — homologação viva | Ciclo completo provado ao vivo no tenant SV (perfil admin): **3 negações-honestas pré-grant nomeando o módulo canônico** (`module=FATURAMENTO_SAAS` nas duas rotas reais, `module=BI` no /Bi) → grants reversíveis tag `HOMOLOG_R13` (efetivos 18→20) + re-login → **200 em `/FaturamentoSaas/Index` e `?status=VENCIDA`**, `/Inadimplencia` 200, shell `/Billing/Faturas` 302→canônico, **drill-down do card "Receita Vencida" do `/Bi` end-to-end (o gap exato do r12b2)** → rollback verbatim + **net-zero duplamente verificado** (banco: `DELETE 2`/`UPDATE 2`, resíduo `HOMOLOG_R13`=0, efetivos aos 18 exatos; UI: nova sessão negada de novo) |

## Contrato alterado na rodada

- `SaasRouteGuardFilter.ControllerModules`: 2 entradas remapeadas de `"BILLING"` (fantasma) para `"FATURAMENTO_SAAS"` (módulo canônico) — mesma classe de fix dos precedentes R6-BlocoA (`Caixa`→`CLINICA_FINANCEIRO`), R7-E1 (`Usuario`→`CONTA`), R12-B1 (`Bi`→`BI`, `DashboardsPremium`→`DASHBOARD`). O literal `"BILLING"` ficou **extinto** do dicionário do guard (fato de fonte anti-regressão).
- Contratos de teste: classe nova `R13GuardFaturamentoSaasCatalogTests` (8 fatos). Nenhuma mudança de Application/API/resx/DDL (nenhuma literal nova de UI — Adm360/650 chaves intocadas).

## Decisões versionadas nesta rodada (resumo do plano r13a0 §2)

1. **D1 — Remapear o guard, não o banco.** Adotar o módulo canônico existente (`FATURAMENTO_SAAS`) sobre criar `BILLING` (rejeitado: duplicaria o domínio de cobrança com suíte 31 e exigiria DDL/migration/manifests — precedente v2197/v2198) ou remapear o card "Receita Vencida" (rejeitado: o alvo foi validado como honesto na R10; trocar o destino esconderia o defeito).
2. **D2 — Sem DDL:** `access_catalog_version` permanece `v2149`; nenhuma migration/manifest tocado.
3. **D3 — Grant mínimo e reversível** (tag `HOMOLOG_R13`), espelhando r12b2. **Refinamento executado (documentado no r13b2 §1):** além de `FATURAMENTO_SAAS`+`.VER`, contratou-se temporariamente `BI`+`.VER` — necessário porque o rollback final da R12 removeu o contrato `BI` do SV e o card do drill-down vive na tela `/Bi/Index`. Ambos revogados no rollback. Escrita (`.CONFIRMAR`/`.EXCLUIR`) mantida dormente, fora do perímetro.
4. **D4 — Os demais fantasmas saem nesta rodada com classificação, não com fix** (cada um exige julgamento canônico próprio; evidência de alcance já medida no r13a0 §1.3) → fila R14 §7.3.
5. **D5 — Exposição em menu para tenants** segue como decisão de produto (fila), agora com a rota funcional.

## Homologação viva — veredicto e novos achados

- **Veredicto: APROVADA.** Porta agora honesta e contratável: a mesma denegação que antes nomeava um módulo impossível de contratar (`module=BILLING`) nomeia o real (`module=FATURAMENTO_SAAS`), e a contratação mínima libera as telas com drill-down do BI funcionando end-to-end ao vivo — o gap exato reproduzido no r12b2 está resolvido. Logs server-side do guard registraram `Modulo:FATURAMENTO_SAAS`/`Modulo:BI` em cada etapa (evidência no r13b2).
- **ACHADO NOVO (P3, Api) — F13-A: `GET /api/faturamento-saas/faturas` responde 500** com `Npgsql 42P08: não foi possível determinar o tipo de dados do parâmetro $5` (`@competencia` nulo nos SQLs de count/lista — `SaasCommercialController.cs` L603–605). Reproduzido 3× na janela, com e sem `?status=VENCIDA`; `/resumo` e `/inadimplencia` saudáveis. Pré-existente, ortogonal ao guard.
- **ACHADO NOVO (P3, Web, honestidade) — F13-B: `Views/FaturamentoSaas/Index.cshtml` nunca lê `ViewBag.ErrorMessage`** (o controller seta em L26) — com o F13-A ativo, a tela degrada silenciosamente para "tabela vazia" sem banner honesto; desvio do padrão de casa usado nos outros 18 casos do ViewBag (família R8-B19: sem falso "vazio" quando houve falha).
- **Nota ambiental (não defeito):** telas fazem `fetch('/api/…')` presumindo origem única (classificada na R12); login-race mecânico na transição pós-rollback (navegação cancelou POST; refeito). Nenhuma mudança de código decorrente.

## Dívidas abertas → Rodada 14 (ordenadas)

1. **P3 (novo, F13-A) — 500 no endpoint paginado de faturas:** corrigir a tipagem do parâmetro `@competencia` (ex.: `::date` ou reestruturar o teste de null) + fato de contrato regressivo do `Faturas()` com `competencia` nulo (caminho tenant não-global).
2. **P3 (novo, F13-B) — banner honesto no Index de Faturamento SaaS:** renderizar `ViewBag.ErrorMessage` no padrão de casa + fato que falha da API listável ≠ tabela vazia silenciosa.
3. **P2/P3 (novos, auditoria r13a0 §1.3) — fantasmas restantes do catálogo guard:** cada um exige decisão canônica própria (módulo novo? gêmeo? global-only?) com a evidência de alcance já medida:
   - **P2 atingíveis por papéis/links reais:** `CENTRAL_ESCALA` (menu sidebar atrás de `Can(…)` + links em Convites/Coordenação — checar gêmeo `ESCALAS`, domínio distinto), `MINHA_AGENDA` (controller só papel `MEDICO`, links em Meu Dia), `MEDICO_AREA` (papéis `MEDICO`+admins de tenant, links em MinhaAgenda).
   - **P3:** `HOSPITAL_AREA`, `MARKETPLACE` e `PROPOSTAS` (`[Authorize]` simples — qualquer autenticado é bloqueado hoje), `PARCEIRO`, e o mismatch **dormente** `OBSERVABILIDADE_GLOBAL`→`OBSERVABILIDADE` (1 linha + fato, mesmo padrão desta rodada; controller só `ADMINISTRADOR_GLOBAL` ⇒ zero mudança comportamental até lá).
4. **P2 (herdada R12z §7.2) — Gaps de drill-down (r10b3 §6):** lista de pagamentos filtrável por `data_pagamento`; scoping de tenant em `api/medicos`; rota de "todos os ativos" de agendamentos; contexto de escopo visível no resumo executivo BI (link global × KPI tenant-scoped).
5. **P2 (herdada R12z §7.3, ampliada) — Exposição em menu:** decidir onde expor `Bi`/`DashboardsPremium` e agora também **`FaturamentoSaas`** (rota funcional e porteadora desde esta rodada; sidebar "Cobranças" só existe na seção global-admin) — decisão de produto, nenhuma UI inventada.
6. **P1b (herdada) — A12 baixa/devolução dirigida por `destino`:** encerrar com destino=descarte ainda devolve tudo a LIBERADO no resolver (sem consumo físico pelo destino).
7. **P3 (herdada) — Janela "hoje" Meu Dia em fuso do tenant/usuário** (decisão de produto).
8. **P3 (herdada) — Spec de design do tile KPI** (pré-requisito da unificação B18, incluindo o action-link do r10b3).
9. **P3 (herdada) — Normalização especialidade clínica texto→id.**
10. **P5 (herdada) — `pg_dump` 18 quebrado neste host** (bloqueio ambiental de snapshot).

## Artefatos desta pasta

`r13a0-auditoria-inicial-plano-sprint.md` (plano + recon F1–F8 + auditoria 55 códigos + baseline com flake ① classificado) · `r13b2-homologacao-viva-guard-faturamento-saas.md` (ciclo completo, grants/rollback verbatim, achados F13-A/F13-B) · este relatório. Scripts reexecutáveis da janela em `%TEMP%\opencode\`: `r13_audit_guard_x_catalogo.sql(.bat)`, `r13_probe_grants.sql(.bat)`, `r13_probe_bi_state.sql(.bat)`, `r13b2_pre_grant.sql(.bat)`, `r13b2_grants_sv.sql/.bat`, `r13b2_rollback_sv.sql/.bat`, `r13b2_health.ps1`, `r13b2_stop_servers.ps1`, log do gate `r13b1_suite.log`, subida `r12_api.cmd`/`r12_web.cmd` (reutilizada).
