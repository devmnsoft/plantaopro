# Disponibilidade médica

## Implementado
Roteiro de demo contempla disponibilidade, indisponibilidade e sugestão de médico disponível via módulos operacionais já expostos.

## Operação e segurança
As rotas usam controllers e services registrados no DI quando há API. As ações críticas registram auditoria via serviço central e não expõem stack trace ao usuário.

## Pendências reais
Pendências reais: persistência granular de preferências por médico.
