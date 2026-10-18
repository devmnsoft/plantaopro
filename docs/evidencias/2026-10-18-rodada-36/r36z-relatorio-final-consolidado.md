# R36 · r36z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R35 (`de16887`, suíte 1557).

## Escopo da rodada

Epic **E3** da diretriz (r24a1) — **lote 2 de 3**: instruções de uso por página nas 12 páginas de
qualidade/transferências/contas a pagar/fiscal/XML/cotações + remoção de códigos internos de decisão
do texto renderizado das 4 telas fiscais.

## Commits

| Commit | Conteúdo |
|---|---|
| `dc93505` | errata docs R35 (r35b3): o próprio relato da R35 citava literalmente o termo vetado pela higiene; reescrito sem a palavra |
| `94755e8` | r36b1: 12 guias "Guia da tela" (`Inspecoes`, `Movimentacoes`, `TituloPagarDetalhes`, `Fiscal×4`, `DocumentosXml\Importar`, `DocumentosXml\Sincronizacao`, `Cotacoes×3`) + limpeza dos códigos `(A33)`, `(A29)` ×2, `(MVP A29)`, `(P1)` ×4, `(H19)` ×3, `(L33/H19)` ×3 em linguagem simples + fato `R36InstrucoesPorPaginaLote2ContratoTests` (2 fatos: estrutura do guia ×12 + anti-código nos textos fiscais) + r36a0 |

## Fidedignidade dos guias

Redigidos contra o comportamento real: nota vai para processamento e retorna autorizada/rejeitada com
motivo; importação manual alimenta a lista de recebidos e permite vínculo a compras; sync DF-e avança
a partir do último ponto processado (sem duplicar); só estabelecimento com conta válida capta cotação;
código externo mapeado liga automaticamente à entidade interna.

## Gates e suíte

- Rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Classes dependentes: **24/24 verde** (R36 + R35 + R29 + R34 + R25 + R30 + R31 + RepositoryHygiene).
- Suíte integral (base **1559** = 1557 + 2 fatos): **1558 aprovados + 1 falha triada** —
  `R7OBOnboardingPreferenciasIdiomaTests.O3_PadraoDaOrganizacao_TambemConclui...` é da família rotativa
  conhecida de flake sob carga (mesma família já registrada na base como `O1_...`); protocolo aplicado:
  **isolate 5× verde** (classe inteira, 5 testes a cada execução). Nada relacionado a esta rodada.
- Esta suíte também **reconfirmou o verde do r35b2/r35b3**: `RepositoryHygieneContractTests` passou
  dentro da execução integral (o termo vetado saiu definitivamente dos arquivos versionados).

## Estado de casa

- Apps PARADOS, portas livres. Rodada 36 fechada neste push.
- Fila: **R37 E3 lote 3 final** (8 páginas de cadastros/gestão → epic E3 completo), depois R38–R39 E2
  e R40+ E9 (consolidação dos flakes de carga — duas famílias já documentadas nesta sessão: B5 P2/P3 e
  R7OB O1/O3 — são candidatos diretos à consolidação num snapshot único).
