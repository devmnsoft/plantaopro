# Regras de negócio aplicadas — v1.85.0

## Consulta
- Paciente, médico, atendimento, anamnese, diagnóstico, conduta e CID principal são impeditivos reais.
- Cobranças particular/convênio/plano exigem valor líquido positivo; valores negativos ou líquidos inválidos retornam 422.
- Cortesia/isento exigem justificativa e não criam conta de valor fictício; faturamento posterior finaliza sem criar financeiro.
- Consulta, atendimento, agenda, fila, conta e histórico são tratados na mesma transação; conflito de versão retorna 409.
- O response informa `FinanceiroId` real e se faturamento pode ser aberto.

## Operação
- Cancelamento de plantão exige motivo.
- Presença é alias canônico da transição persistida para realizado e só aceita escala confirmada.
- Ausência só aceita escala confirmada e exige motivo, persistindo justificativa e histórico.
