# Release — Usabilidade e jornada Saúde 360

Entrega focada em navegabilidade, jornada guiada, lookups para formulários, pendências clínicas, massa demo e documentação para homologação.

## Complemento desta rodada
- O Fluxo de Atendimento passa a explicitar também a etapa 9 de Relatórios/Pendências, fechando a jornada Paciente → Agendamento → Check-in → Painel → Triagem → Consulta → CID → Prescrição → Financeiro → Relatórios.
- O contrato de lookups do Saúde 360 agora contempla `Description` no `LookupItemDto` e inclui o endpoint `GET /api/lookups/status-financeiro` para selects de contas, recebimentos e caixa.
- As pendências clínicas retornam `DataHora`, permitindo ordenação visual por recência/prioridade sem expor conteúdo clínico sensível.
