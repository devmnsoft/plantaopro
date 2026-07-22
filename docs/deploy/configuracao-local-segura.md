# Configuração local segura

O `appsettings.json` versionado do PlantãoPro não deve armazenar connection strings, senhas, chaves JWT reais ou flags legadas inseguras. Configure valores locais por User Secrets ou variáveis de ambiente.

## User Secrets

Execute na raiz do projeto da API:

```powershell
cd backend/PlantaoPro.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=plantaopro;Username=<usuario>;Password=<senha-local>;Pooling=true;Search Path=plantaopro,public"
dotnet user-secrets set "Jwt:Key" "<chave-local-com-32-ou-mais-caracteres>"
```

## Variáveis de ambiente

```bash
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=plantaopro;Username=<usuario>;Password=<senha-local>;Pooling=true;Search Path=plantaopro,public"
export Jwt__Key="<chave-local-com-32-ou-mais-caracteres>"
```

## Rotação obrigatória

A senha exposta anteriormente no histórico do repositório deve ser considerada comprometida. Faça rotação imediata da senha do usuário PostgreSQL afetado, invalide qualquer segredo reutilizado e revise logs de acesso.
