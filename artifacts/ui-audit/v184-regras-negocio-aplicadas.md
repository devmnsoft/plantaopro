# Regras de negócio aplicadas — v1.84.0

## Triagem
- O identificador da triagem e o paciente devem ser reais.
- A classificação de risco é obrigatória na finalização.
- Medidas fora de faixas plausíveis são recusadas.
- Emergência e muito urgente exigem observação clínica na camada Web.
- A API persiste o formulário antes da transição para `FINALIZADA` e usa histórico/auditoria existentes.
- Registro novo continua com “Finalizar” desabilitado até ser salvo.

## Check-in preservado
- Apenas `AGENDADO` ou `CONFIRMADO` pode transicionar.
- `agendamento_checkins` impede duplicidade ativa e registra usuário/hora do banco.
- Recurso inexistente e conflito não retornam falso sucesso.
