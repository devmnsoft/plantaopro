# Inteligência SaaS

A inteligência SaaS calcula score e classificação de saúde do cliente.

## Classificações

- `SAUDAVEL`: score igual ou superior a 80.
- `ATENCAO`: score entre 60 e 79.
- `RISCO`: score entre 35 e 59.
- `CRITICO`: score abaixo de 35.

## Critérios avaliados

- Inadimplência.
- Status suspenso ou cancelado.
- Inatividade operacional.
- Uso acima de 80% dos limites.
- Ausência de assinatura operacional.

## Endpoints

- `GET /api/saas-inteligencia/clientes/{clienteId}/saude`.
- `GET /api/saas-inteligencia/clientes/{clienteId}/alertas`.
- `GET /api/saas-inteligencia/clientes/{clienteId}/recomendacoes`.
- `POST /api/saas-inteligencia/clientes/{clienteId}/recalcular`.
