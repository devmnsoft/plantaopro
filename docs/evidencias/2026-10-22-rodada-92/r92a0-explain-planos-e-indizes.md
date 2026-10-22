# R92 (B5) — Planos de execução EXPLAIN nos caminhos mais quentes + índices faltantes

**Rodada:** 92 · **Bloco:** B5 (Escala, performance e confiabilidade) · **Data local:** 2026-10-22
**Plano:** `docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md` (linhas 84–85)
**Antecedentes:** R54/R55 (paginação + `ix_adm360_reservas_saldo` v2354), R91 (paginação validada e rotas do bloco B4 desambiguadas)

## Objetivo

Rodar `EXPLAIN (ANALYZE, BUFFERS)` nos caminhos mais quentes dos módulos densos (Orçamentos, Estoque,
N+1 de reserva por item do orçamento), cruzar com o inventário de índices de `adm360_%` e criar os
índices que faltam — travados por contrato.

## Recon — amostra e volumetria

- Banco: `plantaopro_test` · tenant mais denso `d3f6584c-...` (10.172 orçamentos / 9.288 grupos de
  saldo / 17.886 movimentos / 2.623 reservas).
- Detalhe do método (lição de casa): a primeira varredura usou `select distinct tenant_id ... limit 1`
  e pescou um tenant de teste com 1 linha — todos os EXPLAINs saíram `rows=1` e fingiam banco vazio.
  O script commitado (`r92a_recon_explain.ps1`) seleciona o tenant mais denso por contagem, um
  orçamento REAL com reserva ativa (N+1 retorna soma ≠ 0) e um produto com saldo físico liberado (FEFO).
- Estatísticas verificadas frescas antes do "antes": `pg_class.reltuples` dentro de ±3% da contagem
  real, `pg_statistic` populado, autovacuum/autoanalyze ativos. O achado era a amostra, não o banco.

## Achados ("antes", `r92a0_recon_antes.txt`)

| Caminho quente | Antes | Plano | Lacuna |
|---|---|---|---|
| ORC listagem p1 (`ORDER BY created_at DESC LIMIT 50`) | **4,41 ms** (buffers 458) | Seq Scan 10.172 linhas + top-N heapsort | sem índice alinhado ao ORDER BY; custo O(N) por página e por profundidade de OFFSET |
| ORC count paginado | 2,57 ms (buffers 455) | Seq Scan | aceitável (contagem exige ver todas) |
| SALDO listagem p1 (view `adm360_saldos`) | 68,78 ms | HashAggregate 9.288 grupos + **16.873 sondas correlacionadas** nas reservas (SubPlan 1×9.288 + 2×7.585 sobre `ix_adm360_reservas_saldo`, ~37 mil buffers) | custo linear no nº de grupos; já coberto pelo índice parcial da R55 (antes dele: 5,3–5,9 s) |
| SALDO somas dos cards | 63,28 ms | idem (o planner elimina as subqueries só no `count(*)`: 25 ms) | idem |
| **RESERVAS por origem (N+1 por item do orçamento)** | **0,64 ms/item (buffers 102, seq scan de TODAS as reservas)** | Seq Scan filtrando 2.622 linhas por item | **nenhum índice líder em `(tenant_id, origem_tipo, origem_id)`** — o padrão alimenta 8 sites (detalhe/planejamento/cancelamento do `OrcamentoCirurgicoRepository` + join de cirurgias do `CirurgiaRepository`) e cresce com o volume de reservas |
| SALDOS FEFO por produto | 1,79 ms | Index Scan `ix_adm360_movimentos_saldo` | OK |

### Probe da view `adm360_saldos` (candidata REJEITADA)

Testei a reescrita "duas subqueries correlacionadas → um único `LEFT JOIN` numa pré-agregação das
reservas ativas" (`r92a_probe_view.sql`):

- **Equivalência exata**: `EXCEPT` nos dois sentidos = **0 linhas diferentes** (todas as colunas, todos os tenants).
- **Plano pior**: **2.951 ms** vs 68,78 ms do estado atual. O planner empurrou o predicado do tenant
  para dentro da subconsulta, tirou `tenant_id` do grupo derivado e montou um Merge Join cuja condição
  fraca eliminou **26.453.511 pares** por filtro (723 ms só no sort direito do join).

**Decisão:** view segue inalterada. A forma correlacionada + o índice parcial `INCLUDE(quantidade)` da
R55 custa ~5 µs/sonda hoje; reabrir esse redesign só sob evidência de carga (R94), não sob equivalência
lógica isolada.

## Mudança (commit r92a)

