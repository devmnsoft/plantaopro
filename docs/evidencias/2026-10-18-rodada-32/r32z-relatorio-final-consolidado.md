# R32 · r32z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R31 (`c9bc313`, suíte 1550).

## Escopo da rodada

Epic **E6** da diretriz (r24a1), parte 1: "listas buscam por nome / salvam id" — quick-win
**OrcamentoEditar** (6 dos 13 campos de ID livres mapeados no recon r32a0).

## Commits

| Commit | Conteúdo |
|---|---|
| `b7a7a0a` | r32b1: `OrcamentoEditar` GET passa a chamar `PreencherLookupsOrcamentoAsync` (espelha o NOVO — raiz: os arrays Hospitais/Medicos/Pagadores/ProdutosDisponiveis chegavam vazios e a tela degradou para inputs de UUID); view trocou os 4 inputs text por selects espelhando o NOVO (hospital/pagador com doc, médico opcional) + itens: header "Produto ID"→"Produto", input text → select com `data-preco` + template `<template id="produtoOptionsTemplate">` + autofill de preço via `bindProdutoChange` (linha server + template JS + classe `input-preco` nos campos de preço) + `R32LookupsCampoDeIdContratoTests` (2 fatos) + ajuste do fato R31 (placeholder "Código do produto" substituído pelo select — agora coberto pelo fato R32) + r32a0 |

## Decisões

- **Zero mudança de contrato BFF/API**: o formulário envia os mesmos nomes de campo (`HospitalId`,
  `Itens[i].ProdutoId`…); o ADR-0003 não toca (D-E6-1).
- **D-E6-2 (fila R33)**: os 7 campos restantes sem lookup exigem lista específica por ação —
  ValeNovo ReservaId ×2 (VM sem lista de reservas; precisar de endpoint), Cotacoes/Detalhes ProdutoId L286
  (escape hatch manual intencional do s8), Relacionamento EntidadeInternaId, DocumentosXml/Detalhes
  PedidoId + LocalId (VM do modal de vínculo). VendedorId também segue texto (sem lista de vendedores).
- Recon confirmou a outra metade do E6: **listas Adm360 já exibem dados legíveis** (nenhum GUID em
  célula de tabela; busca por nome/código/CNPJ) ✓.

## Gates e suíte

- Rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Suíte completa: **Total 1552** (= base 1550 + 2 fatos). Execução de referência (background):
  **1552/1552 verde, exit 0**. Uma reexecução foreground sobreposta (feita enquanto a primeira ainda
  segurava o log/DLL) teve 1 falha em `OnboardingC7Tests.C1_Iniciar_SemContrato_MaterializaSoNucleoDoCatalogo`
  (erro no purge de Dispose com duas suítes no mesmo banco) — **isolate 5× = 5/5 verde**; artefato de
  carga duplicada, não da rodada. Lição de processo: antes de reexecutar, confirmar que o shell de
  background liberou o arquivo de log.

## Estado de casa

- Apps PARADOS, portas livres. `origin/main` sincronizada neste push.
- Fila: **R33 = E6 parte 2** (D-E6-2: lookups específicos por ação para os 7 campos restantes + números
  incrementais visuais onde não houver número natural); depois R34 E7, R35–R37 E3, R38–R39 E2, R40+ E9.
