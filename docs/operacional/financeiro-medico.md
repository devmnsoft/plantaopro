# Financeiro médico

## Implementado
A jornada demonstrável cobre pagamento previsto, confirmado, contestação, histórico e exportação como etapa final do plantão.

## Operação e segurança
As rotas usam controllers e services registrados no DI quando há API. As ações críticas registram auditoria via serviço central e não expõem stack trace ao usuário.

## Pendências reais
Pendências reais: conciliação bancária e nota fiscal.
