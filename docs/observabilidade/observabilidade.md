# Observabilidade

A observabilidade consolida requests, erros, performance, acessos negados e logins.

## Endpoints

- `GET /api/observabilidade/resumo`
- `GET /api/observabilidade/erros`
- `GET /api/observabilidade/performance`
- `GET /api/observabilidade/acessos-negados`
- `GET /api/observabilidade/logins`
- `GET /api/observabilidade/requests`

## Dados registrados

Endpoint, método, status code, usuário, cliente, perfil, IP, duração, sucesso/falha e mensagem resumida. Não são registrados senha, token ou headers sensíveis.
