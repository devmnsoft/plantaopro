# PlantãoPro — Profissionalização SaaS fase 1

## Implementado nesta rodada

- Serviços centrais Web e API para contexto do usuário, permissão, acesso a tenant e acesso a módulos.
- Redirecionamento pós-login por prioridade real de perfil, evitando destino inexistente para coordenação e comercial.
- Menu centralizado por perfil, plano, módulo e tenant no layout principal.
- Base visual SaaS reforçada no layout, nos estados bloqueados e no script de confirmação modal.
- Rotas operacionais essenciais adicionadas para `Convites/Index` e `Pagamentos/Index`, evitando links quebrados no menu profissional.
- Documentação de matriz, QA e pendências atualizada conforme código entregue.

## Mapa real de rotas, views, menus e permissões

| Tela | Controller | Action | View | Perfil esperado | Menu existe? | Action existe? | View existe? | Permissão aplicada? | Tenant aplicado? | Status | Correção aplicada | Pendência real |
|---|---|---:|---|---|---|---|---|---|---|---|---|---|
| Home | Home | Dashboard | Home/Dashboard | autenticado operacional | Sim | Sim | Sim | Parcial por menu | Parcial | Parcial | Mantido como fallback seguro | Consolidar policy por controller |
| Account/Login | Account | Login | Account/Login | anônimo | Não | Sim | Sim | AllowAnonymous | N/A | OK | Redirecionamento por prioridade | Teste integrado com API real |
| Dashboard | Home | Dashboard | Home/Dashboard | admin/coord/financeiro | Sim | Sim | Sim | Parcial | Parcial | Parcial | Menu centralizado | Policy dedicada |
| AdminSaas | AdminSaas | Index | AdminSaas/Dashboard | ADMINISTRADOR_GLOBAL | Sim | Sim | Sim | Authorize roles | Global | OK | Menu e redirect corrigidos | Remover suporte/auditor se regra de negócio exigir |
| ClientePortal | ClientePortal | Index | ClientePortal/Dashboard | ADMINISTRADOR_CLIENTE/ADMINISTRADOR | Sim | Sim | Sim | Authorize roles | Tenant | OK | Menu e redirect corrigidos | Validar tenant em chamadas API reais |
| ParceiroPortal | ParceiroPortal | Index | ParceiroPortal/Dashboard | PARCEIRO | Sim | Sim | Sim | Authorize roles | Parceiro | OK | Menu segregado | Persistir vínculo parceiro-tenant |
| CentralEscala | CentralEscala | Index | CentralEscala/Index | COORDENADOR | Sim | Sim | Sim | Authorize existente | Tenant | OK | Redirect corrigido de Coordenacao para CentralEscala | QA com criação real de plantão |
| MedicoArea | MedicoArea | Index | B2BLaunch/Index | MEDICO | Sim | Sim | Sim | Authorize roles | Próprio médico | Parcial | Menu médico isolado | Aplicar filtro médico em dados reais |
| Financeiro | Financeiro | Index | Financeiro/Index | FINANCEIRO | Sim | Sim | Sim | Authorize existente | Tenant | OK | Menu financeiro | Confirmar exportação com API real |
| Clientes | Clientes | Index | Clientes/Index | ADMINISTRADOR_GLOBAL/CS | Sim | Sim | Sim | Authorize existente | Global/tenant | Parcial | Menu Admin SaaS | Revisar acesso CS por tenant |
| Planos | Planos | Index | Planos/Index | ADMINISTRADOR_GLOBAL/COMERCIAL | Sim | Sim | Sim | Authorize existente | Global | Parcial | Menu Admin SaaS | Unificar Billing global/API |
| Assinaturas | Assinaturas | Index | Assinaturas/Index | ADMINISTRADOR_GLOBAL/admin cliente | Sim | Sim | Sim | Authorize existente | Tenant/global | Parcial | Menu cliente/admin | Bloqueios por limite reais na API |
| Billing | FaturamentoSaas | Index | FaturamentoSaas/Index | GLOBAL/FINANCEIRO | Sim | Sim | Sim | Authorize existente | Tenant/global | Parcial | Menu Billing/Faturas | Criar alias Billing/Faturas se necessário |
| WhiteLabel | WhiteLabel | Index | WhiteLabel/Index | GLOBAL/admin cliente | Sim | Sim | Sim | Authorize roles | Tenant | OK | Menu e bloqueio visual | Persistência de tema avançado |
| Usuarios | Usuarios | Index | Usuarios/Index | GLOBAL/admin cliente | Sim | Sim | Sim | Authorize | Tenant | Parcial | Menu cliente | Limite por plano real |
| Perfis | Perfis | Index | Perfis/Index | GLOBAL/admin cliente | Sim | Sim | Sim | Authorize roles | Tenant | OK | Menu cliente | Permissões custom persistentes |
| Permissoes | Permissoes | Index | Permissoes/Matriz | GLOBAL/admin/auditor | Sim | Sim | Sim | Authorize roles | Tenant/global | OK | Matriz preservada | Policy provider formal |
| Plantões | Plantoes | Index | Plantoes/Index | coord/admin | Sim | Sim | Sim | Authorize existente | Tenant | OK | Menu operação | Limite mensal API |
| Convites | Convites | Index | B2BLaunch/Index | coord/médico | Sim | Sim | Sim | Authorize roles | Tenant/médico | Parcial | Controller adicionado | Trocar placeholder por view operacional real |
| Escalas | Escalas | Index | Escalas/Index | coord/admin | Sim | Sim | Sim | Authorize existente | Tenant | OK | Menu operação | QA fluxo confirmar escala |
| Pagamentos | Pagamentos | Index | B2BLaunch/Index | financeiro/médico | Sim | Sim | Sim | Authorize roles | Tenant/médico | Parcial | Controller adicionado | Trocar placeholder por financeiro real |
| Auditoria | Auditoria | Index | Auditoria/Index | global/auditor/suporte | Sim | Sim | Sim | Authorize existente | Tenant/global | OK | Menu governança | Auditar access denied via filtro global |
| Observabilidade | Observabilidade | Index | Observabilidade/Index | global/suporte | Sim | Sim | Sim | Authorize existente | Global | OK | Menu admin | Separar visão tenant |
| LGPD | Lgpd | Index | Lgpd/Index | autenticado | Sim | Sim | Sim | Authorize | Tenant | OK | Menu governança | Fluxos titular completos |
| Ajuda | Ajuda | Index | Ajuda/Index | autenticado | Sim | Sim | Sim | Authorize/consulta perfil | Tenant/perfil | OK | Menu governança | Métricas de treinamento |

