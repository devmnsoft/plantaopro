# Análise de endpoints financeiro, operacional e admin — v1.86.0

| Módulo | Tela | Ação | Endpoint necessário | Backend/tabela/service existente | Regra | Prioridade | Nesta PR | Motivo pendente |
|---|---|---|---|---|---|---|---|---|
| Pagamentos | Financeiro/Details | marcar pago | `POST /api/pagamentos/{id}/marcar-pago` | `FinanceiroService`; `pagamentos`; `historico_pagamento` | pendente, valor real positivo, baixa única e data UTC real | P1 | Sim | — |
| Pagamentos | Financeiro/Details | contestar | `POST /api/pagamentos/{id}/contestar` | mesmos repository/tabelas | motivo obrigatório, preservar valor, histórico | P1 | Sim | — |
| Fechamentos | OperacaoPremium/Fechamentos | aprovar/devolver/gerar financeiro | rotas propostas | nenhum agregado/repository persistente identificado | estados, divergência, valor e idempotência | P1 | Não | tela recebe modelo sem identidade financeira persistida |
| Financeiro genérico | Financeiro | aprovar/contestar/gerar pagamento | rotas propostas | pagamentos por escala e contas clínicas têm identidades distintas | origem e status financeiro unificados | P1 | Não | alias criaria semântica falsa |
| Glosas | ClinicaFinanceiro/Glosas | criar/resolver | contrato específico | estruturas v116 parciais | origem e valor real | P2 | Não | não há fluxo de resolução comprovado nesta UI |
| Repasses | ClinicaFinanceiro/Repasses | pagar | contrato específico | serviço v115 e pagamentos existem, mas fluxos são distintos | médico/CRM/valor reais | P2 | Não | não mascarar repasse como pagamento genérico |
| Admin | Configuracoes | salvar preferências | contrato tenant-scoped | leitura/UI parcial | tenant e permissão reais | P2 | Não | persistência completa não comprovada |
