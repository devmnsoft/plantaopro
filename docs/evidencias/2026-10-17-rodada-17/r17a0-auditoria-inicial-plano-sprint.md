# R17 — r17a0 · Auditoria inicial e plano (fantasmas atingíveis do catálogo guard)

- **Data**: 2026-10-17 · **Rodada**: 17 · **Fatia**: r17a0 (auditoria/baseline; read-only em código + banco)
- **Topo da fila R17** (r16z §dívidas, item 1, herdado de R13 §D4/r13a0 §1.3): decidir o destino canônico de cada fantasma atingível do catálogo guard — `CENTRAL_ESCALA` (`CentralEscala`, checar gêmeo `ESCALAS`), `MINHA_AGENDA` (`MinhaAgenda`), `MEDICO_AREA` (`MedicoArea`) — com evidência de alcance medida.
- **Baseline (gate)**: build `--no-incremental` Release **0 erros / 80 avisos** (idêntico ao baseline R16); suíte full **1476/1476 aprovadas** (1 m 1 s). Logs: `%TEMP%\opencode\r17a0_build.log`, `%TEMP%\opencode\r17a0_suite.log`. Working tree limpo sobre `505d18a`.

## 1. Mecânica de decisão (recapitulação canônica + fonte única de claims)

Fluxo do guard (`SaasRouteGuardFilter.GateAsync`, invariante desde R6-BlocoA — ver r13a0 §1.2):

1. Controller fora do dicionário `ControllerModules` → `CATALOGO_NAO_CONFIGURADO` (classe de defeito R12).
2. Global admin → bypass total (permissions e módulos via claim `*`).
3. Página HTML autenticada + sessão `v2149` não-global em módulo não-core/comum → **live-check** `effectiveModules.GetModulesAsync()` (`api/auth/effective-modules`, authoritative quando OK) + claims de login como degradação.
4. Libera só se `permissions.HasPermission(modulo, acao)` **E** `modules.IsModuleEnabled(modulo)`. Motivos honestos: `PERMISSAO_NEGADA` vs `MODULO_NAO_CONTRATADO` vs `VERIFICACAO_INDISPONIVEL` (live falhou).

**Cadeia de emissão de claims no login** (medida em `PlantaoPro.Api\Data.cs` L428-430, L513-547):

- `Permissions` = grants de perfil (`usuarios_perfis`→`perfis`→`perfil_permissoes`→`permissoes`) ∪ `usuario_permissoes_especiais` − negações (CTE `LoadPermissionsAsync`). A `Matriz` papel→permissões de `PermissoesController` (Api) é **apenas exibição** (endpoint `/api/permissoes/matriz`); nada emite permissão por papel automaticamente.
- `Modules` = `plantaopro.modulos_efetivos(@tenantId)` (`LoadModulesAsync`), função canônica v2339: contratação direta + herança de pacote + overrides per-capacidade.

## 2. Achados (todos medidos hoje contra `plantaopro` + fonte)

### F1 — Os três códigos não existem em NENHUMA camada do catálogo (nem em camadas comerciais)

`select ... from modulos_sistema where codigo in (...)` → ausentes `CENTRAL_ESCALA`, `MINHA_AGENDA`, `MEDICO_AREA`. Gêmeos vizinhos que **existem**: `ESCALAS` (ordem 100, categoria Operacao, suite completa), `PLANTOES` (90), `AGENDA` (80), `CONVITES` (70), `PAGAMENTOS` (100). Complementar:

- `permissoes`: **0 linhas** com prefixo `CENTRAL_ESCALA%`, `MINHA_AGENDA%`, `MEDICO_AREA%` (idem `HOSPITAL_AREA/MARKETPLACE/PROPOSTAS/PARCEIRO` — fantasmas residuais P3 da fila, fora do perímetro desta rodada). Não há, portanto, grants dormentes dos três — diferente de `ESCALAS`/`FATURAMENTO_SAAS` (R13).
- `tenant_modulos`: **0 contratações** (qualquer tenant) pelos três códigos (via `codigo` ou `codigo_modulo`).
- `saas_modulos` (catálogo comercial): sem `CENTRAL_ESCALA`/`MINHA_AGENDA`/`MEDICO_AREA` — nada é comercializável por esses códigos hoje.

