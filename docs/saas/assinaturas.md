# Assinaturas SaaS

## Ciclo de vida

- `TRIAL`: período de teste.
- `ATIVA`: operação liberada.
- `SUSPENSA`: operação parcialmente bloqueada.
- `CANCELADA`: operação bloqueada.
- `VENCIDA`: operação bloqueada para fluxos críticos.

## Regras

Cada cliente pode ter apenas uma assinatura `ATIVA` ou `TRIAL` por vez. Planos inativos não devem ser usados em novas assinaturas. Alterações de plano e status devem registrar histórico e auditoria.
