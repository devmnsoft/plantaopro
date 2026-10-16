# Rodada 13 — Auditoria inicial & plano de sprint (guard cobra `BILLING`; módulo canônico é `FATURAMENTO_SAAS`)

Data: 2026-10-16 · Base: `e3b7baa` (ponta do push da Rodada 12) · Itens da fila R13 consumidos: **P3 BILLING sem módulo de catálogo** (§7 do r12z — item 1, reproduzido ao vivo no r12b2).

## 0. Contexto e cadeia do achado

- **r10b3** validou por fonte canônica o alvo do card "Receita Vencida" do BI Executivo: `/FaturamentoSaas?status=VENCIDA` (endpoint com escopo automático por contexto `@global or f.cliente_id=@scopedClienteId`; `VENCIDA` já é opção do filtro da view).
- **r12b2** reproduziu ao vivo: com perfil admin SV + contrato BI, o card nega `MODULO_NAO_CONTRATADO` — "BILLING inexistente em `modulos_sistema` (nem `BILLING.VER`) ⇒ decisão de produto: DDL do módulo ou remapear o card" (fila R13 item 1).
- **Este recon (r13a0)** refina a natureza do defeito com evidência versionada no banco e no código (§1). Consequência: **não é "falta DDL"** — é mismatch de código entre o guard e o catálogo canônico, mesma classe de defeito dos fixes R6-BlocoA item 1 (Caixa), R7-E1 (Usuario) e R12-B1 (Bi/DashboardsPremium).

## 1. Recon da fonte canônica

### 1.1 Fatos medidos (probes desta sessão, psql 127.0.0.1/plantaopro)

| # | Fato | Evidência |
|---|---|---|
| F1 | Módulo `FATURAMENTO_SAAS` **existe** no catálogo | `modulos_sistema`: codigo=`FATURAMENTO_SAAS`, nome="Faturamento SaaS", status=`ATIVO`, reg_status=`A`, ordem=200, categoria=`OPERACAO`, disponivel_comercialmente=false |
| F2 | Suite completa de permissões existe | 31 linhas ativas em `permissoes` para o módulo (incl. `FATURAMENTO_SAAS.VER`, `.LISTAR`, `.PAGAR`, `.CONFIRMAR`…) — probe `PERM_POR_MODULO` |
| F3 | Código `BILLING` **não existe** em lugar nenhum | 0 linhas em `modulos_sistema`; 0 linhas em `tenant_modulos`; **zero referências** a `BILLING` fora das 2 entradas do guard; zero referências a `FATURAMENTO_SAAS` no C# |
| F4 | Perfil admin SV já tem grants do módulo certo (dormentes) | `perfil_permissoes`: 31/31 `FATURAMENTO_SAAS.*` com `permitido=true`, todas `reg_status='I'` (inativas = não emitidas no login; query efetiva exige `reg_status='A' AND permitido=true`) — padrão idêntico ao achado-factual da R12 sobre `BI.VER`/`CLIENTES.VER`/`DASHBOARD.VER` |
| F5 | Nenhum tenant contratou (e nenhum poderia) | `tenant_modulos`: 0 linhas para `FATURAMENTO_SAAS` e 0 para `BILLING`; `plano_modulos` vazio (0 linhas, 8 planos) — o contrato efetivo deriva de `tenant_modulos` (`modulos_efetivos(tenant_id)`), sem associação plano↔módulo nesta base |
| F6 | Tela legítima para tenants | `FaturamentoSaasController`: `[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,DIRETOR,FINANCEIRO")]`; escopo automático por contexto no endpoint (r10b3) |
| F7 | `BillingController` é shell de redirects (R5-B6) | `/Billing/*` → `Assinaturas` ou `FaturamentoSaas` (nenhuma tela grava dados); guard cobre os dois controllers via as 2 entradas `"Billing"`/`"FaturamentoSaas"` |
| F8 | Guard atual | `["Billing"]="BILLING"` e `["FaturamentoSaas"]="BILLING"` (`SaasRouteGuardFilter.cs` L18-19) — código fantasma: módulo impossível de contratar ⇒ todo usuário não-global que passa na checagem de papéis cai em `MODULO_NAO_CONTRATADO` (só o bypass global-admin salva) |

