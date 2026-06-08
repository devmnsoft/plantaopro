# Portal parceiro

## Implementado
ParceiroPortal traz leads, clientes, propostas, comissões, repasses, materiais e suporte sem dados clínicos sensíveis.

## Operação e segurança
As rotas usam controllers e services registrados no DI quando há API. As ações críticas registram auditoria via serviço central e não expõem stack trace ao usuário.

## Pendências reais
Pendências reais: persistir regras de comissão por contrato.
