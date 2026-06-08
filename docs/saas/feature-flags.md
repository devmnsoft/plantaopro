# Feature flags

## Implementado
API `/api/feature-flags` permite habilitar flags globais ou por tenant, com justificativa e auditoria.

## Operação e segurança
As rotas usam controllers e services registrados no DI quando há API. As ações críticas registram auditoria via serviço central e não expõem stack trace ao usuário.

## Pendências reais
Pendências reais: criar middleware/filtro para flags por recurso.
