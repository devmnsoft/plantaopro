# Financeiro médico

## Fluxo homologável
1. Escala precisa estar `realizado`.
2. Financeiro gera pagamento com previsão e observação opcional.
3. Sistema bloqueia duplicidade por escala.
4. Médico visualiza pagamento em Minha Agenda/API mobile.
5. Financeiro confirma pagamento com valor positivo, data e forma.
6. Cancelamento exige justificativa.

## Status previstos
- `pendente`
- `disponivel`
- `em_processamento`
- `pago`
- `atrasado`
- `cancelado`
- `contestado`

## Homologação
Validar que notificações e auditoria são gravadas em geração, confirmação, cancelamento e contestação/resolução quando habilitadas na base incremental.
