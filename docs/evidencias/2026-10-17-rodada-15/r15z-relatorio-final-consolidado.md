# Rodada 15 — Relatório final consolidado

- **Data:** 2026-10-17 · **Base:** `ca9c8a5` (`origin/main`, ponta do push da Rodada 14) · **Escopo:** topo da fila R14z — **P1b A12**: baixa/devolução dirigida por `destino` (o encerrar com destino de descarte devolvia tudo a LIBERADO no resolver, sem consumo físico). Fora de escopo e seguidos na fila R16: parsers Web F-R14-C, fantasmas do catálogo, drill-downs, menu FaturamentoSaas, F-R14-B/D e demais P3/P5.

## Gate de suíte (Release)

| Slice | Build | Suíte | Nota |
|---|---|---|---|
| Baseline (r15a0) | 0 | 1455/1456 | flake família ① (`40P01` em teardown `PurgarAsync()`) isolado **5/5** — casa preservada, zero regressão |
| Gate final (r15b1) | 0 (34 avisos pré-existentes) | **1463/1463** | delta exato +7 (3 fatos A12 novos + 4 fatos R15 novos); A12 total = 27 fatos (18 fatos + 9 theory) |

## Slices entregues

- **r15a0** (`aa8b5c5`): auditoria com 11 fatos fixados por fonte (F1–F11), defeito ancorado no próprio contrato atual (F4), decisões D1–D8 versionadas, plano de 11 alvos de fato e baseline.
- **r15b1** (`8db207f`, 13 arquivos, +519/−45): implementação completa do efeito físico dirigido — domínio (`EfeitoFisico ∈ {DEVOLUCAO, BAIXA}`, normalização/validação), command compat (campo opcional no fim), writer (liberação sai do RESOLVER e vai para o ENCERRAR ramificado; nova perna única `BAIXA_OCORRENCIA` com chave `{ocorrenciaId}:BXA:{localId}:BAIXA_BLOQUEADO`; espelho compartilhado `LerBlocosRegistradosAsync`), controller/view com seletor e 3 chaves resx ×4 (650→653), contrato de 7 fatos novos (incluindo source-scan e web-bff stub). Pegou o bug `efetivoFisico` × required implícito do NRT antes do gate fechar (F-R15-A).
- **r15b2** (`b08864f`): homologação viva do ciclo completo no banco dev `plantaopro` (tenant SV) — parceiro fornecedor → pedido PC-1807 → recebimento NF-15-0001 (lote L15-001, LIBERADO 10) → ocorrência QUEBRA qtd 3 (bloqueio imediato LIBERADO 7 / BLOQUEADO 3) → tratar → resolver **sem liberação** → encerrar c/ destino "Descarte assistido" e **efeito BAIXA**: perna única `BAIXA_OCORRENCIA` −3 BLOQUEADO com chave canônica, saldo final **LIBERADO 7 / BLOQUEADO 0**. Resíduo autocontido documentado (sem DDL/grant).

## Causa-raiz e fix final (resumo)

O contrato antigo ancorava o efeito de saldo inteiro no RESOLVER (inversão integral do bloqueio registrado), e o destino do ENCERRAR era texto livre sem classe de efeito — encerrar com "descarte" devolvia 100% a circulação. O fix move o efeito para o ponto em que o destino é conhecido (D1), estrutura-o em enum textual canônico com fallback de compat (D2: vazio/nulo ⇒ `DEVOLUCAO`; estranho ⇒ 409 sem efeito colateral), e define a BAIXA como consumo físico por perna única espelhando exatamente o bloqueio registrado no abrir (D3/D5) — a única exceção autorizada à soma física nula do ciclo, inferível dos movimentos sem DDL (D4). RESOLVER/TRATAR ficam atos administrativos sem efeito em saldo (D6).

## Decisões versionadas nesta rodada (D1–D8, resumo)

D1 efeito move RESOLVER→ENCERRAR · D2 campo canônico `EfeitoFisico` (não texto livre) · D3 BAIXA = perna única `BLOQUEADO −q` `BAIXA_OCORRENCIA` · D4 sem DDL/seed (`v2149` mantido) · D5 princípio do espelho (atua só sobre o registrado) · D6 invariantes intactas (máquina estrita, `FOR UPDATE`+versão, tenant, ordem determinística) · D7 cadeia de contrato em 6 pontos · D8 regra de flake família ① (isolar 5×).

## Homologação viva — veredicto

**PASS.** Ciclo autocontido aprovado pelo usuário; evidência bruta em `r15b2-homologacao-viva-baixa.md` §3 (movimentos do lote na ordem: ENTRADA +10 LIBERADO → par BLQ ±3 → BAIXA_OCORRENCIA −3 BLOQUEADO) e IDs completos na tabela da mesma seção. Sem grants aplicados (SV admin já possui ADM360 ativo) — net-zero trivial: nenhum write fora das entidades de smoke do tenant SV, sem grant/schema alterado.

## Achados da rodada

