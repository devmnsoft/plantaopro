# Relatório BLOCO E — Testes reproduzíveis no repo, banco unificado e seeds idempotentes

| Campo | Valor |
|---|---|
| Data | 2026-09-28 |
| Ambiente | Local dev (Windows) — API `https://localhost:51977` · Web `https://localhost:52977` |
| Banco | PostgreSQL 18 — **`plantaopro_test`** (dev+test unificados, schema `plantaopro`); `postgres` congelado como referência |
| Conta principal | `gestor@santacasa-demo.example` (tenant Santa Casa `d3f6584c-…-7502`) |
| Scripts no repo | `scripts/homologacao/api_matrix.ps1` · `scripts/homologacao/check-seed-idempotency.ps1` |
| Evidências | `artifacts/homologacao/evidencias/bloco-e/` — `api-matrix.log` · `seed-idempotencia.log` · `xunit.log` (extensão `.log` gitignora; regeneráveis pelos scripts acima) |

Status: **COMPLETO** — todos os itens do Bloco E **APROVADOS** (matriz no repo 10/10, banco unificado, 20 arquivos de seed reparados, idempotência ×2 com delta zero em 324 tabelas, verificação pós-seed verde). Declaração final: **MVP INTERNO APTO PARA HOMLOGAÇÃO**.

---

## 1. E1 — Matriz de API dentro do repo (APROVADO)

- `api_matrix.ps1` movida para `scripts/homologacao/api_matrix.ps1`, **autocontida** (sem dependências de scratch: curl + psql + helpers internos, `param(-ApiBase, -DbName, -LogPath)`).
- Execução pré-seed: **10/10 PASS (fails=0)** — M1 login GESTOR (49 permissões, 32 ADM360), M2/M2b reads, M3 escrita `MAT9001-HOMOLOG` com persistência, M4/M5 consulta/auditoria, M6/M7 recusas de escrita, M7b auditoria.
- Reexecução pós-seed (§5): **10/10 PASS (fails=0)** sobre o banco semeado.

## 2. E2 — Banco unificado `plantaopro_test` (APROVADO)

| Alvo | Mudança |
|---|---|
| Secrets API (`plantaopro-api-development`) + Web (`plantaopro-web-development`) | `Database=plantaopro_test` — um único banco para servidores e testes |
| `PostgreSqlTestFixture.cs` | Senha padrão alinhada ao ambiente local (`123456`); DB já era `plantaopro_test` (com convenção do override `PLANTAOPRO_TEST_CONNECTION`) |
| `plantaopro.schema_migrations` (em `plantaopro_test`) | `v2199` + `v2201` aplicadas e registradas manualmente |

**Exceção documentada:** migrations aplicadas via `psql` neste ambiente (política oficial de runner/backfill ainda é a pendência P7). `postgres` ficou **congelado como referência**: mantém a função quebrada de gatilho v2190 e não recebeu v2201 — não é mais o banco de trabalho de nenhuma execução.

## 3. E3 — Reparos dos seeds contra o schema atual (APROVADO)

### Causas raízes identificadas e corrigidas

| # | Achado | Causa raiz | Correção |
|---|---|---|---|
| S1 | Seeds top-level falhavam com colunas inexistentes | A onda v2200 trocou colunas ricas por `dados jsonb` em `pacientes`/`convenios`/`cid`/`planos_saude` — mas as `clinica_*` **permaneceram legado ricas** (sem `dados`) | Mapping duplo: jsonb `dados` nas v2200; colunas reais nas ricas (`forma_cobranca`, `conferencia_observacao`, `aberto_em`); "valor pago" derivado de `valor_total − valor_pendente` |
| S2 | Seed 140 falhava em qualquer INSERT de tenant | Gatilho `adm360_validar_tenant()` quebrado pela v2190 (PL/pgSQL resolve `NEW.campo` na compilação da statement; AND não short-circuita) | `v2199` (IF/ELSIF por tabela) aplicada em `plantaopro_test`; 140 ficou **verde sem edição** |
| S3 | Erros de sintaxe só visíveis em runtime | `INSERT … VALUES … WHERE` é inválido (WHERE exige forma SELECT); `desc` não vale como alias em lista de colunas de FROM | Conversões para forma SELECT e renomeio de alias nos shims |
| S4 | `v114_checklist_implantacao` +4 e `v114_timelines` +2 **por execução** | `ON CONFLICT DO NOTHING` sem alvo não desduplicava (único unique é o pkey uuid gerado) | `not exists` na chave natural (`titulo,perfil_responsavel,ordem` / `entidade,evento`) + remoção das 36 linhas duplicadas acumuladas na base de teste |
| S5 | Violation de unique em `escalas` em re-aplicação | Índice parcial único `ux_escala_ocupacao_ativa` (plantao_id, medico_id) + guarda por status que não batia com formas femininas existentes (`confirmado` × `confirmada`) | Inserts excluem pares já ocupados por escala ativa + `ORDER BY p.id, m.id` determinístico; pagamentos casa `realizado`/`realizada` com `COALESCE(valor,0)` |

