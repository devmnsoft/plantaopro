# R8-B10 — `cid_tabela`: unicidade canônica versionada + contrato anti-drift (§29 item 2)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia "`cid_tabela` criada divergente em 6 arquivos" (item 2 da fila §29, ordem quebrado→alto impacto: … → B7 → B8 → B9 → **B10**)
- **Data:** 2026-10-13
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica)

- Matriz `r8a0-auditoria-inicial-matriz-estado.md` §29 item 2 (linha 228): **"`cid_tabela` criada divergente em 6 arquivos (seeds/migrations) — risco de drift de schema."** Varredura `r8a0-varredura-indicadores-design-redundancias.md` Área C1.1 (linha 111): os 6 `create table if not exists plantaopro.cid_tabela` + consumidor import + risco "primeiro `if not exists` define o schema real".
- **Ground truth do banco de teste (psql, `plantaopro_test`):** colunas = superset completo dos 6 DDLs (convergiu via `alter add column if not exists`); **3 índices únicos sobre `codigo` coexistiam**:
  - `ux_cid_tabela_codigo` = `UNIQUE(codigo)` — **constraint de tabela**, global (todas as linhas)
  - `ux_cid_tabela_codigo_ativo` = `UNIQUE(upper(codigo)) WHERE reg_status='A' AND codigo<>''` — índice, global (só ativos)
  - `ux_cid_tabela_versao_codigo_ativo` = `UNIQUE(versao, upper(codigo)) WHERE reg_status='A' AND codigo<>''` — índice, **versionado**
- **O bug:** o importador grava via `ON CONFLICT (versao, upper(codigo)) WHERE reg_status='A' AND codigo<>''` (`Saude360ClinicalService.cs:~L580`) — modelo **versionado** (o mesmo código pode existir em CID-10 e CID-11 ao mesmo tempo). As duas únicas **não-versionadas** contradiziam esse modelo: importar o mesmo código sob duas versões falhava com **23505** no índice plano. Bug **latente** porque o import nos testes nunca cruzou versões. Base tinha **12 linhas, 0 duplicidades de código** → relaxação sem perda de dados.

## 2. Decisões (sem regras inventadas)

1. **Unicidade canônica = versionada.** `ux_cid_tabela_versao_codigo_ativo` é o alvo real do `ON CONFLICT` do import → é a canônica.
2. **Relaxação monotônica (sem perda de dados):** remover as duas únicas não-versionadas (`ux_cid_tabela_codigo`, `ux_cid_tabela_codigo_ativo`); manter a versionada (que ainda impede duplicata *dentro* da mesma `versao`). Verificado: 0 duplicidades existentes → drop seguro. Caminho de **criação manual** (`~L176`) tem pré-checagem app-level **independente** da constraint (UX inalterado); o caminho de **import** é exatamente o corrigido.
3. **Robustez do drop (gotcha Postgres):** `DROP INDEX` **não** remove índice que sustenta uma **restrição**. Cada nome pode ser constraint OU índice puro conforme ambiente/ordem; então, para cada um: `ALTER TABLE … DROP CONSTRAINT IF EXISTS <nome>` e depois `DROP INDEX IF EXISTS <nome>` (o 1º cobre restrição; o 2º cobre índice puro / vira no-op). Idempotente nos dois sentidos.
4. **Reafirmação idempotente da versionada:** `CREATE UNIQUE INDEX IF NOT EXISTS ux_…_versao_codigo_ativo …`. (O **nome** do índice **não** leva esquema em `CREATE INDEX` — só a tabela: `… ON plantaopro.cid_tabela`.)
5. **Sem mudança de colunas, sem migração de tipo, sem regra de domínio nova.** O superset de colunas já converge na ordem de aplicação (DDLs anteriores usam `alter add if not exists`).
6. **Sem deleção de tabela/bloco DDL nesta rodada** — os 6 blocos `CREATE TABLE` permanecem; a deduplicação segue o rito §29 (paralelismo com B9, que só contratou as 5 calculadoras).
7. **Sem alterar os manifests JSON** (`migration/schema/install-manifest.json`): terminam em v2345 (gerado 2026-10-06) e nenhuma fatia R8 (v2340–v2351) os atualizou — artefato curado/periódico, fora do aceite por-fatia.

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `database/migrations/2026_10_v2351_r8b10_cid_tabela_unicao_versionada.sql` | **Novo** (idempotente): remove as 2 únicas não-versionadas (robusto c/ constraint+índice) e reafirma a versionada canônica. |
| `backend/PlantaoPro.Tests/R8B10CidTabelaUnicaoVersionadaTests.cs` | **Novo**: 3 fatos — contrato de schema (só a versionada existe c/ def. correta; as não-versionadas ausentes; superset de colunas presente) + jornada (mesmo código em 2 versões coexiste) + jornada (upsert **exato** do import atualiza sem conflito). |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §29 item 2 (linha 228) → **R8-B10** (unicidade canônica versionada, `v2351`, contrato 3/3); resumo "Estado" (§29) refletindo itens 1+2 com contrato/canonização, sem deleção de tabela/DDL. |

