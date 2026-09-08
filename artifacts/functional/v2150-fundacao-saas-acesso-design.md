# PlantãoPro v2.15.0 — fundação SaaS, acesso e design

Data: 2026-09-08

Branch: `codex/v2150-auditoria-saas-roadmap-implementacao-segura`

## Resultado da rodada

A fundação de acesso foi endurecida sem ampliar o escopo comercial. A aplicação agora registra cada login bem-sucedido em `auth_sessoes` e valida essa sessão depois da validação criptográfica do JWT. Revogação, expiração, usuário inativo e cliente bloqueado passam a invalidar o bearer antes de qualquer controller protegido.

Também foram fechados vazamentos multi-tenant em endpoints legados, corrigido o escopo do dashboard de segurança e liberada a visão de tenants ativos para o Super Admin sem exigir vínculos artificiais. O catálogo de módulos/planos existente permanece como preparação do Bloco 2.

## Regras efetivas

### Super Admin MNSOFT

- Autentica com perfil global e recebe `access_scope=GLOBAL` e `context_mode=GLOBAL`.
- Não recebe `tenant_id` nem `cliente_id` herdado de vínculo legado no token inicial.
- Não é bloqueado pelo estado de um cliente ao qual seu cadastro histórico esteja associado.
- Recebe permissões e módulos globais.
- Pode listar todos os tenants ativos ligados a clientes ativos.
- Pode selecionar qualquer tenant ativo; a seleção registra sessão de contexto, motivo, histórico e recente.
- É o único perfil autorizado no catálogo global `api/clientes`.

### Administrador do cliente

- Precisa ter tenant no claim; ausência de contexto nunca é convertida em escopo global.
- Lista, detalha, altera status e revoga sessões somente de usuários do próprio tenant nos fluxos de segurança.
- O endpoint legado de usuários aplica `coalesce(tenant_id, cliente_id)` na listagem e no desbloqueio.
- Indicadores de usuários, falhas de login e sessões são calculados somente dentro do tenant.
- Não acessa o catálogo global de clientes.

### Usuário comum

- Precisa de usuário, cliente e perfil ativos.
- Recebe no login apenas permissões efetivas e módulos contratados.
- O menu Web e o guard de rotas usam os claims de módulo/permissão da sessão v2.14.9.
- Módulo não contratado ou ação não concedida permanece oculto e negado no servidor.
- Sessão expirada/revogada ou bloqueio posterior do usuário/cliente invalida chamadas protegidas.

## Login

- Identificadores aceitos: e-mail, CPF e CNPJ normalizados.
- O CNPJ somente autentica quando identifica exatamente um usuário; cenários ambíguos exigem identificação individual.
- A API usa a mesma mensagem genérica para identificador inexistente, ambíguo ou senha inválida, evitando enumeração.
- Lockout, usuário inativo e cliente bloqueado continuam com tratamento próprio após credencial válida.
- Senha compartilhada por empresa não foi criada e nenhum bypass foi adicionado.
- Cada sucesso cria um UUID de sessão, guarda validade de oito horas e minimiza IP/user-agent persistidos.
- O JWT é validado por assinatura, issuer, audience, tempo e estado da sessão persistida.

## Menus por módulo e perfil

- A separação atual permanece: visão geral, operação médica, Saúde 360, financeiro, gestão e modo Global MNSOFT.
- Super Admin visualiza as áreas globais.
- Administrador local visualiza administração básica e apenas módulos contratados.
- Usuário comum depende de `module` e `permission` no cookie emitido a partir da resposta segura da API.
- O guard de rotas é a segunda barreira Web; a autorização da API continua sendo a fonte de verdade.
- Alterações de perfil ou contratação exigem nova emissão de claims; invalidação/cache dinâmico fica para a próxima evolução de segurança.

## Sessão e auditoria

- `AuthenticationSessionService` cria e valida sessões reais em PostgreSQL.
- A validação recusa sessão ausente, de outro usuário, inativa, revogada, expirada, com usuário bloqueado ou cliente inativo.
- `ultimo_uso_em` é atualizado com janela mínima de um minuto para evitar escrita a cada request sucessivo.
- Desbloqueio legado agora registra auditoria central com tenant alvo e indicação de escopo global/local.
- Troca de contexto continua transacional e registra `contexto_sessoes`, `contexto_trocas` e `usuario_contextos_recentes`.
- Logs estruturados passaram a persistir fingerprint de e-mail/IP e omitir query strings com documento ou e-mail.

## Design e experiência

Nenhuma tela foi redesenhada nesta rodada porque a correção foi concentrada no limite de confiança do servidor. A tela de login auditada já contém identidade PlantãoPro/MNSOFT, labels visíveis, validação, Caps Lock, recuperação, estados de erro/conexão, botão submit real, responsividade e o bloco “Como usar esta tela”. O menu já apresenta grupos funcionais sem expor chaves técnicas.

Não foram adicionadas telas, links vazios, diálogos nativos, placeholders no lugar de labels ou campos que peçam IDs técnicos. As telas genéricas da Central de Segurança foram documentadas como pendência e não são declaradas prontas.

