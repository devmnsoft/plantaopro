# Segurança Mobile

A API mobile exige JWT em todos os endpoints exceto login. O acesso valida plano mobile ativo por cliente e registra auditoria quando negado.

## Regras

- `/api/mobile/me` e demais endpoints autenticados exigem token.
- Médico visualiza apenas dados próprios e do cliente da sessão.
- Plantões e solicitações respeitam `cliente_id`.
- Payloads são leves e paginados quando aplicável.
