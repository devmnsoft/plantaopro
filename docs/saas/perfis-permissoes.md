# Perfis e permissões

## Serviços centrais

- `ICurrentUserService`: usuário, tenant e perfis atuais.
- `IPermissionService`: permissões por módulo/ação e capacidades administrativas.
- `IModuleAccessService`: acesso a módulos e features.
- `ITenantAccessService`: isolamento de tenant.

## Perfis cobertos

- `ADMINISTRADOR_GLOBAL`: acesso total e Admin SaaS.
- `ADMINISTRADOR` / `ADMINISTRADOR_CLIENTE` / `DIRETOR`: administração do tenant.
- `COORDENACAO` / `COORDENADOR` / `OPERADOR`: operação de plantões, escalas e convites.
- `FINANCEIRO`: pagamentos, faturas e relatórios financeiros.
- `MEDICO`: área própria, agenda, convites e pagamentos próprios.
- `HOSPITAL`: plantões e escalas da unidade.
- `PARCEIRO`: portal parceiro, propostas e comissões.
- `SUPORTE`: suporte e chamados com auditoria.
- `AUDITOR`: auditoria e relatórios sem alteração operacional.
- `COMERCIAL`: leads, propostas e planos.
- `CUSTOMER_SUCCESS`: health score, onboarding e plano de ação.

## Endpoints

- `GET /api/permissoes/matriz`
- `GET /api/permissoes/perfil/{perfil}`
- `POST /api/permissoes/testar-acesso`
