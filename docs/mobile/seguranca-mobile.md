# Segurança Mobile

A API mobile exige JWT em todos os endpoints exceto login. O acesso valida plano mobile ativo por cliente e registra auditoria quando negado.

## Regras

- `/api/mobile/me` e demais endpoints autenticados exigem token.
- Médico visualiza apenas dados próprios e do cliente da sessão.
- Plantões e solicitações respeitam `cliente_id`.
- Payloads são leves e paginados quando aplicável.

## Auditoria mobile

O login mobile registra `LOGIN_SUCESSO` ou `LOGIN_FALHA` na entidade `API_MOBILE`. Bloqueio por plano ou tentativa de acessar plantão fora do cliente registra `ACESSO_NEGADO` com mensagem amigável para o aplicativo.
