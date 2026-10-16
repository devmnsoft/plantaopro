# Rodada 12 — Relatório final consolidado (fix do catálogo do guard Bi/DashboardsPremium + homologação viva)

- **Data:** 2026-10-16 · **Base:** `10a2171` (ponta do push da Rodada 11) · **Escopo:** P2 da fila R11 — telas `Bi` e `DashboardsPremium` órfãs do catálogo do guard (`CATALOGO_NAO_CONFIGURADO` para todo autenticado, achado vivo do `r11b2`). Fora de escopo e seguidos na fila §7: drill-down gaps da R10, exposição em menu (decisão de produto), P1b `destino`, Meu Dia UTC, tiles B18, especialidade B16, pg_dump P5.
- **Regra de push:** respeitada — **3 commits locais em `main`, sem push** (pendente de autorização ao fim desta rodada): `9720677` → `ae76e36` → `e75c094`.

## Gate de suíte (Release)

| Porta | Resultado | Nota |
|---|---|---|
| Baseline (`10a2171`) | **1428/1428 limpo** | build `EXIT 0`; log `r12_baseline_suite.log` |
| Pós-r12b1 (estado final) | **Total 1437 · Aprovados 1436 · 1 falha = flake catálogo ① conhecido** (`ProvisionamentoB5Tests.P1_Finalizar_CriaMundoTodo_E_AdminAutentica`, `Npgsql 40P01 deadlock` em `PurgarAsync()`/`Dispose()`) — **isolate 5× → 11/11 em cada pass** | não-regressão pela regra de casa (flake já registrado em R8/R11); os **+9 fatos novos** passaram na full e em isolate; log `r12b1_suite.log` + `r12b1_flake_pass1..5.log` |
| Pós-r12b2 (estado final) | suíte do r12b1 vigente | slice só ambiente/dados/docs — nenhum binário alterado |

Δ testes da rodada: **+9** (classe nova `R12GuardBiPremiumCatalogTests`: 2 fatos source-scan anti-regressão do catálogo + existência das views; 5 fatos de comportamento web-bff — negação `MODULO_NAO_CONTRATADO` sem claim, `PERMISSAO_NEGADA` com claim sem permissão, 200 c/ título correto nas duas telas, bypass de global admin; teoria 2 casos).

## Slices entregues

| Commit | Slice | Entrega |
|---|---|---|
| `9720677` | r12a0 | Auditoria inicial / plano: baseline acima; recon por fonte canônica (módulo `BI` e `DASHBOARD` livres no guard, com suíte 31 ações cada; `CLINICA_DASHBOARD` rejeitado — domínio distinto já dominado; módulo novo `PREMIUM` rejeitado — exigiria DDL; sem overrides de ação — todas as ações resolvem para `VER`); fila ordenada |
| `ae76e36` | r12b1 — fix | `SaasRouteGuardFilter.ControllerModules`: `["Bi"]="BI"` + `["DashboardsPremium"]="DASHBOARD"` (comentários de casa citando o achado r11b2) + classe de contrato **+9 fatos** + gate conforme acima. Sem DDL, sem API, sem resx |
| `e75c094` | r12b2 — homologação viva | Ciclo completo provado ao vivo num tenant sem os módulos: `MODULO_NAO_CONTRATADO` (sem contrato) → `PERMISSAO_NEGADA` (contrato, permissão inativa nas claims) → **200 com dados reais** (contrato + permissão), guard logando `Modulo:BI`/`Modulo:DASHBOARD` a cada etapa; grants reversíveis tagados (`HOMOLOG_R12`) + reativação documentada de 3 permissões inativas desde 10/14; **4/7 drill-downs abertos ao vivo** (+1 negado-honesto por papel já previsto em R10, +1 achado novo, +2 condicionados a origem única); **capturas pendentes da R11 fechadas** (Ocorrências ABERTA/ENCERRADA + Estoque BLOQUEADO, fallback DOM aceito); **net-zero verificado** (efetivos 23→18; seed `SEED_HOMOLOG_R11B2` removida: 2 ocorrências / 14 movimentos / 2 locais, residuo 0) |

## Contrato alterado na rodada

- `SaasRouteGuardFilter.ControllerModules`: +2 entradas (`Bi`, `DashboardsPremium`) — mesmo formato dos fixes precedentes (R6-BlocoA `Caixa`→`CLINICA_FINANCEIRO`, R7-E1 `Usuario`→`CONTA`).
- Contratos de teste: classe nova `R12GuardBiPremiumCatalogTests` (9 fatos). Nenhuma mudança de Application/API/resx/DDL (nenhuma literal nova de UI — Adm360/650 chaves intocadas).

## Decisões versionadas nesta rodada (resumo do plano r12a0 §2/§7)

1. **Mapeamento canônico:** `["Bi"]="BI"`, `["DashboardsPremium"]="DASHBOARD"` — dono por semântica + módulo livre no guard. Rejeitados: `CLINICA_DASHBOARD` (domínio clínico já dominado por outro controller — confluência) e módulo novo `PREMIUM` (exigiria DDL — fora de escopo para fix de catálogo).
2. **Sem `PermissionActionOverrides`:** todas as ações dos dois controllers resolvem para `VER` pelo resolver genérico ⇒ gate efetivo `*.VER` + contratação.
3. **Porteada por contrato (não core):** `BI`/`DASHBOARD` não entram em `CoreModules`/`TenantAdministrationModules` — comportamento desejado, coerente com o resto do catálogo; global admin segue com bypass.
4. **Exposição em menu fora de escopo:** o fix torna as rotas resolúveis e porteadas por contrato; link de menu segue como decisão de produto na fila (nenhuma UI inventada).
5. **Correção factual ao r12a0 §2.4:** o perfil admin SV tinha as linhas `.VER` **presentes mas inativas** (`reg_status='I'` desde 2026-10-14 em `BI.VER`/`CLIENTES.VER`/`DASHBOARD.VER`) — "0/31" valia em termos de claims efetivas (a query de login exige `reg_status='A' AND permitido=true`), mas a causa era inativação, não ausência. Registrado no `r12b2` §4.

