# Propostas comerciais

## Implementado
APIs de CRUD, gerar itens, enviar, aprovar, recusar, converter e preview foram adicionadas. Web possui listagem demonstrável, formulário e preview imprimível.

## Operação e segurança
As rotas usam controllers e services registrados no DI quando há API. As ações críticas registram auditoria via serviço central e não expõem stack trace ao usuário.

## Pendências reais
Pendências reais: persistência definitiva, geração PDF server-side e workflow de aprovação avançado.
