# QA Saúde 360 ponta a ponta — versão homologável

Status geral: **Funcional pendente QA**. Esta versão não é declarada produção.

## Roteiro mínimo real

| Etapa | Resultado esperado | Status |
|---|---|---|
| Criar paciente | Registro criado no tenant com auditoria | Funcional pendente QA |
| Editar paciente | Alteração salva sem expor dado sensível em log | Funcional pendente QA |
| Criar agendamento | Agenda clínica consome `/api/agendamentos` | Funcional pendente QA |
| Confirmar agendamento | Ação crítica exige confirmação visual | Parcial |
| Check-in | Fluxo visível em agenda premium | Parcial |
| Chamar no painel | Atalho para Painel de Chamada/Fila | Parcial |
| Iniciar/finalizar triagem | Fluxo Triagem/Fila disponível | Funcional pendente QA |
| Iniciar/finalizar consulta | Fluxo Consultas/Create disponível | Funcional pendente QA |
| Vincular CID | Módulo CID mantido | Funcional pendente QA |
| Criar prescrição | Módulo Prescrições mantido | Funcional pendente QA |
| Gerar/receber conta | Financeiro Clínica mantido | Funcional pendente QA |
| Auditoria | Exigida em ações críticas | Funcional pendente QA |

## Evidências desta rodada

- Agenda clínica premium não mostra paciente demonstrativo por padrão.
- Dashboards Saúde 360 Web consomem endpoint real `/api/dashboards/saude360`.
- Pendências que dependem de banco/PostgreSQL ficam **Bloqueado por ambiente** quando SDK .NET, Docker ou PostgreSQL não estiverem disponíveis.

## Atualização homologação CRUDs, ações e jornadas — 2026-07-09

Classificação geral desta rodada: **Funcional pendente QA** com execução runtime **Bloqueado por ambiente** quando não houver SDK .NET, Docker e PostgreSQL.

- CRUDs e rotas principais mapeados para validação: Pacientes, Agendamentos, Painel de Chamada, Triagem, Consultas, CID, Prescrições, Financeiro Clínica, Convênios, Planos de Saúde, Plantões, Escalas, Financeiro Médico, Notificações, Relatórios, Ajuda e Primeiros Passos.
- Smoke Web/API ampliado para endpoints e telas principais; o critério bloqueia `404` e `500` e aceita `302` em rotas protegidas sem sessão.
- Testes contratuais adicionados para controllers, actions Create/Edit/Details, endpoints API, rotas de menu, padrões proibidos, segredos, mobile e docs.
- Pendências reais: executar QA ponta a ponta com massa PostgreSQL por perfil, validar auditoria de ações críticas, restrições LGPD/RBAC e transições de status em runtime.
- Não declarar produção.
