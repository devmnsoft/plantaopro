# Funcionalidades fechadas — v1.77.0

| Funcionalidade | Fechamento | Evidência |
|---|---|---|
| Dashboard do médico | Não é mais desviado antes de renderizar o contrato por perfil | `HomeController.Dashboard` |
| Dashboard por perfil | Contexto e próxima ação real por Admin, Coordenação, Médico, Hospital, Financeiro e Operador | `Views/Home/Dashboard.cshtml` |
| KPIs honestos | Fallback técnico não aparece como zero operacional | flag `DashboardDataAvailable` e empty state |
| Design de fechamento | Bloco de prioridade e responsividade aditivos, sem `!important` | `v177-product-completion.css` |
| Smoke v177 | 23 rotas, oito viewports e contrato `profileDashboardVisible` | `visual-smoke.mjs` |
| Priorização | Backlog P0–P4 e sequência dos oito módulos documentados | artefatos v177 |

“Fechada” aqui significa implementação ou contrato estático concluído; não significa homologação de runtime, registrada separadamente.
