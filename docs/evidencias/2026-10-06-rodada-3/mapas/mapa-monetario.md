# Mapa — Integridade monetária e cultural (teclado → banco → tela)

> Exploração somente leitura gerada por agente em 2026-10-06 sobre `origin/main` = `7f43af2`. Investigação do finding M2.5 ("12,50" persistido como 1250). Fonte primária do item 2 (GATE financeiro) da rodada 3.

Reprodução investigada: digitar `12,50` em "preço de custo" de produto persistir `1250`. Escopo: somente leitura, caminhos relativos à raiz do repositório.

## Pipeline (estágios 1–7)

### 1. Entrada e máscara (views/JS)
- Campo: `backend\PlantaoPro.Web\Views\Administrativo360\Produtos.cshtml:238` — `<input type="number" step="0.01" min="0" name="PrecoCusto">`, **sem máscara monetária**. Segunda ocorrência (outro formulário de cadastro): `...Administrativo360\Cadastros.cshtml:341`.
- Não existe máscara de valor em lugar nenhum: `wwwroot\js` contém apenas `form-experience.js` (não toca em números); única máscara da aplicação é a de CPF em `...Administrativo360\Colaboradores.cshtml:296-304`. Nenhuma lib vanilla-masker/imask/inputmask.
- JS inline de `Produtos.cshtml:276-312`: só atribui `input.value` com strings brutas (`'0.00'`, valor de edição formatado em formato invariant) e habilita/desabilita o botão — **não normaliza vírgulas**.

### 2. Serialização do formulário (browser)
- POST simples `multipart/form-data` (`<form method="post" asp-action="SalvarProduto">`), sem fetch/JSON no cliente para produtos.
- O que o browser envia depende do motor: Chrome pt-BR normalmente converte `12,50` → `12.5` no blur/commit do `type="number"`, mas **submissão via Enter antes do blur, Safari e webviews podem enviar `"12,50"` cru** (o spec de serialização aceita dígitos locais até o commit). Ou seja, a string com vírgula chega ao servidor em condições reais de uso — é o gatilho do estágio 3.

### 3. Model binding .NET (ponto raiz)
- Ação BFF: `SalvarProduto(Guid? id, string sku, ..., decimal precoCusto, bool ativo = true)` em `backend\PlantaoPro.Web\Controllers\Administrativo360Controller.Cadastros.cs:202-219`.
- Sem `RequestLocalization`, sem `IModelBinder`/`BinderModelFactory`/override de cultura: `backend\PlantaoPro.Web\Program.cs` registra só `AddControllersWithViews` (+ SaasRouteGuardFilter). MVC faz o binding de `decimal` de formulário com `CultureInfo.InvariantCulture`.
- **Mecanismo verificado empiricamente nesta máquina (dotnet 10.0.400):**
  - `Convert.ChangeType("12,50", typeof(decimal), CultureInfo.InvariantCulture)` → **`1250`** (comma tratado como separador de milhar; `NumberStyles.Number`), sem exceção → mutação **silenciosa**.
  - `decimal.Parse("12,50", NumberStyles.Float, InvariantCulture)` → `FormatException` (por isso o caminho relevante é o do binder de formulário com `Number`).
  - pt-BR com `AllowDecimalPoint` → `12.50` (o valor esperado — jamais usado pelo pipeline).

### 4. Opções JSON (BFF→API e API)
- BFF: `backend\PlantaoPro.Web\Controllers\BaseWebController.cs:18-23` — `JsonOptions` estáticos com `NumberHandling = JsonNumberHandling.AllowReadingFromString`; `SendApiAsync` (`BaseWebController.cs:100-115`) serializa o request já tipado: `decimal` sai como número JSON invariante (`1250`). A flag só afeta a direção de *leitura* (aceita string como número nas respostas).
- API: `backend\PlantaoPro.Api\Program.cs` **não tem** `AddJsonOptions`/`JsonNumberHandling` (grep sem correspondências) → defaults de System.Text.Json; o registro de type handler Dapper está em `:25`. Request record `[FromBody]` com `decimal PrecoCusto` em `backend\PlantaoPro.Api\Controllers\Adm360CadastrosController.cs:97-100`.
- Conclusão: após o estágio 3, nenhum estágio posterior pode "reconhecer" a vírgula — o erro já é numérico.

