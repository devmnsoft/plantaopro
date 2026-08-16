# v1.75.0 — varredura de controllers e rotas

## Resultado

- `python3 scripts/check-controllers-uniqueness.py`: **PASS**; 130 nomes únicos.
- Varredura regex manual em `backend/PlantaoPro.Web/Controllers/**/*.cs`: **nenhuma classe Controller em arquivos distintos**.
- Não foram encontrados indícios estáticos de CS0263, CS0101 ou CS0111. A confirmação pelo compilador permanece bloqueada pela ausência do SDK.

## Controllers críticos

| Controller | Arquivo/contrato | Situação |
|---|---|---|
| FaturamentoClinico | controller dedicado, rota explícita `/FaturamentoClinico` e `/FaturamentoClinico/Index` | verificado |
| MinhaAssinatura | controller dedicado | verificado |
| Consultas | `Saude360WebControllers.cs`, convenção `/Consultas` | verificado |
| ClinicaFinanceiro | `Saude360WebControllers.cs`, actions `Index` e `ContasReceber` | verificado; links não estão quebrados |
| Financeiro | `FinanceiroController.cs`, action `Index` | verificado |
| Pagamentos | `OperationalPlaceholderControllers.cs`, action `Index` | verificado |
| Saude360 / Agendamentos | controllers na jornada Saúde 360 | verificado |
| Relatorios | `RelatoriosController.cs` | verificado |
| AdminSaas | `CommercialDemoWebController.cs` | verificado |

## Rotas críticas

`/Consultas`, `/FaturamentoClinico`, `/Financeiro`, `/Pagamentos` e `/ClinicaFinanceiro/ContasReceber` possuem controller/action reais. A renderização e autorização em runtime aguardam homologação Windows/Visual Studio.
