# Catálogo de rotas da API

Este catálogo é governado automaticamente pelos `ApiDescription` da aplicação via `ApiRouteUniquenessIntegrationTests` e pelo `ApiRouteStartupValidator`. A lista abaixo registra as rotas impactadas pela v1.18.3.

| Método | Rota final | Controller | Action | Arquivo | Status |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/relatorios/executivos/exportar-csv` | `RelatoriosExecutivosFase4Controller` | `Exportar` | `backend/PlantaoPro.Api/Controllers/Fase4OperationalController.cs` | canônica |
| GET | `/api/relatorios/valor/exportar-csv` | `RelatoriosValorController` | `ExportarCsv` | `backend/PlantaoPro.Api/Controllers/ValorExecutivoController.cs` | canônica |
| GET | `/api/relatorios/catalogo` | `RelatoriosCentralController` | `Catalogo` | `backend/PlantaoPro.Api/Controllers/RelatoriosCentralController.cs` | canônica |
| GET | `/api/relatorios/{codigo}/preview` | `RelatoriosCentralController` | `Preview` | `backend/PlantaoPro.Api/Controllers/RelatoriosCentralController.cs` | canônica |
| GET | `/api/relatorios/{codigo}/exportar-csv` | `RelatoriosCentralController` | `ExportarCsv` | `backend/PlantaoPro.Api/Controllers/RelatoriosCentralController.cs` | canônica |
| GET | `/api/relatorios/exportacoes` | `RelatoriosCentralController` | `Exportacoes` | `backend/PlantaoPro.Api/Controllers/RelatoriosCentralController.cs` | canônica |
| GET | `/api/relatorios/exportacoes/{id:guid}` | `RelatoriosCentralController` | `Exportacao` | `backend/PlantaoPro.Api/Controllers/RelatoriosCentralController.cs` | canônica |
| GET | `/api/relatorios/fase6/exportacoes` | `Fase6BiIntegracoesController` | `Exportacoes` | `backend/PlantaoPro.Api/Controllers/Fase6BiIntegracoesController.cs` | legada |
