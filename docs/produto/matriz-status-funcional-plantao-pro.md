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
