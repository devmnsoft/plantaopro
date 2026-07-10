# Matriz de status funcional PlantãoPro Saúde 360

| Área | Módulo | Funcionalidade | Controller API | Endpoint API | Controller Web | Action Web | View | Form específico existe? | Ainda usa form genérico? | Ainda usa módulo genérico? | Tem campo ID manual? | Tem lookup? | POST salva? | Edit carrega dados? | Delete/Inativar usa modal? | Tem EmptyState? | Tem toast? | Tem PageHelp? | Tem AssistenteContextual? | Tem permissão? | Tem migration? | Tem seed? | Status | Ação necessária |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Atendimento | Pacientes | Cadastro e busca | Saude360Clinical | /api/pacientes | Pacientes | Index/Create/Edit | Views/Pacientes | Sim | Não nas telas principais | Não | Não | Sim | Sim | Parcial | Parcial | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Parcial | QA manual e edição completa |
| Atendimento | Agendamentos | Agenda e check-in | Saude360Clinical | /api/agendamentos | Agendamentos | Index/Create/Edit/CheckIn | Views/Agendamentos | Sim | Não nas telas principais | Não | Não | Sim | Sim | Parcial | Parcial | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Parcial | Validar fluxo ponta a ponta |
| Atendimento | Triagem | Classificação de risco | Saude360Clinical | /api/triagens | Triagem | Index/Create/Edit | Views/Triagem | Sim | Não nas telas principais | Não | Não | Sim | Sim | Parcial | Parcial | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Parcial | QA com paciente agendado |
| Atendimento | Consultas | Atendimento médico | Saude360Clinical | /api/consultas | Consultas | Atendimento/Create/Edit | Views/Consultas | Sim | Não nas telas principais | Não | Não | Sim | Parcial | Parcial | N/A | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Parcial | Validar prescrição e CID |
| Atendimento | CID | Busca CID | Lookups/Saude360 | /api/lookups/cid | Cid | Index/Create/Edit | Views/Cid | Sim | Não | Não | Não | Sim | Sim | Parcial | N/A | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Completo | Importação real homologar |
| Atendimento | Prescrições | Cadastro | Saude360Clinical | /api/prescricoes | Prescricoes | Index/Create/Edit | Views/Prescricoes | Sim | Não nas principais | Não | Não | Sim | Sim | Parcial | N/A | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Parcial | QA impressão auditada |
| Financeiro | Clínica | Receber/Caixa | Saude360Clinical | /api/clinica-financeiro | ClinicaFinanceiro | Index/Receber/Caixa | Views/ClinicaFinanceiro | Sim | Parcial | Não | Não | Sim | Sim | Parcial | Parcial | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Parcial | Validar baixa financeira |
| Convênios | Convênios | Cadastro/autorização | Saude360Clinical | /api/convenios | Convenios | Index/Create/Edit | Views/Convenios | Sim | Parcial | Não | Não | Sim | Sim | Parcial | Parcial | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Parcial | QA faturamento/glosa |
| Convênios | Planos de Saúde | Planos/pacientes | Saude360Clinical | /api/planos-saude | PlanosSaude | Index/Create/Edit | Views/PlanosSaude | Sim | Parcial | Não | Não | Sim | Sim | Parcial | Parcial | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Parcial | Homologar vínculo paciente |
| Plantões | Plantões/Escalas | Operação | Plantoes/Escalas | /api/plantoes /api/escalas | Plantoes/Escalas | Index/Create/Edit | Views/Plantoes/Escalas | Sim | Não | Não | Não | Sim | Sim | Parcial | Parcial | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Parcial | QA conflitos |
| Gestão | Usuários/Perfis | Acesso | Usuarios/Permissoes | /api/usuarios | Usuarios/Perfis | Index | Views/Usuarios/Perfis | Sim | Não | Não | Não | N/A | Sim | Parcial | Parcial | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Parcial | Revisar RBAC por perfil |
| Ajuda | Manual | Jornada leiga | Ajuda/Manual | /api/ajuda | Ajuda/Manual | Index/Perfil | Views/Ajuda/Manual | N/A | Não | Não | Não | N/A | N/A | N/A | N/A | Sim | Sim | Sim | Sim | Sim | N/A | N/A | Completo | Manter por release |


## Consolidação RC técnica — julho/2026

| Área | Status | Critério honesto |
| --- | --- | --- |
| Build backend | Bloqueado | SDK .NET ausente no container; CI criado para validar em runner com .NET 10. |
| Testes backend | Bloqueado | Dependem do build em ambiente com SDK. |
| CI GitHub Actions | Pronto | Workflow `dotnet-ci.yml` criado para push e pull_request. |
| Segurança de configuração | Pronto | Segredos substituídos por placeholders e banco padronizado como `plantaopro`. |
| Banco/migrations/seeds | Parcial | Ordem documentada; execução PostgreSQL real pendente neste ambiente. |
| Menus/RBAC | Parcial | Varredura estática Web/API sem padrões proibidos; navegação real pendente. |
| Saúde 360 | Parcial | Fluxos documentados para QA; validação ponta a ponta depende de API/Web e banco. |
| Plantões/escalas/financeiro médico | Parcial | Fluxo documentado; validar duplicidades, conflitos e auditoria em runtime. |
| Mobile médico MVP | Parcial | Navegação mínima implementada; start Expo depende de npm/ambiente liberado. |

## Status de homologação 2026-07-07

