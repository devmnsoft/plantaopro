# QA endpoints Saúde 360

Roteiro de homologação após aplicar migrations:

- `POST /api/auth/login`
- `GET /api/pacientes`
- `GET /api/agendamentos`
- `GET /api/painel-chamada`
- `GET /api/triagens`
- `GET /api/consultas`
- `GET /api/cid`
- `GET /api/cid?termo=I10`
- `GET /api/prescricoes`
- `GET /api/clinica-financeiro/contas-receber`
- `GET /api/clinica-financeiro/caixa`
- `GET /api/pendencias-clinicas`
- `GET /api/clinica-dashboard/resumo`

Critério: sem erro 42703, sem erro 42P01 e sem exception técnica para usuário.
