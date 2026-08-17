# Análise de runtime final — v1.82.0

Status desta matriz: rotas confirmadas **estaticamente** nos contratos existentes; homologação HTTP permanece **BLOQUEADA** pela ausência do SDK/runtime e de sessão autenticada. Nenhum status HTTP foi inventado.

| Módulo | Rota | Controller/action esperado | Status esperado | Dado real necessário | Smoke | Risco de runtime | Correção v182 | Pendência |
|---|---|---|---|---|---|---|---|---|
| Público | `/`, `/Account/Login`, `/cadastro/empresa`, `/Planos` | Home/Account/Cadastro/Planos | 2xx ou redirect funcional | catálogo apenas em Planos | público | startup/configuração | runner v182 preparado | executar HTTP |
| Admin | `/AdminSaas/Index`, `/Configuracoes`, `/MinhaAssinatura` | AdminSaas.Index; Configuracoes.Index; MinhaAssinatura.Index | autenticado, sem 500/Razor | tenant, claims, assinatura reais | autenticado | policy/API | checks admin mantidos | storage-state e runtime |
| Central | `/Home/Dashboard`, `/MinhaCentral`, `/MeuDia`, `/Agenda` | actions Index/Dashboard reais | autenticado | perfil, tenant e agenda reais | autenticado | DI/API/redirect | checks shell/client errors adicionados | storage-state e runtime |
| Clínica | `/Agendamentos`, `/Saude360`, `/Pacientes`, `/Triagem`, `/Consultas`, `/FaturamentoClinico` | controllers/actions documentados em v178 | autenticado | paciente/agendamento/consulta reais | jornada clínica | payload/API/Razor | check clínico mantido | executar sem fabricar dados |
| Financeiro | `/Financeiro`, `/Pagamentos` | Financeiro.Index; Pagamentos.Index | autenticado | contas/pagamentos reais | jornada financeira | nulos/status/API | honestidade financeira mantida | executar com tenant real |
| Operação | `/Plantoes`, `/Escalas`, `/Fechamentos` | Plantoes.Index; Escalas.Index; Fechamentos.Index | autenticado | plantões/escalas reais | jornada operacional | transições/API | check operacional mantido | executar com dados reais |
| Relatórios | `/Relatorios` | Relatorios.Index | autenticado | fontes autorizadas | governança | policy/endpoints | ações sem backend continuam desabilitadas | executar com claims reais |
