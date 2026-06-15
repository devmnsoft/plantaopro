# QA API Keys e Usabilidade

## API
- Login em `POST /api/auth/login`.
- Abrir Swagger.
- `GET /api/developer/overview`.
- `GET /api/developer/escopos`.
- Criar API Key com escopos válidos.
- Criar API Key com escopo inválido e confirmar HTTP 400.
- Revogar chave criada.

## Web
- Login admin.
- Abrir Developer Portal.
- Confirmar aviso de exibição única da chave.
- Abrir Fluxo de Atendimento.
- Validar links dos passos principais.

## Pendências reais
Execução completa depende de ambiente com SDK .NET, PostgreSQL e credenciais de teste disponíveis.
