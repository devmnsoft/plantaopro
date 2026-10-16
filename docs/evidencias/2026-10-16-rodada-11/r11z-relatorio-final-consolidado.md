# Rodada 11 — Relatório final consolidado (P1 bloqueio imediato A12 + homologação viva)

- **Data:** 2026-10-16 · **Base:** `1b87cb9` (ponta do push da Rodada 10) · **Escopo:** P1 da fila R10 — **bloqueio imediato de saldo ao abrir ocorrência A12** (spec L112/L296) — mais a janela de homologação webview autorizada pelo usuário. Fora: gaps de drill-down (R10), janela UTC Meu Dia, tiles B18, especialidade B16, pg_dump P5 (todos seguem na fila §6).
- **Regra de push:** respeitada — **3 commits locais em `main`, sem push** (pendente de autorização ao fim desta rodada).

## Gate de suíte (Release)

| Porta | Resultado | Nota |
|---|---|---|
| Baseline (`1b87cb9`) | 1421 total — 1 falha em `R7OBOnboardingPreferenciasIdiomaTests.O3` (`Npgsql 40P01` em teardown `PurgarAsync()`, **classe flake catálogo ①** — mesma classe já registrada em r8b10/r8b22/r8b23/r8b25), **isolada 5× 5/5 EXIT 0** | não-regressão; baseline efetivamente limpo |
| Pós-r11b1 | **1428/1428 limpo** | 1421 base + 7 fatos novos do bloqueio |
| Pós-r11b2 (estado final) | suíte do r11b1 vigente | slice só ambiente/dados/docs — nenhum binário alterado |

Δ testes da rodada: **+7** (fatos do bloqueio imediato na classe A12; o invariante "não apaga estoque" foi renomeado/reafirmado para o caso sem saldo LIBERADO e ganha a variante net-zero com bloqueio).

## Slices entregues

| Commit | Slice | Entrega |
|---|---|---|
| `0c97b95` | r11a0 | Auditoria inicial / plano: baseline acima; matriz D1 com recon por fonte canônica (view `adm360_saldos` agrupa por condição ⇒ mudança de condição = duas pernas somando zero — idioma `QualidadeRepository`/`EstoqueRepository`); decisões fixadas (momento, alcance, ordem, parcial, liberação, chaves, trancas); fila ordenada |
| `04a4264` | r11b1 — D1/P1 | **Bloqueio imediato de saldo**: `AbrirAsync` escreve pernas `BLOQUEIO_OCORRENCIA` (LIBERADO −q / BLOQUEADO +q) na mesma transação da abertura, cobrindo até `quantidade` do disponível do lote em **todos os locais ativos internos e externos** (local da ocorrência primeiro; demais por `(codigo, id)`; parcial sem rejeitar); `TransicionarAsync` no RESOLVIDA escreve `LIBERACAO_OCORRENCIA` espelhando exatamente o registrado por `origem_tipo='OCORRENCIA'`; TRATAR/ENCERRAR sem pernas; reserva/expedição fail-closed por construção (view só conta LIBERADO); motivo/origem consultáveis (L296); chaves idempotência `{ocorrencia_id}:{BLQ\|DEL}:{local_id}:{PERNA}`; advisory locks `{tenant}:{produto}:{lote}:{local}` + validação de inventário por local; **sem DDL, sem API, sem Web, sem resx**; classe A12 24/24; full 1428/1428 |
| `d2189a1` | r11b2 — homologação webview | CRUD Ocorrências completo ao vivo (tenant SV): abrir qtd 8 → bloqueio **DEP(INTERNO) 6 + EXT(EXTERNO) 2** (frase da spec literalmente coberta) → tratar (bloqueio persiste) → resolver (saldo restaurado, net-zero) → encerrar c/ destino+justificativa visíveis na linha; Estoque exibe/filtra BLOQUEADO; seeds reversíveis tagadas documentadas com rollback; evidência DOM versionada (4 artefatos) + verbatim SQL de cada estado; **ACHADO P2 novo e reprodutível** (abaixo) |

## Contrato alterado na rodada

- `Administrativo360OcorrenciasA12Tests`: invariante renomeado para o caso que cobre (`A12_SemSaldoLiberado_CicloCompleto_NaoGeraPernasDeBloqueio`) + 7 fatos novos (pernas com origem/motivo e queda de disponível; reserva acima do disponível falha em ABERTA e EM_TRATAMENTO; multi-local interno+externo em ordem determinística; parcial sem rejeitar; resolver inverte + ciclo net-zero + reserva volta a ser possível; TRATAR/ENCERRAR sem pernas; escopo tenant). Helpers de saldo/pernas adicionados. Sem mudança de contrato Application/rotas/resx (nenhuma literal nova de UI).

