# RELATORIO — BLOCO A: Ciclo de dev reproduzível + Paridade dos caminhos oficiais de instalação (Plantão Pro)

- Data: 2026-09-27 (UTC)
- Base: commit `e7618fd` (branch `main`)
- Status geral do bloco: **APROVADO** (todos os critérios do BLOCO A atendidos; sem FALHOU/BLOQUEADO)

## 1. Escopo (brief)

1. Ciclo de vida reproduzível de desenvolvimento: scripts com exit codes coerentes com a documentação,
   readiness/liveness por HTTP real, diagnóstico sem segredos, cleanup apenas da tentativa própria,
   stop com reconfirmação de identidade por path/cmdline.
2. Segredos fora dos `launchSettings.json` (user-secrets por projeto/ambiente), exemplos sem credenciais
   reais e documentação da rotação das credenciais já versionadas.
3. TODOS os caminhos oficiais de instalação chegam ao mesmo schema (cenários A–E).

## 2. Ciclo de dev reproduzível — APROVADO

Scripts em `scripts/local/` (reescritos neste bloco):

| Script | Função | Exit code |
|---|---|---|
| `setup-local-config.ps1` | cria user-secrets idempotentes (Api e Web, perfil Development); reexecução imprime `[mantido]` (verificado 2×) | 0 ok / !=0 erro |
| `run-dev-build.ps1` | compila a solução; identifica instâncias desta cópia por path/cmdline antes | 0 ok / !=0 erro |
| `run-dev-start.ps1` | inicia Api+Web, aguarda readiness/liveness por **HTTP real** e roda smoke | 0 ok / !=0 erro |
| `run-dev-stop.ps1` | lista instâncias desta cópia, pede reconfirmação de identidade, para e libera artefatos de build | 0 ok / !=0 erro |

Validação executada: ciclo completo `stop(0) → build(0) → start(0)` + smoke com resultado **idêntico ao baseline 105845**
(diag 404; Login GET 200 ~10918 bytes / POST 400 antiforgery / PUT 405 [GET,POST]; Bogus 404; SalvarParceiro 405 [POST];
API health 200 `"status":"Healthy"`). Contrato documentado em `artifacts/homologacao/RELATORIO-ETAPA0-COMPILACAO.md` §6-7.

## 3. Segredos — APROVADO

- User-secrets configurados nos 2 projetos (`PlantaoPro.Api` / `PlantaoPro.Web`, perfil `Development`);
  stores em `%APPDATA%\Microsoft\UserSecrets\{plantaopro-api-development,plantaopro-web-development}\secrets.json`.
- `launchSettings.json` dos 2 projetos limpos de valores de segredo (commitado neste pacote).
- `appsettings.*.example.json`: conferidos por padrão (senha/JWT/rota) — contêm somente placeholders
  (`__SET_VIA_ENVIRONMENT__`, `configure-with-user-secrets`); **nenhuma credencial real**.
- Rotação das credenciais já versionadas no histórico: `docs/seguranca/segredos-locais-e-rotacao.md` (novo).

## 4. Defeitos corrigidos neste bloco

### 4.1 Gap v2199/v2200 — contrato transacional do runner
- A v2200 continha `BEGIN;`/`COMMIT;` standalone, incompatíveis com o contrato do runner
  (cada migration roda em transação própria; o runner aplica `StripStandaloneTransactionControl`).
  Como a migration **nunca havia sido aplicada**, foi removida a transação standalone do arquivo
  (exceção aprovada — nenhum outro arquivo de migration aplicado foi editado).
- Checksum atualizado no manifest: `2026_09_v2200_reconciliar_colunas_medicos_compatibilidade` →
  `926b11c2d6da671365a22e590491196857f2d6cc1a4edc3b8dd59fa5c0261ae6`.
- `install-manifest.json`: seções 63/64 (ordem 63/64) para v2199/v2200; `schemaVersion` → `v2.20.0`.
- Validado: contract tests C# 20/20, validador Python 3/3, `coveragePercent: 100.0`.

