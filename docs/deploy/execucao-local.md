# Execução local — PlantãoPro

> Documento operacional atualizado: consulte também `docs/operacao/execucao-local.md`.

## Portas padrão

- API HTTP: `http://localhost:51976`.
- API HTTPS: `https://localhost:51977`.
- Web HTTP: `http://localhost:52976`.
- Web HTTPS: `https://localhost:52977`.

## Rodar a API

```powershell
dotnet run --project backend/PlantaoPro.Api/PlantaoPro.Api.csproj --urls "http://localhost:51976"
```

Acesse:

- `http://localhost:51976/swagger`
- `https://localhost:51977/swagger` quando usar o profile HTTPS.
- `http://localhost:51976/api/health`

## Rodar o Web

```powershell
dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj --urls "http://localhost:52976"
```

Acesse `http://localhost:52976/Account/Login`.

## Visual Studio — startup múltiplo

1. Solution > **Configure Startup Projects**.
2. Selecione **Multiple startup projects**.
3. Defina **Start** para:
   - `PlantaoPro.Api`
   - `PlantaoPro.Web`

## Configuração Web -> API

No arquivo `backend/PlantaoPro.Web/appsettings.Development.json`, configure:

```json
"ApiSettings": {
  "BaseUrl": "https://localhost:51977"
}
```

Para uso temporário em HTTP, configure `http://localhost:51976` e mantenha qualquer chave legada equivalente com o mesmo valor.

## Resolver SocketException 10013

1. Verifique processo preso na porta:

   ```cmd
   netstat -ano | findstr :52976
   netstat -ano | findstr :52977
   netstat -ano | findstr :51976
   netstat -ano | findstr :51977
   ```

2. Finalize o PID encontrado, se necessário:

   ```cmd
   taskkill /PID <PID> /F
   ```

3. Verifique portas reservadas pelo Windows:

   ```cmd
   netsh interface ipv4 show excludedportrange protocol=tcp
   ```

4. Se houver problema com certificado HTTPS local:

   ```powershell
   dotnet dev-certs https --clean
   dotnet dev-certs https --trust
   ```

## Testes rápidos esperados

1. `GET http://localhost:51976/swagger` retorna UI do Swagger.
2. `GET http://localhost:51976/api/health` retorna 200 com status Healthy.
3. Abrir Web em `http://localhost:52976/Account/Login`.
4. Login admin redireciona para dashboard interno.
5. Login médico redireciona para `MinhaAgenda`.
6. Sem autenticação em rota protegida, volta para `/Account/Login?returnUrl=...`.
7. Após login, volta para `returnUrl` local.
8. Nunca redirecionar para a raiz da API após login.

## Se Swagger não abrir

- Verifique se o profile da API usa `launchUrl: "swagger"` em `launchSettings.json`.
- Verifique se `Program.cs` da API mapeia Swagger em `Development`.
- Confirme que o ambiente está como `Development`.
