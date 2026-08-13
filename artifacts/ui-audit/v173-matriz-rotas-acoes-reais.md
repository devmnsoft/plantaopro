# Matriz de rotas e ações reais — v1.73.0

| Origem | Destino/ação | Situação |
|---|---|---|
| `/FaturamentoClinico` | API `api/v115/faturamento/contas-receber` | Real, autenticada e isolada por tenant no backend |
| `/FaturamentoClinico` | `/Consultas` | Link MVC real para revisar a origem |
| `/FaturamentoClinico` | `/Financeiro` | Link MVC real para conferência financeira |
| `/FaturamentoClinico` | `/Pagamentos` | Link MVC real para acompanhamento |
| `/FaturamentoClinico/Regras` | API de regras v1.15 | Superfície existente preservada |
| `/FaturamentoClinico/Glosas` | API de glosas v1.15 | Superfície existente preservada |
| `/MinhaAssinatura` | `api/minha-assinatura` | Contrato v1.72 preservado |

Aprovação, cobrança, pagamento e exportação não ganharam links simulados. Permanecem fora da tela enquanto não houver fluxo completo e autorizado para o registro selecionado.
