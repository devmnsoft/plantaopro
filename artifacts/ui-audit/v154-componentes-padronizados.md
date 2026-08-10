# Catálogo de componentes padronizados — v1.54

## Fundação

- Tokens oficiais: `--pp-bg`, `--pp-surface`, `--pp-surface-muted`, `--pp-surface-dark`, escala navy/blue/cyan/teal e cores semânticas.
- Layout: `pp-app`, `pp-main`, `pp-content`, `pp-topbar`.
- Navegação: `pp-breadcrumb`, `pp-user-menu`, `pp-user-menu__trigger`, `pp-user-menu__dropdown`.

## Formulários

- Composição: `pp-form-page`, `pp-form-card`, `pp-form-section`, `pp-form-section-header`.
- Grid: `pp-form-grid`; doze colunas no desktop e uma coluna no mobile.
- Campo: `pp-form-field`, `pp-form-label`, `pp-form-control`, `pp-form-help`, `pp-form-error`.
- Ações: `pp-form-actions`, com rodapé aderente em telas pequenas.
- Estados: foco visível, inválido com texto e ícone, desabilitado, somente leitura e ajuda contextual.

## Autenticação

- `pp-auth`, `pp-auth__shell`, `pp-auth-brand`, `pp-auth-panel`.
- Divisão 55/45 em desktop e fluxo de uma coluna no mobile.
- `pp-password-field` mantém controle e toggle alinhados.

## Feedback e overlays

- Toasts: `pp-toast-success`, `pp-toast-error`, `pp-toast-warning`, `pp-toast-info`.
- Persistência: `pp-unsaved-banner`.
- Confirmação: `pp-confirm-modal`; a variante destrutiva recebe `pp-delete-dialog`, ícone, impacto, ações distintas e loading.
- A região de toast usa `aria-live`; o modal usa `role="dialog"`, descrição associada e restauração de foco pelo controlador existente.

## Regras de manutenção

1. Usar tokens, nunca cores duplicadas dentro de views.
2. Não introduzir `!important` para resolver conflito.
3. Todo botão declara `type`; botões apenas com ícone recebem `aria-label`.
4. Todo erro de campo contém texto e é associado ao controle com `aria-describedby`.
5. Não usar `alert()` ou `confirm()` nativos; usar toast e confirmação PlantãoPro.
