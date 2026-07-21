# Role scope migration report

- Version: v1.19.0
- Status: generated
- Global role links are preserved without forcing tenant association.
- Tenant role links with existing tenant_id are copied into `usuario_tenant_acessos` idempotently.