### F2 — `modulos_efetivos()` faz INNER JOIN com `modulos_sistema` nas quatro ramificações ⇒ código fantasma jamais gera claim de módulo

Corpo da função (`pg_get_functiondef`, v2339): todas as ramas — (a) contrato direto por `modulo_id`, (b1) herança por pacote por `modulo_id`, (a′) contrato direto por `codigo_modulo`, (b2) herança por `codigo_modulo` — exigem `join modulos_sistema ms on ... reg_status='A' and upper(status)='ATIVO'`. Consequência medida:

- Um eventual `tenant_modulos` com `codigo='MINHA_AGENDA'` e sem linha de `modulos_sistema` **não entraria** no conjunto efetivo (join descartaria).
- Logo, para qualquer usuário não-global, `IsModuleEnabled(CENTRAL_ESCALA|MINHA_AGENDA|MEDICO_AREA)` é **sempre false** — tanto pelo live-check (authoritative) quanto pelos claims de login (mesma fonte). Os três controllers são **permanente-inatingíveis para não-globais**, com motivo honesto (`MODULO_NAO_CONTRATADO`; `VERIFICACAO_INDISPONIVEL` se a API cair durante o check).
- Global admin: 200 em tudo via bypass (claim `*`).

### F3 — Estado medido do tenant SV (fonte de smoke) e papéis envolvidos

- `modulos_efetivos('8b0c8e74-…-ca301')` = **18 códigos**: ADM360, AGENDAMENTOS, CID, CLINICA_DASHBOARD, CLINICA_FINANCEIRO, CONSULTAS, CONVENIOS, FINANCEIRO, PACIENTES, PAINEL_CHAMADA, PENDENCIAS_CLINICAS, PLANOS_SAUDE, PRESCRICOES, RELATORIOS, SAUDE360, TRIAGEM, UNIDADES, USUARIOS. **Sem** `ESCALAS`, `PLANTOES`, `AGENDA`, `CONVITES`, `PAGAMENTOS` — os próprios gêmeos existentes estão **descontratados** no SV.
- Grants efetivos do admin SV (`LoadPermissionsAsync` espelhado): **307 códigos**, incluindo suítes completas dormentes de `ESCALAS.*` e `PLANTOES.*` (mesmo formato dos grants dormentes de `FATURAMENTO_SAAS` que a R13-D3 reativou) e **nenhum** prefixo fantasma.
- Usuários `MEDICO` no banco: 4 linhas ativas, dentre elas `medico@plantaopro.local` (user `8b0c8e74-…-ca012`) — candidato à homologação viva (r17b2), independente de qual seja o estado contratual de seu tenant.
- Papéis da seção operacional do sidebar (`RolesConstants`): `Operacao` etc.; landing por papel (`AccountController`, método `RedirectToActionByPerfil`, prioridade L240-258): `COORDENADOR`/`COORDENACAO` → `CentralEscala/Index`; `MEDICO` → `MedicoArea/Index` — ambos protegidos pelo **fallback resiliente** L267-276 (módulo desabilitado → `MeuDia`).

### F4 — Superfícies atingíveis de `CENTRAL_ESCALA` (`CentralEscalaController`, `[Authorize]`)

