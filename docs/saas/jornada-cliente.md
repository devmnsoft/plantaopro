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

## Complemento 2026-06-08 — tarefas automáticas e alertas da jornada

A evolução **PlantãoPro SaaS Inteligente** passou a tratar mudanças de etapa como evento operacional crítico: ao mover um cliente para implantação, operação assistida, risco ou cancelamento, o serviço registra evento de jornada, auditoria central e alerta SaaS do cliente. A etapa de implantação cria automaticamente a tarefa **Abrir checklist de operação assistida**, a etapa de operação assistida cria a tarefa **Validar primeiros plantões em operação assistida**, e a etapa de risco cria uma ação pendente de Customer Success.

Essas regras reforçam o roteiro lead → cliente → assinatura → implantação → operação assistida → ativo e evitam que mudanças importantes fiquem sem responsável ou próxima ação.
