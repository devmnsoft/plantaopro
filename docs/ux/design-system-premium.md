# Design system premium

Padrões: cards com sombra suave, botões consistentes, EmptyState, PageHelp, AssistenteContextual, toast, modal para ação destrutiva, campos grandes e linguagem simples.

## Evolução operação inteligente e demo comercial premium — 2026-07-07
- Evoluído dashboard executivo por perfil: Admin Global, Administrador Cliente, Coordenação, Médico, Financeiro e Saúde 360.
- Criado cockpit Operação Inteligente com pendências por prioridade, perfil responsável, CTA seguro e recomendações determinísticas sem IA externa.
- Criada jornada Primeiros Passos por perfil para implantação do tenant e operação diária.
- Agenda clínica recebeu visão comercial por cards para Calendário, AgendaDia, AgendaMedico e CheckIn.
- Relatórios gerenciais priorizam filtros, cards, LGPD e exportação futura bloqueada até auditoria.
- Demo premium documentada com usuários por perfil e seed idempotente `database/seeds/2026_demo_comercial_premium.sql`.
- Mobile médico mantém telas mínimas, fallback amigável, uso de `EXPO_PUBLIC_API_BASE_URL` e sem log de token.
- Classificação: Evolução funcional parcial no ambiente atual quando SDK .NET ou Docker não estiverem disponíveis; Demo premium navegável para apresentação.

## Nota de status real — 2026-07-08

Esta documentação diferencia demonstração comercial de runtime real: dados demo só devem ser usados com `DemoMode=true`. Fluxos marcados como parciais exigem validação em ambiente com API, PostgreSQL, Docker/Expo e massa de homologação antes de qualquer declaração de produção.
