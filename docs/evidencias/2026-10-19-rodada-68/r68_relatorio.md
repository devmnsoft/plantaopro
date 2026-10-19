# R68 — Perfis de importação + reimportação idempotente + contrato travando o padrão

Data: 2026-10-19 · Plano de 50 sprints (R66–R115), bloco B1 · Banco em 1 comando.

## Objetivo da rodada

Deixar o importador do banco explícito em **dois perfis** e provar que **aplicar o mesmo
comando duas vezes não gera nenhum erro** (reinstalação/UPGRADE sem medo):

- **`base`** (padrão) — esquema completo + dados de sistema (módulos, perfis, permissões)
  + superadministrador de bootstrap. Sem nenhum dado de demonstração. É o perfil de produção.
- **`demo`** (opcional, só `Development`) — tudo do `base` + os 4 seeds de demonstração
  (`database/seeds/development/121|130|140|141*.sql`), que criam as contas e os dados de
  exemplo documentados em `database/seeds/development/README.md`.

## O que foi encontrado

A prova empírica de dupla aplicação encontrou **3 defeitos reais**, em camadas (cada um
só aparecia depois de curado o anterior):

### Defeito 1 — restrição não idempotente `ck_pagamentos_status`

Aplicar o script completo 2× num banco novo dava exatamente 1 erro:

```
psql:...scrpt_completo.sql:28609: ERRO:  restrição "ck_pagamentos_status" para relação "pagamentos" já existe
```

(1ª aplicação: 0 erros · 2ª aplicação: 1 erro.) Origem: bloco anexo
`backend/sql/20260521_premium_permissoes_notificacoes.sql` (seção R66).

### Defeito 2 — inchaço fractal do catálogo de permissões

Curado o defeito 1, a dupla aplicação passou a terminar com 0 erros — mas o banco voltava
**inchaço** a cada aplicação. Em banco novo, aplicando o script da época 2 vezes completas:

| estado | módulos | ações | permissões | perfil_permissoes |
|---|---|---|---|---|
| instalação limpa (1 passada) | 1.399 | 398 | 1.850 | 314 |
| 2ª aplicação completa | 3.099 | 25.518 | 556.860 | 826.690 |

**Em linguagem simples:** o script tem uma etapa que "organiza" cada permissão identificando
seu módulo e sua ação a partir do código (ex.: código `TENANTS.SUSPENDER` → módulo
`TENANTS`, ação `SUSPENDER`). Essa etapa lia o código como se o separador fosse o traço
baixo, e não o ponto. Por isso `TENANTS.SUSPENDER` virava um "módulo" falso chamado
`TENANTS_SUSPENDER`. Depois, outra etapa cria automaticamente a combinação de **todos os
módulos × todas as ações** (uma grade) — e os módulos falsos entravam nela, gerando novas
permissões que geravam novos módulos falsos na passagem seguinte. Cada reinstalação
multiplicava o tamanho: era um efeito fractal (crescimento em cascata).

**Correção em duas iterações** (cada uma validada por sonda de múltiplas passadas):

1. `r68c_fix_normalizacao_permissoes.ps1` — normalizar `.` para `_` antes de parsear.
   Cortou o lado dos módulos, mas a sonda ainda mostrou crescimento pelo lado das ações:
   397 → 18.307 → 842.167 ações em 3 passadas (~46× por passada).
2. Diagnóstico: os componentes canônicos **podem conter traço baixo** (`OPERACAO_ASSISTIDA`,
   `FEATURE_FLAGS`, `CUSTOMER_SUCCESS`...). O código da grade `OPERACAO_ASSISTIDA.VISUALIZAR`
   era lido como módulo=`OPERACAO` / ação=`ASSISTIDA_VISUALIZAR`: com ~49 caudas assim, cada
   passada criava 1 ação falsa para cada ação existente → fator ~46× (o 46 é exatamente
   1 + 45 caudas ativas).
3. `r68e_fix_parse_ponto_canonico.ps1` (correção final) — separar **somente pelo 1º ponto**:
   módulo = tudo antes do ponto, ação = tudo depois; o traço baixo só vale como fallback
   para códigos legados sem ponto (comportamento original mantido).

