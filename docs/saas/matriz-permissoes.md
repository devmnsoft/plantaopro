# Matriz de permissões

Web: `/Permissoes/Index`, `/Permissoes/Matriz`, `/Permissoes/PorPerfil`, `/Permissoes/PorUsuario` e `/Permissoes/TestarAcesso`.

API: `GET /api/permissoes/matriz`, `GET /api/permissoes/perfil/{perfil}`, `GET /api/permissoes/usuario/{usuarioId}`, `POST /api/permissoes/testar-acesso`, `POST /api/permissoes/perfil/{perfil}/salvar`, `POST /api/permissoes/perfil/{perfil}/restaurar-padrao` e `POST /api/permissoes/perfil/{perfil}/copiar`.

## Fase 1 — matriz operacional

A matriz base está codificada nos serviços centrais e refletida no menu: Admin SaaS, Operação, Financeiro, Cliente, Médico, Parceiro e Suporte/Governança. A próxima etapa é persistir a matriz customizada por tenant.
