# R54 · r54z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R53 (`9f224a8`, suíte 1605). Plano: `r54a0-paginacao-listas-densas-plano.md`.

## Escopo da rodada

Primeiro item da fila pós-diretriz (r53b1 §7, maior impacto restante): **paginação server-side das duas listas densas** —
`/Administrativo360/Orcamentos` (17,2 MB de HTML) e `/Administrativo360/Estoque` (≈8,75 MB), que saíam da categoria
padrão da linha de base R52 (páginas médias ~50 ms) por volume de dados, não por construção. A prova de reversão da
linha de base R52 (`r52_smoke_e2e.ps1`, script inalterado) é a evidência principal da rodada.

## O que foi entregue

### 1. Paginação server-side com envelopes concretos (D-R54-1..6 do plano)

- `GET api/administrativo360/orcamentos` devolve `OrcamentoResumoPaginado(Itens, Total, Pagina, PorPagina)`;
  `GET api/administrativo360/estoque` devolve `SaldoEstoquePaginado(Itens, Total, Pagina, PorPagina,
  TotalFisico, TotalReservado, TotalDisponivel)`. Envelopes **concretos não genéricos** porque o scanner de tipos do
  contrato R20 não captura registros genéricos (D-R54-1).
- Cards de resumo do Estoque seguem refletindo o **conjunto filtrado completo**: agregado extra
  (`COALESCE(SUM(...),0)` com o mesmo WHERE) dentro de `ConsultarPaginadoAsync` (D-R54-2).
- Parâmetros `pagina` (mínimo 1) e `porPagina` (default 100, teto 500) clampados nos controllers da API e propagados
  no BFF; SQL `LIMIT @porPagina OFFSET @offset` + `count(*)::bigint` sobre a mesma base (D-R54-3).
- `ListarAsync`/`ConsultarAsync` preservadas (~10 call-sites em testes); a base SQL virou `const` compartilhada entre
  as duas variantes (D-R54-4/D-R54-bis).
- Adaptadores secundários migrados: lookups Cirurgia/Vale leem `.Itens` (100 mais recentes em vez de ~8,4 mil opções
  num `<select>` plano) e `Movimentacoes()` passa a pedir `condicao=LIBERADO&porPagina=500` (D-R54-5).
- UI: pager estático sem JS — contagem "Mostrando A–B de N" + Anterior/Próxima preservando os filtros ativos via
  `asp-route-pagina` (D-R54-6). Contratos: dicionário R20 atualizado (2 entradas) e novo
  `R54PaginacaoListasDensasContratoTests` com 6 fatos travados por scan de fonte.

### 2. Correção encontrada na verificação — erro 42803 do Postgres (D-R54-7)

A primeira rodada de e2e rodou sobre um build em que o `ORDER BY` original fazia parte da base SQL compartilhada.
Postgres rejeita coluna não agregada em `count(*)`/`SUM` sem `GROUP BY`:
`42803: coluna "o.created_at" deve aparecer na cláusula GROUP BY ou ser usada em uma função de agregação`.
As páginas renderizavam 200 com tabela vazia (500 na API) — o smoke passou (`FALHAS=0`) mas o pager e2e acusou
4 falhas de contagem, desvalidando os números. Corrigindo: o ORDER BY saiu das bases agregadas e virou const
separado aplicado **somente** às queries de linhas — `ORDER BY o.created_at DESC` (Orçamentos) e
`ORDER BY produto, validade NULLS LAST` (Estoque FEFO) — em `ListarAsync`, `ListarPaginadoAsync`, `ConsultarAsync` e
`ConsultarPaginadoAsync`. Todas as medições abaixo foram refeitas sobre o build corrigido.

## Números antes/depois (prova de reversão da linha de base R52)

Ambiente idêntico ao R52: API+Web Release sobre `plantaopro_test`, login do gestor demo. Totais visíveis ao tenant demo:
**8.404 orçamentos** (tabela global: 9.330 linhas) e **7.831 registros de saldo** (recon havia estimado ~5,7 mil).

