# Rodada 18 — Relatório final consolidado

Data: 2026-10-18 · Base `origin/main` = `73ad4bc` (fim R17) · Encerra em `r18z` (este documento)

## 1. Resumo executivo

Fechados os dois itens P2 do topo da fila R18 (aprovada no r17z):

- **F7 expandido** — o auditoria r18a0 mediu que o problema dos 3 controllers órfãos era na verdade **9**: todos os controllers sem entrada no dicionário do guard respondiam `CATALOGO_NAO_CONFIGURADO` (reproduzido ao vivo para SV admin e superadmin). Mapeados em r18b1 para donos canônicos **medidos no catálogo**, com paridade nos 4 sets de permissões Web+Api, override de escrita (`Fechamentos/Acao→CONFIRMAR`) e extinção do fantasma `FECHAMENTOS` do sidebar.
- **P2 500 resumo** — `api/central-escala/resumo` quebrava para todo tenant atingível por uma coluna inexistente (`n.cliente_id`, 42703). Corrigido em r18b2 com o scoping real (`tenant_id`/`usuario_id`, tolerância broadcast preservada) e validado ao vivo na página `/CentralEscala`.

O smoke vivo (r18b3) expôs **dois 500 pré-existentes novos** que só agora receberam tráfego (F7b `/api/minha-central`, F7c `/api/modulos`) — ambos corrigidos em r18b4 com fatos anti-regressão. Zero mudança de banco na rodada (tudo em código). Suíte final **1508/1508** (1502 antes da b1 → +13 +2 +4 fatos novos); build 0E/80W com chaves idênticas ao pristino em todos os gates.

## 2. Mapa da rodada

| Fatia | Commit | Conteúdo |
|---|---|---|
| r18a0 | `db54e99` | Auditoria: F7 expandido 3→9 órfãos, raiz do 42703, donos canônicos medidos, plano b1–b3 |
| r18b1 | `9a6aebe` | Guard: 9 entradas + override + sets ×4 arquivos + fantasma extinto + rota nua `/CentralEscala` c/ URL legado preservada; 13 fatos (`R18GuardOrphansTests`). Gate 0E/80W; suíte 1502/1502 |
| r18b2 | `5b098ea` | Fix 500 resumo (`n.cliente_id`→`n.tenant_id`) + comentário D5; 2 fatos (`R18OperacaoResumoQueryTests`). Gate 0E/80W; suíte 1504/1504 |
| r18b3 | (doc em `920655b`) | Smoke vivo matriz papel × rota: superadmin 10/10 · SV 9/9 (denegações honestas) · médico 6/6 |
| r18b4 | `920655b` | F7b (WorkItemDto DateTime) + F7c (consulta do catálogo em passe única); 4 fatos (`R18MinhaCentralModulosQueryTests`). Gate 0E/80W; suíte 1508/1508 |

## 3. Decisões homologadas ao vivo (D1–D6 do r18a0)

| Decisão | Resultado vivo |
|---|---|
| D1 comuns em código (`MinhaCentral`→MINHA_CENTRAL, `Notificacoes`→NOTIFICACOES + 4 sets) | 200 nos três papéis |
| D1 `Ajuda`→AJUDA (só faltava a entrada) | 200 nos três papéis |
| D2 `SaasDashboard`/`Modulos`→SAAS | superadmin 200 · SV `MODULO_NAO_CONTRATADO module=SAAS` (antes CATALOGO_NAO_CONFIGURADO) |
| D3 `Fechamentos`→FECHAMENTO (singular canônico; fantasma plural extinto) | superadmin 200 · SV/médico NAO_CONTRATADO honesto |
| D3 `Cobertura`→COBERTURA · `CommandCenter`→OPERACAO360 | superadmin 200 · SV NAO_CONTRATADO honesto |
| D4 `Coordenacao`→COORDENACAO pin contratado-somente | superadmin 200 (stub) · SV NAO_CONTRATADO honesto |
| D5 fix 500 resumo (`tenant_id`) | `/CentralEscala` nua renderiza tiles com dados reais, sem banner |
| D6 rota nua `/CentralEscala` (era 404) + URL legado preservada | nua 200 · legado resolve (negado por permissão quando não contratado, nunca 404) |

**`CATALOGO_NAO_CONFIGURADO`: zero ocorrências em todo o smoke (25 probes).**

## 4. Achados F7b/F7c (nascidos no smoke, fechados em r18b4)

