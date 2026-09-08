# PlantãoPro v2.14.9 — núcleo SaaS, módulos, perfis e menus

## Objetivo e decisão arquitetural

A v2.14.9 consolida o modelo canônico já existente (`clientes`, `tenants`, `planos`, `assinaturas`, `modulos_sistema`, `tenant_modulos`, `perfis`, `permissoes`, `usuarios_perfis` e auditoria). A implementação não cria um terceiro modelo paralelo e não executa DDL durante requisições. As tabelas legadas prefixadas por `saas_` permanecem somente por compatibilidade e deverão ser migradas e removidas em uma evolução controlada.

O Super Administrador MNSOFT usa perfil global, sem tenant obrigatório. Usuários de cliente permanecem vinculados ao próprio tenant; perfil, permissão e contrato são calculados no login e validados novamente por serviços de autorização.

## Inventário inicial

| Área | Situação encontrada | Tratamento nesta rodada |
|---|---|---|
| Clientes/tenants | Persistência, listagem, status, onboarding e isolamento já existiam | Reutilizados; catálogo ganhou acesso direto por cliente |
| Planos/assinaturas | CRUDs Web/API reais e guardas de uso existentes | Reutilizados; criado vínculo persistente plano–módulo |
| Cobranças | Faturas SaaS, geração e indicadores já existentes | Reutilizados no menu global; automação recorrente fica para sprint futura |
| Módulos | Tela e API usavam catálogo estático em memória | Substituídos por catálogo PostgreSQL, preço, limite, essencial/opcional e vínculo por tenant |
| Perfis/permissões | Serviço persistia dados, mas Web era estática e consultas de detalhe não isolavam tenant | Web conectada à API; detalhe, catálogo e alteração de matriz endurecidos por tenant/contrato |
| Usuários | Central de segurança listava banco, mas tela Web era placeholder e ações de status eram respostas vazias | Listagem Web real; bloqueio/desbloqueio persistente, revogação de sessão e proteção do último Super Admin |
| Login | Campo prometia e-mail/CPF/CNPJ, validação aceitava apenas e-mail e API consultava somente e-mail | Identificação real, normalização, resposta não enumerável, lockout, contexto e claims efetivas |
| Menus | Maioria dependia apenas de roles; módulo `BI_AVANCADO` era exceção fixa | Claims de módulo/permissão passaram a controlar visibilidade e acesso |
| Super Admin | Dashboards e contexto assistido existiam, porém o redirect apontava para portal parcial | Redirect e menu apontam para o dashboard SaaS real; operações comerciais críticas são globais |
| Auditoria/LGPD | Serviço central e telas já existiam | Alterações de catálogo, contrato, perfil, login e usuário registram contexto e antes/depois quando aplicável |

## Regras implementadas

### Autenticação

- Um único campo aceita e-mail, CPF ou CNPJ e normaliza documentos sem máscara.
- E-mail e CPF identificam usuário individual. CNPJ só prossegue quando resolve um único usuário; em instituições com múltiplos usuários a resposta orienta uso de e-mail/CPF sem revelar cadastros.
- Não existe senha compartilhada de cliente. A senha é verificada por usuário candidato.
- Identificadores não são gravados em log/auditoria; registra-se tipo e fingerprint SHA-256 truncado.
- Senhas legadas válidas são migradas para BCrypt no acesso.
- Lockout por janela/tentativas foi preservado.
- Usuário inativo/bloqueado e instituição inativa/bloqueada recebem mensagens próprias após credencial válida.
- JWT e cookie Web recebem `permission`, `module` e `access_catalog_version=v2149`.
- O login global direciona para `SaasDashboard/Index`.

### Permissões, contrato e menu

- Códigos `MODULO.ACAO` e o formato legado `MODULO:ACAO` são normalizados.
- Overrides negativos de usuário prevalecem sobre concessões do perfil.
- Perfil inativo, vínculo inativo e permissão bloqueada pelo plano não concedem acesso.
- Módulo operacional precisa estar habilitado em `tenant_modulos`; módulos comuns e de administração do tenant têm tratamento explícito.
- Super Admin global recebe curingas de módulo e permissão.
- Sessões v2.14.9 usam somente o catálogo efetivo. O fallback de roles fica restrito a cookies antigos e desaparece após novo login.
- O menu foi separado em área global, catálogo/planos/assinaturas/cobranças, operação, clínica, financeiro e gestão, sem exibir chaves técnicas ao usuário.

### Catálogo comercial de módulos

- Cadastro e edição persistentes com código, nome, descrição, categoria, preço-base, status, recursos, limite e marcador essencial.
- Vínculo módulo–plano em `plano_modulos`.
- Vínculo módulo–cliente em `tenant_modulos`, com preço e limite contratados.
- Habilitar/bloquear módulo é exclusivo do Super Admin e exige motivo.
- Cada mudança de contrato grava histórico antes/depois e auditoria global.
- Cliente visualiza o próprio contrato, mas não altera preço nem habilita módulo.
- A migration `2026_v2149_saas_core_modulos_perfis_menus.sql` é idempotente, cria índices, constraints e faz backfill de códigos legados.

