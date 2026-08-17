# Matriz de rotas e ações reais — v1.78.0

| Rota | Ação real | Condição | Indisponível com motivo |
|---|---|---|---|
| `/Pacientes` | listar/cadastrar/editar/histórico | API e autorização | vínculos sem paciente real |
| `/Agendamentos` | confirmar, check-in, reagendar, cancelar | ID e status; motivo ao cancelar | chamada; consulta sem ConsultaId |
| `/Triagem` | criar/salvar | agendamento e mínimos válidos | finalizar/encaminhar separados |
| `/Consultas/Atendimento/{id}` | salvar, finalizar, prescrição | ConsultaId real | pagamento sem conta |
| `/FaturamentoClinico?atendimentoId={id}` | listar e filtrar origem | OrigemId/AtendimentoId correspondente | aprovação/glosa/exportação |
