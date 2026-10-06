# Mapa — Governança SaaS (perfis/planos/assinaturas/escopos)

> Exploração somente leitura gerada por agente em 2026-10-06 sobre `origin/main` = `7f43af2`. Fonte primária do mapeamento; referenciado pela auditoria da rodada 3.

## Q1 · Escopos por perfil
- **Global MNSOFT é decidido por NOME**: `SecurityAdministrationServices.cs:93-97` — `upper(coalesce(p.codigo,p.nome)) in ('ADMIN_GLOBAL','ADMINISTRADOR_GLOBAL','SUPER_ADMIN','SUPER_ADMINISTRADOR')` → retorna `GLOBAL_ADMIN` no :97 antes de qualquer outro check. **Sim, um perfil genérico chamado `SUPER_ADMIN` tem escopo global só pelo nome**; a mesma lista de 4 nomes se repete nas rotinas de admin (:285, :334, :413-418) e `RolesConstants.cs` **não** define constante `SUPER_ADMIN`.
- Seeds: `database\seeds\system\040_perfis.sql` tem `ADMINISTRADOR_GLOBAL` e `ADMINISTRADOR_CLIENTE` (IDs md5 determinísticos, `base_sistema=true`); **sem** seed de `SUPER_ADMIN` ali. Existem `database\bootstrap\010..040_superadmin_*.sql`.
- **Vínculo de tenant / delegado**: `SecurityAdministrationServices.cs:98-120` — usuário com `tenant_id`/`cliente_id` próprio OU `usuario_tenant_acessos` ATIVO com janela temporal (`acesso_inicio/acesso_fim`, :109-110); senão `CROSS_TENANT_DENIED` (:117-118) ou `TENANT_INACTIVE` (:119-120).
- **Módulo contratado**: :122-131 — módulos fora de `CoreModules` exigem `tenant_modulos` com `habilitado=true AND status='ATIVO'` (:128-129), senão `MODULE_NOT_CONTRACTED`.
- **Permissão**: :133-136 — `perfil_permissoes` efetivas (com wildcard `*` e `modulo.*`); `bloqueado_por_plano` respeitado em :62. Coordenadores/especialistas não têm classe própria: são perfis com vínculos/permissões restritos por tenant.

## Q2 · Cadeia de decisão de acesso (API)
Existem: (1) **sessão** revalidada a cada requisição — `Program.cs:120-127` `OnTokenValidated` → `IAuthenticationSessionService.ValidateAsync` (expirada/revogada em `auth_sessoes` ⇒ 401); (2) **usuário ativo** — :91-92 (`reg_status='A'` + `status='ATIVO'`); (3) **vínculo tenant** — :101-118; (4) **tenant/cliente ativo** — :112-120; (5) **módulo contratado** — :125-131; (6) **permissão do perfil** — :133-136; (7) **vigência/uso da assinatura** — só em endpoints-guarda (`TenantServices.cs:272-298`, ver Q3); (8) **recursos por ID** — cobertos pela auditoria anterior.
Faltam na cadeia central (`TestarAsync`): **vigência comercial** (assinatura vencida/cancelada não é testada aí — o bloco existe apenas nos guardas pontuais de `ObterUsoPlano`, `TenantServices.cs:362-374`) e **escopo unitário/equipe/vínculo de recurso** (só há `modulo.acao`).

## Q3 · Direitos do plano — fonte canônica e aplicação
- **Fonte canônica**: `plantaopro.planos` (colunas `limite_* int default 0` + `permite_* bool`), `plano_recursos` e `tenant_modulos` (contrato por módulo c/ `limite_contratado`, `status`, `ativado_em/desativado_em`) — DDL em `database\migrations\2026_plantao_pro_saas_comercial_lgpd_jornada.sql:10-149`. O seed comercial é **intencionalmente vazio**: `database\seeds\system\070_planos_recursos.sql` (linha 1, comentário).
- **Semânticas** (`TenantServices.cs`): `limite <= 0` ⇒ **ILIMITADO** (:431-435); sem assinatura ⇒ `SEM_ASSINATURA` 403 (:362); `CANCELADA` ⇒ 403 (:368); `data_fim < hoje` ⇒ `ASSINATURA_VENCIDA` 403 (:374); assinatura vigente p/ limites = `ATIVA|TRIAL` e `data_fim>=hoje` (`SecurityAdministrationServices.cs:321-324`); **TRIAL** passa no `ObterUsoPlano` mas **falha nos flags** pois `ValidarFuncionalidadeAsync` exige `status='ATIVA'` (:389-394).
- **Onde os limites SÃO aplicados** (API, nos controllers de escrita/função): hospitais — `HospitaisController.cs:44`; médicos — `MedicosController.cs:67`; convites e publicação de plantão — `PlantoesController.cs:230,:258`; BI — `BiController.cs:66`; mobile/suporte — `MobileController.cs:132,:783`; operação assistida — `OperacaoAssistidaController.cs:343`; relatórios avançados — `RelatoriosSaasController.cs:124`; vagas de usuário — `SecurityAdministrationServices.cs:320-328 e 439-448` com `clientes ... for update` (:313).
- **NÃO aplicados**: o guarda `PodeCadastrarUsuarioAsync` (`TenantServices.cs:275`) **não é chamado por nenhum controller**; os demais CRUDs (Adm360, Saúde360 etc.) não chamam `ValidarLimiteAsync`; o menu (`MenuBuilderService`) e as rotas Web só filtram exibição via claims, sem checar limite/vigência.

