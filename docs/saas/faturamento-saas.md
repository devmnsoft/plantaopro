# Faturamento SaaS

## Faturas

Status: `ABERTA`, `PAGA`, `VENCIDA`, `CANCELADA`, `EM_CONTESTACAO`.

## Operações

- Gerar mensal: `POST /api/faturamento-saas/gerar-mensal`.
- Marcar paga: `POST /api/faturamento-saas/faturas/{id}/marcar-paga`.
- Cancelar: `POST /api/faturamento-saas/faturas/{id}/cancelar`.
- Contestar: `POST /api/faturamento-saas/faturas/{id}/contestar`.
- Inadimplência: `GET /api/faturamento-saas/inadimplencia`.

Faturas duplicadas por assinatura e competência são bloqueadas por constraint única.

## Contestação

A contestação é iniciada por `POST /api/faturamento-saas/faturas/{id}/contestar` e resolvida por `POST /api/faturamento-saas/faturas/{id}/resolver-contestacao`. A resolução exige resposta comercial e registra auditoria antes de retornar a fatura para cobrança aberta.

## Fechamento funcional auditável — 2026-06-08

O detalhe da fatura SaaS no Web agora concentra as ações operacionais de cobrança com formulários reais e proteção antiforgery:

- notificar cobrança;
- marcar fatura como paga;
- contestar fatura;
- resolver contestação;
- cancelar fatura com justificativa.

Na API, ações críticas de faturamento usam transação explícita para atualizar fatura, registrar pagamento quando aplicável, registrar evento de cobrança e criar ou resolver alertas financeiros. Falhas retornam mensagens funcionais e são registradas em logger/auditoria sem expor stack trace ao usuário.
