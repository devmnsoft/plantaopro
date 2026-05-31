# Tratamento de erros

## API

- 400: validação.
- 401: não autenticado.
- 403: sem permissão.
- 404: não encontrado.
- 409: conflito de negócio.
- 500: erro interno amigável.

A API deve responder com `ApiResponse<T>` e não expor stack trace, SQL ou payload sensível.

## Web

A Web redireciona 401 para login, 403 para AccessDenied, usa páginas amigáveis para 404/500 e mostra falhas via toast/TempData.