## 4. Evidência (executada)

- **Aplicação da migration (DB de teste persistente):** `psql -v ON_ERROR_STOP=1 -f …v2351….sql` → **EXIT=0**, idempotente (re-execução limpa, só no-ops). Estado pós-aplicar verificado: **1** índice `ux_cid_tabela_*` = `ux_cid_tabela_versao_codigo_ativo` (as 2 não-versionadas removidas).
- **Build sln completa:** EXIT 0 (0 erros, 73 avisos pré-existentes).
- **Alvo:** `R8B10CidTabelaUnicaoVersionadaTests` = **3/3 aprovados** (run isolado).
- **Gate phaseA v2 (pipeline):** **52 testes, EXIT=0**.
- **Full ×3 (pipeline v2):** total agora **1343** (1340 baseline + 3 novos). full1 **1343/1343 EXIT=0**; full3 **1343/1343 EXIT=0**; full2 = **1 falha transitória** em `R7OBOnboardingPreferenciasIdiomaTests.O5_CliqueSemDado_NaoConcluiEtapaComCriterio_409` (**1342/1343**) — classe **do catálogo de flake ①**, **limpa em full1/full3 e 5/5 em execução isolada** (regra ①), alheia ao escopo (onboarding/i18n, não cid_tabela). **Sem flake novo — catálogo inalterado.**
- **Verificação byte-level:** migration contém `drop constraint if exists ux_cid_tabela_codigo`, `drop index if exists plantaopro.ux_cid_tabela_codigo_ativo`, `create unique index if not exists ux_cid_tabela_versao_codigo_ativo on plantaopro.cid_tabela (versao, upper(codigo)) where reg_status='A' and codigo <> ''`; teste contém os 3 `[Fact]` e o `on conflict (versao, upper(codigo)) where reg_status='A' and codigo <> ''` espelhando o importador.

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Ordem de install from-scratch (drift residual):** `apply-local-postgres.ps1` aplica `migrations/*.sql` **ordenados por nome**; `2026_10_*` ordena **antes** dos legados `2026_s…`/`2026_f…`, cujas guardas (`if not exists`) recriariam os índices não-versionados num rebuild do zero **após** o drop. **Não afeta**: DB de teste persistente (correto) nem o deploy incremental (main DB v2339→… aplica `v2351` por último). Consolidar num único `CREATE` canônico que vença qualquer ordem = rito §29 (dedup dos 6 blocos).
2. **Manifests JSON desatualizados:** `migration/schema/install-manifest.json` terminam em v2345; nenhuma fatia R8 os registrou (v2340–v2351 fora deles). O validador Python (`scripts/database_manifest_validation.py`) valida a coerência interna desses JSONs — invariado (não alterados). Atualização = manutenção periódica separada.
3. **Drift de tipo residual (cosmético):** entre os 6 DDLs, `reg_date`/`created_at` aparecem como `timestamptz` vs `timestamp without tz`; no banco real o default é sempre `now()` e nenhum consumidor depende do tz. Documentado, não bloqueante.
4. **Main DB em v2339:** lote de deploy agora inclui **v2340–v2351** (+ IIS), invariado nas demais frentes; janela viva de homologação segue em `r6a1b` §7.
5. **Consolidação dos 6 DDLs de cid_tabela (§29 item 2, rito):** esta fatia fixou a unicidade canônica + contrato anti-drift; a dedup física dos blocos `CREATE` (comparar → canônico único → migrar → regressões) segue o rito, sem deleção antecipada.

## 6. Fila seguinte (R8)

Com §29 item 2 entregue (unicidade canônica versionada + contrato anti-drift, `v2351`), o próximo item da fila §29 = **item 3: Mirror de DTOs Api↔Web ×4** (ManagerCommandCenter, Admin360Resumo, ProductivityPage, DashboardPremium). Alternativas: janela viva de homologação (`r6a1b` §7), ou o push da rodada (pendente).

## 7. Gotchas aprendidos (reutilizáveis)

- **Postgres:** `DROP INDEX` falha quando o índice sustenta uma **restrição** de tabela ("…restrição … o requer"). Use `ALTER TABLE … DROP CONSTRAINT IF EXISTS <nome>`; para cobertura robusta (constraint OU índice puro), faça `DROP CONSTRAINT IF EXISTS` **e** `DROP INDEX IF EXISTS`.
- **Postgres:** o **nome** do índice em `CREATE [UNIQUE] INDEX … <nome> ON esquema.tabela` **não** leva esquema (senão: "erro de sintaxe em ou próximo a '.'").
- **Postgres:** `pg_indexes` **não** tem coluna `isunique`; para flags de unicidade use `pg_index.indisunique`/`indisprimary` (join `pg_class`).
- **xUnit 2.9.2:** preferir `Assert.Contains(item, collection)` / `Assert.DoesNotContain(item, collection)` (membros) e `Assert.True(cond, msg)`; evitar `Assert.Contains(collection, predicate)` (resolvo p/ overload void) e `Assert.NotEmpty(x, msg)`.
