# Recomendação de médicos

## Endpoint
- `GET /api/plantoes/{id}/medicos-recomendados?limite=20`

## DTO
`MedicoRecomendadoDto` expõe id, nome, CRM, especialidade, score, motivos, alertas, conflito, disponibilidade, convite existente e escala existente.

## Critérios do MVP
- Médico ativo.
- Especialidade compatível recebe maior score.
- Conflito de horário reduz disponibilidade e score.
- Médico já convidado ou já escalado aparece com alerta/badge para evitar duplicidade.

## Próximas melhorias
- Taxa histórica de aceite/cancelamento.
- Carga horária semanal ponderada.
- Avaliação média e score médico.
- Preferência de turno e disponibilidade declarada.
