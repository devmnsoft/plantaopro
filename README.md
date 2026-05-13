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
