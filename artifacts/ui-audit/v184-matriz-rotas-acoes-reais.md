# Matriz de rotas e ações reais — v1.84.0

| Tela | Ação real | Backend | Estado da UI |
|---|---|---|---|
| `/Agendamentos` | confirmar, check-in, cancelar com motivo | `/api/agendamentos/{id}/{ação}` | habilitada por status, loading/modal/toast |
| `/Triagem/Edit/{id}` | finalizar triagem | `/api/triagens/{id}/finalizar-tipado` | habilitada somente com ID persistido; submit com loading |
| `/Triagem/Create` | salvar | `/api/triagens` | real; finalizar desabilitado até persistir |
| `/Consultas/Atendimento/{id}` | salvar/finalizar consulta | API de consultas/workspace | preservada; faturamento financeiro automático não alegado |
| `/Plantoes`, `/Escalas` | ações operacionais existentes | APIs operacionais | condicionadas; lacunas continuam bloqueadas |
| `/Fechamentos`, `/Financeiro`, `/Pagamentos` | navegação/detalhe real | APIs atuais | mutações sem contrato suficiente continuam desabilitadas |
