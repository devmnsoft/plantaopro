# Matriz de permissões

Web: `/Permissoes/Index`, `/Permissoes/Matriz`, `/Permissoes/PorPerfil`, `/Permissoes/PorUsuario` e `/Permissoes/TestarAcesso`.

API: `GET /api/permissoes/matriz`, `GET /api/permissoes/perfil/{perfil}`, `GET /api/permissoes/usuario/{usuarioId}`, `POST /api/permissoes/testar-acesso`, `POST /api/permissoes/perfil/{perfil}/salvar`, `POST /api/permissoes/perfil/{perfil}/restaurar-padrao` e `POST /api/permissoes/perfil/{perfil}/copiar`.

## Fase 1 — matriz operacional

A matriz base está codificada nos serviços centrais e refletida no menu: Admin SaaS, Operação, Financeiro, Cliente, Médico, Parceiro e Suporte/Governança. A próxima etapa é persistir a matriz customizada por tenant.

## Matriz operacional aplicada no Web MVC

A camada Web usa um guard de rotas por módulo para impedir navegação indevida em telas prioritárias:

| Área | Controllers principais | Módulo aplicado | Perfis esperados | Tenant |
| --- | --- | --- | --- | --- |
| Admin SaaS | `AdminSaas`, `Clientes`, `Planos`, `Assinaturas`, `FaturamentoSaas`, `Marketplace`, `Permissoes`, `Observabilidade` | `ADMIN_SAAS`, `CLIENTES`, `PLANOS`, `ASSINATURAS`, `BILLING_GLOBAL`, `MARKETPLACE`, `PERMISSOES`, `OBSERVABILIDADE_GLOBAL` | `ADMINISTRADOR_GLOBAL` | Global |
| Cliente | `ClientePortal`, `Usuarios`, `Perfis`, `Parametrizacoes`, `WhiteLabel` | `CLIENTE_PORTAL`, `USUARIOS`, `PERFIS`, `CONFIGURACOES`, `WHITE_LABEL` | `ADMINISTRADOR_CLIENTE`, `ADMINISTRADOR`, `DIRETOR` | Próprio tenant |
| Operação | `CentralEscala`, `Plantoes`, `Convites`, `Escalas`, `Medicos`, `Hospitais`, `Especialidades`, `Agenda`, `Comunicacao` | Operação por módulo | Admin tenant, coordenação e operação | Próprio tenant |
| Financeiro | `Financeiro`, `Pagamentos`, `Billing`, `Relatorios` | `FINANCEIRO`, `PAGAMENTOS`, `BILLING`, `RELATORIOS` | Financeiro e admin tenant | Próprio tenant |
| Médico | `MedicoArea`, `MinhaAgenda`, `Convites`, `Pagamentos` | `MEDICO_AREA`, `MINHA_AGENDA`, `CONVITES`, `PAGAMENTOS_PROPRIOS` | `MEDICO` | Dados próprios |
| Parceiro | `ParceiroPortal`, `Comercial`, `PropostasComerciais` | `PARCEIRO`, `LEADS`, `PROPOSTAS` | `PARCEIRO` | Tenants vinculados |
| Governança | `Auditoria`, `Lgpd`, `Suporte`, `Ajuda` | `AUDITORIA`, `LGPD`, `SUPORTE`, `AJUDA` | Auditoria/suporte e perfis autorizados | Conforme escopo |

A autorização global continua permitindo `ADMINISTRADOR_GLOBAL` em todas as telas mapeadas, enquanto o administrador de cliente fica restrito aos módulos do tenant.
