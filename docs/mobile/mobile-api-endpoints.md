# API Mobile MVP — endpoints

## Autenticação
- `POST /api/mobile/auth/login`
- JWT obrigatório nos demais endpoints.

## Identidade e dashboard
- `GET /api/mobile/me`
- `GET /api/mobile/dashboard`
- `GET /api/mobile/perfil`
- `PUT /api/mobile/perfil`

## Plantões e escalas
- `GET /api/mobile/plantoes-disponiveis?page=1&pageSize=20`
- `GET /api/mobile/plantoes/{id}`
- `POST /api/mobile/plantoes/{id}/solicitar`
- `GET /api/mobile/minhas-escalas?page=1&pageSize=20`

## Convites
- `GET /api/mobile/convites?page=1&pageSize=20`
- `POST /api/mobile/convites/{id}/aceitar`
- `POST /api/mobile/convites/{id}/recusar`

## Financeiro e notificações
- `GET /api/mobile/meus-pagamentos?page=1&pageSize=20`
- `GET /api/mobile/notificacoes?page=1&pageSize=20`
- `PUT /api/mobile/notificacoes/{id}/lida`

## Preferências e disponibilidade
- `GET /api/mobile/disponibilidade`
- `PUT /api/mobile/disponibilidade`
- `GET /api/mobile/preferencias`
- `PUT /api/mobile/preferencias`

## Contrato esperado
- Respostas em `ApiResponse<T>` ou envelope equivalente.
- Payload leve e paginado em listagens.
- Sem senha, hash, token persistido, segredo ou payload sensível.
- Erro 403 amigável quando o plano não permitir mobile.
