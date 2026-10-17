# R17z — Relatório final consolidado · Rodada 17 (fantasmas do catálogo guard)

**Data:** 2026-10-17 · **Status:** rodada encerrada, pronta para push (aguardando autorização)

## 1. Resumo executivo

A rodada resolveu os **3 fantasmas atingíveis** do dicionário do guard SaaS com decisões canônicas próprias, evidência medida (baseline → fix → smoke vivo → rollback net-zero) e 13 fatos anti-regressão versionados:

| Fantasma | Decisão | Implementação (r17b1) | Prova viva (r17b2) |
|---|---|---|---|
| `CENTRAL_ESCALA` | **D1**: remapear `CentralEscala` para o dono canônico **`ESCALAS`** (gêmeo real do domínio de cobertura; padrão R13-D1/R5-E13) | guard `["CentralEscala"]="ESCALAS"`; extinto elemento morto `"CENTRAL_ESCALA"` do array `canOperate` no sidebar; link "Cobertura" atrás de `Can("ESCALAS")` | reprovação pré-grant nomeia `module=ESCALAS` (fantasma some de toda superfície); grant reversível `HOMOLOG_R17` ⇒ 200 + links no sidebar; rollback net-zero via `modulos_efetivos()` |
| `MINHA_AGENDA` | **D2**: módulo **COMUM/CORE** em código (audiência restrita pelo gate de papel `[Authorize(Roles=MEDICO)]`; precedentes CONTA R7-E1 / ONBOARDING R5-D9) | `"MINHA_AGENDA"` nos 4 sets (Web `CommonModules`+`CoreModules`; Api espelho) com comentário de casa | médico sem contratação: 3 cards do Meu Dia em **200** (antes: `MODULO_NAO_CONTRATADO` — reproduzido organicamente no binário pristino); papel continua segunda linha (`NaoMedico` 302 honesto) |
| `MEDICO_AREA` | **D3**: **contratado-somente** por desenho (sem gêmeo no catálogo; stubs Fase2; contrato wpa1 intacto) | pin comentado no entry do guard + fato de pin | `/MedicoArea/Index` negado com motivo canônico `MODULO_NAO_CONTRATADO module=MEDICO_AREA`; landing do médico segue Meu Dia (wpa1) |

**Gate final:** build `--no-incremental` **0 erros / 80 avisos** com chaves idênticas chave-a-chave ao build pristine `505d18a` (0 novos / 0 sumidos); suíte full **1489/1489** (baseline 1476 + 13 novos).

## 2. Mapa da rodada

| Slice | Commit | Artefato |
|---|---|---|
| r17a0 auditoria/plano (F1–F8, D1–D5) | `978193e` | `r17a0-auditoria-inicial-plano-sprint.md` + logs baseline |
| r17b1 código + fatos | `34f2a42` | 5 arquivos (guard, sidebar, sets Web×2/Api×2) + `R17CatalogGhostsTests.cs` (13 fatos: fonte + web-bff) |
| r17b2 smoke vivo | `f84d6a3` | `r17b2-smoke-vivo-guard-ghosts.md` (S1/S2/S3 + SQL net-zero) |
| r17z consolidado | (este commit) | este documento + fila R18 final |

## 3. Classificações e lições versionadas desta rodada

