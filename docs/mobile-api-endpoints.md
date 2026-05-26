# Endpoints Mobile

## Autenticação
- `POST /api/auth/login`

## Médico
- `GET /api/medico-area/agenda`
- `GET /api/medico-area/plantoes-disponiveis`
- `POST /api/escalas/solicitar`
- `GET /api/medico-area/convites`
- `POST /api/medico-area/convites/{id}/aceitar`
- `POST /api/medico-area/convites/{id}/recusar`

## Financeiro
- `GET /api/financeiro/pagamentos`

## Exemplo JSON (login)
```json
{ "email": "medico@plantaopro.com", "senha": "***" }
```

## Tratamento de erro
- Exibir mensagem amigável de `ApiResponse`.
- Em 401, limpar sessão local e redirecionar para login.
