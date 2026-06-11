# Operação de migrations — Saúde 360

## Ordem recomendada

Execute as migrations abaixo após o backup operacional do banco:

```bash
psql "$DATABASE_URL" -f database/migrations/2026_fix_api_error_logs_not_null.sql
psql "$DATABASE_URL" -f database/migrations/2026_saude360_base_clinica_minima.sql
```

## Validação das tabelas

```sql
select to_regclass('plantaopro.pacientes');
select to_regclass('plantaopro.agendamentos');
select to_regclass('plantaopro.triagens');
select to_regclass('plantaopro.painel_chamada_fila');
select to_regclass('plantaopro.api_error_logs');
```

Todos os comandos devem retornar o nome completo da tabela.

## Validação dos endpoints

Com a API em execução e usuário autenticado:

```bash
curl -i "$API_URL/api/clinica-dashboard/resumo" -H "Authorization: Bearer $TOKEN"
curl -i "$API_URL/api/pacientes" -H "Authorization: Bearer $TOKEN"
```

Resultados esperados:

- HTTP 200 para ambientes com migrations aplicadas.
- Dashboard com contadores zerados se não houver dados.
- Pacientes com lista vazia se não houver dados.
- Ausência de erros PostgreSQL `42P01` e `23502`.

## Segurança operacional

Não registrar no log conteúdo clínico sensível, senhas, tokens, hashes, API keys ou headers `Authorization`. Em caso de falha na persistência do log estruturado, a request não deve ser derrubada pelo middleware.