### Usuários e perfis

- Listagens usam `coalesce(tenant_id, cliente_id)` para instalações em transição e mantêm o escopo do usuário atual.
- Cadastro e edição de usuário persistem identidade, telefone e perfis do mesmo tenant; a criação exige senha temporária forte e força a troca no primeiro acesso.
- Alterar dados, senha temporária ou perfis revoga sessões anteriores, e perfis globais não podem ser atribuídos pelo fluxo de cliente.
- A matriz é agrupada por módulo, mostra descrição e badge de permissão crítica.
- Só permissões de módulos contratados são oferecidas e aceitas pelo backend.
- Perfis base são visíveis, porém protegidos contra edição/inativação pelo cliente.
- Alterações de matriz são transacionais e auditam permissões adicionadas/removidas.
- Bloqueio/inativação de usuário revoga sessões; o último Super Admin global ativo não pode ser bloqueado.

## Telas alteradas

- Login e mensagens de sessão.
- Sidebar global e menus condicionais.
- Clientes, com acesso à contratação de módulos.
- Catálogo, formulário, detalhe e módulos por cliente.
- Usuários reais por tenant, pesquisa e ações críticas.
- Perfis, formulário e matriz real de permissões.
- O dashboard SaaS, planos, assinaturas, cobranças e auditoria existentes foram conectados pelo menu, sem duplicar tela ou regra.

Todas as telas novas/tocadas possuem cabeçalho, orientação “Como usar”, estados vazios/erro, status, layout responsivo e confirmação acessível via modal compartilhado para ações críticas. Formulários usam labels, validação, ajuda contextual, antiforgery e estado de envio.

## Pendências encontradas e limite desta entrega

- `CommercialDemoService` ainda contém estruturas em memória para landing, leads, propostas, feature flags e modo demonstração. O catálogo de módulos deixou de usá-las; a migração desses outros fluxos não foi incluída para evitar expansão não relacionada.
- A Central de Segurança ainda possui contratos reservados/sem persistência para o painel agregado de sessões e tentativas. Cadastro, edição, atribuição de perfis, listagem, bloqueio/desbloqueio e revogação administrativa foram fechados.
- Há duplicidade histórica entre tabelas canônicas e tabelas `saas_*`. Nenhum dado foi removido nesta rodada.
- Trocas de contrato afetam claims no próximo login. Invalidação imediata de todos os cookies distribuídos depende de um store de sessão centralizado.
- A migration foi criada, mas não executada automaticamente. Deve passar pelo pipeline operacional de banco.
- Validação integrada Web → API → PostgreSQL exige uma base configurada e credenciais reais do ambiente; Swagger 200 não foi considerado prova de login.

## Futuras sprints

1. **Comercial SaaS:** proposta por cliente, upgrade/downgrade, histórico comercial e persistência dos fluxos ainda em memória.
2. **Cobrança:** recorrência, vencimento, inadimplência, bloqueio automático e relatórios financeiros SaaS.
3. **Marketplace de módulos:** catálogo público/contratado, solicitação, aprovação comercial e ativação assistida.
4. **Governança global:** auditoria avançada, trilha consolidada, relatórios e saúde operacional por tenant.
5. **Experiência do cliente:** onboarding, checklist de implantação, suporte, central de ajuda e tour guiado.
6. **Identidade:** MFA, convites por e-mail, recuperação administrativa sem senha transitória e store central de sessões.
7. **Dados:** migrar `saas_*` para o modelo canônico, reconciliar registros e remover estruturas obsoletas somente após auditoria.

## Validação técnica

Executados durante o desenvolvimento:

- diagnóstico Git/remotos/SDKs e inventário com `rg`;
- scans de SQL/Dapper, tenant, Razor/JavaScript e marcadores incompletos;
- `dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj -c Debug --no-restore` — sucesso;
- `dotnet build backend/PlantaoPro.Web/PlantaoPro.Web.csproj -c Debug --no-restore` — sucesso;
- `dotnet build backend/PlantaoPro.sln -c Debug --no-restore` — sucesso;
- `dotnet build backend/PlantaoPro.sln -c Release --no-restore` — sucesso;
- `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build --filter FullyQualifiedName~V2149SaasCoreContractTests` — 13/13 aprovados;
- suíte Release completa — 332 aprovados e 8 falharam por pendências anteriores à entrega: secrets nos `appsettings` locais já alterados, checksums sensíveis a CRLF do checkout Windows e contratos legados de relatório/formulário/padrões proibidos;
- testes contratuais v2.14.9 cobrem normalização do login, DTO Dapper, claims e autorização global de mutações.

Os scans obrigatórios dos arquivos desta entrega não encontraram secret, mock, bypass, padrão de UI proibido ou corrupção textual. Avisos preexistentes do compilador/analisadores foram preservados e não impedem o build.