- Sidebar `_AppSidebar.cshtml` L43: `@if (Can("CENTRAL_ESCALA"))` → sempre false para não-global (F2) ⇒ link invisível; L13: `"CENTRAL_ESCALA"` entra no array `canOperate` como elemento morto (`CanAccessModule` sempre false).
- Cross-links reais apontando para a rota: `Views/Convites/Index.cshtml` (×3: "Convidar médicos", "Localizar pela cobertura", empty-state) e `Views/Coordenacao/Index.cshtml` (×2: "Abrir cobertura", "Enviar convites") — e a página Convites **literalmente diz** "O convite nasce na Cobertura Inteligente" (mesma jornada de produto).
- Landing `COORDENACAO`→`CentralEscala` cai sempre no fallback Meu Dia (F2) — honesto, porém a tela de operação de coordenação fica inacessível inclusive nos tenants que operam escalas (contratados de `ESCALAS`, com grants dormentes prontos — F3).
- Auditoria `B2BCommercialOpsServices.AuditarAsync("CENTRAL_ESCALA", …)` e matriz de exibição (Api `PermissoesController` L37-38; Web `ProfessionalSaasControllers` L71) usam o código como string de domínio — exibição/trilha apenas, zero efeito em runtime (F8).

### F5 — `MINHA_AGENDA` (`MinhaAgendaController`, `[Authorize(Roles=MEDICO)]`) — o único fantasma com superfície **visível para o usuário final em produção**

- `Views/MeuDia/Index.cshtml` L47-64: os três cards "Minha Agenda" / "Meu Turno" / "Minha Produção" (`MinhaAgenda/Index|Presencas|MeusPagamentos`) ficam atrás de **apenas** `@if(isPhysician)` (= `User.IsInRole(MEDICO)`, L10) — nenhum `Can(...)`.
- Consequência: **todo médico logado vê 3 cards que redirecionam para `AccessDenied?reason=MODULO_NAO_CONTRATADO`** (o papel passa na checagem de autorização; o guard nega por F2). Módulo de origem é o comum `MEU_DIA` (sempre liberado) — o ponto de ruptura está no destino.
- É o único dos três fantasmas cujo defeito é **reproduzível por usuário de ponta hoje** (os outros dois têm degradação invisível/honesta).

### F6 — `MEDICO_AREA` (`MedicoAreaController`, `[Authorize(Roles="ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,MEDICO")]`)

- Páginas majoritariamente **placeholder Fase2** (`flowService.Build("MEDICO", section)` → `~/Views/Fase2Operational/Dashboard.cshtml`); exceção `Disponibilidade` (view real).
- Alcance hoje: (i) landing do médico — protegida pelo fallback wpa1 (sempre Meu Dia, honesto; contrato versionado: *médico sem `MEDICO_AREA` contratado → `302 /MeuDia`* — `wpa1-contrato-bff-e-landing.md`, coberto por `WpA1LandingMedicoTests`); (ii) cross-links a partir das views `MinhaAgenda/*` ("Informar disponibilidade", "Revisar convites") e `MedicoArea/Disponibilidade` ("Ver minha agenda") — **circulares** (só atingíveis se o usuário já entrou numa rota que o guard nega para todo não-global); (iii) URL direta.
- Não possui gêmeo no catálogo (nenhum módulo "área do profissional"); gate de papéis amplo (inclui admins de tenant).

### F7 — **NOVO** (descoberto nesta auditoria): `Fechamentos`, `CommandCenter`, `Coordenacao` existem como controllers Web autenticados **sem entrada no dicionário do guard** ⇒ `CATALOGO_NAO_CONFIGURADO` (classe de defeito R12, motivo mais ruidoso que o da família fantasma)

- Grep em `SaasRouteGuardFilter.cs`: nenhuma chave `Fechamentos`/`CommandCenter`/`Coordenacao`. Controllers reais: `V1420OperationalControllers.FechamentosController` (BaseWebController), `CommandCenterController` (`[Authorize(Roles=Operacao)]`, rotas atributadas), `ProfessionalSaasControllers.CoordenacaoController` (`[Authorize(Roles=Operacao)]`).
- Alcance medido: sidebar mostra a `CommandCenter` para `canManage || COORDENACAO/COORDENADOR` (L37-40) e a `Fechamentos` atrás de `Can("FECHAMENTOS")` (L46) — para global admin ambas renderizam (bypass torna `Can()` true) ⇒ **clique global-admin ⇒ AccessDenied CATALOGO_NAO_CONFIGURADO** (reproduzível ao vivo, nenhum smoke anterior visitou essas seções). Detalhe agravante: `FECHAMENTOS` também **não existe** em `modulos_sistema` — dupla ausência (dicionário + catálogo). `Coordenacao` fica dormente (nenhum `asp-controller="Coordenacao"` em Views; só URL direta).
- Não confundir com a fila R17 já existente "fantasmas residuais P3" (códigos ausentes do **catálogo** com entrada no dicionário) — F7 é o inverso (controller ausente do **dicionário**), mesmo motivo final, dono canônico a definir (família `PLANTOES`/`ESCALAS`/financeiro-operacional por controller).

