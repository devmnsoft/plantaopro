# Mobile API MVP

## Autenticação
- `POST /api/mobile/auth/login`
- `GET /api/mobile/me`

## Dashboard
- `GET /api/mobile/dashboard`

## Operação médica
- `GET /api/mobile/plantoes-disponiveis`
- `GET /api/mobile/plantoes/{id}`
- `POST /api/mobile/plantoes/{id}/solicitar`
- `GET /api/mobile/convites`
- `POST /api/mobile/convites/{id}/aceitar`
- `POST /api/mobile/convites/{id}/recusar`
- `GET /api/mobile/minhas-escalas`
- `GET /api/mobile/meus-pagamentos`

## Perfil e notificações
- `GET /api/mobile/notificacoes`
- `PUT /api/mobile/notificacoes/{id}/lida`
- `PUT /api/mobile/notificacoes/lidas`
- `GET /api/mobile/perfil`
- `PUT /api/mobile/perfil`
- `GET /api/mobile/disponibilidade`
- `PUT /api/mobile/disponibilidade`
- `GET /api/mobile/preferencias`
- `PUT /api/mobile/preferencias`
