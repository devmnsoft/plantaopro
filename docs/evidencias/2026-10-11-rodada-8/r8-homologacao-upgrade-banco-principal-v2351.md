# R8 — Homologação · Upgrade formal do banco principal `plantaopro` para v2351

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30)
- **Marco:** janela viva de homologação (`r6a1b` §7) — entregável técnico: upgrade do banco principal (backlog item 180: "Upgrade formal do banco principal `plantaopro` … + IIS").
- **Data:** 2026-10-13
- **Estado:** **IMPLEMENTADO + TESTADO** (estado do banco verificado por consultas executadas no Postgres local). **HOMOLOGADO/LIBERADO** seguem a janela viva (aceite do usuário §7 + apontar app/IIS + fiscal P1/P2).
- **Mudança de produção:** zero (infra/schema/dados locais somente; nenhum `.cs`, nenhuma migration nova).
- **Regra de push:** respeitada — commit local apenas; push só por decisão explícita do usuário.

## 1. Situação encontrada (fonte canônica: o próprio banco)

`plantaopro` ("o banco principal", designado em `r6a1b`) estava **divergente e sem dados**:

- **284 tabelas** vs **718** na referência testada `plantaopro_test` → faltavam **435 tabelas**, inteiros módulos (`saas_*`, `adm360_*`, `white_label_*`, `suporte_*`, `bi_*`, `onboarding_*`, `v116_faturamento_*` …).
- ~0 linhas de negócio (usuarios/clientes/medicos/pacientes/tenant_modulos = 0); só 24 linhas de catálogo `modulos_sistema`.
- Rastreamento `schema_migrations` **esparsos/infiáveis** (8 linhas no main, 122 no teste; nomes divergentes `versao`/`version`) → estado real localizado por **sondagem de objetos** + **diff de tabelas**.

Rebuild via `apply-local-postgres.ps1` em banco vazio mostrou-se frágil:

- extensões `uuid-ossp`/`pgcrypto` caem em `public` e ficam **invisíveis** após `SET search_path TO plantaopro` (falha `função uuid_generate_v4() não existe`);
- PS5 + `$ErrorActionPreference=Stop` transforma NOTICE/WARNING do psql (stderr) em erro terminante;
- base incluído `schema/320_v187_…` referencia `fechamento_plantao` (dependência de ordem dentro da base).

## 2. Decisão e mudança

**Espelhar a referência testada.** Como `plantaopro_test` é o estado v2351 completo e comprovado (suíte verde), copiá-lo garante **paridade exata** sem re-executar 150+ migrations (cujas versões antigas **não são idempotentes** → `already exists`).

Sequência executada (cada passo verificado):

1. **Backup** `pg_dump -Fc plantaopro` → `Temp\opencode\plantaopro_main_backup_20261013.dump` (539 KB) — caminho de roll-back.
2. `DROP DATABASE` + `CREATE DATABASE plantaopro` (limpo).
3. `pg_dump --no-owner --no-privileges --no-tablespaces -Fc plantaopro_test` → `main-full.dump` (~17 MB).
4. `pg_restore --no-owner --disable-triggers -d …/plantaopro main-full.dump` (superuser `postgres`).
5. Verificação (paridade + sentinels + contagens).

## 3. Evidência (executada)

- **Paridade de tabelas:** MAIN=718, TEST=718, **0 diferenças** nos dois sentidos (`diff-tables.ps1`).
- **Sentinels da rodada presentes:** v2340 override=Y · v2341 `medico_especialidades`=Y · v2342 `historico_escala.escala_id`=Y · v2343 `plantao_convites.data_resposta`=Y · v2344 `usuarios.idioma_preferido`=Y · v2346 `notificacoes.origem_id`=Y · v2347 `adm360_pedidos.motivo_cancelamento`=Y · v2349 `pacientes.mesclado_em`=Y · v2350 `v115_recurso_documentos`=Y · **v2351 `ux_cid_tabela_versao_codigo_ativo`=Y** e índice legado não-versionado `ux_cid_tabela_codigo`=**GONE**.
- **Contagens-chave idênticas à referência:** usuarios 1470=1470 · clientes 87=87 · modulos_sistema 1142=1142 · tenant_modulos 23=23 · adm360_cotacao_envios 1098=1098 · adm360_vale_itens 2445=2445.
- **Catálogo funcional presente:** perfis 26 · saas_planos 4 · 2 usuários super/admin (login possível).
- **2 avisos FK** de fixtures de teste (`adm360_cotacao_envios`, `adm360_vale_itens`) — ignorados pelo `pg_restore`; contagens finais **idênticas** à referência (nenhuma linha perdida).
- **Artefatos:** backup `plantaopro_main_backup_20261013.dump`; dumps `main-full.dump`/`main-full.sql`; logs `apply-main.log`, `restore-main-full.log`, `pgrestore-main.log`; helpers `diff-tables.ps1`, `build-main.ps1`, `restore-main*.ps1`, `pgrestore-main.ps1` (em `Temp\opencode\`).

## 4. Pendências externas (janela viva — convertem para HOMOLOGADO/LIBERADO)

1. **Apontar app/IIS para `plantaopro`** (user-secret `ConnectionStrings__Default`) + reiniciar pool/site.
2. **Roteiro de aceite do usuário (`r6a1b` §7)** — requer host vivo + tenant real com SAUDE360 + perfis RECEPÇÃO/TRIAGEM: (1) logar perfil clínico → liberado/menu coerente; (2) suspender contrato com sessão aberta → denegação imediata **sem re-login**; (3) derrubar host da API → janela documentada degrada aos claims (perfil contratado liberado; perfil não-contratado com botão "Tentar novamente"); (4) URL direta de módulo nunca contratado; (5) chamada direta à API clínica durante suspensão → 403; (6) layout AccessDenied nos 4 idiomas.
3. **Conector fiscal P1/P2** (credenciais).
4. **Observação:** `plantaopro` agora espelha `plantaopro_test` (inclui fixtures de teste acumulados — usuarios 1470). Se um conjunto limpo de tenants for desejado para a janela, provisionar novo ou limpar fixtures (não bloqueante).

## 5. Gotchas (reutilizáveis)

- `SET search_path TO <schema>` esconde extensões instaladas em `public` → instalar as extensões usadas (uuid-ossp, pgcrypto, btree_gist, unaccent) **no schema do app**.
- PS5 + `$ErrorActionPreference=Stop`: NOTICE/WARNING do psql (stderr) vira erro terminante → usar `-v ON_ERROR_STOP=1` + checar `$LASTEXITCODE` com `Continue`.
- `\"` em identificador com hífen é distorcido pelo wrapper → passar SQL via arquivo + `psql -f`.
- Migrations antigas **não idempotentes** → espelhar a referência (dump/restore) é mais confiável que replay incremental.
- `pg_restore --disable-triggers` absorve FK de fixtures órfãs; mesmo assim podem surgir avisos com contagens finais coincidentes.
