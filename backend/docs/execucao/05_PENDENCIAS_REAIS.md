# Pendências reais

| Pendência | Impacto | Prioridade | Arquivo provável | Recomendação |
|---|---|---:|---|---|
| SDK `dotnet` ausente | impede build, startup, publish e validação de DI | P0 bloqueante | ambiente de execução | instalar SDK compatível com os `TargetFramework` e repetir comandos |
| Build .NET não executado no ambiente de manutenção | SDK .NET 10 indisponível localmente | validação dependente do CI | `.github/workflows/dotnet-ci.yml` | executar restore, build e testes no runner com SDK 10 |
| PostgreSQL real não disponibilizado | startup validator, health DB, bootstrap e login sem smoke | P0 | configuração externa | fornecer instância descartável e connection string via secret/env |
| `/SystemHealth` ainda não executado | rota/parsing compilação não confirmados | P0 | `ConfiguracoesController.cs` | executar API/Web e smoke HTTP após SDK/banco |
| Mapa completo de rotas/BFF | risco de links 404 não mensurado | P1 | controllers, views e JS existentes | inventariar Swagger e rotas em runtime após P0 |
| Fases SaaS/operacional/go-live | não devem começar com P0 bloqueada | P1+ | módulos existentes | concluir e homologar P0 antes da próxima fatia vertical |

## Revalidação v1.95.1

Não há declaração de GA nesta atualização corretiva. Build/testes .NET, clean install e replay PostgreSQL 16, upgrade legado, equivalência estrutural, instaladores, runtime API/Web, autenticação e isolamento multiempresa devem apresentar sucesso executável no CI antes da aprovação da Sprint 0.1.
