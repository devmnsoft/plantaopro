# R8-B1 — "Plantões do mês" com filtro de mês corrente (+ bloqueadores da mesma statement)

Data: 2026-10-11 · Base: `a607c75` (R7-D-F6). Primeiro corte dos bugs R8 (ordem
quebrado → ausente-alto-impacto, conforme §4 de `r8a0-auditoria-inicial-matriz-estado.md`).

## Problema (achado r8a0 §27 / r8a0-varredura-indicadores)

O KPI `plantoes_mes` de `Fase6BiIntegracoesService.DashboardAsync` (endpoints
`api/bi/operacional|clinico|convenios`) era rotulado **"Plantões do mês"**, mas o SQL
contava **todos** os plantões ativos do tenant — sem filtro de mês. Ao executar a correção,
dois defeitos bloqueadores adicionais na MESMA statement vieram à tona (o endpoint 500-ava
antes mesmo de exibir qualquer KPI, então o bug do label nunca era observável em runtime):

1. **Tabela inexistente**: `total_receber` referenciava
   `plantaopro.financeiro_contas_receber.valor` — a tabela não existe no schema
   (`42P01`), derrubando o dashboard inteiro para todo perfil não-saas;
2. **Leitura dinâmica zerando valores**: o trio de KPIs era lido via
   `QueryFirstOrDefaultAsync` não-generica (dynamic/DapperRow); nesta forma o acesso
   resolvia nulo e os três KPIs saíam 0 mesmo com a linha presente (provo em execução
   durante o desenvolvimento: contagem direta do SQL = 1, leitura dinâmica = nulo).

## Solução

| Peça | O que mudou | Evidência |
|---|---|---|
| `Fase6BiIntegracoesServices.cs` (DASHBOARDASYNC, ramo tenant) | filtro de mês: `data_inicio >= date_trunc('month', now()) and data_inicio < date_trunc('month', now()) + interval '1 month'`; `total_receber` → `sum(valor_pendente)` de `clinica_contas_receber` (canônica R4B8); leitura do trio via tupla tipada `(long Plantoes, long Agendamentos, decimal Receber)` — padrão já usado pelo ramo saas na mesma classe | teste novo |
| Testes `R8B1Fase6BiPlantoesDoMesTests.cs` (novo) | 1 fato: 1 plantão do tenant com início no mês corrente + 3 distrações (mês anterior / outro tenant / registro inativo) → KPI vale exatamente 1; rótulo preservado; demais KPIs do painel presentes | `[números abaixo]` |

## Decisões registradas

1. **Corrigir o FILTRO (e não o label)**: rótulo "Plantões do mês" E código `plantoes_mes`
   já declaravam a intenção mensal — duas evidências contra uma (só a SQL faltava o filtro).
   Janela = **mês corrente**, limite superior exclusivo (`[inicio, fim)`), calculada no banco
   com `now()` (mesmo relógio dos dados).
2. **Sem filtro de status de negócio** no plantão (ex.: `status not in ('rascunho','cancelado')`):
   essa semântica pertence à calculadora BI (`BiServices.PlantoesPublicadosMes`) e não era
   declarada neste KPI — aplicar aqui seria inventar regra (canon: sem validação/proposta
   além da classificação existente). Mantido só `reg_status='A'` + tenant + janela.
3. **`total_receber` = `sum(valor_pendente)` sobre `clinica_contas_receber` c/
   `reg_status='A'`**: a tabela é a canônica do financeiro (consolidada em R4B8);
   `valor_pendente` é o campo "a receber" por construção (conta quitada contribui 0),
   dispensando whitelist de status inventada.
4. **Tupla tipada em vez de dynamic**: elimina o binding dinâmico frágil e alinha o ramo
   tenant ao ramo saas da mesma classe (que já usava tupla).

## Validação

- Run direcionado: **R8B1 1/1** (~0.6 s).
- Builds Web+Tests `-m:1 -nodeReuse:false`: **0 erros**.
- Pipeline v2: gate `phaseA` **52/52**; full ×3 = **1277 / 1275+2 / 1277**. As 2 falhas do
  full1 foram `Npgsql 40P01 (impasse)` em `ProvisionamentoB5Tests.PurgarAsync()/Dispose()`
  — categoria ① do catálogo de flakes; **provadas transientes** por rerun isolado da classe:
  **11/11 aprovados** (a classe passou também nos outros dois fulls).
- Baseline da suíte sobe de 1276 para **1277** (+1 fato deste corte).

**Estado honesto**: IMPLEMENTADO → TESTADO. AINDA NÃO HOMOLOGADO em host vivo e não LIBERADO.
Commit local; push diferido por decisão da rodada.