Superfície do patch (idêntica nas duas iterações): fontes `database/schema/000_schema_canonico_base.sql`,
`database/schema/010_identity_access.sql`, `database/migrations/2026_v1186_schema_permissoes_compatibilidade.sql`,
os 2 scripts completos (3 ocorrências cada) e os 2 backups do pipeline R66 (2 ocorrências cada),
+ hashes nos 2 `source-checksums.json` + marcadores `-- SOURCE-SHA256` das seções 000/010.

Resultado após a correção final (sonda, 5 passadas completas em banco novo, 0 erros em todas):

| passada | módulos | ações | permissões | perfil_permissoes |
|---|---|---|---|---|
| 1 (instalação limpa) | 93 | 46 | 1.850 | 314 |
| 2 (1º upgrade) | 93 | 46 | 4.336 | 3.693 |
| 3 | 93 | 46 | 4.336 | 6.907 |
| 4 e 5 | 93 | 46 | 4.336 | 6.907 ← **ponto fixo** |

As transições da 1→2 (a grade módulo×ação fecha na 1ª passagem em que todo o vocabulário
já existe) e da 2→3 (os grants de perfis×permissões rodam antes das seções de grade na
ordem do arquivo, então completam na passagem seguinte) são **esperadas, únicas e
documentadas**; a partir da 3ª aplicação nada muda. Descoberta colateral: o "número
canônico" de 1.399 módulos já era ele mesmo artefato do bug (na 1ª instalação da história);
o catálogo real de fábrica tem **93 módulos / 46 ações**.

### Defeito 3 — propriedade dos objetos na 2ª aplicação do perfil demo

Curados 1 e 2, a 4ª aplicação da prova falhou:

```
psql:...database/instalar_plantaopro.psql:98: ERRO:  permissão negada para tabela setores
```

Causa: o seed demo `130_operacao_demo_santacasa.sql` cria objetos compatibilidade (tabela
`setores` e ~167 funções/views) com a role que aplicou o seed — quem aplica via instalador
é a role de conexão (superusuária), não a role dona do banco. Na aplicação seguinte, o passo
`GRANT ... ON ALL TABLES` roda como a role dona e não tem direito sobre objetos de outro dono.

Correção: `database/bootstrap/normalize_owner_grants.sql` (novo, idempotente) devolve a
propriedade de tabelas/views/sequências/funções do schema para a role dona e reemite os
privilégios da role da aplicação. Invocado em 2 pontos: dentro do `instalar_plantaopro.psql`
(anterior ao bloco de GRANT, cuidando de resíduo de execuções anteriores) e ao final dos dois
instaladores (`install-plantaopro.ps1`/`.sh`, cuidando dos objetos criados pelos seeds da própria
execução). Com isso, reinstalação de qualquer perfil converge sempre ao mesmo estado.

## Mudanças

1. **`backend/sql/20260521_premium_permissoes_notificacoes.sql`** — o
   `ALTER TABLE ... ADD CONSTRAINT ck_pagamentos_status` agora roda dentro de
   `DO $$ ... IF NOT EXISTS (pg_constraint) ... END $$;` (idempotente).
   Pipeline R66 regenerado na sequência (checksums 208 / manifest 130); espelhos
   `database/scrpt_completo.sql` e `database/pgadmin/instalar_no_banco_atual.sql`
   atualizados com o mesmo conteúdo e marcador `-- SOURCE-SHA256` novo.
2. **`scripts/database/install-plantaopro.ps1`** — novo parâmetro
   `[ValidateSet('base','demo')][string]$Profile='base'`. Com `-Profile demo` exige
   `-Environment Development` (falha rápida com mensagem clara) e aplica os 4 seeds demo
   em ordem determinística com `ON_ERROR_STOP=1` logo após a instalação.
3. **`scripts/database/install-plantaopro.sh`** — equivalente `PLANTAOPRO_PROFILE`
   (mesma validação, mesma ordem de seeds). Sintaxe validada com `bash -n`.
