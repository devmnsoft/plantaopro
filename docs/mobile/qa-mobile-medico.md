# QA mobile médico

Status geral: **Parcial / Funcional pendente QA**.

## Checklist obrigatório

- Usar `EXPO_PUBLIC_API_BASE_URL`; não usar URL fixa de produção.
- Não registrar token em log.
- Não usar `Alert.alert`; confirmações devem usar componentes próprios.
- Exibir loading, empty state e error state.
- Botões grandes e navegação mobile-first.

## Fluxos

| Fluxo | Endpoint esperado | Status |
|---|---|---|
| Plantões disponíveis: listar/detalhe/solicitar | `mobile/plantoes-disponiveis`, `mobile/plantoes/{id}`, `mobile/plantoes/{id}/solicitar` | Funcional pendente QA |
| Convites: listar/detalhe/aceitar/recusar | `mobile/convites` | Funcional pendente QA |
| Escalas confirmadas/realizadas | `mobile/minhas-escalas` | Funcional pendente QA |
| Pagamentos e contestação | `mobile/meus-pagamentos` | Parcial |
| Notificações e marcação de leitura | `mobile/notificacoes` | Funcional pendente QA |
| Disponibilidade e preferências | `mobile/disponibilidade`, `mobile/preferencias` | Parcial |

Validação Expo/Metro real permanece **Bloqueado por ambiente** quando dependências Node/Expo não estiverem instaladas.

## Atualização homologação CRUDs, ações e jornadas — 2026-07-09

Classificação geral desta rodada: **Funcional pendente QA** com execução runtime **Bloqueado por ambiente** quando não houver SDK .NET, Docker e PostgreSQL.

- CRUDs e rotas principais mapeados para validação: Pacientes, Agendamentos, Painel de Chamada, Triagem, Consultas, CID, Prescrições, Financeiro Clínica, Convênios, Planos de Saúde, Plantões, Escalas, Financeiro Médico, Notificações, Relatórios, Ajuda e Primeiros Passos.
- Smoke Web/API ampliado para endpoints e telas principais; o critério bloqueia `404` e `500` e aceita `302` em rotas protegidas sem sessão.
- Testes contratuais adicionados para controllers, actions Create/Edit/Details, endpoints API, rotas de menu, padrões proibidos, segredos, mobile e docs.
- Pendências reais: executar QA ponta a ponta com massa PostgreSQL por perfil, validar auditoria de ações críticas, restrições LGPD/RBAC e transições de status em runtime.
- Não declarar produção.
