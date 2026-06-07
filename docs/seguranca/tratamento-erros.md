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

## Regras operacionais

O middleware registra requests 401/403 como eventos de auditoria resumidos, sem payload e sem headers sensíveis. Exceções 500 continuam retornando mensagem amigável por `ApiResponse.Fail`, enquanto detalhes técnicos ficam apenas no log estruturado.
