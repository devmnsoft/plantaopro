# R6-A1 — Acesso canônico: função única, conflito pacote×capacidade e verificação live no Web

Data: 2026-10-09. Base: `HEAD = 4e0869a` (R6-A2/v2339). Tudo desta entrega está **não commitado** neste
momento (10 arquivos alterados + 4 novos); commit com prefixo `R6-A1` e push ao final da rodada.
Banco de prova: `plantaopro_test` (PostgreSQL 18 local); migração `v2340` aplicada via
`PlantaoPro.Tools.Database upgrade` → saída `upgrade: 2026_10_v2340_r6a1_acesso_canonico_conflito_pacote_capacidade aplicada.`
Suíte de fechamento: **1198/1198** (baseline da rodada: 1191), executada 2x seguidas sem oscilação.

**Estado honesto**: IMPLEMENTADO → TESTADO (suíte + banco real). AINDA NÃO HOMOLOGADO em host vivo
(roteiro da seção 6) e não LIBERADO.

## 1. O que o item pediu (Bloco A, item 1) e o que foi feito

| Exigência | Situação | Como ficou |
|---|---|---|
| Validar `modulos_efetivos`/`modulo_efetivo` em API, sessão Web, menu, onboarding, serviços | Parcial → fechado para o caminho crítico | Sessão: `LoadModulesAsync` já via função canônica desde v2339. Menu: alinhado (seção 3). Onboarding/serviços: mantêm o kernel B6 existente (mesma fonte `ModuleContractVigencia`). Agora existe **endpoint canônico** `GET api/auth/effective-modules` (consulta direta da `ModulosEfetivosSql`, sem o efeito colateral de insert de sessão do `RefreshContextAsync`) |
| Provar que o pacote SAUDE360 concede capacidades com permissões por ação | Comprovado | Testes de banco S1/S2/S3 (transação com rollback) + grants da própria v2339 (ex.: RECEPCAO→PACIENTES/AGENDAMENTOS/PAINEL_CHAMADA com VER) + teste live web |
| Suspensão/expiração/alteração/revogação refletidas | Comprovado na função e no Web | S3 (revogação do pacote remove filhos), S2 (restrição individual), teste live web (revogado depois do login → negado **sem re-login**) |
| Conflito pacote×capacidade tratado explicitamente | Decisão materializada em `v2340` | Regra: **linha própria de contrato da capacidade prevalece sobre a herança do pacote**. Nas duas ramificações de herança de `modulos_efetivos` foi adicionado anti-join `not exists` (por `modulo_id` ou `codigo_modulo`, `reg_status='A'`). O predicado B6 ficou textualmente idêntico ×4 (teste de sincronia preserva a contagem) |
| Nada liberado pela simples presença de menu | Corrigido onde divergia | Menu agora espelha o contrato exato do guard (mesmo par módulo+ação). Guard continua a autoridade. `/Caixa`, linkado no sidebar, estava **fora do catálogo do guard** (usuário autenticado caía em `CATALOGO_NAO_CONFIGURADO`) → mapeado para `CLINICA_FINANCEIRO` (mesmo domínio de `ClinicaFinanceiro`) |
| Não depender somente de claims antigos para comandos críticos | Implementado com resiliência documentada | Guard Web (agora `IAsyncActionFilter`) consulta a fonte canônica por request quando: sessão v2149 + módulo não-core + módulo consta nas claims do login (ou seja, só quando a verificação pode mudar a decisão). Resposta disponível = authoritative; indisponível/desabilitada = degrada aos claims do login (mesma fonte canônica emitida no login — janela documentada, sem falha em cascata se a API estiver fora) |

## 2. Matriz estado × efeito (item 1)

