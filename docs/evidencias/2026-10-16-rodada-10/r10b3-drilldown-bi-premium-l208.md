# R10-B3 — Drill-down §27-L208: "Abrir registros que compõem o total" nos cards Bi/Premium

- **Rodada:** 10 — fatia 3 de 4 (plano em `r10a0-auditoria-inicial-plano-sprint.md`)
- **Data:** 2026-10-16
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva.

## 1. O que a dívida dizia

`r8a0` L208 (§27): "Abrir registros que compõem o total" — padrão presente em Operacional ("Ver registros"), CommandCenter ("Ver detalhe") e MeuDia (ação primária), **ausente nos cards Bi/Premium** ("KPIs sem drill-down"). R8-B19 deixou como proposta/pendente: "estender padrão — depende das definições §27-B9/B18; sem definição = inventar alvo".

Regra desta fatia (plano r10b3): estender o padrão **apenas** para KPIs cujo conjunto de registros tem uma **rota de listagem existente filtrável pela dimensão do KPI**; KPI sem rota alvo existente fica sem link — **zero link para rota inventada**. Alvos validados por fonte canônica (SQL dos endpoints), não por suposição.

## 2. Recon por fonte canônica — mapa de alvos

### BI Executivo (`Views/Bi/Index.cshtml`, dados de `GET /api/bi/resumo-executivo`)

| Card (id JS) | Fonte do KPI (SQL real) | Decisão | Alvo / motivo |
|---|---|---|---|
| Clientes Ativos | `clientes` reg_status='A' AND upper(status)='ATIVO' | ✅ link estático | `/Clientes?status=ATIVO` — `api/clientes/central` usa **predicado idêntico** (`reg_status='A' and upper(c.status)=@status`) |
| Receita Estimada | `assinaturas` ativa (valor_contratado) | ✅ link estático | `/Assinaturas?status=ATIVA` — `api/assinaturas`: `reg_status='A' and upper(a.status)=upper(@status)` |
| Receita Vencida | `faturas_saas` status='VENCIDA' | ✅ link estático | `/FaturamentoSaas?status=VENCIDA` — endpoint com **escopo automático por contexto** (`@global or f.cliente_id=@scopedClienteId`); 'VENCIDA' já é opção do filtro da view |
| Cobertura (%) | avg ocupação de plantões da janela mensal (exclui rascunho/cancelado) | ✅ link via JS | `/Plantoes?dataInicio=&dataFim=` — caveat: a listagem inclui rascunho/cancelado (superset documentado) |
| Escalas confirmadas no período | escalas confirmado na janela mensal | ✅ link via JS | `/Escalas?status=confirmado&dataInicio=&dataFim=` — caveats documentados no §3 |
| Valor pago no período | `pagamentos` pago por **data_pagamento** na janela | ❌ sem link | `api/financeiro/pagamentos` filtra por **`pl.data_inicio`** (data do plantão) — dimensão diferente; link seria desonesto |

Links de período são montados no JS a partir de `data.periodo` (`yyyy-MM`) e só ficam visíveis se o fetch der ok (`d-none` até então — em falha do serviço não há número nem drill, coerente com R8-B19).

### Dashboards Premium (`Views/DashboardsPremium/Perfil.cshtml`, set fixo de `DashboardPremiumService`)

Set fixo de KPIs para todos os perfis: **Plantões, Escalas, Médicos, Agendamentos** (`reg_status='A'`, escopo clienteId/tenantId, estado atual).

| KPI (título da API) | Decisão | Alvo / motivo |
|---|---|---|
| Plantões | ✅ link | `/Plantoes` — listagem existente com escopo tenant, estado atual |
| Escalas | ✅ link | `/Escalas` (todos os status = o conjunto completo reg_status='A', mesmo escopo) |
| Médicos | ❌ sem link | `api/medicos` (`MedicoService.ListarAsync`) é **diretório global sem escopo de tenant** vs KPI tenant-scoped — alinharia exigir scoping da lista (extensão funcional, fora da rodada) |
| Agendamentos | ❌ sem link | `/agenda` é por período (default semana); não existe rota de "todos os ativos" que reproduza o total |

