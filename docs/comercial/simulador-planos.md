# Simulador de planos

## Implementado
Implementado em `/simulador`, `/simulador/resultado` e nas APIs `/api/public/simulador/perguntas`, `/calcular` e `/gerar-lead`. Recomenda Essencial, Profissional ou Enterprise por volume e módulos.

## Operação e segurança
As rotas usam controllers e services registrados no DI quando há API. As ações críticas registram auditoria via serviço central e não expõem stack trace ao usuário.

## Pendências reais
Pendências reais: calibrar preços finais com comercial e ligar histórico ao banco.