## Bugs corrigidos

1. Sessões emitidas sem registro persistente.
2. JWT continuava aceito após revogação administrativa.
3. Bloqueio posterior de usuário/cliente não invalidava o bearer.
4. Super Admin herdava tenant legado no login.
5. Super Admin dependia de `usuario_tenant_acessos` para entrar em tenant ativo.
6. Administrador local listava usuários de outros tenants pelo endpoint legado.
7. Administrador local podia desbloquear usuário de outro tenant.
8. Administrador local acessava e alterava o catálogo global de clientes.
9. Dashboard local somava tentativas e sessões de todos os tenants.
10. Contexto de tenant ausente era indistinguível de escopo global em serviços administrativos.
11. Resposta específica de CNPJ permitia inferir cenário institucional.
12. Logs estruturados mantinham e-mail e IP em claro.
13. `TenantDisponivelDto.ClienteId` não representava corretamente o valor nulo aceito pelo banco.

## Testes adicionados

O conjunto `V2150SaasAccessFoundationTests` cobre:

- sessão ativa;
- sessão expirada;
- sessão revogada;
- usuário bloqueado;
- cliente bloqueado;
- DTO Dapper materializável;
- hook de validação da sessão no bearer;
- criação de sessão no login;
- Super Admin sem tenant inicial;
- resposta não enumerável do CNPJ;
- catálogo de clientes exclusivo do global;
- papéis aceitos no endpoint legado de usuários;
- isolamento da lista e do desbloqueio;
- dashboard de segurança por tenant;
- listagem/seleção global de tenant com auditoria;
- continuidade do filtro de menu por módulo e perfil;
- estados críticos e submit real da tela de login.

Somados aos contratos v2.14.9, foram executados 29 testes focados com 29 aprovações.

## Validações executadas

| Comando | Resultado |
|---|---|
| `dotnet clean backend/PlantaoPro.sln` | aprovado |
| `dotnet restore backend/PlantaoPro.sln` | aprovado |
| build API Debug | aprovado, zero erros |
| build solução Debug sem restore | aprovado, zero erros |
| build solução Release sem restore | aprovado, zero erros |
| testes focados v2.14.9 + v2.15.0 em Release | 29/29 aprovados |
| suíte completa Release | 348 aprovados, 8 falhas preexistentes/fora do diff |
| compatibilidade C# 10/CSS Razor | aprovado |
| integridade do script completo | aprovado, cobertura 100% |
| segurança do repositório | bloqueado por credenciais/flags nos `appsettings` já modificados pelo usuário |
| `git diff --check` global | bloqueado por whitespace em `UnitPortalV2100Tests.cs`, já modificado pelo usuário |
| `git diff --check` excluindo os quatro arquivos do usuário | aprovado |
| padrões proibidos nos arquivos desta entrega | nenhum |

As oito falhas da suíte completa são: duas varreduras que encontram texto proibido em teste legado; uma divergência de checksum sensível a conteúdo/normalização; uma expectativa antiga de relatório; uma validação antiga de assinatura; e três falhas causadas pelas credenciais locais presentes nos `appsettings` do usuário. Nenhum dos quatro arquivos locais foi editado ou incluído na entrega.

## Pendências reais

- Central de Segurança: substituir respostas estáticas de sessões, tentativas, auditoria e endpoints duplicados de perfil por um único fluxo persistente.
- Troca de contexto: emitir novo JWT/cookie com claims do tenant selecionado e oferecer UI com banner de contexto.
- Impersonação: emitir identidade derivada, aplicar expiração e exibir banner obrigatório antes de liberar uso funcional.
- Sessão Web: sincronizar imediatamente o cookie local quando a sessão API for revogada.
- Perfis: eliminar duplicidade entre `api/seguranca`, `api/perfis` e `api/permissoes`.
- Operação legada: aplicar tenant a médicos, hospitais, especialidades, plantões, escalas e pagamentos que ainda usam queries globais.
- Observabilidade: definir retenção, chave/HMAC para pseudonimização e acesso por necessidade.
- UX: evoluir cada tela genérica somente junto de dados reais, com estados e guia contextual.

## Próximos blocos recomendados

- Sprint 2.15.1 — catálogo de módulos e planos: consolidar fonte de verdade, preço, limite, assinatura, upgrade/downgrade e status comercial.
- Sprint 2.15.2 — cobrança SaaS: competência, vencimento, inadimplência, bloqueio/desbloqueio e histórico global.
- Sprint 2.15.3 — cliente e perfis: CRUD/matriz únicos, onboarding, usuários locais e auditoria local.
- Sprint 2.15.4 — operação assistencial: isolamento tenant ponta a ponta e fechamento das jornadas reais.
- Sprint 2.15.5 — diferenciais inteligentes: risco, recomendação e indicadores explicáveis sobre dados consolidados.

O detalhamento do inventário e da priorização está em `artifacts/analysis/v2150-inventario-saas-plantaopro.md`.
