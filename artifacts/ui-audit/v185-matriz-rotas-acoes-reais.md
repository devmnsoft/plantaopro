# Matriz de rotas e ações reais — v1.85.0

| UI | Ação | API real | Persistência/estado |
|---|---|---|---|
| AgendaPremium | Check-in | `/api/agendamentos/{id}/checkin` | check-in e agenda |
| Consultas/Atendimento | Finalizar | `/api/consultas/{id}/finalizar` | consulta + jornada + financeiro opcional transacional |
| Plantoes/Details | Publicar/cancelar | `/api/plantoes/{id}/publicar`, `/cancelar` | plantão + histórico |
| Escalas/Details | Confirmar/presença/ausência/substituir | `/api/escalas/{id}/...` | escala + plantão + histórico |
| Fechamentos | ações críticas | nenhuma comprovada | desabilitadas |
| Pagamentos | gerar/confirmar/cancelar existentes | rotas de `FinanceiroController` | pagamento + auditoria; habilitação contextual pendente |
