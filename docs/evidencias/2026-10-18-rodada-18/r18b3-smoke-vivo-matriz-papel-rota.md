# R18-b3 — Smoke vivo: matriz papel × rota nos 9 órfãos + resumo operacional + rota nua

Data: 2026-10-18 · Build: Release `--no-incremental` (gate r18b2, 0E/80W chaves idênticas ao pristino) · Suíte 1504/1504
Apps: Api :51976 + Web :52976 (`dotnet run --no-build`, binário pós-b2). Sessão in-memory ⇒ re-login por papel.
Apps parados ao fim (portas 51976/52976 verificadas livres). Zero mudança de banco na rodada.

## Critério de passagem

1. Nenhum dos 9 controllers órfãos (r18a0 F7) pode responder `CATALOGO_NAO_CONFIGURADO` para papel com módulo resolvido — o guard deve achar o dono canônico no dicionário.
2. Denegações sem contratação devem ser **honestas**: `module=<CÓDIGO_CANÔNICO>&reason=MODULO_NAO_CONTRATADO` (nem 404, nem "catálogo não configurado").
3. `GET /api/central-escala/resumo` (raiz do P2/r17b2) deve responder 200 ⇒ página `/CentralEscala` renderiza dados sem banner de falha.
4. Rota nua `/CentralEscala` resolve (era 404); URL legado `/CentralEscala/Index` continua resolvendo (D6).

## Matriz executada

### Superadmin (`superadmin@plantaopro.local`, ADMINISTRADOR_GLOBAL — bypass global)

| URL | Resultado | Status |
|---|---|---|
| `/MinhaCentral` | 200 · h1 "Minha Central" | OK |
| `/Ajuda/Index` | 200 · h1 "Central de Ajuda" | OK |
| `/Notificacoes/Index` | 200 · h1 "Central de Notificações" | OK |
| `/SaasDashboard/Index` | 200 · h1 "Dashboard SaaS" | OK |
| `/Modulos/Index` | 200 · h1 "Catálogo de módulos" | OK |
| `/fechamentos` | 200 · h1 "Fechamento Operacional" | OK |
| `/CommandCenter` | 200 · h1 "Command Center" | OK |
| `/Coordenacao/Index` | 200 · h1 "Central de Escala" (stub) | OK |
| `/cobertura` | 200 · h1 "Central de Cobertura" (stub) | OK |
| `/CentralEscala` | 200 · h1 "Central de Operação", tiles do resumo renderizados ("PLANTÕES HOJE 0 programados no período"), **sem banner de falha** | OK — **valida D6 (rota nua) + b2 (resumo sem 500) ao vivo** |

**10/10 OK.** Antes da rodada: todos os 9 órfãos caiam em `CATALOGO_NAO_CONFIGURADO`; `/CentralEscala` nua era 404; `/CentralEscala/Index` 500 no resumo.

### SV tenant-admin (`admin.sistemavertical@plantaopro.local`)

| URL | Resultado | Status |
|---|---|---|
| `/MinhaCentral` | 200 · h1 "Minha Central" | OK (comum-em-código D1) |
| `/Ajuda/Index` | 200 · h1 "Central de Ajuda" | OK (comum D1) |
| `/Notificacoes/Index` | 200 · h1 "Central de Notificações" | OK (comum D1) |
| `/CentralEscala` | AccessDenied `module=ESCALAS&reason=MODULO_NAO_CONTRATADO` | OK — denegação honesta (SV não contrata ESCALAS; r18a0 F3: 0 tenants contratam os donos operacionais) |
| `/fechamentos` | AccessDenied `module=FECHAMENTO&reason=MODULO_NAO_CONTRATADO` | OK (código canônico singular, D3) |
| `/CommandCenter` | AccessDenied `module=OPERACAO360&reason=MODULO_NAO_CONTRATADO` | OK (D3) |
| `/cobertura` | AccessDenied `module=COBERTURA&reason=MODULO_NAO_CONTRATADO` | OK (D3) |
| `/Coordenacao/Index` | AccessDenied `module=COORDENACAO&reason=MODULO_NAO_CONTRATADO` | OK (pin contratado-somente D4) |
| `/SaasDashboard/Index` | AccessDenied `module=SAAS&reason=MODULO_NAO_CONTRATADO` | OK (D2 — antes era CATALOGO_NAO_CONFIGURADO) |

**9/9 OK.** `CATALOGO_NAO_CONFIGURADO`: **zero ocorrências**. Nota: o `retorno=` das negações aponta para URLs canônicas validas (ex.: `%2FCentralEscala%2FIndex` continua servida pela ação legado — ver abaixo).

### Médico (`medico@plantaopro.local`)

