# Central SaaS, clientes, equipe e autorização

## Baseline e método

- Commit inicial: `0a73e60` (`work`), árvore limpa. Não foi localizado `AGENTS.md` no repositório ou no diretório pai.
- Foram examinados o histórico recente, os workflows `dotnet-ci.yml` e `database-one-click.yml`, os relatórios funcionais v2.15.0, v2.15.5, v2.16.3–v2.16.5 e, principalmente, código, SQL e testes. Relatórios foram tratados como orientação, nunca como prova de execução.

## Inventário comprovável antes desta entrega

| Capacidade | Classificação | Evidência concreta |
|---|---|---|
| Login, hash, cookie e renovação | Implementada sem validação neste executor | `AccountController`, `AuthenticationService`, middleware de sessão e `Views/Account/Login.cshtml`; artefatos anteriores não substituem nova execução. |
| Contexto de organização | Parcial | claims e `ICurrentUserService`; `EffectivePermissionService` confere vínculo em `usuario_tenant_acessos`. A Central mostra ator/contexto, mas a gestão antiga ainda modela vários usuários pelo tenant principal. |
| Clientes e unidades | Implementada e coberta por contratos estáticos | `SaasClientService.ListCentralAsync`, `ClientesController`, `Views/Clientes/Index.cshtml` e `Details.cshtml`, `V2165CentralClientesContractTests`. Integração PostgreSQL permanece necessária. |
| Catálogo, contratação e módulos | Implementada sem validação de integração | `ModuleContractingService`, `tenant_modulos`, `modulos_sistema`; diagnóstico efetivo exige módulo habilitado e não transforma contratação em permissão. Não há telemetria confiável de uso: UI declara “uso não mensurado”. |
| Identidade, vínculo e perfis | Parcial | `usuarios`, `usuario_tenant_acessos`, `usuarios_perfis`, `perfis` e `SecurityAdministrationService`. Convite individual canônico completo e edição independente do vínculo secundário não foram encontrados. |
| Autorização no backend | Implementada sem validação E2E | `EffectivePermissionService.TestarAsync` valida identidade, vínculo, cliente, módulo e permissão; `SegurancaController` restringe mutações. |
| Navegação e dashboards | Parcial | Central e detalhe são reais; ainda existem páginas históricas de demonstração em `CommercialDemoWebController` que não servem como prova da jornada. |
| Auditoria | Implementada sem validação de integração | `IAuditService`, auditoria de cliente, usuário, perfil e sessão; falha posterior ao commit ainda precisa de política operacional uniforme. |

## Correções desta entrega

1. A criação de membro agora bloqueia a linha do cliente (`FOR UPDATE`) e revalida, na mesma transação, a assinatura vigente, o limite persistido do plano e o total ativo. Requisições concorrentes do mesmo cliente são serializadas e limite zero continua significando ilimitado conforme a regra existente.
2. Bloqueio/inativação passou a bloquear cliente e usuário antes da decisão. O último `ADMINISTRADOR_CLIENTE`/`ADMIN_CLIENTE` ativo do tenant é recusado com `409`, exigindo que outro administrador seja concedido primeiro.
3. Alteração de status e revogação das sessões daquele contexto são persistidas na mesma transação. Assim uma resposta de sucesso não deixa sessão antiga válida por uma falha intermediária.
4. Foram adicionados contratos de regressão para os invariantes de concorrência, limite, último administrador e revogação transacional.

## Matriz efetiva de autorização

| Ator/condição | Catálogo | Contrato ativo | Perfil/ação | Escopo do recurso | Resultado |
|---|---:|---:|---:|---:|---|
| Super administrador global real | sim | caminho global explícito | papel global | cliente selecionado e auditado | permitido nos endpoints globais declarados; não assume autoria profissional |
| Administrador do cliente | sim | sim | concedida e delegável | tenant da sessão | permitido |
| Administrador do cliente | sim | sim | ausente | tenant da sessão | `PERMISSION_NOT_GRANTED` |
| Usuário do cliente | sim | não | concedida | tenant da sessão | `MODULE_NOT_CONTRACTED` |
| Qualquer usuário local | sim | sim | concedida | outro tenant | `CROSS_TENANT_DENIED`/404 sem materializar o alvo |
| Vínculo bloqueado/revogado | sim | sim | concedida anteriormente | tenant bloqueado | negado na próxima requisição; sessões afetadas são revogadas |

## Validação e pendências concretas

- Verificações executadas nesta rodada são registradas no commit/PR; nenhuma revisão estática é apresentada como homologação.
- Se o SDK .NET 10 ou PostgreSQL isolado não estiverem disponíveis, restore/build/testes e a jornada navegada permanecem **não executados**, e não “aprovados”.
- O login real só pode ser declarado funcional após navegador + PostgreSQL comprovarem hash persistido, cookie, página protegida, refresh e logout. Credenciais, hashes e tokens não foram adicionados nem alterados nesta entrega.
- Pendências: consolidar convite individual no modelo existente; administrar vínculo secundário sem bloquear identidade global; executar testes comportamentais concorrentes em PostgreSQL; executar E2E integral e capturar telas reais em 360 px, 768 px e desktop.
- Nenhum SQL/migration foi alterado; instalação limpa e upgrade não se aplicam a este patch.
