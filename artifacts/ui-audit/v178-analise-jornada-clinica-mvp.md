# Análise da Jornada Clínica MVP — v1.78.0

| Tela | Rota | Controller/action | Estado atual | Próxima ação | Ação real existente | Ação sem backend | Correção feita | Pendência restante |
|---|---|---|---|---|---|---|---|---|
| Pacientes | `/Pacientes` | `Pacientes.Index` | Lista API e blocos honestos | Selecionar registro real | Cadastro e histórico contratados | Vínculos sem paciente selecionado | Contexto Dados/Agenda/Triagem/Consultas/Faturamento | Homologar payload longitudinal |
| Agendamentos | `/Agendamentos` | `Agendamentos.Index/ExecutarAcao` | Cards da API | Derivada do status | confirmar, check-in, reagendar, cancelar com motivo | chamada e consulta sem vínculo | capacidades conservadoras por status | Homologar matriz completa de status |
| Saúde 360 | `/Saude360` | módulo Saúde 360 | Painel da API | Abrir registro/vínculo | rotas clínicas existentes | contador quando API falha | total indisponível em erro e painel de próxima ação | Homologar indicadores reais |
| Triagem | `/Triagem` | `Triagem.Index/Create/Salvar` | formulário API | salvar dados válidos | salvar com validação server-side | finalizar e encaminhar separados | regras visíveis e CTAs indisponíveis com motivo | endpoints transacionais separados |
| Consultas | `/Consultas` | `Consultas.Index/Atendimento` | workspace por consulta real | salvar/finalizar | rascunho, finalização, prescrição | pagamento sem conta | faturamento recebe `consultaId` real | homologar status final e conta gerada |
| Faturamento clínico | `/FaturamentoClinico` | `FaturamentoClinico.Index` | contas da API | conferir origem | filtro por Atendimento/Origem ID | outras ações financeiras | filtro clínico real e mensagem de vínculo | aprovação/glosa/exportação |
