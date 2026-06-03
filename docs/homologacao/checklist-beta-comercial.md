# Checklist Beta Comercial Controlada

## Objetivo
Consolidar o PlantãoPro como **Beta Comercial Controlada**: demonstrável para diretoria, homologável com um cliente real, operável com acompanhamento assistido e pronto para iniciar o app mobile.

## Usuários de teste
| Perfil | Usuário sugerido | Validação principal |
| --- | --- | --- |
| ADMINISTRADOR_GLOBAL | admin.global@plantaopro.local | Visão executiva, clientes, planos, assinaturas, faturamento SaaS, auditoria e observabilidade. |
| ADMINISTRADOR | admin.cliente@hospital.local | Operação do próprio cliente, usuários, cadastros e relatórios. |
| COORDENACAO | coordenacao@hospital.local | Plantões, escalas, convites, Central de Escala, agenda e alertas. |
| FINANCEIRO | financeiro@hospital.local | Pagamentos médicos, faturas SaaS, contestação e confirmação. |
| MEDICO | medico@hospital.local | Minha Agenda, plantões disponíveis, convites, pagamentos, notificações e suporte. |
| HOSPITAL | hospital@hospital.local | Plantões e escalas da unidade/hospital autorizado. |

## Checklist técnico obrigatório
- [ ] `git status` sem alterações inesperadas antes do início.
- [ ] Sem binários versionados: dll, exe, pdb, cache, zip, apk, aab, db, sqlite, bin, obj ou .vs.
- [ ] `dotnet clean` API/Web executado no ambiente com SDK instalado.
- [ ] `dotnet build` API/Web verde.
- [ ] Varredura sem `@page`, `asp-page`, `@model dynamic`, `href="#"`, `alert()`, `confirm()` ou `ADD CONSTRAINT IF NOT EXISTS`.
- [ ] Swagger abre e autentica com JWT.
- [ ] `/api/health` retorna saudável.
- [ ] Login Web mantém Cookie Authentication e redirecionamento pós-login.

## Checklist operacional médico
- [ ] Criar cliente, hospital, especialidade, médico e plantão em RASCUNHO.
- [ ] Publicar plantão somente com hospital/especialidade ativos e cliente apto.
- [ ] Médico visualiza somente seus dados e solicita plantão sem duplicidade/conflito.
- [ ] Coordenação confirma escala e reduz vaga disponível sem ficar negativa.
- [ ] Coordenação marca escala como REALIZADA.
- [ ] Financeiro gera pagamento apenas para escala REALIZADA.
- [ ] Financeiro confirma pagamento com valor, data e forma.
- [ ] Médico visualiza pagamento confirmado e notificação.
- [ ] Dashboard, Central de Escala, Agenda e relatório financeiro refletem o fluxo.
- [ ] Auditoria registra solicitação, confirmação, realização e pagamento.

## Checklist SaaS básico
- [ ] Criar plano ativo com limites comerciais.
- [ ] Criar assinatura ativa para cliente, com no máximo uma assinatura ativa por cliente.
- [ ] Bloquear nova assinatura em plano inativo.
- [ ] Bloquear cadastro quando limite de médicos/hospitais for atingido.
- [ ] Bloquear publicação quando limite mensal de plantões for atingido.
- [ ] Bloquear operação de cliente SUSPENSO ou CANCELADO.
- [ ] Bloquear API Mobile quando plano não permitir mobile.
- [ ] Bloquear BI quando plano não permitir BI.
- [ ] Gerar fatura SaaS, marcar como paga com valor/data/forma, cancelar/contestar com justificativa.
- [ ] Inadimplência, suspensão, reativação e notificação ao admin do cliente validadas.

## Checklist UX por perfil
- [ ] Coordenação: Central de Escala com KPIs, pendências, badges, filtros, EmptyState, toast e modais críticos.
- [ ] Médico: Minha Agenda mobile-first com próximo plantão, convites, escalas, pagamentos, feed de notificações e botões grandes.
- [ ] Financeiro: cards de pendente/pago/atrasado/contestado, filtros, timeline, modais e EmptyState.
- [ ] Admin global: visão executiva com clientes ativos/teste/suspensos/risco, faturas vencidas, receita prevista, chamados críticos e erros.
- [ ] Hospital: acesso restrito aos dados da unidade/hospital.

## Checklist segurança, auditoria e observabilidade
- [ ] Admin global vê todos os clientes; usuários comuns respeitam `cliente_id`; médico vê apenas dados próprios.
- [ ] Acessos negados retornam 403 amigável e registram auditoria.
- [ ] Erros não expõem stack trace, SQL, senha, hash, token ou segredo.
- [ ] Exportações/downloads registram auditoria.
- [ ] Observabilidade mostra erros do dia, endpoints lentos, últimos erros, últimos logins, acessos negados, faturas vencidas e chamados críticos.

## Critérios de aprovação
- Build verde no ambiente de homologação.
- Fluxo operacional ponta a ponta concluído sem exceção técnica.
- Fluxo SaaS básico concluído com bloqueios e auditoria.
- API Mobile MVP validada com JWT, paginação, payload leve, 401 sem token e 403 para plano sem mobile.
- Documentação de demo, deploy, mobile, suporte, Customer Success e go-live revisada.

## Pendências conhecidas aceitas para Beta
- Push notification real, publicação nas lojas e integrações externas ficam para pós-Sprint Zero mobile.
- Carga de performance deve ser repetida com dados reais do cliente piloto.
- Regras fiscais/contábeis definitivas dependem de validação do cliente e contador.

## Operação assistida com cliente real

- [ ] Cliente aparece no painel de operação assistida.
- [ ] Checklist padrão de implantação disponível.
- [ ] Conclusão de item registra auditoria e atualiza progresso.
- [ ] Reabertura exige justificativa.
- [ ] Ocorrência crítica gera alerta operacional.
- [ ] Treinamentos por perfil registrados.
- [ ] Nenhuma ocorrência crítica aberta antes do go-live beta.

## Operação assistida com cliente real

- [ ] Cliente aparece no painel de operação assistida.
- [ ] Checklist padrão de implantação disponível.
- [ ] Conclusão de item registra auditoria e atualiza progresso.
- [ ] Reabertura exige justificativa.
- [ ] Ocorrência crítica gera alerta operacional.
- [ ] Treinamentos por perfil registrados.
- [ ] Nenhuma ocorrência crítica aberta antes do go-live beta.
