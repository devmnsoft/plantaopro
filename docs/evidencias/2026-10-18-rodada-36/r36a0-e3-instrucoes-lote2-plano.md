# R36 · r36a0 — E3 lote 2: qualidade/transferências/pagamentos/fiscal/XML/cotações

Data: 2026-10-18 · Sessão: `opencode` · Base: R35 (`de16887`, suíte 1557).

## Escopo (plano herdado do r35a0)

12 páginas sem bloco de instrução + limpeza de códigos internos de decisão que escaparam para o texto
renderizado das 4 telas fiscais.

## Correções (r36b1)

**Guias "Guia da tela" (padrão da casa, 4 seções) em:**
`Inspecoes`, `Movimentacoes`, `TituloPagarDetalhes`, `Fiscal\Configurar`, `Fiscal\Detalhes`,
`Fiscal\Notas`, `Fiscal\Nova`, `DocumentosXml\Importar`, `DocumentosXml\Sincronizacao`,
`Cotacoes\ContasPortal`, `Cotacoes\Estabelecimentos`, `Cotacoes\Relacionamento`.

**Códigos internos removidos de texto renderizado (Fiscal):**
`(A33)` ×2, `(A29)` ×2, `(MVP A29)`, `(P1)` ×4, `(H19)` ×3, `(L33/H19)` ×3 — reescritos em linguagem
simples mantendo o sentido ("ainda não integrado", "o sistema confere antes de criar",
"dependem da consulta ao órgão externo", "Situação encerrada", etc.).

## Fato permanente

`R36InstrucoesPorPaginaLote2ContratoTests` (2 fatos): estrutura do guia nas 12 views (âncora + seções
1/2/4 invariantes) e ausência definitiva dos códigos `(A29)/(A33)/(MVP/H19/L33/P1)` nos 4 textos fiscais.

## Gates e suíte

- Rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Classes dependentes: **24/24 verde** (R36 + R35 + R29 + R34 + R25 + R30 + R31 + RepositoryHygiene).
- Suíte integral na sequência (base 1559 = 1557 + 2 fatos); esta suíte também reconfirma o verde do
  r35b2 (correção de higiene do doc da R35).
