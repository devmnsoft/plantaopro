# PlantãoPro
Projeto full-stack para gestão de plantões médicos com backend ASP.NET Core (DDD/Clean), PostgreSQL e app mobile React Native/Expo.
## Estrutura
- backend/ (API, Web, Domain, Application, Infrastructure, CrossCutting)
- database/ (script completo + seeds)
- mobile/PlantaoPro.App (Expo TypeScript)
- docs/
## Execução
1. PostgreSQL: crie DB `plantaopro`.
2. Execute `database/PlantaoPro_PostgreSQL_Completo.sql` e depois `database/seeds.sql`.
3. Ajuste connection string em `backend/PlantaoPro.Api/appsettings.json`.
4. Backend API: `dotnet run --project backend/PlantaoPro.Api`.
5. Web: `dotnet run --project backend/PlantaoPro.Web`.
6. Mobile: `cd mobile/PlantaoPro.App && npm install && npx expo start`.
Usuário admin: admin@plantaopro.com / Admin@123

## Backend MVP (rodada atual)
Foram adicionados endpoints reais para hospitais, especialidades, plantões, escalas (aceitar), financeiro (gerar pagamento), notificações e dashboard usando Dapper/PostgreSQL.

## Novos fluxos backend (Escalas/Financeiro/Notificações/Dashboard)
- Endpoints de escalas: `/api/escalas`, `/api/escalas/{id}`, `/api/medicos/me/plantoes`, `/api/plantoes/{id}/aceitar`, `/api/escalas/{id}/confirmar|recusar|cancelar|substituir|marcar-realizado`.
- Endpoints financeiros: `/api/financeiro/pagamentos`, `/api/financeiro/pagamentos/{id}`, `/api/financeiro/pagamentos/gerar`, `/api/financeiro/pagamentos/{id}/confirmar|cancelar`, `/api/financeiro/meus-pagamentos`.
- Endpoints notificações: `/api/notificacoes`, `/api/notificacoes/nao-lidas`, `PUT /api/notificacoes/{id}/lida`, `PUT /api/notificacoes/lidas`.
- Dashboard: `GET /api/dashboard` e `GET /api/mobile/home` com indicadores/listas/gráficos.
- Operações críticas devem sempre usar transação explícita, histórico de status e auditoria.
