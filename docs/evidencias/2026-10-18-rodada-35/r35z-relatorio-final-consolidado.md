# R35 · r35z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R34 (`90ec664`, suíte 1556).

## Escopo da rodada

Epic **E3** da diretriz (r24a1) — **lote 1 de 3**: instruções de uso por página nos 7 pontos do fluxo
cirúrgico central + compras/estoque, que não tinham nenhum bloco orientando o usuário.

## Commits

| Commit | Conteúdo |
|---|---|
| `8e63dce` | r35b1: cards "Guia da tela" (padrão da casa, 4 seções: para que serve / como utilizar / efeito / próxima ação) em `OrcamentoNovo`, `OrcamentoEditar`, `OrcamentoDetalhes`, `PedidosCompra`, `Estoque`, `Coleta`, `ValeDetalhes` + subtítulo de Estoque sem "atomicamente" + botão "Confirmar Expedição" sem "Transacional" + fato `R35InstrucoesPorPaginaLote1ContratoTests` (estrutura do guia nas 7 views) + r35a0 (inventário das 29 páginas e divisão em 3 lotes) |
| `r35b2` | errata r35a0 (termo de produto externo vetado pela higiene do repositório) + este r35z |

## Fidedignidade dos guias

Cada guia foi redigido contra a ação real do controller/view: edição de orçamento aprovado gera revisão
numerada e volta ao Rascunho; aprovação de pedido congela preços e habilita recebimento; expedição do
vale registra saída da custódia; coleta vincula leitura à tarefa com rastro. Redação simples (E1),
zero jargão novo — coberto pelos scanners existentes (R29) e pelo fato próprio (estrutura).

## Gates e suíte

- Rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Classes dependentes: **11/11 verde** (R35 + R29 + R30 + R31 + R33 + R34).
- Primeira suíte completa: exit 1 com exatamente 3 falhas, todas triadas:
  - **REAL (1)**: `RepositoryHygieneContractTests` — meu próprio doc r35a0 continha um termo de produto
    externo vetado pelo teste de higiene (a palavra em si não podia ser citada aqui sem re-acender o scan;
    ver o diff do r35b2 para o trecho exato). Corrigido no r35b2; classe de higiene
    re-executada em isolamento: **8/8 verde**. Varredura preventiva dos 24 termos vetados sobre todo o
    repo confirmou ausência de outras ocorrências (só hits legados pré-existentes fora do escopo do teste).
  - **FLAKES (2)**: `ProvisionamentoB5Tests.P2_EmailDuplicado_400_SemResiduo` e
    `P3_CorridaMesmoCnpj_ExatamenteUmVence_Outro409` — família conhecida de flake sob carga paralela;
    protocolo aplicado: **isolate 5× verde para cada um**. Nada a ver com a rodada (nem BFF nem UI).
- Reconfirmação completa (exit 0 sobre a base 1557 = 1556 + 1 fato) acontece na próxima suíte integral
  (pós-r36b1, que já inclui a correção do r35b2).

## Estado de casa

- Apps PARADOS, portas livres. Rodada 35 fechada neste push.
- Fila: **R36 E3 lote 2** (12 páginas: qualidade/transferências/contas a pagar/fiscal/XML/cotações —
  recon completo já feito, incluindo 4 micro-correções de código interno nos subtítulos fiscais
  "(A29)"/"(A33)"/"(MVP A29)"/"(P1)"), **R37 E3 lote 3** (cadastros organizacionais + gestão),
  depois R38–R39 E2 e R40+ E9.
