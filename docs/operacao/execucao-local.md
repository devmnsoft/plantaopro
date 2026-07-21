# Execução local PlantãoPro v1.18.5

## Instalação nova segura

1. Crie o banco `plantaopro`.
2. Execute `psql -v ON_ERROR_STOP=1 -d plantaopro -f database/scrpt_completo.sql`.
3. Crie o primeiro administrador com a CLI de bootstrap, fornecendo a senha por prompt interativo ou por `PLANTAOPRO_BOOTSTRAP_PASSWORD`.
4. No primeiro login, o usuário é marcado para troca obrigatória de senha.

A senha administrativa não é gravada em Git nem no script completo.

## Desenvolvimento local

Use `Database=plantaopro`. Configure segredos fora do Git:

```bash
dotnet user-secrets set "DevelopmentSeed:Enabled" "true" --project backend/PlantaoPro.Api
dotnet user-secrets set "Demo:Password" "<senha-local-forte>" --project backend/PlantaoPro.Api
```

Com `DevelopmentSeed:Enabled=true`, o seed cria contas demo previsíveis sem exibir senha. A senha é a definida no user-secrets/ambiente.
