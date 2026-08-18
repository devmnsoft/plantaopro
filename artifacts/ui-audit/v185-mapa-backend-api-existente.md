# Mapa backend/API existente — v1.85.0

| Fluxo | Controller | Service/repository | Persistência | Situação |
|---|---|---|---|---|
| Check-in | `AgendamentosController` | `Saude360ClinicalService.AcaoAsync` | `agendamentos`, `agendamento_checkins` | real e idempotente |
| Consulta | `ConsultasWorkspaceController` | `IConsultaApplicationService` / `ConsultaRepository` | `consultas`, `atendimentos`, `agendamentos`, `fila_atendimento`, `clinica_contas_receber`, histórico | transação real; hardened nesta PR |
| Plantão | `PlantoesController` | `PlantaoService` | `plantoes`, `plantao_historico` | publicar/cancelar reais |
| Escala | `EscalasController` | `EscalaService` | `escalas`, `escala_historico`, `plantoes` | confirmar/substituir/presença/ausência reais |
| Pagamento | `FinanceiroController` | `FinanceiroService` | pagamentos e auditoria existentes | geração/confirmação/cancelamento reais nas rotas existentes |
| Fechamento operacional | somente Web com modelo indisponível | nenhum agregado comprovado | não comprovada | mantido desabilitado |
