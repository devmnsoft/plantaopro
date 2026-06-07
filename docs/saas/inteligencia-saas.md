# Inteligência SaaS

O motor é determinístico, sem IA externa. Ele usa regras para calcular saúde, riscos, oportunidades e próximas ações.

## Endpoints

- `GET /api/inteligencia/saas/resumo`
- `GET /api/inteligencia/clientes/{clienteId}/saude`
- `GET /api/inteligencia/clientes/{clienteId}/alertas`
- `GET /api/inteligencia/clientes/{clienteId}/proximas-acoes`
- `POST /api/inteligencia/sugerir-plano`
- `POST /api/inteligencia/recalcular`
