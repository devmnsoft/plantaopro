# Mapa inicial de rotas API, BFF e Web

Este mapa registra somente rotas verificadas no código presente. As rotas Valora solicitadas não existem neste produto e não serão simuladas.

| Web | BFF/Controller | API | Consumidor | Status | Pendência |
|---|---|---|---|---|---|
| `/SystemHealth` e `/Configuracoes/Saude` | `ConfiguracoesController.Saude` | `GET api/health` | action server-side | Implementado | smoke com API |
| Login existente | controllers de autenticação Web | endpoints Auth existentes | formulário Web | Existente | smoke com banco/bootstrap |
| Dashboard existente | `DashboardController` | serviços/API dashboard | views existentes | Existente | smoke autenticado |
| Configurações | `ConfiguracoesController.Index` | `GET api/usuarios/me` | action server-side | Existente | smoke autenticado |
| Health DB | não exposto diretamente na tela | `GET api/health/db` | operação/API | Existente | PostgreSQL real |
| Health Auth | não exposto diretamente na tela | `GET api/health/auth` | operação/API | Existente | schema e admin bootstrap |
| Hubs | Web/JS existente conforme módulo | `/hubs/operacao`, `/hubs/fila`, `/hubs/notificacoes`, `/hubs/escalas` | SignalR | Mapeados | smoke runtime |

## Pendência de inventário
Gerar mapa completo por reflexão/Swagger somente depois de o SDK estar disponível e `ApiRouteStartupValidator` executar com sucesso. Não declarar ausência de 404 sem esse smoke.
