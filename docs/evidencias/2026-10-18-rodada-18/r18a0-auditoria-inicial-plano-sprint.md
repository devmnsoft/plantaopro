# R18a0 — Auditoria inicial e plano (topo da fila R18: F7 expandido + 500 resumo operacional)

**Data:** 2026-10-18 · **Baseline:** build `--no-incremental` **0E/80W** (`r18a0_build.log`) · suíte **1489/1489** (`r18a0_suite.log`, 1m09s) · tree limpa em `73ad4bc` (fim da R17, pushada).

## 1. Perímetro (herdado da fila R18, r17z §5)

1. **F7 (topo):** controllers sem entrada no dicionário do guard ⇒ `CATALOGO_NAO_CONFIGURADO` — r17a0 mediu 3 (`CommandCenter`/`Fechamentos`/`Coordenacao`); sub-item: rota nua `/CentralEscala` 404.
2. **(P2)** `api/central-escala/resumo` 500 — `42703 coluna n.cliente_id não existe` em `OperacaoServices.cs` (`GetResumoAsync`), pré-existente (provado em estado pristino no r17b2).

## 2. Achados medidos

### F1 — O F7 real tem **9** órfãos (não 3); o scan do r17a0 cobriu só a seção Operação
Todos reproduzidos ao vivo **hoje** com o binário da R17 (`73ad4bc`), como **SV tenant-admin** E como **superadmin global** (`ADMINISTRADOR_GLOBAL`, landing `/AdminSaas/Index`). O lookup do dicionário precede o bypass de global-admin (o bypass existe só em `IsModuleEnabled`/`HasPermission`, que o guard chama DEPOIS do `TryGetValue`):

| Controller | Rota | Papel/papel-gate | Superadmin | SV admin |
|---|---|---|---|---|
| `MinhaCentral` | `/MinhaCentral` | `[Authorize]` | CATALOGO_NAO_CONFIGURADO | idem |
| `Notificacoes` | `/Notificacoes/Index` | nenhum (Base) | idem | idem |
| `Ajuda` | `/Ajuda/Index` | `[Authorize]` | idem | idem |
| `SaasDashboard` | `/SaasDashboard/Index` | `ADMINISTRADOR_GLOBAL,ADMINISTRADOR` | idem | idem |
| `Modulos` | `/Modulos/Index` | `GLOBAL,ADMIN,CLIENTE` | idem | idem |
| `Fechamentos` | `/fechamentos*` (+POST acao, CSV) | `[Authorize]` | idem | idem |
| `CommandCenter` | `/CommandCenter` | `Roles.Operacao` | idem | idem |
| `Coordenacao` | `/Coordenacao/Index` (stub) | `Roles.Operacao` | idem | idem |
| `Cobertura` | `/cobertura*` (stub V1420) | `[Authorize]` | idem | idem |

Controle positivo: `/MeuDia` → 200 para SV admin.

### F2 — Superfície atingível (por que isso é P1 na prática)
- **`MinhaCentral`**: logo do sidebar (botão HOME do app, L25), sidebar L32, `Home/Dashboard` (L34 "Ver próximas ações", L107 card), páginas `NotFound`/`Error` ("Ir para Minha Central" — a rota de fuga universal está quebrada), `_NavigationRail`.
- **`Ajuda`**: ícone de ajuda do topbar (`_AppTopbar` L64 — toda página), footer do sidebar, **botão "Acionar suporte" da própria tela AccessDenied**, `_LookupSelect`.
- **`Notificacoes`**: footer do sidebar (seção sempre visível), drawer de notificações, MeuDia L198, Home "Ver todas", quick-link da CentralEscala.
- **`SaasDashboard`/`Modulos`**: seção global-admin do sidebar + botão "Gerenciar módulos" (AdminSaas) + **"Contratos" (Clientes/Index e Details → `Modulos/Tenant`)** — o fluxo de gestão de contratos do superadmin está inteiro atrás de CATALOGO_NAO_CONFIGURADO.
- **`CommandCenter`**: sidebar (canManage/coordenadores) + card "is-platform" do Meu Dia (L92).
- **`Fechamentos`**: sidebar L49. **`Coordenacao`**: sem link (só URL direta). **`Cobertura`**: auto-link interno (`/cobertura/criticos`).