## Homologação viva — veredicto e novos achados

- **Ciclo completo validado ao vivo** (§3–5 do `r12b2`), incluindo evidência server-side dos logs do guard em cada etapa e o fail-closed `VERIFICACAO_INDISPONIVEL` confirmando o comportamento R6-BlocoA sem falso positivo em degradação.
- **Drill-downs (7):** 4 homologados ao vivo (`/Assinaturas`, `/Plantoes`, `/Escalas` + tela Premium renderizada com KPI real); `/Clientes` negado-honesto por `[Authorize(Roles="ADMINISTRADOR_GLOBAL")]` (caveat já previsto em `r10b3` §3.4); **2 âncoras JS** (`escalas`/`cobertura`) condicionadas a fetch ok — bloqueadas apenas pela configuração local multi-porta.
- **ACHADO NOVO (P3) — `BILLING` inexistente em `modulos_sistema`:** o card "Receita Vencida" do BI aponta para `/FaturamentoSaas`, cujo módulo `BILLING` **não existe no catálogo** (nem `BILLING.VER`) ⇒ nenhum tenant pode contratá-lo ⇒ negação permanente para tenants (só global admin passa). Reprodzível; artefato dedicado no `r12b2`.
- **Nota ambiental (não defeito):** `Views/Bi/Index.cshtml` (e `Operacoes/Index.cshtml`, único outro caso) faz `fetch('/api/...')` client-side presumindo origem única; em dev multi-porta o painel KPI exibe o banner honesto (sem número demonstrativo — R8-B19 intacto). Para KPIs numéricos localmente bastaria servir Web+API sob uma origem (proxy) — sem mudança de código.
- **Pendências R11 encerradas:** capturas (fallback DOM) + rollback da seed `SEED_HOMOLOG_R11B2` — banco devolvido ao estado exato pré-janela.

## Dívidas abertas → próximas rodadas (ordenadas)

1. **P3 (novo, achado r12b2) — `BILLING` sem módulo de catálogo:** criar `BILLING` em `modulos_sistema` (+ permissões, DDL) e incluí-lo nos contratos de plano, **ou** remapear o card "Receita Vencida" do BI para rota com módulo contratável. Decisão de produto + fatia própria.
2. **P2 — Gaps de drill-down (r10b3 §6):** lista de pagamentos filtrável por `data_pagamento` (card "Valor pago"); scoping de tenant em `api/medicos`; rota de "todos os ativos" de agendamentos; **contexto de escopo no payload do resumo executivo BI** — reforçado pelo `r12b2`: link estático global (Clientes central) × KPI tenant-scoped precisa de decisão de escopo visível ao usuário.
3. **P2 — Exposição em menu de `Bi`/`DashboardsPremium`:** agora que as rotas são resolúveis e porteadas por contrato, decidir onde expor (decisão de produto).
4. **P2 (residual visual) — capturas de pixel:** fechadas nesta rodada por fallback DOM (aceito); se a casa quiser pixels, janelar com browser desktop em 1º plano (servidores/scripts da receita ficam em `%TEMP%\opencode\`: `r12_api.cmd`/`r12_web.cmd`).
5. **P1b — A12 baixa/devolução dirigida por `destino`** (herdada): encerrar com destino = descarte ainda devolve tudo a LIBERADO no resolver (sem consumo físico pelo destino) — extensão funcional própria.
6. **P3 — Janela "hoje" Meu Dia em fuso do tenant/usuário** (herdada; decisão de produto).
7. **P3 — Spec de design do tile KPI** (herdada; pré-requisito da unificação B18, incluindo o action-link do r10b3 na API do tile).
8. **P3 — Normalização especialidade clínica texto→id** (herdada; rodada própria).
9. **P5 (herdada) — `pg_dump` 18 quebrado neste host** (tooling de snapshot; bloqueio ambiental).

## Artefatos desta pasta

`r12a0-auditoria-inicial-plano-sprint.md` (plano + painel de fechamento atualizado) · `r12b1-fix-catalogo-guard-bi-premium.md` (evidência do slice + gate) · `r12b2-homologacao-viva-guard-bi-premium-drilldowns.md` (+ `artefatos-dom/` com 8 capturas DOM: 2 negações pré-grant, página Bi, página Premium, mapa vivo dos 7 drill-downs, achado P3 BILLING, Ocorrências ABERTA/ENCERRADA, Estoque BLOQUEADO) · este relatório. Scripts reexecutáveis da janela em `%TEMP%\opencode\` (`r12_api.cmd`/`r12_web.cmd`, `r12b2_grants_sv.sql`/`r12b2_rollback_sv.sql`, `r11b2_seed_rollback.sql`, probes `r12_probe_*.sql`, logs do gate `r12b1_*.log`).
