# Demo mode comercial

## Implementado
APIs `/api/demo/gerar-dados`, `/limpar-dados`, `/status` e `/roteiros` gerenciam dados fictícios marcados como demo.

## Operação e segurança
As rotas usam controllers e services registrados no DI quando há API. As ações críticas registram auditoria via serviço central e não expõem stack trace ao usuário.

## Pendências reais
Pendências reais: ampliar seed relacional após execução em banco real.
