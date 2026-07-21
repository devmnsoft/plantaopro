# PlantãoPro
Projeto full-stack para gestão de plantões médicos com backend ASP.NET Core (DDD/Clean), PostgreSQL e aplicativo móvel multiplataforma.
## Estrutura
- backend/ (API, Web, Domain, Application, Infrastructure, CrossCutting)
- database/ (script completo + seeds)
- mobile/PlantaoPro.App (TypeScript mobile)
- docs/
## Instalação limpa oficial do banco (v1.18.4)

O ponto oficial para banco novo é `database/scrpt_completo.sql` (grafia intencional). Não execute dezenas de migrations manualmente para uma instalação nova.

```bash
createdb plantaopro

psql \
  -v ON_ERROR_STOP=1 \
  -U postgres \
  -d plantaopro \
  -f database/scrpt_completo.sql
```

O banco de dados deve existir antes do `psql`; o script não cria usuários PostgreSQL, não contém senha real, não cria administrador com senha conhecida e não inclui seeds de demonstração.

## Execução
1. Configure a connection string por variável de ambiente ou user-secrets, por exemplo `ConnectionStrings__Default="Host=localhost;Port=5432;Database=plantaopro;Username=<usuario>;Password=<senha>"`.
2. Configure JWT por segredo seguro (`Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`); não versionar chaves reais.
3. Crie o administrador inicial por fluxo seguro da aplicação ou comando operacional interno com senha temporária rotacionada fora do repositório.
4. Backend API: `dotnet run --project backend/PlantaoPro.Api`.
5. Web: `dotnet run --project backend/PlantaoPro.Web`.
6. Mobile: `cd mobile/PlantaoPro.App && npm ci && npm run dev`.

## Backend MVP (rodada atual)
Foram adicionados endpoints reais para hospitais, especialidades, plantões, escalas (aceitar), financeiro (gerar pagamento), notificações e dashboard usando Dapper/PostgreSQL.

## Novos fluxos backend (Escalas/Financeiro/Notificações/Dashboard)
- Endpoints de escalas: `/api/escalas`, `/api/escalas/{id}`, `/api/medicos/me/plantoes`, `/api/plantoes/{id}/aceitar`, `/api/escalas/{id}/confirmar|recusar|cancelar|substituir|marcar-realizado`.
- Endpoints financeiros: `/api/financeiro/pagamentos`, `/api/financeiro/pagamentos/{id}`, `/api/financeiro/pagamentos/gerar`, `/api/financeiro/pagamentos/{id}/confirmar|cancelar`, `/api/financeiro/meus-pagamentos`.
- Endpoints notificações: `/api/notificacoes`, `/api/notificacoes/nao-lidas`, `PUT /api/notificacoes/{id}/lida`, `PUT /api/notificacoes/lidas`.
- Dashboard principal: `GET /api/dashboard` e `GET /api/mobile/home` com indicadores/listas/gráficos.
- Dashboard legado v1.12: `GET /api/v112/dashboard`; a rota foi versionada para evitar duplicidade no Swagger/OpenAPI com o dashboard principal.
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
scripts/database/apply-local-postgres.sh --host localhost --port 5432 --database plantaopro --user postgres --password "$PGPASSWORD"
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

## Homologação funcional CRUDs, ações e jornadas — 2026-07-09

Classificação: **Funcional pendente QA / Bloqueado por ambiente local**. A rodada reforça contratos, smoke e documentação para validar CRUDs de Pacientes, Agendamentos, Painel de Chamada, Triagem, Consultas, CID, Prescrições, Financeiro Clínica, Convênios, Planos de Saúde, Plantões, Escalas, Financeiro Médico, Notificações e Mobile Médico.

