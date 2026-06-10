# Agendamento clínico

Agendamentos vinculam paciente, médico/profissional, unidade, sala, especialidade, tipo, início, fim e status.

## Status

AGENDADO, CONFIRMADO, CHECKIN_REALIZADO, EM_TRIAGEM, AGUARDANDO_CONSULTA, EM_ATENDIMENTO, ATENDIDO, FALTOU, CANCELADO e REAGENDADO.

## Regras implementadas

- Escopo obrigatório por tenant.
- Bloqueio de conflito de horário para o mesmo médico.
- Fim deve ser maior que início.
- Cancelamento exige motivo/justificativa.
- Check-in somente para AGENDADO ou CONFIRMADO.
- Check-in gera registro de check-in, fila do painel e fila de triagem quando a migration da fase está aplicada.
