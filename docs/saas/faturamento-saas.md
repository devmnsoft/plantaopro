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
