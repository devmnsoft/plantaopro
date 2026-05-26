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

## Complementos MVP Comercial (26/05/2026)

- Validar fluxo de onboarding comercial com perfil `ADMINISTRADOR_GLOBAL` antes da janela de homologação.
- Validar criação de assinatura vinculada a plano e unidade inicial no mesmo ciclo de implantação.
- Executar checklist de segurança de permissões por perfil (admin global, admin cliente, coordenação, financeiro, médico, hospital).
- Confirmar disponibilidade dos módulos de Suporte, Customer Success e Faturamento SaaS na base de homologação.
- Registrar evidências (prints/logs) dos fluxos críticos para apresentação comercial.
