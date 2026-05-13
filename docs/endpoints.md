# Endpoints principais

## Escalas
- GET `/api/escalas` (filtros: medicoId, plantaoId, status, dataInicio, dataFim, hospitalId, especialidadeId, page, pageSize)
- GET `/api/escalas/{id}`
- GET `/api/medicos/me/plantoes`
- POST `/api/plantoes/{id}/aceitar`
- POST `/api/escalas/{id}/confirmar`
- POST `/api/escalas/{id}/recusar`
- POST `/api/escalas/{id}/cancelar`
- POST `/api/escalas/{id}/substituir`
- POST `/api/escalas/{id}/marcar-realizado`

## Financeiro
- GET `/api/financeiro/pagamentos`
- GET `/api/financeiro/pagamentos/{id}`
- POST `/api/financeiro/pagamentos/gerar`
- POST `/api/financeiro/pagamentos/{id}/confirmar`
- POST `/api/financeiro/pagamentos/{id}/cancelar`
- GET `/api/financeiro/meus-pagamentos`
- GET `/api/financeiro/meus-pagamentos/{id}`

## Notificações
- GET `/api/notificacoes`
- GET `/api/notificacoes/nao-lidas`
- PUT `/api/notificacoes/{id}/lida`
- PUT `/api/notificacoes/lidas`

## Dashboard
- GET `/api/dashboard`
- GET `/api/mobile/home`
