# Landing page pública comercial

## Implementado
Implementada nas rotas `/`, `/plantaopro`, `/contato`, `/demo` e integrada às APIs públicas `/api/public/landing`, `/api/public/lead` e `/api/public/agendar-demo`.

## Operação e segurança
As rotas usam controllers e services registrados no DI quando há API. As ações críticas registram auditoria via serviço central e não expõem stack trace ao usuário.

## Pendências reais
Pendências reais: persistir leads no PostgreSQL em ambiente com migrations aplicadas e parametrizar textos via CMS.
