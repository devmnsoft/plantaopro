# R32 · r32a0 — E6 parte 1: listas buscam por nome / salvam id (campo de ID livre → lookup)

Data: 2026-10-18 · Sessão: `opencode` · Base: R31 (`c9bc313`, suíte 1550).

## Recon (`%TEMP%\opencode\r32_scan_e6.ps1`)

- **Listas Adm360 já exibem dados legíveis**: nenhum GUID em célula de tabela; busca por nome/código/CNPJ.
  Metade do objetivo do E6 ("buscam por nome") já está viva nas listagens ✓.
- **Alvo real: 13 campos de ID livres** (usuário digita UUID na mão) em 7 views:
  OrcamentoEditar (HospitalId, ResponsavelFinanceiroId, MedicoId, VendedorId, Itens[].ProdutoId ×2 templates),
  OrcamentoNovo (VendedorId), ValeNovo (Itens[].ReservaId ×2), Cotacoes/Detalhes (ProdutoId),
  Cotacoes/Relacionamento (EntidadeInternaId), DocumentosXml/Detalhes (PedidoId + LocalId).

## Escopo r32b1 (quick-win OrcamentoEditar — 6 dos 13 campos)

Raiz encontrada: o GET `OrcamentoEditar` constrói o MESMO `OrcamentoFormViewModel` do NOVO mas **não chama
`PreencherLookupsOrcamentoAsync`** — os arrays Hospitais/Medicos/Pagadores/ProdutosDisponiveis chegam vazios,
por isso a tela degradou para inputs text. Correção:

| # | Local | Mudança |
|---|---|---|
| 1 | Controller `OrcamentoEditar` GET | chamar `PreencherLookupsOrcamentoAsync(client, form)` antes do `return View(form)` (espelha NOVO; POST-fail redireciona, sem View) |
| 2 | View L55 HospitalId | input text → `<select>` de `Model.Hospitais` (nome + doc), espelhando NOVO |
| 3 | idem L59 ResponsavelFinanceiroId | input text → `<select>` de `Model.Pagadores` |
| 4 | idem L67 MedicoId | input text → `<select>` opcional de `Model.Medicos` |
| 5 | idem L100/L112/L156 tabela de itens | cabeçalho "Produto ID"→"Produto"; input text → `<select>` de `Model.ProdutosDisponiveis` com `data-preco` (linha server + template JS), classe `select-produto`, autofill de preço espelhando `bindProdutoChange` do NOVO (inclui classe `input-preco` nos campos de preço) |

Decisões:
- **VendedorId segue texto** em NOVO e EDITAR: `Lookups360ViewModel` não tem lista de vendedores —
  inventar endpoint só para um campo opcional fica registrado na fila R33 (decisão D-E6-2, junto com
  ReservaId/PedidoId/LocalId/ProdutoId-cotação/EntidadeInternaId, que exigem listas específicas por ação).
- `Guid?`/model intactos: o formulário continua enviando os mesmos nomes de campo; zero mudança de contrato
  BFF/API (ADR-0003 não toca).

## Fato permanente (r32b1)

`R32LookupsCampoDeIdContratoTests`: contrato de estrutura — o controller GET edita chama o helper (regex
no arquivo .cs) e a view usa `<select name=…>` nos 4 campos (e não mais `input name="HospitalId"` etc.),
com o template de opções presente.

## Plano de fatias R32

| Fatia | Conteúdo | Fatos |
|---|---|---|
| r32b1 | Os 5 pontos acima (1 controller + 1 view) | 1 fato (2 asserts) |
| r32z | Consolidado + suíte (base 1550) + push | — |

Próxima: R33 = E6 parte 2 (D-E6-2: lookups específicos por ação para os 7 campos restantes + números
incrementais visuais nas listagens quando não houver número natural, ex.: tabelas que só têm GUID).
