# Financeiro médico

## Ciclo de pagamento
1. Escala é marcada como realizada.
2. Financeiro gera pagamento pendente.
3. Sistema bloqueia pagamento duplicado para a escala.
4. Financeiro confirma pagamento com valor, data e forma.
5. Médico visualiza pagamento confirmado.
6. Notificação e auditoria são registradas.

## Regras
- Apenas escala realizada pode gerar pagamento.
- Escala com não comparecimento não gera pagamento automático.
- Valor não pode ser negativo.
- Cancelamento exige justificativa.
- Contestação exige motivo.
- Médico só contesta pagamento próprio.
- Não pode existir contestação aberta duplicada.

## Evidências para homologação
- Tela de lista com filtros.
- Detalhe com timeline.
- Comprovante lógico da confirmação.
- Auditoria da geração e confirmação.
