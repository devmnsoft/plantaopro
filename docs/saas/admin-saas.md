# Admin SaaS

## Implementado
Painel web AdminSaas e API `/api/admin-saas/resumo` entregam KPIs comerciais/SaaS demonstráveis para MNSOFT.

## Operação e segurança
As rotas usam controllers e services registrados no DI quando há API. As ações críticas registram auditoria via serviço central e não expõem stack trace ao usuário.

## Pendências reais
Pendências reais: substituir agregados em memória por consultas otimizadas.
