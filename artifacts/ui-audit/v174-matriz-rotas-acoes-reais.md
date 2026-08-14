# Matriz de rotas e ações reais — v1.74.0

| Origem | Ação | Destino/backend | Estado |
|---|---|---|---|
| Consultas | Abrir faturamento | `/FaturamentoClinico` | Habilitada; rota MVC real |
| Consulta | Abrir financeiro | `/ClinicaFinanceiro/ContasReceber` | Habilitada; rota e API reais |
| Faturamento | Abrir origem | `/Consultas/Details/{AtendimentoId}` | Condicional ao ID real |
| Faturamento | Abrir financeiro clínico | `/ClinicaFinanceiro/ContasReceber` | Habilitada |
| Faturamento | Aprovar/glosar/exportar | — | Desabilitada; contrato por conta ausente |
| Financeiro | Detalhes do pagamento | `/Financeiro/Details/{id}` | Habilitada com ID real |
| Pagamentos | Revisar financeiro | `/Financeiro` | Habilitada |

Nenhuma ação utiliza `href="#"`, `alert()` ou `confirm()`.
