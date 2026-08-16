# Jornada clínica — v1.76.0

| Etapa | Destino real | Regra honesta |
|---|---|---|
| Paciente → agendamento | `/Pacientes`, `/Agendamentos` | Não criar paciente ou agenda |
| Check-in → triagem → consulta | módulos existentes | Só abrir ação com vínculo retornado |
| Consulta → faturamento | `/FaturamentoClinico` | Ausência de origem/valor permanece ausente |
| Financeiro → pagamento | `/Financeiro`, `/Pagamentos` | Não inferir status ou pagamento |

O smoke v176 inclui `clinicalJourneyClear`; homologação navegada permanece bloqueada sem runtime.