### 4.2 Sete checksums defasados no `migration-manifest.json` (defeito pré-existente no commit `e7618fd`)
- Prova no nível do commit: `git show HEAD:<arquivo>` × manifest = mismatch de blob (não era ruído local).
- Fórmula canônica confirmada: **SHA256(UTF-8, CRLF→LF)**. Auditoria das 68 entradas: 61 batem só na
  fórmula canônica, 8 batem nas duas (raw e canônica), 0 batem somente na raw, **7 não batiam em nenhuma**.
- Versões corrigidas (substituição byte-a-byte, preservando BOM/line-endings; correção de **metadados** do
  manifest, sem alterar `.sql` já aplicado):

| Versão | Checksum corrigido |
|---|---|
| `2026_v1186_schema_permissoes_compatibilidade` | `7e0300da69ae292b91574aef00a82190e487a64b55a1b908ca881baa0338e60e` |
| `2026_v1250_jornada_clinica_operacional` | `2695c0500536102d0948e19512264529471dd219bff8fcc4d1310b3a4d14a8e5` |
| `2026_v192_saved_views` | `df55fc3e7c192bc4d6d7d6c8064d38638980e782215fea5a33e5c2ace2bef7b6` |
| `2026_v2159_saude360_agenda_recepcao` | `cec52f65dc1477179d1e90453c2262e7d4a4b9d5fcc8e250fbafd922145b0211` |
| `2026_v2160_triagem_consulta_jornada` | `313d92fe8c3319ad979d9e1af2808ac748e932110204d0b321cddaed62141403` |
| `2026_v2163_portal_cliente_modulos` | `496b631938ba68ac14d8b582644a985eb3d5535337d020f10c313a74b2eeaab7` |
| `2026_v2170_cobertura_substituicoes` | `7c7fd13bb91f61ad13f1c1da01903d7af52208aef5dc01345888bc15f591d30b` |

- Compatibilidade com bancos existentes **provada**: as bases de trabalho têm **zero linhas** nessas 7 versões
  (`postgres`: tabela com 0 linhas; `plantaopro`: 8 linhas legado `2026_fix_*`/`v1.31.0`/`v1.95.1`;
  `plantaopro_test`: 2 linhas na coluna legado `versao`) → nenhum banco emite "Checksum aplicado diverge".
- Revalidação pós-fix: **0/68 mismatches**. Seed `seed_pre2199.sql` (66 linhas ≤ v2198) regenerado do manifest.
- Justificativa: não há gerador do manifest no repositório (CI não regenera), portanto a correção dos
  metadados no arquivo é o meio permanente; evidências em scripts auxiliares
  (`fix_manifest_checksums.py`, `chk_formula.py`, `chk_commit_seed.py` em `%TEMP%\opencode`).

### 4.3 Gerador do script consolidado — idempotência
- `scripts/generate-scrpt-completo.py`: regra de normalização que impõe `CREATE [UNIQUE] INDEX IF NOT EXISTS`
  (antes um índice era emitido sem `IF NOT EXISTS`, quebrando a 2ª execução psql com ON_ERROR_STOP).
- Artefatos regenerados: `database/scrpt_completo.sql` (+ `.sha256` =
  `ce6e63c3537899fbf9419d80b9c78ed9b71a4c91a86b8d7995f349c18f868bf4`),
  `database/pgadmin/instalar_no_banco_atual.sql`, `database/source-checksums.json`,
  artefatos de coberturas/plano em `artifacts/`.
- Teste guardiã em `backend/PlantaoPro.Tests/DatabaseGeneratorIntegrityTests.cs`:
  `ScriptCompleto_NaoContemCriacaoDeIndiceSemIdempotencia` (novo nesta execução) +
  `ScriptCompleto_DeveReferenciarHashAtualDeCadaFonteCanonica` (existente).

