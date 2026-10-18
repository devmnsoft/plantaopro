# R31 · r31a0 — E5 formulários parte 2: operações + pisos/tetos (mesma régua D-R30-1)

Data: 2026-10-18 · Sessão: `opencode` · Base: R30 (`19c7bf6`, suíte esperada 1548).

## Método

Inventário de campos (input/select/textarea com tipo de dados) das views operacionais Adm360
(`%TEMP%\opencode\r31_inventario_inputs.txt`, 24 views) cruzado com os tamanhos reais de coluna
(`information_schema` do banco dev — dump completo em `adm360%`). Decisão **D-R30-1** reaplicada sem
alteração (válida desde r30a0).

## Correções (r31b1)

| # | Local | Problema → correção (fonte da coluna) |
|---|---|---|
| 1 | `Recebimentos.cshtml` L119 | documento sem teto → `maxlength="80"` (adm360_recebimentos.documento 80) |
| 2 | idem L187 | lote sem teto → `maxlength="80"` (lotes/saldos/leituras.lote 80) |
| 3 | `Coleta.cshtml` L93 | código do leitor sem teto → `maxlength="100"` (leituras.codigo 100) |
| 4 | idem L99 | lote opcional sem teto → `maxlength="80"` |
| 5 | `PedidosCompra.cshtml` L187 | frete sem piso → `min="0"` |
| 6 | `FluxoCaixa.cshtml` (modal conta) | nome/banco/agência/conta sem teto → `maxlength` 120/40/20/30 (adm360_contas_financeiras) |
| 7 | `ContasFinanceiras.cshtml` (edição inline) | idem nos 4 campos `*-@c.Id` |
| 8 | idem (modal nova conta) | idem nos 4 campos `*Nova` |
| 9 | `TituloDetalhes.cshtml` L134 | valorRecebido sem piso → `min="0.01"` (baixa zerada não é lançamento) |
| 10 | `TituloPagarDetalhes.cshtml` L340 | nº documento sem teto → `maxlength="80"` (titulos_pagar.documento 80) |
| 11 | `NovaDespesa.cshtml` L63/L88 | documento → 80; centroCusto → `maxlength="80"` (centro_custo 80) |
| 12 | `OrcamentoNovo.cshtml` L53/L76 | procedimento sem teto → `maxlength="180"` (orcamentos.procedimento 180); vendedorId sem teto → `maxlength="36"` (vendedor_id uuid) |
| 13 | `OrcamentoEditar.cshtml` L63/L79 | idem (procedimento 180; vendedorId 36) + placeholder residual "ID do Produto" (×2, linha + template JS) → "Código do produto" (E1) |

## Whitelist desta rodada (auditado, nada a fazer)

Colunas `text` (sem teto legítimo): movimentos.motivo, inspecoes.justificativa, ocorrencias.descricao,
ocorrencias.destino, vales.observacoes, titulo_baixas.referencia, orcamentos.observacoes. Campos controlados
por `<select>` (destino de inspeção, tipos de conta, condições) não precisam de teto. Filtros de listagem
(busca/início/fim) seguem sem teto por design.

## Fato permanente (r31b1)

`R31ValidacaoFormulariosOperacionaisContratoTests`: contrato de atributo por arquivo (teto/piso esperado
nos campos operacionais + ausência do placeholder "ID do Produto").

## Plano de fatias R31

| Fatia | Conteúdo | Fatos |
|---|---|---|
| r31b1 | Os 13 pontos (11 views) | 2 fatos |
| r31z | Consolidado + suíte (base 1548) + push | — |

Com R31 fecha o E5 no escopo Adm360. Roadmap: R32–R33 = E6 (listas buscam por nome / números
incrementais visuais).
