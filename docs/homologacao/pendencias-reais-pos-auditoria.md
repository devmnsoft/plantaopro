# Pendências reais pós-auditoria — RC julho/2026

## Bloqueado por ambiente

- Instalar SDK .NET 10 no runner/local para executar `dotnet restore`, `dotnet build` e `dotnet test`.
- Executar PostgreSQL real e aplicar `database/PlantaoPro_PostgreSQL_Completo.sql`, migrations e seeds.
- Liberar registry npm para validar `npm install` e `npm run start` do app Expo.

## Parcial

- Homologação ponta a ponta Saúde 360 em Web/API com perfis RECEPCAO, TRIAGEM, MEDICO, FINANCEIRO_CLINICA, COORDENADOR_CLINICO, ADMINISTRADOR_CLINICA e AUDITOR_CLINICO.
- Homologação de plantões/escalas/financeiro médico com validação de conflitos, duplicidade de pagamento, justificativas, auditoria e notificações.
- Secure storage nativo no mobile para persistência criptografada de JWT.

## Pendente antes de piloto produtivo

- Evidência de Swagger, `/api/health`, login Web e menus sem 404 em ambiente executável.
- Relatório SQL com aplicação real das migrations/seeds e correção de eventuais 42P01/42703.

## Pendências reais pós-homologação 2026-07-07

- Validar `dotnet restore`, `dotnet build` e `dotnet test` no GitHub Actions com SDK 10 preview.
- Executar PostgreSQL local via Docker e aplicar `scripts/database/apply-local-postgres.*` com `psql`.
- Executar roteiro QA completo por perfil e registrar evidências.
- Homologar Expo/Metro em ambiente interativo e substituir storage em memória por secure storage quando possível.

## Rodada 2026-07-07 — CI/runtime/homologação final

Classificação honesta desta rodada: **Bloqueado por ambiente**.

### Execuções realizadas

- `dotnet --info`, `dotnet restore backend/PlantaoPro.sln`, `dotnet build backend/PlantaoPro.sln -c Release` e `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release`: bloqueados porque `dotnet não encontrado` no executor.
- `docker compose config`, `docker compose up -d` e `docker compose ps`: bloqueados porque `docker não encontrado` no executor.
- `bash scripts/database/apply-local-postgres.sh`: bloqueado porque `psql não encontrado` no executor.
- `npm install` em `mobile/PlantaoPro.App`: executado com sucesso.
- `CI=1 npm run start` em `mobile/PlantaoPro.App`: Expo/Metro iniciou, mas falhou com `TypeError: fetch failed` no ambiente não interativo/rede.

### Correções aplicadas

- Corrigido arquivo `backend/PlantaoPro.sln`, removendo bloco duplicado inválido após `EndGlobal`.
- Atualizado workflow `.github/workflows/dotnet-ci.yml` para incluir diagnóstico `dotnet --info` antes do restore/build/test.
- Criados scripts reproduzíveis `scripts/smoke/smoke-api.sh` e `scripts/smoke/smoke-api.ps1` para `/`, `/api/health`, `/api/health/db`, `/swagger`, login admin e endpoint autenticado `/api/usuarios/me` sem expor token.

### Comandos de revalidação

```bash
dotnet --info
dotnet restore backend/PlantaoPro.sln
dotnet build backend/PlantaoPro.sln -c Release
dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release
docker compose config
docker compose up -d
bash scripts/database/apply-local-postgres.sh
bash scripts/smoke/smoke-api.sh
cd mobile/PlantaoPro.App && npm install && CI=1 npm run start
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

## Rodada runtime real — 2026-07-08

- Corrigida a duplicidade de `AgendamentosController`: permanece apenas o controller consolidado em `Saude360WebControllers.cs`, herdando `Saude360WebControllerBase` e consumindo `Saude360WebService`.
- Operação Inteligente deixou de usar demo como fonte padrão na Web e agora consome `GET /api/operacao-inteligente/resumo`; fallback demo somente com `DemoMode=true`.
- `OperacaoRecomendacaoService` foi direcionado para PostgreSQL/Dapper com consultas reais e tratamento amigável quando tabelas/colunas ainda não existirem no ambiente.
- Dashboards por perfil receberam contratos API reais em `/api/dashboards/*`; a camada Web premium ainda deve ser evoluída para visualização rica completa.
- Validação de build/test permanece bloqueada neste container por ausência de SDK `dotnet`.
- PR #222 deve ser considerada superada por esta rodada para os itens de Agendamentos e Operação Inteligente; trechos úteis devem ser reaproveitados manualmente apenas após rebase/validação, pois a PR estava antiga e não mergeável.


## Pendências reais pós-RC UX/QA

- Validar `dotnet restore/build/test` em ambiente com SDK .NET.
- Validar Docker Compose, migrations, seeds e smoke com PostgreSQL.
- Fechar QA real dos fluxos Saúde 360, Plantões/Escalas/Financeiro e Mobile Expo.
- PR #222: sem acesso local a remoto/metadata para fechamento; considerada superada pela evolução atual e deve ser encerrada no GitHub com comentário.

## Atualização homologação CRUDs, ações e jornadas — 2026-07-09

Classificação geral desta rodada: **Funcional pendente QA** com execução runtime **Bloqueado por ambiente** quando não houver SDK .NET, Docker e PostgreSQL.

- CRUDs e rotas principais mapeados para validação: Pacientes, Agendamentos, Painel de Chamada, Triagem, Consultas, CID, Prescrições, Financeiro Clínica, Convênios, Planos de Saúde, Plantões, Escalas, Financeiro Médico, Notificações, Relatórios, Ajuda e Primeiros Passos.
- Smoke Web/API ampliado para endpoints e telas principais; o critério bloqueia `404` e `500` e aceita `302` em rotas protegidas sem sessão.
- Testes contratuais adicionados para controllers, actions Create/Edit/Details, endpoints API, rotas de menu, padrões proibidos, segredos, mobile e docs.
- Pendências reais: executar QA ponta a ponta com massa PostgreSQL por perfil, validar auditoria de ações críticas, restrições LGPD/RBAC e transições de status em runtime.
- Não declarar produção.

## Pendências runtime real — 2026-07-09

- Classificação: **Bloqueado por ambiente / Funcional pendente QA**.
- Executar build/test com SDK .NET disponível.
- Subir Docker/PostgreSQL, aplicar migrations/seeds e rodar smoke real.
- Validar CRUDs e RBAC com usuários demo via variáveis de ambiente, sem versionar senha real.