## Decisões versionadas nesta rodada (resumo do plano r11a0 §2 + ajustes de implementação)

1. **Momento:** bloqueio no ABRIR, mesma transação serializável; persiste em EM_TRATAMENTO.
2. **Alcance:** saldo LIBERADO não vencido do lote em todos os locais ativos (INTERNO e EXTERNO); ordem determinística local-da-ocorrência → `(codigo, id)` (ajuste vs plano: id como desempate, pois unicidade de `codigo` por tenant não está garantida no DDL); **parcial** se saldo < quantidade (abertura nunca rejeitada por saldo).
3. **Liberação só em RESOLVER**, espelhando o registrado (nada recalculado) ⇒ ciclo completo net-zero ⇒ "registro de ocorrência não apaga estoque" vale para o caminho com bloqueio. Caveat registrado: inventário que ajustar a quantidade bloqueada entre abrir/resolver — liberação espelha o registrado; divergência física é papel do inventário.
4. **Chaves/idem:** chave por `local_id` + sufixo de perna (dois INSERTs por local) — ajuste documentado vs formato do plano.
5. **Sem exposição visual inventada:** motivo do bloqueio nos movimentos (consultável); se a homologação futura apontar falta de visibilidade na tela, pendência visual — não criar tela fora de escopo.

## Achado vivo desta rodada (nova dívida P2, reproduzido 2×)

- **`/Bi/*` e `/DashboardsPremium/*` → `CATALOGO_NAO_CONFIGURADO` para TODO autenticado** (inclui global admin): `SaasRouteGuardFilter.ControllerModules` sem entradas para `BiController`/`DashboardsPremiumController`; controllers fora do catálogo são barrados antes de qualquer checagem de módulo/role. Nenhuma view referencia esses controllers (telas dormentes, sem menu) — padrão exato dos defeitos já corrigidos em R6-BlocoA (`Caixa`) e R7-E1 (`Usuario`). Bloqueia integralmente o clique nos 7 drill-downs da R10 (pendência §27-L208). Correção proposta detalhada em `r11b2-homologacao-bloqueio-a12-drilldowns.md` §4 (entrada no catálogo + ação de permissão + decisão de exposição + contrato anti-furo).

## Dívidas abertas → próximas rodadas (ordenadas)

1. **P2 — Fix do catálogo do guard** (`Bi` + `DashboardsPremium`) + ação de permissão + decisão de exposição (menu) + contrato estilo R6-BlocoA — desbloqueia o item 3. *(novo, achado r11b2)*
2. **P2 — Gaps de drill-down (r10b3 §6):** lista de pagamentos filtrável por `data_pagamento` (card "Valor pago"); scoping de tenant em `api/medicos`; rota de "todos os ativos" de agendamentos; contexto de escopo no payload do resumo executivo BI (links estáticos globais × KPI tenant-scoped).
3. **P2 — Homologação residual viva:** clique nos 7 drill-downs (requer item 1 + grants reversíveis de módulos `BI/CLIENTES/ASSINATURAS/BILLING/ESCALAS/PLANTOES` ao tenant SV, estilo r8b26) + **capturas de pixel** pendentes de janela em 1º plano (estados preservados; ocorrência nº2 ABERTA com bloqueio ativo) + rollback das seeds §2 do r11b2.
4. **P1b — A12 baixa/devolução dirigida por `destino`:** encerrar com destino = descarte, p. ex., ainda devolve tudo a LIBERADO no resolver (sem consumo físico pelo destino) — extensão funcional própria, fora desta máquina estrita (caveat versionado no r11b1 §5).
5. **P3 — Janela "hoje" Meu Dia em fuso do tenant/usuário** (mudança de comportamento; decisão de produto).
6. **P3 — Spec de design do tile KPI** (pré-requisito da unificação B18, incluindo o action-link do r10b3 na API do tile).
7. **P3 — Normalização especialidade clínica texto→id** (rodada própria: backfill nome→id, picker, dual-write).
8. **P5 (herdada) — `pg_dump` 18 quebrado neste host** (tooling de snapshot; bloqueio ambiental).

## Artefatos desta pasta

`r11a0-auditoria-inicial-plano-sprint.md` (plano + painel de fechamento atualizado) · `r11b1-bloqueio-imediato-a12.md` · `r11b2-homologacao-bloqueio-a12-drilldowns.md` (+ `artefatos-dom/` com 4 capturas DOM) · este relatório · `imagens/` (reservada para as capturas de pixel pendentes).