Mapeamento por título no loop genérico da view (`kpi.Titulo == "Plantões"` / `"Escalas"`) — títulos são fixos e definidos no serviço API; qualquer título novo fica sem link até decisão consciente (o contrato trava isso).

## 3. Caveats registrados (honestidade dos alvos aproximados)

1. **Escalas no período**: o filtro da listagem é `e.status=@s` **exato** vs o KPI contando `lower(status) in ('confirmado','confirmada')` — em dados canônicos o valor escrito pela UI é `'confirmado'`; variante `'confirmada'` (defensiva no KPI) ficaria fora do link se existisse. Janela: listagem exige `pl.data_inicio>=di AND pl.data_fim<=df` vs KPI `pl.data_inicio in [ini,fim)` — plantão que **cruza mês** entra no total mas não na listagem.
2. **Cobertura**: listagem sem filtro de status inclui rascunho/cancelado (KPI os exclui) — superset.
3. **Período via JS**: as datas partem do fuso local do navegador (`.toISOString()`), enquanto o KPI usa o fuso da competência exibida — aproximação documentada; o rótulo da view mostra competência+fuso.
4. **Escopo global vs tenant** nos cards estáticos do BI: as listagens `api/clientes/central` e `api/assinaturas` são globais (sem filtro tenant), e o KPI é escopado por contexto (`@cid`). Para perfil global-admin o conjunto coincide exatamente; para perfil tenant a lista é superset do escopo. Pendência registrada: expor o contexto no payload para o link levar o escopo quando disponível.

## 4. Arquivos alterados

| Arquivo | Mudança |
|---|---|
| `backend/PlantaoPro.Web/Views/Bi/Index.cshtml` | Links "Ver registros": 3 estáticos (Clientes/Assinaturas/FaturamentoSaas com o valor exato do filtro) + 2 dinâmicos `data-drill` (escalas/cobertura, `d-none` até fetch ok, JS monta URL com período); card "Valor pago no período" intencionalmente sem link |
| `backend/PlantaoPro.Web/Views/DashboardsPremium/Perfil.cshtml` | No loop de KPIs: `Plantões → /Plantoes` e `Escalas → /Escalas`, com comentário registrando as decisões (Médicos/Agendamentos sem link e por quê) |
| `backend/PlantaoPro.Tests/R10b3BiPremiumDrillDownContratoTests.cs` | Novo — 4 fatos: (1) cards com alvo têm o drill com os parâmetros exatos; (2) "Valor pago" sem rota honesta não tem link; (3) Premium mapeia só os 2 títulos com rota; (4) anti-inventário: toda rota referenciada tem action `Index` real no projeto Web |

Sem toque em API, banco ou controller — a fatia é 100% Web (views + contrato), como o padrão "Ver registros" de Operacional sempre foi.

## 5. Evidência executada

- Build sln Release: `BUILD_EXIT:0` (0 erros).
- Fatia: filtro `R10b3|R8B18|R8B19|R8B20BiDefinicoes|PosRcUxFinalQa` = **17/17 aprovados** (novos ×4 + scanners globais de views existentes que tocam nestas views — nenhum âncora quebrado).
- Suíte full Release: **`Total de testes: 1421`** (= 1417 pós-r10b2 + 4 novos R10b3), **TEST_EXIT:0 — 1421/1421 limpo** (o `Falha ao registrar auditoria legada` no log é linha não-fatal recorrente em rodadas verdes).

## 6. Pendências residuais desta fatia

- **Link "Valor pago no período"** — exige lista de pagamentos filtrável por `data_pagamento` (extensão de endpoint).
- **Premium Médicos** — scoping de tenant em `api/medicos` (ou parâmetro de escopo no link) para o alvo ser honesto.
- **Premium Agendamentos** — rota de listagem "todos os ativos" (sem default de semana).
- **Escopo tenant nos links estáticos do BI** — context no payload do resumo executivo.
- Navegação webview real (clique nos 5 links + 2 Premium) — se houver janela de homologação.
