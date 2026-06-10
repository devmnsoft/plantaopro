# Triagem clínica

Triagem registra queixa principal, sinais vitais, alergias, medicamentos em uso, classificação de risco, observações e status.

## Classificação de risco

EMERGENCIA, MUITO_URGENTE, URGENTE, POUCO_URGENTE e NAO_URGENTE.

## Regras

- Triagem exige paciente.
- Pode ser vinculada ao agendamento.
- Início altera status para EM_TRIAGEM.
- Finalização altera status para FINALIZADA, cria encaminhamento e atualiza agendamento para AGUARDANDO_CONSULTA.
- Edição de triagem finalizada é bloqueada no SQL de atualização do serviço.
