# Matriz de rotas e ações reais v1.72.0

| Rota | Fonte | Ações reais | Indisponibilidade honesta |
|---|---|---|---|
| `/Saude360` | `api/clinica-dashboard/resumo` | navegar pelas etapas existentes | contadores ausentes não são renderizados |
| `/Agendamentos` | `api/agendamentos` | confirmar, check-in, reagendar, cancelar com motivo, abrir triagem e jornada | chamar paciente desabilitado até existir endpoint |
| `/Triagem` | `api/triagens` | criar/editar e salvar com validação clínica server-side | fila vazia não cria paciente |
| `/Consultas` | `api/consultas` | abrir atendimento, salvar rascunho, vincular CID, prescrever e finalizar | histórico ausente apresenta estado vazio |
| `/Pacientes` | `api/pacientes` | criar, editar, detalhar e abrir histórico real | relações ausentes não são simuladas |
| `/Plantoes` | API de plantões | criar, editar, detalhar e abrir contexto | ações sem endpoint são desabilitadas |
| `/Escalas` | API de escalas | detalhar e solicitar substituição disponível | presença/pagamento dependem do backend correspondente |
| `/Financeiro` | API financeira | filtrar, abrir origem e detalhes existentes | valores e timeline ausentes não são calculados no cliente |
| `/Pagamentos` | API de pagamentos | abrir composição/origem e ações permitidas por status | nenhum pagamento demonstrativo é criado |
| `/MinhaCentral` | BFF de work items | assumir, mover, comentar, adiar, concluir e reabrir | 403/409 têm feedback contextual |
