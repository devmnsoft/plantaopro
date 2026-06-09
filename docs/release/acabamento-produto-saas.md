# PlantãoPro — acabamento SaaS profissional

## O que foi implementado nesta rodada

- Núcleo de permissões consolidado para Web e API com `ICurrentUserService`, `IPermissionService`, `ITenantAccessService` e `IModuleAccessService`.
- Administrador global com bypass explícito de permissões funcionais e acesso a Admin SaaS, billing, white label, auditoria, observabilidade e portais.
- Perfis base padronizados para admin cliente, coordenação, financeiro, médico, hospital, parceiro, suporte, auditoria, comercial e customer success.
- Matriz visual de permissões com teste de acesso e motivos comerciais de bloqueio.
- API de permissões com matriz, consulta por perfil, consulta por usuário, teste de acesso, salvar, restaurar padrão e copiar perfil.
- Layout SaaS autenticado com sidebar por perfil, topbar, tenant, plano, busca, notificações, ajuda, upgrade e rodapé comercial.
- Design system Web com CSS de layout, tema, componentes, dashboards, formulários e white label.
- Partials Razor para cabeçalho, breadcrumb, KPIs, badges, empty state, action/filter bars, modal, toast, contexto de tenant, uso do plano, bloqueios e CTA de upgrade.
- Redirecionamento pós-login por perfil, incluindo hospital para área hospitalar e médico para área médica.
- Bloqueios de plano e módulo com linguagem comercial, CTA de upgrade, suporte e assinatura.
- Documentação de QA com pendências reais de validação manual em ambiente com .NET instalado.

## MAPA REAL DO PRODUTO

| Área | Tela | Controller | Action | View | Perfil esperado | Menu existe? | Action existe? | View existe? | Permissão aplicada? | Plano/módulo aplicado? | Status | Correção aplicada | Pendência real |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Admin SaaS | Visão geral | AdminSaasController | Index | AdminSaas/Dashboard | ADMINISTRADOR_GLOBAL/SUPORTE/AUDITOR | Sim | Sim | Sim | Sim | Parcial | OK | Controller restringido por role | Persistir ações SaaS reais além da demo |
| Cliente | Portal | ClientePortalController | Index | ClientePortal/Dashboard | ADMINISTRADOR_CLIENTE/ADMINISTRADOR/DIRETOR | Sim | Sim | Sim | Sim | Parcial | OK | Controller restringido por role | Conectar indicadores em banco |
| Parceiro | Portal | ParceiroPortalController | Index | ParceiroPortal/Dashboard | PARCEIRO/ADMINISTRADOR_GLOBAL | Sim | Sim | Sim | Sim | Parcial | OK | Controller restringido por role | Integrar com comissões reais |
| Permissões | Matriz | PermissoesController | Matriz | Permissoes/Matriz | Admin global, admin cliente, suporte, auditor | Sim | Sim | Sim | Sim | Parcial | OK | API e Web ampliadas | Persistência customizada em tabela definitiva |
| Médico | Área do médico | MedicoAreaController | Index | B2BLaunch/Index | MEDICO | Sim | Sim | Sim | Sim | Parcial | OK | Criado Index e role MEDICO | Isolar dados por médico no backend real |
| Hospital | Área hospitalar | HospitalAreaController | Index | B2BLaunch/Index | HOSPITAL | Pós-login | Sim | Sim | Sim | Parcial | OK | Criado controller e redirect | Tela dedicada com dados da unidade |
| Financeiro | Pagamentos | FinanceiroController | Index | Financeiro/Index | FINANCEIRO/Admin tenant/Admin global | Sim | Sim | Sim | Sim | Parcial | OK | Menu preservado por perfil | Confirmar pagamento ponta-a-ponta em banco |
| White label | Configuração | WhiteLabelController | Index | WhiteLabel/Index | Admin global/admin tenant | Sim | Sim | Sim | Sim | Parcial | OK | Controller restringido por role | Publicação real de favicon/assets |
| Suporte | Chamados | SuporteController | Index | B2BLaunch/Index | SUPORTE/Admin/usuários autenticados | Sim | Sim | Sim | Sim | Parcial | OK | Mantida rota navegável | Workflow SLA persistente |
| Observabilidade | Monitoramento | ObservabilidadeController | Index | Observabilidade/Index | Admin global/suporte/auditor | Sim | Sim | Sim | Sim | Parcial | OK | Menu de governança | Métricas reais de APM |

## Problemas encontrados

- O ambiente de execução não possui `dotnet`, impedindo build/testes locais nesta rodada.
- A branch `main` e remoto não estavam disponíveis no checkout local; a branch de trabalho foi criada a partir de `work`.
- Alguns módulos comerciais já existiam como dashboards demonstráveis; foram consolidados sem duplicar controllers quando havia implementação parcial.

## Pendências reais

- Executar `dotnet clean`, `dotnet build` e `dotnet test` em ambiente com SDK .NET instalado.
- Persistir alterações customizadas da matriz de permissões em tabela auditável.
- Conectar dashboards comerciais aos serviços definitivos de tenant, billing e operação.
- Validar visual por screenshot em navegador com API disponível.
