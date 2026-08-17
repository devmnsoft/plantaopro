# Matriz de rotas e ações reais — v1.77.0

| Jornada | Rotas auditadas pelo smoke | Regra da ação |
|---|---|---|
| Pública | `/`, `/Account/Login`, `/cadastro/empresa`, `/Planos` | Endpoint real ou estado vazio |
| Shell/perfil | `/AdminSaas/Index`, `/Home/Dashboard`, `/MinhaCentral`, `/MeuDia` | Autorização no servidor |
| Clínica | `/Agenda`, `/Agendamentos`, `/Saude360`, `/Pacientes`, `/Triagem`, `/Consultas` | ID, vínculo e status reais |
| Financeira | `/FaturamentoClinico`, `/Financeiro`, `/Pagamentos` | Origem, valor e status não presumidos |
| Operacional | `/Plantoes`, `/Escalas` | Transição real; motivo quando exigido |
| Gestão | `/Relatorios`, `/Configuracoes`, `/MinhaAssinatura` | Policy e backend real |

O catálogo tem 23 rotas same-origin. `actionsWithoutBackendDisabled` exige motivo acessível nas jornadas condicionadas.