### 5. Cálculos de serviço
- No caminho do produto **não há conversão de centavos** (×100/÷100): `precoCusto` flui direto. Os únicos ×/÷100 da camada são percentuais/margens (ver seção "Pontos de mutação silenciosa").

### 6. Persistência Dapper/PostgreSQL
- `SalvarProdutoAsync` em `backend\PlantaoPro.Infrastructure\Administrativo360\CadastrosRepository.cs:165-259`: parâmetro `decimal` direto (`preco_custo = @precoCusto`), sem cast string no SQL.
- Coluna: `database\migrations\2026_09_v2194_administrativo360_valorizacao_vendas_financeiro.sql:18` → `preco_custo numeric(18,4)` (nota: **não é (15,2)**); check constraint de não-negativo em `...v2198...sql:276`.

### 7. Apresentação
- Lista: `Produtos.cshtml:121` → `ToString("N2", pt-BR)`; botão edit passa string invariant (`:153`, setada em `:297`).
- `Cadastros.cshtml:189` usa `ToString("C")` (cultura da thread) — formato inconsistente com os demais.
- Financeiro/Relatórios: `"0.00"` invariant em `Administrativo360Controller.Financeiro.cs:600-643`, `...Financeiro.cs:600`+ e `...Relatorios.cs:63+`, `...RelatoriosFinanceiros.cs:60-127` (parâmetros `decimal` também em Financeiro.cs `:63,:160,:313,:514`).

## Root cause (ordenada por probabilidade)

1. **Alta — binding de formulário em cultura invariante + campo `type="number"` sem máscara.** `Administrativo360Controller.Cadastros.cs:202` (`decimal precoCusto`) recebe `"12,50"` (disparado por submissão antes do blur / Safari / webview, estágio 2); `NumberStyles.Number` + invariant trata a vírgula como **separador de milhar** → `1250` sem exceção (verificado empiricamente). Nenhum estágio posterior reconstitui a fração.
2. **Média — mesmo mecanismo nos demais formulários ADM360** (todos os ~40 inputs `type="number"` abaixo seguem o mesmo pattern: POST de formulário → `decimal` no controller → BFF→API→Dapper).
3. **Baixa/média — fluxo assistido por IA**: `AssistenteIaController.cs:75` faz `decimal.TryParse(valorConfirmado, NumberStyles.Number, CultureInfo.InvariantCulture, ...)` sobre a string crua do usuário — `"12,50"` daria igualmente `1250` nesse canal.
4. **Baixa — leitura JSON com `AllowReadingFromString`** (`BaseWebController.cs:18-23`): se um cliente enviasse a string `"12,50"` no corpo JSON para a API, o STJ aceitaria a string, mas a interpretação dependeria do formato — secundário, pois o BFF sempre reescreve números invariantes.
5. **Inexistente** como causa: arredondamentos/regra de centavos de serviço (nenhum ×100 de conversão monetária no caminho) e o schema `(18,4)` (que apenas preserva o erro com 4 casas, ex.: `1250.0000`).

## Pontos de mutação silenciosa (×100/÷100/round)

| Local | O que faz |
|---|---|
| `Administrativo360Controller.Cadastros.cs:202` | Implícito: parsing invariant de string com vírgula → ×100 (raiz) |
| `Administrativo360Controller.Financeiro.cs:63/:160/:313/:514` | Mesma classe de binding (`decimal` de formulário, invariant) em ações financeiras |
| `AssistenteIaController.cs:75` | `TryParse(..., NumberStyles.Number, InvariantCulture)` em string crua |
| `Infra\Administrativo360\ValorizacaoRepository.cs:161,267` | Comissão = total × pct/100m; `Math.Round(...,2)` |
| `Infra\Administrativo360\ComprasRepository.cs:224` | `Math.Round(totalRecebimento,2)` |
| `Infra\Administrativo360\ContasPagarRepository.cs:649` | `Math.Round(totalRecebimento,2)` |
| `Infra\Administrativo360\Adm360FinanceiroRelatoriosRepository.cs:115` | Arredondamento em relatório |
| `Infra\Administrativo360\GestaoDashboardRepository.cs:69,73-74,85` | Percentuais/margens |
| Api `ImplantacaoController.cs:56`, `OnboardingController.cs:156`, `OperacaoAssistidaController.cs:523` | ÷/×100 de percentual |