### 1.2 Fluxo do guard (relevante para a homologação)

1. Controller fora do dicionário → `CATALOGO_NAO_CONFIGURADO` (defeito R12; não se aplica aqui — as entradas existem).
2. Global admin → bypass total.
3. Módulo core/comum (sets em código: `CommonModules` MEU_DIA/AJUDA/LGPD/CONTA/TREINAMENTO; `CoreModules`/`TenantAdministrationModules` +USUARIOS/PERFIS/PERMISSOES/CONFIGURACOES/SEGURANCA/ASSINATURAS/CLIENTE_PORTAL/ONBOARDING) → sem live-check.
4. Sessão v2149 não-global em módulo não-core → **live-check** `effectiveModules.GetModulesAsync()` (contratos efetivos; falha ⇒ `VERIFICACAO_INDISPONIVEL` fail-closed) + claims de login.
5. Libera apenas se `permissions.HasPermission(modulo, acao)` E `modules.IsModuleEnabled(modulo)`. Motivos honestos: `PERMISSAO_NEGADA` vs `MODULO_NAO_CONTRATADO`.

Resolução ação→permissão de `FaturamentoSaasController` (sem overrides no dicionário): `Index`/`Details`/`Inadimplencia`/`Cobrar`/`GerarMensal`/`Contestar`/`ResolverContestacao`/`Notificar` → **VER**; `MarcarPaga` → **CONFIRMAR**; `Cancelar` → **EXCLUIR**.

### 1.3 Auditoria sistemática catálogo × guard (55 códigos distintos do dicionário)

Probe `r13_audit_guard_x_catalogo.sql` (VALUES dos 55 códigos LEFT JOIN `modulos_sistema` ativos + contagem de suite). **14 códigos ausentes do catálogo**; classificação:

| Código do guard | Catálogo | Classificação | Ação |
|---|---|---|---|
| `BILLING` (`Billing`, `FaturamentoSaas`) | AUSENTE — gêmeo canônico `FATURAMENTO_SAAS` existe (F1-F5) | **Mismatch corrigido nesta rodada (r13b1)** | Remapear guard → `FATURAMENTO_SAAS` |
| `OBSERVABILIDADE_GLOBAL` (`Observabilidade`) | AUSENTE — gêmeo canônico `OBSERVABILIDADE` existe (ordem 130, suite 31, 31 grants dormentes no admin SV) | Mismatch **dormente**: controller só aceita `ADMINISTRADOR_GLOBAL` (autorização por papel antecede o guard; global passa por bypass) — zero mudança comportamental hoje | **Fila (novo P3, R14)**: remapear `OBSERVABILIDADE_GLOBAL`→`OBSERVABILIDADE` (mesmo padrão, 1 linha + fato) |
| `MEU_DIA`, `CONTA`, `TREINAMENTO` (+`LGPD`, que também tem módulo) | AUSENTE/no-bypass | Por construção: sets `CommonModules`/`CoreModules` (bypass sem live-check) | Nada a fazer |
| `CLIENTE_PORTAL`, `ONBOARDING` | AUSENTE | Por construção: sets `CoreModules`/`TenantAdministrationModules` (comentário R5-D9 no código) | Nada a fazer |
| `CENTRAL_ESCALA` (`CentralEscala`) | AUSENTE — nenhum gêmeo óbvio (`ESCALAS` existe, domínio distinto: escala de plantões) | Fantasma; menu sidebar atrás de `Can("CENTRAL_ESCALA")`; links em Convites/Coordenacao | **Fila (novo P2)**: decisão canônica/produto (módulo próprio vs remapeio vs global-only) |
| `MINHA_AGENDA` (`MinhaAgenda`) | AUSENTE | Fantasma **atingível**: controller só papel `MEDICO` (passa na checagem), links em Meu Dia (módulo comum) | **Fila (novo P2)** |
| `MEDICO_AREA` (`MedicoArea`) | AUSENTE | Fantasma atingível: papéis incluem `MEDICO` + admins de tenant; links em MinhaAgenda | **Fila (novo P2)** |
| `HOSPITAL_AREA` (`HospitalArea`) | AUSENTE | Fantasma atingível: papéis incluem `HOSPITAL`/admins de tenant | **Fila (novo P3)** |
| `MARKETPLACE` (`Marketplace`) | AUSENTE | Fantasma atingível: `[Authorize]` simples (qualquer autenticado passa) | **Fila (novo P3)** |
| `PROPOSTAS` (`PropostasComerciais`) | AUSENTE | Fantasma atingível: `[Authorize]` simples | **Fila (novo P3)** |
| `PARCEIRO` (`ParceiroPortal`) | AUSENTE | Fantasma: papéis `ADMINISTRADOR_GLOBAL`+`PARCEIRO` | **Fila (novo P3)** |

