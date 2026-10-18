# R55a0 — Plano: latência server-side do Estoque (view `adm360_saldos`)

Rodada: **R55** · Data: 2026-10-18 · Status: PLANO (pré-implementação)

## 1. Objetivo e origem

Pendência registrada em `r54z-relatorio-final-consolidado.md` (§ Pendência honesta) e promovida a
fila: o endpoint `/api/administrativo360/estoque` respondeu com **7,4–10,3 s mesmo quente** na R54.
A paginação da R54 resolveu o *peso* da página (8,75 MB → 181,9 kB), mas não a *latência* da query.
Este é o próximo item de fila por impacto (r53b1 §7, item 1, metade ainda aberta).

Meta: **< 200 ms por query** no banco de teste (tenant demo, 7.831 saldos), sem mudança de
semântica nenhuma nos valores devolvidos.

## 2. Recon (medido no PostgreSQL local, banco `plantaopro_test`, tenant demo
`d3f6584c-...-7502` = 7.831 das 7.833 linhas da view)

Volumes reais:

| tabela | linhas |
|---|---|
| `adm360_movimentos` | 15.156 |
| `adm360_lotes` | 5.729 |
| `adm360_reservas` | 2.168 |
| `adm360_produtos` | 1.110 |
| `adm360_locais` | 1.980 |
| `adm360_saldos` (view) | 7.833 |

Índices existentes em `adm360_reservas`: apenas `pkey` e
`adm360_reservas_tenant_id_idempotency_key_key`. **Nenhum índice cobre o padrão da subquery
correlacionada** `(tenant_id, produto_id, lote_id, local_id)` com `situacao='ATIVA'`.

Causa raiz (estrutural + medida): a view repete **duas vezes** a mesma subquery correlacionada sobre
`adm360_reservas` por grupo de saldo (`reservado` e, de novo, dentro do `CASE` de `disponivel`).
Sem índice, cada execução é scan completo das ~2.168 reservas → ~7.831 grupos × 2 × scan.

Tempo medido das 3 queries que o endpoint paginado executa por pedido (sem índice):

| query | tempo |
|---|---|
| `count(*)` tenant demo | **78 ms** (planner descarta as expressões não usadas) |
| `COALESCE(SUM(fisico/reservado/disponivel),0)` | **5.339 ms** |
| linhas `ORDER BY produto, validade NULLS LAST LIMIT 100` | **5.871 ms** |

Prova de conceito (índice parcial de teste criado manualmente, `tmp_r55_probe_reservas`):

| query | após índice | valores devolvidos |
|---|---|---|
| SUM agregado | **97,8 ms** (54×) | idênticos (46252 / 3228 / 36952) |
| linhas LIMIT 100 | **56,8 ms** (103×) | idênticos (mesmas primeiras linhas) |

Semântica preservada pelos próprios resultados iguais.

## 3. Decisões

### D-R55-1 — Índice parcial com INCLUDE na migration v2354

DDL exata (estilo dos DDLs existentes dos scripts canônicos, sem espaço entre tabela e coluna):

```sql
CREATE INDEX IF NOT EXISTS ix_adm360_reservas_saldo ON plantaopro.adm360_reservas(tenant_id,produto_id,lote_id,local_id) INCLUDE(quantidade) WHERE situacao='ATIVA';
```

- Nome segue a convenção `ix_adm360_*` já usada (`ix_adm360_movimentos_saldo`, `ix_adm360_lotes_validade`).
- Parcial `WHERE situacao='ATIVA'`: cobre exatamente o padrão da subquery da view; menor custo de
  manutenção (só reservas ativas entram no índice).
- `INCLUDE(quantidade)`: habilita index-only scan na agregação `sum(r.quantidade)`.
- Arquivo: `database/migrations/2026_10_v2354_r55_indice_reservas_saldo.sql`
  (última migration existente: v2353; prefixo de rodada como em `v234x_r7*/r8*/r9*`).
- Convenção do repo (ver v2191): o mesmo DDL entra nos dois scripts canônicos —
  `database/scrpt_completo.sql` e `database/pgadmin/instalar_no_banco_atual.sql` — logo após
  `ix_adm360_lotes_validade`, antes da `CREATE OR REPLACE VIEW plantaopro.adm360_saldos`.

### D-R55-2 — Sem mudança de C# e sem reescrever a view