- Smoke `scripts/smoke/smoke-api.sh` cobre API e Web principais; rotas protegidas podem retornar `302`, mas `404` e `500` falham.
- Testes contratuais verificam controllers Web, endpoints principais, menu sem rota órfã conhecida, ausência de padrões MVC proibidos, ausência de segredos em `appsettings`, mobile com `EXPO_PUBLIC_API_BASE_URL` e sem `Alert.alert`.
- Não declarar produção até executar build/test, Docker/PostgreSQL, migrations/seeds e QA manual por perfil.

Homologação funcional implementada e preparada para validação em ambiente com SDK .NET, Docker e PostgreSQL.

## Homologação runtime real — 2026-07-09

Classificação final: **Bloqueado por ambiente / Funcional pendente QA**. A preparação de runtime foi registrada em `docs/homologacao/evidencia-runtime-real.md`; neste executor não há SDK .NET, Docker, PostgreSQL/psql nem `gh`, então a validação real permanece pendente em ambiente homologável. Não declarar produção.

## v1.13 — persistência real para homologação funcional

A v1.13 mantém compatibilidade com as rotas operacionais v1.12, mas direciona o fluxo para endpoints versionados `/api/v113/*` com `ApiResponse<T>`, autenticação e persistência PostgreSQL via Dapper. O módulo não declara produção: o status é homologável, com boleto demonstrativo explícito e outbox persistida sem envio externo real.

Arquivos principais:

- Migration idempotente: `database/migrations/2026_v113_operacional_real.sql`.
- Seed demo seguro: `database/seeds/2026_demo_v113_operacional.sql`.
- Serviço operacional: `backend/PlantaoPro.Api/V113OperationalService.cs`.
- Smoke: `scripts/smoke-test-v113.sh` e `scripts/smoke-test-v113.ps1`.
- CI runtime: job `runtime-e2e-v113` em `.github/workflows/dotnet-ci.yml`.


## v1.14 — Consolidação de produto

A v1.14 consolida clientes/produtos/pedidos/faturamento genéricos em linguagem PlantãoPro: Itens Faturáveis, Faturamento Clínico, Operação Inteligente 3.0, Jornada por Perfil, favoritos, timelines e mobile médico. Consulte `docs/v1.14-consolidacao-produto.md`.

## v1.15 — regras reais de faturamento, repasses e jornadas

A v1.15 adiciona migration/seed, motor de faturamento, motor de repasse médico, glosas por convênio, alertas financeiros, smoke e documentação. Boleto real continua fora do escopo; apenas demo-boleto explícito é permitido. Integrações externas são dependentes de provedor e não devem ser apresentadas como produção.

## v1.16 — Consolidação operacional final

A v1.16 adiciona consolidação operacional para convênios, autorizações, guias, lotes de faturamento, caixa, recebimentos parciais, estornos, timelines, notificações operacionais e relatórios executivos. Status: funcional pendente QA; integrações externas permanecem dependentes de provedor e não há declaração de produção.


### Swagger/OpenAPI e rotas de dashboard

O contrato OpenAPI exige uma única action por combinação de método HTTP e rota. Por isso, `GET /api/dashboard` fica reservado ao `DashboardController` principal. Rotas legadas de homologação devem ser versionadas, como `GET /api/v112/dashboard`, evitando conflitos como `Conflicting method/path combination "GET api/dashboard"`. Consulte `docs/homologacao/correcao-swagger-rotas-duplicadas.md`.

## Configuração JWT obrigatória

A API exige `Jwt__Key`, `Jwt__Issuer` e `Jwt__Audience`. Em desenvolvimento local, use user-secrets ou variável de ambiente. A chave deve ter pelo menos 32 caracteres. Nunca versione segredo real. Consulte `docs/configuracao-jwt-local-ci-iis.md`.

## v1.17 hardening runtime

A v1.17 fortalece a base v1.16 sem declarar produção: Swagger sem conflito de dashboard, logging de status final por middleware, claims `uid` robustas, serviços v1.16 persistidos via PostgreSQL/Dapper, smoke bloqueante v1.16/v1.17, rotas legadas versionadas e documentação honesta de QA por perfil.
