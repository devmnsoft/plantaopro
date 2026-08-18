# Análise de ações desabilitadas P0/P1 — v1.84.0

| Módulo | Tela | Ação | Motivo atual | Rota/API necessária | Estrutura existente | Prioridade | Nesta PR | Pendência |
|---|---|---|---|---|---|---|---|---|
| Clínica | AgendaPremium | Check-in | Já havia API e persistência; UI condicionada ao status | `POST /api/agendamentos/{id}/checkin` | `agendamentos`, `agendamento_checkins`, `Saude360ClinicalService` | P0 | Sim, validado/conectado | Runtime com banco real |
| Clínica | Formulário de triagem | Finalizar | UI dizia não haver transição embora a API tipada existisse | `POST /api/triagens/{id}/finalizar-tipado` | `triagens`, histórico e auditoria clínica | P0 | Sim | Runtime com registro real |
| Clínica | Consulta | Enviar ao faturamento | Não há garantia de conta/valor real na finalização | serviço transacional consulta → faturamento | consulta e faturamento existem, vínculo incompleto | P0 | Não | Definir composição e valor |
| Operacional | Plantão | Publicar/cancelar | Endpoints existentes; habilitação depende de estado/dados reais | rotas de status de plantão | `plantoes`, serviço e auditoria | P0 | Não alterado | Teste integrado com PostgreSQL |
| Operacional | Escalas | presença/ausência/substituição | API existe; listagem mantém ações fechadas sem contexto/permissão suficiente | rotas em `/api/escalas` | `escalas`, state machine e auditoria | P0 | Não | DTO de capacidades por item |
| Fechamento | Fechamentos | aprovar/devolver/gerar financeiro | tela não recebe fechamento transacional confiável | endpoints e serviço de fechamento | estrutura parcial | P0/P1 | Não | Consolidar identidade/status/valor |
| Financeiro | Financeiro/Pagamentos | aprovar/contestar/gerar/pagar | detalhe e valores não estão garantidos na listagem | serviços transacionais financeiros | tabelas existentes, fluxo fragmentado | P0/P1 | Não | Consolidar regra de origem e idempotência |
| Admin SaaS | Configurações | salvar preferências | estrutura clara não comprovada para todos os grupos | endpoint tenant-scoped | infraestrutura parcial | P1 | Não | Contrato por tenant e autorização |
