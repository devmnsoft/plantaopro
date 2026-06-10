# LGPD e segurança clínica

## Controles

- Tenant obrigatório em queries clínicas.
- Dados sensíveis fora dos logs técnicos.
- Consentimentos em `paciente_consentimentos`.
- Finalidade de tratamento em `pacientes.finalidade_tratamento`.
- Histórico e auditoria para acesso e ações críticas.

## Restrições por perfil

Recepção acessa cadastro, agendamento, check-in e painel básico. Triagem/enfermagem acessam fila e triagens necessárias. Médico visualiza agenda e triagens dos atendimentos permitidos. Financeiro não acessa triagem clínica completa.
