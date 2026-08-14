# Matriz de empty states — v1.74.0

| Tela | Fonte | Estado sem dados |
|---|---|---|
| Faturamento Clínico | API v1.15 de contas a receber | Informa que a API não retornou contas; não cria valor/histórico |
| Financeiro | API de pagamentos | Informa ausência para os filtros; indicadores derivam apenas da página retornada |
| Pagamentos | API de pagamentos | Informa ausência da API; não presume liquidação, valor ou histórico |
| Consultas | API de consultas via Saúde 360 | Componente do módulo renderiza somente retorno real |
| Dashboard | API de overview | Mantém o estado de indisponibilidade/coleções vazias do backend, sem novos KPIs sintéticos |
| Minha Central / Notificações | BFFs existentes | Itens e contadores continuam condicionados à resposta real; não foram adicionados mocks |
