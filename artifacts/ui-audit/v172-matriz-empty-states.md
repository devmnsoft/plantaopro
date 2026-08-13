# Matriz de empty states v1.72.0

| Superfície | Estado honesto |
|---|---|
| Saúde 360 | “Nenhum registro encontrado”; não mostra etapas quantitativas sem indicador retornado. |
| Agenda | “Agenda sem registros”; explica que pacientes demonstrativos não são exibidos. |
| Triagem e Consultas | coleção vazia orienta a voltar ao fluxo, sem inventar paciente ou risco. |
| Pacientes | ausência de cadastros e relações é exibida sem timeline sintética. |
| Plantões e Escalas | ausência de dados não produz cobertura, conflito ou pagamento presumido. |
| Fechamentos | pendências e timeline só são renderizadas quando presentes no view model. |
| Financeiro e Pagamentos | ausência de títulos não gera valores, competência ou histórico. |
| Minha Central | cada filtro pode ficar vazio sem substituir o resultado por tarefas fictícias. |
| Notificações | ausência de eventos reais mantém contador e drawer vazios. |
