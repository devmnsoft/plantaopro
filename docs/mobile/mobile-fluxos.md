# API Mobile MVP — fluxos

## Login
1. App envia e-mail/senha para `POST /api/mobile/auth/login`.
2. API retorna JWT, validade e roles.
3. App usa `Authorization: Bearer {token}` nos demais endpoints.

## Solicitar plantão
1. Médico lista `GET /api/mobile/plantoes-disponiveis`.
2. App abre `GET /api/mobile/plantoes/{id}`.
3. App solicita em `POST /api/mobile/plantoes/{id}/solicitar`.
4. API valida médico autenticado, duplicidade, vaga e conflito crítico.

## Convite
1. Médico lista convites.
2. Aceita ou recusa informando motivo quando aplicável.
3. API registra auditoria e cria notificação operacional.

## Pagamentos
1. Médico consulta `GET /api/mobile/meus-pagamentos`.
2. Status `pago` indica confirmação financeira.
3. Contestação deve usar o fluxo financeiro web/API quando disponível para o cliente.
