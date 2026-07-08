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
