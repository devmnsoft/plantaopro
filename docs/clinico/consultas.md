# Consultas médicas — Saúde 360 Fase 5.2

A jornada de consulta médica conecta triagem finalizada, atendimento, CID, conduta, prescrição e impressão/resumo. Todos os endpoints exigem autenticação, respeitam `cliente_id`/`tenant_id` e registram auditoria clínica em acessos, histórico e impressão.

## Fluxo operacional

1. A recepção cria paciente/agendamento e realiza check-in.
2. A triagem registra sinais vitais e finaliza o encaminhamento.
3. O médico inicia a consulta, mudando o status para `EM_ATENDIMENTO`.
4. O atendimento registra anamnese, exame físico, diagnóstico/CID e conduta.
5. A prescrição é vinculada à consulta.
6. A consulta é finalizada como `FINALIZADA` e o resumo pode ser impresso com auditoria.
7. O histórico do paciente reúne consultas, prescrições e trilhas clínicas permitidas por perfil.

## Endpoints principais

- `GET /api/consultas`
- `GET /api/consultas/{id}`
- `POST /api/consultas`
- `PUT /api/consultas/{id}`
- `POST /api/consultas/{id}/iniciar`
- `POST /api/consultas/{id}/finalizar`
- `POST /api/consultas/{id}/cancelar`
- `GET /api/consultas/paciente/{pacienteId}`
- `GET /api/consultas/{id}/historico`
- `GET /api/consultas/{id}/resumo`

## Segurança e LGPD

- Recepção e financeiro não devem acessar evolução clínica completa.
- Médico acessa consultas próprias ou autorizadas dentro do tenant.
- Auditor clínico acessa somente leitura.
- Logs técnicos não devem conter anamnese, diagnóstico, prescrição ou posologia.