| Página | Antes (R52) | Depois (R54) | Redução |
| --- | --- | --- | --- |
| `/Administrativo360/Orcamentos` | 17.215.398 B · ~12–20 s · HEAVY | **260.597 B** (página 1, 100 linhas) · média **366 ms** (n=5) · API 30–45 ms/página | **≈66×** menor em peso; fora da categoria HEAVY |
| `/Administrativo360/Estoque` | ≈8,75 MB · ~9–19 s · HEAVY | **181.902 B** (página 1, 100 linhas) · API **7,4–10,3 s**/pedido mesmo quente | **≈48×** menor em peso; latência server-side NÃO resolvida (abaixo) |

Prova do pager (`r54_pager_e2e.ps1`, nova nesta rodada, `R54_PAGER_E2E_OK`, `FALHAS=0`):
- Orçamentos p1: "Mostrando 1–100 de 8.404 orçamentos"; p2 com `porPagina=50`: faixa "Mostrando 51–100…" + link de
  retorno para `pagina=1` (len 260.597 → 158.857 B).
- Estoque p1: "Mostrando 1–100 de 7.831 registros" + card de resumo intacto (somas do conjunto filtrado completo);
  p2 `porPagina=50`: faixa deslocada (len 181.902 → 121.497 B).
- Filtro preservado na navegação: `busca=ORC&pagina=2&porPagina=50` mantém `busca=ORC` nos links do pager.

Smoke R52 reexecutado sobre o build final: `R52_E2E_OK`, `FALHAS=0`, 13× `PAGE_OK`; a linha de HEAVY do baseline
desapareceu para Orçamentos.

## Pendência honesta registrada

A latência server-side do Estoque **não melhorou estruturalmente** com a paginação: cada pedido demora 7,4–10,3 s
mesmo quente, pois a consulta (linhas + `count(*)` + 3 `SUM`) percorre a mesma view pesada de saldos — o ganho aqui é
de **peso da página** (transferência + render), não de custo de query. Próximo item de fila sugerido: otimizar a view
de saldos (índices ou materialização), fora do escopo desta rodada. Orçamentos, em contrapartida, respondeu em
30–45 ms por página.

## Evidências

- Rebuild real `dotnet build backend\PlantaoPro.sln -c Release --no-incremental` → exit 0; gate `r20b1_gate.ps1`
  `Now=160 Base=160 DIFF=NENHUMA` (multiset idêntico chave-a-chave). Detalhe de processo: o primeiro rebuild pós-fix
  falhou só por lock de DLL (apps antigos ainda em pé segurando os binários); teardown por porta antes de reconstruir.
- Dirigidas (família de banco + contratos tocados): **47/47** (R54 + R20BffMapping +
  Administrativo360EstoqueEOrcamentos + FiltrosOpcionais).
- Suíte integral: **1611/1611**, exit 0 (55 s) = base 1605 + 6 fatos novos do contrato R54; executada sobre o build
  final (SQL corrigida). Contrato i18n (653 valores ×4 culturas) segue verde dentro da suíte.
- Smoke e2e versionada: `R52_E2E_OK` (script inalterado do R52); pager e2e versionada: `R54_PAGER_E2E_OK`. Teardown
  por porta ao fim: `PORTAS_LIVRES=True`.
- Acessibilidade (R49) inalterada por construção: o pager usa apenas `<a>`/`<span>`, sem novos campos/botões.

## Arquivos

Ver inventário completo no §4 de `r54a0-paginacao-listas-densas-plano.md` (Application/Infrastructure/Api/Web/Tests),
mais esta pasta de evidências: `r54a0-paginacao-listas-densas-plano.md`, `r54_pager_e2e.ps1` e este relatório.

## Commits

| Commit | Conteúdo |
|---|---|
| `web,api,app,infra,tests (rodada 54 r54)` | paginação server-side: envelopes, repositórios, BFF, pagers, contrato R54, dicionário R20 |
| (este) `docs (rodada 54 r54)` | plano r54a0 + prova pager e2e + este relatório final |
