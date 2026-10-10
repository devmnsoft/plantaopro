# R6-A1b — Completar autorização canônica: fontes residuais, falhas honestas e mensagens separadas

Data: 2026-10-10. Base: `HEAD = 857a60a` (R6-A1, já pushado em `origin/main`). Este é o
**continuação do Bloco A item 1** ("Concluir autorização canônica"), cobrindo exatamente as
pendências registradas em `r6a1-acesso-canonico.md` §5. Banco de prova: `plantaopro_test`
(PostgreSQL 18 local). Suíte de fechamento: **1204/1204** (baseline da rodada: 1198, +6 novos),
executada 3x seguidas (1 falha isolada de timing não se reproduziu nas 2 execuções seguintes).

**Estado honesto**: IMPLEMENTADO → TESTADO (suíte + banco real + e2e web). AINDA NÃO HOMOLOGADO
em host vivo (roteiro da seção 7) e não LIBERADO. Não houve nova migração (v2339/v2340 preservados).

## 1. O que o item pediu e o que foi encontrado ao revisar

| Pendência registrada em r6a1 | Situação encontrada em `857a60a` | Ação desta rodada |
|---|---|---|
| `Saude360ModuleFilter` (API) com SQL próprio e fail-open | **Já resolvida em R6-A2** (função canônica `modulo_efetivo()` + falha propaga 500) — o adendo D.3 é anterior à v2339 | Nenhuma mudança de lógica; separação de mensagem quando falta contexto de organização (seção 2, linha 9) |
| `SelfServiceServices` só `habilitado=true` | **Já resolvida** (`TenantContextService` usa `ModulosEfetivosSql` + `modulo_efetivo` na validação de permissões) | Reclassificada como fechada |
| Guard de módulo do onboarding (5ª variante SQL) | **Já resolvido** (`OnboardingJornadaService` consulta `modulos_efetivos`) | Reclassificada como fechada |
| `[RequireModule]` lendo claims JWT congeladas | **Não existe** como nomeado no repositório; a política por request da API consulta o banco a cada request (fonte canônica desde B6) | Reclassificada como fechada/N-A; a exigência "revogação refletida sem re-login via chamada direta à API" agora tem prova dedicada (seção 2, linha 3) |
| `RolesConstants.Saude360*` quase-duplicados Web/API (adendo D.3 #8) | Os **23 códigos base eram literalmente duplicados**; as composições divergem por política legítima de cada ponta | Base única em `CrossCutting/Security/AppRoles.cs`; ambos os `RolesConstants` delegam (seção 3) |
| Fallbacks pré-v2149 `AccessServices.cs:128–199` (adendo D.3 #9) | Vivo no Web (~76 linhas papel→módulo) e no API (~25 linhas + bypass "tudo menos BI_AVANCADO"); **código morto em produção** — todo emissor atual carrega `access_catalog_version=v2149` (`Data.cs`, `SessionClaimsBuilder`, `TestSigninController`) | Removidos: caminho único por claims (módulo efetivo + permissão por ação) — seção 4, decisão D2 |
| Separar mensagens: não contratado × permissão insuficiente × sessão expirada × verificação indisponível | Só existiam CLIENTE_BLOQUEADO / MODULO_NAO_CONTRATADO / PERMISSAO_NEGADA | Novo motivo `VERIFICACAO_INDISPONIVEL` com retry honesto; "sessão expirada" permanece na camada de autenticação (cookie expira → redirect login) — decisão D3 |
| Não transformar falha de banco/API em autorização automática para dados sensíveis ou comandos de gravação | Comandos de gravação passam pela API (autorização por request no banco — sua própria fonte; banco fora = comando falha honestamente, nunca "autorizado") | Comportamento confirmado e provado; páginas comuns (core/comuns) seguem acessíveis; páginas não-core passam pela decisão live (decisão D1) |

Pontos **só-de-exibição** que também respondiam "módulo contratado" com predicados parciais
(administração SaaS global, leitura): unificados à função canônica onde barato e inequívoco —
`ModulosVigentes` (rótulo por cliente), `ClientesAtivos` e flag `Contratado` do catálogo SaaS.
O count de `ManagerCommandCenterService` permanece parcial (exibição, não autoriza) — dívida P3.

## 2. Matriz estado × efeito (continuação do item 1)

| Cenário | Antes de R6-A1b | Depois | Prova executada |
|---|---|---|---|
| Suspensão/revogação do contrato DURANTE sessão ativa, via chamada direta à API (mesmo usuário, mesmas claims do token) | Comportava-se certo na prática (filter consulta por request) mas sem prova dedicada sequencial | Idêntico + **prova**: contrato ativo → chamada passa; `habilitado=false` no banco → próximo request da mesma sessão nega 403 com a mensagem exata de contrato, sem re-login/refresh | `R6A1bRevogacaoSessaoAtivaApiTests.FiltroApi_SuspensaoDuranteSessaoAtiva_NegaSemReloginEmChamadaDireta` (banco real) |
| Check live fora do ar + módulo **nunca** contratado no login (sem claim) | Guard dizia `MODULO_NAO_CONTRATADO` — afirmava ausência de contrato **sem ter podido consultá-la** | Motivo honesto `VERIFICACAO_INDISPONIVEL` + botão "Tentar novamente" apontando para a página de origem (parâmetro `retorno` do guard) | `R6A1bVerificacaoIndisponivelWebTests.LiveDown_ModuloNuncaContratadoNoLogin_MotivoVerificacaoIndisponivelComRetornoHonesto` (web e2e, collection `web-bff-live`) |
| Check live fora do ar + módulo contratado no login (claim presente) | Janela documentada degradando aos claims | Idêntica (janela mantida — claims são a mesma fonte canônica emitida no login) + prova dedicada de que a página segue liberada | `R6A1bVerificacaoIndisponivelWebTests.LiveDown_ClaimDoModuloNoLogin_JanelaDocumentadaDegradaAosClaimsELiberaPagina` (web e2e) |
| Check live OK + conjunto efetivo sem o módulo + sem claim | `MODULO_NAO_CONTRATADO` | Idêntico, agora como **contraste comprovado** com o caso acima (prova × indisponibilidade distinguíveis no redirect) | `R6A1bVerificacaoIndisponivelWebTests.LiveOk_ConjuntoVazio_SemClaimNoLogin_MotivoModuloNaoContratadoComProva` (web e2e) |
| Módulo contratado **depois** do login (ausente das claims, presente no conjunto efetivo) | Página negada até re-login (guard só verificava live quando a claim existia) | Guard consulta live para **qualquer** página não-core: conjunto efetivo é authoritative → página liberada se o perfil der a permissão (`PERMISSAO_NEGADA` caso contrário) | Decisão D1; coberto indiretamente pelos 3 e2e (condição ampliada exercida em todos) |
| Sessão sem `access_catalog_version` (só possível fora dos emissores atuais) + claims de módulo/permissão | Decidia por conjuntos papel→módulo hardcoded | Decide pelos **mesmos** claims que o login canônico emite — um caminho só | `R6A1bDecisaoUnicaPorClaimsTests.SessaoSemCatalogo_PoremComClaimsDeModuloEPermissao_DecidePorClaims` |
| Papel clínico sem claims (ex.: TRIAGEM sem módulo/permissão) | Papel dava acesso a módulos (fallback) e o bypass pré-v2149 liberava tudo menos BI_AVANCADO | Papel isolado **não** autoriza módulo nem permissão; BI_AVANCADO não tem mais bypass por ausência de catálogo | `R6A1bDecisaoUnicaPorClaimsTests.PapelSemClaims_NaoAutorizaModuloClinico_NemBypassPorAusenciaDeCatalogo` |
| Chamada direta à API clínica sem contexto de organização (tenantId nulo) | 403 disfarçado de "Módulo Saúde 360 não contratado para este cliente." | 403 "Sem contexto de organização nesta sessão. Entre novamente para reavaliar o acesso." (não há com quem comparar contrato) | `Saude360JornadaSFixesRegressionTests.F4_AutenticadoSemTenantNoEscopo_BloqueadoCom403EMensagemDeContexto` |
| Rótulo "Módulos vigentes" por cliente (admin SaaS) | Somente linhas próprias `habilitado+ATIVO+vigência` — escondia capacidades herdadas do pacote SAUDE360 | Função canônica (herança + override v2340 incluídos) | SQL de `SaasServices.cs`; suíte completa (nenhum contrato de exibição quebrou) |
| Catálogo SaaS: "clientes ativos" por módulo + flag "contratado?" por tenant | Contagem exigia linha própria (sem vigência/herança); flag existia só pela linha (ignorava status/vigência) | Contagem `m.codigo = any(modulos_efetivos(t))`; flag `modulo_efetivo(@tenantId, m.codigo)` (herança inclusa); `Habilitado` mantém o detalhe administrativo da linha própria | SQL de `SaasCoreServices.ListAsync`; suíte completa |
| Códigos de papel base (23) duplicados em dois `RolesConstants` | Duplo literal idêntico | Fonte única `AppRoles` (CrossCutting); composições permanecem locais (política de cada ponta) | Contrato atualizado `CanonicalRoles_MustMatchProductSpecification` (verifica fonte **e** delegação nos dois arquivos) |
| Contratos existentes de guarda/motivos (R6-A1) e demais domínios | 1198/1198 | Preservados | Suíte 1204/1204 (1198 antigos + 6 novos) |

## 3. Alterações por arquivo

Novos (2):

| Arquivo | O que traz |
|---|---|
| `backend/PlantaoPro.CrossCutting/Security/AppRoles.cs` | Os 23 códigos de papel base canônicos (valores persistidos) como const única compartilhada API/Web |
| `backend/PlantaoPro.Tests/R6A1bAutorizacaoCanonicaComplementoTests.cs` | 6 testes: 1 e2e web por motivo novo + janela (3), 1 revogação-durante-sessão na API com banco real, 2 unidade do caminho único por claims |

Alterados (14):

| Arquivo | O que mudou |
|---|---|
| `backend/PlantaoPro.Web/Security/RolesConstants.cs` | Códigos base delegam a `AppRoles`; composições locais intocadas |
| `backend/PlantaoPro.Api/RolesConstants.cs` | Idem (aliás B2B + composições clínicas locais intocados) |
| `backend/PlantaoPro.Web/Services/Security/AccessServices.cs` | `PermissionService.HasPermission`: removidos os ~76 linhas de fallback papel→módulo pré-v2149 — caminho único (CommonModules → administração core de tenant admin → claim de módulo + claim de permissão). `IsModuleEnabled`: removido o ramo pré-v2149 "tudo liberado menos BI_AVANCADO" |
| `backend/PlantaoPro.Api/SecurityAccessServices.cs` | Espelho do mesmo no `ModulePermissionService` (HasPermission + IsModuleEnabled) |
| `backend/PlantaoPro.Web/Services/Security/EffectiveModuleResolver.cs` | Novo `enum EffectiveModulesStatus { NaoTentada, Ok, Falhou }` + `GetStatus()` no resolver (chave ausente = não tentada; valor nulo = tentada e falhou; conjunto = ok — sem nova estrutura, usando a semântica existente do cache) |
| `backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs` | Condição do check live ampliada: dispara para qualquer página de módulo não-core em sessão v2149 não-global (antes exigia a claim — o que tornava o motivo "indisponível" inalcançável). Denegação passa a calcular o motivo com prova (seção 4/D3) e redireciona com `retorno=/Controller/Acao` |
| `backend/PlantaoPro.Web/Views/Account/AccessDenied.cshtml` | Título/descrição/botões para `VERIFICACAO_INDISPONIVEL` ("Tentar novamente" → `ViewBag.Retorno`; "Acionar suporte") |
| `backend/PlantaoPro.Web/Controllers/AccountController.cs` | `AccessDenied` aceita e repassa `retorno` (ViewBag) |
| `backend/PlantaoPro.Api/SaasServices.cs` | Subquery "ModulosVigentes" → função canônica `modulos_efetivos` + nome do catálogo (com fallback para o código) |
| `backend/PlantaoPro.Api/SaasCoreServices.cs` | `ClientesAtivos` e `Contratado` → função canônica (detalhe acima) |
| `backend/PlantaoPro.Api/Saude360ModuleFilter.cs` | Sem contexto de organização: mensagem própria (não "não contratado") |
| `backend/PlantaoPro.Tests/Saude360JornadaSFixesRegressionTests.cs` | `AssertEnvelope403Objeto` aceita mensagem opcional; teste F4 sem-tenant renomeado e fixado à nova mensagem |
| `backend/PlantaoPro.Tests/GenericClientOperationalContractTests.cs` | Contrato de papéis canônicos lê `AppRoles.cs` (fonte) e exige a delegação nos dois `RolesConstants` |
| `backend/PlantaoPro.Tests/Saude360BaseClinicaContractTests.cs` | Varredura de papéis clínicos inclui `AppRoles.cs` |

## 4. Decisões tomadas nesta rodada (classificadas)

- **EX-C (existente, confirmada)** — a API reavalia autorização por request contra o banco
  (sua própria fonte de dados); comandos de gravação nunca foram autorizados "em memória":
  banco fora = comando falha com erro honesto. Prova sequencial adicionada (matriz linha 1).
- **PROP aplicada (decisão registrada)** **D1 — live authoritative para toda página não-core**:
  removida a pré-condição "módulo consta nas claims do login" da condição do guard. Efeitos:
  (i) torna alcançável e honesto o motivo `VERIFICACAO_INDISPONIVEL`; (ii) módulo contratado
  **depois** do login já libera a página (permissão de perfil continua exigida — sem claim de
  permissão o motivo é `PERMISSAO_NEGADA`, coerente: contratação × permissão permanecem
  distinguíveis); (iii) custo de 1 HTTP call por request (cacheado; endpoint próprio, interno).
  A fábrica compartilhada de testes mantém a flag OFF, então contratos determinísticos de
  guarda não sentem a mudança; a coleção `web-bff-live` (flag ON) cobre o novo caminho.
- **PROP aplicada** **D2 — caminho único por claims**: todo emissor atual de sessão carrega
  `access_catalog_version=v2149` incondicionalmente (evidência: `Data.cs` no login,
  `SessionClaimsBuilder` no refresh, `TestSigninController` nos testes), portanto os fallbacks
  papel→módulo eram código morto em produção — e, enquanto vivos, conflitavam com a regra de
  distinguir contratação × permissão (era o próprio resíduo que o adendo D.3 registrou).
- **PROP aplicada** **D3 — separação final dos motivos**: `CLIENTE_BLOQUEADO` (status do
  cliente), `MODULO_NAO_CONTRATADO` (há **prova**: live OK sem o módulo, ou claims sem o
  módulo quando a verificação não foi tentada), `PERMISSAO_NEGADA` (contratado, sem grant),
  `VERIFICACAO_INDISPONIVEL` (tentou consultar e não pôde — nunca afirma ausência de contrato).
  "Sessão expirada" continua sendo tratamento da camada de autenticação (cookie expirado →
  redirect para login), documentado em vez de duplicado.
- **PROP aplicada** **D4 — base única de papéis** em CrossCutting (dupliquémos a parte
  efetivamente duplicada; composições locais legítimas permanecem).
- **PROP aplicada** **D5 — exibição admin unificada** somente onde barato/inequívoco
  (rótulo, contagem e flag do painel SaaS global); `ManagerCommandCenterService` fica como
  dívida P3 exibição (não autoriza nada).

## 5. Testes (classificados)

- **Novos (6)** — `R6A1bAutorizacaoCanonicaComplementoTests.cs`:
  - *Integração banco real (revogação)*: `FiltroApi_SuspensaoDuranteSessaoAtiva_...` — seed de
    contrato SAUDE360 ativo → filtro passa → `update habilitado=false` → mesma sessão nega 403
    com a mensagem exata; cleanup transacional por Guid.
  - *E2E web (collection `web-bff-live`, stub determinístico do endpoint canônico)*: 3 fatos da
    seção 2 (indisponível c/ retorno honesto; janela c/ claims; prova do não-contratado).
  - *Unidade (caminho único)*: 2 fatos sobre sessões sem catálogo decidindo por claims, e papel
    sem claims não autorizando (inclui fim do bypass de BI_AVANCADO).
- **Ajustados (3, sem mudança de intenção)**: F4 sem-tenant (mensagem de contexto), contrato de
  papéis canônicos (fonte `AppRoles` + delegação), varredura de menus clínicos do Web (inclui
  `AppRoles`).
- **Regressão**: suíte completa **1204/1204** (1198 baseline + 6 novos), 3 execuções seguidas;
  1 falha isolada de timing em `Relatorio_PagamentosMedicos_FiltrosDeDimensaoAusenteEFormaCaseInsensitive`
  ocorreu na 1ª execução desta rodada e não se reproduziu — raiz identificada e corrigida fora
  do código (seção 6).

## 6. Achado e correção de dados (fora de migração, banco de teste)

`Relatorio_PagamentosMedicos_...` falhava na 1ª execução porque uma linha **demo** de
`plantaopro.pagamentos` (criada 2026-09-28, "Dra. Ana Souza — Demonstração", valor 1500) ficou
com `tenant_id`/`cliente_id` NULL. O filtro de tenant dos relatórios B9 é
`coalesce(tenant_id, cliente_id) is null or ... = @tenantId` — linha sem tenant conta para
**todo** tenant. A janelinha móvel do teste (−5 dias UTC) chegou hoje a 2026-10-05, exatamente o
`data_prevista` dessa linha: por isso verde ontem e vermelho hoje (reprodução isolada em HEAD
limpo confirmou que não era este slice). Corrigido backfillando a linha a partir da origem
(canônica: o plantão pai `d3f6584c-…-4751` pertence ao tenant `d3f6584c-…-47502`/cliente
`d3f6584c-…-47501`) — sem tocar código do relatório.

**Vulnerabilidade residual (decisão pendente, não inventei regra)**: linhas sem tenant continuam
visíveis em todo relatório via `is null or` — comportamento legítimo para tabelas legadas sem
coluna de tenant, mas quebra isolamento de tenant se um dia existir linha operacional sem
tenant. Opções: (a) manter compatibilidade legada (status quo), (b) filtro estrito
`coalesce(...)=@tenantId` (relatórios de tabelas antigas esvaziam), (c) matriz por tabela.
Escolha registrada como **DEC pendente** na seção 8.

## 7. Roteiro de aceite do usuário (homologação viva — converte TESTADO → HOMOLOGADO)

Host vivo, tenant real com SAUDE360 contratado e perfis RECEPÇÃO/TRIAGEM disponíveis:

1. Logar como perfil clínico; abrir Pacientes/Agendamento/Triagem → liberado, menu coerente.
2. Com a sessão aberta, suspender o contrato no backoffice (ou `update tenant_modulos set
   habilitado=false`): voltar às páginas clínicas → denegação imediata **sem re-login**, motivo
   "Módulo não contratado" (live respondeu).
3. Desligar/derrubar o host da API (janela de manutenção) e repetir: perfil com claims
   contratadas → página liberada (janela documentada); perfil sem o módulo → tela
   "Verificação de contrato temporariamente indisponível" com botão **Tentar novamente** que
   volta à página original e refaz a decisão.
4. URL direta de módulo nunca contratado → "Módulo não contratado" (com prova quando a API
   responde).
5. Chamada direta à API clínica com o JWT da sessão ativa durante suspensão → 403 com a
   mensagem exata de contrato (prova do spec "tanto pelo Web quanto por chamada direta").
6. Conferir `lang`/layout da tela AccessDenied nos 4 motivos (sem corte de texto).

## 8. Dívidas e decisões pendentes (estado pós-R6-A1b)

- **Fechadas nesta rodada**: fallbacks pré-v2149 (Web+API); duplicação base de papéis;
  `Saude360ModuleFilter` fail-open e SQL próprio (desde R6-A2); variante SQL do onboarding e
  `SelfServiceServices` (já canônicas); `[RequireModule]` (reclassificado N-A); parâmetro de
  motivo de denegação (agora `VERIFICACAO_INDISPONIVEL` implementado).
- **DEC pendente**: filtro de relatórios para linhas sem tenant (opções a/b/c da seção 6).
- **DEC pendente (comercial/clínica)**: código fino `UNIDADES` para `ClinicaUnidades` (hoje
  `SAUDE360` grosso, decisão E13 registrada) — entra no guard/catálogo quando houver linha
  própria em `modulos_sistema`.
- **DEC pendente (homologação)**: upgrade formal do banco principal `plantaopro` + IIS
  (v2339/v2340 aplicados apenas em `plantaopro_test` até agora).
- **P3 (higiene)**: count parcial em `ManagerCommandCenterService` (exibição, não autoriza);
  strings de composição de papéis API/Web permanecem locais por design (registrado).
- **Sessão expirada × anônimo**: distinguidos pela camada de autenticação (documentado); sem
  mecanismo novo.

## 9. Backlog ordenado (atualiza o §4 do r6a0)

1. **Homologação viva** deste corte + R6-A1 (roteiro acima) — depende do usuário/host.
2. **Upgrade formal** do banco principal `plantaopro` (v2339+v2340) + IIS — janela do usuário.
3. ~~Unificar fontes residuais de acesso~~ → **concluído por R6-A1b** (restam só os itens DEC/P3 acima).
4. **Bloco E — i18n** (pt-BR padrão + en/es/fr; glossário canônico; precedência
   usuário→organização→navegador→pt-BR) — próximo corte de código.
5. **Bloco D — design global** (tokens, contraste, viewports 1440/1024/768/390 + 200%).
6. Código fino `UNIDADES` (aguarda DEC comercial).
7. Conector fiscal P1/P2 (aguarda credenciais).
8. P3s diversas (ManagerCommandCenter, etc.).
