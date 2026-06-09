# QA Fluxo Comercial Final

## Fluxo alvo

Landing -> Planos -> Simulador -> Lead -> Proposta -> Aprovação -> Conversão -> Tenant -> Assinatura -> Admin Cliente -> Onboarding.

## Status real

| Etapa | Status | Observação |
|---|---|---|
| Landing | Parcial | Existe fluxo público demonstrável. |
| Planos | Parcial | Planos públicos e internos existem; validar persistência/API. |
| Simulador | Parcial | Recomendação demonstrável existe. |
| Lead | Parcial | Cadastro/contato registra intenção em fluxo Web; persistência real deve ser validada. |
| Proposta | Parcial | Tela e preview existem; validar validade e aprovação real. |
| Aprovação | Pendente runtime | Requer API/banco. |
| Conversão cliente/tenant | Pendente runtime | Verificar idempotência e CNPJ único. |
| Assinatura | Parcial | Telas existem; validar criação automática. |
| Admin cliente | Pendente runtime | Validar criação e primeiro login. |
| Onboarding | Parcial | Tela existe; validar tarefas geradas. |

## Pendências críticas

- Garantir que proposta vencida não aprove.
- Garantir provisionamento idempotente.
- Auditar falha de provisionamento sem duplicar tenant.
