# Governança de módulos

## Implementado
APIs `/api/modulos` controlam módulos globais, beta, ocultos e habilitação por tenant com auditoria.

## Operação e segurança
As rotas usam controllers e services registrados no DI quando há API. As ações críticas registram auditoria via serviço central e não expõem stack trace ao usuário.

## Pendências reais
Pendências reais: aplicar bloqueio transversal em todos endpoints legados.
