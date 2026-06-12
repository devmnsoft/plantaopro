# Release — Correção da tabela de consultas do Saúde 360

## Resumo

Esta correção cria a base mínima do módulo Consultas para evitar erro `42P01` no endpoint `GET /api/consultas` quando a tabela `plantaopro.consultas` ainda não existe no banco.

## Problema encontrado

O serviço clínico já mapeava o `tableKey` `consultas` para a tabela `plantaopro.consultas`, porém a tabela não estava garantida na base mínima automática de alguns ambientes. O endpoint podia falhar com erro técnico do PostgreSQL em vez de responder um `ApiResponse<T>` controlado.

## Migration criada

Arquivo:

```bash
database/migrations/2026_saude360_consultas_base.sql
```

A migration é idempotente e cria/atualiza:

- `plantaopro.consultas`
- `plantaopro.consulta_anamnese`
- `plantaopro.consulta_exame_fisico`
- `plantaopro.consulta_diagnosticos`
- `plantaopro.consulta_condutas`
- `plantaopro.consulta_encaminhamentos`
- `plantaopro.consulta_historico`

## Como aplicar

```bash
psql "$ConnectionStrings__Default" -f database/migrations/2026_saude360_consultas_base.sql
```

## Como testar

1. Build da API:

   ```bash
   dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj
   ```

2. Build da Web:

   ```bash
   dotnet build backend/PlantaoPro.Web/PlantaoPro.Web.csproj
   ```

3. Subir API:

   ```bash
   dotnet run --project backend/PlantaoPro.Api/PlantaoPro.Api.csproj --urls "http://localhost:51976"
   ```

4. Validar no Swagger:

   - `POST /api/auth/login`
   - `GET /api/consultas`
   - `GET /api/pacientes`
   - `GET /api/agendamentos`
   - `GET /api/triagens`

## Resultado esperado

- `GET /api/consultas` retorna HTTP 200 com lista vazia quando não houver registros.
- Não há erro `42P01` para `plantaopro.consultas` após aplicar a migration.
- O retorno continua envelopado em `ApiResponse<T>`.
- Logs e auditoria não registram conteúdo clínico sensível.
- Login, Swagger, Pacientes, Agendamentos, Painel de Chamada e Triagem permanecem compatíveis.

## Pendências para próxima fase

- Completar integração de CID no fluxo de diagnóstico da consulta.
- Completar módulo de Prescrição com itens, modelos e histórico auditável.
