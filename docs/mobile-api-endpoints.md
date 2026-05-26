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

## Cobertura MVP Comercial (26/05/2026)

Endpoints mínimos obrigatórios para a primeira versão do app:
- `POST /api/mobile/auth/login`
- `GET /api/mobile/me`
- `GET /api/mobile/dashboard`
- `GET /api/mobile/plantoes-disponiveis`
- `GET /api/mobile/plantoes/{id}`
- `POST /api/mobile/plantoes/{id}/solicitar`
- `GET /api/mobile/convites`
- `POST /api/mobile/convites/{id}/aceitar`
- `POST /api/mobile/convites/{id}/recusar`
- `GET /api/mobile/minhas-escalas`
- `GET /api/mobile/meus-pagamentos`
- `GET /api/mobile/notificacoes`
- `PUT /api/mobile/notificacoes/{id}/lida`
- `GET /api/mobile/perfil`
- `PUT /api/mobile/perfil`
- `GET /api/mobile/disponibilidade`
- `PUT /api/mobile/disponibilidade`
- `GET /api/mobile/preferencias`
- `PUT /api/mobile/preferencias`

Regras de qualidade:
- JWT obrigatório em todos os endpoints (exceto login).
- Resposta padronizada `ApiResponse<T>`.
- Mensagens amigáveis para erros de negócio.
- Bloqueio com `403` amigável quando plano do cliente não permitir mobile.
