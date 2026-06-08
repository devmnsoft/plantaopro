# Central de plantões

## Implementado
A versão comercial reforça a demonstração da central com filtros, status visual, alertas e links para fluxo plantão-convite-escala-pagamento existente.

## Operação e segurança
As rotas usam controllers e services registrados no DI quando há API. As ações críticas registram auditoria via serviço central e não expõem stack trace ao usuário.

## Pendências reais
Pendências reais: evoluir calendário drag-and-drop.
