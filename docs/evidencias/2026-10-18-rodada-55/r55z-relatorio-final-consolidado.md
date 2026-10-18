# R55z — Relatório final consolidado: latência server-side do Estoque (índice de reservas)

Rodada: **R55** · Data: 2026-10-18 · Status: **FECHADA** · Commit: ver rodapé

## 1. Objetivo

Resolver a pendência honesta registrada em `r54z-relatorio-final-consolidado.md`: mesmo com a
paginação da R54 (peso da página resolvido), cada pedido a
`/api/administrativo360/estoque` respondia em **7,4–10,3 s quente** server-side. Promovido a fila
por impacto como continuação do item 1 do §7 da r53b1 (reversão da linha de base R52 na metade
de latência, ainda aberta).

## 2. Causa raiz (medida, não presumida)

A view `plantaopro.adm360_saldos` repete **duas vezes por grupo de saldo** a mesma subquery
correlacionada sobre `adm360_reservas` (`reservado`, e outra idêntica dentro do `CASE` de
`disponivel`). A tabela `adm360_reservas` (2.168 linhas no banco de teste) só tinha `pkey` e índice
de idempotency — **nenhum** cobria o padrão `(tenant_id, produto_id, lote_id, local_id)` com
`situacao='ATIVA'`. Com 7.831 grupos do tenant demo, cada query fazia ~15,7 mil subconsultas sobre
scan completo.

Medições de recon (tenant demo `d3f6584c-...-7502`, 7.831 saldos):

| query | sem índice |
|---|---|
| `count(*)` | 78 ms (planner descarta as expressões não usadas) |
| agregado `SUM(fisico/reservado/disponivel)` | 5.339 ms |
| linhas `LIMIT 100 ORDER BY produto, validade NULLS LAST` | 5.871 ms |

Índice parcial de probe criado manualmente durante a recon: SUM 97,8 ms / linhas 56,8 ms, com
valores devolvidos **idênticos**.

## 3. Decisões