Os outros 41 códigos do dicionário existem no catálogo com suite ativa (31; `ADM360`=70 por ações fiscais extra). **`FATURAMENTO_SAAS` é o único módulo do domínio de cobrança do SaaS no catálogo** (junto com `ASSINATURAS`, `PLANOS`, `CLIENTES`, `ADMIN_SAAS`).

### 1.4 Mecânica de exposição (sem mudança de produto nesta rodada)

- Sidebar: item "Cobranças" (`FaturamentoSaas`) só na seção **global admin** (`_AppSidebar.cshtml` L81-94) — hoje invisível a tenants pelo menu.
- Caminho do tenant = **drill-down do card "Receita Vencida"** do BI Executivo (r10b3) — é exatamente esse caminho que está quebrado e será homologado.
- Decisão de produto sobre expor a rota em menu para tenants segue aberta (mesma pendência genérica da R12), agora com a rota funcional.

## 2. Decisões versionadas

- **D1 — Direção do fix: remapear o guard, não o banco.** `["Billing"]="FATURAMENTO_SAAS"` e `["FaturamentoSaas"]="FATURAMENTO_SAAS"`. Mesma regra do R12-B1 ("adotar módulo canônico existente sobre criar novo"). Rejeitados:
  - *(a) DDL de módulo `BILLING`* — criaria 2º módulo do domínio de cobrança com suíte duplicada de 31 (risco de ambiguidade já reconciliado uma vez em v2197/v2198); o catálogo já registrou a intenção com `FATURAMENTO_SAAS` + grants completos nos perfis; exigiria migration + manifest + checksums sem agregar capacidade.
  - *(b) Remapear o card "Receita Vencida"* do BI para outra rota — o alvo `/FaturamentoSaas?status=VENCIDA` foi validado como honesto na R10 (dados reais, escopo por contexto); trocar o destino para esconder o defeito inverteria a ordem do tratamento.
- **D2 — Sem DDL no slice b1.** Nenhuma migration, sem alterar `migration-manifest.json`/checksums/consolidados, `access_catalog_version` permanece `v2149` (mudança é de mapeamento de rota em código Web, não de conteúdo de catálogo).
- **D3 — Homologação b2 com grant mínimo e reversível (tag `HOMOLOG_R13`), espelhando r12b2:** (i) 1 linha em `tenant_modulos` para `FATURAMENTO_SAAS` (tenant SV); (ii) **reativação** (UPDATE `reg_status 'I'→'A'`) do grant já existente do perfil admin para `FATURAMENTO_SAAS.VER` — suíte mínima de leitura, pois o guard resolve as telas provadas para `VER` (§1.2). Escrita (`MarcarPaga`→`CONFIRMAR`, `Cancelar`→`EXCLUIR`) fora do perímetro — os grants existem dormentes (F4) e a operação escrita é decisão operacional futura. Rollback exato (DELETE 1 + UPDATE 1) e verificação net-zero via `modulos_efetivos()`.
- **D4 — Os demais fantasmas (§1.3) não entram nesta rodada.** Cada um exige julgamento canônico próprio (gêmeo? novo módulo? global-only?) — entram na fila R14 classificados (item 2 abaixo), com a evidência de alcance já medida aqui.
- **D5 — Exposição em menu para tenants** segue como decisão de produto (fila), sem bloquear esta rodada.

## 3. Slices planejados

