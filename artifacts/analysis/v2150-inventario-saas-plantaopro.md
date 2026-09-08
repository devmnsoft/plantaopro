# PlantãoPro v2.15.0 — inventário SaaS e diagnóstico técnico

Data da auditoria: 2026-09-08

Base auditada: `origin/main` em `e5983410`

Escopo: API, Web, contratos de acesso, migrations, scripts, testes e experiência das áreas SaaS.

## Resumo executivo

O PlantãoPro já possui uma fundação SaaS relevante: autenticação por identificador, catálogo de perfis e permissões, módulos por tenant, áreas global e local, auditoria e vários domínios operacionais. A entrega v2.14.9 consolidou login por e-mail/CPF/CNPJ, claims de permissões/módulos, gestão persistente de usuários e catálogo comercial inicial.

A auditoria v2.15.0, porém, encontrou coexistência de implementações modernas e legadas. Essa duplicidade cria divergências entre o que o menu comunica e o que alguns endpoints realmente protegem. Os riscos mais altos estão na fronteira de tenant, no ciclo de vida da sessão e em endpoints antigos ainda globais ou apenas aparentando persistência.

Decisão desta rodada: fechar primeiro o limite de confiança do Bloco 1. O trabalho de implementação fica restrito a isolamento de tenant, Super Admin global, sessão revogável/expirável, escopo de contadores de segurança e auditoria de contexto. O Bloco 2 permanece preparado pelo catálogo persistente existente, sem ampliar cobrança ou contratação antes de estabilizar acesso.

## Dimensão do repositório auditado

| Área | Inventário observado |
|---|---:|
| Controllers API | 75 arquivos |
| Controllers Web | 73 arquivos |
| Views Razor | 403 arquivos |
| Migrations SQL | 59 arquivos |
| Arquivos de teste C# | 88 arquivos |
| Views com texto “Como usar esta tela” | 25 ocorrências |

Os números indicam cobertura funcional ampla, mas não equivalem a maturidade. Há telas e endpoints com nomes comerciais completos que ainda devolvem coleções vazias ou confirmações sem persistência.

## Matriz de módulos e maturidade

