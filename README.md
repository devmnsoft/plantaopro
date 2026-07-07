# PlantãoPro
Projeto full-stack para gestão de plantões médicos com backend ASP.NET Core (DDD/Clean), PostgreSQL e aplicativo móvel multiplataforma.
## Estrutura
- backend/ (API, Web, Domain, Application, Infrastructure, CrossCutting)
- database/ (script completo + seeds)
- mobile/PlantaoPro.App (TypeScript mobile)
- docs/
## Execução
1. PostgreSQL: crie DB `plantaopro`.
2. Execute `database/PlantaoPro_PostgreSQL_Completo.sql` e depois `database/seeds.sql`.
3. Ajuste connection string em `backend/PlantaoPro.Api/appsettings.json`.
4. Backend API: `dotnet run --project backend/PlantaoPro.Api`.
5. Web: `dotnet run --project backend/PlantaoPro.Web`.
6. Mobile: `cd mobile/PlantaoPro.App && npm install && npm run dev`.
Usuário admin: admin@plantaopro.com / 123456

## Backend MVP (rodada atual)
Foram adicionados endpoints reais para hospitais, especialidades, plantões, escalas (aceitar), financeiro (gerar pagamento), notificações e dashboard usando Dapper/PostgreSQL.

## Novos fluxos backend (Escalas/Financeiro/Notificações/Dashboard)
- Endpoints de escalas: `/api/escalas`, `/api/escalas/{id}`, `/api/medicos/me/plantoes`, `/api/plantoes/{id}/aceitar`, `/api/escalas/{id}/confirmar|recusar|cancelar|substituir|marcar-realizado`.
- Endpoints financeiros: `/api/financeiro/pagamentos`, `/api/financeiro/pagamentos/{id}`, `/api/financeiro/pagamentos/gerar`, `/api/financeiro/pagamentos/{id}/confirmar|cancelar`, `/api/financeiro/meus-pagamentos`.
- Endpoints notificações: `/api/notificacoes`, `/api/notificacoes/nao-lidas`, `PUT /api/notificacoes/{id}/lida`, `PUT /api/notificacoes/lidas`.
- Dashboard: `GET /api/dashboard` e `GET /api/mobile/home` com indicadores/listas/gráficos.
- Operações críticas devem sempre usar transação explícita, histórico de status e auditoria.

## Release Candidate técnico — julho/2026

### Build, testes e CI

- A solution oficial é `backend/PlantaoPro.sln` e o projeto de testes esperado é `backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj`.
- O CI `.github/workflows/dotnet-ci.yml` executa restore, build Release e testes em push e pull request com SDK `10.0.x` preview, necessário para projetos `net10.0`.
- No ambiente desta consolidação local, o comando `dotnet --info` ficou bloqueado porque o SDK .NET não está instalado no container; a validação executável fica coberta pelo GitHub Actions até o SDK estar disponível na estação.

### Configuração segura

- O banco padronizado para desenvolvimento e homologação é `plantaopro`.
- `appsettings.json` e `appsettings.example.json` usam placeholders (`Password=CHANGE_ME` e `Jwt:Key=CHANGE_ME_WITH_32+_CHARS`).
- Em desenvolvimento, use `dotnet user-secrets` para sobrescrever `ConnectionStrings:Default` e `Jwt:Key`.
- Em produção, use variáveis de ambiente/secret manager. Não versionar senha, token, JWT real, connection string real ou chaves de integrações.

### Mobile médico MVP

- O app Expo possui fluxo autenticado com Login, Dashboard, Plantões Disponíveis, Convites, Detalhe do Convite, Minhas Escalas, Pagamentos, Notificações, Perfil, Disponibilidade e Preferências.
- A API base deve ser informada por `EXPO_PUBLIC_API_BASE_URL`; não há URL fixa de produção.
- O armazenamento de JWT está encapsulado em storage compatível e documentado como parcial para hardening com secure storage nativo quando a política do registry permitir instalar dependências adicionais.

## Homologação local Saúde 360

Status em 2026-07-07: **Release Candidate parcial** até validação real de build/test/banco/QA em ambiente completo.

Comandos esperados:

```bash
docker compose up -d
scripts/database/apply-local-postgres.sh --host localhost --port 5432 --database plantaopro --user postgres --password 123456
dotnet restore backend/PlantaoPro.sln
dotnet build backend/PlantaoPro.sln -c Release
dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release
```

Health checks: `GET /`, `GET /api/health`, `GET /api/health/db`. O mobile usa `EXPO_PUBLIC_API_BASE_URL` e deve ser validado em sessão Expo/Metro interativa.
