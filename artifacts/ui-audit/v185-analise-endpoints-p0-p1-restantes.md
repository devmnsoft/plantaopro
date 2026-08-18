# Análise de endpoints P0/P1 restantes — v1.85.0

| Módulo | Tela | Ação | Status atual | Backend/tabela existente | Regra necessária | Prioridade | Nesta PR | Motivo pendente |
|---|---|---|---|---|---|---|---|---|
| Clínica | AgendaPremium | Check-in | Real | `Saude360ClinicalService`; `agendamentos`, `agendamento_checkins` | status, paciente e idempotência | P0 | revisado/preservado | runtime PostgreSQL |
| Clínica | Consultas/Atendimento | Finalizar e gerar financeiro | Real/hardened | `ConsultaApplicationService`; `consultas`, `clinica_contas_receber` | prontuário mínimo, valor real, idempotência | P0 | sim | runtime PostgreSQL |
| Operação | Plantoes/Details | Publicar/cancelar | Real/hardened | `PlantaoService`; `plantoes`, histórico | dados mínimos, transição, motivo | P0 | sim | runtime PostgreSQL |
| Operação | Escalas/Details | Confirmar/presença/ausência | Real/hardened | `EscalaService`; `escalas`, histórico | status, vínculo, motivo de ausência | P0 | sim | timestamp de presença dedicado não existe |
| Operação | Escalas/Substituir | Substituir | Real existente | `EscalaService`; `escalas` | substituto, elegibilidade, motivo | P1 | preservado | runtime PostgreSQL |
| Fechamento | Fechamentos | Aprovar/devolver/gerar financeiro | Desabilitado | ViewModel vazio; sem agregado/repository transacional comprovado | identidade, divergência, valor e idempotência | P0 | não | falta estrutura real de fechamento |
| Financeiro | Financeiro/Pagamentos | gerar/confirmar/cancelar pagamento | Backend real existente; UI contextual | `FinanceiroService`; `pagamentos` | origem/valor/status | P1 | documentado | consolidar capacidades por item |
| Admin | Configurações | preferências tenant | Desabilitado quando ausente | infraestrutura parcial | autorização e contrato tenant-scoped | P1 | não | contrato incompleto |
