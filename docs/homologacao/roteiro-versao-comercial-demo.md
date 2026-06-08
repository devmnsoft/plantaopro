# Roteiro de homologação comercial

## Implementado
Validar: landing, planos, simulador, lead, demo, proposta, aprovação, conversão, portais, módulos, feature flags, demo mode, auditoria e build.

## Operação e segurança
As rotas usam controllers e services registrados no DI quando há API. As ações críticas registram auditoria via serviço central e não expõem stack trace ao usuário.

## Pendências reais
Pendências reais: ambiente atual sem SDK dotnet impede execução local automatizada nesta rodada.
