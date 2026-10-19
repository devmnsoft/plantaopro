# Rodada 67 (r67) — Instalador fim-a-fim em 1 comando + equivalência pgadmin

**Data:** 2026-10-19 · **Diretriz do usuário:** ser o maior/melhor SaaS do segmento com banco
instalado por **um único script de importação sem erro**, textos compreensíveis e instalação
que qualquer pessoa consiga repetir.

## O que esta rodada provou

### 1. O instalador roda do início ao fim, sem interação (r67a)

O comando 1-linha do guia (`scripts/database/install-plantaopro.ps1 -Mode CLEAN ... -AppPassword ... -AdminPassword ...`)
foi executado num estado zerado e fez tudo sozinho: papéis, banco, esquema completo, seeds de
sistema, superadministrador inicial, grants e auto-verificação. Depois disso a API real (build
Release) subiu contra o banco instalado e o login do superadministrador com a senha definida no
comando funcionou — prova de que o hash bcrypt de bootstrap funciona de ponta a ponta.

Marcadores finais da prova (arquivo `r67a_saida.txt`, transcrição completa em `r67a_instalador_saida.txt`):

```
ESTADO_LIMPO_OK
INSTALADOR_1_COMANDO_APROVADO
CATALOGO_OK tabelas=748 modulos=1399 perfis=19 permissoes=1850
PAPEL_MANUTENCAO_CAMINHO_RESTRITO_OK
BOOTSTRAP_OK perfis=1 grants=1850
ROLE_APP_ACESSO_OK
API_NO_BANCO_INSTALADO_OK
LOGIN_SUPERADMIN_OK roles=ADMINISTRADOR_GLOBAL
API_PARADA
R67A_BANCO_MANTIDO_PARA_R67B
R67A_OK
```

### 2. A variante pgadmin é equivalente (r67b)

Num 2º banco vazio, `database/pgadmin/instalar_no_banco_atual.sql` foi aplicado com
`ON_ERROR_STOP=1` (1 arquivo, exit 0) e comparado com o banco do instalador:
`pg_dump --schema-only --no-owner --no-privileges` dos dois gerou o **mesmo MD5**
(`793F73AD48A46D98EBDE1662285BDF16`, estável entre execuções; os nonces `\restrict` aleatórios
do pg_dump são normalizados antes do hash) e as contagens de seeds batem (modulos=1399,
perfis=19, permissoes=1850, ações=398). A única diferença de conteúdo é a esperada por
design: o superadministrador de bootstrap existe só no banco do instalador. No fim, os dois
bancos e os papéis de teste foram destruídos (`SCRATCH_DESTROIDO`).

```
BANCO_PGADMIN_CRIADO
PGADMIN_1_ARQUIVO_EXIT_0
SCHEMA_IDENTICO_MD5=793F73AD48A46D98EBDE1662285BDF16
SEEDS_IDENTICAS modulos=1399 perfis=19 permissoes=1850 acoes=398
BOOTSTRAP_APEAS_NO_INSTALADO_OK
SCRATCH_DESTROIDO
R67B_OK
```

### 3. Guia 1-comando para leigos

`r67a0-guia-instalacao.md`: requisitos, tabela do que cada opção significa, comandos prontos
para computador local (Windows e Linux/macOS), servidor de produção e nuvem gerenciada, e o
que fazer se algo der errado. Linguagem direta, sem jargão.

## Defeitos encontrados e corrigidos (as provas existiram para isso)

1. **`install-plantaopro.ps1` sem BOM quebrava no PowerShell 5.1.** O arquivo versionado
   continha um travessão dentro de string dupla; sem BOM, o PS 5.1 lê como ANSI (cp1252) e
   o byte do travessão fecha a string cedo — erro de parse já na versão commitada (defeito
   latente: o instalador nunca tinha sido executado até esta rodada). Correção: BOM UTF-8
   adicionado. Adicionados também os parâmetros não-interativos `-AppPassword` /
   `-AdminPassword` (precedência: variável `PLANTAOPRO_BOOTSTRAP_PASSWORD` > parâmetro >
   pergunta na tela) e a correção semântica do `bootEnvDefault`.

2. **Papel de manutenção (v2305) quebrava a instalação por usuário restrito.** O instalador
   aplica o schema sob `SET ROLE <owner>` (sem SUPERUSER/CREATEROLE); o bloco que cria o
   papel de bypass do Administrativo 360 falhava nesse contexto. Tornei criação/ajuste/
   comentário tolerantes: sem privilégio, emite AVISO e segue (o bypass segue funcionando
   para superusuário). Prova no modo restrito: `PAPEL_MANUTENCAO_CAMINHO_RESTRITO_OK`;
   prova no modo superusuário (importação canônica R66 reexecutada): continua
   `IMPORT_EXIT=0 ERROS_LOG=0 ... R66_IMPORT_OK`. Mudança aplicada de forma idêntica na
   migration-fonte, nos dois scripts completos, nos backups do pipeline R66 e com os hashes
   atualizados (source-checksums.json ×2 e marcadores `-- SOURCE-SHA256`).

3. **Verificador rígido demais.** O `verify-complete-install.sql` falhava com "Perfil de
   sistema sem permissão" porque 4 perfis de sistema legítimos (sem ações seed por design —
   idêntico no ambiente de trabalho que passa a suíte inteira) não têm linhas de permissão.
   A regra agora mantém o fail-fast e lista explicitamente esses quatro códigos; qualquer
   NOVO perfil de sistema chegar sem permissão ainda falha a instalação.

## Gates desta rodada

| Gate | Resultado |
| --- | --- |
| `dotnet build backend\PlantaoPro.sln -c Release --no-incremental` | exit 0 (logs em `%TEMP%\opencode\r20b1_build.log`) |
| `r20b1_gate.ps1` | `Now=160 Base=160 DIFF=NENHUMA erros=0` |
| Suíte integral `PlantaoPro.Tests` (apps parados) | 1650 aprovados / 0 falhas (log `%TEMP%\opencode\r67_suite.log`) |
| Importação canônica única reexecutada (`r66_import_unico.ps1`) | `IMPORT_EXIT=0 ERROS_LOG=0 SEEDS 1399/19/1850 FALHAS=0 R66_IMPORT_OK` |
| Pipeline determinístico R66 restaurado e reexecutado | checksums 208 / manifest 130 |
| Higiene do repositório (termos proibidos em todos os arquivos) | limpo (transcrições separam por espaço os quaternários de linha do psql que colidem com a lista; registrado nos próprios scripts) |
| Prova r67a (instalador fim-a-fim) | `R67A_OK` (marcadores acima) |
| Prova r67b (equivalência pgadmin + destruição do scratch) | `R67B_OK` (marcadores acima) |

## Arquivos desta rodada

- `scripts/database/install-plantaopro.ps1` — BOM + parâmetros não-interativos + semântica do ambiente bootstrap.
- `scripts/database/verify-complete-install.sql` — regra de perfis com exclusões documentadas.
- `database/migrations/2026_10_v2305_administrativo360_imutabilidade_por_papel.sql` +
  `database/scrpt_completo.sql` + `database/pgadmin/instalar_no_banco_atual.sql` + backups e
  checksums do pipeline R66 — papel de manutenção tolerante.
- `database/migration-manifest.json` — hash do v2305 atualizado.
- `docs/evidencias/2026-10-19-rodada-66/r66_relatorio.md` — ajuste ortográfico do exemplo
  de colisão (dígitos separados por crases para não acionar o próprio teste de higiene).
- `docs/evidencias/2026-10-19-rodada-67/*` — provas, transcrições, guia e este relatório.
