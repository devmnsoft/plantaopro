# Endpoints implementados/hardened — v1.84.0

| Método | Rota | Controller | Request DTO | Response DTO | Regra | HTTP | Tela | Validação |
|---|---|---|---|---|---|---|---|---|
| POST | `/api/triagens/{id}/finalizar-tipado` | `TriagensController` | `TriagemUpdateRequest` | `ApiResponse<Saude360RegistroDto>` | exige triagem persistida, paciente, risco e medidas plausíveis; salva antes de transicionar; alto risco exige observação na UI; audita | 200, 400, 401, 403, 404, 409 | `Saude360/Formulario` via `Triagem.Finalizar` | contrato v1.84 + gates estáticos; runtime bloqueado sem .NET/PostgreSQL |
| POST | `/api/agendamentos/{id}/checkin` | `AgendamentosController` | `Saude360ActionRequest` | `ApiResponse<Saude360RegistroDto>` | somente AGENDADO/CONFIRMADO; cria check-in sem duplicidade, altera status e audita | 200, 401, 403, 404, 409 | `Agendamentos/AgendaPremium` | implementação existente revisada e preservada; runtime bloqueado |

A mudança funcional desta PR é a conexão honesta da finalização tipada de triagem. O check-in já era real e permanece documentado como P0 efetivamente conectado.
