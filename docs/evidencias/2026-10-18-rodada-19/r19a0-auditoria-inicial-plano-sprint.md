# Rodada 19 — Auditoria inicial e plano (r19a0)

- **Data:** 2026-10-18 · **Base:** `6fa7177` (`origin/main`, ponta do push da Rodada 18) · **Escopo:** topo da fila r18z §7 — item 14 (auditoria da família F7b: `DateTimeOffset` em tipos ligados ao Dapper) e item 15 (guarda global SRF-em-WHERE), com expansão orgânica para os achados que a auditoria expôs. Fora de escopo e seguidos na fila R20: scanner Adm360*WebController* (item 1), whitelist JS-driven (item 2), fantasmas P3 (item 9).

## Achados

### F1 — Mecanismo da família F7b versionado (probe executado com as versões reais do app)

Probe Dapper/Npgsql contra coluna `timestamptz` (reader devolve `DateTime`):

| Forma | Resultado |
|---|---|
| `record T(DateTimeOffset X)` / `DateTimeOffset?` | **falha** na construção do deserializer: `InvalidOperationException: A parameterless default constructor or one matching signature (System.DateTime X) is required for T materialization` — quando há ≥1 linha |
| `class { DateTimeOffset X }` (nullable ou não) | **OK** — o mapeamento de membros de classe converte |
| zero linhas (classe e record) | OK (o deserializer do record só é exigido na materialização) |

Consequência decisiva: o defeito da família F7b existe **somente em records com parâmetro `DateTimeOffset`** ligados ao Dapper. Toda a família de classes com DTOFF (ConferenceRow, PresenceRow, ProfessionalContext, ModuleRow, ProductivityRow, OperacaoAssistidaOcorrenciaDto, trio Medico*, etc.) é **isenta** pelo mecanismo — a auditoria passa a filtrar apenas records.

### F2 (F7e) — 4 records ligados ao Dapper com parâmetro DTOFF (defeito real)

Censo: 263 tipos ligados via `Query*Async<T>` no backend; 189 propriedades `DateTimeOffset` em blocos declarados; após classificação (forma classe vs record × SQL efetiva × colunas `timestamptz`) restaram exatamente 4 records:

| Record | Arquivo | Parâmetros DTOFF | Coluna (timestamptz) | Dado vivo no banco operacional |
|---|---|---|---|---|
| `OcorrenciaDto` | Api/OcorrenciaDtos.cs | PrazoResolucao?, CriadoEm, AtualizadoEm | ocorrencias_operacionais | **1 linha** (Santa Casa Demonstração, ABERTA) → `GET /api/ocorrencias` listagem/detalhe **500 hoje** |
| `OcorrenciaEventoDto` | Api/OcorrenciaDtos.cs | CriadoEm | ocorrencia_eventos | 0 (por evento) |
| `WorkItemHistoryDto` | Api/Operation360/WorkItems/WorkItemModels.cs | CriadoEm | work_item_history | 0 (work_items também 0) |
| `ProductivityItemDto` | Api/Operation360/Productivity/ProductivityService.cs | Data | user_favorites.criado_em / recent_items.acessado_em | 0 |

Falsos positivos explicados no censo: (a) supercaptura de bloco em files sem chaves (ex.: `T` genérico em Contracts.cs, `DueAt` do WorkItemPostponeRequest vazando para o bloco do WorkItemHistoryDto); (b) duplicatas Web (ViewModels.cs) alimentadas por JSON — imunes; (c) records de request/filtro (`NotificationFilter.De/Ate`, `WorkItemPostponeRequest.DueAt`) — entrada HTTP, não Dapper; (d) `NotificationDto` já corrigido com nota de casa prévia (DateTime).

Precedentes que o censo confirmou: `NotificationService.cs` já trazia a NOTE idêntica à adotada aqui; `PilotoOcorrenciaDto` já nasceu com `DateTime`.

