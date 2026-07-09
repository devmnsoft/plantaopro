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

## Status de homologação — rodada 2026-07-07

Classificação atual: **Bloqueado por ambiente**.

Nesta rodada foram preparados CI e scripts de smoke para validação real, mas o executor não possui `dotnet`, `docker` nem `psql`. Por isso, build/test, PostgreSQL, migrations/seeds, API, Web e smoke funcional completo não puderam ser aprovados localmente. O mobile executou `npm install`, mas `CI=1 npm run start` falhou com `TypeError: fetch failed` durante inicialização do Expo/Metro.

Revalidação mínima:

```bash
dotnet --info
dotnet restore backend/PlantaoPro.sln
dotnet build backend/PlantaoPro.sln -c Release
dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release
docker compose config
docker compose up -d
bash scripts/database/apply-local-postgres.sh
bash scripts/smoke/smoke-api.sh
```

## Evolução operação inteligente e demo comercial premium — 2026-07-07
- Evoluído dashboard executivo por perfil: Admin Global, Administrador Cliente, Coordenação, Médico, Financeiro e Saúde 360.
- Criado cockpit Operação Inteligente com pendências por prioridade, perfil responsável, CTA seguro e recomendações determinísticas sem IA externa.
- Criada jornada Primeiros Passos por perfil para implantação do tenant e operação diária.
- Agenda clínica recebeu visão comercial por cards para Calendário, AgendaDia, AgendaMedico e CheckIn.
- Relatórios gerenciais priorizam filtros, cards, LGPD e exportação futura bloqueada até auditoria.
- Demo premium documentada com usuários por perfil e seed idempotente `database/seeds/2026_demo_comercial_premium.sql`.
- Mobile médico mantém telas mínimas, fallback amigável, uso de `EXPO_PUBLIC_API_BASE_URL` e sem log de token.
- Classificação: Evolução funcional parcial no ambiente atual quando SDK .NET ou Docker não estiverem disponíveis; Demo premium navegável para apresentação.

### Atualização runtime real (2026-07-08)

- Operação Inteligente Web agora prioriza a API real `/api/operacao-inteligente/resumo`; modo demo exige `DemoMode=true`.
- Recomendações operacionais consultam PostgreSQL via Dapper e registram pendências amigáveis quando a estrutura de banco do ambiente ainda não estiver disponível.
- Dashboards premium por perfil possuem endpoints API iniciais em `/api/dashboards/admin-global`, `/admin-cliente`, `/coordenacao`, `/medico`, `/financeiro` e `/saude360`.
- O controller duplicado de Agendamentos foi removido; a rota Web permanece centralizada no controller Saúde 360.


## Status pós-RC UX/QA

Classificação atual: **Funcional pendente QA** para versão homologável/demonstrável, sem declaração de produção. Dashboards Web premium consomem `/api/dashboards/*`; agenda clínica premium consome `/api/agendamentos*`; smoke cobre health, login, `/api/usuarios/me`, Operação Inteligente e dashboards. PR #222 foi considerada superada/documentada nesta rodada porque o repositório local não expõe remoto/branch da PR para fechamento automático.