| Cenário | Antes desta entrega | Depois | Prova executada |
|---|---|---|---|
| Pacote SAUDE360 ativo, capacidade-filha sem linha própria | efetiva por herança (v2339) | igual | `S1_ContratarPacote_LiberaCapacidadesHerdadas` (banco real) |
| Pacote ativo + capacidade com linha própria **desabilitada** | herança **vencia** — capacidade continuava efetiva (conflito não resolvido) | linha própria vence → capacidade sai do conjunto efetivo | `S2_CapacidadeDesabilitadaIndividualmente_NaoEntraEmPacoteAtivo` (banco real) + sync-test do anti-join (`not exists` ×2, `ovr.modulo_id=ch.id`, `codigo_modulo`) |
| Pacote revogado/suspenso | filhos deixam de ser efetivos (v2339) | igual + página web nega na hora | `S3_RevogacaoDoPacote_RemoveTudoInclusoCapacidadeComLinhaAtiva` (banco real) + `RevogadoDesdeLogin_GuardNegaSemReloginComModuloNaoContratado` (web end-to-end) |
| Capacidade com linha própria **ativa** dentro de pacote revogado | aparecia (linha direta B6) | igual — override independente | assert final do S3 |
| Módulo revogado após o login, usuário com sessão aberta | guard Web aceitava pelos claims do login até re-login/refresh (8 h) | guard consulta a fonte canônica por request e nega com `MODULO_NAO_CONTRATADO` | teste live web (stub determinístico do endpoint) |
| API operacional fora do ar | — | páginas core/comuns seguem abertas; páginas não-core degradam aos claims do login (mesma fonte canônica) em vez de derrubar tudo | comportamento do resolver (null → claims); flag `Access:LiveEffectiveModuleCheck` |
| Menu (sidebar/catálogo) para sessões v2149 de tenant com SAUDE360 contratado | itens Pacientes/Agendamentos/Triagem/Jornada clínica/Caixa **ocultos** — o menu checava a família legada `SAUDE360_*`, que não existe nas claims v2149, enquanto o guard liberava as mesmas páginas com os códigos finos | menu espelha o guard (mesmos pares módulo+ação que o `ControllerModules`/`ResolvePermissionAction` cobram) | `FeatureCatalogService` + `MenuBuilderService.HasAccess` + `_AppSidebar` alinhados; suíte 1198/1198 (nenhum contrato de guarda/monetário/fiscal quebrou) |
| `/Caixa` para usuário autenticado | controller fora do catálogo do guard → redirect `CATALOGO_NAO_CONFIGURADO` | gate `CLINICA_FINANCEIRO` coerente com o domínio | entrada adicionada em `ControllerModules`; sidebar idem |
| Motivos de denegação nos contratos existentes (`PERMISSAO_NEGADA` vs `MODULO_NAO_CONTRATADO`) | estáveis | preservados (a degradação a claims mantém o motivo determinístico nos testes) | 17→0 falhas após refinamento (fail-closed rígido virou degradação documentada — decisão registrada na seção 5) |

## 3. Alterações por arquivo

Novos (4):

| Arquivo | O que traz |
|---|---|
| `database/migrations/2026_10_v2340_r6a1_acesso_canonico_conflito_pacote_capacidade.sql` | `create or replace` de `plantaopro.modulos_efetivos(uuid)` com o override per-capacidade (anti-join `not exists` nas duas ramificações de herança); assinatura de retorno idêntica à v2339; idempotente/aditiva |
| `backend/PlantaoPro.Web/Services/Security/EffectiveModuleResolver.cs` | `IEffectiveModuleResolver`/`EffectiveModuleResolver`: cache por request em `HttpContext.Items["__EffectiveModules__"]`; Bearer do `Session["JwtToken"]`; parse de `ApiResponse<string[]>` (espelhado em `PlantaoPro.Web.Models`); `null` em falha ou desabilitado; global admin → `{"*"}`; flag `Access:LiveEffectiveModuleCheck` (padrão ativo) |
| `backend/PlantaoPro.Tests/R6A1AcessoCanônicoConflitoPacoteCapacidadeTests.cs` | 3 testes de integração em banco real (escopo transacional com rollback, códigos únicos por Guid — não dependem nem poluem o catálogo compartilhado): S1/S2/S3 da matriz |
| `backend/PlantaoPro.Tests/R6A1LiveContractCheckWebTests.cs` | Fábrica/collection `web-bff-live` (check ON) + 2 testes end-to-end: revogado-depois-do-login → `AccessDenied?reason=MODULO_NAO_CONTRATADO` **sem re-login** (e o endpoint canônico foi de fato consultado); ainda-contratado → página OK |

