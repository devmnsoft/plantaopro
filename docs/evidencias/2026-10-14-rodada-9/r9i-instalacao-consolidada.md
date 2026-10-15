# r9i — s14: manifestos de banco + script consolidado (auditoria §3.10)

Data: 2026-10-15 · Rodada 9 (slice r9i) · Commit local (sem push/merge) · Bancos scratch descartados após validação

## Diagnóstico

- `migration-manifest.json` (caminho de **upgrade**): **completo até v2353** — 126 entradas; checksums conferem com o SHA-256 dos arquivos; v2346-v2353 presentes. Sem alteração necessária.
- `install-manifest.json` (caminho de **instalação limpa**): defasado — `v2.21.8` / 2026-10-04 / seções até ordem 80 (≈v2311). Faltavam **42 migrações v2312-v2353** (incluindo as desta rodada: v2352 ANVISA, v2353 preço de venda).
- Falhas reais em banco novo confirmadas executando o CLI `install` em scratch (transação única), em 3 classes:
  1. **8 tabelas legadas** criadas apenas por migrações fora do install-manifest: `plano_modulos`, `plano_recursos`, `lgpd_consentimentos`, `v116_faturamento_lote_itens`, `cid_tabela` (ALTERs de v2324/v2325/v2331/v2350/v2351) e `clinica_lancamentos`, `clinica_estornos`, `convenio_autorizacoes` (índices/DELETE de v2315) → erro `42P01`.
  2. **Códigos de permissão** exigidos pelos sanity-checks do v2322 (13 códigos `ADM360.*`) e do v2329 (`USUARIOS.VER/CRIAR/EDITAR/CONVIDAR`): no homologado existiam via scripts consolidados pré-manifest; em instalação limpa o seeder canônico cobre só parte → erro `P0001`.
  3. **Divergência de forma**: a base canônica de instalação cria 66 tabelas com forma mais enxuta que o homologado — faltavam as colunas de escopo/auditoria (`tenant_id/cliente_id/parceiro_id/plano_id/metadata/reg_date/reg_update/reg_status/created_at/updated_at/created_by/updated_by`) e campos de domínio → v2333 falhava em `coluna a.reg_status não existe` (`tenant_onboarding_checklist`). Mapeada sistematicamente por diff de catálogo live × instalação-base: **501 colunas em 66 tabelas**.

## Alterações

1. `database/schema/450_v2219_tabelas_legado_gap_instalacao.sql` (novo) — snapshot de schema das 8 tabelas legadas (colunas/constraints/índices do catálogo do homologado, sem triggers/sequences extras) + bloco idempotente final de garantia dos códigos exigidos pelos v2322/v2329 (dedupe por código normalizado `:`→`.`; módulo `ADM360` garantido pelo v2197 já no install; ações/módulo `USUARIOS` canônicos dos seeds/system).
2. `database/schema/460_v2219_colunas_convergencia_instalacao.sql` (novo) — 501 `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` nas 66 tabelas, gerados do diff de catálogo. Regra de segurança: `NOT NULL` mantido somente quando há `DEFAULT` (linhas existentes herdariam o valor); `NOT NULL` sem default entra como NULLABLE para nunca falhar.
3. `database/install-manifest.json` → `schemaVersion v2.21.9` / `generatedAt 2026-10-15`; 99 seções / **128 objetos**: ordem 81 = tabelas de gap, 82 = convergência de colunas (`dependsOn` nos 12 donos das 66 tabelas), 83-124 = migrações v2312-v2353 (`dependsOn` calculado pelo DAG semântico). Validador `scripts/database_manifest_validation.py`: `VALIDATION_OK` (0 erros) em cada geração.
4. `database/scrpt_completo.sql` (+ `scrpt_completo.sha256`, `pgadmin/instalar_no_banco_atual.sql`, `source-checksums.json`, `artifacts/schema-*.json|md`, `artifacts/database-object-coverage.*`) — regenerados via `scripts/generate-scrpt-completo.py` (SHA do script: `2c0b2b9ad5c55bc1706511eef1ff845473e2ad326176f69278c39e30e005b8e4`).

## Evidências

- **Instalação limpa** em banco scratch `r9install_test` (`CREATE DATABASE` → `dotnet exec PlantaoPro.Tools.Database.dll install`):
  - `install concluído com 128 fontes canônicas do manifesto v2.21.9.` · `IDENTITY_SCHEMA_READY` · exit 0.
  - `install_manifest_runs`: `v2.21.9 | 128 | success=true`.
  - Spot-check no scratch: 8 tabelas de gap presentes; `ux_cid_tabela_versao_codigo_ativo` criado (0 linhas em `cid_tabela` = correto — as 12 linhas do homologado vêm de seeds de demo, não de DDL); colunas ANVISA (`anvisa_numero/anvisa_aplicabilidade/anvisa_entrada_manual/anvisa_vigencia/anvisa_justificacao`) e `preco_venda` presentes em `adm360_produtos`; códigos `ADM360.REABRIR/ADM360.CONFERIR/USUARIOS.VER/USUARIOS.CONVIDAR` em `permissoes`; `tenant_onboarding_checklist` com `reg_status/onboarding_id/titulo`; amostra da convergência em `planos/tenants/pacientes` (metadata/reg_status/dominio_customizado/subdominio/consentimento_lgpd/data_nascimento/nome_completo).
- **Caminho de upgrade**: inalterado (manifesto verificado completo até v2353, checksums conferem).
- **Script consolidado**: `scripts/validate-scrpt-completo.py` → `{"ok": true, "coveragePercent": 100.0}`.
- **Build** da solução (Release): 0 erros (80 warnings preexistentes de estilo xUnit). **Suíte**: **1396/1396 aprovados**, 0 falhas.
- Iterações do gate (todas documentadas nos logs): 1ª falha em v2315 (`clinica_lancamentos` ausente) → gap expandido 5→8 tabelas; 2ª em v2322 (códigos ADM360) → bloco de garantia; 3ª em v2329 (códigos USUARIOS) → garantia ampliada; 4ª em v2333 (`reg_status`) → diff sistêmico + arquivo de convergência; 5ª execução = verde.
- Scratch `r9install_test` e `r9diff` descartados após o verde.

## Observações (sem ação neste slice)

- ~368 tabelas legadas continuam fora da curadoria de instalação (decisão de curadoria de rodadas anteriores: base canônica enxuta). Nenhuma é referenciada pelas 42 migrações do lote novo nem pelos contratos de teste atuais; lista completa preservada na análise de diff.
- Instalação limpa entrega estrutura + seeds/system (`seeds/system/*`): tenants/usuarios/planos zerados no banco novo — mesmo comportamento do baseline anterior à rodada; dados de desenvolvimento/homologação vivem em `seeds/development/122|123` (aplicados manualmente).