- **F7b — `GET /api/minha-central` 500** para todo papel não-global. `WorkItemDto` (record) declarava `DateTimeOffset`; o reader do Npgsql devolve `DateTime` para timestamptz e o Dapper não converte. O deserializer é gerado **antes da primeira linha** — superadmin escapava só pelo short-circuit GlobalView e a suíte por ter zero work_items. Fix: datas como `DateTime` (padrão de todos os DTOs Dapper da casa) + ajuste `.UtcDateTime.Date`→`.Date` no serviço.
- **F7c — `GET /api/modulos` 500** para todo papel. SRF `modulos_efetivos()` dentro de `any()` em WHERE é rejeitada pelo PG18 (`0A000`, posição 489; reproduzida em psql). O BFF mostrava catálogo vazio sem banner — o endpoint nunca havia completado em nenhum ambiente. `EXISTS` resolvia o parse mas custava ~4min23s (acima do `Command Timeout=60s`); forma final: passe única por tenant (`cross join lateral` + `group by codigo`) — count distinct equivalente, **validado 12/12 contra a versão lenta, ~180ms** (1142 módulos × 5196 tenants).

## 5. Classificações da casa aplicadas

- **Flakes (família 40P01, deadlock de concorrência no banco de teste em corrida paralela)**: `SaasGovernancaB6Rodada4Tests.G1a_...` e `ProvisionamentoB5Tests.P2_EmailDuplicado_400_SemResiduo` — cada classe isolada **5× limpa** ⇒ flake classificado, não regressão. Snapshot final de suíte all-green capturado (`r18b4_suite.log`).
- **Pré-existência**: F7b/F7c existiam antes da rodada (reprodução lógica: o guard barrava o tráfego antes; nenhuma query do b1–b3 as tocava). O 500 do resumo (P2) já estava classificado como pré-existente no r17z.

## 6. Escopo intencionalmente NÃO tocado

- Contratos reais dos donos operacionais continuam zerados (0 tenants contratam FECHAMENTO/COBERTURA/OPERACAO360 etc.) — decisão de produto/comercial, fora do escopo de código.
- `MEDICO_AREA` ainda cai para Meu Dia como landing (herdado da fila, item de decisão wpa1).
- Stubs `Coordenacao`/`Cobertura` permanecem placeholders (suíte vazia de funcionalidade própria).
- Schema/DDL: **zero alterações** (mesmo estado dos dados dos bancos operacional e teste).

## 7. Fila R19 (consolidada)

Herdada da r17z §5 itens 3–15 (itens 1–2 consumidos nesta rodada) + 2 novos desta rodada:

1. (r16z #2) Estender scanner F-R15-A: paths explícitos `Adm360*WebController*` + camada DTO-API×ViewModel-Web (formato F9d).
2. (r16z #3) Whitelist JS-driven 6 actions (OrcamentoRejeitar/Cancelar/CancelarReserva, ValeCancelar, AtivarDepartamento/Cargo).
3. (r16z #4) Drill-downs r10b3 §6.
4. (r16z #5) Menu Bi/DashboardsPremium/FaturamentoSaas (decisão de produto).
5. (r16z #6) F-R14-D (premissa refinada).
6. (r16z #7) F-R14-B.
7. (r16z #8) F14 justificativa FecharCaixa.
8. (r16z #9) F-R15-C `pedidoItemId`.
9. (r16z #10) Fantasmas residuais P3: `HOSPITAL_AREA`, `MARKETPLACE`, `PROPOSTAS`, `PARCEIRO`, `OBSERVABILIDADE_GLOBAL`. *Sub-item (D3): cross-links `MinhaAgenda→MedicoArea` — contratar em tenants piloto vs absorver no comum (decisão wpa1).*
10. (r16z #11) Meu Dia fuso (produto).
11. (r16z #12) Tile B18 spec.
12. (r16z #13) Especialidade texto→id.
13. (r16z #14) pg_dump 18 (P5).
14. **(NOVO r18b4, P4) Auditoria da família F7b**: varrer o Api em busca de outros records ligados ao Dapper com parâmetro `DateTimeOffset` sobre colunas timestamptz (mesma classe latente do WorkItemDto; WorkItemDto foi o único atingido pelo smoke) — fato-escâner + correções pontuais.
15. **(NOVO r18b4, P4) Fato de guarda global**: asserção de fonte de que nenhum SQL do Api mantém `any(plantaopro.<srf>(` em WHERE (a ocorrência F7c foi única; o guard impede regressão silenciosa — o BFF engole o 500 e mostra vazio).

## 8. Estado para encerramento

- Local `main` = `920655b` + commit deste r18z; `origin/main` = `73ad4bc` ⇒ **4 commits + este para push único ao fim, com autorização**.
- Apps parados (portas 51976/52976 livres). Working tree limpo após este commit. Banco net-zero.
- Logs de gate: `%TEMP%\opencode\r18*_build.log` / `r18*_suite.log` / `r18_iso*.log` (referenciados pelas fatias acima).
