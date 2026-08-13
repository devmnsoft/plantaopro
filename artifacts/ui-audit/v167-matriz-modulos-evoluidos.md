# v1.67 — matriz de módulos evoluídos

| Área | Evolução | Fonte dos dados | Mobile | Regressão |
|---|---|---|---|---|
| Financeiro | workspace, resumo previsto/pago/pendente/cancelado, consolidação e cartões | `ListPageViewModel<PagamentoResumoDto>` | tabela oculta e cards até 767 px | operational + layout |
| Login | contrato de shell/card/form auditado | modelo de login real | ações e benefícios empilhados | layout + form |
| Cadastro | contrato de stepper/grid/resumo auditado | formulário real | uma coluna | layout + form |
| Admin SaaS | contrato de cockpit auditado | view existente | layout em coluna | layout + SaaS |
| Dashboard | agenda, riscos, pagamentos e timeline auditados | `DashboardOverviewDto` | tabela/cards | layout + operational |
| Minha Central | filtros, drawer, 403/409 e seis ações protegidos | work items reais | cards/drawer | operational + feedback |
| Saúde 360 | oito etapas e validação clínica protegidas | serviços existentes | composição responsiva | operational |
| Smoke | Planos, 1440×900, forms/dialogs/overlays e limites estritos | navegador real | 8 viewports | layout |
