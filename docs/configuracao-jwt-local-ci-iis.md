# Configuração JWT local, CI, Docker e IIS

## 1. Por que o erro acontece

A API valida `Jwt:Key`, `Jwt:Issuer` e `Jwt:Audience` na inicialização. A chave precisa existir e ter pelo menos 32 caracteres. Quando qualquer valor obrigatório está ausente, vazio ou inválido, a aplicação falha rápido para evitar subir sem autenticação segura.

A chave `PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS` é apenas placeholder de desenvolvimento local. Não use esse valor em produção e não versione segredo real.

## 2. PowerShell

```powershell
$env:Jwt__Key="PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS"
$env:Jwt__Issuer="PlantaoPro"
$env:Jwt__Audience="PlantaoPro"
dotnet run --project backend/PlantaoPro.Api/PlantaoPro.Api.csproj
```

## 3. CMD

```cmd
set Jwt__Key=PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS
set Jwt__Issuer=PlantaoPro
set Jwt__Audience=PlantaoPro
dotnet run --project backend/PlantaoPro.Api/PlantaoPro.Api.csproj
```

## 4. Linux/macOS

```bash
export Jwt__Key="PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS"
export Jwt__Issuer="PlantaoPro"
export Jwt__Audience="PlantaoPro"
dotnet run --project backend/PlantaoPro.Api/PlantaoPro.Api.csproj
```

## 5. User-secrets

Opção recomendada para desenvolvimento local sem gravar segredo no repositório:

```bash
cd backend/PlantaoPro.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS"
dotnet user-secrets set "Jwt:Issuer" "PlantaoPro"
dotnet user-secrets set "Jwt:Audience" "PlantaoPro"
```

## 6. Docker

Copie `.env.example` para `.env` e ajuste os valores locais quando necessário:

```env
PLANTAOPRO_JWT_KEY=PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS
PLANTAOPRO_JWT_ISSUER=PlantaoPro
PLANTAOPRO_JWT_AUDIENCE=PlantaoPro
```

O `docker-compose.yml` atual sobe PostgreSQL. Se a API for executada fora do compose, exporte `Jwt__Key`, `Jwt__Issuer` e `Jwt__Audience` no processo da API.

## 7. IIS

No IIS, configure as variáveis de ambiente no Application Pool/processo da aplicação, ou no `web.config` do deploy com valores fornecidos pelo cofre/esteira do ambiente:

- `Jwt__Key`: segredo real com pelo menos 32 caracteres;
- `Jwt__Issuer`: emissor do ambiente;
- `Jwt__Audience`: audiência do ambiente.

Nunca use o placeholder local em produção/homologação real.

## 8. GitHub Actions

Os jobs que sobem a API definem variáveis no bloco `env`:

```yaml
Jwt__Key: PLANTAOPRO_CI_JWT_KEY_2026_CHANGE_ME_64_CHARS
Jwt__Issuer: PlantaoPro
Jwt__Audience: PlantaoPro
```

Para ambientes reais, prefira `secrets.PLANTAOPRO_JWT_KEY` e não imprima o valor em logs.

## 9. Como validar que a API subiu

```bash
curl -fsS http://localhost:5000/api/health
curl -fsS http://localhost:5000/swagger/index.html
```

Depois de autenticar, valide um endpoint protegido:

```bash
curl -fsS -H "Authorization: Bearer <token>" http://localhost:5000/api/usuarios/me
```

## 10. Como evitar versionar segredo real

- Use `dotnet user-secrets` para desenvolvimento.
- Use variáveis de ambiente em CI/CD, Docker e IIS.
- Mantenha apenas placeholders em arquivos versionados.
- Não registre `jwtKey`, tokens JWT ou headers `Authorization` em logs.
- Revogue qualquer segredo real que tenha sido commitado acidentalmente.
