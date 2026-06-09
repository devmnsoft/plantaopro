# Design system SaaS PlantãoPro

CSS ativos: `plantaopro-design-system.css`, `plantao-saas.css`, `plantao-components.css`, `plantao-layout.css`, `plantao-theme.css`, `plantao-white-label.css`, `plantao-dashboard.css` e `plantao-forms.css`.

Partials ativos: `_PageHeader`, `_Breadcrumb`, `_KpiCard`, `_StatusBadge`, `_EmptyState`, `_ActionBar`, `_FilterBar`, `_ConfirmModal`, `_ToastMessages`, `_TenantContextBadge`, `_PlanUsageMiniCard`, `_ModuleLockedNotice`, `_PlanLimitReachedNotice`, `_UpgradeCta`, `_ProfileHomeCard` e `_QuickActions`.

Padrão: cards com sombra leve, KPIs claros, badges de status, botões primários/secundários, alertas comerciais, empty states explicativos e tabelas responsivas.

## Fase 1 — base SaaS aplicada

- O layout principal usa `plantao-saas.css`, `plantao-components.css`, `plantao-layout.css`, `plantao-dashboard.css` e `plantao-forms.css` como camadas de design system.
- Estados bloqueados de módulo/plano foram padronizados visualmente com cards, borda pontilhada e CTA de upgrade.
- Menus bloqueados usam classe `nav-link-app locked`, sem `href="#"`, para evitar ação sem destino.
