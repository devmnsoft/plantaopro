# Saúde 360 — Base mínima de Consultas

## Problema encontrado

O endpoint `GET /api/consultas` consultava `plantaopro.consultas`, mas alguns bancos locais ou ambientes já existentes não possuíam essa tabela. Nesses cenários, o PostgreSQL retornava o erro `42P01` (`relação "plantaopro.consultas" não existe`) antes de o módulo de consultas conseguir responder de forma controlada.

## Tabela ausente

A tabela principal ausente era:

- `plantaopro.consultas`

Para sustentar a primeira fase do prontuário de consultas sem apagar dados e mantendo tenant, LGPD e auditoria, a base mínima também inclui:

- `plantaopro.consulta_anamnese`
- `plantaopro.consulta_exame_fisico`
- `plantaopro.consulta_diagnosticos`
- `plantaopro.consulta_condutas`
- `plantaopro.consulta_encaminhamentos`
- `plantaopro.consulta_historico`

## Migration criada

A migration idempotente está em:

```bash
database/migrations/2026_saude360_consultas_base.sql
```

Ela usa `CREATE TABLE IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS` e blocos `DO $$` com consulta a `pg_constraint` para criar constraints de forma compatível com PostgreSQL.

## Como aplicar a migration

Execute a migration contra o banco PostgreSQL configurado para a API:

```bash
psql "$ConnectionStrings__Default" -f database/migrations/2026_saude360_consultas_base.sql
```

Ou, quando estiver usando a connection string do `appsettings.Development.json`, substitua a variável pelo valor local equivalente.

## Como validar as tabelas

Após aplicar a migration, confirme as tabelas:

```sql
select to_regclass('plantaopro.consultas');
select to_regclass('plantaopro.consulta_anamnese');
select to_regclass('plantaopro.consulta_exame_fisico');
select to_regclass('plantaopro.consulta_diagnosticos');
select to_regclass('plantaopro.consulta_condutas');
select to_regclass('plantaopro.consulta_encaminhamentos');
select to_regclass('plantaopro.consulta_historico');
```

Todas as consultas devem retornar o nome qualificado da tabela.

## Como testar o endpoint

1. Suba a API:

   ```bash
   dotnet run --project backend/PlantaoPro.Api/PlantaoPro.Api.csproj --urls "http://localhost:51976"
   ```

2. No Swagger, autentique usando `POST /api/auth/login`.
3. Chame `GET /api/consultas`.
4. O retorno esperado é `ApiResponse<T>` com HTTP 200 e lista vazia quando não houver registros.
5. Valide também `GET /api/pacientes`, `GET /api/agendamentos` e `GET /api/triagens` para garantir que os módulos existentes continuam funcionando.

## Auditoria, LGPD e tenant

- A listagem de consultas registra auditoria técnica sem gravar conteúdo clínico sensível.
- A query permanece filtrada pelo contexto já usado no serviço clínico (`cliente_id`/tenant e administrador global).
- Quando a tabela de consultas ainda não existir, o serviço retorna mensagem amigável sem expor stack trace.

## Pendências para próxima fase

- Evoluir o vínculo estruturado com CID, incluindo busca, seleção e histórico clínico por diagnóstico.
- Implementar Prescrição clínica com modelos, itens, orientações e histórico auditável.
