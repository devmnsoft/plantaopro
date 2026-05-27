# API Mobile Endpoints

## Auth
- `POST /api/mobile/auth/login`
- `GET /api/mobile/me`

## Dashboard
- `GET /api/mobile/dashboard`

## Plantões
- `GET /api/mobile/plantoes-disponiveis`
- `GET /api/mobile/plantoes/{id}`
- `POST /api/mobile/plantoes/{id}/solicitar`

## Convites
- `GET /api/mobile/convites`
- `GET /api/mobile/convites/{id}`
- `POST /api/mobile/convites/{id}/aceitar`
- `POST /api/mobile/convites/{id}/recusar`

## Escalas
- `GET /api/mobile/minhas-escalas`
- `GET /api/mobile/minhas-escalas/{id}`

## Pagamentos
- `GET /api/mobile/meus-pagamentos`
- `GET /api/mobile/meus-pagamentos/{id}`
- `POST /api/mobile/meus-pagamentos/{id}/contestar`

## Notificações
- `GET /api/mobile/notificacoes`
- `GET /api/mobile/notificacoes/contador`
- `PUT /api/mobile/notificacoes/{id}/lida`
- `PUT /api/mobile/notificacoes/lidas`

## Perfil
- `GET /api/mobile/perfil`
- `PUT /api/mobile/perfil`

## Disponibilidade
- `GET /api/mobile/disponibilidade`
- `PUT /api/mobile/disponibilidade`

## Preferências
- `GET /api/mobile/preferencias`
- `PUT /api/mobile/preferencias`

## Desempenho
- `GET /api/mobile/meu-desempenho`

## Regras transversais
- JWT obrigatório.
- `ApiResponse<T>` em todas as respostas.
- Paginação em listagens.
- Escopo de dados por médico/cliente.
- 403 amigável para plano sem permissão mobile.
