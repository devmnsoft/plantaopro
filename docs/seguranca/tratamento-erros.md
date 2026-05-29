# Tratamento de erros

## API

A API deve retornar `ApiResponse<T>` para erros conhecidos:

- 400: validação.
- 401: autenticação obrigatória.
- 403: permissão negada.
- 404: recurso inexistente.
- 409: conflito/regra de negócio.
- 500: falha interna amigável.

JWT challenge/forbid já retorna JSON amigável. Stack trace e SQL bruto não devem ser expostos.

## Web

A Web usa páginas amigáveis `AccessDenied`, `NotFound` e `Error`, com toast para falhas operacionais quando aplicável.
