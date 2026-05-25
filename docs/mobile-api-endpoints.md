# Mobile API Endpoints

Base: `/api/mobile`

## Autenticação
- `POST /auth/login`
  - Request: `{ "email": "medico@exemplo.com", "senha": "***" }`
  - Response: `ApiResponse<MobileLoginResponseDto>`

## Dashboard e Agenda
- `GET /dashboard`
- `GET /plantoes-disponiveis?page=1&pageSize=20`
- `GET /minhas-escalas?page=1&pageSize=20`
- `GET /meus-pagamentos?page=1&pageSize=20`

## Notificações
- `GET /notificacoes?page=1&pageSize=20`
- `GET /notificacoes/contador`

## Regras de autenticação
- JWT obrigatório em todos os endpoints, exceto login.
- Usuário médico só visualiza dados próprios.
