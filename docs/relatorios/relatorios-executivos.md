# Relatórios executivos e exportações

Endpoints implementados:
- `GET /api/relatorios/operacional`
- `GET /api/relatorios/financeiro`
- `GET /api/relatorios/saas`
- `GET /api/relatorios/exportar-csv`
- `POST /api/relatorios/filtros-salvar`
- `GET /api/relatorios/filtros-salvos`

Os relatórios aplicam período padrão de 30 dias, respeitam `cliente_id` quando presente e registram exportações em `relatorios_exportacoes`.
