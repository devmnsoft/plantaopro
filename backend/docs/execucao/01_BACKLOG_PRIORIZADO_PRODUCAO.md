# Backlog priorizado de produção

| Ordem | Módulo/contexto | Regra | Arquivos prováveis | Dependências | Prioridade | Status | Aceite |
|---:|---|---|---|---|---:|---|---|
| 1 | Fundação/build | Não remover recursos para compilar | `PlantaoPro.sln`, projetos | SDK .NET | P0 | Bloqueado pelo ambiente | clean/restore/build passam |
| 2 | Configuração JWT | chave >=32; segredo real somente fora do Git | `PlantaoPro.Api/appsettings*.json`, `Security/JwtConfigurationValidator.cs` | configuração por ambiente | P0 | Implementado, não compilado | Development inicia; produção sem segredo falha |
| 3 | Banco/install | migração idempotente e não destrutiva | `PlantaoPro.Tools.Database/Program.cs`, fonte canônica ausente `database/scrpt_completo.sql` | recuperar script oficial | P0 | Pendente real | install e upgrade repetíveis |
| 4 | System Health | não expor segredo/connection string; rota real | `HealthController.cs`, `ConfiguracoesController.cs`, View | API iniciada | P0 | Compatibilidade corrigida, smoke pendente | `/SystemHealth` abre e lê contrato atual |
| 5 | Login/bootstrap | inativos/bloqueados não autenticam; seed só Development e idempotente | `AuthService`, `DevelopmentSeed`, ferramentas bootstrap | banco pronto | P0 | Pendente de smoke | login válido/inválido e auditoria |
| 6 | Base SaaS | isolamento tenant e autorização no backend | controllers/services existentes | P0 | P1 | Não iniciada | CRUDs e bloqueios reais |
| 7 | Fluxo principal PlantaoPro | fechar fluxo existente antes de extensão | módulos operacionais existentes | P1 | P2 | Não iniciada | jornada real ponta a ponta |
| 8 | Operação/go-live | secrets externos, CORS/HTTPS e publish | Programs/appsettings/docs | P0-P4 | P6 | Não iniciada | publish e checklist aprovados |
| 9 | Testes automatizados novos | somente após estabilidade produtiva | nenhum nesta fase | P0-P7 | P8 | Adiado | criação autorizada após homologação |
