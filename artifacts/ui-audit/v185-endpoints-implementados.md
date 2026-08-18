# Endpoints implementados e hardened — v1.85.0

| Método/rota | Controller | Service | Repository/tabela | Request | Response | Regra | HTTP | View | Gate |
|---|---|---|---|---|---|---|---|---|---|
| POST `/api/consultas/{id}/finalizar` | `ConsultasWorkspaceController` | `ConsultaApplicationService` | `ConsultaRepository`; consultas/contas a receber | `FinalizarConsultaRequest` | `FinalizarConsultaResponse` | prontuário e conduta; valor > 0 para cobrança; cortesia/isento com justificativa; transação/idempotência | 200/400/403/404/409/422 | Consultas/Atendimento | `V185BusinessActionsContractTests` |
| POST `/api/agendamentos/{id}/check-in` (alias legado: `/checkin`) | `AgendamentosController` | `Saude360ClinicalService` | agendamentos/agendamento_checkins/triagem_fila/painel_chamada_fila | `Saude360ActionRequest` | `Saude360RegistroDto` | existência, paciente vinculado, status AGENDADO/CONFIRMADO e prevenção de duplicidade | 200/404/409/422 | Agendamentos/AgendaPremium | `V185BusinessActionsContractTests` |
| POST `/api/plantoes/{id}/cancelar` | `PlantoesController` | `PlantaoService` | plantoes/histórico | `StatusRequest` | `ApiResponse` | motivo obrigatório antes da transição persistida | 200/404/409/422 | Plantoes/Details | contrato v185 |
| POST `/api/escalas/{id}/presenca` | `EscalasController` | `EscalaService` | escalas/plantoes/histórico | `CompleteEscalaRequest` | `ApiResponse<string>` | somente confirmada; persiste realização e histórico | 200/404/409 | Escalas/Details | contrato v185 |
| POST `/api/escalas/{id}/ausencia` | `EscalasController` | `EscalaService` | escalas/histórico | `CompleteEscalaRequest` | `ApiResponse<string>` | somente confirmada e motivo obrigatório | 200/404/409/422 | Escalas/Details | contrato v185 |

O check-in, publicação e confirmação já eram persistidos e foram preservados como contratos reais; não foram duplicados.