### F3 — Catálogo (modulos_sistema reg_status='A') e camada comercial
- Pais ativos com suítes completas (31 permissões cada em `permissoes`): **`FECHAMENTO`**, **`COBERTURA`**, **`OPERACAO360`**, `SAAS`, `NOTIFICACOES`, `AJUDA` (entre outros). **NÃO existem** `FECHAMENTOS` (plural), `MINHA_CENTRAL`, `COORDENACAO`, `MEU_DIA` (ghosts). `OPERACAO` existe como pai órfão: **0** suítes `OPERACAO.%` (a única linha `OPERACAO_*` é `OPERACAO_ASSISTIDA`, módulo próprio).
- `tenant_modulos`: **0 tenants** contratam `FECHAMENTO`/`COBERTURA`/`OPERACAO`/`OPERACAO360` (nenhum deles vende hoje).
- `saas_modulos` (14 códigos comerciais): `ATENDIMENTO, PLANTOES, ESCALAS, FINANCEIRO, CONVENIOS, CID, PRESCRICOES, RELATORIOS, DASHBOARD_GESTOR, API_PUBLICA, WEBHOOKS, WHITE_LABEL, MARKETPLACE, SUPORTE_PREMIUM` — nenhum dos donos candidatos está comercializável agora (**mesmo estado em que `ESCALAS` estava na R13/R17**: guard alinhado ao catálogo, venda é decisão comercial posterior; provisionamento manual via 1 linha em `tenant_modulos` já validado no r17b2).

### F4 — Sets em código (paridade Web×Api)
- `AJUDA` **já está** em `CommonModules` E `CoreModules` (Web `AccessServices.cs` L166/L246 + espelhos Api) — o padrão já foi estabelecido; só falta a entrada no dicionário.
- `NOTIFICACOES` e `MINHA_CENTRAL` ausentes dos 4 sets (Web Common+Core, `SecurityAccessServices.CommonModules` L162, `SecurityAdministrationServices.CoreModules` L50).
- Mecânica confirmada no código: `PermissionService.HasPermission` dá `true` para qualquer autenticado quando o módulo ∈ `CommonModules` (L119); global-admin `true` p/ tudo (L106); `IsCoreOrCommonModule` pula a live-check do guard (L228-230) — precedente exato do D2/R17 (`MINHA_AGENDA`).

### F5 — Fantasma residual `"FECHAMENTOS"` no sidebar (mesma família R17)
`_AppSidebar.cshtml` L15 (`canOperate` array) e L49 (`Can("FECHAMENTOS")`) usam o plural **ausente do catálogo** — ramo morto para não-global (claim jamais contém) e gate que nunca revela o link. Código real: `FECHAMENTO` (singular).

### F6 — Raiz do 500 (item P2 da fila)
`OperacaoServices.cs:55`: subconsulta `(select count(1) from plantaopro.notificacoes n where ... (@isAdminGlobal or n.cliente_id=@clienteId or n.cliente_id is null) and coalesce(n.lida,false)=false and (n.usuario_id=@userId or n.usuario_id is null))`. Schema real da tabela: **coluna de escopo é `tenant_id`** (uuid, nullable) — **`cliente_id` não existe** (⇒ `42703`). Writer único (`Data.cs:2208 CriarNotificacaoAsync`) **não popula `tenant_id`** (todas as N linhas do banco têm NULL) e o modelo é por-design per-`usuario_id` (unique index `(usuario_id, origem_tipo, origem_id, titulo)`).

## 3. Decisões

### D1 — Cluster workspace comum (todo autenticado): `MinhaCentral`, `Notificacoes`, `Ajuda`
Padrão COMUM-em-código (precedentes CONTA R7-E1, AJUDA/LGPD/TREINAMENTO já nos sets, MINHA_AGENDA R17-D2):
- `["Ajuda"] = "AJUDA"` — sets já existentes; **só a entrada no dicionário**.
- `["Notificacoes"] = "NOTIFICACOES"` + código nos 4 sets (Web Common+Core, Api SecurityAccessServices, Api SecurityAdministrationServices).
- `["MinhaCentral"] = "MINHA_CENTRAL"` (ghost-comum, classe MINHA_AGENDA/CONTA) + 4 sets.
Sem DDL. O papel-gate (ou ausência dele) continua autoridade de audiência; a central/notificações/ajuda são infraestrutura do login, não mercadoria contratada.

### D2 — Cluster administração global SaaS: `SaasDashboard` e `Modulos` → `SAAS`
Pai do catálogo com suíte completa. Global-admin passa pelo bypass (F4); tenant-admin por URL direta recebe `MODULO_NAO_CONTRATADO` honesto (módulo não-vendido hoje, F3) — coerente com a semântica SaaS que a R13/R17 consolidaram. Sem DDL.

### D3 — Cluster domínio operacional
- `["Fechamentos"] = "FECHAMENTO"` (singular canônico; padrão R13-D1/R17-D1 — gêmeo real com suíte + zero contratos ⇒ denegação honesta até a venda; sem DDL). **Acompanhamento obrigatório**: (a) trocar `"FECHAMENTOS"` → `"FECHAMENTO"` nas 2 referências do sidebar (F5); (b) override `["Fechamentos/Acao"]="CONFIRMAR"` — hoje o mapeamento genérico manda o POST de escrita (aprovar/devolver/rejeitar/gerar-financeiro) para VER (padrão R5-A2).
- `["Cobertura"] = "COBERTURA"` (gêmeo canônico 1:1 no catálogo, mesmo formato ESCALAS; stub V1420 visível para global-admin, denegado honesto para os demais até contrato).
- `["CommandCenter"] = "OPERACAO360"` — único módulo operacional do catálogo com suíte completa (`OPERACAO` pai tem suíte vazia, F3; hipótese PLANTOES/ESCALAS do r17a0 rejeitada: o endpoint é `api/manager-command-center`, agregado da operação, não de plantões nem de escalas em si).

