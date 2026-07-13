# Roteiro demo comercial premium

“O PlantãoPro organiza a operação médica de ponta a ponta: do plantão ao pagamento, do agendamento ao atendimento, do painel de chamada ao financeiro, com SaaS white label, auditoria e LGPD.”

## Narrativa
1. Abrir Dashboard Admin Global e mostrar clientes ativos, tenants, receita SaaS estimada, faturas e adoção por módulo.
2. Entrar como Administrador Cliente e mostrar plantões do mês, agenda do dia, contas a receber e uso do plano.
3. Abrir Operação Inteligente para priorizar plantões sem médico, escalas, convites, conflitos, triagem, contas vencidas e alertas de plano.
4. Demonstrar Primeiros Passos para usuário leigo por perfil.
5. Abrir AgendaDia e CheckIn com cards visuais por status.
6. Mostrar relatórios gerenciais com aviso LGPD e exportação bloqueada até auditoria.
7. Apresentar mobile médico: home, plantões disponíveis, convites, escalas, pagamentos, notificações, disponibilidade, preferências e perfil.

## Usuários demo
- admin.demo@plantaopro.local — Administração Cliente.
- coordenacao.demo@plantaopro.local — Coordenação.
- medico.demo@plantaopro.local — Médico.
- recepcao.demo@plantaopro.local — Recepção.
- triagem.demo@plantaopro.local — Triagem.
- financeiro.demo@plantaopro.local — Financeiro.

## Pendências reais
Build, banco e smoke dependem do SDK .NET e Docker disponíveis no ambiente de homologação. Exportação real de relatórios permanece bloqueada até trilha de auditoria específica.

## Nota de status real — 2026-07-08

Esta documentação diferencia demonstração comercial de runtime real: dados demo só devem ser usados com `DemoMode=true`. Fluxos marcados como parciais exigem validação em ambiente com API, PostgreSQL, Docker/Expo e massa de homologação antes de qualquer declaração de produção.


## Roteiro demonstrável pós-RC UX/QA

Classificação: **Demo explícito / Funcional pendente QA**. Demonstrar dashboards premium com badge de fonte, agenda clínica premium sem pacientes fictícios por padrão, Operação Inteligente, fluxo Saúde 360 documentado, fluxo Plantões/Escalas/Financeiro e mobile médico com `EXPO_PUBLIC_API_BASE_URL`. Não declarar produção.

## Atualização homologação CRUDs, ações e jornadas — 2026-07-09

Classificação geral desta rodada: **Funcional pendente QA** com execução runtime **Bloqueado por ambiente** quando não houver SDK .NET, Docker e PostgreSQL.

- CRUDs e rotas principais mapeados para validação: Pacientes, Agendamentos, Painel de Chamada, Triagem, Consultas, CID, Prescrições, Financeiro Clínica, Convênios, Planos de Saúde, Plantões, Escalas, Financeiro Médico, Notificações, Relatórios, Ajuda e Primeiros Passos.
- Smoke Web/API ampliado para endpoints e telas principais; o critério bloqueia `404` e `500` e aceita `302` em rotas protegidas sem sessão.
- Testes contratuais adicionados para controllers, actions Create/Edit/Details, endpoints API, rotas de menu, padrões proibidos, segredos, mobile e docs.
- Pendências reais: executar QA ponta a ponta com massa PostgreSQL por perfil, validar auditoria de ações críticas, restrições LGPD/RBAC e transições de status em runtime.
- Não declarar produção.

## Nota runtime real — 2026-07-09

Classificação para demo: **Demo explícito / Funcional pendente QA**. Demonstrar apenas fluxos mapeados e informar que a validação runtime real ficou bloqueada por ambiente nesta rodada. Não declarar produção.

## Complemento v1.14 — Consolidação premium

1. Entrar no dashboard v1.14 e explicar indicadores por perfil.
2. Abrir Operação Inteligente 3.0 e mostrar “O que fazer agora”.
3. Demonstrar Itens Faturáveis com procedimento, plantão, taxa e pacote.
4. Gerar conta de atendimento e título demonstrativo.
5. Reforçar que demo-boleto não é cobrança real.
6. Mostrar Jornada por Perfil para recepção, triagem, médico, financeiro e coordenação.
7. Abrir favoritos, últimos acessados, timelines e histórico de ações.
8. Mostrar mobile médico com base em `EXPO_PUBLIC_API_BASE_URL`.
9. Encerrar com matriz honesta: validado, pendente QA, parcial e dependente de provedor.
