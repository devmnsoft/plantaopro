# Rodada 16 — Relatório final consolidado

- **Data:** 2026-10-17 · **Base:** `f9c1128` (`origin/main`, ponta do push da Rodada 15) · **Escopo:** topo da fila R15 — **P2 F-R14-C**: uniformizar a exposição de erro dos parsers Web (a `message` do envelope em 2xx vazava como `Error` e virava banner de erro "Sucesso" em telas como Minha Assinatura/Minha Agenda), com adesão preventiva ao padrão de casa **F-R15-A** (required implícito do NRT × `ModelStateInvalidoFiltro`). Fora de escopo e seguidos na fila R17: fantasmas do catálogo, drill-downs, menus, F-R14-B/D e demais P3/P5.

## Gate de suíte (Release)

| Slice | Build | Suíte | Nota |
|---|---|---|---|
| Baseline (r16a0) | 0 erros / 80 avisos (`--no-incremental`) | 1463/1463 | família ① (`40P01`) não acionou nesta rodada |
| Gate final (r16b2) | 0 erros / 80 avisos = idêntico ao baseline (diff linha-a-linha vazio, 71 únicas) | **1476/1476** | delta exato +13 (12 fatos r16b1 + 1 fato regressão F9d r16b2); i18n 653 intacto (D7) |

## Slices entregues

- **r16a0** (`5e31c78`): auditoria com achados F1–F8 fixados por fonte, censo de consumidores (nenhum consome a mensagem 2xx de forma útil), decisões D1–D7 versionadas, plano de 9 fatos e baseline (build 80 avisos / suíte 1463).
- **r16b1** (`5ad0de9`, 4 arquivos): fix em um ponto no parser (L302 → `null` no 2xx) + limpeza D2 (`InfoMessage: null` literal, com a correção F10 de que o campo é morto) + os 9 fatos planejados que viraram **12** (classe `R16WebParserErrorContratoTests` + classe `R16ScanFormBindingFonteTests`) + **4 bugs reais F-R15-A encontrados pelo próprio scan novo** (F9a–c: binds mortos de Pagar Título ×2, Fechar Caixa e Reabrir Caixa — rename de 4 atributos `name` nas views). Pegou a própria família antes do gate fechar.
- **r16b2** (`8669951`, 8 arquivos, +95/−9): smoke vivo dos 3 fluxos no tenant SV com prova de efeito físico no banco dev (título pago R$ 40 + movimento vinculado; fechamento FECHADO diferença 0; REABERTO c/ motivo e autor) — e durante a homologação o **F9d**: 5º caso da família F-R15-A, desta vez em deserialização JSON (`Ativa` nunca mapeava a chave `"ativo"` da API ⇒ toda conta financeira chegava inativa nos Web, quebrando 5 consumers). Corrigido na fatia (rename record + 8 refs views) com fato de regressão #13 e validação ao vivo em 3 telas.

## Causa-raiz e fix final (resumo)

O parser simples de leitura (`ReadApiResponsePayloadAsync`, L302) devolvia `TryParseMessage(content)` no ramo 2xx — a chapa de sucesso da API (`"Sucesso"`, arrays crus, DTOs com `title`) vira­va valor de `Error` e era exibida como erro pelo consumidor. O fix (D1) estabelece a semântica canônica **`Error` não-nulo ⟺ a chamada não deu certo**, num único ponto que cobre os 4 métodos de leitura/escrita do BFF, com a assimetria de catch preservada (D3: escrita escreve `TempData`, leitura não) e os 500 mantendo literal fixo (D4/F-R14-D segue como decisão separada). O scan F-R15-A versionado (baseline 54 formações action×form / 141 parâmetros / whitelist das 6 actions JS-driven / antivacuidade ≥40 e ≥120) converteu a família do required implícito em contrato permanente — e imediatamente pagou a conta em produção (F9a–c no r16b1; F9d no ar durante o r16b2).

## Decisões versionadas nesta rodada (D1–D7)

D1 semântica canônica `Error` nulo em 2xx, implementada em um ponto (L302) · D2 consumidores intocados, só `InfoMessage: null` literal (campo morto documentado — F10) · D3 assimetria de catch preservada · D4 `BuildApiErrorMessage` intocado (intenção dos 500 segue P3) · D5 imunização local do FaturamentoSaas mantida como referencial · D6 contrato F-R15-A permanente via source-scan versionado · D7 sem novas literais de UI ⇒ resx 653 intacto.

## Homologação viva (r16b2) — veredicto

**PASS.** Fluxos validados de ponta a ponta no banco dev `plantaopro` (tenant SV), binário do gate final: (1) **Pagar Título** — `TP-SV-R9G-01` → `PAGO` 40,00, linha `adm360_titulo_pagamentos` c/ key de idempotência + movimento SAIDA vinculado; (2) **Fechar Caixa** — POST chegou e processou (prova do bind), 1ª tentativa 400 por regra de negócio expôs a lacuna **F14**, 2ª tentativa fechamento `FECHADO` diferença 0; (3) **Reabrir Caixa** — `REABERTO` c/ motivo e autor gravados; (4) **KPIs de Contas** — saldo total real (R$ 460,00) e badge Ativa visíveis após o F9d (antes: sempre 0/invertido); (5) **Minha Assinatura** — classe do leak limpa ao vivo (sem texto "Sucesso"; o 2xx happy-path ficou restrito por ausência de cliente de assinatura no tenant SV, cobertura da classe remanesce pelos 12 fatos + homologação R14). Seed e resíduos autocontidos no tenant SV, sem DDL/grant.