4. **Contrato versionado** `backend/PlantaoPro.Tests/DatabaseIdempotenciaContratoTests.cs`
   (roda em toda suíte), cobrindo `scrpt_completo.sql` + espelho pgadmin + os 4 seeds demo:
   - `CREATE TABLE/INDEX` top-level só com `IF NOT EXISTS`;
   - `ADD CONSTRAINT` top-level só depois de `DROP CONSTRAINT IF EXISTS`;
   - `INSERT INTO` top-level só com `ON CONFLICT` ou guarda `NOT EXISTS`;
   - `CREATE TRIGGER` só depois de `DROP TRIGGER IF EXISTS` do mesmo nome;
   - `CREATE FUNCTION` top-level só como `CREATE OR REPLACE FUNCTION`;
   - espelho pgadmin com a MESMA lista de marcadores `SOURCE/SOURCE-SHA256` do script
     completo (mesmo caminho e mesmo hash, na mesma ordem);
   - instaladores expõem o perfil (`ValidateSet('base','demo')` / `PLANTAOPRO_PROFILE`)
     e os 4 seeds existem e estão referenciados pelos dois instaladores.
   Regras aplicadas por cima de um rastreador de blocos `DO $tag$...END $tag$;` (tags
   arbitrárias): DDL dentro de bloco `DO` é lógica plpgsql guardada, coberta pela prova
   empírica de dupla aplicação abaixo.
5. **Guia 1-comando** (`docs/evidencias/2026-10-19-rodada-67/r67a0-guia-instalacao.md`)
   — nova linha na tabela de opções e seção "Perfis de instalação" explicando `base` e
   `demo` em linguagem de leigo, com os dois exemplos de comando.
6. **Prova R66** (`r66_import_unico.ps1`) — lista de falsos positivos documentada ganhou
   `ps1` e `sh` (extensões de arquivo citadas em literais de string pelo novo contrato;
   mesma classe de ruído já listada: `com`, `css`, `sql`).
7. **Parse do código canônico `MODULO.ACAO` pelo 1º ponto** — nos 3 arquivos-fonte acima,
   nos 2 scripts completos (3× cada) e nos 2 backups do pipeline (2× cada), com comentário
   `-- R68:` explicando a regra (patches `r68c_fix_normalizacao_permissoes.ps1` e
   `r68e_fix_parse_ponto_canonico.ps1`, ambos idempotentes e versionados nesta pasta).
8. **`database/bootstrap/normalize_owner_grants.sql`** (novo) + ganchos no
   `database/instalar_plantaopro.psql` e nos dois instaladores (defeito 3).
9. **Protocolo da prova** `r68a_perfis_reimportacao.ps1` — a fingerprint de sistema ganhou
   `perfil_permissoes` (antes invisível); as transições de assentamento RUN2/RUN3 são
   registradas como esperadas e o invariante real é **ponto fixo no RUN4**
   (fingerprint do RUN4 idêntica à do RUN3). As asserções de identidade demo passaram a usar
   o slug canônico do tenant `santa-casa-demonstracao` (consolidado pelo upsert do seed 130).

## Provas

### Dupla aplicação direta (diagnóstico → cura)

- Antes do reparo (defeito 1): PASS1_ERROS=0, PASS2_ERROS=1 (`ck_pagamentos_status`).
- Depois do reparo (pipeline regenerado): **PASS1_ERROS=0, PASS2_ERROS=0**.
- Sonda de ponto fixo (script final, 5 passadas): **ERROS=0 em todas** e contagens estáveis
  a partir da passada 3 (tabela na seção do defeito 2). Scripts de apoio:
  `r68_probe_fixpoint.ps1` / `r68_probe_passes45.ps1` (no TEMP; resultados copiados acima).

### `r68a_perfis_reimportacao.ps1` (instalador completo, 4 aplicações)

Saída integral da execução final (banco/roles novos a cada rodada da prova):

