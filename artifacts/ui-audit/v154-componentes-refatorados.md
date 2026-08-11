# Componentes refatorados — v1.54.0

## Shell e navegação
`pp-app`, `pp-main`, `pp-content`, `pp-topbar`, `pp-breadcrumb`, `pp-user-menu` e footer formam um shell único. Listas semânticas continuam no Razor, mas marcadores são neutralizados somente dentro de componentes de navegação.

## Páginas e cards
A camada `v154-clinical-pages.css` introduz `pp-page`, `pp-page-hero`, `pp-section`, `pp-action-card`, `pp-checklist-card`, `pp-plan-card`, `pp-plan-grid`, `pp-clinical-grid` e `pp-toolbar`. Admin SaaS, B2B e Planos usam esses componentes com dados fornecidos pelos respectivos models.

## Formulários e onboarding
`pp-form-card`, `pp-form-section`, `pp-form-grid`, `pp-form-field`, `pp-form-error`, `pp-form-actions`, `pp-stepper`, `pp-wizard-layout` e `pp-wizard-summary` organizam empresa, plano/unidade e administrador sem divisores brutos. Mensagens de ajuda e erro estão associadas aos controles.

## Dados e feedback
`pp-feature-list`, `pp-progress` e `pp-data-table` completam a linguagem visual. Toast, modal, drawer e empty state existentes foram mantidos como fontes únicas para feedback, evitando `alert()` e `confirm()` nativos.

## Breakpoints
- Desktop: três colunas para cards e planos.
- Tablet: duas colunas e resumo do wizard no fluxo.
- Mobile: uma coluna, hero empilhado, stepper horizontal e ações flexíveis.
