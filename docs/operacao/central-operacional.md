# Central Operacional

## Objetivo
Dar visão de sala de controle para coordenação e operação com foco em plantões críticos, pendências e conflitos.

## Indicadores
- Plantões abertos.
- Plantões sem médico.
- Escalas solicitadas.
- Convites pendentes.
- Conflitos detectados.
- Pagamentos pendentes.
- Alertas críticos.

## Endpoints
- `GET /api/operacao/resumo`
- `GET /api/operacao/pendencias`
- `GET /api/operacao/alertas`
- `GET /api/operacao/plantoes-criticos`

## Controle de acesso
- `ADMINISTRADOR_GLOBAL`: visão total.
- `ADMINISTRADOR`, `COORDENACAO`: visão por cliente.
- `OPERADOR`: visão limitada por permissão.
- `MEDICO`: sem acesso à central operacional.
