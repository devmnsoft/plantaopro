# Matriz de rotas e ações reais — v1.86.0

| Ação | Rota | Persistência | UI | Estado |
|---|---|---|---|---|
| Gerar pagamento por escala realizada | `/api/financeiro/pagamentos/gerar` | pagamentos/histórico | Financeiro | existente real |
| Confirmar baixa detalhada | `/api/financeiro/pagamentos/{id}/confirmar` | pagamentos/histórico | Financeiro/Details | existente real |
| Marcar pago canônico | `/api/pagamentos/{id}/marcar-pago` | pagamentos/histórico | equivalente de baixa disponível | novo real |
| Contestar pagamento | `/api/pagamentos/{id}/contestar` | pagamentos/histórico | Financeiro/Details | novo real |
| Resolver contestação | — | — | desabilitada | pendente honesta |
| Mutar fechamento | — | — | desabilitada | pendente honesta |