## Achados da rodada

- **F9d (novo, 5º da família F-R15-A, camada JSON):** `ContaFinanceiraViewModel.Ativa` vs chave `"ativo"` do `ContaFinanceiraDto` — STJ case-insensitive ignora caixa, não grafia; propriedade sempre `false` em runtime. Impacto: selects de Pagar Título / Receber Título / Fechar Caixa vazios, Fluxo Caixa filtrado, saldo total e contador "ativas" zerados, badges invertidos em Contas Financeiras. Convenção dos irmãos já era `Ativo`. Fix: rename do parâmetro + 8 refs; fato #13 trava a regressão.
- **F14 (novo, P3, UI/API):** modal FecharCaixa sem campo `justificativa` — a API exige justificativa quando há diferença entre conferido e calculado (400), mas o form não envia o parâmetro `string? justificativa` ⇒ impasse para o usuário com diferença ≠ 0. Reprodutível (e reproduzido) no smoke.
- **F10:** correção ao plano — `ListPageViewModel.InfoMessage` nunca foi renderizado por layout de lista (o "banner azul Sucesso" genérico não existia).
- **F12:** gotchas .NET 10 do harness de controller sem host documentados (migração de `ITempDataProvider`, `ITempDataDictionaryFactory`, `ISessionFeature`, assinatura `[NotNullWhen]` de `TryGetValue`) — receita de casa para BFF tests futuros.
- **Família ① (40P01):** não acionou em nenhum gate da rodada (regra de isolar 5× preservada).

## Dívidas abertas → Rodada 17 (ordenadas)

1. **P2 (herdada R13) — Fantasmas atingíveis do catálogo guard:** `CENTRAL_ESCALA` (checar gêmeo `ESCALAS`), `MINHA_AGENDA` (controller só papel `MEDICO`), `MEDICO_AREA` — cada um com decisão canônica própria e evidência de alcance medida.
2. **P2 (novo, r16b1 §5) — Estender o scanner F-R15-A:** (a) família `Adm360*WebController*` (views com paths explícitos `~/Views/Administrativo360/<pasta>/…` por ação); (b) **contra a camada do F9d** — mapeamento nome-chave DTO da API (camelCase) × parâmetros dos ViewModels Web consumidores, travando mismatch de grafia (proposta: teste de reflexão por par DTO→VM usado no BFF).
3. **P2 (novo, r16b1 §5) — Whitelist JS-driven do scan:** validar/contratar os 6 actions JS-driven (`OrcamentoRejeitar`, `OrcamentoCancelar`, `OrcamentoCancelarReserva`, `ValeCancelar`, `AtivarDepartamento`, `AtivarCargo`) — hoje isentos por whitelist; falta o fato que garanta que a URL montada em JS carrega os escalares não-nuláveis certos.
4. **P2 (herdada R12z §7.2) — Gaps de drill-down (r10b3 §6):** pagamentos filtráveis por `data_pagamento`; scoping de tenant em `api/medicos`; rota "todos os ativos" de agendamentos; contexto de escopo visível no resumo executivo BI.
5. **P2 (herdada R12z §7.3) — Exposição em menu:** decidir onde expor `Bi`/`DashboardsPremium` e `FaturamentoSaas` — decisão de produto.
6. **P3 (herdada, F-R14-D, premissa refinada) — Alinhar a intenção dos 500** entre chamada simples (literal fixo) e paginada (mensagem da API).
7. **P3 (herdada, F-R14-B) — Total de paginação do wrapper de relatórios** (truncado ao pageSize) + scoping de tenant do relatório SaaS de faturamento.
8. **P3 (novo, F14) — Campo `justificativa` no modal FecharCaixa** (comportamento: oculto até surgir diferença, ou sempre visível — decisão de UX) ou relaxar a regra da API; fechar o impasse do smoke.
9. **P3 (herdada, F-R15-C) — Auto-preencher `pedidoItemId`** no form de Recebimentos ao selecionar o pedido.
10. **P3 (herdada) — Fantasmas residuais do catálogo:** `HOSPITAL_AREA`, `MARKETPLACE`, `PROPOSTAS`, `PARCEIRO`, mismatch dormente `OBSERVABILIDADE_GLOBAL`.
11. **P3 (herdada) — Janela "hoje" Meu Dia em fuso do tenant/usuário** (decisão de produto).
12. **P3 (herdada) — Spec de design do tile KPI** (pré-requisito da unificação B18).
13. **P3 (herdada) — Normalização especialidade clínica texto→id.**
14. **P5 (herdada) — `pg_dump` 18 quebrado neste host.**

## Artefatos desta pasta

`r16a0-auditoria-inicial-plano-sprint.md` (achados F1–F8, censo de consumidores, decisões D1–D7, plano de fatos, baseline) · `r16b1-fix-parser-erro-2xx-contrato-binding.md` (fix L302/D2/D3/D4, F9a–c, F10–F12, 12 fatos, baseline do scan 54/141, gate 1475) · `r16b2-smoke-vivo-financeiro-f9d.md` (seed, passos S1–S5 com IDs reais no banco dev, F9d com diagnóstico ao vivo, F14, gate final 1476) · este relatório. Logs reexecutáveis em `%TEMP%\opencode\`: baseline de avisos `r16a0_build.log`, diff de gate `r16b1_warn_base.txt` × `r16b2_warn_now.txt` (Compare-Object vazio), scanner PS de referência `r16b1_fieldscan_v6.ps1` (lógica idem do teste C#).
