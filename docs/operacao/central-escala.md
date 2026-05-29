# Central de Escala

A Central de Escala consolida a operação diária em uma sala de controle para coordenação e operação.

## Endpoints
- `GET /api/central-escala/resumo`: KPIs e listas críticas.
- `GET /api/central-escala/plantoes`: plantões filtrados e paginados.
- `GET /api/central-escala/pendencias`: pendências priorizadas.
- `GET /api/central-escala/acoes-recomendadas`: ações sugeridas para cobertura, escala e financeiro.

## Web
- `/CentralEscala`: visão executiva responsiva com KPIs, plantões sem cobertura, escalas solicitadas e pagamentos pendentes.
- `/CentralEscala/Plantao/{id}`: leitura operacional do plantão com atalhos para detalhes, escalas e agenda.

## Regras
- Médico não deve acessar a central.
- Coordenação/administrador/operador acessam dados do cliente permitido.
- Ações críticas continuam sendo feitas nos módulos origem para preservar validação, auditoria e modais existentes.
