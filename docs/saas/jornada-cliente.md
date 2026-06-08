# Jornada do Cliente SaaS

O PlantãoPro expõe a jornada do cliente por API e Web para acompanhar lead, negociação, implantação, treinamento, operação assistida, ativação, expansão, risco e cancelamento.

## Funcionalidades implementadas

- API `GET /api/jornada-clientes` para listagem paginada/limitada.
- API `POST /api/jornada-clientes/{clienteId}/avancar` com motivo obrigatório.
- API `POST /api/jornada-clientes/{clienteId}/retroceder` com justificativa obrigatória.
- Eventos e tarefas por cliente para timeline e próximas ações.
- Funil por etapa em `GET /api/jornada-clientes/funil`.
- Auditoria em mudanças de etapa.
- Criação automática de ação de Customer Success quando a jornada entra em risco.

## Etapas oficiais

Lead cadastrado, demonstração agendada, demonstração realizada, proposta enviada, negociação, convertido, implantação, treinamento, operação assistida, ativo, expansão, risco e cancelado.
