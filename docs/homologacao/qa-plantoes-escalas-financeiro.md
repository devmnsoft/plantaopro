# QA Plantões, Escalas e Financeiro Médico

Status geral: **Funcional pendente QA**. Não declarar produção.

## Fluxo operacional

| Etapa | Regra de aceite | Status |
|---|---|---|
| Criar e publicar plantão | Vagas não negativas e tenant respeitado | Funcional pendente QA |
| Médico visualiza disponível | Médico só vê dados próprios/permitidos | Funcional pendente QA |
| Solicitar ou aceitar convite | Médico inativo bloqueado; recusa exige justificativa | Funcional pendente QA |
| Coordenação confirma escala | Não duplicar escala e conflito crítico bloqueia | Funcional pendente QA |
| Realizar plantão | Auditoria e notificação geradas | Funcional pendente QA |
| Gerar pagamento | Não duplicar pagamento | Funcional pendente QA |
| Médico contesta | Justificativa obrigatória | Parcial |
| Financeiro resolve e paga | Financeiro não acessa conteúdo clínico sensível | Funcional pendente QA |

## Evidências desta rodada

- Mobile médico consome endpoints reais para plantões, convites, escalas e pagamentos quando disponíveis.
- Roteiro de smoke inclui dashboards e Operação Inteligente.
- Validação real de concorrência/vagas depende de ambiente com PostgreSQL.

## Atualização homologação CRUDs, ações e jornadas — 2026-07-09

Classificação geral desta rodada: **Funcional pendente QA** com execução runtime **Bloqueado por ambiente** quando não houver SDK .NET, Docker e PostgreSQL.

- CRUDs e rotas principais mapeados para validação: Pacientes, Agendamentos, Painel de Chamada, Triagem, Consultas, CID, Prescrições, Financeiro Clínica, Convênios, Planos de Saúde, Plantões, Escalas, Financeiro Médico, Notificações, Relatórios, Ajuda e Primeiros Passos.
- Smoke Web/API ampliado para endpoints e telas principais; o critério bloqueia `404` e `500` e aceita `302` em rotas protegidas sem sessão.
- Testes contratuais adicionados para controllers, actions Create/Edit/Details, endpoints API, rotas de menu, padrões proibidos, segredos, mobile e docs.
- Pendências reais: executar QA ponta a ponta com massa PostgreSQL por perfil, validar auditoria de ações críticas, restrições LGPD/RBAC e transições de status em runtime.
- Não declarar produção.
