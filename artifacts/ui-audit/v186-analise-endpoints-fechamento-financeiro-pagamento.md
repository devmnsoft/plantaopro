# Análise de endpoints — fechamento, financeiro e pagamento — v1.86.0

Esta matriz separa ações respaldadas pelo modelo persistente das ações que permanecem indisponíveis. Ausência de agregado, identidade ou histórico suficiente é tratada como bloqueio, nunca como sucesso simulado.

| Módulo | Tela | Ação pendente | Endpoint necessário | Controller existente | Service existente | Repository/tabela existente | Regra de negócio | Prioridade | Implementado nesta PR | Motivo se pendente |
|---|---|---|---|---|---|---|---|---|---|---|
| Fechamentos | `OperacaoPremium/Fechamentos` | Aprovar | `POST /api/fechamentos/{id}/aprovar` | Não | Não | Não há agregado de fechamento comprovado | Existência, transição válida, valor e divergências reais | P0 | Não | Não há entidade, tabela ou histórico transacional que permita persistir a aprovação. |
| Fechamentos | `OperacaoPremium/Fechamentos` | Devolver com motivo | `POST /api/fechamentos/{id}/devolver` | Não | Não | Não há campo/tabela de motivo | Motivo obrigatório e preservação do histórico | P0 | Não | Não existe destino persistente para status e motivo; ação segue desabilitada. |
| Fechamentos | `OperacaoPremium/Fechamentos` | Gerar financeiro | `POST /api/fechamentos/{id}/gerar-financeiro` | Não | Não | Não há vínculo fechamento-financeiro | Aprovação e valor real; idempotência | P0 | Não | Não é possível comprovar origem, valor ou duplicidade. |
| Financeiro | `Financeiro/Index` | Aprovar item genérico | `POST /api/financeiro/{id}/aprovar` | Parcial, para pagamentos de escala | Parcial, `FinanceiroService` | `pagamentos` representa pagamentos de escala, não item financeiro genérico | Valor real e transição válida | P0 | Não | As contas clínicas e os pagamentos de escala não têm identidade financeira única. |
| Financeiro | `Financeiro/Details` | Contestar item genérico | `POST /api/financeiro/{id}/contestar` | Parcial | Parcial | Sem tabela de item financeiro genérico | Motivo obrigatório e valor preservado | P1 | Não | Evitada equivalência artificial com pagamento. |
| Financeiro | `Financeiro/Details` | Gerar pagamento por item | `POST /api/financeiro/{id}/gerar-pagamento` | Não | Geração existente somente por escala real | `pagamentos`, `escalas`, `plantoes` | Financeiro aprovado, valor real e idempotência | P0 | Não | O fluxo real recebe `escala_id`; não existe `financeiro_id`. |
| Pagamentos | `Financeiro/Details` | Marcar como pago | `POST /api/pagamentos/{id}/marcar-pago` | Sim, `PagamentosController` | Sim, `FinanceiroService.MarcarPagoAsync` | Dapper: `pagamentos`, `historico_pagamento` | Pendente, valor positivo, baixa atômica e data real | P0 | Sim | — |
| Pagamentos | `Financeiro/Details` | Contestar com motivo | `POST /api/pagamentos/{id}/contestar` | Sim, `PagamentosController` | Sim, `FinanceiroService.ContestarAsync` | Dapper: `pagamentos`, `historico_pagamento` | Motivo obrigatório, status pendente, valor preservado | P0 | Sim | — |
| Pagamentos | `Financeiro/Details` | Resolver contestação | `POST /api/pagamentos/{id}/resolver-contestacao` | Não | Não | Histórico registra contestação, mas não decisão/resolução tipada | Decisão, justificativa, responsável e histórico | P1 | Não | Faltam contrato e estado persistente de resolução; ação não é oferecida. |
| Glosas | Financeiro/Pagamentos | Criar ou resolver | Contrato ainda indefinido | Não | Não | Estruturas parciais sem origem financeira unificada | Origem, valor e motivo reais | P1 | Não | Um endpoint genérico inventaria semântica e vínculo. |
| Repasses | Financeiro/Pagamentos | Baixar repasse | Preservar contrato v1.15 | Não nesta jornada | Serviço legado específico | Estrutura específica de repasse | Médico, CRM, valor e status reais | P1 | Não | Não foi criada equivalência entre repasse e pagamento de escala. |
| Admin SaaS | `Configuracoes` | Salvar grupos genéricos | Endpoint tenant-scoped a definir | Não completo | Não completo | Persistência tenant-scoped incompleta | Tenant existente, autorização e validação | P1 | Não | Controles permanecem desabilitados até existir contrato persistente completo. |

## Decisão de segurança

Somente as duas mutações de pagamento usam registros reais e transação com auditoria. Os contratos canônicos sem suporte continuam ausentes; testes contratuais verificam essa ausência para impedir a introdução acidental de endpoint que apenas retorne sucesso.
