# Mobile API endpoints (PlantãoPro)

## Autenticação
- `POST /api/mobile/auth/login`

## Usuário e dashboard
- `GET /api/mobile/me`
- `GET /api/mobile/dashboard`

## Plantões/Convites/Escalas
- `GET /api/mobile/plantoes-disponiveis`
- `GET /api/mobile/plantoes/{id}`
- `POST /api/mobile/plantoes/{id}/solicitar`
- `GET /api/mobile/convites`
- `POST /api/mobile/convites/{id}/aceitar`
- `POST /api/mobile/convites/{id}/recusar`
- `GET /api/mobile/minhas-escalas`

## Financeiro e notificações
- `GET /api/mobile/meus-pagamentos`
- `GET /api/mobile/notificacoes`
- `PUT /api/mobile/notificacoes/{id}/lida`
- `PUT /api/mobile/notificacoes/lidas`

## Perfil médico
- `GET /api/mobile/perfil`
- `PUT /api/mobile/perfil`
- `GET /api/mobile/disponibilidade`
- `PUT /api/mobile/disponibilidade`
- `GET /api/mobile/preferencias`
- `PUT /api/mobile/preferencias`

## Exemplo de resposta padrão
```json
{ "success": true, "message": "OK", "data": {}, "errors": [] }
```
