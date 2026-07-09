# QA CRUDs e telas funcionais — PlantãoPro

Status geral: **Funcional pendente QA**. Esta matriz não declara produção; registra o que está preparado para homologação em ambiente com SDK .NET, Docker e PostgreSQL.

| Área | Rotas/telas | Ações esperadas | Status | Observação |
|---|---|---|---|---|
| Pacientes | `/Pacientes`, `/Pacientes/Create`, `/Pacientes/Edit/{id}`, `/Pacientes/Details/{id}` | criar, editar, detalhar, buscar, filtrar, paginar, inativar com modal/toast | Funcional pendente QA | Validar CPF duplicado por tenant e paciente sem CPF com documento alternativo. |
| Agendamentos | `/Agendamentos`, `/Agendamentos/AgendaDia`, `/Agendamentos/CheckIn` | criar, editar, confirmar, check-in, reagendar, cancelar, falta, chamar, enviar triagem | Funcional pendente QA | Validar conflito de horário e transições de status no PostgreSQL. |
| Painel de chamada | `/PainelChamada`, `/PainelChamada/Tv`, `/PainelChamada/Historico` | chamar, rechamar, ausente, finalizar, histórico | Funcional pendente QA | TV deve mascarar dados sensíveis. |
| Triagem | `/Triagem`, `/Triagem/Fila`, `/Triagem/ClassificacaoRisco` | iniciar, editar, finalizar, cancelar, histórico | Funcional pendente QA | Validar bloqueio de edição após finalização e auditoria clínica. |
| Consultas | `/Consultas`, `/Consultas/Atendimento`, `/Consultas/HistoricoPaciente` | iniciar, finalizar, cancelar, imprimir, histórico | Funcional pendente QA | Médico só deve ver próprias consultas; recepção/financeiro sem evolução clínica. |
| CID | `/Cid`, `/Cid/Importar`, `/Cid/Favoritos`, `/Cid/MaisUsados` | buscar, importar, favoritar, inativar | Parcial | Importação CSV/URL depende de massa real. |
| Prescrições | `/Prescricoes`, `/Prescricoes/Create`, `/Prescricoes/Imprimir` | criar, editar, finalizar, cancelar, imprimir auditado | Funcional pendente QA | Bloquear alteração após finalizar. |
| Financeiro clínica | `/ClinicaFinanceiro`, `/ClinicaFinanceiro/Receber`, `/ClinicaFinanceiro/Caixa` | receber, caixa, repasse, glosa, cancelar com justificativa | Funcional pendente QA | Não expor conteúdo clínico. |
| Convênios/planos | `/Convenios`, `/PlanosSaude` | CRUD, contratos, autorizações, vínculo paciente/plano | Parcial | Faturamento/glosas exigem QA com dados de convênio. |
| Plantões/escalas/financeiro médico | `/Plantoes`, `/Escalas`, `/Financeiro` | publicar, convidar, aceitar/recusar, confirmar, pagamento, contestação | Funcional pendente QA | Validar conflitos, duplicidade e vaga negativa. |

## Evidências estáticas desta rodada

- Smoke Web/API ampliado para rotas principais, aceitando `200`, `204` ou `302` para rota protegida sem sessão e falhando em `404`/`500`.
- Testes contratuais adicionados para controllers principais, rotas de menus, endpoints smoke, padrões proibidos, segredos em `appsettings`, mobile sem URL fixa e docs de QA.
- Homologação funcional implementada e preparada para validação em ambiente com SDK .NET, Docker e PostgreSQL.
