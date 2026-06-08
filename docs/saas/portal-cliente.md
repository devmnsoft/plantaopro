# Portal cliente

## Implementado
ClientePortal consolida plano, uso, faturas, onboarding, suporte, treinamento e white label. API respeita escopo autenticado.

## Operação e segurança
As rotas usam controllers e services registrados no DI quando há API. As ações críticas registram auditoria via serviço central e não expõem stack trace ao usuário.

## Pendências reais
Pendências reais: detalhar isolamento por claims reais em todos os widgets.
