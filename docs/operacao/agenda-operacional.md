# Agenda Operacional Inteligente

## Objetivo
Centralizar a visualização operacional de plantões por período (dia/semana/mês), com filtros por hospital, médico, especialidade, status e cliente.

## Endpoints
- `GET /api/agenda/operacional`
- `GET /api/agenda/medico/{medicoId}`
- `GET /api/agenda/hospital/{hospitalId}`
- `GET /api/agenda/resumo`
- `GET /api/agenda/conflitos`

## Regras de negócio
- Médico visualiza apenas a própria agenda.
- Hospital visualiza apenas a própria unidade/hospital.
- Coordenação visualiza agenda do cliente atual.
- Financeiro usa visão de consulta (sem ações operacionais).
- Admin global pode filtrar todos os clientes.
- Eventos cancelados devem ter destaque visual.
- Eventos com conflito devem mostrar badge de conflito.
- A API deve limitar o intervalo máximo para evitar carga excessiva.

## Status operacionais
- RASCUNHO
- ABERTO
- EM_ESCALA
- PREENCHIDO
- CONFIRMADO
- REALIZADO
- CANCELADO

## UX esperada
- Calendário com visão mensal, semanal e diária.
- Lista mobile por dia com cards.
- EmptyState para ausência de eventos.
- Toast para erros e ações relevantes.
- Acesso rápido para plantão, escala e conflitos.

## Checklist de homologação
1. Aplicar filtros e validar escopo de cliente.
2. Validar perfis com regras de visualização.
3. Validar visual de cancelados e conflitos.
4. Validar desempenho em períodos extensos (limitação ativa).
