# PlantãoPro SaaS self-service multi-tenant white label

## Implementado
- Migração incremental `database/migrations/2026_plantao_pro_white_label_self_service.sql` com estrutura multi-tenant, planos, assinaturas, cadastro self-service, perfis, permissões, white label, parametrizações, onboarding e LGPD.
- APIs públicas para planos, comparativo e cadastro self-service.
- APIs autenticadas para contexto de tenant, white label, perfis, permissões, parametrizações, onboarding e minha assinatura.
- Telas MVC para planos públicos, cadastro em etapas, white label, perfis, parametrizações e uso/upgrade/downgrade da assinatura.

## Pendências reais
- Executar a migração em ambiente PostgreSQL de homologação.
- Conectar as telas MVC ao endpoint real de finalização self-service em ambiente com API disponível.
- Validar manualmente upload físico de assets em storage definitivo.
