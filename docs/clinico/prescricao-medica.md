# Prescrição médica — Saúde 360 Fase 5.2

Prescrições são vinculadas a consultas e pacientes. O módulo suporta rascunho, finalização, cancelamento com justificativa, impressão auditada e modelos reutilizáveis por médico/tenant.

## Endpoints

- `GET /api/prescricoes`
- `GET /api/prescricoes/{id}`
- `POST /api/prescricoes`
- `PUT /api/prescricoes/{id}`
- `POST /api/prescricoes/{id}/finalizar`
- `POST /api/prescricoes/{id}/cancelar`
- `GET /api/prescricoes/paciente/{pacienteId}`
- `GET /api/prescricoes/consulta/{consultaId}`
- `GET /api/prescricoes/modelos`
- `POST /api/prescricoes/modelos`
- `POST /api/prescricoes/modelos/{id}/usar`

## Segurança

- Prescrição finalizada não deve ser editada sem permissão especial.
- Cancelamento exige motivo/justificativa.
- Impressão é acesso clínico sensível e deve ser auditada.
- Medicamento, posologia e orientações não devem ser enviados a logs técnicos.