### 4.4 Installer oficial
- `scripts/database/install-plantaopro.ps1`: faltava o default de `PLANTAOPRO_BOOTSTRAP_ENVIRONMENT`
  (o guard de ambiente do psql rejeitava execução sem a variável) — default `Development` adicionado.

### 4.5 P2 (já commitado em `e7618fd`, listado para rastreabilidade)
- `[HttpGet]` removido de `ErrorController.HttpStatus`/`Error()`: o 405 mascarava o 400 antiforgery
  do POST `/Account/Login`.

## 5. Cenários A–E — resultados (bases descartáveis `plantaopro_install_test_*`)

Harness: `%TEMP%\opencode\run_install_scenarios.ps1 -Scenario <A|A2|B|C|D|E|COMPARE>`.

| Cenário | Caminho oficial | Estado inicial | Esperado (definido antes) | Real | Status |
|---|---|---|---|---|---|
| A | psql consolidado `instalar_plantaopro.psql` ×3 | base vazia | 3× exit 0, mensagem final de aprovação (idempotência CI) | 3× exit 0 "APROVADO" | **APROVADO** |
| A2 | `Tools.Database install` | base vazia | 66 fontes canônicas, manifest v2.20.0, IDENTITY_SCHEMA_READY | conforme esperado, exit 0 | **APROVADO** |
| B | pgadmin + cirurgia pre-v2199 + `Tools.Database upgrade` | 66 aplicadas ≤v2198, 0 cols v2200 | upgrade aplica **exatamente** v2199+v2200 → 68 sucessos, 22 cols em `medicos` | 68 / 22 | **APROVADO** |
| C | idem a B (cenário coluna legada) | 22 cols removidas | `select m.crm` **falha antes** (SQLSTATE 42703) e **funciona depois** | 42703 confirmado (ver abaixo); ok pós-upgrade | **APROVADO** |
| D | pgadmin puro `instalar_no_banco_atual.sql` (sem upgrade) | base vazia | schema completo com 22 cols v2200 | 22, exit 0 (1ª tentativa) | **APROVADO** |
| E | idem a B + falha injetada (`medicos` → `medicos_falha_injetada`) | rename pré-upgrade | v2199 ok; v2200 falha 42P01, exit≠0, rollback atômico (0 cols parciais), falha registrada; retomada substitui a linha → 68/22 | todos os itens conferidos | **APROVADO** |

**Prova do 42703 do cenário C**: na execução original o harness já exigia `exit≠0` pré-upgrade e ok pós,
mas a mensagem exibida veio vazia (filtro em texto inglês contra saída localizada). Nesta sessão o estado
pré-upgrade exato foi re-produzido numa base scratch (`pgadmin` + cirurgia) e a condição foi capturada
diretamente via `DO … EXCEPTION WHEN undefined_column THEN RAISE NOTICE` →
`CAPTURADO_UNDEFINED_COLUMN_SQLSTATE_42703`. Evidência fechada.

## 6. Paridade de schema entre caminhos — APROVADO

Inventário de schema de aplicação por base: `pg_class` (relname|relkind, schema `plantaopro`) +
`information_schema.columns` + tipos e/d + `pg_extension`; SHA256 sobre o inventário normalizado.

Resultado final:

```
plantaopro_install_test_a   obj=1010 col=3656 types=0 ext=5 sha=2448a0f7…
plantaopro_install_test_a2  obj=1010 col=3656 types=0 ext=5 sha=2448a0f7…
plantaopro_install_test_b   obj=1010 col=3656 types=0 ext=5 sha=2448a0f7…
plantaopro_install_test_c   obj=1010 col=3656 types=0 ext=5 sha=2448a0f7…
plantaopro_install_test_d   obj=1010 col=3656 types=0 ext=5 sha=2448a0f7…
plantaopro_install_test_e   obj=1010 col=3656 types=0 ext=5 sha=2448a0f7…
COMPARE_RESULT=IGUAL sha=2448a0f7c6d410cd4d4de1744d54d03a35d72c10abc129233a5a8580af07d259 (6 bases, 3 caminhos oficiais)
```