### F3 (F7d) — `unnest(SRF)` na central de clientes (novo, P2; em duas etapas)

`SaasServices.ListCentralAsync` (`GET /api/clientes/central`, superadmin) usava `select unnest(plantaopro.modulos_efetivos(t.id)) as codigo` dentro de derived table. Probe no banco operacional: **`função unnest(record) não existe`** — erro de parse para todo o endpoint (a SRF retorna `TABLE(codigo, modulo_id, pacote_codigo)`, não array). Forma validada no banco (equivalência semântica + velocidade): subconsulta escalar com `cross join lateral plantaopro.modulos_efetivos(t.id)` — Santa Casa Demonstração devolveu os 17 módulos vigentes (mesma lista que o catálogo R18 expõe); varredura dos 88 clientes: 8 ms (1 cliente com módulos).

Segunda etapa (exposta pelo smoke vivo pós-fix da primeira): a statement de totais falhava em runtime com a assinatura F7b — `sum()` de contagem é `numeric` no Postgres e o reader devolve `Decimal`, mas o record `ClienteCentralTotais` declara os dois campos como `long`; corrigido com cast `::bigint` na SQL (contratos JSON/DTO intocados). Ambas as etapas ficam cobertas por um fato que extrai as statements exatas da fonte e executa via `QueryMultipleAsync` contra o schema real.

### F4 — Guardas globais definidas (item 15 da fila)

Inventário de SRF: a única função set-returning do app é `plantaopro.modulos_efetivos` (pg_proc/proretset). Pontos de chamada em código de produção: ModuleContractVigencia (`from` direta), SaasCoreServices (cross join lateral, R18 F7c), OnboardingJornadaService (`from` direta), SaasServices (corrigida em F3) — todos em posição FROM após o fix. Guardas versionadas como fatos de fonte (escopo: código de produção, excluindo arquivos `*Tests.cs`, que contêm SQL em asserções): (i) interseção ∅ entre "records com parâmetro DTOFF" × "tipos referenciados em `Query*Async<T>`"; (ii) nenhum `\bany(plantaopro.`; (iii) nenhum `unnest(plantaopro.<srf>(`; (iv) toda chamada a `modulos_efetivos(` precedida de `from`/`join`/`lateral` (fora de comentário).

## Decisões

- **D1:** Fix cirúrgico nos 4 records do F2 (`DateTimeOffset`→`DateTime`, padrão de casa — todos os DTOs Dapper usam `DateTime`), com NOTE explicativa no estilo da casa; ripple verificado ausente (nenhum `.UtcDateTime`/construção manual desses records; Web consome JSON com seus próprios espelhos).
- **D2:** F3 na forma canônica R18 (cross join lateral), preservando semântica do rótulo (string_agg distinct sobre módulos efetivos por tenant do cliente).
- **D3:** Guardas como fatos de teste (mecanismo + as 4 guardas de fonte + 3 fatos de regressão de banco com seed aleatório e limpeza — mesmo padrão r18b4/F7b).
- **D4 (design):** o avanço do design do sistema entra nesta rodada como ADRs: `docs/arquitetura/adr/adr-0001` (contrato de binding Dapper/Npgsql: datas em tipos ligados ao Dapper) e `adr-0002` (posicionamento de funções set-returning no PG18), com índice em `docs/arquitetura.md`. Rodadas seguintes seguem 1–2 ADRs acoplados ao trabalho.

## Plano de slices

| Slice | Conteúdo | Gate |
|---|---|---|
| r19b1 | Fixes F2+F3 + `R19DapperBindingGuardTests` (9 fatos) + ADRs 0001/0002 + índice arquitetura.md | build `--no-incremental` 0E/80W chave-a-chave + suíte |
| r19b2 | Smoke vivo: matriz papel×rota p/ os endpoints tocados (superadmin central clientes; médico ocorrências com seed net-zero; work-items historico com seed net-zero) | evidência versionada |
| r19z | Consolidado + fila R20 + push c/ autorização | — |
