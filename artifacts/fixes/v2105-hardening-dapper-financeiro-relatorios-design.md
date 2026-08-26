# v2.10.5 — hardening Dapper financeiro, relatórios e design

## Escopo e inventário auditado

A varredura foi feita com os padrões `QueryAsync`, `QueryFirst*`, `QuerySingle*` e `ExecuteScalarAsync`, com revisão dirigida para financeiro, pagamentos, relatórios e dashboards. Nesta rodada, o fluxo financeiro principal em `Data.cs` foi priorizado, sem criação de módulos.

| Método / endpoint | DTO Dapper | Query e aliases relevantes | Tenant |
|---|---|---|---|
| `FinanceiroService.ListarAsync` / `GET api/financeiro/pagamentos` e `resumo` | `PagamentoResumoDto` | seleção explícita de IDs, médico, hospital, especialidade, datas, `ValorPrevisto`, `ValorPago`, `ValorBruto`, `ValorLiquido`, `Descontos`, `Acrescimos`, status, liquidação e `RegDate` | `pg.tenant_id=@TenantId`, com exceção explícita para administrador global |
| `FinanceiroService.GetByIdAsync` / `GET api/financeiro/pagamentos/{id}` | `PagamentoDetailsDto` | aliases explícitos e homônimos para todas as propriedades, inclusive composição financeira | mesmo isolamento da listagem |
| `FinanceiroService.MeusAsync` e `MeuByIdAsync` / `meus-pagamentos` | `PagamentoResumoDto` e detalhe | descoberta parametrizada do médico, verificação de propriedade e reutilização da consulta isolada | contexto de tenant obrigatório; consulta do pagamento inclui `tenant_id` |
| `MedicoService.MeusPagamentosAsync` | `MedicoPagamentoDto` | aliases de pagamento, hospital, especialidade, datas e valores | escopo do médico autenticado (inventariado; permanece limitação descrita abaixo) |
| `DashboardService.GetAsync` | `DashboardDto`, `PagamentoResumoDto`, `DashboardChartItem` | indicadores, últimos pagamentos e séries | inventariado; o dashboard legado agrega dados globais e requer migração separada para um contexto de tenant injetado |

Também foram auditados os DTOs `DashboardDto`, `DashboardChartItem` e `MedicoPagamentoDto`: agora são classes graváveis com construtor padrão, mantendo construtores auxiliares somente onde havia chamadas existentes.

## Problemas encontrados e correções

- A listagem financeira não aplicava filtro de tenant, embora o detalhe já aplicasse.
- `PagamentoResumoDto.ValorPago` era não anulável e a SQL substituía ausência por zero, confundindo “não pago” com “pago no valor zero”.
- A composição solicitada (`ValorBruto`, `ValorLiquido`, `Descontos`, `Acrescimos`) não fazia parte dos DTOs/aliases do pagamento de plantão.
- A listagem não retornava `RegDate` e não filtrava forma de pagamento.
- DTOs de dashboard e pagamento do médico ainda dependiam de construtor posicional na materialização.
- A tela não oferecia filtros financeiros visíveis e o detalhe mostrava a chave Pix integralmente.

As queries continuam parametrizadas, sem SQL interpolado por valores, sem `SELECT *` no fluxo auditado e sem captura destinada a mascarar erro de materialização. A composição financeira é derivada do esquema vigente de pagamentos de plantão: bruto e líquido correspondem ao valor previsto, e descontos/acréscimos são zero até que o domínio passe a persistir esses componentes. Isso evita consultar colunas inexistentes e mantém o contrato explícito.

## Tipos, datas e nullability

- Datas de plantão e `RegDate` permanecem `DateTime`, pois são timestamps no PostgreSQL.
- `DataPrevista` e `DataPagamento` permanecem `DateOnly?`, pois são colunas `date` opcionais e não representam horário.
- `ValorPago` e `ValorLiquido` são `decimal?`: pagamento pendente pode não ter baixa e o contrato distingue ausência de zero.
- Nomes e dados vindos de joins são anuláveis nos DTOs detalhados; a listagem usa `coalesce` quando o contrato visual exige texto.
- IDs obrigatórios do lançamento permanecem `Guid`; nenhuma relação opcional foi artificialmente convertida em obrigatória.

## Cobertura e endpoints

Os testes de contrato v2.10.5 cobrem construtor padrão/propriedades graváveis, pagamento pendente, pago, sem data, sem valor pago, com líquido, aliases explícitos, parametrização, tenant, filtros e registro inexistente. O detalhe v2.10.4 permanece coberto pelos testes anteriores. Relatório/dashboard são protegidos contra regressão de forma do DTO; integração PostgreSQL real não é executável neste contêiner sem SDK/banco.

## Melhorias visuais

- Filtros responsivos por período, status e forma de pagamento, usando date pickers e selects (nenhum ID manual).
- Cards e tabela responsiva existentes foram preservados; valores ausentes continuam honestos, sem mock.
- Detalhe passou a exibir bruto, previsto, descontos, acréscimos, líquido e pago.
- Chave Pix é mascarada por padrão para reduzir exposição de dado pessoal.
- Estados vazio/erro, badges, paginação, formulários com labels e confirmações não bloqueantes do design system foram preservados.

## Validações executadas

- `dotnet clean backend/PlantaoPro.sln` — não executado: SDK `dotnet` indisponível no ambiente.
- `dotnet restore backend/PlantaoPro.sln` — não executado: SDK `dotnet` indisponível no ambiente.
- Builds Debug/Release e `dotnet test` — não executados pela mesma limitação real.
- `python3 scripts/repository-security-check.py` — aprovado (`repository-security ok`).
- `python3 scripts/check-csharp10-compatibility.py` — aprovado (`Compatibilidade C# 10 e CSS Razor validada`).
- `python3 scripts/validate-scrpt-completo.py` — aprovado (`ok: true`, cobertura 100%).
- Busca de padrões proibidos no escopo alterado e `git diff --check` — aprovados, sem ocorrências/erros. O comando amplo fornecido gera falsos positivos por incluir alternativas comuns e foi preservado como evidência de baseline.

## Limitações reais restantes

- O `DashboardService` legado ainda agrega várias tabelas sem contexto tenant próprio. Alterar sua assinatura e semântica transversal nesta correção preventiva elevaria o risco de quebrar administradores globais; deve ser migrado em rodada específica com testes PostgreSQL multi-tenant.
- O filtro por médico, hospital e especialidade continua suportado pela API, mas a página não possui ainda endpoints de lookup/autocomplete próprios para alimentar seletores; IDs não são expostos como campos manuais.
- Não há PostgreSQL de integração nem SDK .NET disponível neste ambiente. Assim, a materialização real deve ser confirmada no CI com banco migrado.
