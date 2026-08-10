# Mapa de componentes v1.53

| Componente | Arquivo | Onde aplicado | Estado | Pendência |
|---|---|---|---|---|
| `pp-alert` e painéis semânticos | `v153-feedback-experience.css` | Plantões, auth e uso global | Pronto | Expandir legado |
| `pp-toast` | CSS + `plantaopro-toast.js` | TempData e AJAX global | Pronto | Nenhuma |
| `pp-modal` / `pp-confirm-dialog` | `_ConfirmModal.cshtml`, `plantaopro-ui.js` | Layout global | Pronto | Textos específicos por ação legado |
| `pp-delete-confirmation` | `_ConfirmModal.cshtml` | Confirmações danger | Pronto | Motivo opcional por domínio |
| `pp-inline-validation` / `pp-form-error` | CSS + `form-experience.js` | Auth, Pacientes, Agendamentos, Plantões | Pronto | Expandir legado |
| `pp-field-help` | CSS | Auth, Pacientes, Agendamentos | Pronto | Expandir legado |
| `pp-form-section` / grid / card | CSS | Pacientes, Agendamentos, Plantões | Pronto | Escalas e clínicas |
| `pp-form-footer` / sticky footer | CSS | Pacientes, Agendamentos, Plantões | Pronto | Consultas longas |
| `pp-loading-state` | CSS + UI runtime | AJAX e catálogo | Pronto | Aplicar em relatórios |
| `pp-empty-state` | CSS + `_EmptyState.cshtml` | Compartilhado | Pronto | Textos por jornada |
| `pp-update-banner` | CSS | Catálogo global | Pronto | Concorrência por tela |
| `pp-unsaved-changes-banner` | CSS + `form-experience.js` | Forms com `data-unsaved-form` | Pronto | Ativar no legado |
| Banners contextuais | CSS | Forgot/Reset e catálogo global | Pronto | Financeiro/Configurações |
