# Evolução da interface administrativa — verificação de 2026-09-14

## Escopo e baseline

- Baseline auditada: `f6e303d`, branch inicialmente limpa. Não havia `AGENTS.md` no repositório nem nos diretórios superiores localizados.
- A auditoria foi concentrada na jornada **login → contexto → central de clientes → módulos → equipe/perfis → autorização efetiva**. Módulos clínicos e financeiros não foram ampliados.
- Os commits v2.16.4/v2.16.5 foram tratados como código a verificar, não como comprovação de homologação. Histórico, workflows, relatórios e fontes foram confrontados.

## CS0122 e compatibilidade C# 10

`V2160ClinicalJourneyContractTests` já chegou à baseline usando `RepositoryPathResolver.RepoRoot`. O helper mantém `Root` como `private static readonly Lazy<string>` e oferece `RepoRoot` como `string`. A busca em `backend` não encontrou outro acesso externo a `RepositoryPathResolver.Root`.

Foi acrescentado um contrato de regressão que lê a fonte sem reflection: ele preserva o encapsulamento do helper e falha se um teste voltar a acessar `Root`. A string SQL do catálogo continua sendo literal verbatim C# 10 (`@"..."`) com aspas duplicadas em `""DependenciasArray""`; não foi convertida em raw string.

## Matriz de capacidades observadas

| Capacidade | Classificação nesta auditoria | Evidência objetiva | Limite conhecido |
|---|---|---|---|
| Login Web/API | Implementada, sem validação runtime nesta rodada | `Account/Login.cshtml`, `auth-login.js`, `AccountController` e `AuthController`; POST, antiforgery, bloqueio de duplo envio e recuperação por timeout/pageshow | SDK e aplicação indisponíveis para repetir o E2E |
| Seleção/revalidação de contexto | Implementada, sem validação runtime nesta rodada | `ContextoController`, `ContextSelectionService`, `EffectiveAccessAuthorization` | Troca entre dois clientes e resposta atrasada exigem runtime |
| Administração global de clientes | Implementada, sem validação runtime nesta rodada | endpoint `api/clientes/central`, consulta paginada/agregada e MVC `Clientes/Index` | indicadores dependem dos registros persistidos disponíveis |
| Detalhe do cliente | Parcial | abas e rotas existentes para organização, módulos, equipe, cobranças e histórico | nem toda aba constitui uma jornada homogênea dentro do detalhe |
| Contratação de módulos | Implementada e reforçada por contrato estático | revisão versionada, idempotency key, decisão sob lock e snapshot comercial em `ModuleContractingService` | concorrência e banco real continuam gates de integração |
| Dependências de módulos | Implementada nesta rodada | revisão aceita dependência já `ATIVO`; `SUSPENSO` e `AGENDADO` não liberam operação | requer cenário PostgreSQL para prova integrada |
| Onboarding | Parcial | criação transacional e checklist existentes | convite individual canônico e sua entrega não formam fluxo integral validado |
| Usuários e vínculos | Parcial | administração e `usuario_tenant_acessos` separam acessos por tenant | telas e estados de convite não estão concluídos ponta a ponta |
| Perfis/permissões | Implementada, sem validação runtime nesta rodada | perfil local, permissões efetivas, contrato de módulo e negações especiais no servidor | último administrador e revogação em sessão aberta precisam do banco/E2E |
| Convite individual/aceite | Ausente como jornada administrativa canônica completa | tabelas legadas existem, mas não foi comprovado um único fluxo de expiração, revogação, uso único e aceite concorrente | não se afirma envio de e-mail nem conclusão |

## Correção funcional adicional

A revisão de contratação anteriormente exigia que toda dependência estivesse no mesmo pedido, mesmo quando ela já estava ativa para o cliente. Isso induzia recompra desnecessária e contradizia a distinção entre catálogo e contrato vigente. A revisão agora usa uma única fotografia do catálogo: dependência selecionada ou já `ATIVO` satisfaz a regra; estados suspenso/agendado continuam bloqueando a operação. A proteção que impede contratar novamente um módulo ativo ou suspenso foi mantida.

## Segurança e invariantes verificados em fonte

- CPF/CNPJ/e-mail permanecem identificadores; autorização deriva de identidade ativa, vínculo, contexto, contrato e permissão efetiva.
- Administração local não recebe papel global pelo layout. Endpoints administrativos continuam protegidos no servidor.
- A central global possui escopo explícito e a alteração de situação exige motivo, lock e auditoria.
- O catálogo distingue disponibilidade comercial de estado contratual. Solicitações guardam versão, preço e condições; a chave de idempotência possui tratamento de concorrência.
- Nenhuma credencial padrão, permissão automática, telemetria fictícia, `alert()`, `confirm()` ou `href="#"` foi adicionada.

## Evidência visual disponível

Não houve alteração visual nesta rodada; por isso não foi fabricada uma nova captura sem aplicação executável. As capturas reais versionadas da baseline permanecem em `artifacts/screenshots/login-desktop.png`, `login-mobile.png`, `contexto-global.png`, `contexto-tenant.png`, `dashboard-desktop.png` e `dashboard-mobile.png`. Elas são evidência histórica, não homologação visual deste commit.

## Validações e limitações

- Disponíveis localmente: buscas com `rg`, validação C# 10 por script, verificações estáticas da experiência, higiene do diff e inspeção de fontes/histórico.
- Bloqueio ambiental: `dotnet` não está instalado. Portanto restore, builds Debug/Release, `V2160ClinicalJourneyContractTests` e a suíte não foram executados localmente.
- Não há PostgreSQL/aplicação iniciada neste executor. Como nenhum SQL foi alterado, instalação limpa/upgrade não eram necessários ao diff; os cenários com dois clientes, payload adulterado, vínculo bloqueado, contrato suspenso, duplo envio, concorrência, limite, conflito e revogação durante sessão continuam obrigatórios no CI/homologação.
- Esta é uma revisão dirigida às áreas listadas, não uma revisão integral do sistema e não uma promessa de ausência de bugs.
