# Matriz v1.60 — fluxos e endpoints

| Módulo | Ação | Rota/controller | Endpoint/API | Permissão | Confirmação | Feedback | Status |
|---|---|---|---|---|---|---|---|
| Saúde 360 | Consultar jornada | `ClinicaDashboard/Index` | `GET api/clinica-dashboard/resumo` | roles Saúde 360 + tenant | não aplicável | erro/empty state | implementado |
| Agendamentos | Listar | `Agendamentos/Index` | `GET api/agendamentos` | roles Saúde 360 + tenant | não | erro/empty state | implementado |
| Agendamentos | Confirmar | `Agendamentos/ExecutarAcao` | `POST api/agendamentos/{id}/confirmar` | sessão/roles + API | modal | loading + toast/erro | implementado |
| Agendamentos | Check-in | `Agendamentos/ExecutarAcao` | `POST api/agendamentos/{id}/checkin` | sessão/roles + API | modal | loading + toast/erro | implementado |
| Agendamentos | Cancelar com motivo | `Agendamentos/ExecutarAcao` | `POST api/agendamentos/{id}/cancelar` | sessão/roles + API | modal e motivo obrigatório | loading + toast/erro | implementado |
| Agendamentos | Reagendar | `Agendamentos/Edit` | `PUT api/agendamentos/{id}` via `Save` | sessão/roles + API | formulário | erros preservam dados | implementado |
| Agendamentos | Chamar paciente | — | endpoint não localizado | pendente | necessária | necessária | desabilitado |
| Agendamentos | Abrir triagem | `Triagem/Create?agendamentoId=` | vínculo enviado ao formulário | roles Saúde 360 | não | validação do formulário | implementado |
| Triagem | Criar/editar | `Triagem/Salvar` | `POST/PUT api/triagens` | assistencial + tenant | não | summary acessível | implementado |
| Triagem | Finalizar/encaminhar | — | endpoint específico não localizado | pendente | necessária | necessário | pendente |
| Consultas | Atender/salvar rascunho | `Consultas/Atendimento/{id}` | `api/consultas` | assistencial + tenant | conflito/finalização em dialog | indicador de salvamento | implementado |
| Consultas | Prescrever | `Prescricoes/Editor/{consultaId}` | `api/prescricoes` | assistencial + tenant | conforme editor | retorno do editor | implementado |
| Pacientes | Consultar histórico | `Pacientes/Historico` | API de pacientes | assistencial + tenant | não | erro/empty state | implementado |
| Fechamentos | Conferir/aprovar/devolver | — | contrato não localizado | pendente | necessária | necessário | pendente |
| Financeiro | Consultar composição | `Financeiro/Index`, `Details` | API de pagamentos | perfil financeiro + tenant | não | erro/empty state | implementado |
| Financeiro | Aprovar/pagar/contestar | — | contratos completos não localizados | pendente | necessária | necessário | desabilitado |
| Convites | Abrir plantão | `Plantoes/Details` | leitura real do plantão | perfil operacional | não | página de detalhe | implementado |
| Convites | Convidar outro | `CentralEscala/Index` | fluxo real da cobertura | perfil operacional | conforme fluxo | conforme fluxo | implementado |
| Convites | Reenviar/cancelar | — | endpoint auditável não localizado | pendente | necessária | necessário | pendente |
| Notificações | Consultar/preferências | `Notificacoes`, `Preferencias` | serviço autenticado existente | usuário autenticado | não | empty state honesto | implementado |
| Command Palette | Pesquisar e navegar | shell / `command-palette.js` | `GET /GlobalSearch` | resultado filtrado no servidor | não | loading/erro/sem resultado | implementado |
| Relatórios | Abrir visões | `Relatorios/*` | actions existentes | por controller/perfil | não | página/empty state | implementado |
| Relatórios | Favoritos/agendamento | — | persistência/fila ausente | pendente | — | explicação na interface | desabilitado |
| Configurações | Abrir áreas | `Configuracoes/Index` | controllers reais por área | por controller/perfil | nas telas sensíveis | página/erro | implementado |
| Admin SaaS | Consultar tenant/plano | `AdminSaas/Index` | serviços SaaS existentes | admin SaaS | não | erro/empty state | implementado |