## Build e validações

O ambiente desta execução não possui SDK `dotnet` instalado. Os comandos obrigatórios foram executados, mas retornaram `dotnet: command not found`. A validação está limitada a inspeção estática com `rg` e revisão de rotas/menus.

## Pendências reais

- Instalar SDK .NET no ambiente de CI/execução para confirmar build final.
- Substituir telas genéricas `B2BLaunch/Index` de Convites e Pagamentos por views operacionais completas.
- Evoluir filtros tenant/cliente/médico diretamente em todas as queries operacionais legadas.
- Persistir permissões customizadas por tenant em banco e ligar ao `PermissionService`.
- Implementar auditoria automática para todos os eventos de acesso negado via middleware/filtro.

## Implementação real complementar — permissões, menus e rotas protegidas

Nesta rodada, a Fase 1 deixou de depender apenas de especificação e passou a ter enforcement no código MVC:

- O Web agora registra um filtro global de guarda SaaS para telas conhecidas da plataforma.
- O filtro bloqueia acesso por módulo/ação antes da execução da action, registra log de auditoria operacional em acesso negado e redireciona para a tela amigável de acesso negado.
- O menu lateral passou a validar perfil mínimo além da permissão do módulo, evitando que administradores de cliente vejam o grupo **Admin SaaS** global.
- O login passa a popular `tenant_id`, `cliente_id`, nome do tenant e nome do cliente nos claims quando a API retorna cliente vinculado.
- A rota `/Billing/Faturas` foi consolidada como alias funcional para o faturamento SaaS existente, sem criar tela falsa ou controller duplicado de cobrança.

### Pendências reais

- Validar em ambiente com SDK .NET instalado, pois o container desta execução não possui `dotnet` disponível.
- Evoluir o guard para carregar matriz de permissões persistida quando o cadastro de permissões por tenant estiver completo.
- Conectar bloqueios por módulo/plano a dados reais de assinatura em vez de regras locais de demonstração.
