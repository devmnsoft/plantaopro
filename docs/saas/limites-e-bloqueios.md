# Limites e bloqueios

Os guardas SaaS bloqueiam cliente suspenso/cancelado, assinatura inativa/vencida, limite de médicos, hospitais e plantões/mês, além de recursos não contratados como mobile, BI, integrações e relatórios avançados.

Cada bloqueio registra `cliente_bloqueios`, alerta em `cliente_alertas` e auditoria sem expor stack trace ao usuário.

## Aplicação operacional reforçada nesta rodada

- O acesso aos painéis de BI da API passa obrigatoriamente por `AssinaturaGuardService.PodeUsarBIAsync`, retornando `ApiResponse` amigável e registrando auditoria de acesso negado quando o plano não possui BI.
- A exportação CSV dos relatórios SaaS passa por `AssinaturaGuardService.PodeUsarRelatoriosAvancadosAsync`, evitando que planos sem relatórios avançados baixem dados consolidados.
- As validações continuam sem registrar segredos, tokens ou stack trace, e usam o cliente/perfil do contexto autenticado para manter isolamento multiempresa.

## Fechamento 2026-06-08 — limites operacionais adicionais

Nesta rodada o motor `AssinaturaGuardService` passou a tratar também limites mensais de convites e limite de usuários, além dos limites já existentes de médicos, hospitais e plantões. A validação usa a assinatura ativa do cliente, registra bloqueio em `cliente_bloqueios`, gera alerta em `cliente_alertas` e devolve mensagem amigável em `ApiResponse<T>` sem expor exceção técnica ao usuário.

Recursos premium também foram explicitados no cadastro de planos:

- `permite_operacao_assistida`: libera o módulo de operação assistida para o cliente.
- `permite_suporte_prioritario`: libera chamados de suporte com prioridade ALTA ou CRITICA.
- `limite_usuarios`: controla o crescimento administrativo do cliente; `0` representa ilimitado.
- `limite_convites_mes`: controla convites enviados no mês; `0` representa ilimitado.

Quando um recurso premium não está contratado, a API retorna bloqueio 403 com CTA operacional para regularização/upgrade nas telas consumidoras.