Alterados (10):

| Arquivo | O que mudou |
|---|---|
| `database/migration-manifest.json` | entrada `v2340` (active, transactional, dependsOn v2339) |
| `backend/PlantaoPro.Api/Controllers/AuthController.cs` | `[Authorize] [HttpGet("effective-modules")]`: global admin → `["*"]`; senão consulta `ModulosEfetivosSql` pelo claim `tenant_id`/`cliente_id` (padrão inline-SQL do reset-password; sem insert de sessão) |
| `backend/PlantaoPro.Web/Program.cs` | registro do `IEffectiveModuleResolver` no DI (scoped), junto dos serviços de segurança |
| `backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs` | conversão para `IAsyncActionFilter`; dispara a resolução live apenas quando v2149 + não-core + módulo nas claims do login; nova entrada `["Caixa"]="CLINICA_FINANCEIRO"` |
| `backend/PlantaoPro.Web/Services/Security/AccessServices.cs` | `IsModuleEnabled`: conjunto resolvido é authoritative; sem estado ou falha → claims do login. Novo helper estático `IsCoreOrCommonModule` + `HasModuleClaim` |
| `backend/PlantaoPro.Web/Services/FeatureCatalogService.cs` | família `SAUDE360_*` → códigos finos canônicos (`PACIENTES`, `AGENDAMENTOS`, `PAINEL_CHAMADA`, `TRIAGEM`) com as ações que o guard cobra nas páginas (`VER`) |
| `backend/PlantaoPro.Web/Services/Security/MenuBuilderService.cs` | `HasAccess` espelha o contrato exato do guard; removido `IsFeatureEnabled(feature.Code)` (slug de exibição tratado como módulo ocultava itens legítimos: `AGENDA`, `CHECK_IN`, `FILA_ATENDIMENTO`, `COBERTURA`) |
| `backend/PlantaoPro.Web/Views/Shared/_AppSidebar.cshtml` | seção Saúde 360: `Can(legado) || Can(fino)`; cabeçalhos incluem os códigos finos; Caixa idem |
| `backend/PlantaoPro.Tests/R6GranularidadeSaude360Tests.cs` | sincronia B6×4 agora valida **as duas** migrações (v2339 original + v2340 vigente); novos fatos: anti-join de override (contagem == 2 e chaves) e wiring do manifesto (v2340 active/transactional/dependsOn v2339) |
| `backend/PlantaoPro.Tests/Infrastructure/PlantaoProWebFactory.cs` | `UseSetting("Access:LiveEffectiveModuleCheck","false")` — os contratos determinísticos de guarda/monetário/fiscal não modelam o endpoint e preservam "sem chamada de API"; classe deixou de ser sealed (subclasse live) |

## 4. Testes classificados e evidência