Todos os roundings reais são de **percentual/margem**; nenhum é conversão centavos/real. A mutação silenciosa dominante é o *parse*, não o *rounding*.

## Campos afetados (mesmo mecanismo, inventário de inputs)

- **Custos/preços de produto:** `Produtos.cshtml:238` (PrecoCusto), `Cadastros.cshtml:341`.
- **Financeiro:** `NovaDespesa.cshtml:81` (valor), `TituloDetalhes.cshtml:134` (valorRecebido), `TituloPagarDetalhes.cshtml:314` (valor), `FechamentoCaixa.cshtml:233` (saldoConferido), `ContasFinanceiras.cshtml:260` / `FluxoCaixa.cshtml:315` (saldoInicial), `Financeiro\Details:88`, `FaturamentoSaas\Details:93`.
- **Valoração:** `ValorizacaoPrevia.cshtml:227/235` (descontoGeral), `:240` (comissaoPercentual — %).
- **Pedidos/cotações:** `PedidosCompra.cshtml:187` (frete), `:206` (quantidade), `:210` (precoUnitario), `:214` (desconto); `OrcamentoNovo`/`OrcamentoEditar` (Itens[].PrecoUnitario/Desconto/Quantidade); `Cotacoes.cshtml:260-271`.
- **Vales:** `ValeNovo.cshtml:164-167`.
- **Demais:** `V114\Form:13`.
- Modelos de formulário com `decimal` correspondentes: `backend\PlantaoPro.Web\Models\Administrativo360Models.cs` (Orcamento `:184-203`, Vale `:338-360`, Despesa `:827`, ContaFinanceira `:893`).
- Mobile React Native (`mobile\PlantaoPro.App`): telas voltadas a médico/plantões; **nenhum cadastro ADM360 de produto encontrado lá** — reprodução assumida no fluxo Web Razor.

## Dados históricos suspeitos (evidências; sem recomendação de correção)

- `database\pgadmin\administrativo360_demo_completo.sql:447-449` — produto demo com `preco_custo 600.00`; `:475` — item de pedido `preco_unitario 100`. Valores plausíveis (sem fração), **flagrados para inspeção**, não demonstradamente corrompidos.
- `database\seeds\development\140_administrativo360_demo.sql:12` — `salario 12500` (mensal plausível; não é um "12,50" estourado).
- `database\seeds\development\141_administrativo360_suprimentos_demo.sql` — todos os valores monetários plausíveis (`85`: 100; `129-151`: 1200/600/2500/500/100; `204`: 6000/600; `237,:263,:295`: 600/500; `306-366`: 10000/500/300/1500/350/400/9600); nenhum segue o padrão ×100-de-cents.
- `DevelopmentSeed.cs` (API): **zero** INSERTs em tabelas ADM360 com valores monetários.
- Padrão a procurar em produção (evidência da causa, não remediação): linhas onde a coluna `numeric(18,4)` guarda inteiro grande com `.0000` em campo que historicamente recebeu valor fracionado (ex.: custo digitado `12,50` → `1250.0000`), típico do parse de separador de milhar descrito acima.

---

**Resumo de uma linha:** a vírgula sobrevive do teclado ao POST (sem máscara + browser), e o binding de `decimal` do MVC em cultura invariante (`Administrativo360Controller.Cadastros.cs:202`) interpreta-a como separador de milhar — `12,50` vira **1250** silenciosamente no primeiro estágio .NET; JSON e Dapper apenas carregam o valor já corrompido até `numeric(18,4)`.
