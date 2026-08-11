# Diagnóstico de telas funcionais — v1.56.0

## Escopo e método

Auditoria estática das views reais em `backend/PlantaoPro.Web/Views`, sem criação de mocks. Foram verificados shell v1.55, composição de página, contexto, indicadores derivados do model, filtros, tabela e alternativa mobile, ações com rota, detalhe/drawer, feedback vazio/erro e adequação médica. “Parcial” indica uma limitação real do contrato atual da view, e não uma simulação adicionada.

| Área / view | Shell / página | Contexto e dados reais | Filtros / leitura | Mobile / detalhe | Feedback | Resultado da auditoria e correção |
|---|---|---|---|---|---|---|
| Dashboard (`Home/Dashboard`) | Sim / `pp-page` | KPIs do `DashboardOverviewDto`, saudação autenticada e resumo diário | Filtro rotulado; agenda, riscos, financeiro e timeline | Tabela + cards; detalhe pela rota de plantão | Empty states contextuais | **Refatorada:** removidos plano, saudação e recomendações estáticas; painel executivo agora deriva todo conteúdo do model. |
| Minha Central | Sim / introdução equivalente | `work_items`, resumo, prioridades e atividade | Kanban e prioridade | Drawer existente e layout próprio | Erro com retry | Homologada; mantém central de ação real. |
| Meu Dia | Sim / hero próprio | Resumo, próxima ação e fila derivados do model | Ordenação por criticidade e prazo | Workspace responsivo e drawer compartilhado | Erro e dia sem pendências | Homologada; central pessoal já atende prioridade, timeline e atalhos. |
| Agenda | Sim / `pp-page` | Plantões reais, cobertura, valor e status | Modos dia/semana/mês e filtros | Cards mobile e drawer operacional | Empty state orientativo | Homologada; ações existentes preservadas. |
| Plantões | Sim / introdução equivalente | Totais e risco calculados da página | Filtros operacionais rotulados | Tabela desktop, cards mobile e detalhe | Empty state com próxima ação | Homologada; central já substitui CRUD simples. |
| Escalas | Sim / introdução equivalente | Médico, CRM, plantão, presença/status e ações permitidas | Leitura tabular contextual | Wrapper responsivo; detalhe existente | Empty state | Homologada; sem habilitar ações sem regra real. |
| Fechamentos | Sim / workspace operacional | Etapas, divergência, conferência e financeiro | Fluxo por estado | Cards operacionais | Vazio orientativo | Homologada nas views operacionais existentes (`OperacaoPremium/Fechamentos`). |
| Pendências | Sim / Minha Central | `work_items` reais | Kanban, prioridade e responsável | Cards e drawer | Erro/vazio | Homologada sem contadores inventados. |
| Saúde 360 | Sim / `clinical-workspace` | Jornada assistencial baseada no módulo recebido | Ações por etapa | Cards clínicos responsivos | Estado vazio de módulo | Homologada; rotas clínicas reais preservadas. |
| Pacientes | Sim / módulo Saúde 360 | Dados do `Saude360PageViewModel`; proteção LGPD existente | Busca e listagem do módulo | Composição responsiva | Empty state contextual | Homologada sem expor novos dados sensíveis. |
| Agendamentos | Sim / módulo Saúde 360 | Horário, paciente, profissional e status do model | Agenda e filtros existentes | Cards/wrapper conforme módulo | Empty state | Homologada; ações continuam condicionadas às rotas existentes. |
| Triagem | Sim / módulo clínico | Fila, sinais vitais e risco em views dedicadas | Leitura clínica e validação | Layout responsivo | Validação e conclusão | Homologada; não foram inferidos alertas clínicos sem regra de domínio. |
| Consultas | Sim / módulo clínico | Paciente, anamnese, conduta, CID e prescrição | Seções clínicas | Formulários responsivos | Resumo de validação | Homologada; dados sensíveis permanecem no contexto autorizado. |
| Financeiro | Sim / introdução equivalente | Valores calculados somente dos itens retornados | Consolidação por status | Tabela responsiva | Empty state orientativo | Homologada; valores deixam explícito quando representam a página. |
| Pagamentos | Sim / introdução equivalente | Médico, origem, previsto/pago e status | Leitura tabular | Wrapper responsivo e detalhe | Empty state | Homologada; ações bloqueadas explicam a regra. |
| Convites | Sim / introdução equivalente | Plantão, médico, status e histórico retornados | Filtro contextual | Wrapper responsivo | Erro/vazio | Homologada; sem CTA para endpoint inexistente. |
| Relatórios | Sim / `pp-page` | Biblioteca de relatórios disponíveis | Categorias e descrições | Grid responsivo | Recursos futuros sem CTA ativo | Homologada. |
| Configurações | Sim / `pp-page` | Conta autenticada e áreas administrativas | Organização por responsabilidade | Grid responsivo | Erro de conta e retry | Homologada; ações apontam para módulos reais. |
| Admin SaaS | Sim / `pp-page` | Cards fornecidos pelo model | Áreas e checklist | Grid responsivo | Sem métricas artificiais | Homologada. |
| Planos | Sim / `pp-page` | Catálogo, limites e preços cadastrados | Filtro de status | Grid responsivo | Empty state | Homologada; nenhuma oferta fictícia. |
| Onboarding | Sim / `pp-page` | Dados informados pelo operador | Wizard em cinco etapas | Formulário e resumo responsivos | Summary e erros associados | Homologada. |

## Riscos e decisões

- O dashboard só sinaliza risco quando um próximo plantão real possui vagas disponíveis; não foi criado score sintético.
- “Agenda de hoje” considera apenas os próximos plantões retornados pelo endpoint. A interface declara esse recorte para não sugerir total global.
- Ações destrutivas ou transições sem endpoint/regra confirmada permanecem bloqueadas ou ausentes.
- Não foi adicionado dado clínico, financeiro, comercial ou de plano para preencher estados vazios.
