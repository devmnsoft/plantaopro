# Diagnóstico CI v1.20.0

| Gate/job | Causa raiz | Arquivo | Correção | Teste executado | Evidência | Resultado final |
|---|---|---|---|---|---|---|
| repository-security | `appsettings.json` versionado continha connection string local, senha local conhecida, placeholder JWT inseguro e flag legada habilitada. | `backend/PlantaoPro.Api/appsettings.json` | Substituído por configuração vazia/segura; exemplos e scripts passam a orientar User Secrets e variáveis de ambiente. | `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build` | `AppsettingsDaApiVersionadoDevePermanecerSemSegredosEFlagsInseguras` e `AppsettingsVersionadosNaoDevemConterSegredosOperacionaisConhecidos`. | A validar no CI. |
| build-test | Uso de primary constructors em projeto configurado para C# 10. | `backend/PlantaoPro.CrossCutting/AccessScope.cs` | Convertidos `PrimaryRoleResolver`, `AccessScopeResolver` e `TenantContextResolver` para construtores explícitos e campos privados. | `dotnet build backend/PlantaoPro.sln -c Release --no-restore` | Build local registrado em `artifacts/build.log`. | A validar no CI. |
| database-* | Sem alteração estrutural de banco nesta correção incremental; scripts completos permanecem preservados. | `database/*` | Não aplicado nesta etapa para evitar regressão sem validação PostgreSQL completa. | Não executado localmente nesta etapa. | Pendência honesta registrada. | Pendente. |
| swagger/auth/security e2e | Dependem de runtime e ambiente com PostgreSQL/segredos configurados. | `backend/PlantaoPro.Api/*` | Configuração local segura documentada. | Não executado localmente nesta etapa. | Pendência honesta registrada. | Pendente. |

> Observação: a senha exposta no histórico deve ser rotacionada fora do Git imediatamente.