### Arquivos reparados (todos reaplicáveis)

- `database/seeds.sql` (canônico): especialidades por nome; escalas/pagamentos com as guardas S4/S5.
- 8 shims top-level em `database/seeds/` (padrão house-style: `DO` + `to_regclass` por tabela, dedupe por chave natural, seleção determinística `ORDER BY <ts> NULLS LAST, id`, colunas sumidas preservadas em `dados jsonb` quando a tabela tem): `2026_demo_comercial_premium_saude360.sql`, `2026_demo_comercial_saude360.sql`, `2026_demo_jornada_saude360_api_keys.sql`, `2026_demo_saas_comercial_completo.sql`, `2026_demo_saude360_complementar.sql`, `2026_demo_v116_consolidacao_operacional.sql`, `2026_demo_v117_runtime_integrado.sql`, `2026_saude360_demo_convenios_financeiro_cid.sql`, `2026_saude360_demo_seed.sql`.
- `2026_demo_v114_consolidacao_produto.sql` (S4 acima).
- Seeds de desenvolvimento `122_usuarios_homologacao_plantaopro.sql` e `130_operacao_demo_santacasa.sql` (autoconsistência entre re-aplicações).
- `140_administrativo360_demo.sql`: **sem edição** — causa raiz era apenas a v2199 pendente (S2).

**Probe completo: 20/20 arquivos OK** (aplicação individual contra `plantaopro_test` semeado).

## 4. E4 — Idempotência de seeds ×2 sem duplicação (APROVADO)

`check-seed-idempotency.ps1` (novo, no repo): snapshots de contagem por tabela — S0 → run 1 → S1 → run 2 → S2 — sobre **20 arquivos de seed** (canônico + 18 top-level + development) e **324 tabelas monitoradas**; critério: `delta(S2−S1) = 0` em todas.

| Rodada | Resultado |
|---|---|
| 1ª execução (pré-fix v114) | fails=2 — `v114_checklist_implantacao` +4, `v114_timelines` +2 → corrigidos (S4) |
| **Execução final (formal)** | **fails=0 — nenhuma linha criada na segunda aplicação** (`seed-idempotencia.log`) |

## 5. Verificação pós-seed (APROVADO)

Após a semente final em `plantaopro_test`, com servidores no ar sobre o mesmo banco:

| Check | Esperado | Real |
|---|---|---|
| Matriz API (`api_matrix.ps1`) | 10/10 PASS | ✅ 10/10 PASS (fails=0) — login/gestor, reads, escrita persistida, recusas, auditoria |
| Suíte xUnit (`PlantaoPro.Tests.csproj`) | 668/668 | ✅ **668/668 aprovados, 0 falhas** (5 s, EXIT=0) — flake conhecido não se repetiu nesta corrida |

## 6. Entregáveis do bloco — status

| # | Item | Status | Evidência |
|---|---|---|---|
| E1 | Testes de homologação reproduzíveis no repo (`api_matrix.ps1` + `check-seed-idempotency.ps1` em `scripts/homologacao/`) | **APROVADO** | scripts commitados; 10/10 PASS; fails=0 |
| E2 | Banco unificado `plantaopro_test` (secrets API+Web + fixture da suíte) | **APROVADO** | diff dos secrets/fixture; suíte 668/668 sobre o mesmo banco |
| E3 | Migrations v2199/v2201 aplicadas e registradas em `plantaopro_test` | **APROVADO** | `schema_migrations` atualizado (exceção manual documentada — P7) |
| E4 | Seeds reaplicáveis: 20 arquivos, reparados e verdes | **APROVADO** | probe 20/20 |
| E5 | Seed ×2 sem duplicação | **APROVADO** | `seed-idempotencia.log` — delta zero, 324 tabelas |
| E6 | Relatório + atualização do roadmap + commit único | **APROVADO** | este arquivo + `PENDENCIAS-ROADMAP.md` §3.4 |

Itens **FALHOU**: nenhum. **BLOQUEADO**: nenhum. **NÃO EXECUTADO**: nenhum (o escopo do bloco não incluía novos módulos — nenhum fix dos blocos A–D foi reapresentado como funcionalidade nova).

## 7. Observações e follow-ups documentados

- `schema_migrations` segue a exceção manual (P7): v2199/v2201 registradas em `plantaopro_test` com checksums do manifest; política oficial de runner/backfill continua em aberto no roadmap (Fase 2).
- `migration-manifest.json` tem 69 entradas vs 75 arquivos de migration em disco (6 fora do manifest) — diferença observável, não computada nesta execução.
- `postgres` permanece congelado (função quebrada do gatilho v2190; sem v2201) até a decisão do backfill — usar `plantaopro_test` para qualquer nova execução.
- Sessões em memória continuam zerando no restart (comportamento dev pré-existente, já documentado no BLOCO D).