### D4 — `Coordenacao` → pin CONTRATADO-SOMENTE (mesmo desenho do D3-R17/MEDICO_AREA)
Sem gêmeo no catálogo, página stub, audiência restrita por `Roles.Operacao` no papel-gate. `["Coordenacao"] = "COORDENACAO"` com comentário de pin: a denegação deixa de ser "catálogo mal configurado" (bug, loga ERROR) e passa a ser "módulo não contratado" (estado canônico, loga WARN) até decisão de produto/Fase2 construir o domínio. Sem DDL.

### D5 — Fix do 500: coluna inexistente → semântica real da tabela
Substituir `(@isAdminGlobal or n.cliente_id=@clienteId or n.cliente_id is null)` por `(@isAdminGlobal or n.tenant_id=@clienteId or n.tenant_id is null)` (L55 de `OperacaoServices.cs`), com comentário de casa documentando: (i) `cliente_id` nunca existiu em `notificacoes` (raiz do 42703); (ii) o writer atual não popula `tenant_id` (tolerância `is null` preserva o comportamento de broadcast); (iii) se o writer passar a populá-la, deve usar `tenant_id` do contexto do usuário. Comportamento equivalente ao estado de dados atual; corrigido o erro de schema. Fato anti-regressão em b2.

### D6 — Sub-item rota nua: `CentralEscala`
`[Route("CentralEscala")]` na classe + `[HttpGet("")]` em `Index` (restam as demais actions sem template ⇒ `{controller}/{action}` preserva `RedirectToAction`); `/CentralEscala` volta a resolver. Alinhado ao estilo da casa (todos os vizinhos usam attribute-route).

## 4. Plano

| Slice | Conteúdo |
|---|---|
| **r18b1** | Dicionário do guard: 9 entradas (D1–D4) c/ comentários de casa; override `Fechamentos/Acao→CONFIRMAR`; sidebar `FECHAMENTOS→FECHAMENTO` ×2; 4 sets Web+Api c/ `NOTIFICACOES`+`MINHA_CENTRAL`; rota `CentralEscala` (D6); fatos de fonte (resolução dos 9, membership dos sets, ausências de literais no sidebar, shape da override) |
| **r18b2** | Fix D5 (`OperacaoServices.cs:55`) + fato anti-regressão (query do resumo roda contra o schema real) |
| **r18b3** | Smoke vivo pós-build: matriz papel × rota (superadmin, SV admin, médico) antes/depois nos 9 órfãos + `/api/central-escala/resumo` 200 + rota nua 200; **zero mudança de banco nesta rodada** (tudo em código) |
| **r18z** | Consolidado + fila R19 (herda r17z §5 removidos #1/#2; resto mantém ordem) + push c/ autorização |

## 5. Riscos e mitigações
- D1 alargue: `MinhaCentral`/`Notificacoes` abrem para QUALQUER papel autenticado (inclusive MEDICO/RECEPCÃO) — é a semântica desejada (infraestrutura do login; hoje o link está quebrado para todos, então o delta é "de quebrado para funcional"); fato comportamental pinando `Ajuda`/`MinhaCentral` para médico e `AccessDenied→Ajuda` para um papel operatório.
- D2 expõe `Modulos/Tenant` (contratos do cliente) a tenant-admin com `SAAS` contratado por URL direta — o papel-gate do controller (GLOBAL/ADMIN/CLIENTE) já restringe mais que o módulo; sem novo alargue prático.
- D3(a): trocar o literal do sidebar muda a exibição do link Fechamentos para tenants com `FECHAMENTO` contratado (hoje: nenhum — F3) ⇒ delta vazio no estado atual, correto no futuro.
- D5: se existir outro consumer esperando contagem tenant-wide (não per-user) em `NotificacoesNaoLidas`, o valor pode mudar de 500→número; o consumo conhecido é o sino/bell (per-user) — sem efeito colateral esperado.

## 6. Fila R19 (previa, pendente do r18z)
#3–#15 do r17z §5 mantêm ordem (scanner F-R15-A, whitelist JS-driven, drill-downs r10b3 §6, menu Bi, F-R14-D/B, FecharCaixa, pedidoItemId, fantasmas residuais P3 + sub-item MinhaAgenda→MedicoArea, fuso Meu Dia, tile B18, especialidade texto→id, pg_dump 18). Sem novos itens nesta auditoria.
