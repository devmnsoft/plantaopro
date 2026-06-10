# Comunicação operacional interna

A comunicação interna existente foi complementada com:

- Detalhe de conversa: `GET /api/comunicacao/conversas/{id}`
- Encerramento: `POST /api/comunicacao/conversas/{id}/encerrar`
- Templates: `GET/POST /api/comunicacao/templates`

Acesso ao detalhe exige participação na conversa. Templates são registrados com auditoria.