- `EstoqueRepository` (bases `BasePosicaoSql` + ORDER BY separado da R54) permanece intacto.
- Reescrever a view para `LEFT JOIN LATERAL`/pré-agregação é a alternativa estrutural (custo linear
  em vez de O(grupos × subquery)) e fica documentada como evolução futura se o volume crescer ~10×;
  com o índice, os números ficam ~2 orders de magnitude abaixo da meta e o risco de regressão nos
  6+ consumidores da view (Estoque, OrcamentoCirurgico, ValeConsignacao, Ocorrencias, Inventario,
  Inventarios) não se justifica nesta rodada.

### D-R55-3 — Contrato travando o índice (testes novos: 3 fatos, suíte 1611 → 1614)

Nova classe `backend/PlantaoPro.Tests/R55EstoqueLatenciaContratoTests.cs`:

1. Migration v2354 existe e contém a DDL exata (scan de fonte via `RepositoryPathResolver`,
   normalizando espaços/CRLF).
2. Os dois scripts canônicos contêm a mesma DDL (sincronia das 3 cópias).
3. Fato de banco: sob advisory lock (`hashtext('r55_ix_adm360_reservas_saldo')`) reaplica a migration
   (idempotente) com retry de impasse 40P01 (padrão Bloco7 `ExecutarMigrationComRetryAsync`) e
   afirma que `pg_indexes` contém exatamente 1 `ix_adm360_reservas_saldo` em
   `plantaopro.adm360_reservas` e que as colunas-chave (via `pg_index.indkey`) são, em ordem,
   `tenant_id, produto_id, lote_id, local_id`.

Fato de latência fica fora da suíte (depende da máquina); a prova de desempenho é a medição
versionada (`r55_medicao_estoque.ps1`) + e2e com os apps no ar (mesmo padrão da R54).

## 4. Fora de escopo

- Reescrita da view (D-R55-2).
- Item 2 da fila r53b1 §7 ("Dashboard"→"Painel", decisão de produto) e demais itens.
- Ajuste de `porPagina`/padrões já contratados na R54.

## 5. Metas e gates (todos obrigatórios antes de fechar)

1. **Banco**: aplicar migration no `plantaopro_test`; evidência controlada no próprio psql:
   drop do índice probe → re-medir (deve voltar a ~5 s, confirmando a repro) → criar índice final →
   re-medir (SUM e LIMIT ambos < 200 ms), com valores idênticos aos do `r54z`.
2. **Build real**: teardown das portas 51976/51977 antes; `dotnet build backend\PlantaoPro.sln -c Release --no-incremental`
   → log `%TEMP%\opencode\r55_build.log` → copiar p/ `%TEMP%\opencode\r20b1_build.log` →
   `r20b1_gate.ps1` com `Now=160 Base=160 DIFF=NENHUMA`.
3. **Testes dirigidos**: `R55EstoqueLatenciaContratoTests` (3) + `R54PaginacaoListasDensasContratoTests`
   (6) + `Administrativo360EstoqueEOrcamentosTests` + `Administrativo360Bloco7ExportacaoPropostaTests`.
4. **Suíte integral**: exit 0, **1614** testes (1611 + 3 novos), ~55–75 s.
5. **E2E**: apps no ar (mesmo ritual da R54) → `r52_smoke_e2e.ps1` (`R52_E2E_OK FALHAS=0`) →
   `r54_pager_e2e.ps1` (`R54_PAGER_E2E_OK FALHAS=0`) → medição do log de request logging da API
   mostrando `/estoque` agora rápido (meta: p95 < 1 s no endpoint, vs 7,4–10,3 s) →
   `r55_medicao_estoque.ps1` versionada → teardown por porta com `PORTAS_LIVRES=True`.
6. **Docs**: este plano + `r55z-relatorio-final-consolidado.md` + docscan (`bad=0` nos 2 md).
7. **Commits** sem acentos, prefixo `<secoes> (rodada 55 r55): …`; push canônico
   `git push origin main`.

## 6. Riscos e mitigações

- `CREATE INDEX` durante a suíte paralela: advisory lock serializa + `IF NOT EXISTS` + retry 40P01
  (mesmo padrão já validado no Bloco7). Índice aditivo: zero risco de migração de dados.
- Planner deixando de usar o índice em outro ambiente: mitigado pelo `INCLUDE` + parcial exato do
  padrão; o fato de banco garante existência, a e2e garante efeito no ambiente desta rodada.
- Corrupção de canal do nome do produto (troca de vogal documentada nas rodadas anteriores):
  char codes conferidos nos docs + docscan no fechamento (meta `bad=0`).
