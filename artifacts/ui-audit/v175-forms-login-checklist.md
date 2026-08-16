# Checklist de forms e login — v1.75.0

| Item | Login | Cadastro | Evidência |
|---|---|---|---|
| `pp-form` e labels associadas | OK estático | OK estático | Tag Helpers e classes do design system. |
| Erro junto ao campo e resumo | OK estático | OK estático | `asp-validation-for` e `asp-validation-summary`. |
| Foco no primeiro inválido | OK estático | OK estático | `data-focus-invalid` e `form-experience.js`. |
| Prevenção de duplo envio | OK estático | OK estático | `aria-busy`, desabilitação e spinner. |
| Alterações não salvas | Não aplicável | OK estático | `data-unsaved-form`. |
| Toggle de senha e Caps Lock | OK estático | Não aplicável | Controles acessíveis em `auth-login.js`. |
| Mobile e desktop | Pendente runtime | Pendente runtime | O smoke contém todos os oito viewports; execução bloqueada sem .NET. |
