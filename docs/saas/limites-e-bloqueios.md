# Limites e bloqueios

Os guardas SaaS bloqueiam cliente suspenso/cancelado, assinatura inativa/vencida, limite de médicos, hospitais e plantões/mês, além de recursos não contratados como mobile, BI, integrações e relatórios avançados.

Cada bloqueio registra `cliente_bloqueios`, alerta em `cliente_alertas` e auditoria sem expor stack trace ao usuário.

## Aplicação operacional reforçada nesta rodada

- O acesso aos painéis de BI da API passa obrigatoriamente por `AssinaturaGuardService.PodeUsarBIAsync`, retornando `ApiResponse` amigável e registrando auditoria de acesso negado quando o plano não possui BI.
- A exportação CSV dos relatórios SaaS passa por `AssinaturaGuardService.PodeUsarRelatoriosAvancadosAsync`, evitando que planos sem relatórios avançados baixem dados consolidados.
- As validações continuam sem registrar segredos, tokens ou stack trace, e usam o cliente/perfil do contexto autenticado para manter isolamento multiempresa.
