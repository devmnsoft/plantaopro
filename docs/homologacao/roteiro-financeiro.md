# Roteiro de homologação — Financeiro

## Escopo
Validar geração, confirmação, cancelamento e contestação de pagamentos médicos.

## Passos
1. Entrar como `FINANCEIRO`.
2. Abrir Financeiro e conferir cards de pendentes, pagos, atrasados e valor pago.
3. Filtrar por médico, hospital, status e período.
4. Gerar pagamento para escala `realizado`.
5. Tentar gerar pagamento duplicado para a mesma escala e validar bloqueio.
6. Confirmar pagamento informando valor pago, data e forma.
7. Cancelar pagamento pendente com justificativa.
8. Entrar como médico e contestar pagamento próprio com motivo.
9. Entrar como financeiro e resolver contestação.

## Critérios de aceite
- Valor negativo é recusado.
- Confirmação exige valor, data e forma.
- Contestação duplicada aberta é bloqueada.
- Auditoria e notificação são geradas nas ações financeiras.
