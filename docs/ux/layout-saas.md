# Layout SaaS

O layout autenticado usa sidebar por perfil, topbar com busca, notificações, ajuda, tenant, plano, upgrade e rodapé com versão/ambiente. Menus administrativos são separados de operação, financeiro, cliente, médico, parceiro e governança.

## Fase 1 — layout principal

- Sidebar passa a ser montada por `MenuBuilderService`, respeitando perfil, módulo e bloqueio.
- Topbar mantém busca, contexto de tenant, plano, notificações, ajuda, upgrade e usuário.
- Rodapé mantém versão, ambiente e `Powered by MNSOFT` como fallback white label.
