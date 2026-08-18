# Endpoints implementados — v1.86.0

| Método | Rota | Controller | Service | Repository/tabela | Request DTO | Response DTO | Regra | HTTP | View | Gate |
|---|---|---|---|---|---|---|---|---|---|---|
| POST | `/api/pagamentos/{id}/marcar-pago` | `PagamentosController` | `FinanceiroService.MarcarPagoAsync` | Dapper; `pagamentos`, `historico_pagamento` | `MarcarPagamentoPagoRequest` | `ApiResponse<PagamentoActionResponse>` | existência, somente pendente, valor previsto > 0, bloqueio e baixa/data real | 200/400/401/403/404/409/422/500 | fluxo equivalente de baixa em `Financeiro/Details` | `V186FinancialOperationalContractTests` |
| POST | `/api/pagamentos/{id}/contestar` | `PagamentosController` | `FinanceiroService.ContestarAsync` | Dapper; `pagamentos`, `historico_pagamento` | `ContestarPagamentoRequest` | `ApiResponse<PagamentoActionResponse>` | motivo obrigatório, somente pendente, valor preservado, auditoria | 200/400/401/403/404/409/500 | `Financeiro/Details` via proxy Web | contrato v186 + gates |
