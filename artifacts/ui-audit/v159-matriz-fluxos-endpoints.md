# Matriz de fluxos e endpoints — v1.59.0

| Módulo | Ação | Rota/controller web | Endpoint/API | Permissão | Confirmação | Feedback | Status |
|---|---|---|---|---|---|---|---|
| Saúde 360 | consultar jornada | `ClinicaDashboard/Index` | `GET api/clinica-dashboard/resumo` | roles Saúde 360 + tenant | não | alerta/empty state | implementado |
| Agendamentos | confirmar | `Agendamentos/ExecutarAcao` | `POST api/agendamentos/{id}/confirmar` | autenticação + roles Saúde 360 | modal | loading/toast/erro | implementado |
| Agendamentos | check-in | `Agendamentos/ExecutarAcao` | `POST api/agendamentos/{id}/checkin` | autenticação + roles Saúde 360 | modal | loading/toast/erro | implementado |
| Agendamentos | cancelar | `Agendamentos/ExecutarAcao` | `POST api/agendamentos/{id}/cancelar` | autenticação + roles Saúde 360 | modal + motivo | loading/toast/erro | implementado |
| Agendamentos | reagendar | `Agendamentos/ExecutarAcao` | `POST api/agendamentos/{id}/reagendar` | autenticação + roles Saúde 360 | modal | loading/toast/erro | implementado no BFF; UI usa edição até definir data |
| Triagem | salvar/finalizar | `Triagem/Edit` | `POST api/triagens/{id}/salvar`, `finalizar-tipado` | autenticação clínica | confirmação ao finalizar | validação API | implementado existente |
| Consultas | rascunho | `Consultas/Atendimento` | `POST api/consultas/{id}/salvar-rascunho` | perfil médico/tenant | não | workspace | implementado existente |
| Consultas | finalizar | `Consultas/Atendimento` | rota legada de finalização | perfil médico/tenant | sim | workspace | pendente de rota canônica |
| Pacientes | histórico | `Pacientes/Historico` | `GET api/pacientes/{id}/historico` | dados sensíveis + tenant | não | erro/empty state | implementado existente |
| Financeiro | contestar | — | — | a definir | sim | — | desabilitado por falta de backend |
| Fechamentos | gerar financeiro | — | — | a definir | sim | — | pendente de contrato canônico |
| Convites | histórico de tentativas | `Convites/Index` | — | autenticação | não | empty state | pendente de endpoint |
| Notificações | consultar | dropdown/layout | fonte de notificações existente | autenticação | não | empty state | implementado existente |
| Command Palette | busca global | `/GlobalSearch` | `GET /GlobalSearch?q=` | resultado filtrado por permissão | não | loading/vazio/erro | implementado existente |
