# Execução local — PlantãoPro

Este guia padroniza a execução local da API e do Web para evitar erro de conexão recusada entre `PlantaoPro.Web` e `PlantaoPro.Api`.

## Portas padrão

### API

- HTTP: `http://localhost:51976`
- HTTPS: `https://localhost:51977`
- Swagger: `/swagger`

### Web

- HTTP: `http://localhost:52976`
- HTTPS: `https://localhost:52977`
- Login: `/Account/Login`

## Subir API

```bash
dotnet run --project backend/PlantaoPro.Api/PlantaoPro.Api.csproj --urls "http://localhost:51976"
```

Swagger:

```text
http://localhost:51976/swagger
```

## Subir Web

```bash
dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj --urls "http://localhost:52976"
```

Login:

```text
http://localhost:52976/Account/Login
```

## Ordem correta

1. Subir PostgreSQL.
2. Subir `PlantaoPro.Api`.
3. Confirmar Swagger.
4. Subir `PlantaoPro.Web`.
5. Fazer login.

## Login de teste

Usuários de teste em ambiente de desenvolvimento:

- `admin@plantaopro.com` / `123456`
- `coordenacao@plantaopro.com` / `123456`
- `operador@plantaopro.com` / `123456`
- `financeiro@plantaopro.com` / `123456`
- `medico@plantaopro.com` / `123456`
- `hospital@plantaopro.com` / `123456`

Em ambiente Development, o `DevelopmentSeed` atualiza a senha dos usuários de teste para `123456` a cada execução.

## Configuração Web -> API

Em desenvolvimento, o Web usa HTTP por padrão para evitar problemas com certificado local:

```json
"ApiSettings": {
  "BaseUrl": "http://localhost:51976"
}
```

A chave legada `PlantaoProApi:BaseUrl`, quando existir, deve permanecer alinhada com `ApiSettings:BaseUrl`.

## Execução com HTTPS local

Use HTTPS somente quando o certificado de desenvolvimento do .NET estiver confiável:

```bash
dotnet dev-certs https --trust
```

Profiles HTTPS padronizados:

- API: `https://localhost:51977/swagger`
- Web: `https://localhost:52977/Account/Login`


## Verificação local

Antes de testar o login, valide que os dois projetos compilam nas portas padronizadas:

```bash
dotnet clean backend/PlantaoPro.Api/PlantaoPro.Api.csproj
dotnet clean backend/PlantaoPro.Web/PlantaoPro.Web.csproj
dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj
dotnet build backend/PlantaoPro.Web/PlantaoPro.Web.csproj
```

Depois suba a API, confirme o Swagger em `http://localhost:51976/swagger`, suba o Web e acesse `http://localhost:52976/Account/Login`.

## Checklist rápido

- API HTTP abre em `http://localhost:51976/swagger`.
- Web HTTP abre em `http://localhost:52976/Account/Login`.
- `PlantaoPro.Web` aponta para `http://localhost:51976` em desenvolvimento.
- PostgreSQL está em execução antes da API.

## Segredos e banco padronizado

- O banco local padrão é `plantaopro`; evite `Database=postgres` nos appsettings do projeto.
- Não grave senha real, JWT real, token ou connection string produtiva no repositório.
- Para desenvolvimento, configure segredos com:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=plantaopro;Username=postgres;Password=CHANGE_ME;Search Path=plantaopro" --project backend/PlantaoPro.Api
dotnet user-secrets set "Jwt:Key" "CHANGE_ME_WITH_32+_CHARS" --project backend/PlantaoPro.Api
```

- Em produção/homologação, prefira variáveis de ambiente ou secret manager da plataforma.
