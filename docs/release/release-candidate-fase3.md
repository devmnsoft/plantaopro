# PlantãoPro — Release Candidate Fase 3

Data da rodada: 2026-06-09.
Branch: `codex/plantaopro-release-candidate-fase3`.

## Escopo executado

A Fase 3 foi tratada como estabilização de Release Candidate, sem criação de grandes módulos novos. A rodada concentrou-se em build/varredura, saneamento de navegação e permissões, documentação de homologação real e registro das limitações ambientais encontradas.

## Validações técnicas realizadas

| Validação | Resultado | Evidência |
|---|---:|---|
| Limpeza legado não PlantãoPro | OK | `rg` não retornou ocorrências em código ativo. |
| Build API | Bloqueado | Ambiente sem SDK .NET: `dotnet: command not found`. |
| Build Web | Bloqueado | Ambiente sem SDK .NET: `dotnet: command not found`. |
| Varredura Razor/links críticos | OK | Sem `href="#"`, `alert(`, `confirm(`, `@page`, `asp-page`, `@model dynamic` ou `NotImplementedException` em Web/API ativos. |
| Varredura permissões/tenant | Parcial | Há cobertura em serviços de segurança Web e guard de rotas; validação runtime depende de SDK/API. |

## Correções aplicadas

- Menu Admin SaaS passou a apontar Billing para `Billing/Faturas`, mantendo a semântica exigida para homologação de faturas.
- Menu Financeiro passou a apontar Faturas para `Billing/Faturas`, evitando dispersão entre nomes de módulo.
- Menu Parceiro passou a apontar Leads, Propostas, Comissões, Repasses e Materiais para actions reais do `ParceiroPortalController`.
- Guard de rotas passou a cobrir `MinhaAssinatura` e `Treinamento`, reduzindo bypass de módulos sensíveis.
- Permissões Web passaram a tratar Treinamento como módulo acessível autenticado e a bloquear Marketplace para tenant admin quando não for liberado pelo fluxo global/plano.
- Suporte e Auditoria foram alinhados aos módulos de observabilidade/auditoria para evitar acesso negado indevido em telas autorizadas por controller.

## Mapa real de homologação

