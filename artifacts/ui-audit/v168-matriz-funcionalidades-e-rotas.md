# Matriz de funcionalidades e rotas — v1.68.0

| Jornada | Rotas auditadas | Fonte/ação real | Estado sem dados |
|---|---|---|---|
| Aquisição | `/`, `/Planos`, `/cadastro/empresa`, `/Account/Login` | navegação, autenticação e cadastro existentes | sem conteúdo inventado |
| SaaS | `/AdminSaas/Index`, `/Home/Dashboard`, `/Configuracoes`, `/Relatorios` | controllers e links Razor existentes | cards futuros sem CTA |
| Operação | `/MinhaCentral`, `/MeuDia`, `/Agenda`, `/Plantoes`, `/Escalas`, `/fechamentos` | drawers/BFFs e filtros protegidos | fila e timeline honestas |
| Saúde 360 | `/Saude360`, `/Pacientes`, `/Agendamentos`, `/Triagem`, `/Consultas` | APIs clínicas autenticadas e antiforgery | registro vazio orientado |
| Financeiro | `/Pagamentos`, `/Financeiro` | valores retornados e filtros de competência | saldo calculado apenas com itens reais |

## Fechamentos

A sequência **Plantão realizado → Divergências → Conferência → Aprovação → Financeiro → Pagamento** agora permanece visível mesmo sem registros. Detalhes, ações e timeline só aparecem quando `Pendentes`/`Timeline` contêm dados da fonte; a tela não fabrica métricas.
