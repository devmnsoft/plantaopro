# Checklist de banco para auditoria e observabilidade

Use este checklist após publicar uma versão que grave auditoria central ou logs estruturados de requests no schema `plantaopro`.

## 1. Aplicar script incremental

Execute o script corretivo no banco da aplicação:

```bash
psql "$DATABASE_URL" -f backend/sql/2026-06-03_corrigir_auditoria_request_logs.sql
```

O script cria/atualiza as tabelas `plantaopro.auditoria_acoes_criticas`, `plantaopro.api_request_logs` e `plantaopro.api_error_logs` com `CREATE TABLE IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS` e `CREATE INDEX IF NOT EXISTS`.

## 2. Validar colunas da auditoria central

Confirme que `plantaopro.auditoria_acoes_criticas` possui a coluna `cliente_id` e as demais colunas usadas pelo `AuditService`:

```sql
select column_name
from information_schema.columns
where table_schema = 'plantaopro'
  and table_name = 'auditoria_acoes_criticas'
order by ordinal_position;
```

Colunas esperadas: `id`, `usuario_id`, `cliente_id`, `entidade`, `entidade_id`, `acao`, `detalhes`, `sucesso`, `ip_origem`, `perfil`, `user_agent` e `reg_date`.

## 3. Validar colunas dos logs de request

Confirme que `plantaopro.api_request_logs` possui a coluna `perfil` e as demais colunas usadas pelo `RequestLoggingMiddleware`:

```sql
select column_name
from information_schema.columns
where table_schema = 'plantaopro'
  and table_name = 'api_request_logs'
order by ordinal_position;
```

Colunas esperadas: `id`, `endpoint`, `metodo`, `status_code`, `sucesso`, `duracao_ms`, `usuario_id`, `cliente_id`, `email`, `perfil`, `ip_origem`, `user_agent`, `query_string`, `erro` e `reg_date`.

## 4. Validar API e fluxo funcional

1. Inicie a API.
2. Abra o Swagger.
3. Verifique o health check:

```bash
curl -i http://localhost:5000/api/health
```

4. Faça login com `admin@plantaopro.com` e confirme HTTP 200.
5. Abra o dashboard e confirme HTTP 200.
6. Verifique os logs da API e confirme que não aparecem mais estas mensagens:
   - `coluna "cliente_id" da relação "auditoria_acoes_criticas" não existe`
   - `coluna "perfil" da relação "api_request_logs" não existe`

## 5. Validar registros gravados

Confira os últimos registros de auditoria central:

```sql
select *
from plantaopro.auditoria_acoes_criticas
order by reg_date desc
limit 10;
```

Critério esperado: existir auditoria `LOGIN_SUCESSO` após login válido.

Confira os últimos logs estruturados de request:

```sql
select *
from plantaopro.api_request_logs
order by reg_date desc
limit 10;
```

Critérios esperados:

- existir registro de `POST /api/auth/login`;
- existir registro de `GET /api/dashboard`;
- não existir falha de coluna ausente nos logs da API.