### F8 — Matrizes de **exibição** referenciam os códigos fantasma (efeto zero em runtime, mas divergência canônica pós-fix)

- Api `PermissoesController.Matriz` L37-40: `COORDENADOR`/`OPERADOR` levam `"CENTRAL_ESCALA:VER"`; `MEDICO` leva `"MEDICO_AREA:VER"` (display do endpoint `/api/permissoes/matriz`).
- Web `ProfessionalSaasControllers` L71/L73: módulos `CENTRAL_ESCALA` e `MEDICO_AREA` na matriz estática de permissões (tela legada de perfis). Nenhum grant possível (F1: 0 linhas em `permissoes`).

## 3. Decisões versionadas

- **D1 — `CENTRAL_ESCALA` ⇒ REMAPEAR guard para `ESCALAS` (adotar gêmeo canônico existente; padrão R13-D1 / R5-E13; sem DDL).** Evidência que `ESCALAS` é o dono de domínio correto (reconsiderando a ressalva "domínio distinto" de r13a0, hoje com base completa):
  - `ESCALAS` existe no catálogo com suite completa (31 ações, incl. `.VER`) e **grants dormentes já existentes** nos perfis admin de tenant (F3) — exatamente o formato que a R13-D3 explorou para `FATURAMENTO_SAAS`;
  - a jornada de produto é única: a própria tela `Convites` diz que "o convite nasce na Cobertura Inteligente" (F4); o sidebar agrupa Plantões/Agenda/Cobertura/Convites/Escalas numa mesma seção operacional sob o mesmo conjunto `canOperate`;
  - nada é comercializado como `CENTRAL_ESCALA` (F1: sem linha em `saas_modulos`, sem contratação) — remapeio não quebra oferta;
  - ações do controller resolvem majoritariamente para `VER` (`DecidirSubstituicao`, `Convidar` → `VER`; `Cancelar` → `EXCLUIR`) — suíte `ESCALAS.*` cobre.
  Mudanças acopladas (mesmo commit b1): guard `["CentralEscala"]="ESCALAS"` + comentário de casa citando r17a0; sidebar L43 `Can("CENTRAL_ESCALA")`→`Can("ESCALAS")` (link passa a aparecer onde o módulo opera) e L13 remove o elemento morto `"CENTRAL_ESCALA"` do array; matriz Api: **sub-decisão D1a** — remover `"CENTRAL_ESCALA:VER"` dos defaults de `COORDENADOR`/`OPERADOR` (verificar consumo UI do endpoint em b1; se houver fato/UI dependente, mantém-se exibição e só o guard muda).
  Rejeitados: *(a)* DDL de módulo novo `CENTRAL_ESCALA` — mais pesado (migration + manifest + checksums + suite + grants + camada comercial) quando o dono de domínio já existe; *(b)* manter fantasma documentado — deixa link permanentemente invisível e cross-links de Convites/Coordenação mortos exatamente nos tenants operacionais; *(c)* global-only por construção — comportamento atual é acidental (ausência de catálogo), não desenho: a prioridade de landing `COORDENACAO` aponta para lá.
