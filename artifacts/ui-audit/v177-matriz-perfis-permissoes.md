# Matriz de perfis e permissões — v1.77.0

| Perfil | Foco do dashboard | Rotas reais prioritárias | Limite |
|---|---|---|---|
| Admin | Governança SaaS, configuração e assinatura | `/AdminSaas/Index`, `/Configuracoes`, `/MinhaAssinatura` | Clínica somente por policy explícita |
| Coordenação | Cobertura, escalas, convites e fechamentos | `/Plantoes`, `/Escalas`, `/MinhaCentral` | Financeiro sensível por policy |
| Médico | Agenda, plantões e pagamentos próprios | `/Home/Dashboard`, `/Agenda`, `/Pagamentos` | Somente vínculos próprios |
| Hospital | Cobertura, equipes e solicitações da unidade | `/Home/Dashboard`, `/Plantoes`, `/Escalas` | Sem acesso cross-tenant |
| Financeiro | Faturamento, glosas, repasses e pagamentos | `/FaturamentoClinico`, `/Financeiro`, `/Pagamentos` | Prontuário em leitura mínima autorizada |
| Operador | Agenda, check-in e triagem | `/Agendamentos`, `/Triagem`, `/MinhaCentral` | Sem aprovação financeira/SaaS |

A visibilidade melhora orientação, mas controllers, `[Authorize]`, policy, tenant e escopo do recurso são a fonte de verdade.
