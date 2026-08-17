# Matriz de empty states — v1.78.0

| Tela/bloco | Estado vazio honesto | Não presume |
|---|---|---|
| Paciente: Dados/Agenda/Triagem/Consultas/Faturamento | selecione paciente retornado pela API | histórico ou vínculo |
| Agenda filtrada | nenhum agendamento real retornado | paciente/status |
| Saúde 360 com erro | total indisponível | contador zero |
| Triagem | requer vínculo e dados mínimos | classificação |
| Consulta → faturamento | nenhuma conta com origem correspondente | pendência ou valor |
| Faturamento filtrado | nenhum resultado para os filtros | pago, convênio ou competência |
