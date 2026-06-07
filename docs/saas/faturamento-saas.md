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