| URL | Resultado | Status |
|---|---|---|
| `/MinhaCentral` | 200 · h1 "Minha Central" | OK (comum D1) |
| `/Ajuda/Index` | 200 · h1 "Central de Ajuda" | OK (comum D1) |
| `/Notificacoes/Index` | 200 · h1 "Central de Notificações" | OK (comum D1) |
| `/MeuDia` | 200 · h1 "Meu Dia" (controle) | OK |
| `/fechamentos` | AccessDenied `module=FECHAMENTO&reason=MODULO_NAO_CONTRATADO` | OK — honesta (médico não opera fechamento) |
| `/CentralEscala/Index` | AccessDenied `module=ESCALAS&reason=PERMISSAO_NEGADA` — **resolveu, não 404** | OK — valida a ação legado `IndexLegacy` (D6) |

**6/6 OK.**

## Verificação cruzada do resumo (P2)

- O 500 era `Npgsql 42703 coluna n.cliente_id` em `GET /api/central-escala/resumo` (reproduzido r17b2).
- Ao vivo agora: superadmin abre `/CentralEscala` nua → BFF chama `api/central-escala/resumo` → página renderiza tiles com dados reais, zero banner de erro. O polling do sino (`/api/notificacoes/nao-lidas`) também respondeu 200 em todas as requisições logadas (visível no log do Api após stop).
- Cobertura anti-regressão em código: `R18OperacaoResumoQueryTests` (fonte sem `n.cliente_id` + subquery extraída do arquivo executa contra o schema real).

## Achados colaterais do smoke (F7b/F7c) — corrigidos em r18b4

Ao ler os logs do Api após o stop, dois endpoints 500 ficaram visíveis — ambos sempre existiram, mas só agora o tráfego chegou neles (antes o guard barrava com `CATALOGO_NAO_CONFIGURADO` ou o BFF engolia o erro):

- **F7b — `GET /api/minha-central` 500** para todo papel não-global. Raiz: `WorkItemDto` (record) declarava `DateTimeOffset` onde o reader do Npgsql devolve `DateTime` para timestamptz; Dapper não converte e a materialização falha. O superadmin só passava por short-circuit `GlobalView` (sem `ListAsync`) e a suíte verde porque o banco de teste tinha zero work_items — o deserializer é gerado **antes** da primeira linha (reproduzido: 0 linhas e 500).
- **F7c — `GET /api/modulos` 500** para todo papel. Raiz: SRF `modulos_efetivos()` dentro de `any()` em WHERE — PG18 rejeita (`0A000`, posição 489, reproduzida em psql). O BFF renderizava o catálogo vazio sem banner. O `EXISTS` no lugar do `any()` resolvia o parse mas custava ~4min23s (SRF por módulo × tenant; acima do `Command Timeout=60s`). Forma final: única passagem por tenant (`cross join lateral`) com `group by codigo` — `count distinct` equivalente, validado 12/12 contra a versão lenta e ~180ms no banco de teste.

Fatos anti-regressão: `R18MinhaCentralModulosQueryTests` (fonte + execução da subquery extraída do arquivo contra o schema real). Commit `r18b4`.

## Artefatos da rodada (todos versionados)

- `r18a0-auditoria-inicial-plano-sprint.md` — achados F1–F6, decisões D1–D6 (commit `db54e99`)
- **r18b1** — guard: 9 entradas + override `Fechamentos/Acao→CONFIRMAR` + sets ×4 arquivos + fantasma `FECHAMENTOS` extinto do sidebar + rota nua `/CentralEscala` c/ URL legado preservada; 13 fatos novos (`R18GuardOrphansTests`). Gate: build 0E/80W chaves idênticas; suíte 1502/1502. Flakes classificados p/ 5× isolado: `SaasGovernancaB6Rodada4Tests` e `ProvisionamentoB5Tests` (família 40P01, só em corrida paralela). Commit `9a6aebe`.
- **r18b2** — fix 500: scoping `n.cliente_id`→`n.tenant_id` (coluna real) c/ tolerância broadcast preservada; 2 fatos novos (`R18OperacaoResumoQueryTests`). Gate: 0E/80W chaves idênticas; suíte 1504/1504. Commit `5b098ea`.
- **r18b3** — este documento.
- **r18b4** — fix F7b/F7c (datas do WorkItemDto + consulta do catálogo em passe única) com 4 fatos novos.

## Conclusão

Rodada 18 completa: F7 expandido (9 órfãos) eliminado com donos canônicos medidos no catálogo; P2 (500 resumo) corrigido e validado ao vivo; rota nua restaurada com legado preservado; zero mudança de banco. Fila herdada → r18z.