| Módulo/contexto | Telas e controllers principais | Serviços/tabelas/scripts | Permissões/tenant | Status | Risco | Prioridade |
|---|---|---|---|---|---|---|
| Autenticação e sessão | `AccountController`, `AuthController`, Login | `AuthService`, `login_tentativas`, `auth_sessoes`, recuperação de senha | JWT + cookie + claims; sessão ainda não era registrada/validada | Parcial | Crítico | P0 |
| Contexto global/tenant | APIs `contexto` e `impersonacao` | `ContextoService`, `ContextoRepository`, `contexto_sessoes`, `contexto_trocas` | políticas efetivas; Super Admin dependia de vínculo individual | Parcial/quebrado | Alto | P0 |
| Usuários | telas `Usuarios` e legado `Usuario`; APIs `seguranca/usuarios` e `usuarios` | `SecurityAdministrationService`, `UserService`, `usuarios`, `usuarios_perfis` | fluxo moderno isolado; endpoint legado listava/desbloqueava globalmente | Parcial/quebrado | Crítico | P0 |
| Perfis e permissões | `Perfis`, `Permissoes`, `Seguranca` | `EffectivePermissionService`, `perfil_permissoes`, overrides | cálculo efetivo real existe; parte de `api/seguranca` ainda responde placeholder | Parcial | Alto | P1 |
| Menus | `_AppSidebar`, guard global de rotas | `PermissionService`, `ModuleAccessService`, `SaasRouteGuardFilter` | claims `module` e `permission`; fallback legado permanece | Parcial | Alto | P1 |
| Clientes/tenants | `Clientes`, `ClientePortal`, APIs `clientes`, `tenants` | `ClienteService`, `TenantContextService`, `clientes`, `tenants` | controller de clientes aceitava administrador local em operações globais | Quebrado | Crítico | P0 |
| Módulos | `Modulos`, marketplace | `SaasModuleCatalogService`, `modulos_sistema`, `tenant_modulos`, `plano_modulos` | alterações comerciais restritas ao global; leitura por contexto | Pronto para fundação | Médio | P1 |
| Planos/assinaturas | `Planos`, `Assinaturas`, `MinhaAssinatura` | `planos`, `assinaturas`, guard de assinatura | domínio persistente, mas regras comerciais estão espalhadas | Parcial | Alto | P1 |
| Cobrança SaaS | `FaturamentoSaas`, billing | `faturas_saas`, bloqueios e relatórios | rotas globais existem | Parcial | Alto | P1 |
| Operação assistencial | escalas, plantões, agenda, convites, cobertura | serviços Dapper e tabelas operacionais | muitos fluxos usam `cliente_id`; legado possui queries sem escopo explícito | Parcial | Alto | P2 |
| Profissionais/unidades | médicos, hospitais, especialidades | serviços legados em `Data.cs` | listagens antigas sem tenant em várias queries | Quebrado para SaaS estrito | Crítico | P1 |
| Saúde 360 | pacientes, agendamento, triagem, consultas, CID, prescrição | serviços/repositórios clínicos e work items | serviços mais novos recebem tenant/unidade | Parcial | Alto | P2 |
| Financeiro assistencial | pagamentos, caixa, fechamentos, repasses, glosas | serviços financeiros e migrations próprias | mistura de serviços tenant-aware e legados | Parcial | Alto | P2 |
| Relatórios/BI | relatórios SaaS, executivos e BI | catálogo/exportação e queries Dapper | há endpoints de séries/ranking ainda vazios | Parcial | Médio | P3 |
| Notificações/comunicação | central de notificações, conversas | repositories por tenant/usuário | núcleo recente é tenant-aware; endpoints antigos coexistem | Parcial | Médio | P2 |
| Observabilidade/auditoria | auditoria, logs e saúde | `RequestLoggingMiddleware`, tabelas de logs e auditoria | logs persistentes; telas/rotas de segurança têm respostas vazias | Parcial | Alto | P1 |
| White label/onboarding | cadastro, onboarding, templates | serviços self-service e migrations | existe estrutura, sem fechamento comercial completo | Parcial | Médio | P3 |
| Ajuda/UX premium | ajuda, LGPD, guias em páginas | componentes Razor/CSS/JS compartilhados | cobertura de mini manual ainda baixa frente a 403 views | Parcial | Médio | P3 |

## O que já está implementado

- Login aceita e normaliza e-mail, CPF e CNPJ.
- O CNPJ não é tratado como senha coletiva: quando encontra múltiplos usuários, exige identificador individual.
- Falhas de autenticação usam resposta genérica, fingerprint do identificador e lockout configurável.
- Senhas BCrypt são suportadas e hashes legados são migrados após autenticação válida.
- Perfis são carregados de forma normalizada; Super Admin, administrador do cliente e perfis operacionais têm catálogo compartilhado.
- JWT e cookie Web transportam perfil principal, papéis, tenant/cliente, escopo, módulos e permissões.
- O menu principal filtra grupos e links por papel, módulo contratado e permissão efetiva para sessões v2.14.9.
- Existe guard global de rotas Web para impedir acesso direto a controllers sem módulo liberado.
- `EffectivePermissionService` considera usuário/tenant ativos, módulo contratado, perfil, concessões e negações especiais.
- Gestão moderna de usuários persiste cadastro, edição, status, perfis e revogação administrativa.
- Catálogo de módulos, vínculo de módulo ao tenant/plano, preço e limite já são persistentes.
- Auditoria central e logs estruturados existem; tentativas de login não gravam o identificador em claro.
- A tela de login possui labels, validação, Caps Lock, recuperação, estados de erro/conexão, responsividade e “Como usar esta tela”.

## Implementado parcialmente

