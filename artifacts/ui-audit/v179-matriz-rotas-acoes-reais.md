# Matriz de rotas e ações reais — v1.79.0

| Rota | Ação real | Condição | Ação desabilitada com motivo |
|---|---|---|---|
| `/Plantoes` | listar, filtrar, criar, detalhar | API e autorização | nenhuma rota é simulada |
| `/Plantoes/Details/{id}` | editar/publicar/cancelar | status, mínimos e justificativa | fechamento/financeiro sem vínculo |
| `/Convites?plantaoId={id}` | listar convites do plantão | PlantaoId real | reenviar sem endpoint |
| `/Escalas` | listar/filtrar/detalhar | API e autorização | transições exigem detalhe/status |
| `/Escalas/Details/{id}` | confirmar, recusar, realizar, substituir | status e endpoint | pagamento/fechamento sem vínculo |
| `/Escalas/Substituir/{id}` | substituir | médico cadastrado + motivo | aprovação/histórico não expostos |
| `/Fechamentos` | consultar estado BFF | fonte operacional | todas as mutações sem backend |
| `/Financeiro` e `/Pagamentos` | consultar módulos reais | autorização/dados | origem operacional não presumida |
