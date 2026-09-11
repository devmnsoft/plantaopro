# PlantãoPro v2.15.9 — Saúde 360, agenda e recepção

## Referência e continuidade

- Commit de referência inspecionado: `34e5757` (merge da v2.15.8).
- A linha atual contém os merges v2.15.6 (`550fed7`), v2.15.7 (`9dbd21e`) e v2.15.8 (`34e5757`).
- A implementação canônica consumida pelo MVC é `Saude360ClinicalService`, exposta pelos controllers `/api/pacientes`, `/api/agendamentos` e `/api/painel-chamada`. `AgendaOperacionalController` permanece como agenda de plantões e não foi duplicado.
- Escopo desta rodada: cadastro/identificação, reserva, estados, recepção/fila e painel. Consulta, prescrição, convênio avançado e pagamento permanecem fora do escopo.

## Matriz operacional

| Estado do agendamento | Ações permitidas | Próximo estado |
|---|---|---|
| AGENDADO | confirmar, cancelar, reagendar, marcar falta | CONFIRMADO, CANCELADO, REAGENDADO, FALTOU |
| CONFIRMADO | check-in, cancelar, reagendar, marcar falta | CHECKIN_REALIZADO, CANCELADO, REAGENDADO, FALTOU |
| CHECKIN_REALIZADO | encaminhar à triagem/consulta, cancelar | EM_TRIAGEM, AGUARDANDO_CONSULTA, CANCELADO |
| EM_TRIAGEM | encaminhar à consulta, cancelar | AGUARDANDO_CONSULTA, CANCELADO |
| AGUARDANDO_CONSULTA | iniciar atendimento, cancelar, marcar falta | EM_ATENDIMENTO, CANCELADO, FALTOU |
| EM_ATENDIMENTO | concluir ou cancelar | ATENDIDO, CANCELADO |
| CANCELADO, REAGENDADO, FALTOU, ATENDIDO | nenhuma transição implícita | terminal; retorno exige ação explícita/auditada |

Status do agendamento, status da entrada de fila e etapa clínica permanecem separados. `CHAMADO` identifica somente a chamada da fila; não significa consulta realizada.

## Controles entregues

- CPF continua opcional. Quando informado, é normalizado, validado e protegido por unicidade por cliente; listagens de TV não expõem documento.
- Transições inválidas passam a retornar conflito antes da mutação.
- Constraints parciais tornam check-in e entradas ativas de fila idempotentes mesmo sob concorrência.
- Exclusão temporal no PostgreSQL arbitra dupla reserva do mesmo profissional dentro do cliente.
- Índices suportam agenda diária e fila com desempate estável por `id`.
- A credencial pública do painel existente continua sendo hash, expira, pode ser revogada e é vinculada ao painel/unidade.

## Evidência e limitações

Os testes unitários cobrem CPF e matriz de transições. Os testes PostgreSQL, navegação autenticada, reconexão SignalR e screenshots dependem do ambiente integrado com banco, tenant, módulos e usuários de recepção. Eles devem ser executados no CI/homologação antes de considerar a rodada homologada; este relatório não inventa execução desses cenários.
