# Deploy Homologação PlantãoPro

## Pré-requisitos
- .NET SDK compatível
- PostgreSQL acessível
- Variáveis de ambiente configuradas

## Publicação
1. Publicar API (`dotnet publish`).
2. Publicar Web (`dotnet publish`).
3. Aplicar scripts SQL no schema `plantaopro`.
4. Executar seed de demonstração.

## Validação
- Testar Swagger da API.
- Testar login na Web.
- Verificar logs de inicialização e erros.
