# Diagnóstico v1.60 — jornadas completas e UX operacional

## Método e limites

Auditoria estática realizada em 12/08/2026 sobre views, controllers, serviços, view models e JavaScript solicitados. O container não dispõe de `dotnet`; portanto, build, testes .NET e runtime autenticado não foram declarados como aprovados. Nenhum dado demonstrativo foi criado.

## Diagnóstico por módulo

| Módulo | Estado encontrado | Funcionalidade real e endpoints | Lacunas confirmadas | Entrega v1.60 |
|---|---|---|---|---|
| Saúde 360 | Agregado do dashboard já consome `api/clinica-dashboard/resumo`; a view compõe oito etapas e só mostra quantidades retornadas. | BFF `Saude360WebService.ListarAsync`, tenant/JWT e rotas reais de Pacientes, Agendamentos, Painel, Triagem, Consultas, Prescrições e Financeiro. | Tempo médio não existe no contrato e não deve ser exibido. | Jornada preservada e coberta pelo gate operacional v1.60. |
| Agendamentos | Agenda real, agrupamento por horário, status derivados da página e POST antiforgery para confirmar/check-in/cancelar/reagendar. | `api/agendamentos`, `/{id}/confirmar`, `/{id}/checkin`, `/{id}/cancelar`, `/{id}/reagendar`. | Endpoint transacional de chamada não está exposto no BFF. | Campos opcionais de atendimento, convênio e sala; drawer longitudinal; reagendamento real; chamada explicitamente desabilitada; triagem recebe `agendamentoId`. |
| Triagem | Fila e CRUD usam `api/triagens`; formulário era genérico e aceitava faixas implausíveis. | `api/triagens`, `/fila`, `/historico-paciente`. | Não há endpoint separado para rascunho/finalização/pendência clínica no Web BFF auditado. | Validação server-side de risco e sinais vitais, limites HTML e observação obrigatória em alto risco. |
| Consultas | Workspace clínico carrega uma consulta real e oferece rascunho, CID, prescrição e finalização com conflitos e modal. | `api/consultas`, rota `/Consultas/Atendimento/{consultaId}`, editor de prescrição. | Encaminhamento financeiro isolado não está publicado como ação Web. | Mantido sem criar CTA; dados clínicos continuam fora de toast/console. |
| Pacientes | CRUD, busca, histórico e resumo clínico são views compostas pelo BFF Saúde 360. | `api/pacientes`, histórico e resumo existentes. | Drawer dedicado com todos os agregados financeiros não existe no contrato atual. | Ações continuam limitadas às rotas existentes; nenhuma exposição adicional de documento. |
| Fechamentos | Não existe `Views/Fechamentos`; o fechamento aparece como parte das jornadas de escalas/financeiro. | Origens disponíveis em Plantões, Escalas, Financeiro e Pagamentos. | Workflow autônomo de conferência/aprovação e endpoints correlatos não foram encontrados. | Registrado como pendência, sem botão falso e sem nova entidade especulativa. |
| Financeiro/Pagamentos | Valores são calculados apenas dos DTOs retornados; detalhes e paginação existem. | `FinanceiroController`, `Pagamentos`, `ClinicaFinanceiro` e endpoints de resumo/contas/caixa/repasses. | Aprovar, pagar e contestar não possuem todos os endpoints Web verificáveis. | Ações indisponíveis permanecem desabilitadas; nenhum valor ou status inventado. |
| Convites | Cards reais por plantão com médico, envio, resposta e links para plantão/cobertura. | Controller e Central de Escala existentes. | Reenvio/cancelamento auditável não identificado no BFF Web. | Mantidas somente ações reais Abrir plantão e Convidar outro. |
| Notificações | Dropdown/central usa sua fonte autenticada e possui empty state. | Rotas `Notificacoes` e preferências. | Busca auditada não confirmou backend para todos os oito tipos pedidos. | Sem notificações sintéticas. |
| Command Palette | Pesquisa `/GlobalSearch`, sem resultados locais falsos; Ctrl/Cmd+K já abria. | Endpoint real `/GlobalSearch?q=...`. | Retorno de foco e Escape explícito não estavam garantidos pelo código. | Escape/cancel fecham e devolvem foco ao disparador. |
| Relatórios | Biblioteca aponta apenas para actions existentes e explicita recursos futuros sem CTA. | Cobertura, Convites, Produtividade, SLA, Faturamento SaaS e SaaS. | Histórico/geração assíncrona não existem para todo relatório. | Mantida a distinção implementado/indisponível. |
| Configurações | Central agrupa conta, permissões, assinatura, white label, notificações, LGPD, integrações e parâmetros. | Todos os cards são tag helpers para controllers reais. | “Última atualização” não consta dos view models. | Não foi inventada data de atualização. |
| Admin SaaS | `Index`/`Dashboard` usam container do shell e módulos reais. | Controllers/serviços SaaS existentes. | Runtime autenticado não executável sem .NET e credenciais. | Cobertura mantida nos gates e no smoke v1.60. |

## Drawers, responsividade e acessibilidade

`_DetailDrawer` mantém `role="dialog"`, `aria-modal`, estados loading/error, timeline, Escape, armadilha/retorno de foco e full-screen mobile. O drawer agora também recebe agendamentos. `_WorkItemDrawer` permanece ligado às ações reais de `work_items`. O smoke visual foi atualizado para `390x844`, `768x1024`, `1366x768` e `1920x1080`, com screenshots na pasta v160 quando houver runtime e estado autenticado.

## Fora do escopo por ausência de contrato

Fechamentos autônomos, chamada transacional, reenvio/cancelamento de convite e todas as mutações financeiras permanecem pendentes. Implementá-los sem regras, permissões e endpoints confirmados violaria a regra de não criar mock/CTA falso.
