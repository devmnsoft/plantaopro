# Gerar storage state autenticado

1. Inicie a aplicação (`ASPNETCORE_URLS=http://localhost:5000 dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj`).
2. Instale dependências e Chromium: `npm install && npm run playwright:install`.
3. Execute o login manual:
   ```bash
   export PLANTAOPRO_BASE_URL=http://localhost:5000
   npm run smoke:auth
   ```
   No Windows: `$env:PLANTAOPRO_BASE_URL="http://localhost:5000"; npm run smoke:auth`.
4. Autentique uma conta real na janela aberta. Após sair de `/Account/Login`, o script salva `artifacts/auth/storage-state.json`.

## Variáveis opcionais

- `PLANTAOPRO_STORAGE_STATE`: caminho alternativo;
- `PLANTAOPRO_LOGIN_EMAIL` e `PLANTAOPRO_LOGIN_PASSWORD`: automação explícita; informe ambas. A senha não é impressa nem escrita pelo script;
- `PLANTAOPRO_AUTH_TIMEOUT_MS`: tempo máximo para concluir o login.

O arquivo de sessão pode conter cookies/tokens e está ignorado pelo Git. Não o copie para logs, PRs ou mensagens, não versione `.env`, e revogue/regenere a sessão se houver exposição. O script retorna erro quando o login não termina no prazo.