| Classe | Teste | O que prova | Resultado executado |
|---|---|---|---|
| Integração (banco real, transação+rollback) | `R6A1…S1` | contratar pacote libera capacidades-filhas (herança por `modulo_id`) | PASS |
| Integração | `R6A1…S2` | conflito: capacidade com linha própria desabilitada **sai** do conjunto efetivo; demais continuam | PASS |
| Integração | `R6A1…S3` | revogação do pacote remove a herança; linha própria ativa sobrevive (override independente) | PASS |
| Contratos de sincronia (texto↔C#↔manifesto) | `R6GranularidadeSaude360Tests` (2 fatos novos + sincronia alargada p/ v2340) | predicado B6 idêntico ao C# ×4 nas duas migrações; anti-join presente nas duas ramificações; manifesto correto | PASS |
| End-to-end Web (host Testing + stub determinístico) | `RevogadoDesdeLogin_GuardNegaSemReloginComModuloNaoContratado` | revogação reflete na hora, motivo honesto, endpoint canônico consultado (não só claims) | PASS |
| End-to-end Web | `AindaContratado_EndpointConfirma_ModuloLiberado` | caminho de liberação com a verificação live confirmando | PASS |
| Regressão | Suíte completa `dotnet test PlantaoPro.Tests --no-restore --no-build` | nenhum contrato existente quebrou (guarda, monetário, fiscal BFF, provisionamento, landing médico, flags de signin…) | **1198/1198** em 2 execuções (baseline da rodada 1191; +7 novos) |

Decisão de desenho registrada: o check live começou fail-closed rígido (API fora → módulo negado) e
quebrou 17 contratos que fixam o motivo/ausência de chamada; refinado para **authoritative-when-available
com degradação aos claims do login** (mesma fonte canônica emitida no login). Em produção o endpoint
está disponível e é o decisor; a degradação é a janela de resiliência documentada. Na fábrica web genérica
o check fica desligado (`Access:LiveEffectiveModuleCheck=false`) porque o stub não modela o endpoint; a
coleção `web-bff-live` o liga para a prova dedicada.

## 5. Classificação das regras tocadas pelo item

**Existentes + comprovadas (esta rodada)**: herança pacote→capacidades; revogação/suspensão via
predicado B6; conflito por linha própria (nova, `v2340`); grants de perfil por capacidade (v2339);
endpoint canônico; verificação live no guard; menu coerente com o guard.

**Sem validação viva (dívida mantida de propósito, sessões legadas)**:
- Fallbacks pré-v2149 hardcoded em `AccessServices.cs:128–199` (janela de cookies ≤ 8 h; preservados para não quebrar tickets antigos — por isso o sidebar usa `Can(legado) || Can(fino)`).
- `RolesConstants.Saude360*` — strings de papel quase-duplicadas entre Web e API (adendo D.3 #8).
- `Saude360ModuleFilter` com SQL próprio e fail-open em erro de banco (D.3 #2).
- 5ª variante SQL do guard de módulo no onboarding (D.3 #10).
- `[RequireModule]` da API lê o JWT cunhado no login: rotas de dados da API continuam com a vigência
  congelada até refresh (mitigação no nível da API já existe via `SecurityAdministrationServices`, que é
  canônica). A live-check cobre o que o usuário vê/navega no Web.

**Proposta materializada**: a regra de precedência "linha própria > herança de pacote" (era pendência
do item; agora é código de migração + teste).

**Decisões pendentes (fora do alcance deste slice, registradas para o backlog)**:
1. Motivo `AccessDenied`: hoje a degradação a claims preserva `MODULO_NAO_CONTRATADO` mesmo quando a
   causa é "verificação indisponível". Separar os dois motivos exige parametrize o redirect (dono: rodada de design/i18n).
2. Código fino `UNIDADES` no guard (hoje `ClinicaUnidades→SAUDE360` grosso por decisão E13) — expansão de contrato.
3. Unificar as fontes residuais D.3 #2–#10 numa única leitura do kernel quando as sessões legadas desaparecerem.

## 6. Roteiro de aceite (homologação em host vivo, ~15 min)

Pré-requisito: hosts API+Web apontando para o banco homologado; um tenant com contrato SAUDE360 ativo
e um usuário de Recepção nesse tenant.

1. Logar como Recepção → sidebar mostra Pacientes/Agendamentos/Triagem (menu coerente); abrir cada uma.
2. Com o navegador aberto, desabilitar **apenas** a linha própria de `PACIENTES` em `tenant_modulos`
   (`habilitado=false`) do tenant → recarregar `/Pacientes`: deve cair em `AccessDenied` com
   `MODULO_NAO_CONTRATADO` **sem re-login** (verificação live) — e `/Agendamentos` continua aberto.
3. Reabilitar a linha → recarregar: abre novamente.
4. Suspender o pacote `SAUDE360` inteiro → todas as capacidades ficam negadas; `/MeuDia`, `/Ajuda`,
   `/Lgpd` (core/comum) permanecem abertos.
5. Derrubar a API → páginas core seguem abertas; página clínica degrada aos claims do login (comportamento
   documentado, não é erro falso).
6. Confirmar `select version from schema_migrations order by version desc limit 1` = `v2340`.

Aceite = passos reproduzíveis com motivos honestos e sem liberar o que o contrato não cobre.
