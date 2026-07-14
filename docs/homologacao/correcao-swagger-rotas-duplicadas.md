# Correção Swagger: rotas duplicadas de dashboard

## Causa

O Swagger/OpenAPI falhava ao gerar o contrato porque duas actions expunham a mesma combinação de método e rota: `GET /api/dashboard`.

Controllers envolvidos:

- `DashboardController.Dashboard`, rota principal do produto.
- `V112HomologationController.Dashboard`, rota legada de homologação v1.12.

O OpenAPI exige unicidade por método HTTP e path. Portanto, a solução correta é remover a duplicidade de rota, não mascarar o problema com `ResolveConflictingActions`.

## Decisão

- A rota principal permanece em `GET /api/dashboard` no `DashboardController`.
- O dashboard legado v1.12 passa a responder em `GET /api/v112/dashboard`.
- A rota versionada v1.13 já existente em `GET /api/v113/dashboard` foi preservada para compatibilidade da linha operacional seguinte.

## Compatibilidade

A única quebra intencional é para clientes que ainda chamavam o dashboard legado v1.12 por `GET /api/dashboard`. Esses clientes devem migrar para `GET /api/v112/dashboard`. O dashboard principal continua disponível em `GET /api/dashboard`.

## Validação

A correção adiciona um teste contratual de unicidade para garantir que:

- `GET api/dashboard` existe uma única vez e pertence ao controller principal.
- `V112HomologationController` não expõe mais `api/dashboard`.
- `GET api/v112/dashboard` existe como rota versionada.
- Dashboards versionados não conflitam com a rota principal.

O smoke também passa a validar `/swagger/v1/swagger.json`, falhando se o Swagger retornar erro 500.

## Pendências reais

- Revalidar o smoke completo em ambiente com API, PostgreSQL e JWT configurados.
- Não declarar produção antes de build, testes, smoke e QA manual por perfil em ambiente homologável.
