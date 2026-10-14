# r6 — Cadastro regulatório ANVISA em produtos (§6)

**Data:** 2026-10-14 · **Base:** commit local r9b (`8b83e10`) · **Banco:** `plantaopro` local + `plantaopro_test` (PostgreSQL 18) · **Tenant de prova:** Sistema Vertical (`…ca301`)

## 1. Escopo do corte

O cadastro de produto ganhou a seção **Regulação ANVISA**, com as regras exigidas pela rodada:

- **Número como TEXTO** (`varchar(60)`) — preserva zeros à esquerda; nunca é convertido para numérico.
- **Aplicabilidade tri-estável**: `INFORMADO` / `NAO_APLICAVEL` / `PENDENTE` (default `PENDENTE` em migração).
- **Flag de entrada manual** (`anvisa_entrada_manual`, default `true`): **não há busca simulada** — toda entrada atual é manual; o campo existe para distinguir fontes automatizadas futuras.
- **Vigência regulatória** (`anvisa_vigencia date`) é **independente da validade do lote** (`adm360_lotes.validade`) — conceitos distintos, armazenados em colunas/tabelas distintas, e a UI diz isso explicitamente.
- **Justificação obrigatória** quando o produto é marcado como "não aplicável" (`anvisa_justificacao text`).

## 2. Migração (padrão incremental do projeto)

`database/migrations/2026_10_v2352_r9c_cadastro_anvisa_produtos.sql`:

```sql
alter table plantaopro.adm360_produtos
    add column if not exists anvisa_numero varchar(60),
    add column if not exists anvisa_aplicabilidade varchar(15) not null default 'PENDENTE',
    add column if not exists anvisa_entrada_manual boolean not null default true,
    add column if not exists anvisa_vigencia date,
    add column if not exists anvisa_justificacao text;

alter table plantaopro.adm360_produtos
    drop constraint if exists ck_adm360_produtos_anvisa_aplicabilidade,
    add constraint ck_adm360_produtos_anvisa_aplicabilidade
        check (anvisa_aplicabilidade in ('INFORMADO', 'NAO_APLICAVEL', 'PENDENTE'));
```

Execução real: **aplicada 2× no banco operacional** (rerun idempotente confirmado) e **1× no banco de testes** `plantaopro_test` (regra P0: testes nunca escrevem no banco da aplicação). Probes no banco: 5 colunas presentes, default `PENDENTE`, `anvisa_numero` não numérica, check constraint dispara `check_violation` para valor inválido.

### Manifesto

`database/migration-manifest.json`: **backfill v2346→v2351** (drift existente auditado na r9-auditoria-inicial §manifesto) + nova entrada **v2352**, cada uma com checksum SHA-256 do arquivo e cadeia `dependsOn` encadeada; `generatedAt` atualizado para 2026-10-14. Observação: os consolidados `scrpt_completo.sql` / `instalar_no_banco_atual.sql` **não** contêm nem as migrações v2346–v2351 das rodadas anteriores — o padrão real do projeto é migration + manifest, então os consolidados seguem como dívida conhecida (pendência).

## 3. Alterações por arquivo

