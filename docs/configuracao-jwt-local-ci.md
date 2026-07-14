# Configuração JWT segura — local, CI, Docker e IIS

## Causa do erro `Jwt:Key`

A API valida `Jwt:Key`, `Jwt:Issuer` e `Jwt:Audience` na inicialização. A chave deve existir e ter pelo menos 32 caracteres. Quando a chave está vazia, curta ou ausente, a API falha rápido com orientação de configuração. O valor da chave nunca deve ser logado ou versionado.

## Desenvolvimento local com user-secrets

```bash
cd backend/PlantaoPro.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS"
dotnet user-secrets set "Jwt:Issuer" "PlantaoPro"
dotnet user-secrets set "Jwt:Audience" "PlantaoPro"
```

## Linux/macOS por variável de ambiente

```bash
export Jwt__Key="PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS"
export Jwt__Issuer="PlantaoPro"
export Jwt__Audience="PlantaoPro"
dotnet run --project backend/PlantaoPro.Api/PlantaoPro.Api.csproj
```

## Windows PowerShell

```powershell
$env:Jwt__Key="PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS"
$env:Jwt__Issuer="PlantaoPro"
$env:Jwt__Audience="PlantaoPro"
dotnet run --project backend/PlantaoPro.Api/PlantaoPro.Api.csproj
```

## Windows CMD

```cmd
set Jwt__Key=PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS
set Jwt__Issuer=PlantaoPro
set Jwt__Audience=PlantaoPro
dotnet run --project backend/PlantaoPro.Api/PlantaoPro.Api.csproj
```

## Docker Compose

Copie `.env.example` para `.env` somente no ambiente local e substitua `PLANTAOPRO_JWT_KEY` por um segredo local com pelo menos 32 caracteres. Não faça commit do `.env`.

```env
PLANTAOPRO_JWT_KEY=PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS
PLANTAOPRO_JWT_ISSUER=PlantaoPro
PLANTAOPRO_JWT_AUDIENCE=PlantaoPro
```

Se a API for executada fora do compose e o compose subir apenas PostgreSQL, exporte `Jwt__Key`, `Jwt__Issuer` e `Jwt__Audience` no processo da API.

## GitHub Actions

Jobs que iniciam a API devem declarar variáveis de ambiente não produtivas para CI:

```yaml
env:
  Jwt__Key: PLANTAOPRO_CI_JWT_KEY_2026_CHANGE_ME_64_CHARS
  Jwt__Issuer: PlantaoPro
  Jwt__Audience: PlantaoPro
```

## IIS / hospedagem

Configure variáveis de ambiente no Application Pool, Web.config transformado fora do repositório ou cofre de segredos do provedor:

- `Jwt__Key`: segredo real com pelo menos 32 caracteres;
- `Jwt__Issuer`: `PlantaoPro` ou emissor definido para o ambiente;
- `Jwt__Audience`: `PlantaoPro` ou audiência definida para o ambiente.

Em produção/homologação, a ausência da chave deve continuar impedindo a inicialização. Nunca versionar segredo real em `appsettings.json`, `.env`, logs, prints ou evidências.
