# Controllers e rotas — v1.74.0

## Resultado

- `python3 scripts/check-controllers-uniqueness.py`: **PASS**, 130 nomes únicos.
- Scanner manual solicitado: **PASS**, nenhuma classe controller presente em mais de um arquivo.

## Controllers críticos

| Controller | Arquivo canônico | Rota relevante | Situação |
|---|---|---|---|
| MinhaAssinaturaController | `Controllers/MinhaAssinaturaController.cs` | `/MinhaAssinatura` | Único |
| FaturamentoClinicoController | `Controllers/FaturamentoClinicoController.cs` | `/FaturamentoClinico` | Único |
| FinanceiroController | `Controllers/FinanceiroController.cs` | `/Financeiro` | Único |
| PagamentosController | `Controllers/OperationalPlaceholderControllers.cs` | `/Pagamentos` | Único |
| ConsultasController | `Controllers/Saude360WebControllers.cs` | `/Consultas` | Único |
| Saude360Controller | `Controllers/Saude360WebControllers.cs` | `/Saude360` | Único |
| AgendamentosController | `Controllers/Saude360WebControllers.cs` | `/Agendamentos` | Único |

A validação de resolução HTTP depende do runtime .NET, indisponível neste ambiente.