- **D2 — `MINHA_AGENDA` ⇒ MÓDULO COMUM/CORE (bypass de contratação e de permissão via sets em código; padrão R7-E1 `CONTA` / R5-D9; sem DDL, sem grants).** Justificativa: (i) o módulo de origem `MEU_DIA` já é comum e os cards são expostos a todo médico só pelo papel (F5) — o workspace do próprio médico (sua agenda, presença, produção) é core da conta dele, não capacidade contratável do tenant; (ii) o gate de audiência segue sendo o `[Authorize(Roles=MEDICO)]` do controller — os sets só eliminam a negação por módulo/permissão, não alargam o público; (iii) é o **único** remédio de um-lugar por código para as 3 rotas visíveis (`Index`, `Presencas`, `MeusPagamentos` + writes do mesmo controller) — remapear para `AGENDA` não resolveria no SV (gêmeo descontratado — F3) e estenderia semanticamente "agenda clínica de consultas" ao "plantão do médico" (domínios distintos: `api/agenda` × `api/medico-area/*`).
  Alvos exatos (paridade Web↔Api mantida): `PermissionService.CommonModules` (Web `AccessServices.cs` L161) + `ModuleAccessService.CoreModules` (Web L239) + `SecurityAccessServices.CommonModules` (Api L161) + `SecurityAdministrationServices.CoreModules` (Api L50), cada um com comentário de casa citando r17a0/D2. Sem impacto em `WpA1LandingMedicoTests` (fixa `MEDICO_AREA`, não `MINHA_AGENDA`).
- **D3 — `MEDICO_AREA` ⇒ MANTER CONTRATADO-SOMENTE (dormente por desenho); documento + comentário de casa no entry do guard + fato de pin.** Justificativa: (i) sem gêmeo no catálogo; (ii) páginas placeholder Fase2 (F6) — liberar globalmente moveria o **landing de todos os médicos** para dashboards stub e invertiria o contrato versionado wpa1 (caso 2 de `WpA1LandingMedicoTests`: sem claim ⇒ Meu Dia) — decisão de produto maior que o escopo desta rodada; (iii) degradação atual é honesta (fallback Meu Dia, nunca AccessDenied logo após login). Resíduo registrado na fila R18: cross-links `MinhaAgenda→MedicoArea` ficarão visíveis após D2 e seguirão mortos para médicos sem contratação (sub-item: contratar `MEDICO_AREA` em tenants piloto OU absorver as 3 ações em conjunto comum — decisão de produto com o wpa1).
- **D4 — F7 (Fechamentos/CommandCenter/Coordenacao fora do dicionário) ⇒ NOVO ITEM P2 DE FILA R18** com a evidência medida acima; cada controller exige dono canônico próprio (`Fechamentos` até carece de módulo no catálogo — dupla ausência; `CommandCenter`/`Coordenacao` candidatos à família `PLANTOES`/`ESCALAS`). Precedente D4-R13: fantasma descoberto no meio da rodada entra na fila com alcance já medido; não amplia o escopo do b1.
- **D5 — Sem DDL no b1**; sem resx (nenhum rótulo novo — o reuso do label "Escalas"/rótulos existentes basta); `access_catalog_version` permanece `v2149`; nenhuma mudança de contrato de API (o b1 toca apenas mapeamento de rota Web + sets em código + opcionalmente a matriz de exibição da D1a).

## 4. Slices planejados

