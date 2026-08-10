# Mapa de componentes v1.53

| Componente | Arquivo | Onde aplicado | Estado | Pendência |
|---|---|---|---|---|
| `pp-alert` e painéis semânticos | `v153-form-feedback-experience.css` | Plantões e uso compartilhado | Pronto | Migrar alertas legados ao tocar cada view |
| `pp-toast` | `plantaopro-toast.js` + CSS v1.53 | Layout global | Pronto | Nenhuma |
| `pp-modal` / `pp-confirm-dialog` | `_ConfirmModal.cshtml` + CSS v1.53 | Layout global e ações `data-confirm` | Pronto | Enriquecer textos genéricos legados |
| `pp-delete-confirmation` | CSS v1.53 + confirmação declarativa | Exclusões com `data-confirm-type=danger` | Pronto | Motivo é específico do domínio |
| `pp-inline-validation` | CSS/JS v1.53 | Login, Pacientes e Agendamentos | Pronto | Migração incremental |
| `pp-field-help` | CSS/JS v1.53 | Formulários no layout | Pronto | Migração incremental |
| `pp-form-section` / `pp-form-footer` | CSS v1.53 | Formulários prioritários e API de classes | Pronto | Escalas Create/Edit não existem nesta árvore |
| `pp-loading-state` | CSS v1.53 | AJAX e API de classes | Pronto | Adotar em relatórios assíncronos futuros |
| `pp-empty-state` | CSS v1.53 / `_EmptyState` | Compartilhado | Pronto | Revisar cópias genéricas legadas |
| `pp-update-banner` | CSS v1.53 | API para configurações/concorrência | Pronto | Ligar a respostas 409 por endpoint |
| `pp-unsaved-changes-banner` | `form-experience.js` + CSS v1.53 | Formulários com ações | Pronto | Nenhuma |
