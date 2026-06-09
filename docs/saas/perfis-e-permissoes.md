# Perfis e permissões

Perfis base: ADMINISTRADOR_GLOBAL, ADMINISTRADOR_CLIENTE, ADMINISTRADOR, DIRETOR, COORDENADOR, OPERADOR, FINANCEIRO, MEDICO, HOSPITAL, PARCEIRO, SUPORTE, AUDITOR, COMERCIAL e CUSTOMER_SUCCESS.

ADMINISTRADOR_GLOBAL tem acesso global. Administradores de cliente não acessam Admin SaaS global. Coordenação acessa escala/plantões/convites. Financeiro acessa financeiro e relatórios. Médico acessa agenda, convites, disponibilidade, substituições e pagamentos próprios. Parceiro acessa leads, propostas e comissões sem dados clínicos sensíveis.

## Fase 1 — serviços centrais

- Web: `ICurrentUserService`, `IPermissionService`, `ITenantAccessService`, `IModuleAccessService` e `IMenuBuilderService` registrados no DI.
- API: `ICurrentUserService`, `IPermissionService`, `ITenantAccessService` e `IModuleAccessService` registrados no DI.
- `ADMINISTRADOR_GLOBAL` é tratado como acesso total; admin cliente fica fora de Admin SaaS global; médico e parceiro ficam segregados por módulos próprios.