- `auth_sessoes` e tabelas de revogação existem, mas o login não criava a sessão e o bearer não validava revogação.
- Contexto assistido grava sessão, histórico, motivo e recentes, mas o Super Admin não conseguia selecionar qualquer tenant ativo sem vínculo em `usuario_tenant_acessos`.
- Menus novos usam catálogo efetivo, mas sessões antigas ainda entram por fallback baseado em papel.
- A Central de Segurança tem usuários reais e cálculo real de permissão, porém sessões, tentativas, auditoria e parte do CRUD de perfis ainda contêm respostas estáticas.
- Planos, módulos, assinaturas e faturas possuem modelos e endpoints, mas regras de upgrade/downgrade, competência, inadimplência e limites precisam de um único agregado comercial.
- Operação, Saúde 360 e financeiro possuem muita implementação real, porém parte do código anterior à fundação SaaS não recebeu escopo uniforme.
- A experiência premium existe no login e em áreas novas; o padrão de guia e estados não cobre a maioria das 403 views.

## Quebrado ou inseguro antes desta implementação

1. `GET /api/usuarios` listava usuários de todos os clientes para administrador local.
2. `POST /api/usuarios/unlock/{id}` permitia desbloquear usuário de outro tenant.
3. `api/clientes` aceitava `ADMINISTRADOR`, expondo e alterando o catálogo global de clientes.
4. O dashboard de segurança filtrava usuários pelo tenant, mas contava todas as tentativas e sessões do banco.
5. Um administrador sem claim de tenant poderia cair em consultas com `tenantId = null`, que significa escopo global.
6. A tabela de sessões não participava efetivamente do login/autorização, portanto “revogar sessões” não invalidava JWTs emitidos.
7. O Super Admin com vínculo legado a cliente podia herdar tenant no token e ser afetado pelo bloqueio daquele cliente.
8. A seleção de contexto do Super Admin dependia de concessão individual, contrariando a regra de visão global.
9. `SegurancaController` ainda anuncia persistência em endpoints que apenas devolvem objetos/arrays estáticos.
10. Há services operacionais legados de médicos, hospitais e outras entidades com queries sem `tenant_id`/`cliente_id`.

## Ausências funcionais relevantes

- Fluxo completo e real de criação/cópia/edição de perfil na Central de Segurança.
- Listagem e revogação individual de sessões com UX real.
- Consulta paginada de tentativas de login e acessos negados por escopo.
- Tela operacional para troca de contexto que renove token/cookie com os claims do tenant selecionado.
- Emissão real de token de impersonação; hoje há registro de sessão, mas não há troca completa da identidade efetiva.
- Modelo comercial único para upgrade, downgrade, prorrata, inadimplência e reativação.
- Enforcement sistemático de limites do plano em todos os comandos de criação.
- Cobertura tenant-aware dos serviços legados de operação e financeiro.
- Estratégia de cache/invalidação dos claims quando perfil, módulo ou contrato muda.

## Riscos técnicos e de regra de negócio

### Segurança e LGPD

- P0: qualquer query com `tenantId` opcional precisa diferenciar explicitamente “global autorizado” de “contexto ausente”. `null` não pode significar os dois estados.
- P0: sessão revogada precisa falhar no bearer antes de chegar ao controller.
- P0: endpoints globais devem exigir papel global, mesmo que o menu já os esconda.
- P1: e-mail, IP, user-agent e query string ainda aparecem em alguns logs legados; deve haver política uniforme de minimização/mascaramento.
- P1: mudança de contexto precisa produzir nova identidade efetiva; somente gravar a escolha no banco não altera claims já emitidos.
- P1: impersonação não está pronta para uso funcional até existir token derivado, expiração aplicada e banner Web permanente.

### Dapper e banco

- Há DTOs posicionais e classes mutáveis misturados. DTO usado diretamente no Dapper deve ter construtor compatível ou propriedades públicas com aliases exatos.
- Existem queries interpoladas em relatórios/ordenação. Algumas usam allowlist, mas todas devem permanecer sob revisão de injeção SQL.
- Foi encontrado `select *` em uma consulta dinâmica com tabela escolhida por código; mesmo com allowlist, isso fragiliza o contrato de materialização.
- A coexistência de `tenant_id` e `cliente_id` exige `coalesce(tenant_id, cliente_id)` nos pontos legados até a migração de dados ser concluída.
- Migrations repetem estruturas históricas em arquivos completos e incrementais; a ordem de instalação precisa permanecer idempotente e verificável.

