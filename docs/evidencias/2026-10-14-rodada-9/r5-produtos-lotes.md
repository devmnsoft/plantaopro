# r5 — Tela de produtos com lotes vinculados: listagem + registro sem estoque automático (§5)

**Data:** 2026-10-14 · **Base:** commit local r9a (`7a3f5ad`) · **Banco:** `plantaopro` local (PostgreSQL 18) · **Tenant de prova:** Sistema Vertical (`…ca301`)

## 1. Escopo do corte

- A tela de **Produtos & Materiais** agora lista os lotes vinculados ao produto selecionado e permite **registrar lote** (código, data de validade, custo unitário opcional), reutilizando o módulo existente de lotes (mesma tabela `adm360_lotes`, mesma unique e mesma semântica de preenchimento do fluxo de compras).
- **Registro de lote ≠ entrada automática de estoque**: a tela não possui campo de quantidade. A quantidade entra apenas por movimentação com origem (`adm360_movimentos`: `origem_tipo`/`origem_id`, `quantidade <> 0`, chave de idempotência) — semântica já existente no módulo e preservada integralmente.
- A mensagem informativa da seção distingue **validade do lote** (expiração do produto) da **vigência regulatória** (preparação para o §6 — cadastro ANVISA).

## 2. Alterações por arquivo

| Arquivo | Alteração |
|---|---|
| `backend/PlantaoPro.Application/Administrativo360/Contracts.cs` | `Lote360` ganha `CustoUnitario` (parâmetro final opcional, compatível); `ICadastrosRepository.SalvarLoteAsync` novo |
| `backend/PlantaoPro.Infrastructure/Administrativo360/CadastrosRepository.cs` | `SalvarLoteAsync`: valida produto/controle de lote e faz upsert `ON CONFLICT(tenant_id, produto_id, codigo)` com `COALESCE` (idêntico ao fluxo de compras); **não insere em `adm360_movimentos`** (comentário documenta a regra); `ListarLotesAsync` e lookups agora retornam `custo_unitario` |
| `backend/PlantaoPro.Api/Controllers/Adm360CadastrosController.cs` | `POST api/administrativo360/cadastros/lotes` (policy `Adm360.MapearProdutos`) + `SalvarLoteRequest`; mapeia KeyNotFound→404, Argument/InvalidOperation→400 |
| `backend/PlantaoPro.Web/Models/Administrativo360Models.cs` | `Lote360ViewModel.CustoUnitario`; `ProdutosIndexViewModel` ganha contexto de produto + lista de lotes |
| `backend/PlantaoPro.Web/Controllers/Administrativo360Controller.Cadastros.cs` | `Produtos(...)` aceita `produtoId` (carrega lotes via endpoint dedicado, sem teto do bundle de lookups); nova ação `SalvarLote` [HttpPost, ValidateAntiForgeryToken] |
| `backend/PlantaoPro.Web/Views/Administrativo360/Produtos.cshtml` | Botão "Lotes vinculados" por linha da tabela; seção com tabela de lotes (código/validade/custo, destaque "(vencido)") + formulário de registro com `@Html.AntiForgeryToken()` (padrão dos demais formulários do módulo) e aviso "registrar um lote **não ajusta o estoque**" |
| `backend/PlantaoPro.Web/Localization/UiTexts*.resx` (pt/en/es/fr) | 2 novas chaves: `Adm360.Cadastros.OkLoteSaved`, `Adm360.Cadastros.FallbackSaveLote` (4 idiomas) |
| `backend/PlantaoPro.Tests/R7DF6Admin360I18nContratoTests.cs` | Contrato de chaves Adm360 atualizado 244→246 (as 2 novas existem nos 4 arquivos — paridade mantida) |

## 3. Semânticas preservadas / reutilizadas

- Upsert com `COALESCE(lotes.validade, excluded.validade)` e `COALESCE(lotes.custo_unitario, excluded.custo_unitario)` — re-registrar um lote **não sobrescreve** validade/custo já preenchidos (comportamento idêntico ao recebimento de compras).
- Validações de servidor: código obrigatório; validade obrigatória; custo ≥ 0; produto inexistente → 404; produto sem controle de lote → 400 com instrução clara.
- Nenhuma escrita em `adm360_movimentos` durante o registro (verificado por contagem antes/depois abaixo).

## 4. Execução real (prova de ponta a ponta)

```
API_LOGIN_OK TOKEN_LEN=10164                          (login SV, escopo TENANT)
CRIAR_P1 STATUS=200 BODY={"id":"3a002ffc-…"}          (SKU SV-LTE-001, controla_lote=true)
CRIAR_P2 STATUS=200 BODY={"id":"83816d7d-…"}          (SKU SV-NOL-001, controla_lote=false)
MOV_P1_BEFORE=0
REG_L1 STATUS=200 BODY={"id":"911839cc-…"}            (LT-2026-001, 2027-01-31, custo 9.50)
REG_L1_AGAIN STATUS=200                               (re-registro c/ 2099-01-01 e custo 1)
LISTAR_L1 N=1 CODIGO=LT-2026-001 VALIDADE=2027-01-31 CUSTO=9.5000   ← convergência COALESCE
NEG_VALIDADE_NULA STATUS=400 "Data de validade do lote é obrigatória."
NEG_PRODUTO_SEM_LOTE STATUS=400 "Este produto não tem controle de lote habilitado…"
NEG_PRODUTO_GHOST STATUS=404 "Produto não encontrado neste cliente."
NEG_CUSTO_NEGATIVO STATUS=400 "O custo unitário do lote não pode ser negativo."
MOV_LOTE_L1=0 MOV_P1_AFTER=0 NO_AUTO_STOCK=True       ← registro não gerou movimentação

WEB_CATALOG_STATUS=200 HAS_LOTES_BTN=True             (botão por linha no catálogo)
WEB_LOTES_PAGE_STATUS=200 WEB_HAS_SECTION=True WEB_HAS_L1=True
WEB_HAS_NOAUTOSTOCK_INFO=True WEB_HAS_CUSTO_L1=True   ("9,50" formatado pt-BR)
WEB_POST_LOTE_STATUS=302 LOCATION=/Administrativo360/Produtos?produtoId=3a002ffc-…  (formulário real c/ antiforgery da página)
WEB2_HAS_L2=True WEB2_HAS_TOAST_OK=True WEB2_HAS_CUSTO_L2=True      (toast "Lote registrado com sucesso.", "11,25")
MOV_P1_FINAL=0 INVARIEDO=True                         ← após o registro pelo formulário Web também
```

Artefatos HTML: `r5-sv-produtos-lotes-antes.html` (seção com LT-2026-001) e `r5-sv-produtos-lotes-depois.html` (com LT-2026-002 + toast de sucesso).

## 5. Testes automatizados

- Suite completa: **1394 aprovados / 0 falhas** (após todas as alterações deste corte, incluindo a revisão do formulário).
- Par `Administrativo360AutenticacaoEJornadasTests` que falhou uma vez na primeira execução da rodada: **5 execuções isoladas consecutivas verdes** → transiente de ambiente (máquina de baixa RAM), não regressão.
- Contrato i18n: 246 chaves Adm360 nos 4 idiomas (verde).

## 6. Observações para os próximos cortes

- O custo unitário do lote já está disponível nos DTOs (`Lote360.CustoUnitario`) — será a base do melhor custo para o §7 (precificação).
- O campo ANVISA do item ainda é AUSENTE (§6) — a próxima migração adicionará as colunas; o layout da seção de lotes reserva espaço textual sem quebrar (tabela com 3 colunas estável).
