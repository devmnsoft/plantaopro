# Relatórios executivos e exportações

Endpoints implementados:
- `GET /api/relatorios/executivos/operacional`
- `GET /api/relatorios/executivos/financeiro`
- `GET /api/relatorios/executivos/saas`
- `GET /api/relatorios/executivos/exportar-csv`
- `GET /api/relatorios/valor/exportar-csv`
- `POST /api/relatorios/executivos/filtros-salvar`
- `GET /api/relatorios/executivos/filtros-salvos`

Os relatórios aplicam período padrão de 30 dias, respeitam `cliente_id` quando presente e registram exportações em `relatorios_exportacoes`.
