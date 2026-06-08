# Roteiro Self Service

## Implementado nesta rodada

Roteiro: plano público, cadastro empresa/plano/admin, aceite, tenant, assinatura, onboarding e login.

## Componentes relacionados

- Migração: `database/migrations/2026_plantao_pro_white_label_b2b_launch.sql`.
- APIs B2B: `backend/PlantaoPro.Api/Controllers/B2BLaunchController.cs`.
- Serviços B2B: `backend/PlantaoPro.Api/B2BLaunchServices.cs`.
- Web B2B: `backend/PlantaoPro.Web/Controllers/B2BLaunchWebControllers.cs`.

## Pendências reais

- Validar manualmente em ambiente com PostgreSQL e SDK .NET instalado.
- Conectar as telas genéricas aos formulários AJAX finais quando o design system definitivo estiver homologado.
