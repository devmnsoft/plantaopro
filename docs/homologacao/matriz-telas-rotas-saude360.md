# Matriz final de telas, menus e rotas — PlantãoPro Saúde 360

Status da homologação premium em 2026-06-16. Critério: nenhuma entrada comercial deve resultar em 404; módulos não concluídos devem cair em tela explicativa, dashboard ou checklist operacional.

| Grupo | Tela | Controller | Action | View | Endpoint API | Perfil | Plano/módulo | Menu | Controller | Action | View | Endpoint | Form | EmptyState | PageHelp | Toast | Modal | Status |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| INÍCIO | Visão Geral | Home | Dashboard/Index | Home/Dashboard | api/clinica-dashboard/resumo | Todos autenticados | Essencial | Sim | Sim | Sim | Sim | Sim | N/A | Sim | Sim | Sim | N/A | OK |
| INÍCIO | Fluxo de Atendimento | ClinicaDashboard | FluxoAtendimento | ClinicaDashboard/FluxoAtendimento | api/clinica-dashboard/resumo | Assistencial/Gestão | Essencial | Sim | Sim | Sim | Sim | Sim | N/A | Sim | Sim | Sim | N/A | OK |
| INÍCIO | Pendências do Dia | PendenciasClinicas | Index | Saude360/Modulo | api/pendencias-clinicas | Assistencial | Essencial | Sim | Sim | Sim | Sim | Sim | N/A | Sim | Sim | Sim | N/A | OK |
| ATENDIMENTO | Pacientes | Pacientes | Index/Create/Edit/Details | Saude360/Modulo/Formulario | api/pacientes | Recepção/Assistencial | Essencial | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | OK |
| ATENDIMENTO | Agendamentos | Agendamentos | Index/Create/CheckIn | Saude360/Modulo/Formulario | api/agendamentos | Recepção/Assistencial | Essencial | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | OK |
| ATENDIMENTO | Check-in | Agendamentos | CheckIn | Saude360/Formulario | api/agendamentos/check-in | Recepção | Essencial | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | OK |
| ATENDIMENTO | Painel de Chamada | PainelChamada | Index/Fila/Chamar | Saude360/Modulo/Formulario | api/painel-chamada | Recepção/Triagem | Essencial | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | OK |
| ATENDIMENTO | Triagem | Triagem | Index/Fila/Create | Saude360/Modulo/Formulario | api/triagens | Enfermagem/Triagem | Profissional | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | OK |
| ATENDIMENTO | Consultas | Consultas | Index/Atendimento/Create | Saude360/Modulo/Formulario | api/consultas | Médico | Profissional | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | OK |
| ATENDIMENTO | Prescrições | Prescricoes | Index/Create/Modelos | Saude360/Modulo/Formulario | api/prescricoes | Médico | Profissional | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | OK |
| ATENDIMENTO | CID | Cid | Index/Importar/Favoritos | Saude360/Modulo/Formulario | api/cid | Médico/Auditor | Profissional | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | N/A | OK |
| FINANCEIRO | Dashboard Financeiro | ClinicaFinanceiro | Index | Saude360/Modulo | api/clinica-financeiro/resumo | Financeiro/Admin | Profissional | Sim | Sim | Sim | Sim | Sim | N/A | Sim | Sim | Sim | N/A | OK |
| FINANCEIRO | Contas a Receber | ClinicaFinanceiro | ContasReceber | Saude360/Modulo | api/clinica-financeiro/contas-receber | Financeiro | Profissional | Sim | Sim | Sim | Sim | Sim | N/A | Sim | Sim | Sim | N/A | OK |
| FINANCEIRO | Recebimentos/Caixa/Repasses/Relatórios | ClinicaFinanceiro | Receber/Caixa/Repasses/Relatorios | Saude360/Modulo/Formulario | api/clinica-financeiro/* | Financeiro | Profissional | Sim | Sim | Sim | Sim | Sim | Parcial | Sim | Sim | Sim | Sim | Corrigido por tela genérica |
| CONVÊNIOS | Convênios/Autorizações/Glosas/Faturamento | Convenios | Index/Autorizacoes/Glosas/Faturamento | Saude360/Modulo | api/convenios* | Convênios/Faturamento | Enterprise | Sim | Sim | Sim | Sim | Sim | Parcial | Sim | Sim | Sim | N/A | OK |
| CONVÊNIOS | Planos de Saúde | PlanosSaude | Index/Create/Pacientes | Saude360/Modulo/Formulario | api/planos-saude | Convênios/Recepção | Enterprise | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | Sim | OK |
| PLANTÕES | Dashboard/Plantões/Escalas/Convites/Disponibilidade/Substituições/Pagamentos | Plantoes/Escalas/Operacao | Index correlatos | Views existentes | api operacional | Coordenação/Admin | Operacional | Sim | Sim | Sim | Sim | Sim | Parcial | Sim | Sim | Sim | Sim | OK |
| CONFIGURAÇÕES | Usuários/Perfis/Permissões/White Label/LGPD/Auditoria/Integrações/Ajuda/Manual | Usuarios/Configuracoes/Lgpd/Auditoria/Integracoes/Ajuda/Manual | Index correlatos | Views existentes | api gestão | Admin/Auditor | Plataforma | Sim | Sim | Sim | Sim | Sim | Parcial | Sim | Sim | Sim | Sim | OK |
| GESTÃO SAAS | Clientes/Tenants/Planos/Assinaturas/Billing/Marketplace/Parceiros | Clientes/Planos/Assinaturas/Billing/Marketplace | Index correlatos | Views existentes | api SaaS | Admin global | SaaS | Sim | Sim | Sim | Sim | Sim | Parcial | Sim | Sim | Sim | Sim | OK |

## Pendências reais
- Homologação executada neste ambiente sem SDK `dotnet`; build/testes ficaram bloqueados por dependência local ausente.
- Testes funcionais com banco PostgreSQL e navegador real devem ser executados no ambiente de homologação com seeds aplicados.