## Q4 · Estados de vigência
- **Criação**: `SaasCommercialController.Criar` insere sempre `'ATIVA'` (sem AGENDADO de assinatura) — :371-372; self-service cria tenant+cliente+assinatura `ATIVA` de 1 mês — `SelfServiceServices.cs:248-249`.
- **AGENDADO (módulo)**: `ModuleContractingService.cs:129` insere `'ATIVO'` se `inicio_previsto<=now()`, senão `'AGENDADO'`. **Nenhum job de runtime muda AGENDADO→ATIVO** (único UPDATE em todo o backend é a ferramenta offline `PlantaoPro.Tools.Database\Program.cs:557`). Como o gate exige `status='ATIVO'` (`SecurityAdministrationServices.cs:129`), módulo agendado fica **bloqueado até intervenção manual**.
- **Suspensão/reação/cancelamento**: `SaasCommercialController.cs:383-390` — updates simples de `status` (sem datas, sem trava, sem recálculo); suspensão também reflete em `cliente.status` que bloqueia via `TenantServices.cs:328-331`.
- **Upgrade/downgrade**: `AlterarPlano` (:393-427) troca `plano_id` + grava `assinatura_historico` (dados preservados), **sem revalidar uso vs limites do plano destino**; no self-service o downgrade valida uso na solicitação (`SelfServiceServices.cs:478-485`) mas apenas insere `SOLICITADO` (:468-499). Cancelamento por self-service só insere `CANCELAMENTO_SOLICITADO` (:529-548).
- **Pós-vencimento**: leitura histórica **não** é bloqueada (a cadeia `TestarAsync` não consulta `assinaturas`); novas operações só nas rotas-guardadas (403 `VENCIDA`, `TenantServices.cs:374`).

## Q5 · Concorrência no último slot
- **Contratação de módulo**: serializada por `pg_advisory_xact_lock(hashtextextended(tenant))` — `ModuleContractingService.cs:71` + idempotência única `(tenant_id,chave_idempotencia)` parcial (:76-79); aprovação só para global admin (:96). Revalidação de condições por versão (:108-125).
- **Assinatura (último slot do cliente)**: `SaasCommercialController.Criar` faz check-then-insert **sem transação/trava** (:364-372); a proteção real é o índice parcial único `ux_assinaturas_cliente_ativa_trial ON assinaturas(cliente_id) WHERE reg_status='A' AND status IN ('ATIVA','TRIAL')` (`database\migrations\2026_plantao_pro_saas_inteligente_funcional.sql:111`; idêntico em `_saas_inteligente.sql:340`). Perdedor vira 23505 → **500 genérico** (:376-380), não 409. `Reativar` (:386-387) é update simples que pode violar o mesmo índice se já houver outra ativa.
- **Vagas de usuário**: trava de linha `for update` em `clientes` (`SecurityAdministrationServices.cs:313`) + checagem de limite (:320-328, :439-448).
- **Testes**: NÃO existe teste de concorrência de criação/reativação de assinatura "duas disputam, exatamente uma vence". Existe: cota IA — `AiRodada2GovernancaTests.cs:173`; triagem/consulta — `Saude360JornadaSFixesRegressionTests.cs:85,:188`; contratação de colaborador — `Administrativo360OrganizacaoTests.cs:609`; e contract-tests que só assertam o texto do lock na fonte (`ClientModuleJourneyContractTests.cs:28`, `CentralSaasClientesEquipeContractTests.cs:13`).

## Q6 · Consistência menu/web/API e revogação
- **Fonte lógica igual, mecanismo diferente**: API = re-consulta o banco a cada requisição (`TestarAsync` + revalidação de sessão `Program.cs:120-127`). Web = **claims fixadas no cookie no login** (`SessionClaimsBuilder`; catálogo v2149 em `AccessServices.cs:113`), atualizadas apenas em `/Account/RefreshContext` (`AccountController.cs:427-447`).
- Rotas/menu Web usam o mesmo conjunto de claims: `MenuBuilderService` (catálogo × `HasPermission` × `ModuleAccessService`) e `SaasRouteGuardFilter` (claims; bloqueio de cliente via claim **fixa** `cliente_status` — :202-207).
- **Revogação**: expiração/revogação de sessão no banco derruba a **API imediatamente**, mas a sessão Web aberta segue válida (com permissões antigas) até `RefreshContext` ou um erro 401/403 numa chamada BFF para a API. Há ainda fallback legado Web com mapa de permissões por role **hardcoded**: `AccessServices.cs:124-200`.

