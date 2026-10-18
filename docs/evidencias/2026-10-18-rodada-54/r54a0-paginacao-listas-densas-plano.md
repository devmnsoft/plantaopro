# R54 — Paginação das listas densas (Orçamentos + Estoque)

Rodada 54 da pós-diretriz r24a1 — item 1 da fila do r53b1 §7 (maior impacto restante).

## 1. Problema (evidência R52, versão versionada r52_smoke_e2e.ps1)

| Página | HTML medido | Tempo medido | Causa (confirmada nesta recon) |
| --- | --- | --- | --- |
| `/Administrativo360/Orcamentos` | ~17,2 MB | ~12–20 s | 9.330 linhas em `plantaopro.adm360_orcamentos` (psql, plantaopro_test); SQL de `ListarAsync` sem LIMIT; view renderiza TODAS as linhas |
| `/Administrativo360/Estoque` | ~8,75 MB | ~9–19 s | ~5,7 mil linhas em `plantaopro.adm360_saldos` (pg_stat: `adm360_lotes`=5.687 na mesma escala); SQL de `ConsultarAsync` sem LIMIT |

A view em si é pequena (Orcamentos.cshtml 9,8 KB / Estoque.cshtml 11,1 KB) — o peso é 100% do loop dinâmico.
Páginas padrão da suíte média: 50 ms. As duas páginas acima estão fora da média por volume de dados,
não por construção do código.

## 2. Decisões

- **D-R54-1** — Paginação NO SERVIDOR (API) nos dois endpoints, com envelope próprio:
  - `GET api/administrativo360/orcamentos` devolve
    `OrcamentoResumoPaginado(Itens, Total, Pagina, PorPagina)`
  - `GET api/administrativo360/estoque` devolve
    `SaldoEstoquePaginado(Itens, Total, Pagina, PorPagina, TotalFisico, TotalReservado, TotalDisponivel)`
  - Envelopes **concretos, não genéricos**: o scanner de tipos do contrato R20
    (`CarregarTipos`, regex `record|class nome (|\{`) não captura tipos genéricos — um
    `Paginado<T>` invisível ao parser quebraria o contrato.
- **D-R54-2** — Os cards de resumo do Estoque (Físico/Reservado/Disponível) continuam refletindo o
  CONJUNTO FILTRADO COMPLETO, não só a fatia da página: agregação SQL extra
  (`COALESCE(SUM(...),0)` com o MESMO WHERE) dentro de `ConsultarPorPaginaAsync`/
  `ConsultarPaginadoAsync`. Sem agregação, os cards dariam soma parcial por página (regressão semântica).
  O envelope de Orçamentos não precisa de agregados (a página não tem cards de resumo).
- **D-R54-3** — Parâmetros de paginação: `pagina` (default 1, mínimo 1) e `porPagina`
  (default 100, teto 500), clampado no controller com `Math.Max(1, pagina ?? 1)` /
  `Math.Clamp(porPagina ?? 100, 1, 500)`. SQL: `LIMIT @porPagina OFFSET @offset`
  (parâmetros nominais Npgsql) + `SELECT count(*)::bigint` sobre a mesma base (padrão D-R24-2).
- **D-R54-4** — Assinaturas existentes preservadas: `ListarAsync`/`ConsultarAsync` não mudam
  (≥10 call-sites em testes de banco). Adiciona-se `ListarPaginadoAsync`/`ConsultarPaginadoAsync`;
  a base SQL vira `const` compartilhada (WHERE + ORDER BY idênticos nas duas variantes).
- **D-R54-5** — Adaptadores secundários do mesmo endpoint (inventariados nesta recon):
  - `PreencherLookupsCirurgiaAsync` / `PreencherLookupsValeAsync` (seletores de orçamento nos
    formulários): passam a ler `.Itens` do envelope; recebem o default (100 mais recentes,
    `ORDER BY created_at DESC`) — antes carregavam 9.330 opções num `<select>` plano.
  - `Movimentacoes()` (transferências): picker de saldos, já filtra `LIBERADO && Disponivel > 0`
    no cliente; passa a pedir `condicao=LIBERADO&porPagina=500` no servidor.
