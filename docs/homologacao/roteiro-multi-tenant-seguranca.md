# Roteiro multi-tenant, segurança e LGPD — PlantãoPro RC

1. Criar Tenant A e Tenant B.
2. Criar admin cliente A, admin cliente B, parceiro A, médico A e médico B.
3. Confirmar que admin global visualiza todos os tenants.
4. Confirmar que admin cliente A não lista nem acessa dados do Tenant B.
5. Confirmar que parceiro A só acessa tenants vinculados.
6. Confirmar que médico A só acessa agenda, convites, escalas e pagamentos próprios.
7. Validar white label por tenant sem vazamento entre hosts/contexts.
8. Validar billing/faturas isoladas por tenant.
9. Abrir solicitação LGPD e exportar apenas dados do próprio titular/tenant.
10. Verificar auditoria de acesso negado e troca de contexto.

## Pendências reais
- Anexar evidências de requests 403/404 amigáveis sem stack trace.
- Revisar queries novas para `tenant_id`/`cliente_id` antes de produção.
