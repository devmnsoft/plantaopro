# R33 · r33a0 — E6 parte 2: lookups específicos por ação para os campos restantes (D-E6-2)

Data: 2026-10-18 · Sessão: `opencode` · Base: R32 (`1bab4ed`, suíte 1552).

## Recon

Os endpoints necessários **já existem na API** — nada de rota nova:

- `GET api/administrativo360/orcamentos/{id}/reserva-planejamento` retorna
  `PlanejamentoReservaOrcamento(…, ReservasAtivas: IReadOnlyList<ReservaAtivaPlanejamento(Id, Produto, Lote, Quantidade, CriadoEm)>)`
  (Application/Administrativo360/Contracts.cs L39/L41). 404 quando o orçamento não foi aprovado — tratado como lista vazia.
- `GET api/administrativo360/compras` (Listar sem filtro) → `PedidoResumo(Id, Numero, Fornecedor, Situacao, …)`
  já desserializado no Web como `PedidoCompraResumoViewModel`.
- `GET api/administrativo360/cadastros/lookups` → `Lookups360ViewModel.Locais`
  (`Local360ViewModel(Id, Codigo, Nome, Tipo, Ativo)`) — o mesmo helper usado nos demais formulários.

## Correções (r33b1)

| # | Local | Mudança |
|---|---|---|
| 1 | Models `ValeFormViewModel` | nova propriedade `ReservasAtivas` + novos tipos `ReservaAtivaResumoViewModel` (record) e `PlanejamentoReservaOrcamentoDto` (só lê `ReservasAtivas`) |
| 2 | `PreencherLookupsValeAsync` (controller principal) | se `model.OrcamentoId` existe → busca o planejamento de reserva e popula `ReservasAtivas` (404/vazio → lista vazia; vale em orçamento não aprovado só permite "Sem reserva") |
| 3 | `ValeNovo.cshtml` | coluna "Reserva vinculada": input text de UUID → `<select>` com as reservas ativas ("Produto · Lote (Qtde)") nas linha server (Itens[0]) E template JS (`tmplReservaOptions`, padrão `tmplProdOptions`); `atualizarIndices` passa a renomear pelo novo select (classe `select-reserva`) — antes usava índice de `input`, o quebraria com o select no lugar |
| 4 | Page VM `DocumentoXmlDetalhesPageViewModel` | novas propriedades `Pedidos` + `Locais` |
| 5 | `Adm360DocumentosXmlWebController.Detalhes` GET | busca a lista de pedidos de compra + lookups (locais) e popula o VM (falha não-fatal: lista vazia) |
| 6 | `DocumentosXml\Detalhes.cshtml` (modal de vínculo) | `PedidoId` e `LocalId`: input text de UUID → `<select>` (pedido "Número — Fornecedor (situação)"; local "Código — Nome (tipo)") |

## Decisões KEEP (auditoria completa dos 13 campos do recon r32a0)

- **Cotacoes\Detalhes ProdutoId L286**: escape hatch manual intencional do s8 — o caminho primário JÁ é o
  select `selProduto` (catálogo com regulação ANVISA); o campo manual é rotulado "IdentificadorManual" e
  documentado em comentário. Manter.
- **Cotacoes\Relacionamento EntidadeInternaId L158**: entidade interna genérica do PlantãoPro (não é só
  produto), campo opcional acompanhado de descrição livre; lookup ambíguo. Manter com placeholder de formato.
- **OrcamentoNovo/Editar VendedorId**: `Lookups360ViewModel` não tem lista de vendedores; campo opcional com
  teto de 36 (R31). Manter até existir endpoint de vendedores (fila, sem sprint agendada).
- **Números incrementais visuais** (meta 2 do E6): recon confirmou que as listagens Adm360 exibem números de
  negócio naturais (pedidos/vales/orçamentos têm `Numero`; nenhum GUID em célula de tabela — scan r32) e os
  registros sem número natural usam código legível. Considerado SATISFEITO por design; não há alvo residual.

## Fato permanente (r33b1)

`R33E6LookupsReservaEPedidoContratoTests`: contrato de estrutura — helper do vale chama
`reserva-planejamento`; VMs têm as listas; views usam select (e a ausência regressiva dos inputs/placeholder
UUID de reserva/pedido/local).

## Plano de fatias R33

| Fatia | Conteúdo | Fatos |
|---|---|---|
| r33b1 | Os 6 pontos (2 models, 2 controllers, 2 views) | 1 fato (2 asserts) |
| r33z | Consolidado + suíte (base 1552) + push | — |

Com R33 fecha o E6. Roadmap: R34 E7 (cadastro ≠ operação), R35–R37 E3 (instruções por página),
R38–R39 E2 (acesso/tenant visível só p/ super-admin), R40+ E9.