| Módulo | Tela | Controller | Action | API relacionada | Perfil autorizado | Permissão aplicada? | Tenant aplicado? | Plano/módulo aplicado? | Validação existe? | Auditoria existe? | Status | Correção aplicada | Pendência real |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Login | Login | Account | Login | Auth/cookie | Público/autenticado | Sim | N/A | N/A | Sim | Parcial | Parcial | Mantido | Validar runtime com SDK .NET. |
| Dashboard | Dashboard | Home | Dashboard | `/api/dashboard` quando integrado | Autenticado | Sim | Parcial | Parcial | Parcial | Parcial | Parcial | Mantido | QA manual pendente por ambiente. |
| AdminSaas | Index | AdminSaas | Index | `/api/admin-saas/resumo` planejada/integrada por tela | ADMINISTRADOR_GLOBAL, SUPORTE, AUDITOR | Sim | Global | Sim | Parcial | Parcial | Parcial | Permissão suporte/auditoria alinhada | Validar endpoints reais. |
| ClientePortal | Index | ClientePortal | Index | `/api/cliente-portal/resumo` | ADMINISTRADOR_CLIENTE, ADMINISTRADOR | Sim | Sim | Sim | Parcial | Parcial | Parcial | Mantido | Validar tenant real. |
| ParceiroPortal | Index/Leads/Clientes/Propostas | ParceiroPortal | Múltiplas | `/api/parceiro-portal/resumo` | PARCEIRO, ADMINISTRADOR_GLOBAL | Sim | Sim | Sim | Parcial | Parcial | OK | Links corrigidos para actions reais | Validar vínculos do parceiro. |
| Planos | Index | Planos | Index | `/api/planos` | ADMINISTRADOR_GLOBAL/COMERCIAL | Sim | Global | N/A | Parcial | Parcial | Parcial | Mantido | Testar CRUD/API. |
| Assinaturas | Index | Assinaturas | Index | `/api/assinaturas` | ADMINISTRADOR_GLOBAL | Sim | Sim | Sim | Parcial | Parcial | Parcial | Mantido | Testar troca de plano. |
| Billing | Faturas | Billing/FaturamentoSaas | Faturas/Index | `/api/billing/faturas`, `/api/faturamento-saas/faturas` | ADMINISTRADOR_GLOBAL, FINANCEIRO, ADMIN tenant | Sim | Sim | Sim | Sim | Parcial | Parcial | Menus apontam para `Billing/Faturas` | Confirmar contratos de API. |
| Marketplace | Index | Marketplace | Index | Marketplace/módulos | ADMINISTRADOR_GLOBAL | Sim | N/A | Sim | Parcial | Parcial | Parcial | Tenant admin bloqueado por permissão | Definir liberação por plano. |
| WhiteLabel | Index | WhiteLabel | Index | `/api/white-label/configuracao` | ADMINISTRADOR_GLOBAL, ADMIN tenant | Sim | Sim | Sim | Parcial | Parcial | Parcial | Mantido | Validar upload real. |
| Perfis | Index | Perfis | Index | Perfis | ADMIN tenant/global | Sim | Sim | Parcial | Parcial | Parcial | Parcial | Mantido | Testar persistência. |
| Permissões | Matriz | Permissoes | Matriz | Matriz local/API futura | ADMIN/SUPORTE/AUDITOR | Sim | Parcial | Parcial | Sim | Parcial | OK | Mantido | Persistência granular futura. |
| Usuários | Index | Usuarios | Index | Usuários | ADMIN tenant/global | Sim | Sim | Sim | Parcial | Parcial | Parcial | Mantido | QA CRUD. |
| Clientes | Index | Clientes | Index | Clientes | ADMINISTRADOR_GLOBAL/CS | Sim | Global/tenant conforme perfil | N/A | Parcial | Parcial | Parcial | Mantido | Validar isolamento. |
| Hospitais | Index | Hospitais | Index | Hospitais | Coordenação/admin | Sim | Sim | Sim | Parcial | Parcial | Parcial | Mantido | QA criação. |
| Especialidades | Index | Especialidades | Index | Especialidades | Coordenação/admin | Sim | Sim | Sim | Parcial | Parcial | Parcial | Mantido | QA criação. |
| Médicos | Index | Medicos | Index | Médicos | Coordenação/admin | Sim | Sim | Sim | Parcial | Parcial | Parcial | Mantido | QA vínculo usuário médico. |
| Plantões | Index | Plantoes | Index | Plantões | Coordenação/admin/hospital | Sim | Sim | Sim | Parcial | Parcial | Parcial | Mantido | QA publicação. |
| Convites | Index | Convites | Index | Convites | Coordenação/médico/admin | Sim | Sim | Sim | Parcial | Parcial | Parcial | Mantido | Médico deve ver próprios convites. |
| Escalas | Index | Escalas | Index | Escalas | Coordenação/admin/hospital | Sim | Sim | Sim | Parcial | Parcial | Parcial | Mantido | QA confirmação/substituição. |
| MedicoArea | Index | MedicoArea | Index | `/api/medicos/me/agenda` | MEDICO | Sim | Próprio médico | Sim | Parcial | Parcial | Parcial | Mantido | Validar isolamento por médico. |
| Financeiro | Index | Financeiro | Index | `/api/financeiro/pagamentos` | FINANCEIRO/admin tenant/global | Sim | Sim | Sim | Sim | Parcial | Parcial | Menu de faturas corrigido | QA confirmação pagamento. |
| Pagamentos | Index | Pagamentos/Financeiro | Index | `/api/financeiro/pagamentos` | FINANCEIRO/MEDICO/admin | Sim | Sim/próprio médico | Sim | Parcial | Parcial | Parcial | Mantido | Separar visão médico/financeiro em runtime. |
| LGPD | Index | Lgpd | Index | `/api/lgpd/solicitacoes` | Autenticado conforme perfil | Sim | Sim | N/A | Parcial | Parcial | Parcial | Mantido | Testar exportação/anonimização. |
| Auditoria | Index | Auditoria | Index | `/api/auditoria` | ADMIN_GLOBAL/AUDITOR/SUPORTE | Sim | Sim/global | N/A | Parcial | Sim | Parcial | Permissão observabilidade/auditoria alinhada | Validar eventos críticos. |
| Observabilidade | Index | Observabilidade | Index | `/api/observabilidade` | ADMIN_GLOBAL/SUPORTE/AUDITOR | Sim | Global | N/A | Parcial | N/A | Parcial | Permissão suporte/auditoria alinhada | Validar métricas reais. |
| Ajuda | Index | Ajuda | Index | Ajuda | Autenticado | Sim | N/A | N/A | Sim | N/A | OK | Mantido | Conteúdo evolutivo. |
| Treinamento | Index | Treinamento | Index | Conteúdo treinamento | Admin tenant/autenticado | Sim | Sim | Sim | Parcial | Parcial | Parcial | Guard e permissão adicionados | Expandir trilhas por perfil. |
| Onboarding | Index | Onboarding | Index | Onboarding | ADMIN tenant/CS | Sim | Sim | Sim | Parcial | Parcial | Parcial | Mantido | Validar tarefas. |
| Comercial | Funil/Leads | Comercial | Múltiplas | Leads/propostas | COMERCIAL/global/parceiro limitado | Sim | Parcial | N/A | Parcial | Parcial | Parcial | Links parceiro removidos do menu comercial genérico | QA conversão completa. |
| Propostas | Index/Create | PropostasComerciais | Múltiplas | Propostas | COMERCIAL/global/parceiro | Sim | Sim | N/A | Parcial | Parcial | Parcial | Parceiro usa action própria | Validar validade/provisionamento. |
| CentralEscala | Index | CentralEscala | Index | `/api/central-escala/resumo` | COORDENADOR/admin | Sim | Sim | Sim | Parcial | Parcial | Parcial | Mantido | QA fluxo operacional completo. |

## Pendências reais para go/no-go

1. Instalar SDK .NET compatível no ambiente de CI/agente para executar build e testes.
2. Executar QA manual autenticado com banco/API disponíveis.
3. Validar contratos reais dos endpoints de billing, white label, auditoria e observabilidade.
4. Confirmar persistência e auditoria fim a fim em ações críticas de proposta, tenant, assinatura, plantão, convite, escala e pagamento.
5. Executar teste visual com navegador após subir Web/API.
