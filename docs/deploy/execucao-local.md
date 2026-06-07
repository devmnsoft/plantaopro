# Execução local — PlantãoPro

## Portas padrão
- API: `https://localhost:51977` (`http://localhost:51978`).
- Web MVC: `https://localhost:58285` (`http://localhost:58286`).

## Rodar a API
1. Abra `backend/PlantaoPro.Api`.
2. Execute o projeto `PlantaoPro.Api`.
3. Acesse:
   - `https://localhost:51977/` (redireciona para Swagger em Development)
   - `https://localhost:51977/swagger`
   - `https://localhost:51977/api/health`

## Rodar o Web
1. Abra `backend/PlantaoPro.Web`.
2. Execute o projeto `PlantaoPro.Web`.
3. Acesse `https://localhost:58285/Account/Login`.

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
  "BaseUrl": "https://localhost:51977/"
}
```

## Testes rápidos esperados
1. `GET https://localhost:51977/` abre Swagger (Development).
2. `GET https://localhost:51977/swagger` retorna UI do Swagger.
3. `GET https://localhost:51977/api/health` retorna 200 com status Healthy.
4. Abrir Web em `/Account/Login`.
5. Login admin redireciona para dashboard interno.
6. Login médico redireciona para `MinhaAgenda`.
7. Sem autenticação em rota protegida, volta para `/Account/Login?returnUrl=...`.
8. Após login, volta para `returnUrl` local.
9. Nunca redirecionar para a raiz da API após login.

## Se `https://localhost:51977/` abrir 404
- Verifique se o profile da API usa `launchUrl: "swagger"` em `launchSettings.json`.
- Verifique se `Program.cs` da API mapeia `app.MapGet("/", ...)`.
- Confirme que o ambiente está como `Development` para redirecionar automaticamente ao Swagger.
