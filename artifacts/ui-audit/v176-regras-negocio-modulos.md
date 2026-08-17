# Regras de negócio por módulo — v1.76.0

| Módulo | Pré-condições e transições | Estado honesto na interface |
|---|---|---|
| Agenda/Agendamentos | Status recebido define check-in, triagem, consulta, cancelamento e próxima ação; cancelar exige motivo | Sem vínculo, a ação não navega; espera/atraso só é calculado com horários reais |
| Triagem | Finalização exige classificação e mínimos; temperatura, saturação, pressão e frequência devem passar pelas validações clínica e de servidor; risco alto exige observação | Sem endpoint/vínculo, salvar ou finalizar fica indisponível com motivo |
| Consultas | Estado é retornado pelo backend; conduta mínima precede finalização; faturamento exige `AtendimentoId`/`OrigemId` | Prescrição, CID, histórico e destinos financeiros não aparecem como ações falsas |
| Pacientes | Documento sensível é minimizado/mascarado; edição e inativação exigem rota, policy e escopo | Abas sem histórico mostram empty state, não conteúdo sintético |
| Plantões | Unidade, especialidade, início/fim e estado precedem publicação; cancelamento/substituição exigem motivo | Cobertura e risco derivam apenas das vagas e escalas retornadas |
| Escalas | Médico, CRM, plantão e status são obrigatórios; confirmação/presença/substituição exigem transição real | Conflito e pagamento só aparecem quando retornados |
| Fechamentos | Realizado → conferência → divergência ou aprovação → financeiro → pagamento; devolver/divergir exige motivo | Não permite financeiro antes da aprovação nem pagamento antes do financeiro |
| Faturamento | Origem, competência, convênio, valor e status são opcionais até serem informados pela API | Ausente aparece como “não informado”, nunca zero ou pago |
| Financeiro/Pagamentos | Itens surgem somente após geração real e avançam conforme status retornado | Resumos contam somente coleções reais; exportação exige endpoint |
| Relatórios | Geração, histórico e exportação dependem de backend e permissão | Categoria em implantação fica desabilitada e explica a dependência |
| Notificações | Itens/contador vêm da API same-origin e são filtrados por tipo real | 401/403/404 e filtro vazio possuem mensagens distintas |
| Command Palette | Catálogo contém somente rotas existentes e same-origin | Ctrl+K, setas, Enter, Escape, ARIA e retorno de foco compõem o contrato |

## Fonte de verdade

Views derivam disponibilidade de IDs, status e permissões recebidos; validação definitiva, autorização, concorrência e transição de estado pertencem ao backend. A UI não promove ausência para um valor padrão de negócio.
