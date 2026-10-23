# R95 (B5) — Drill de backup/restore: dados do banco vivo + artefato do script único (B1)

## Objetivo

Linha 87 do plano (r66a0): "drill de backup/restore usando o script único (B1) como artefato".

A R92 (carga) provou que o sistema aguenta tráfego; a R95 prova **continuidade**: o banco de
teste — sobre o qual correm mais de 90 rodadas — pode ser recuperado de (PA) um **backup de
dados** dele mesmo e (PB) apenas de **artefatos versionados** (script único + migrações
oficiais)? Se uma das rotas falhar, o plano de recuperação do ambiente de prova deixa de ser
afirmativo.

Entregas da rodada:
1. `r95a_drill_backup_restore.ps1` — um único script, duas rotas de recuperação, relatório
   máquina-legível (`RESULTADO=PASSEI/FALHOU`, exit 0/1);
2. Execução canônica → `r95a_drill_saida.txt` + `r95a_linhas_antes.txt`/`r95a_linhas_depois.txt`;
3. Contrato `R95DrillBackupRestoreContratoTests` (4 fatos; suíte 1789 → 1793);
4. Três defeitos latentes descobertos e documentados abaixo (um deles reparado nesta rodada:
   checksums do manifest).

## Recon

- **Artefato B1**: `database/scrpt_completo.sql` (espelho `pgadmin/instalar_no_banco_atual.sql`)
  executado por `database/instalar_plantaopro.psql` / wrapper `scripts/database/install-plantaopro.ps1`;
  termina em auto-verificação com selo `APROVADO`.
- **Banco vivo** (`plantaopro_test`): 729 tabelas, 402.071 linhas, 206 FKs, 2.890 índices,
  8 sequências, extensões btree_gist/pgcrypto/plpgsql/unaccent/uuid-ossp, sem conexões ativas externas.
- **Smoke de round-trip**: dump ~31,9 MB em 4 s; recriação em 3 s; restore em ~20 s — já
  revelando duas FKs que `pg_restore` perderia por ordem, dependendo do estado da fonte.
- **Investigação das FKs**: `adm360_vale_itens_vale_id_fkey` e
  `adm360_cotacao_envios_resposta_id_fkey` existem na fonte como **NOT VALID**, por causa de
  linhas órfãs (557 em `adm360_vale_itens` e 224 em `adm360_cotacao_envios`). Uma FK validada não
  convive com órfãos ⇒ o estado da fonte é fielmente NOT VALID; com esse estado gravado no dump, o
  restore canônico correu com **zero avisos**.
- **Auto-cura oficial**: runner `backend/PlantaoPro.Tools.Database` (`upgrade`/`status`, manifest
  com 144 entradas). A API não aplica migrações no boot (`DatabaseStartupReadinessValidator`
  exige base pronta) — o runner é o único caminho oficial de convergência.
- **Segunda rota oficial de instalação** (achado): `Tools.Database install` →
  `database/install-manifest.json` → seções `database/schema/*.sql`, com conjunto de tabelas
  ligeiramente diferente do do script único (ver Achado 4).

## Decisões

- **PA (dados)**: `pg_dump -Fc` → `DROP ... WITH (FORCE)` → recriar (template0, UTF8, owner
  postgres) → `pg_restore --no-owner --no-privileges` → **paridade completa**: catálogo das 729
  tabelas, contagem de TODAS as linhas (uma única chamada PL/pgSQL), conjunto de índices, 206 FKs
  *com definição* (`pg_get_constraintdef`), sequências, extensões, **digest de conteúdo**
  (SHA-256 sobre concatenação ordenada de `row_text`, independente da ordem física de escrita)
  num conjunto estável de 10 tabelas e o invariante `fn_adm360_bypass_habilitado()='f'` →
  **API funcional contra o banco RESTAURADO** (login do gestor + rota quente `/orcamentos` 200).
  O banco vivo termina **restaurado**: as rodadas seguintes seguem sobre o mesmo estado, e se
  algo falha após o DROP o dump preservado permite restaurar de novo.
- Repair defensivo de FKs: se o restore deixasse alguma chave fora, o drill recria fielmente com
  `ADD CONSTRAINT ... NOT VALID` + tentativa de `VALIDATE CONSTRAINT` (se os dados permitem).
  Não foi necessário na execução canônica (0 faltantes).
