# Pendências operacionais

Pendências operacionais centralizam riscos e tarefas da coordenação/financeiro/admin:

- `GET /api/pendencias`
- `GET /api/pendencias/resumo`
- `POST /api/pendencias`
- `POST /api/pendencias/{id}/atribuir`
- `POST /api/pendencias/{id}/resolver`
- `POST /api/pendencias/{id}/adiar`
- `POST /api/pendencias/recalcular`

Resolver exige observação; recálculo cria pendências para plantões abertos com vagas nos próximos 14 dias.
