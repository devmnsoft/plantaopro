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

## Persistência

`RequestLoggingMiddleware` persiste `api_request_logs` para todos os requests processados e `api_error_logs` para falhas 500. Respostas 401/403 também geram auditoria central para facilitar correlação entre tentativa, perfil, IP e endpoint.

## Performance

Endpoints com duração igual ou superior a 2000 ms são registrados como warning estruturado com endpoint, método, status, usuário, cliente, perfil, IP e duração. A visão Web é restrita a `ADMINISTRADOR_GLOBAL`.
