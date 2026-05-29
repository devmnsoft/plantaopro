# API Mobile MVP — endpoints

Todos os endpoints mobile devem usar JWT, retornar `ApiResponse<T>`, não expor dados sensíveis e aplicar plano mobile quando configurado.

## Autenticação
- `POST /api/mobile/auth/login`
- `GET /api/mobile/me`

## Dashboard e operação médica
- `GET /api/mobile/dashboard`
- `GET /api/mobile/plantoes-disponiveis?page=1&pageSize=20`
- `GET /api/mobile/plantoes/{id}`
- `POST /api/mobile/plantoes/{id}/solicitar`
- `GET /api/mobile/convites`
- `GET /api/mobile/convites/{id}`
- `POST /api/mobile/convites/{id}/aceitar`
- `POST /api/mobile/convites/{id}/recusar`
- `GET /api/mobile/minhas-escalas?page=1&pageSize=20`

## Financeiro e notificações
- `GET /api/mobile/meus-pagamentos?page=1&pageSize=20`
- `GET /api/mobile/notificacoes?page=1&pageSize=20`
- `PUT /api/mobile/notificacoes/{id}/lida`

## Perfil, disponibilidade e preferências
- `GET /api/mobile/perfil`
- `PUT /api/mobile/perfil`
- `GET /api/mobile/disponibilidade`
- `PUT /api/mobile/disponibilidade`
- `GET /api/mobile/preferencias`
- `PUT /api/mobile/preferencias`
