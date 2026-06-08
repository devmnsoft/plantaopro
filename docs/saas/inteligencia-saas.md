# Inteligência SaaS baseada em regras

O motor `SaasIntelligenceService` usa regras determinísticas; não há chamada para IA externa.

## Sinais avaliados

- Faturas vencidas.
- Cliente suspenso ou cancelado.
- Ausência de plantões recentes.
- Uso acima de 80% dos limites do plano.
- Ausência de assinatura ativa.

## Saídas

- Saúde do cliente: `SAUDAVEL`, `ATENCAO`, `RISCO` ou `CRITICO`.
- Alertas de risco e upgrade.
- Recomendações de Customer Success.
- Listas executivas para dashboard: clientes em risco, faturas vencidas, oportunidades de upgrade e funil comercial.