| Slice | Entregável | Gate |
|---|---|---|
| **r13b1** | Fix no `SaasRouteGuardFilter` (2 entradas + comentário de casa citando achado) + contrato anti-regressão (fato de fonte: entradas mapeadas para `FATURAMENTO_SAAS` e literal `"BILLING"` extinto do dicionário; fatos comportamentais web-bff: denegação `MODULO_NAO_CONTRATADO` sem contrato / `PERMISSAO_NEGADA` com contrato sem permissão / 200 com contrato+permissão / filtro `?status=VENCIDA` refletido / shell `/Billing` redireciona quando liberado / global admin 200 sem claims) | build Release EXIT 0 + suíte full ≥ 1437 + novos fatos, 0 falha (flake conhecido catálogo ① ⇒ isolate 5×) |
| **r13b2** | Homologação viva: subida Api/Web (padrão `r12_*.cmd`), denegação-honesta pré-grant reproduzida a partir do card do BI, grant `HOMOLOG_R13`, re-login, ciclo até 200 com KPI real, drill-down `/Bi → /FaturamentoSaas?status=VENCIDA`, rollbacks net-zero verificados | Capturas DOM + verificação SQL final (efetivos pré/pós/rollback) |
| **r13z** | Relatório final consolidado + fila R14 atualizada | — |

## 4. Riscos e mitigações

| Risco | Mitigação |
|---|---|
| Renomear o código cobrado muda o motivo exibido na negação para tenants (de `module=BILLING` para `module=FATURAMENTO_SAAS`) | Comportamentalmente equivalente (negação honesta mantida); nenhum contrato existente referencia `BILLING` (F3/F5) — ninguém migra de um estado |
| Sessão in-memory do Web invalida token pós-restart (`VERIFICACAO_INDISPONIVEL`) | Re-login no fluxo da b2 (já previsto no r12) |
| Flake catálogo ① (`ProvisionamentoB5Tests`/`R7OBOnboarding…`, 40P01 em teardown) | Isolate 5× antes de tratar como regressão (regra de casa R12) |
| Telas fazem `fetch('/api/…')` client-side presumindo origem única (achado r12b2) | Mesma limitação de configuração local já classificada; não bloqueia |

## 5. Critérios de fechamento da rodada

1. r13b1 commitado com gate limpo (base 1437 + novos fatos).
2. Ciclo completo ao vivo na b2: `MODULO_NAO_CONTRATADO` (pré-grant) → grant → 200 com dados reais (`/FaturamentoSaas/Index` e `?status=VENCIDA`, card do BI) → rollback net-zero.
3. Banco devolvido ao estado pré-rodada (efetivos SV = 18; grants do perfil nos valores anteriores; 0 resíduo `HOMOLOG_R13`).
4. r13z com fila R14 priorizada (incluindo os novos itens do §1.3).
5. Push somente com autorização explícita ao fim da rodada.

## 6. Baseline (e3b7baa)

- **Build Release**: EXIT 0, 0 erros (tempo ~14 s incremental).
- **Suíte full (Release, --no-build)**: `Total de testes: 1437` · **Aprovados: 1436** · **Falhas: 1** →
  `SaasGovernancaB6Rodada4Tests.G4b_ReativarComConflito_Retorna409_E_AtivaOriginalIntacta` com
  `Npgsql.PostgresException 40P01: impasse detectado` em `PurgarAsync()` no teardown (`Dispose`).
  Assinatura exata da família de flake conhecida ① (40P01 em `PurgarAsync()` — em R12 foi
  `ProvisionamentoB5Tests.P1_Finalizar…`). Regra de casa aplicada: isolate 5× → **5/5 verde
  (8/8 aprovados por passada, zero falhas)** ⇒ flake transitório inter-classe (deadlock de purge
  concorrente na suíte paralela), **não regressão**. Baseline aceito: 1437 com flake ① classificado.
- **Nota de ambiente**: 1ª tentativa do build falhou por lock de DLLs Release — os servidores da
  janela R12 (`.NET Host` Api :51976 e Web :52976) ainda estavam no ar segurando os binários;
  processos encerrados (PIDs 12560/26676) e build refeito limpo. Postgres **em execução** durante
  o baseline (127.0.0.1:5432) — os testes que usam banco real (incl. a família de flake ①) rodam
  contra ele, como o próprio 40P01 do G4b confirma.
