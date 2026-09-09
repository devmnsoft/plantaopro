# v2.15.3 - status real pos v2.15.2

Data da analise: 2026-09-09
Branch: `codex/v2153-cockpit-saas-cobranca-modulos-design`

## Validacao inicial

Executado:

- `git status --short --branch`: repositorio em `main`, limpo antes da branch.
- `dotnet --info`: SDK .NET 10.0.400 disponivel.
- `dotnet restore backend/PlantaoPro.sln`: sucesso.
- `dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj -c Debug`: sucesso, com avisos existentes.
- `dotnet build backend/PlantaoPro.sln -c Debug --no-restore`: sucesso, com avisos existentes.

## Inventario encontrado

- Controllers API: 75 arquivos em `backend/PlantaoPro.Api/Controllers`.
- Services/API: 56 arquivos com padrao `*Service*.cs` ou `*Services.cs`.
- DTOs/modelos API: DTOs concentrados principalmente em `backend/PlantaoPro.Api/Models.cs` e arquivos especializados.
- Scripts SQL: 152 arquivos entre `database` e `backend/sql`.
- Views Razor: 403 arquivos em `backend/PlantaoPro.Web/Views`.
- Testes: 90 arquivos em `backend/PlantaoPro.Tests`.

## v2.15.2 - verificacao real

| Item | Status | Evidencia | Risco | Prioridade | Recomendacao |
| --- | --- | --- | --- | --- | --- |
| Login deixou de travar | Parcial, corrigido nesta rodada | `AuthService.LoginAsync` em `backend/PlantaoPro.Api/Data.cs` autentica usuarios validos; cliente suspenso agora pode logar com restricao operacional. | Sem smoke com banco real nesta rodada. | P0 | Rodar smoke com PostgreSQL populado e usuario suspenso. |
| Web autentica por Cookie | Feito | `AccountController` cria `ClaimsIdentity` com `CookieAuthenticationDefaults.AuthenticationScheme`. | Baixo. | P1 | Manter teste de regressao de claims/cookie. |
| API autentica por JWT | Feito | `Program.cs`, `JwtConfigurationValidator` e `GenerateToken` em `Data.cs`. | Medio se ambiente subir sem `Jwt__Key`. | P0 | Segredos somente por ambiente/secret store. |
| Claims de usuario, perfil e tenant | Feito/parcial | Claims incluem usuario, perfis, tenant, cliente, modulos e agora `cliente_status`. | Claims dependem da consistencia dos dados de cliente/tenant. | P0 | Validar materializacao com banco real. |
| Super Admin global | Feito | Perfis/constantes globais e dashboard SaaS existentes; `/AdminSaas` agora usa cockpit real. | Baixo para navegacao; medio para dados agregados sem smoke DB. | P0 | Testar com tenant global e tenant comum. |
| Cliente so acessa seu tenant | Parcial, endurecido nesta rodada | Varias queries ja possuem `tenant_id`/`cliente_id`; cobranca SaaS foi escopada por usuario atual. | Ainda ha servicos legados com queries sem tenant em areas antigas. | P0 | Auditoria query a query em `Data.cs`, `Saude360ClinicalService.cs` e controllers legados. |
| Menus filtram por perfil e modulo | Parcial, corrigido nesta rodada | `MenuBuilderService`, `AccessServices` e `SaasRouteGuardFilter`; filtro agora exige permissao e modulo contratado. | Claims de modulo so atualizam em novo login. | P0 | Criar invalidacao/refresh de claims ao contratar/bloquear modulo. |
| Formularios possuem validacao | Parcial | Varios ViewModels usam DataAnnotations; assinatura agora valida vigencia invertida tambem por atributo. | Telas legadas ainda variam em profundidade. | P1 | Padronizar validacao por tela tocada. |
| Telas tocadas possuem guia | Feito para telas alteradas | `AdminSaas/Index`, `Account/AccessDenied`, telas SaaS ja tinham blocos de guia. | Algumas telas antigas ainda usam guias curtos. | P1 | Continuar refinamento gradual. |
| Mensagens padronizadas | Parcial | `ApiResponse`, `_Alerts`, toasts e access denied padronizados; bloqueio de tenant recebeu mensagem propria. | Legado ainda tem textos inconsistentes. | P1 | Catalogar mensagens por modulo. |

## Areas mapeadas

- Autenticacao: `AuthController`, `AccountController`, `AuthService`, `JwtConfigurationValidator`, cookie Web e JWT API.
- Tenant e permissoes: `TenantContextService`, `CurrentUserService`, `SaasRouteGuardFilter`, `AccessServices`, `MenuBuilderService`, `FeatureCatalogService`.
- Modulos: `ModulosController`, `ModuleAccessService`, `modulos_sistema`, modulos contratados por tenant e claims de modulo.
- Planos e assinaturas: `PlanosController`, `AssinaturasController`, `PlanosSaasController`, `SaasCommercialController`.
- Cobranca SaaS: `FaturamentoSaasController`, views `FaturamentoSaas`, portal `MinhaAssinatura`.
- Cockpit SaaS: `SaasDashboardController` API e Web; `/AdminSaas` antes era demonstrativo e agora consome APIs reais.
- Auditoria: `IAuditService`, `AuditService`, `AuditoriaConstants`, controllers e telas `Auditoria`/`Seguranca`.
- Design/mensagens: partials `_KpiCard`, `_StatusBadge`, `_EmptyState`, `_ScreenGuide`, `_Alerts` e CSS/JS premium.

## Ausente, parcial ou quebrado

- Quebrado antes da rodada: `backend/PlantaoPro.Api/appsettings.json` versionava connection string com senha obvia e flags inseguras de banco legado/desenvolvimento.
- Quebrado antes da rodada: contratos de testes continham literais proibidos que faziam a propria varredura falhar.
- Quebrado antes da rodada: `database/source-checksums.json` e `database/scrpt_completo.sql` estavam desalinhados com os hashes reais dos arquivos fonte em Windows.
- Parcial: isolamento por tenant e modulo estava forte em varias areas SaaS, mas cobranca permitia papel `Administrador` acessar escopo global se omitisse `clienteId`.
- Parcial: `/AdminSaas` existia como pagina comercial/demo e nao como cockpit operacional real.
- Parcial: cliente bloqueado era impedido de logar em vez de entrar em modo restrito com faturas/suporte.
- Risco tecnico: ainda existem queries legadas com interpolacao e/ou sem escopo tenant em services antigos; algumas usam tabelas dinamicas controladas por whitelist, mas precisam revisao com banco real.

## Recomendacao priorizada

1. P0: manter login de cliente suspenso com operacao bloqueada e auditar tentativa de operacao.
2. P0: escopar cobranca SaaS por usuario atual e reservar geracao mensal global ao Super Admin.
3. P0: exigir modulo contratado no guard Web alem de permissao de perfil.
4. P0: remover segredos/flags inseguras de appsettings versionados.
5. P1: ampliar testes com PostgreSQL para confirmar materializacao Dapper e filtros tenant em rotas reais.
6. P1: revisar queries legadas de `Data.cs`, `Saude360ClinicalService.cs`, `UnitPortalServices.cs` e controllers operacionais que ainda carregam SQL dinamico.
