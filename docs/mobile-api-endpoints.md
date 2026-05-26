# Endpoints Mobile (MVP Piloto)

## Auth
- `POST /api/mobile/auth/login`

## Sessão e perfil
- `GET /api/mobile/me`
- `GET /api/mobile/perfil`
- `PUT /api/mobile/perfil`

## Dashboard e operação
- `GET /api/mobile/dashboard`
- `GET /api/mobile/plantoes-disponiveis`
- `GET /api/mobile/convites`
- `POST /api/mobile/convites/{id}/aceitar`
- `POST /api/mobile/convites/{id}/recusar`
- `GET /api/mobile/minhas-escalas`

## Financeiro
- `GET /api/mobile/meus-pagamentos`

## Notificações
- `GET /api/mobile/notificacoes`
- `PUT /api/mobile/notificacoes/{id}/lida`

## Padrões
- JWT obrigatório.
- Respostas em `ApiResponse<T>`.
- Mensagens amigáveis para falha de validação e permissão.
