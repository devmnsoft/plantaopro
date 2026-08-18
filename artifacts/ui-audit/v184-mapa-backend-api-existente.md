# Mapa backend/API existente — v1.84.0

| Endpoint | Serviço | Tabela | Action MVC/view | Lacuna |
|---|---|---|---|---|
| `POST api/agendamentos/{id}/checkin` | `Saude360ClinicalService.AcaoAsync` | `agendamentos`, `agendamento_checkins` | `Agendamentos.ExecutarAcao` / `AgendaPremium` | executar integração PostgreSQL |
| `POST api/triagens/{id}/finalizar-tipado` | atualização + ação + histórico/auditoria clínica | `triagens` | `Triagem.Finalizar` / `Saude360/Formulario` | executar integração PostgreSQL |
| rotas de consulta | `Saude360ClinicalService` | `consultas` | workspace de consulta | faturamento idempotente sem valor inventado |
| rotas de plantão | serviço operacional | `plantoes` | detalhes de plantão | validar ciclo completo em runtime |
| rotas de escala | serviço operacional/state machine | `escalas` | telas de escala | capacidades por registro para habilitar botões |
| rotas financeiras | serviços financeiro/pagamento | tabelas financeiras | Financeiro/Pagamentos | origem, valor, status e idempotência consolidados |

O projeto usa Dapper/Npgsql e auditoria central/legada. Não foi criada tabela nesta entrega.
