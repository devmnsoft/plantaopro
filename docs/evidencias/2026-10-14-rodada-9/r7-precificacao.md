# r7 — Precificação: preço de venda + margem/markup recalculados no servidor (§7)

**Data:** 2026-10-14 · **Base:** commit local r9c (`d0fcbdc`) · **Banco:** `plantaopro` local + `plantaopro_test` (PostgreSQL 18) · **Tenant de prova:** Sistema Vertical (`…ca301`)

## 1. Escopo do corte

O cadastro de produto ganhou **Preço de Venda padrão** e as derivadas de precificação **recalculadas no servidor** (definição única no registro `Produto360`, espelhada no `Produto360ViewModel`):

- **Lucro bruto unitário** = `preco_venda − preco_custo`
- **Margem sobre venda (%)** = `(preco_venda − preco_custo) / preco_venda × 100` — margem **% da VENDA**
- **Markup sobre custo (%)** = `(preco_venda − preco_custo) / preco_custo × 100` — markup **% do CUSTO** (conceitos distintos, exibidos juntos)

Regras exigidas e garantidas:
- **“Não calculável” com denominador zero**: margem → nula quando `preco_venda = 0`; markup → nulo quando `preco_custo = 0`.
- **Custo ausente ≠ ZERO**: `preco_custo` passa a admitir NULL (a coluna era `NOT NULL DEFAULT 0`, o que tornava impossível representar "custo não informado" e virava margem falsa de 100%). Custo/venda NULL ⇒ derivadas NULL (não entram na conta como zero). Linhas existentes **não foram modificadas** (sem backfill); cotações/vendas passadas não foram reescritas (DDL restrita a `adm360_produtos`).
- **Margem negativa evidente**: valores negativos passam sem clamp, destacados em vermelho na tabela com badge "margem negativa".
- **Política de arredondamento**: `Math.Round(…, 2)` — padrão do módulo (mesmo do relatório de margem de contribuição).
- Validações de servidor: `preco_custo < 0` → 400 (existente), `preco_venda < 0` → 400 (nova).
- A tela tem **prévia local** de usabilidade no modal, rotulada como prévia — o cálculo oficial é o do servidor ao salvar.

## 2. Migração

`database/migrations/2026_10_v2353_r9d_precificacao_preco_venda.sql` (idempotente):

```sql
alter table plantaopro.adm360_produtos
    add column if not exists preco_venda numeric(18,4);   -- mesma precisão do preco_custo

alter table plantaopro.adm360_produtos
    alter column preco_custo drop not null;               -- custo ausente ≠ zero

comment on column ... preco_venda is '...NULL = nao definido; derivadas calculadas no servidor...';
```

Aplicada em **ambos os bancos** (`plantaopro` ×3 idempotentes durante o desenvolvimento; `plantaopro_test` ×1 — regra P0 dos testes). Manifesto: nova entrada **v2353** com SHA-256 `4580ee72…` encadeada após v2352.

## 3. Alterações por arquivo

| Arquivo | Alteração |
|---|---|
| `database/migrations/2026_10_v2353_r9d_precificacao_preco_venda.sql` | Nova migração (coluna + nullable do custo + comment) |
| `database/migration-manifest.json` | Entrada v2353 (checksum + dependsOn) |
| `backend/PlantaoPro.Application/Administrativo360/Contracts.cs` | `Produto360`: `PrecoCusto` → `decimal?`, novo `PrecoVenda` (parâmetro final opcional) + **propriedades calculadas** `LucroBrutoUnitario`/`MargemPercentual`/`MarkupPercentual` (recalculo de servidor com as regras acima, comentadas); `SalvarProdutoAsync`: `precoCusto` → `decimal?` + `precoVenda` final opcional |
| `backend/PlantaoPro.Infrastructure/Administrativo360/CadastrosRepository.cs` | Validação `precoVenda ≥ 0`; INSERT/UPDATE persistem `preco_venda`; os dois SELECTs passam a selecionar `preco_custo` **sem COALESCE** e `preco_venda`, na **mesma ordem do construtor** (exigência prática do Dapper 2.1.35 nesta base — ver §5) |
| `backend/PlantaoPro.Api/Controllers/Adm360CadastrosController.cs` | `SalvarProdutoRequest`: `PrecoCusto` → `decimal?` + `PrecoVenda` opcional encaminhado ao repositório |
| `backend/PlantaoPro.Web/Models/Administrativo360Models.cs` | `Produto360ViewModel` espelha registro + mesmas propriedades calculadas |
| `backend/PlantaoPro.Web/Controllers/Administrativo360Controller.Cadastros.cs` | Ação `SalvarProduto`: parâmetros `decimal?` + `PrecoVenda` no payload |
| `backend/PlantaoPro.Web/Views/Administrativo360/Produtos.cshtml` | Coluna **Preço Venda / Precificação** (valor + lucro unitário + "M vend." + Markup, com estados "—"/"não calc." e destaque vermelho + badge "margem negativa"); célula de custo nulo-safe ("—" quando ausente); campo "Preço de Venda padrão" no modal + prévia local `recalcularPrecos()` (rotulada: cálculo oficial no servidor); JS de edição/limpeza |
| `backend/PlantaoPro.Web/Views/Administrativo360/Cadastros.cshtml` | Célula de custo nulo-safe ("—" quando ausente) |
| `backend/PlantaoPro.Web/Views/Administrativo360/OrcamentoNovo.cshtml` | `data-preco` dos seletores nulo-safe (custo ausente vira vazio, não erro) |