## Q7 · Testes de isolamento: MESMO módulo contratado nos DOIS tenants
**NÃO EXISTE** esse cenário específico. O que existe em `PlantaoPro.Tests`:
- `Administrativo360FiltrosOpcionaisTests.cs:395` `IsolamentoEntreTenants_TenantB_NaoVejaDadosDoTenantA` — TenantB é GUID fixo sem contrato (:29); o arquivo **não** insere `tenant_modulos` para B (islação testada só por escopo `tenant_id` dos serviços).
- `Saude360JornadaSFixesRegressionTests.cs:316` `F1b_IsolamentoDeTenant_...` — TenantB sem contrato; ação sobre triagem do A ⇒ 404; F4 (:404-504) testa o gate de módulo **um tenant por vez**, com `modulos_sistema` distintos por teste (:380).
- `Administrativo360CadastrosSeletoresEUnicidadeTests.cs:28` (write bloqueada em B), `Administrativo360ContasPagarEFechamentoTests.cs:622` (B efêmero, sem contrato).
- XML/arquivos (`Administrativo360DocumentosXmlA3Tests.cs:56-64`, `B1`, `CotacoesXmlDashboard`) contratam ADM360 **apenas** para o `TenantSantaCasa` — nunca para dois tenants na mesma suíte.
Logo: leitura/escrita/exclusão/arquivo/exportação **bidirecionais com o mesmo módulo ATIVO nos dois lados** não estão cobertas.

## Q8 · TestSigninController
- **Ativação SOMENTE por ambiente** (sem flag/config/atributo): `[Route("__test")]` + `[AllowAnonymous]` (`TestSigninController.cs:20-21`); guard `if (!_env.IsDevelopment() && !_env.IsEnvironment("Testing")) return NotFound();` em **:45-46** (`signin`) e **:100-101** (`dump`).
- **Desabilitado por padrão**: nenhum `appsettings*.json` versionado define `ASPNETCORE_ENVIRONMENT` (base do Web: `PlantaoPro.Web\appsettings.json` — só Logging/PlantaoProApi/Demo; API: `PlantaoPro.Api\appsettings.json` — Jwt.Key vazio); `Development` vem apenas de `launchSettings.json:11,:21`. Em Production (ambiente não definido ≠ Development/Testing) a rota responde **404** — inacessível. Ressalva: qualquer deploy com `ASPNETCORE_ENVIRONMENT=Development` reativa a ponta, sem chave adicional.

## GAPS (por risco)
1. **Crítico — sem ativação automática de AGENDADO**: módulo com início futuro fica bloqueado para sempre (`ModuleContractingService.cs:129` + gate `'ATIVO'` em `SecurityAdministrationServices.cs:129`); sem job no runtime.
2. **Crítico — Web opera com claims congeladas**: mudanças de perfil/permissão/módulo/cliente_status no banco não atingem sessões abertas até `RefreshContext`/401 BFF (`SessionClaimsBuilder`; `AccessServices.cs:113`; `SaasRouteGuardFilter.cs:202-207`).
3. **Crítico — admin global por casca**: escopo global concedido por correspondência de nome de perfil (`SecurityAdministrationServices.cs:93-97`), sem constante e sem seed canônico de `SUPER_ADMIN`; duplicar perfil com nome da lista abre tudo silenciosamente.
4. **Alto — AlterarPlano sem revalidação de uso** (`SaasCommercialController.cs:393-427`): downgrade abaixo do consumo atual passa sem bloqueio nem aviso.
5. **Alto — corrida de última assinatura sem trava** (`Criar` :364-372; `Reativar` :386-387): protegida apenas pelo índice parcial único (`2026_plantao_pro_saas_inteligente_funcional.sql:111`), que vira 500 genérico (:376-380) em vez de 409, e sem teste de concorrência dedicado.
6. **Alto — falta teste de isolamento com o mesmo módulo contratado nos DOIS tenants** (Q7: "NÃO EXISTE").
7. **Médio — fallback legado Web com permissões hardcoded por role** duplica o catálogo (`AccessServices.cs:124-200`).
8. **Médio — `bloqueado_por_plano` respeitado na leitura** (`SecurityAdministrationServices.cs:62`) **mas nenhum código o marca** em troca de plano — o bloqueio "por plano" pode ficar desatualizado.
9. **Médio — auditoria comercial com ator hardcoded** `ADMINISTRADOR_GLOBAL` (`SaasCommercialController.cs:457`).
10. **Médio — limites do plano aplicados só em ~7 superfícies** (médico/hospital/plantão/convite/BI/mobile/relatórios/operação assistida + vagas); o resto das escritas e o menu ignoram limite/vigência; guardas definidos mas não consumidos (`PodeCadastrarUsuarioAsync`, `PodeUsarAPIAsync`).
11. **Baixo — `tenant_modulos` sem unique (tenant, módulo)** no DDL base (`2026_plantao_pro_white_label_b2b_launch.sql:67-94`); integridade depende da ferramenta offline de reconciliação (`Tools.Database\Program.cs:557`).
12. **Baixo — TestSignin protegido só por ambiente**, sem flag de configuração independente (Q8).