- **F-R15-A (documentado, padrão de casa): required implícito do NRT × `ModelStateInvalidoFiltro`.** Parâmetro `string` não-nulável em action do Web ganha `[Required]` implícito; o filtro global redireciona (302 p/ Referer) **antes da ação e da API** quando o campo chega ausente (ex.: typo `name="efetivoFisico"` na view). Com a view não renderizando `TempData` (gotcha conhecido), a falha é silenciosa. Provação obtida por sonda no filtro (dump de `ModelState`); o contrato web-bff novo pegou o bug antes do gate fechar. Recomendação de casa: source-scan de nomes de campo vs parâmetros em actions que ganham campos (estilo R15).
- **F-R15-B (smoke): confirmação custom `data-confirm` intercepta submissão programática.** `requestSubmit()` só abre o modal; o POST efetivo exige o clique no botão do modal. Padrão operatório para toda smoke browser Adm360 desta base.
- **F-R15-C (novo, P3, UI): `pedidoItemId` do form de Recebimentos não é auto-preenchido** ao selecionar o pedido (hidden segue `00000000-…`; preenchido manualmente no smoke). Gap atingível em pedido real de item único.
- **G-amb: lambda ternário multi-linha quebrou o csc 34× uma vez** (bytes da região ASCII limpos; if-chain resolveu, builds subsequentes determinísticos) — registrado como caso ambiental, sem reprovação.

## Dívidas abertas → Rodada 16 (ordenadas)

1. **P2 (herdada, F-R14-C) — Uniformizar a exposição de erro dos parsers Web:** varrer consumidores de `.Error` em 2xx; o leak "Sucesso" ainda existe em todo parser simples (o action de FaturamentoSaas é imune pela D6). Adesão natural: adicionar à varredura o padrão F-R15-A (nomes de campo × ModelState implícito).
2. **P2 (herdada R13) — Fantasmas atingíveis do catálogo guard:** `CENTRAL_ESCALA` (checar gêmeo `ESCALAS`), `MINHA_AGENDA` (controller só papel `MEDICO`), `MEDICO_AREA` — cada um com decisão canônica própria e evidência de alcance medida.
3. **P2 (herdada R12z §7.2) — Gaps de drill-down (r10b3 §6):** pagamentos filtráveis por `data_pagamento`; scoping de tenant em `api/medicos`; rota "todos os ativos" de agendamentos; contexto de escopo visível no resumo executivo BI.
4. **P2 (herdada R12z §7.3) — Exposição em menu:** decidir onde expor `Bi`/`DashboardsPremium` e `FaturamentoSaas` (rota funcional e homologada; sidebar "Cobranças" só existe na seção global-admin) — decisão de produto.
5. **P3 (herdada, F-R14-D) — Alinhar a intenção dos 500** entre chamada simples (literal fixo) e paginada (mensagem da API).
6. **P3 (herdada, F-R14-B) — Total de paginação do wrapper de relatórios** (truncado ao pageSize) + scoping de tenant do relatório SaaS de faturamento (global por design hoje).
7. **P3 (novo, F-R15-C) — Auto-preencher `pedidoItemId`** no form de Recebimentos ao selecionar o pedido (evitar submissão com GUID vazio → 400 na API).
8. **P3 (herdada) — Fantasmas residuais do catálogo:** `HOSPITAL_AREA`, `MARKETPLACE`, `PROPOSTAS` (`[Authorize]` simples), `PARCEIRO`, mismatch dormente `OBSERVABILIDADE_GLOBAL`→`OBSERVABILIDADE`.
9. **P3 (herdada) — Janela "hoje" Meu Dia em fuso do tenant/usuário** (decisão de produto).
10. **P3 (herdada) — Spec de design do tile KPI** (pré-requisito da unificação B18, incluindo o action-link do r10b3).
11. **P3 (herdada) — Normalização especialidade clínica texto→id.**
12. **P5 (herdada) — `pg_dump` 18 quebrado neste host** (bloqueio ambiental de snapshot).

## Artefatos desta pasta

`r15a0-auditoria-inicial-plano-sprint.md` (recon F1–F11, decisões D1–D8, plano de fatos, baseline com flake ① isolado 5/5) · `r15b1-efeito-fisico-no-encerrar.md` (diff por camada, fatos F1–F4, gotchas G1–G4 com prova, sondas instaladas/removidas) · `r15b2-homologacao-viva-baixa.md` (ciclo vivo completo com IDs reais, evidência final de movimentos/saldos, resíduo documentado, gotchas G2.1–G2.5) · este relatório. Scripts e logs reexecutáveis em `%TEMP%\opencode\`: gate `r15b1_gate.log` (1463/1463), build `r15b1_build6.log`, suíte filtrada `r15b1_filtered.log` (31/31), baseline/smoke `r15b2_base.sql(.bat)(.out)`, `r15b2_final.bat/.out` (evidência final), sondas de tabela `r15b2_tbls*.out`, snapshots `r15b2_pc.bat/.out`, `r15b2_bloq.out`, `r15b2_resolvida.out`, `r15b2_snap.out`.