Sem novas chaves i18n (labels estáticos pt-BR por convenção do módulo; toasts reutilizados) → contrato de 246 chaves inalterado.

## 4. Execução real (prova de ponta a ponta)

```
CRIAR_P3_STATUS=200                                  (SKU SV-PREC-001 criado SEM custo)
CUSTO_AUSENTE_JSON_NULO=True DB_IS_NULL=true         ← custo ausente viaja nulo (não zero)
CUSTO_AUSENTE_DERIVADAS=True/True/True               ← lucro/margem/markup todos "não calculável"
PREC_COMPLETA_STATUS=200                             (P1: custo 10 → venda 15.25)
LUCRO_OK=True[5.25] MARGEM_OK=True[34.43] MARKUP_OK=True[52.50]      ← fórmulas exatas
CUSTO_ZERO MARGEM=100 MARKUP_NULO=True LUCRO=10.00   ← custo 0: margem calculável, markup NÃO
VENDA_ZERO MARGEM_NULA=True MARKUP=-100 LUCRO=-5.00  ← venda 0: margem NÃO; markup negativo calculável
MARGEM_NEGATIVA=-25.00 MARKUP_NEGATIVO=-20.0 LUCRO=-2.00             ← negativa evidente, sem clamp
NEG_VENDA_NEG STATUS=400 "O preço de venda não pode ser negativo."
NEG_CUSTO_NEG STATUS=400 "O preço de custo não pode ser negativo."
SEM_ESCRITURA_VENDAS=True                            ← cotações/vendas passadas intactas (count invariado)
WEB_POST_PRODUCT_STATUS=302                          (formulário real c/ antiforgery)
WEB2_TOAST=True WEB2_VENDA_16=True WEB2_MARGEM_375=True WEB2_MARKUP_60=True
FINAL_VIA_API VENDA=16.0000 MARGEM=37.50 MARKUP=60.0 LUCRO=6.00
```

Artefato HTML: `r7-sv-produtos-precificacao.html` — contém a coluna `<th>Preço Venda / Precificação</th>` (linha 537), a linha do produto com `16,00 · M vend.: 37,50% · Markup: 60,00%`, o campo `produtoPrecoVenda` e a função `recalcularPrecos()`. Nota: a asserção automatizada `WEB_HAS_HEADER_PREC` deu `False` apenas por artefato de encoding ANSI no script PowerShell (regex acentuada em .ps1 sem BOM); a verificação direta no snapshot UTF-8 confirmou o cabeçalho presente.

Estados finais dos produtos de demonstração (Sistema Vertical): P1 `SV-LTE-001` custo 10,00 / venda 16,00 (margem 37,50%); P2 `SV-NOL-001` custo 0,00 / venda 10,00 (markup "não calculável"); P3 `SV-PREC-001` custo 5,00 / venda 0,00 (margem "não calculável", markup −100%).

## 5. Testes automatizados + incidente Dapper

- Suite completa: **1394 aprovados / 0 falhas** (após todas as alterações).
- Incidente documentado: com `preco_venda` adicionado **no meio** da ordem das colunas (posição 10) enquanto o parâmetro correspondente do record fica **no final**, o Dapper 2.1.35 falhava com `A parameterless default constructor or one matching signature (…) is required for Produto360 materialization`, apesar de os nomes baterem 1:1. Alinhando a ordem do SELECT à ordem dos parâmetros do construtor (padrão já seguido por `Lote360`) a suíte voltou a 1394 verdes. **Regra para esta base: a ordem das colunas nos SELECTs que materializam um record deve espelhar a ordem dos parâmetros do construtor.**
- Incidente anterior (idêntico ao do r9c): falhas por coluna ausente no `plantaopro_test` até a migração ser aplicada no banco de testes.

## 6. Preparação para os próximos cortes

- `PrecoVenda` já flui nos DTOs (API + Web + lookups) — a cotação (r9e/§8) poderá sugerir o preço ofertado a partir do preço de venda padrão do produto sem nova migração de produto.
- Cotações/vendas históricas preservadas (snapshot próprio de preço ofertado; sem reescrita).