1. **500 em `api/central-escala/resumo` — NOVO, raiz encontrada.** O log da Api durante o smoke capturou a exceção: `Npgsql.PostgresException 42703: coluna n.cliente_id não existe (POSITION: 2207)` em `OperacaoService.GetResumoAsync` (`PlantaoPro.Api/OperacaoServices.cs:38`). **Pré-existente** (reproduzido em estado pristino sem nenhum grant da rodada, com o banco net-zero). A Web já degrada com empty-state honesto (view 200, mensagem clara). Impacto: quebra o resumo da Cobertura para **todo** tenant que chegue à página — entra na fila R18 como P2 logo atrás do F7 (§5).
2. **Binário stale no smoke (processo).** O primeiro ciclo do b2 rodou sobre o build pristine do stash — corrigido com rebuild+restart e convertido em evidência ("antes do fix" orgânico). Regra reforçada: entre `dotnet build` e `dotnet run --no-build` não pode haver outro build (mesma família do gotcha "DLL antiga").
3. **Snapshot de avisos lossy.** `r16b1_warn_base.txt` (71 chaves) perdia 9 avisos xUnit por quebra de linha no log; o critério válido é comparativo chave-a-chave contra build pristine da mesma metodologia — feito, vazio. Sem ação além do registro.
4. **Flake de paralelismo** (`Administrativo360OrganizacaoTests.Resumo_…`, Expected 6/Actual 5): isolate 5× limpo ⇒ família conhecida, pré-existente, não bloqueou.
5. **Semântica confirmada ao vivo:** porta 1 (módulo) passa com live-check após grant, porta 2 (permissão nas claims snapshot) exige re-login — comportamento canônico das duas linhas de defesa, agora com prova viva.

## 4. Escopo intencionalmente NÃO tocado

- Matrizes de exibição (Api `PermissoesController.Matriz` / Web `ProfessionalSaasControllers`) — D1a conservadora: sem consumer ativo em Web.
- Códigos fantasma residuais P3 (`HOSPITAL_AREA`, `MARKETPLACE`, `PROPOSTAS`, `PARCEIRO`, `OBSERVABILIDADE_GLOBAL`) — fila, sem superfície atingível nesta rodada.
- Contratos/stubs de `MedicoArea` (Fase2) e o resíduo de cross-links — fila R18.

## 5. Fila R18 (final, aprovada no r17z)

1. **(P2, topo, herdado r17a0/F7)** Controllers sem entrada no dicionário do guard ⇒ `CATALOGO_NAO_CONFIGURADO`: `CommandCenter`, `Fechamentos` (dupla ausência: também sem módulo no catálogo), `Coordenacao` — dono canônico próprio + reprodução ao vivo antes do mapeamento. *Sub-item (capturado no r17b2): normalizar rota nua `/CentralEscala` (404) enquanto a seção de Cobertura/Operação estiver no escopo.*
2. **(P2, NOVO r17b2)** `api/central-escala/resumo` 500: `42703 coluna n.cliente_id não existe` em `OperacaoServices.cs:38` (`GetResumoAsync`) — corrigir a query (alias/coluna) com fato anti-regressão; quebra o resumo da Cobertura para todo tenant atingível.
3. (herdado r16z #2) Estender scanner F-R15-A: paths explícitos `Adm360*WebController*` + camada DTO-API×ViewModel-Web (formato F9d).
4. (r16z #3) Whitelist JS-driven 6 actions (OrcamentoRejeitar/Cancelar/CancelarReserva, ValeCancelar, AtivarDepartamento/Cargo).
5. (r16z #4) Drill-downs r10b3 §6.
6. (r16z #5) Menu Bi/DashboardsPremium/FaturamentoSaas (decisão de produto).
7. (r16z #6) F-R14-D (premissa refinada).
8. (r16z #7) F-R14-B.
9. (r16z #8) F14 justificativa FecharCaixa.
10. (r16z #9) F-R15-C `pedidoItemId`.
11. (r16z #10) Fantasmas residuais P3: `HOSPITAL_AREA`, `MARKETPLACE`, `PROPOSTAS`, `PARCEIRO`, `OBSERVABILIDADE_GLOBAL`. *Sub-item (D3): cross-links `MinhaAgenda→MedicoArea` — contratar em tenants piloto vs absorver no comum (decisão wpa1).*
12. (r16z #11) Meu Dia fuso (produto).
13. (r16z #12) Tile B18 spec.
14. (r16z #13) Especialidade texto→id.
15. (r16z #14) pg_dump 18 (P5).

## 6. Estado para próxima rodada

- `origin/main` ainda aponta para `505d18a` (fim da R16); local `main` carrega `978193e` → `34f2a42` → `f84d6a3` → r17z. Push único ao encerrar, com autorização.
- Banco em estado net-zero (grant `HOMOLOG_R17` removido; verificado na função canônica). Apps parados. Working tree limpo após este commit.