### UX e design

- O login já atende o padrão premium e não deve ser redesenhado nesta rodada.
- A Central de Segurança possui várias views informativas que não consomem dados reais.
- Apenas 25 ocorrências de “Como usar esta tela” foram encontradas para 403 views; a adoção deve ser feita ao tocar cada jornada, não por texto genérico em massa.
- O menu tem separação clara de visão global, operação, saúde e financeiro, mas ainda contém links condicionados por papéis amplos; a autorização do servidor continua sendo a fonte de verdade.
- Não foram introduzidos nesta rodada links vazios, diálogos JavaScript nativos proibidos ou campos por ID técnico.

## Roadmap em blocos

### Bloco 1 — Fundação SaaS e Acesso (v2.15.0)

- Fechar login e sessão revogável.
- Garantir Super Admin realmente global e seleção auditada de tenant.
- Bloquear vazamentos nos endpoints legados de clientes/usuários.
- Aplicar tenant aos indicadores da Central de Segurança.
- Validar claims, menus, acesso negado e estados de login.

### Bloco 2 — Catálogo Comercial SaaS (v2.15.1)

- Consolidar módulos, planos, preços e limites já existentes.
- Definir agregado de assinatura e transições válidas.
- Implementar upgrade/downgrade, vigência e trilha comercial.
- Fazer o contrato ativo ser a única fonte dos módulos efetivos.

### Bloco 3 — Gestão do Cliente (v2.15.3)

- Finalizar CRUD real de perfis e matriz na Central de Segurança.
- Fechar onboarding, usuários locais, auditoria local e visão da contratação.
- Remover endpoints legados redundantes após migração de consumidores.

### Bloco 4 — Operação Assistencial (v2.15.4)

- Auditar e isolar médicos, hospitais, especialidades, plantões, escalas e convites.
- Consolidar confirmações, check-in/out, ocorrências e pendências.

### Bloco 5 — Financeiro (v2.15.2 para cobrança SaaS; sequência assistencial posterior)

- Fechar cobrança SaaS, vencimento, inadimplência, bloqueio e histórico.
- Depois consolidar pagamentos assistenciais, repasses, glosas e relatórios por tenant.

### Bloco 6 — Inteligência e Diferenciais (v2.15.5)

- Alertas de risco, recomendação de profissional, ranking e saúde operacional.
- Somente sobre dados reais, com explicabilidade, escopo e auditoria.

### Bloco 7 — Design Premium (evolução transversal)

- Aplicar cabeçalho, subtítulo, ações, estados, validação, responsividade e guia contextual em cada tela efetivamente evoluída.
- Remover progressivamente telas genéricas e respostas sem persistência.

## Critério de saída do Bloco 1

O Bloco 1 só pode ser considerado fechado quando: login cria uma sessão persistente; JWT expirado/revogado ou ligado a usuário/cliente bloqueado é recusado; Super Admin opera sem tenant obrigatório e pode escolher tenant ativo; administrador local nunca transforma contexto ausente em escopo global; endpoints legados de usuário e o catálogo de clientes respeitam sua fronteira; menu e guard continuam consumindo claims efetivos; e os contratos críticos têm testes automatizados.

## Sequência recomendada de sprints

- 2.15.1: catálogo de módulos e planos, assinatura, limites e transições comerciais.
- 2.15.2: cobrança SaaS, competência, vencimento, inadimplência, bloqueio e relatório global.
- 2.15.3: usuários e perfis do cliente, matriz real, onboarding e auditoria local.
- 2.15.4: operação assistencial tenant-aware ponta a ponta.
- 2.15.5: alertas e indicadores inteligentes sobre dados consolidados.
