# Manual de execução

1. Criar banco PostgreSQL e executar:
   - `database/PlantaoPro_PostgreSQL_Completo.sql`
   - `database/seeds.sql`
2. Configurar connection string em `backend/PlantaoPro.Api/appsettings.json`.
3. Subir API.

## Fluxos críticos
- Aceite/Confirmação/Recusa/Cancelamento/Substituição/Realização de escala devem registrar histórico (`historico_escala`) e auditoria (`auditoria`).
- Geração/Confirmação/Cancelamento de pagamento devem registrar `historico_pagamento` e auditoria.
- Notificações são criadas automaticamente nas operações de escala e financeiro.

## Exemplo curl
```bash
curl -X GET "http://localhost:5000/api/dashboard" -H "Authorization: Bearer <token>"
```
