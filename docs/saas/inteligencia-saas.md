# Inteligência SaaS baseada em regras

O motor não usa IA externa. Ele calcula saúde do cliente com base em faturas vencidas, status contratual, plantões recentes e uso dos limites do plano.

## Classificações

- `SAUDAVEL`: uso recorrente, faturas em dia e atividade operacional.
- `ATENCAO`: sinais de queda, uso alto ou pouca atividade.
- `RISCO`: inadimplência, inatividade ou assinatura ausente.
- `CRITICO`: suspensão, cancelamento, inadimplência recorrente ou operação parada.

## Alertas

Alertas incluem limite próximo, sem uso, inadimplência, oportunidade de upgrade e necessidade de contato de Customer Success.
