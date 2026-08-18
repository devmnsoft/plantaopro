# Regras de negócio aplicadas — v1.86.0

- A baixa canônica bloqueia inexistente (404), pago novamente/status incompatível (409) e valor não positivo (422).
- O valor pago deriva de `valor_previsto` persistido; data de pagamento usa a data UTC da operação e nunca um placeholder.
- Contestação exige motivo não vazio, aceita somente pagamento pendente, preserva `valor_previsto` e persiste status/motivo.
- Ambas as transições bloqueiam a linha (`FOR UPDATE`), escrevem histórico, auditoria/notificação e somente retornam sucesso após commit.
- Resolução de contestação não foi simulada por ausência de workflow persistente.