1. **Migration v2373** — `database/migrations/2026_10_v2373_r92_indizes_caminhos_quentes.sql`:
   - `ix_adm360_reservas_origem ON adm360_resertas(tenant_id, origem_tipo, origem_id) INCLUDE (produto_id, quantidade) WHERE situacao = 'ATIVA'`
     → a soma por item vira **index-only** na parcial (filtra produto e agrega quantidade sem tocar a heap); o prefixo também atende o join de cirurgias e as buscas sem `produto_id`.
   - `ix_adm360_orcamentos_tenant_criado ON adm360_orcamentos(tenant_id, created_at DESC)`
     → a listagem padrão (e cada página de OFFSET profundo) passa a ser proporcional ao tamanho da página, não ao tenant.
   - Idempotente (`CREATE INDEX IF NOT EXISTS`), reaplicável.
2. **Manifest** — `database/migration-manifest.json`: entrada v2373 (142→143), checksum SHA-256 real
   (`9334ee36…7b4c4`, normalização LF), `dependsOn` v2372, `active`, `transactional`.
3. **Contrato** — `backend/PlantaoPro.Tests/R92IndizesCaminhosQuentesContratoTests.cs` (3 fatos):
   DDL exata nos dois índices; manifest com hash real/dependência/status; presença no banco de teste com
   `pg_get_indexdef` exato — auto-cura pela reaplicação idempotente sob advisory lock + retry de impasse
   40P01 (padrão R55/R89). Execução imediata: **3/3 aprovados**.

## Evidência ("depois", `r92a2_recon_depois.txt`)

| Caminho | Antes | Depois | Plano depois |
|---|---|---|---|
| ORC listagem p1 | 4,41 ms (seq 455 págs + top-N) | **0,11 ms** | Index Scan `ix_adm360_orcamentos_tenant_criado` (rows=50, sem sort) |
| ORC count paginado | 2,57 ms | 2,40 ms | Seq Scan (invariado por construção — contagem) |
| SALDO listagem p1 | 68,78 ms | 63,05 ms | view inalterada (diferença dentro do ruído) |
| SALDO somas | 63,28 ms | 61,44 ms | idem |
| RESERVAS por origem (N+1) | 0,64 ms/item (seq scan 102 págs) | **0,197 / 0,184 / 0,147 ms** (3 reps aquecidas) | **Index Only Scan** `ix_adm360_reservas_origem` |
| SALDOS FEFO | 1,79 ms | 0,38 ms | Index Scan (invariado, cache quente) |

Ganho estrutural, não apenas pontual: a listagem deixa de refazer seq scan + sort a cada página/OFFSET
e o N+1 deixa de escanear a tabela inteira de reservas por item — ambos passam a escala constante no
volume atual e linear apenas na própria janela de consulta.

### Medição ao vivo nas rotas (mesmas rotas da R91, n=3, `r92a3_medicao_saida.txt`)

| Rota | R91 (pré-índices) | R92 (pós-índices) |
|---|---|---|
| `/Administrativo360/Orcamentos` (265 kB) | 544 ms | 594 ms |
| `/Administrativo360/Orcamentos?pagina=2` | — | 415 ms |
| `/Administrativo360/Estoque` (186 kB) | 516 ms | 608 ms |
| `/api/administrativo360/orcamentos` | 113 ms | 100 ms |
| `/api/administrativo360/estoque` | 285 ms | 272 ms |

Na escala atual (~11 mil orçamentos) a latência de página é dominada pelo pipeline BFF→API e pela
serialização de 265 kB — por isso o wall-clock de página fica estável dentro do ruído enquanto o plano
no banco muda de classe (O(N) → O(página)). A demonstração de teto de escala é papel da R94 (carga).

## Gates

| Gate | Resultado |
|---|---|
| A/B Release `--no-incremental` (`r87abuild.ps1 -Tag r92`) | **DIFF=NENHUMA** — avisos únicos 76 base / 76 nova (canon R91) |
| Suíte completa `--no-build -c Release` (1ª, pré-doc) | **1780/1780** (1777 canon R91 + 3 fatos novos) |
| Docscan pós-doc (`r62_docscan.ps1`) | **ARQ_VERIFICADOS=11 · DOCSCAN_BAD=0** |
| Higiene `RepositoryHygieneContractTests` (varre os .md) | **8/8** (suíte 2ª: 1780/1780) |
| Inventário `r75_gap.ps1` | **N/A** — sem nova página/artigo nesta rodada |

Artefatos: `r92a_recon_explain.ps1` (varredura reproduzível) · `r92a0_recon_antes.txt` ·
`r92a_probe_view.sql` + `r92a1_probe_view.txt` · `r92a2_recon_depois.txt` · `r92a3_medicao_pags.ps1` +
`r92a3_medicao_saida.txt`.

## Pendências

- View `adm360_saldos`: 16,8k sondas correlacionadas (~5 µs cada) custam ≈ 50 ms por consulta
  completa; manter sob observação na carga R94 (se grupos crescerem, avaliar reescrita com controle de
  propagação do predicado — o probe ingênuo regrediu 43×).
- ORC count continua seq scan por construção; só viraria problema se a listagem filtrasse menos de um
  percentual pequeno do tenant por coluna indexável (há `ix_adm360_orcamentos_tenant_sit` para o caso).
