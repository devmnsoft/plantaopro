# Auditoria v1.18.3 — conflitos de rotas de relatórios

## Causa raiz

O Swagger falhava porque dois controllers publicavam a mesma combinação método + caminho: `GET /api/relatorios/exportar-csv`.

| Rota anterior | Método | Controller | Action | Serviço | Parâmetros | CSV | Decisão |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `/api/relatorios/exportar-csv` | GET | `RelatoriosExecutivosFase4Controller` | `Exportar` | `OperationalAutomationService.ExportarCsvAsync` | `tipo`, `inicio`, `fim` | Totais operacionais, escalas, convites, substituições, pagamentos e valor total | Movida para `/api/relatorios/executivos/exportar-csv` e delegada ao `IReportExportService` com código `EXECUTIVO_GERAL`. |
| `/api/relatorios/exportar-csv` | GET | `RelatoriosValorController` | `ExportarCsv` | geração inline anterior; agora `IReportExportService` | sem parâmetros; agora aceita `inicio`, `fim` | Indicador/valor para visão de valor e financeiro | Movida para `/api/relatorios/valor/exportar-csv` e delegada ao `IReportExportService` com código `FINANCEIRO_CLINICA`. |

## Consumidores localizados

`rg` localizou apenas documentação produtiva para a URL antiga e referências de implementação/teste para `exportar-csv`. Não foi localizado consumidor Web, JavaScript, mobile ou script de smoke chamando `/api/relatorios/exportar-csv` como `GET` legado. A rota ambígua foi removida.

## Matriz canônica

| Relatório | Rota canônica | Status |
| --- | --- | --- |
| Indicadores executivos | `GET /api/relatorios/executivos/exportar-csv` | Canônica específica e transitória |
| Relatório de valor | `GET /api/relatorios/valor/exportar-csv` | Canônica específica e transitória |
| Catálogo | `GET /api/relatorios/catalogo` | Canônica central |
| Prévia | `GET /api/relatorios/{codigo}/preview` | Canônica central |
| Exportação CSV por código | `GET /api/relatorios/{codigo}/exportar-csv` | Canônica central |
| Histórico | `GET /api/relatorios/exportacoes` | Canônica central |

## Governança

Foram adicionados teste de integração baseado em `IApiDescriptionGroupCollectionProvider` e validador de startup para Development, Testing e CI. Duplicidades passam a falhar com mensagem explícita.