- **PB (artefato)**: instalador em modo **CLEAN** num banco descartável (`plantaopro_r95_script`)
  → `APROVADO` → `upgrade` oficial (auto-cura/convergência) → API sobe no scratch + login do
  superadministrador de bootstrap → paridade estrutural contra o original com **delta
  classificado** (Achado 4). O scratch sempre é derrubado ao final (exceto `-KeepScratch`).
- Artefatos operacionais: dump volátil em `%TEMP%\opencode` com SHA-256 registrado no relatório
  (binário não entra no repo); sem segredos em appsettings (conexão/JWT só por variáveis de
  ambiente do drill); porta 51977 dedicada, comprovada livre no fim.

## Achados

1. **FKs NOT VALID por linhas órfãs** (qualidade de dados): 557 + 224 órfãs nos módulos adm360 de
   vales e cotações. O restore fiel exige recriá-las como NOT VALID; `ADD CONSTRAINT` validada
   falha sobre esses dados. Pendência: auditoria/limpeza das órfãs.
2. **Drift de checksum arquivo × manifest** (reparado nesta rodada): as entradas
   `2026_v1186_schema_permissoes_compatibilidade` (arquivo editado na R68 sem o manifest) e
   `2026_10_v2360_r66_gap_instalacao_convergencia` (nascida dessincronizada na própria R66)
   bloqueavam o runner oficial em QUALQUER banco — inclusive o vivo — porque a verificação de
   checksum roda antes de qualquer consulta de estado aplicado. Nenhum contrato anterior cobria
   essas entradas (cada rodada valida apenas a própria). Reparo: checksums dos dois alinhados ao
   hash real dos arquivos (SHA-256, convenção CRLF→LF); varredura das 144 entradas confirmou
   drift=0; a classe ficou travada na suíte (fato 4 do contrato).
3. **Instalador sensível a stderr**: o wrapper `install-plantaopro.ps1` usa
   `$PSNativeCommandUseErrorActionPreference=$true` com `EAP=Stop`, então QUALQUER
   NOTICE/WARNING do psql virava erro fatal ("extensão pgcrypto já existe, ignorando" e
   "sem privilegio para ajustar o papel plantaopro_maintenance; mantendo os atributos atuais"). Solução operacional adotada
   pelo drill: `PGOPTIONS='-c client_min_messages=error'` (erros reais seguem fatais, mantendo
   `ON_ERROR_STOP` + selo APROVADO como portões). Endurecer o wrapper é pendência do lado B1.
4. **Delta estrutural entre rota de instalação + perfil** (classificado): banco criado pelo
   script único + migrações vs banco vivo de teste = 32 tabelas legadas criadas SOMENTE pelo
   script único (DDL ausente das seções `schema/*` e das migrações; algumas ainda referenciadas
   por controllers legados, mas sem caminho exercitado no vivo) + 2 tabelas criadas somente pelos
   seeds de perfil demo (`setores`, `plantao_pro_demo_premium`), pois o vivo roda perfil demo e
   o drill usa `-Profile base`. As 4 linhas de linhagem semeadas pelo instalador em
   `schema_migrations` explicam `aplicadas=148 = 144+4`. O drill falha para qualquer nome FORA
   dessa classificação documentada.

## Evidência (execução canônica)

Linha a linha de `r95a_drill_saida.txt` — resumo dos marcos:

```
ESTAGEM_1_BASELINE_OK tabelas=729 linhas_totais=402071 fks=206 indices=2890 sequencias=8 extensoes=btree_gist,pgcrypto,plpgsql,unaccent,uuid-ossp digest_tabelas=10
ESTAGEM_2_DUMP_OK arquivo=C:\Users\NCELL-~1\AppData\Local\Temp\opencode\r95_backup_plantaopro_test.dump tamanho_bytes=31879780 sha256=06fe1bf785ce02ab7af6ccc5b3414c01d881599a608c0c21f76b930c6ad1766a tempo_seg=5
ESTAGEM_3_DROP_RECREATE_OK tempo_seg=6
ESTAGEM_4_RESTORE_OK tempo_seg=18 avisos_pg_restore=0 fks_recriadas=0 fks_nao_validas=0 fks_faltando_fim=0
DIF_TABELAS faltando=0 extra=0 amostra=[]
DIF_INDICES faltando=0 extra=0 amostra=[]
DIF_SEQUENCIAS faltando=0 extra=0 amostra=[]
DIF_CONTAGENS faltando=0 extra=0 amostra=[]
ESTAGEM_5_PARIDADE extensoes_iguais=SIM digest_ok=10/10 fn_bypass=f
PARIDADE_COMPLETA_OK
ESTAGEM_6_FUNCIONAL_API_OK porta=51977 login_gestor=SIM orcamentos_http=200 orcamentos_total=10332
PA_CONCLUIDO
ESTAGEM_7_SCRIPT_UNICO_APROVADO banco=plantaopro_r95_script modo=CLEAN tempo_seg=15
ESTAGEM_8_MIGRACOES_UPGRADE_OK Migrations: aplicadas=148 esperadas=144 falhas=0 pendentes=0
ESTAGEM_9_AUTO_CURA_E_LOGIN_OK login_superadmin=SIM
DIF_SCRIPT_UNICO_VS_ORIGINAL faltando=2 extra=32 amostra=[...]
CLASSIFICACAO_DELTA_ESTRUTURAL_OK extra_classificado=32 faltante_classificado=2 inesperados=0
PB_CONCLUIDO
PORTAS_LIVRES=SIM
RESULTADO=PASSEI
```

Artefato volátil do dump: `%TEMP%\opencode\r95_backup_plantaopro_test.dump`
(31.879.780 bytes, SHA-256 `06fe1bf785ce02ab7af6ccc5b3414c01d881599a608c0c21f76b930c6ad1766a`)
— não commitado de propósito. O `orcamentos_total=10332` da etapa 6 é a visão paginada do
tenant do gestor (login demo) contra o banco restaurado.

## Contrato

`R95DrillBackupRestoreContratoTests` — 4 fatos:
1. O script trava as duas rotas e os marcadores máquina-legíveis da saída;
2. A execução canônica passou: dump válido (sha256 `^[0-9a-f]{64}$`, ≥ 10 MB),
   `fks_faltando_fim=0`, paridade PA completa (DIF_* faltando/extra=0, digest n/n com n≥8,
   extensões iguais, `fn_bypass=f`), funcionalidade nas duas rotas (gestor no banco restaurado,
   http 200; superadmin no banco do script único), convergência `falhas=0 pendentes=0`, delta
   estrutural classificado (faltando=2/extra=32 com `inesperados=0`), portas livres,
   `RESULTADO=PASSEI` e nenhuma linha `FALHA `;
3. Os arquivos de contagem por tabela são idênticos antes/depois do restore, cobrem ≥ 700
   tabelas, total ≥ 300 mil linhas, spots estáveis (`adm360_orcamentos` ≥ 11.000, `usuarios` ≥ 5);
4. Os checksums de TODAS as entradas ativas do `migration-manifest.json` casam com os arquivos
   em disco (mesma lógica do runner: CRLF→LF com fallback para bytes crus) — trava a classe de
   defeito do Achado 2.

Suíte: 1789 → 1793.

## Gates

- Build A/B Release `--no-incremental` (tag r95): `base-exit=0 nova-exit=0`;
  avisos únicos **76 por lado**, `DIFF=NENHUMA`.
- Suíte completa pré-doc (`--no-build -c Release`): **1793/1793 aprovados** (22 s) —
  inclui higiene `RepositoryHygieneContractTests` 8/8.
- Docscan (U+00C3 nos arquivos da rodada): 0 defeitos.
- Suíte completa pós-doc (`--no-build -c Release`): **1793/1793 aprovados**.

## Pendências

- Auditoria/limpeza das linhas órfãs (`adm360_vale_itens`: 557; `adm360_cotacao_envios`: 224)
  para as duas FKs poderem ser validadas quando os módulos forem tocados.
- Endurecer `scripts/database/install-plantaopro.ps1` contra stderr benigno (B1); hoje o drill
  contorna com `PGOPTIONS`.
- Conciliar o script único com a rota `install-manifest.json` (32 tabelas legadas + seeds demo):
  decidir o estado canônico do conjunto de tabelas — rodada futura.
- Política de retenção de backups (dump volátil fora do repo nesta rodada).