| Slice | Entregável | Gate |
|---|---|---|
| **r17b1** | (i) guard: `["CentralEscala"]="ESCALAS"` + comentário casa; (ii) sidebar L43/L13; (iii) sets comuns: `MINHA_AGENDA` nos 4 sets (Web×2, Api×2) + comentários; (iv) comentário de pin em `["MedicoArea"]`/`["MinhaAgenda"]` citando D2/D3; (v) sub-decisão D1a conforme UI; (vi) fatos anti-regressão novos: **fonte** — `TryResolveModule("CentralEscala")=="ESCALAS"`, `"CENTRAL_ESCALA"` extinto dos valores do dicionário e do sidebar; **comportamental** (harness web-bff, estilo R12/R13): médico sem claims → `GET /MinhaAgenda/*` 200 e `GET /MedicoArea/Index` negado honestamente; sessão com claim `ESCALAS`+`ESCALAS.VER` → `GET /CentralEscala/Index` 200; sem claim → `reason=MODULO_NAO_CONTRATADO`; global admin bypass intacto | build `--no-incremental` 0 erros / 80 avisos idênticos + suíte full ≥ 1476 + novos fatos, 0 falhas (flake família ① 40P01 ⇒ isolate 5× antes de tratar como regressão) |
| **r17b2** | Smoke vivo: subir Api(51976)+Web(52976); (S1) login médico (`medico@plantaopro.local`) → landing Meu Dia (fallback MEDICO_AREA descontratado) → **antes**: click "Minha Agenda" ⇒ `MODULO_NAO_CONTRATADO`; **depois do rebuild**: 3 cards 200; (S2) ESCALAS no SV: reprovar `/CentralEscala` pré-grant (razão honesta) → grant reversível tag `HOMOLOG_R17` (1 linha `tenant_modulos` `ESCALAS`; grants dormentes já existem — F3) → re-login → 200 + link no sidebar; rollback net-zero verificado via `modulos_efetivos()`; (S3) `/MedicoArea/Index` continua honrado (D3) | Capturas DOM + SQL pré/pós/rollback |
| **r17z** | Relatório consolidado + fila R18 atualizada (F7 no topo; demais itens do r16z mantêm ordem) | — |

## 5. Riscos e mitigações

| Risco | Mitigação |
|---|---|
| D2 alargue acesso além do intended (sets comuns valem para todo autenticado) | O gate de papel do controller permanece autoridade de audiência; fato comportamental pinando `MedicoArea` (continua negada) e `MinhaAgenda` para médico; smoke S1 ao vivo |
| Remapeio `CentralEscala→ESCALAS` exponha Cobertura a papéis sem o desenho original (ex.: OPERADOR com `ESCALAS.VER`) | Perfil de exposição é o MESMO dos vizinhos `Plantoes`/`Escalas`/`Agenda`/`Convites` da seção (política por módulo+ação igual); grants dormentes determinam quem efetivamente vê |
| Paridade dos sets Web↔Api divergir no futuro | Os 4 sets ganham comentário cruzado citando r17a0/D2 (mesma prática R5-D9); fatos do b1 cobrem via comportamento |
| Matriz de exibição (D1a/F8) com consumer UI desconhecido | b1 verifica o consumer de `/api/permissoes/matriz` antes de tocar; default conservador = não mexer na matriz |

## 6. Fila R18 (previa, pendente do r17z)

1. **(novo, P2) F7**: `CommandCenter`/`Fechamentos`/`Coordenacao` sem entrada no dicionário do guard (Fechamentos também sem módulo no catálogo) — dono canônico próprio + reprodução ao vivo antes do mapeamento.
2. (herdado r16z #2) Estender scanner F-R15-A: `Adm360*WebController*` paths explícitos + camada DTO-API×ViewModel-Web (formato F9d).
3. (r16z #3) Whitelist JS-driven 6 actions (OrcamentoRejeitar/Cancelar/CancelarReserva, ValeCancelar, AtivarDepartamento/Cargo).
4. (r16z #4) Drill-downs r10b3 §6.
5. (r16z #5) Menu Bi/DashboardsPremium/FaturamentoSaas (decisão de produto).
6. (r16z #6) F-R14-D (premissa refinada).
7. (r16z #7) F-R14-B.
8. (r16z #8) F14 justificativa FecharCaixa.
9. (r16z #9) F-R15-C `pedidoItemId`.
10. (r16z #10) Fantasmas residuais P3: `HOSPITAL_AREA`, `MARKETPLACE`, `PROPOSTAS`, `PARCEIRO`, `OBSERVABILIDADE_GLOBAL`.
11. (sub-item novo, junto #10) R17-D3 resíduo: cross-links `MinhaAgenda→MedicoArea` — contratar `MEDICO_AREA` vs absorver no comum (decisão wpa1).
12. (r16z #11) Meu Dia fuso (produto). 13. (r16z #12) Tile B18 spec. 14. (r16z #13) Especialidade texto→id. 15. (r16z #14) pg_dump 18 (P5).