Escopo e decisão documentada:
- Excluem-se do inventário 4 tabelas de **bookkeeping** (`schema_migrations`, `install_manifest_runs`,
  `schema_migration_repairs`, `tenant_modulos_reconciliacao`) **e os objetos que delas dependem**
  (sequências/índices comuns via `pg_depend`; índices de constraint PK/UNIQUE via
  `pg_constraint.conindid/conrelid` — estes não têm dependência direta para a tabela).
- **Causa raiz da divergência inicial** (explicada, não é drift de schema de aplicação):
  1. As fontes canônicas/preamble do gerador criam `schema_migrations` na forma **legada**
     (`id text PRIMARY KEY`, 4 colunas, depois ampliada por ALTERs históricos), enquanto o runner
     `Tools.Database` tem DDL **moderna** própria (`bigserial`, `version text UNIQUE`, `success` etc.)
     e normalização ao vivo (bloco repair em `Program.cs` L185-208). Bancos reais em trânsito exibem a
     forma mista — o runner converge em runtime no upgrade.
  2. `install_manifest_runs` (histórico de execução) só é criada pelo comando `install`.
  - Ambas as formas são legítimas por caminho; a comparação cobre o schema que a aplicação consome.

## 7. Suíte de testes

- `dotnet test` (PlantaoPro.Tests): **659/659 aprovados** (baseline 658 + 1 teste novo do §4.3).
- Contract tests de migrations: C# 20/20; validador Python 3/3; coverage do consolidado 100%.
- Smoke HTTP após ciclo dev: idêntico ao baseline (item 2).

## 8. Como reproduzir

```powershell
# ciclo de dev
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\setup-local-config.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\run-dev-stop.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\run-dev-build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\run-dev-start.ps1

# cenários de instalação (harness em %TEMP%\opencode\run_install_scenarios.ps1)
powershell -NoProfile -ExecutionPolicy Bypass -File $env:TEMP\opencode\run_install_scenarios.ps1 -Scenario A
powershell -NoProfile -ExecutionPolicy Bypass -File $env:TEMP\opencode\run_install_scenarios.ps1 -Scenario COMPARE

# suíte
dotnet test backend\PlantaoPro.Tests\PlantaoPro.Tests.csproj
```

Notas de higiene: PS 5.1 (sem `&&`; captura de native via tuple `(exit, stdout, stderr)`); bases de teste
são descartáveis e criadas/dropadas pelo harness; banco de trabalho (`postgres`) nunca recebe migrations.

## 9. Status por entregável (BLOCO A)

| Entregável | Status |
|---|---|
| Ciclo de dev com exit codes coerentes + docs | APROVADO |
| Readiness/liveness por HTTP real | APROVADO |
| Diagnóstico sem segredos | APROVADO |
| Cleanup/stop por identidade (path/cmdline) | APROVADO |
| Segredos fora dos launchSettings (user-secrets) | APROVADO |
| Exemplos sem credenciais reais | APROVADO |
| Documentação da rotação das credenciais versionadas | APROVADO |
| Caminho A (psql consolidado, idempotente ×3) | APROVADO |
| Caminho A2 (Tools.Database install) | APROVADO |
| Caminhos B/C/E (upgrade a partir de base ≤v2198) | APROVADO |
| Caminho D (pgadmin puro) | APROVADO |
| Paridade de schema entre todos os caminhos | APROVADO |
| Checksums do manifest coerentes com os arquivos (0/68) | APROVADO |

Pendências **fora** deste bloco (roadmap, sem alteração aqui): Blocos B–E do Administrativo 360
(sessão/permissões, matriz+jornadas, template/UI+cookie, testes no repositório) e push dos commits
locais (`bb89062`, `d5dda67`, `e7618fd` + este).
