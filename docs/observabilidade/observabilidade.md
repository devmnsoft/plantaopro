# Observabilidade

A observabilidade operacional usa `RequestLogContextFilter` para registrar requests e falhas em tabelas PostgreSQL.

## Tabelas

- `plantaopro.api_request_logs`
- `plantaopro.api_error_logs`
- `plantaopro.observabilidade_eventos`

## Endpoints

- `GET /api/observabilidade/resumo`
- `GET /api/observabilidade/erros`
- `GET /api/observabilidade/performance`
- `GET /api/observabilidade/acessos-negados`
- `GET /api/observabilidade/logins`
- `GET /api/observabilidade/requests`

Somente `ADMINISTRADOR_GLOBAL` deve acessar a visão de observabilidade.
