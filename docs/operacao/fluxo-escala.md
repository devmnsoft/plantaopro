# Fluxo operacional de Escala

## Status oficiais
`SOLICITADA -> CONFIRMADA -> REALIZADA`

Alternativos: `RECUSADA`, `CANCELADA`, `SUBSTITUIDA`, `NAO_COMPARECEU`.

## Regras centrais
- Médico não pode solicitar mesmo plantão duas vezes.
- Solicitação valida conflito de horário.
- Médico inativo é bloqueado.
- Confirmação reduz vaga disponível do plantão.
- Cancelamento de escala confirmada devolve vaga.
- Recusa/cancelamento/substituição exigem justificativa.
- Substituição exige médico elegível.
- `REALIZADA` permite geração de pagamento.
- `NAO_COMPARECEU` não gera pagamento.
- Toda ação crítica deve gerar auditoria.

## Endpoints principais
- `GET /api/escalas`
- `GET /api/escalas/{id}`
- `POST /api/escalas`
- `POST /api/escalas/{id}/confirmar`
- `POST /api/escalas/{id}/recusar`
- `POST /api/escalas/{id}/cancelar`
- `POST /api/escalas/{id}/substituir`
- `POST /api/escalas/{id}/realizar`
- `POST /api/escalas/{id}/nao-compareceu`