| Arquivo | Alteração |
|---|---|
| `database/migrations/2026_10_v2352_r9c_cadastro_anvisa_produtos.sql` | Nova migração idempotente (colunas + check) |
| `database/migration-manifest.json` | Backfill v2346–v2351 + v2352 (SHA-256, dependsOn, installRequired=false) |
| `backend/PlantaoPro.Application/Administrativo360/Contracts.cs` | `Produto360` +5 parâmetros finais opcionais; `SalvarProdutoAsync` +5 parâmetros finais opcionais (após `ct` — compatibilidade de fonte preservada; verificado todos os call sites, inclusive testes) |
| `backend/PlantaoPro.Infrastructure/Administrativo360/CadastrosRepository.cs` | Validações ANVISA (aplicabilidade válida; INFORMADO exige número; NAO_APLICAVEL exige justificação; texto trimado sem conversão numérica); INSERT e UPDATE incluem as 5 colunas; `ListarProdutosAsync` e `ObterLookupsAsync` selecionam as 5 colunas (Dapper: parâmetro opcional exigiria coluna em TODO SELECT que materializa o registro — ambos atualizados) |
| `backend/PlantaoPro.Api/Controllers/Adm360CadastrosController.cs` | `SalvarProdutoRequest` +5 campos opcionais encaminhados ao repositório |
| `backend/PlantaoPro.Web/Models/Administrativo360Models.cs` | `Produto360ViewModel` espelha os 5 campos |
| `backend/PlantaoPro.Web/Controllers/Administrativo360Controller.Cadastros.cs` | Ação `SalvarProduto` aceita e repassa os 5 campos (validação no servidor/API) |
| `backend/PlantaoPro.Web/Views/Administrativo360/Produtos.cshtml` | Coluna **ANVISA** na tabela (badge verde com número / "n/a" cinza com justificação no tooltip / "pendente" âmbar); seção "Regulação ANVISA" no modal (número texto, aplicabilidade select, vigência date, justificação, hidden `AnvisaEntradaManual=true` + texto informativo distinguindo vigência regulatória × validade do lote); JS `abrirModalNovoProduto` reseta os campos; `editarProduto` carrega os 4 valores |

Sem novas chaves i18n (labels estáticos pt-BR conforme convenção do módulo; erros vêm da API; toasts reutilizam chaves existentes) → contrato de 246 chaves inalterado.

## 4. Execução real (prova de ponta a ponta)

```
ANVISA_INFORMADO STATUS=200                            (P1: numero '0001234567890123', vigencia 2028-06-30)
ZERO_A_ESQUERDA_API=True ZERO_A_ESQUERDA_DB=True VAL_DB=[0001234567890123]   ← zeros preservados em JSON e banco
DB_ENTRADA_MANUAL=t DB_VIGENCIA=2028-06-30
NEG_INFORMADO_SEM_NUMERO STATUS=400 "Informe o número de registro ANVISA (ou marque o status como pendente/não aplicável)."
NEG_NAOAPLIC_SEM_JUSTIFICACAO STATUS=400 "Informe a justificação para marcar o produto como não aplicável à regulação ANVISA."
NEG_APLICABILIDADE_INVALIDA STATUS=400 "Aplicabilidade ANVISA inválida (use informado, não aplicável ou pendente)."
NAO_APLICAVEL_COM_JUST STATUS=200                      (P2)
NAPLIC_APLIC=NAO_APLICAVEL NAPLIC_NUMERO=(null)
WEB_STATUS=200 WEB_HAS_HEADER_ANVISA=True WEB_BADGE_NUMERO=True WEB_HAS_ANVISA_FORM=True WEB_FORM_ONCLICK_NUMERO=True
WEB_POST_PRODUCT_STATUS=302                            (formulário real c/ antiforgery da página)
WEB2_TOAST=True WEB2_VIGENCIA_NO_ONCLICK=True WEB2_BADGE_NUMERO=True
FINAL_VIA_API VIGENCIA=2029-01-31 NUMERO_OK=True APLIC=INFORMADO MANUAL=True  ← data do formulário fez round-trip
```

Artefato HTML: `r6-sv-produtos-anvisa.html` (tela de produtos com coluna ANVISA preenchida + toast de sucesso após o POST do formulário).

## 5. Testes automatizados

- Suite completa: **1394 aprovados / 0 falhas**.
- Nota de incidente: a primeira execução pós-migração falhou com 12 erros `column "anvisa_numero" does not exist` — causa: os testes usam o banco separado `plantaopro_test` (regra P0 do `TestDatabase`); a migração foi aplicada ali e a suite voltou a 1394 verdes. Confirma que novo banco/clean install precisa executar a fila de migrations.

## 6. Preparação para os próximos cortes

- Os 5 campos já fluem nos DTOs de produto (API + Web + lookups) — a **cotação** (r9e/§8) poderá snapshotear o número/aplicabilidade por item sem nova migração de produto.
- Pendência registrada: consolidated scripts (`scrpt_completo.sql`, `pgadmin/instalar_no_banco_atual.sql`) sem as migrações v2346+ (dívida pré-existente).