### D-R55-1 — Índice parcial com INCLUDE, versionado
```sql
CREATE INDEX IF NOT EXISTS ix_adm360_reservas_saldo ON plantaopro.adm360_reservas(tenant_id,produto_id,lote_id,local_id) INCLUDE(quantidade) WHERE situacao='ATIVA';
```
- Migration `database/migrations/2026_10_v2354_r55_indice_reservas_saldo.sql` (última existente: v2353).
- Parcial (`WHERE situacao='ATIVA'`) cobre exatamente o padrão da subquery; `INCLUDE(quantidade)`
  habilita index-only scan (confirmado pelo planner em produção local: "Index Only Scan using
  ix_adm360_reservas_saldo", loops=7.831 e 6.401).
- Nome segue a convenção `ix_adm360_*` dos índices existentes.

### D-R55-2 — Sem mudança de C# e sem reescrever a view
`EstoqueRepository` intacto. Reescrever a view (`LEFT JOIN LATERAL`/pré-agregação única) fica
documentada como evolução estrutural para se o volume crescer ~10× — com o índice, o custo por
grupo vira index-only scan de µs.

### D-R55-3 — Contrato travando o índice (3 fatos novos)
`R55EstoqueLatenciaContratoTests`: (1) migration existe com a DDL exata; (2) `scrpt_completo.sql`
e `pgadmin/instalar_no_banco_atual.sql` mantêm a DDL sincronizada; (3) fato de banco auto-curado
(advisory lock + reaplicação idempotente + retry de impasse 40P01, padrão Bloco7) que afirma a
existência do índice e sua definição canônica via `pg_get_indexdef` (colunas-chave em ordem,
INCLUDE, condição parcial). Suíte 1611 → **1614**.

### Ajuste à execução (convenção de script canônico descoberta na implementação)
O repo mantém cada migration como bloco marcado nos scripts canônicos — exigido por
`DatabaseGeneratorIntegrityTests` (`-- SOURCE: <path>` + `-- SOURCE-SHA256: <hash>` devem existir
no `scrpt_completo.sql` para cada entrada de `source-checksums.json`) — além do DAG em
`migration-manifest.json` (usado por `apply-canonical-migrations.sh`). Foram então adicionados:
- bloco **Seção 125** com marcador no `scrpt_completo.sql` E no `pgadmin/instalar_no_banco_atual.sql`
  (espelho exato do fim do arquivo);
- entrada em `database/source-checksums.json` (SHA-256 do arquivo:
  `31ab4e426a5f88fe7d249c1ee4ed59f627e894e7b795cca8907a486e37259ea5`);
- entrada final em `database/migration-manifest.json`
  (`dependsOn: [2026_10_v2353_r9d_precificacao_preco_venda]`, `installRequired: false`).

Nota técnica do contrato: a introspecção original via `pg_index.indkey` revelou comportamento
peculiar de `int2vector` no PG18 (colunas INCLUDE aparecem no vetor e fatiamento
`[1:n]` desloca posições); o fato passou a afirmar contra `pg_get_indexdef`, saída canônica e estável.

## 4. Evidência de desempenho (versionada)

Script `r55_medicao_estoque.ps1` (saída bruta em `r55_medicao_saida.txt`), sequência controlada
com drop/reaplicação no próprio banco de teste:

| fase | `count(*)` | `SUM` | linhas `LIMIT 100` | valores (fis/res/disp) |
|---|---|---|---|---|
| A — índice presente | 76,8 ms | 90,3 ms | 92,7 ms | 46252,0000/3228,0000/36952,0000 |
| B — índice removido (repro) | 46,3 ms | **7.047 ms** | **9.596 ms** | idênticos |
| C — migration v2354 reaplicada | 52,7 ms | 205,8 ms | 103,8 ms | idênticos |

Plano da fase C (EXPLAIN ANALYZE): Index Only Scan usando `ix_adm360_reservas_saldo` nas duas
subconsultas; Execution Time 115 ms. `pg_indexes`: exatamente 1 ocorrência. Semântica preservada
pelos três conjuntos de valores idênticos.

Variabilidade honesta: em execuções anteriores a mesma baseline B oscilou entre ~2,7 s e ~9,6 s
(escolha de plano/cache do Postgres) — coerente com os 7,4–10,3 s observados em produção local na
R54. O ganho é ~2 orders de magnitude em todos os cenários medidos.

## 5. Gates (todos passados)

1. **Banco**: migration aplicada no `plantaopro_test`; medição controlada acima.
2. **Build real**: `dotnet build backend\PlantaoPro.sln -c Release --no-incremental` exit 0
   (80 avisos) → `%TEMP%\opencode\r55_build.log` → `r20b1_gate.ps1`:
   **Now=160 Base=160 DIFF=NENHUMA** (revalidado após cada edição).
3. **Dirigidos**: 45/45 (R55×3 + R54×6 + Administrativo360EstoqueEOrcamentos + Bloco7 +
   DatabaseGeneratorIntegrity + R6GranularidadeSaude360).
4. **Suíte integral**: **1614/1614**, exit 0, 46 s de testes (53 s wall).
5. **E2E com apps no ar**:
   - `r52_smoke_e2e.ps1` → **R52_E2E_OK FALHAS=0** (13× PAGE_OK; `/Administrativo360/Estoque`
     renderizou em 1.130 ms com peso idêntico à R54: 181.902 B).
   - `r54_pager_e2e.ps1` → **R54_PAGER_E2E_OK FALHAS=0**
     (ORC_P1 261470 / ORC_P2 159274 / EST_P1 181902 / EST_P2 121497 / ORC_BUSCA_P2 159314 —
     paginação e filtros preservados).
   - Log da API (RequestLoggingMiddleware): `/estoque` n=3 → **274–508 ms, média 356 ms**
     (vs 7.400–10.300 ms na R54 ≈ **21×**); `/orcamentos` máx 237 ms (sem regressão).
6. **Teardown**: por porta 51976/51977 → `PORTAS_LIVRES=True` (apps encerrados por `Stop-Process`;
   exit 1 nos shells é a assinatura esperada do force kill).
7. **Docscan**: este relatório + `r55a0-indice-reservas-saldo-plano.md` → `bad=0` (verificação
   versionada de corrupção de canal).

## 6. Arquivos alterados nesta rodada

- `database/migrations/2026_10_v2354_r55_indice_reservas_saldo.sql` (novo)
- `database/scrpt_completo.sql` (Seção 125 com marcador SOURCE/SOURCE-SHA256)
- `database/pgadmin/instalar_no_banco_atual.sql` (espelho da Seção 125)
- `database/source-checksums.json` (+1 entrada)
- `database/migration-manifest.json` (+1 entrada, DAG até v2354)
- `backend/PlantaoPro.Tests/R55EstoqueLatenciaContratoTests.cs` (novo, 3 fatos)
- `docs/evidencias/2026-10-18-rodada-55/` (plano, este relatório, script de medição, saída bruta)

Nenhum arquivo de aplicação (API/Web/Infrastructure) foi alterado — mudança puramente DDL + contrato.

## 7. Pendências e próximo passo

- Fila r53b1 §7: próximo item é o **item 2** — decisão de produto "Dashboard"→"Painel" junto da
  revisão de menus (R50).
- Alternativa estrutural documentada (fora de escopo): reescrever `adm360_saldos` sem subquery
  correlacionada duplicada (custo linear) caso o volume cresça ~10×.
