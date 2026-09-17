# Acesso modular, administração do cliente e template

## Estado inicial comprovado

Base registrada em `3a46733` (merge da correção de autenticação), branch de trabalho `codex/acesso-modular-administracao-cliente-template`. A árvore estava limpa no início.

* O login já declarava `data-submit-loading="manual"`; `form-experience.js` já ignorava esse formulário e verificava `event.defaultPrevented`, enquanto `auth-login.js` era o único responsável por loading e POST nativo. Os testes `LoginSubmitRegressionContractTests` e `scripts/ui/login-submit-regression.mjs` cobrem a regressão de primeira submissão.
* Cookie/sessão são emitidos pelo Web após a API autenticar contra a identidade persistida. A API valida `session_id` persistido em toda validação JWT. Swagger não foi usado como evidência de login.
* `ApiRouteUniquenessIntegrationTests` preserva o validador geral de rotas e exige exatamente um GET para `/api/medicos/me/disponibilidade`.
* Equipe, perfis, vínculo por tenant, proteção do último administrador, revogação imediata de sessões e auditoria já estavam implementados em `SecurityAdministrationService` e nas telas `Usuarios`/`Perfis`. Não foram criados subsistemas paralelos.
* Existia API transacional de catálogo/revisão/solicitação/aprovação de módulos, porém `MinhaAssinatura/Modulos` era uma composição fixa, não consultava essa API e podia sugerir disponibilidade sem comprovação persistida.

## Matriz das fontes canônicas

| Regra | Fonte persistida | Serviço responsável | Tela / endpoint | Teste existente | Lacuna tratada ou remanescente |
|---|---|---|---|---|---|
| Identidade e hash | `usuarios`, credenciais de autenticação | `AuthService` / `AuthenticationSessionService` | `Account/Login`, `api/auth/login` | `LoginSubmitRegressionContractTests`, contratos de sessão v2156 | E2E PostgreSQL requer ambiente executável |
| Cliente e tenant | `clientes`, `tenants` | `UsuarioContextService` | seletor/context bar; endpoints de contexto | v2150/v2155 | Sem lacuna nova identificada |
| Vínculo ativo e vigência | `usuario_tenant_acessos` | `EffectivePermissionService`, `SecurityAdministrationService` | `Usuarios`; APIs de segurança | `V2155TenantPermissionBoundaryTests` | Sem lacuna nova identificada |
| Unidades autorizadas | vínculos de unidade do catálogo de segurança | serviços de acesso efetivo | equipe/perfis e APIs operacionais | contratos v2155 | Métrica histórica de atividade não existe |
| Perfis e permissões | `perfis`, `permissoes`, associações e overrides | `SecurityAdministrationService`, `EffectivePermissionService` | `Perfis/Permissoes`, APIs segurança/permissões | `PerfilCrudAuditTests`, v2155 | Revogação já encerra sessões afetadas |
| Catálogo de módulos | `modulos_sistema`, `modulo_catalogo_dependencias` | `ModuleContractingService` | `MinhaAssinatura/Modulos`, `api/portal-cliente/modulos/catalogo` | v2163/v2164 | Tela fixa foi conectada à fonte real |
| Contrato e direito | `tenant_modulos`, `tenant_modulos_historico` | acesso efetivo e `ModuleContractingService` | Meus módulos; APIs de acesso | v2149/v2163 | Política de consulta histórica após encerramento segue pendente |
| Oferta e solicitação | `solicitacoes_modulos`, `solicitacao_modulo_itens` | `ModuleContractingService` | revisão/confirmação/cancelamento | v2163 e `ClientModuleJourneyContractTests` | Concorrência entre chaves distintas foi serializada |
| Limites | `tenant_modulos.limite_contratado` e limites do plano | serviços SaaS existentes | Meus módulos/uso | `SaasLimitsAndPremiumFeaturesContractTests` | Sem telemetria por módulo: UI informa indisponibilidade, não zero |
| Menus | catálogo de features/navegação + acesso efetivo | `MenuBuilderService` | layout compartilhado | v2149 | Claims continuam antecipando UI; API sempre revalida sessão/políticas |
| Auditoria | tabelas de auditoria e históricos específicos | `AuditService`, serviços administrativos | `Auditoria`, APIs segurança/admin | `PerfilCrudAuditTests` e v2155 | Não houve novo fluxo de impersonação |

