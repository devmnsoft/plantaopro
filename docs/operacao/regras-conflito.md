# Regras de conflito de horário

## Sobreposição
Um conflito existe quando:

```text
novoInicio < escalaExistenteFim
AND
novoFim > escalaExistenteInicio
```

## Status considerados
- `SOLICITADA` / `solicitado`
- `CONFIRMADA` / `confirmado`
- `EM_ANDAMENTO` / `em_andamento`

## Status ignorados
- `CANCELADA`
- `RECUSADA`
- `SUBSTITUIDA`
- `REALIZADA` não bloqueia novos plantões futuros quando não há sobreposição ativa.

## Endpoints
- `POST /api/conflitos/verificar`
- `GET /api/conflitos/medico/{medicoId}`
- `GET /api/conflitos/plantao/{plantaoId}`

## Retorno
O retorno usa `ApiResponse<ConflitoHorarioResultadoDto>` com `PossuiConflito`, `TotalConflitos`, `Grau`, `Mensagem` e lista de conflitos.
