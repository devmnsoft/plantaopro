# QA Permissões Final

## Resultado desta rodada

- Corrigido alinhamento de permissão Web para `Treinamento`.
- Corrigido bloqueio de `Marketplace` para administradores de tenant quando o módulo não estiver autorizado pelo fluxo global/plano.
- Alinhado acesso de Suporte e Auditoria para observabilidade/auditoria em leitura, reduzindo risco de acesso negado indevido em controllers já autorizados.
- Incluído `MinhaAssinatura` no guard de rotas para que plano/assinatura/uso/faturas não fiquem fora do controle modular.

## Matriz objetiva

| Perfil | Deve acessar | Deve bloquear | Status |
|---|---|---|---|
| ADMINISTRADOR_GLOBAL | Admin SaaS, tenants, planos, assinaturas, billing, white label, auditoria, observabilidade, permissões, marketplace | N/A | Parcial até QA runtime |
| ADMINISTRADOR_CLIENTE/ADMINISTRADOR | Portal cliente, usuários, perfis, assinatura, uso, faturas, operação do tenant | Admin SaaS global, billing global, observabilidade global, marketplace global | Parcial |
| COORDENADOR | Central de escala, plantões, convites, escalas, médicos, hospitais, especialidades | Billing global, Admin SaaS, white label | Parcial |
| MEDICO | Área médica, convites próprios, agenda própria, pagamentos próprios | Dados de outros médicos, financeiro geral, Admin SaaS | Parcial |
| FINANCEIRO | Pagamentos, faturas, relatórios financeiros | Admin SaaS, white label | Parcial |
| PARCEIRO | Portal parceiro, leads, propostas, clientes vinculados, comissões, repasses, materiais | Dados clínicos sensíveis e tenants não vinculados | Parcial |
| SUPORTE | Suporte, ajuda, auditoria, observabilidade, visão Admin SaaS autorizada | Alterações críticas sem regra específica | Parcial |
| AUDITOR | Auditoria, relatórios, LGPD/observabilidade em leitura | Criação/edição/exclusão | Parcial |
| COMERCIAL | Comercial, propostas, planos, marketplace | Dados clínicos/financeiro operacional | Parcial |
| CUSTOMER_SUCCESS | Jornada, onboarding, clientes, suporte | Billing global e operações clínicas sensíveis | Parcial |

## Pendências

- Executar login real por perfil com banco e API disponíveis.
- Validar claims `tenant_id`, `cliente_id` e `role` emitidas no login.