```
ESTADO_LIMPO_OK
CATALOGO_RUN1 tabelas=748 fingerprint=47f0f67001d649af
PAPEL_MANUTENCAO_CONSISTENTE n=1
BOOTSTRAP_RUN1_OK
ASSENTAMENTO_RUN2_ESPERADO sistema_fp1=47f0f67001d649af -> fp2=2bbbf6a729fb1d24
SUPERADMIN_UNICO_APOS_REIMPORTACAO
ASSENTAMENTO_RUN3_ESPERADO sistema_fp2=2bbbf6a729fb1d24 -> fp3=c5a2ddbd2a4ce50e
PERFIL_DEMO_OK sistema_inalterado identidades_demo=1_1_1
PONTO_FIXO_APOS_4_APLICACOES sistema_estavel
SEEDS_DEMO_IDEMPOTENTES_OK contagens_estaveis
API_NO_BANCO_INSTALADO_OK
LOGIN_SUPERADMIN_BASE_OK roles=ADMINISTRADOR_GLOBAL
LOGIN_GESTOR_DEMO_OK roles=ADMINISTRADOR_CLIENTE
API_PARADA
SCRATCH_DESTROIDO
R68A_OK
```

- RUN1 (CLEAN, `base`): APROVADO, zero erro, superadmin único de bootstrap.
- RUN2 (UPGRADE, `base`): APROVADO, zero erro; assentamento esperado da grade (permissões
  fecham a grade módulo×ação).
- RUN3 (UPGRADE, `demo`): APROVADO, zero erro; identidades demo presentes (gestor, médico,
  tenant `santa-casa-demonstracao`); assentamento esperado dos grants de perfil×permissão.
- RUN4 (UPGRADE, `demo` de novo — **toda** a instalação pela 2ª vez): APROVADO, zero erro,
  fingerprint idêntica à do RUN3 (**ponto fixo**) e contagens demo estáveis.
- Funcional: API Release contra o banco instalado; login do superadmin (perfil base) e do
  gestor demo (perfil demo) OK.

### Reexecução da prova canônica R66 (script completo mudado nesta rodada)

Executada após a última regeneração do pipeline (19/10/2026 19:50) — verde no script final:

```
IMPORT_EXIT=0 ERROS_LOG=0 LINHAS=19064
GAPS_OBJETO=0 (codigo referencia 359 nomes)
SEEDS_SISTEMA modulos_sistema=93 perfis=19 permissoes=1850
SECAO_R62_DRAEL_OK
PARIDADE_FUNCIONAL scratch=223 teste=222 dif_corpo_comum=222 so_no_teste=0
FALHAS=0            -> R66_IMPORT_OK
```

(EA lista `DIF_FUNCOES` é a diferença conhecida e documentada da seção R62 Drael,
invariante entre execuções. Arquivos: `r66_import_unico_saida.txt` / `r66_import_raw.txt`.)

### Bancos de desenvolvimento já existentes (congelados)

Os bancos locais que viveram a era do bloat trazem vocabulário sintético antigo e
**congelam** com o parser novo (nenhuma re-aplicação adiciona mais; nenhum remove):

| banco | módulos | ações | permissões | perfil_permissoes |
|---|---|---|---|---|
| plantaouro | 1.142 | 232 | 1.761 | 4.920 |
| plantaouro_test | 1.142 | 232 | 1.761 | 3.159 |

Limpeza one-off (remover módulos/ações compostos sem uso) fica registrada para R69+.

## Gates de casa

| gate | resultado |
|---|---|
| `dotnet build PlantaoPro.sln -c Release --no-incremental` | **exit 0 · 0 erro · 80 avisos conhecidos** |
| `r20b1_gate.ps1` (baseline de avisos) | **Now=160 Base=160 · erros=0**; diff bruto 244 é artefato puro de codificação (o log-baseline legado `r17_pristine_build.log` tem acentos com mojibake, o log atual é UTF-8 limpo); comparando o mesmo multiset de chaves `arquivo|aviso` com os não-ASCII normalizados: **DIFF=NENHUMA** — nenhum aviso novo ou removido |
| `r62_docscan.ps1` (caça a `Ã` U+00C3) | ARQ_VERIFICADOS=25 · BAD=4, todos do falso positivo legítimo já documentado: comentários `NÃO`/`PADRÃO` em `database/scrpt_completo.sql`, `database/pgadmin/instalar_no_banco_atual.sql` e nos 2 backups `_backup_*` da roda 66. Nenhum arquivo novo/modificado desta rodada com texto foi flagrado |
| Suíte integral (apps parados, portas 51976/51977 varridas) | **Aprovado: 1653 · Falha: 0 · Total: 1653** (inclui o contrato `DatabaseIdempotenciaContratoTests` 3/3) |