- **D-R54-6** — UI: pager estático (sem JS) com contagem "Mostrando A–B de N" e botões
  Anterior/Próxima preservando os filtros ativos (o formulário GET já reseta para a página 1,
  pois omite `pagina`). Contratos atualizados: dicionário R20 (2 entradas) + novo
  `R54PaginacaoListasDensasContratoTests` (fatos travados por scan de fonte).

## 3. Inventário de consumidores (recon desta rodada)

`GET api/administrativo360/orcamentos`:
1. `Administrativo360Controller.Orcamentos.cs:16` — página principal → paginada
2. `Administrativo360Controller.cs:51` — lookup Cirurgia → `.Itens`
3. `Administrativo360Controller.cs:63` — lookup Vale → `.Itens`

`GET api/administrativo360/estoque`:
4. `Administrativo360Controller.EstoqueValorizacao.cs:21` — página principal → paginada
5. `Administrativo360Controller.EstoqueValorizacao.cs:42` — Movimentações → `.Itens` + limite

## 4. Arquivos alterados

**Api/Application/Infra**
- `Application/Administrativo360/Contracts.cs` — +2 records + 2 métodos de interface
- `Infrastructure/Administrativo360/OrcamentoCirurgicoRepository.cs` — base const + `ListarPaginadoAsync`
- `Infrastructure/Administrativo360/EstoqueRepository.cs` — base const + `ConsultarPaginadoAsync` (linhas + agregado)
- `Api/Controllers/Adm360OrcamentosController.cs` — `Listar` p/ envelope c/ clamp
- `Api/Controllers/Adm360SuprimentosControllers.cs` — `Estoque360Controller.Consultar` p/ envelope

**Web**
- `Models/Administrativo360Models.cs` — +2 espelhos; `EstoqueIndexViewModel` + props (TotalRegistros, Pagina, PorPagina, 3 somas)
- `Controllers/Administrativo360Controller.Orcamentos.cs` — `Orcamentos` c/ pagina/porPagina + ViewBag de paginação
- `Controllers/Administrativo360Controller.cs` — 2 lookups lêem `.Itens`
- `Controllers/Administrativo360Controller.EstoqueValorizacao.cs` — `Estoque` paginado + `Movimentacoes` limitada
- `Views/Administrativo360/Orcamentos.cshtml` — pager
- `Views/Administrativo360/Estoque.cshtml` — pager + cards p/ somas do conjunto filtrado

**Testes**
- `Tests/R54PaginacaoListasDensasContratoTests.cs` — novo (5 fatos)
- `Tests/R20BffMappingContratoTests.cs` — 2 entradas do dicionário p/ os envelopes

## 5. Verificação (gate de casa)

1. Rebuild real Release `--no-incremental` → `%TEMP%\opencode\r54_build.log` → `r20b1_gate.ps1` 160/160.
2. Dirigidas: R54 (novo), R20BffMappingContratoTests, Administrativo360EstoqueEOrcamentosTests,
   Administrativo360FiltrosOpcionaisTests.
3. Suíte integral (esperado ≥1610 = 1605 + 5 novos fatos; protocolo flake imutável se B5/R7O/R19 flakerearem).
4. **Reversão da linha de base R52**: API+Web em plantaopro_test, `r52_smoke_e2e.ps1` →
   Orçamentos/Estoque devem sair da categoria HEAVY (>1 MB) e os marcadores PAGE_OK se manterem.
   Teardown por porta, PORTAS_LIVRES=True.
5. Evidência r54z com números antes/depois; docscan (byte-scan de termos vetados/corrompidos nos docs novos).
6. Commits sem acentos (prefixo de seções) + push canônico `git push origin main`.

## 6. Riscos

- **R20BffMappingContratoTests**: o dicionário é Ordinal-exacto sobre o elemento de cada chamada
  `ReadApiResponse<T>`; TODOS os 5 call-sites + as 2 entradas precisam mudar juntos (build local valida).
- **C# 10**: nenhum ternário direto em interpolação (CS8361) nas mudanças Razor/controller.
- **Testes de banco** que usam `ListarAsync`/`ConsultarAsync`: assinatura intacta (D-R54-4).
- **Whitelists**: nenhuma mudança em copy vetada; textos novos do pager são neutros e sem exclamação.
