# Notificações inteligentes

A fase 4 acrescenta preferências por usuário e reprocessamento de fila:

- `GET /api/notificacoes/preferencias`
- `PUT /api/notificacoes/preferencias`
- `POST /api/notificacoes/reprocessar-pendentes`

A migration cria regras, fila, eventos, destinatários e preferências para suportar eventos como convite, risco operacional, substituição, pagamento e fatura.
