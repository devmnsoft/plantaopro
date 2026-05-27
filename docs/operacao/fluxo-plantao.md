# Fluxo operacional de Plantão

## Status oficiais
`RASCUNHO -> ABERTO -> EM_ESCALA -> PREENCHIDO -> EM_ANDAMENTO -> REALIZADO -> ENCERRADO`

Também permitido: `CANCELADO` (com justificativa e auditoria).

## Regras centrais
- Plantão inicia em `RASCUNHO`.
- `data_fim` precisa ser maior que `data_inicio`.
- `valor >= 0`.
- `vagas > 0` e `vagas_disponiveis >= 0`.
- Publicação somente se status atual for `RASCUNHO`.
- Cancelamento exige justificativa e grava auditoria.
- Cancelamento com pagamento confirmado apenas para `ADMINISTRADOR_GLOBAL`.
- Quando vagas disponíveis zeram: `PREENCHIDO`.
- Se vaga for liberada, retorna para `ABERTO`.
- `REALIZADO` libera geração financeira.
- `ENCERRADO` depende de escalas/pagamentos finalizados.

## Endpoints principais
- `GET /api/plantoes`
- `GET /api/plantoes/{id}`
- `POST /api/plantoes`
- `PUT /api/plantoes/{id}`
- `POST /api/plantoes/{id}/publicar`
- `POST /api/plantoes/{id}/cancelar`
- `POST /api/plantoes/{id}/realizar`
- `GET /api/plantoes/{id}/historico`
- `GET /api/plantoes/{id}/escalas`
- `GET /api/plantoes/{id}/convites`

## Homologação rápida
1. Criar plantão válido.
2. Publicar plantão.
3. Confirmar escalas até zerar vagas.
4. Marcar realizado.
5. Validar geração de pagamento.
6. Encerrar após baixa financeira.