## Regras consolidadas e funcionalidades entregues

1. **Meus módulos real:** a tela carrega catálogo e solicitações do tenant autenticado, separando ativo/agendado/suspenso, oferta disponível e solicitação pendente.
2. **Contrato não é permissão:** a interface explica que vínculo, perfil e escopo continuam obrigatórios. O menu permanece produzido por `MenuBuilderService`; o servidor revalida sessão e política.
3. **Condição histórica:** valor e limite contratados vêm de `tenant_modulos`; valor e periodicidade da nova oferta vêm de `modulos_sistema`. Preço ausente é exibido como proposta, nunca como gratuito.
4. **Confirmação comercial segura:** navegador envia somente ids, versão, início e chave idempotente. A API relê oferta, dependências e elegibilidade. Mudança de condição devolve conflito e exige nova confirmação.
5. **Concorrência:** solicitações do mesmo tenant recebem advisory lock transacional; outra chave não cria solicitação pendente sobreposta. Aprovação bloqueia o registro contratual antes de ativar e não sobrescreve suspensão/concessão concorrente.
6. **Ativação:** aprovação global existente continua sendo o evento autorizador. Uma solicitação do cliente permanece `PENDENTE` e a UI declara que não houve ativação.
7. **Cancelamento:** somente solicitação pendente do próprio tenant é cancelada; nenhum dado operacional é excluído.
8. **Utilização:** limite verificável é mostrado. Sem telemetria consolidada por módulo, a tela apresenta “Informação indisponível”, sem fabricar zero, recência ou histórico.

## Telas e componentes alterados

* `MinhaAssinatura/Modulos`: cabeçalho, orientação de escopo, cards contratuais, solicitações, catálogo, revisão comercial, cancelamento, loading, validação, ajuda e layout responsivo Bootstrap.
* `MinhaAssinaturaController`: consulta, revisão, confirmação e cancelamento pelo fluxo API existente, com antiforgery e mensagens distintas para conflito e infraestrutura.
* DTOs Web/API e consulta do `ModuleContractingService`: vigência, limite e preço histórico, estado pendente e periodicidade persistida.

## Validação e evidências

O commit que contém este documento é a evidência versionada da entrega; consulte `git show --stat HEAD` e os testes adicionados. Resultados executados nesta rodada:

* **Aprovado:** inspeção estática do conflito de submit, do endpoint único de disponibilidade, das fontes persistidas e do fluxo transacional.
* **Aprovado:** `git diff --check` (executar novamente no fechamento do commit).
* **Bloqueado pelo ambiente:** restore/build/test .NET — o contêiner não possui o executável `dotnet` (`command not found`). Isso não é declarado como homologação.
* **Bloqueado pelo ambiente:** `npm run test:login-submit` não iniciou porque o Chromium não está instalado. A tentativa `npx playwright install chromium` recebeu HTTP 403 do CDN. Por isso E2E e screenshots em 360/768/1440 não foram apresentados como executados.
* **Bloqueado pelo ambiente:** E2E com PostgreSQL e navegador não foi executado nesta rodada; não há serviço/SDK provisionado. Testes falsos ou Swagger não foram usados como substituto.

## Limitações restantes

* A política de leitura histórica depois do encerramento não está formalizada. Por segurança, esta entrega não concede leitura geral.
* Não existe telemetria histórica consolidada por módulo capaz de provar usuário ativo no período ou última atividade; a tela assume explicitamente indisponibilidade.
* Aprovação comercial continua exclusiva do Super Administrador no fluxo já existente. Não foram criados gateway, nota fiscal, carência, proporcionalidade, desconto, multa ou inadimplência.
* Homologação completa da jornada com identidades persistidas, dois vínculos e PostgreSQL isolado deve ser executada quando o runtime .NET, banco e navegador estiverem disponíveis.
