# Assinaturas SaaS

## Ciclo de vida

- `TRIAL`: período de teste.
- `ATIVA`: operação liberada.
- `SUSPENSA`: operação parcialmente bloqueada.
- `CANCELADA`: operação bloqueada.
- `VENCIDA`: operação bloqueada para fluxos críticos.

## Regras

Cada cliente pode ter apenas uma assinatura `ATIVA` ou `TRIAL` por vez. Planos inativos não devem ser usados em novas assinaturas. Alterações de plano e status devem registrar histórico e auditoria.

## Funcionalidade entregue nesta rodada

- CRUD Web em `Assinaturas/Index`, `Assinaturas/Create`, `Assinaturas/Edit`, `Assinaturas/Details`, `Assinaturas/Uso` e `Assinaturas/AlterarPlano`.
- API de assinatura atual por cliente em `GET /api/assinaturas/cliente/{clienteId}/atual`.
- API de alteração de plano em `POST /api/assinaturas/{id}/alterar-plano`, com justificativa, histórico e auditoria.
- Reativação de assinatura exige justificativa para manter rastreabilidade comercial.
