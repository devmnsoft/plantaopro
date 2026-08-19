# Pendências reais

| Pendência | Impacto | Prioridade | Arquivo provável | Recomendação |
|---|---|---:|---|---|
| SDK `dotnet` ausente | impede build, startup, publish e validação de DI | P0 bloqueante | ambiente de execução | instalar SDK compatível com os `TargetFramework` e repetir comandos |
| Produto no checkout é PlantaoPro, não Valora Insight | requisitos/rotas/tabelas não correspondem ao domínio presente | P0 decisão | repositório/branch | confirmar checkout correto antes de qualquer módulo novo; não misturar domínios |
| Fonte `database/scrpt_completo.sql` referenciada pela CLI e por testes não existe | instalação limpa falha | P0 bloqueante | `PlantaoPro.Tools.Database/Program.cs`, `database/scrpt_completo.sql` | recuperar o script canônico do histórico/artefato oficial; não reconstruir schema por suposição |
| PostgreSQL real não disponibilizado | startup validator, health DB, bootstrap e login sem smoke | P0 | configuração externa | fornecer instância descartável e connection string via secret/env |
| `/SystemHealth` ainda não executado | rota/parsing compilação não confirmados | P0 | `ConfiguracoesController.cs` | executar API/Web e smoke HTTP após SDK/banco |
| Mapa completo de rotas/BFF | risco de links 404 não mensurado | P1 | controllers, views e JS existentes | inventariar Swagger e rotas em runtime após P0 |
| Fases SaaS/operacional/go-live | não devem começar com P0 bloqueada | P1+ | módulos existentes | concluir e homologar P0 antes da próxima fatia vertical |
