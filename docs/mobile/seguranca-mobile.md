# Segurança mobile

Os endpoints `/api/mobile/*` exigem JWT, exceto login mobile.

## Regras

- Plano precisa permitir mobile.
- Cliente precisa estar identificado por `cliente_id`.
- Médico deve operar somente seus dados.
- Payload deve ser leve e sem dados sensíveis.
- Acesso negado por plano/tenant gera auditoria em `API_MOBILE`.

## Testes mínimos

- Sem token retorna 401.
- Token válido sem plano mobile retorna 403.
- Médico não acessa dados de outro médico.
