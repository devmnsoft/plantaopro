# Limites e Bloqueios SaaS

Os limites são controlados por plano e assinatura.

## Bloqueios cobertos

- Cliente suspenso ou cancelado.
- Assinatura vencida/cancelada.
- Limite de médicos, hospitais e plantões do mês.
- Plano sem mobile, BI, API, integrações ou relatórios avançados.

Cada bloqueio deve gerar retorno amigável, auditoria, registro em `cliente_bloqueios` e alerta para regularização ou upgrade.

## Complemento 2026-06-08 — bloqueios críticos registrados

O guard de assinatura agora registra bloqueios SaaS e alertas quando encontra cliente suspenso, cliente cancelado, ausência de assinatura, assinatura cancelada, assinatura vencida ou assinatura sem status ativo. Esses bloqueios são persistidos em `cliente_bloqueios`, aparecem como alerta em `cliente_alertas` e geram auditoria central sem expor stack trace ao usuário.

Fluxos operacionais como publicação de plantão, cadastro de médico, cadastro de hospital e uso do mobile continuam retornando mensagens amigáveis via `ApiResponse<T>` com status 403 quando a regra comercial impede a operação.
