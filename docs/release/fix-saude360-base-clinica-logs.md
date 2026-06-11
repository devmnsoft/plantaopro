# Correção da base clínica Saúde 360 e logs estruturados

## Problema encontrado

A Fase 5.1 do Saúde 360 apresentava duas falhas críticas:

- O middleware de logging estruturado podia tentar inserir `error_message` nulo em `plantaopro.api_error_logs`, causando erro PostgreSQL `23502`.
- Endpoints clínicos como `GET /api/clinica-dashboard/resumo` e `GET /api/pacientes` podiam falhar com `42P01` quando as tabelas clínicas mínimas ainda não existiam.

## Causa

- A tabela `api_error_logs` exige `error_message` não nulo, mas o fluxo de logging nem sempre produzia uma mensagem segura para respostas sem exceção.
- A base clínica mínima dependia de inicialização prévia no banco e alguns ambientes ainda não possuíam as tabelas `pacientes`, `agendamentos`, `triagens` e `painel_chamada_fila`.

## Correção aplicada

- O `RequestLoggingMiddleware` agora sempre monta uma mensagem segura para erro HTTP ou usa `string.Empty` quando não há erro.
- A gravação em `api_error_logs` ocorre apenas para exceção ou status HTTP maior ou igual a 400.
- O insert de log usa `stack_trace`, `query_string` e `duration_ms` com valores seguros.
- Foi criada migration idempotente para ajustar defaults e dados nulos em `api_error_logs`.
- Foi criada migration idempotente com a base clínica mínima do Saúde 360.
- O serviço clínico trata `PostgresException` `42P01` com mensagem amigável em listagens e dashboard.

## Scripts criados

- `database/migrations/2026_fix_api_error_logs_not_null.sql`
- `database/migrations/2026_saude360_base_clinica_minima.sql`

## Validação esperada

Após aplicar as migrations:

- `GET /api/clinica-dashboard/resumo` deve retornar `ApiResponse<T>` com contadores zerados quando não houver dados.
- `GET /api/pacientes` deve retornar `ApiResponse<T>` com lista vazia quando não houver dados.
- Logs estruturados não devem quebrar a request por `error_message` nulo.

## Pendências reais

- Aplicar as migrations em todos os ambientes antes de evoluir novos módulos clínicos.
- Validar o fluxo completo com banco PostgreSQL disponível e usuário admin local.
