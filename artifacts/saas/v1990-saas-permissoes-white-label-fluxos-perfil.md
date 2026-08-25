# PlantãoPro v1.99.0 — permissões, white label e fluxos por perfil

## Resumo da implementação

A versão consolida um catálogo canônico e previsível de autorização B2B, preservando os códigos legados no banco e JWT. A autorização de white label passou a ser aplicada no servidor, com validação explícita do tenant, papéis administrativos, contraste, conteúdo textual e assets. O menu existente continua sendo montado por perfil, permissão e módulo em `MenuBuilderService`; nenhuma alteração visual foi realizada.

## Perfis criados/consolidados

| Perfil canônico | Código compatível | Fluxo principal |
|---|---|---|
| PlatformAdmin | `ADMINISTRADOR_GLOBAL` | tenants, planos, módulos, auditoria global e saúde da plataforma |
| TenantAdmin | `ADMINISTRADOR_CLIENTE` | usuários, unidades, perfis, assinatura e white label do próprio tenant |
| UnitManager | `COORDENADOR` | equipe, plantões, pendências e relatórios da unidade |
| ScheduleManager | `COORDENACAO` | escalas, plantões, conflitos, cobertura e relatórios operacionais |
| Professional | `MEDICO` | agenda/plantões, confirmações, histórico e dados próprios |
| FinanceManager | `FINANCEIRO` | financeiro e exportações autorizadas |
| Auditor | `AUDITOR` | auditoria e relatórios somente leitura |
| Support | `SUPORTE` | diagnóstico técnico controlado e auditável, somente leitura por padrão |

## Permissões disponíveis

`tenants.read`, `tenants.manage`, `users.read`, `users.manage`, `roles.read`, `roles.manage`, `units.read`, `units.manage`, `professionals.read`, `professionals.manage`, `schedules.read`, `schedules.manage`, `shifts.read`, `shifts.manage`, `reports.read`, `reports.export`, `finance.read`, `finance.manage`, `audit.read`, `settings.manage`, `white_label.read`, `white_label.manage`, `plans.read`, `plans.manage`, `modules.manage`.

## Matriz perfil x permissão

| Perfil | Permissões efetivas resumidas |
|---|---|
| PlatformAdmin | todas |
| TenantAdmin | usuários/perfis/unidades/profissionais/escalas/plantões; relatórios; leitura financeira/auditoria/plano; configurações e white label |
| UnitManager | leitura de unidade, profissionais, escalas, plantões e relatórios |
| ScheduleManager | leitura de unidade/profissionais; gestão de escalas/plantões; relatórios e exportação |
| Professional | leitura de suas escalas e plantões (escopo próprio é obrigatório na query operacional) |
| FinanceManager | leitura/exportação de relatórios e gestão financeira |
| Auditor | leitura de relatórios e auditoria; exportação autorizada; nenhuma mutação crítica |
| Support | leitura de tenants, usuários e auditoria; mutações exigem elevação explícita e auditada |

## Matriz plano x módulo

A fonte de verdade é `tenant_modulos`/assinatura. PlatformAdmin não é limitado por plano; todos os demais exigem módulo ativo **e** permissão.

| Módulo | Essencial | Profissional | Enterprise |
|---|:---:|:---:|:---:|
| Escalas / Plantões / Profissionais / Unidades | ✓ | ✓ | ✓ |
| Relatórios | básico | ✓ | ✓ |
| Financeiro | — | ✓ | ✓ |
| Auditoria | — | leitura | ✓ |
| White Label | — | opcional | ✓ |
| API/Integrações | — | — | ✓ |
| Mobile | opcional | ✓ | ✓ |

O menu usa `IsModuleEnabled` e o guard de rota aplica a mesma decisão. Acesso direto negado produz 403 e o middleware existente registra bloqueios de autorização sem segredos.

## Regras de isolamento multi-tenant

* Uma rota com tenant explícito agora compara o identificador com a claim `tenant_id` antes de abrir conexão; somente PlatformAdmin pode consultar outro tenant.
* White label sempre resolve o contexto autorizado antes de leitura/escrita e todas as operações SQL usam `tenant_id`.
* Queries operacionais devem receber o tenant do contexto autenticado, nunca da URL isoladamente.
* Rotas globais continuam exigindo `ADMINISTRADOR_GLOBAL`/policy `GlobalAccess`.
* Evidência auditada: `TenantContextService`, serviço de white label e testes contratuais v1.99.0.

## Fluxo de white label

1. TenantAdmin abre a configuração/preview do próprio tenant.
2. O servidor valida tenant e papel, formato de cor e contraste WCAG AA 4.5:1.
3. Campos textuais rejeitam marcação HTML/script; CSS arbitrário não é aceito.
4. Logo/favicon aceitam apenas PNG, JPEG, WebP, SVG ou ICO, até 2 MiB, em URL HTTP(S).
5. A publicação persiste por `tenant_id` e gera evento `ALTERAR_WHITE_LABEL` sem dados secretos.
6. A restauração publica o DTO padrão: nome PlantãoPro e paleta segura.

## Telas alteradas

Nenhuma alteração visual. As telas existentes de perfis, permissões, usuários, assinatura, diagnóstico e white label permanecem funcionais e protegidas pelo catálogo/menu/guard existentes. Os endpoints de mutação de white label agora exigem explicitamente administrador global ou do tenant.

## Scripts de banco alterados

Nenhum DDL novo foi necessário: `perfis`, `permissoes`, `perfil_permissoes`, `tenant_modulos`, `assinaturas`, `tenant_white_label` e auditoria já existem nos scripts canônicos e consolidados. Portanto não houve migração destrutiva nem risco de upgrade.

## Testes executados

Consulte o resultado final da PR. Foi adicionada cobertura contratual para catálogo, separação global/tenant, Auditor/Support somente leitura, fallback, contraste, HTML, assets e papéis dos endpoints.

## Limitações e próximos passos

* A matriz comercial de planos é uma referência; a concessão real permanece nos registros por tenant para suportar contratos customizados.
* Evoluir `IModuleAccessService` para cache distribuído e invalidação por evento de alteração de assinatura.
* Adicionar testes PostgreSQL de integração quando o serviço estiver disponível no CI.
* Migrar gradualmente permissões legadas `MODULO:ACAO` para nomes canônicos, mantendo compatibilidade de tokens durante a transição.
