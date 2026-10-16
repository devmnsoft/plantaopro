# Rodada 10 — Relatório final consolidado (dívidas herdadas R8 / P3)

- **Data:** 2026-10-16 · **Base:** `6b26ec8` (ponta do push da Rodada 9) · **Escopo:** B16/B17/B18 residuais, drill-down §27-L208 e writer da `adm360_ocorrencias` (A12). Fora: P5 `pg_dump` (tooling de snapshot deste host).
- **Regra de push:** respeitada — **5 commits locais em `main`, sem push** (pendente de autorização).

## Gate de suíte (Release)

| Porta | Resultado | Nota |
|---|---|---|
| Baseline (`6b26ec8`) | **1398/1398 limpo** | build EXIT 0 |
| Pós-r10b1 | 1416 total — 1 falha em `ProvisionamentoB5Tests.P3` (**classe flake catálogo ①**, método diferente a cada run — padrão já documentado em r8b13/r8b20/r9-dre/r9-financeiro/r9k-b14), **isolada 5× 11/11 EXIT 0** | não-regressão |
| Pós-r10b2 | **1417/1417 limpo** | |
| Pós-r10b3 | **1421/1421 limpo** | |
| Pós-r10b4 (estado final) | **1421/1421 limpo** | slice só-doc; confirma estado de fechamento |

Δ testes da rodada: **+23** (17 A12 + 1 completude i18n + 1 fact novo R8B17 + 4 R10b3).

## Slices entregues

| Commit | Slice | Entrega |
|---|---|---|
| `d246346` | r10a0 | Auditoria inicial / plano (matriz D1–D5 com evidência por fonte canônica) |
| `287c652` | r10b1 — D1 | **Writer A12** `adm360_ocorrencias`: máquina estrita ABERTA→EM_TRATAMENTO→RESOLVIDA→ENCERRADA (1 passo; encerrar exige destino+justificativa; concorrência por `versao` 409; estoque invariante); API `api/administrativo360/ocorrencias` com política `Adm360.Qualidade`; Web lê tabela real + 4 comandos; i18n **605→650 chaves ×4** + fact de completude; 17 casos de teste |
| `99982d6` | r10b2 — D2/B17 | **Consolidação da facade Meu Dia**: `MeuDiaServices.cs` deletado, controller direto sobre `IProductivityActionService`, janela (UtcNow+1d / PageSize 25 / CLINICO) relocada p/ helpers privados; rotas byte-inalteradas; contrato R8B17 reescrito para a nova topologia (4 fatos) |
| `92360b5` | r10b3 — D3/L208 | **Drill-down "Ver registros"** nos cards Bi/Premium onde há rota-alvo real validada pelo SQL dos endpoints (7 links: Clientes ATIVO, Assinaturas ATIVA, FaturamentoSaas VENCIDA, Escalas período, Cobertura período, Plantões, Escalas premium); sem link onde desonesto (Valor pago — dimensão data_pagamento × pl.data_inicio; Médicos — lista global; Agendamentos — rota por período); caveats documentados; contrato anti-inventário |
| `1dd5202` | r10b4 — D4+D5 | **Decisões formais**: B16 = manter texto livre clínico até rodada de normalização própria (guarda ativa); B18 = unificação de tiles KPI como refator de design futuro (spec de alvo + paridade JS + requisito de action-link r10b3); painel de dívidas atualizado |

## Contratos alterados na rodada

- `R7DF6Admin360I18nContratoTests`: 605→**650 chaves** (+ fact `ViewsAdm360_R10b1OcorrenciasCompletudeSemLiterals`).
- `R8B17MeuDiaFacadeContratoTests`: reescrito (3→4 fatos) para trancar a topologia consolidada + mantra de janela UTC preservado.
- `R10b3BiPremiumDrillDownContratoTests` (novo, 4 fatos): drill presente com parâmetros exatos / card sem alvo sem link / mapeamento Premium restrito / anti-rota-inventada.

## Decisões versionadas (sem mudança de comportamento)

1. **B16:** texto livre segue canônico na clínica; normalização texto→id = rodada própria com backfill+picker; híbrido rejeitado.
2. **B18:** unificação de tiles adiada como refator de design (redundância conceitual, não cópia).
3. **B17 (residual mantido):** janela "hoje" segue em **UTC** — caveat documentado no controller, no contrato e nas pendências (mudança de comportamento exige decisão de produto).
4. **A12 (escopo):** bloqueio imediato de saldo interno/externo fica registrado como P1 do A12 (fora da fatia r10b1 por delimitação do plano; coluna `tipo`/`situacao` já suportam sem DDL).

## Dívidas abertas → próximas rodadas (ordenadas)

1. **P1 — A12 bloqueio imediato de saldo** ao abrir ocorrência (linha do spec que ficou fora do escopo r10b1 por delimitação).
2. **P2 — Homologação viva (webview):** CRUD completo da tela Ocorrências do Admin360 + clique nos 7 novos links de drill-down.
3. **P2 — Gaps de drill-down (r10b3 §6):** lista de pagamentos filtrável por `data_pagamento`; scoping de tenant em `api/medicos`; rota de "todos os ativos" de agendamentos; contexto de escopo no payload do resumo executivo BI para os links estáticos.
4. **P3 — Janela "hoje" Meu Dia em fuso do tenant/usuário** (mudança de comportamento; decisão de produto).
5. **P3 — Spec de design do tile KPI** (pré-requisito da unificação B18, incluindo o action-link do r10b3 na API do tile).
6. **P3 — Normalização especialidade clínica texto→id** (rodada própria: backfill nome→id, picker, dual-write).
7. **P5 (herdada) — `pg_dump` 18 quebrado neste host** (tooling de snapshot; bloqueio ambiental).

## Artefatos desta pasta

`r10a0-auditoria-inicial-plano-sprint.md` (plano + painel de fechamento) · `r10b1-writer-adm360-ocorrencias-a12.md` · `r10b2-consolidacao-facade-meu-dia-b17.md` · `r10b3-drilldown-bi-premium-l208.md` · `r10b4-decisoes-b16-b18.md` · este relatório.