| Área | Status | Observação |
| --- | --- | --- |
| CI/build/test | Parcial | CI configurado; execução local bloqueada por ausência de `dotnet`. |
| Banco PostgreSQL | Parcial | Compose e scripts criados; aplicação real depende de Docker/psql. |
| Web/API | Parcial | Health real preparado; QA ponta a ponta pendente. |
| Mobile MVP | Parcial | Navegação MVP existe; Expo interativo pendente. |
| Demo comercial | Implementado sem validação real | Roteiro e checklist criados para execução guiada. |

## Status 2026-07-07

Classificação geral: **Bloqueado por ambiente**.

Motivo: o executor não contém `dotnet`, `docker` nem `psql`, impedindo build/test, PostgreSQL, migrations/seeds, API, Web, smoke API/Web e QA funcional real. O mobile executou `npm install`, mas o Metro falhou com `TypeError: fetch failed` em ambiente não interativo/rede.

## Evolução operação inteligente e demo comercial premium — 2026-07-07
- Evoluído dashboard executivo por perfil: Admin Global, Administrador Cliente, Coordenação, Médico, Financeiro e Saúde 360.
- Criado cockpit Operação Inteligente com pendências por prioridade, perfil responsável, CTA seguro e recomendações determinísticas sem IA externa.
- Criada jornada Primeiros Passos por perfil para implantação do tenant e operação diária.
- Agenda clínica recebeu visão comercial por cards para Calendário, AgendaDia, AgendaMedico e CheckIn.
- Relatórios gerenciais priorizam filtros, cards, LGPD e exportação futura bloqueada até auditoria.
- Demo premium documentada com usuários por perfil e seed idempotente `database/seeds/2026_demo_comercial_premium.sql`.
- Mobile médico mantém telas mínimas, fallback amigável, uso de `EXPO_PUBLIC_API_BASE_URL` e sem log de token.
- Classificação: Evolução funcional parcial no ambiente atual quando SDK .NET ou Docker não estiverem disponíveis; Demo premium navegável para apresentação.

## Atualização runtime real — 2026-07-08

| Área | Status honesto | Observação |
| --- | --- | --- |
| Agendamentos Web | Funcional/pendente QA | Controller duplicado removido; agenda segue no controller Saúde 360 real. |
| Operação Inteligente | Funcional/pendente QA banco | Web consome API real; service consulta PostgreSQL com fallback de pendência ambiental por tabela ausente. |
| Dashboards premium API | Parcial funcional | Endpoints por perfil criados com KPIs reais agregados; Web rica ainda parcial. |
| Agenda visual premium | Parcial | Sem dados demo no controller duplicado removido; requer refinamento visual completo em cima do fluxo Saúde 360. |
| Mobile médico | Parcial | Contratos existentes mantidos; evolução visual completa depende de validação Expo/API. |
| Relatórios | Parcial | Exportação sensível segue bloqueada até trilha de auditoria específica. |


## Classificação final pós-RC UX/QA

| Área | Classificação | Observação |
|---|---|---|
| Dashboards Web premium | Funcional pendente QA | Consumo real de `/api/dashboards/*`, sem números falsos por padrão. |
| Agenda clínica premium | Funcional pendente QA | Cards/filtros/status/ações, sem paciente demo por padrão. |
| Saúde 360 E2E | Funcional pendente QA | Roteiro documentado; validação real depende de SDK/Docker/PostgreSQL. |
| Plantões/Escalas/Financeiro | Funcional pendente QA | Regras críticas roteirizadas para QA. |
| Mobile médico | Parcial | Endpoints reais preparados; Expo interativo pendente. |
| RBAC/menus | Funcional pendente QA | Teste contratual de padrões e rotas visíveis reforçado. |
| Relatórios/exportação | Parcial | Exportação sensível deve permanecer bloqueada sem auditoria. |
| PR #222 | Bloqueado por ambiente | Sem remoto/metadata local para fechar via CLI; documentada como superada. |

## Atualização homologação CRUDs, ações e jornadas — 2026-07-09

Classificação geral desta rodada: **Funcional pendente QA** com execução runtime **Bloqueado por ambiente** quando não houver SDK .NET, Docker e PostgreSQL.

- CRUDs e rotas principais mapeados para validação: Pacientes, Agendamentos, Painel de Chamada, Triagem, Consultas, CID, Prescrições, Financeiro Clínica, Convênios, Planos de Saúde, Plantões, Escalas, Financeiro Médico, Notificações, Relatórios, Ajuda e Primeiros Passos.
- Smoke Web/API ampliado para endpoints e telas principais; o critério bloqueia `404` e `500` e aceita `302` em rotas protegidas sem sessão.
- Testes contratuais adicionados para controllers, actions Create/Edit/Details, endpoints API, rotas de menu, padrões proibidos, segredos, mobile e docs.
- Pendências reais: executar QA ponta a ponta com massa PostgreSQL por perfil, validar auditoria de ações críticas, restrições LGPD/RBAC e transições de status em runtime.
- Não declarar produção.

## Atualização runtime real — 2026-07-09

| Área | Classificação | Observação |
| --- | --- | --- |
| API/Web/Smoke real | Bloqueado por ambiente | Executor sem SDK .NET, Docker e PostgreSQL/psql. |
| CRUDs Saúde 360 | Funcional pendente QA | Rotas e contratos preparados; validação runtime pendente. |
| Plantões/Escalas/Financeiro | Funcional pendente QA | Smoke preparado; execução real pendente. |
| Mobile médico | Funcional pendente QA | Uso de `EXPO_PUBLIC_API_BASE_URL`; runtime Expo pendente. |
| Provedores externos | Dependente de provedor | Integrações reais exigem credenciais/ambiente próprios. |
