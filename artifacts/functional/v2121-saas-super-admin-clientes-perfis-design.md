# PlantãoPro v2.12.1 — SaaS, Super Admin, clientes, perfis e design guiado

## Diagnóstico e modo de execução

- **Modo usado:** `MODO DESIGN ESTÁTICO`.
- **SDK:** `dotnet` não está instalado (`dotnet: command not found`). Por isso, conforme a regra desta rodada, nenhum arquivo C#, projeto, solução, contrato, migration ou script de banco foi alterado.
- **Git:** a branch de trabalho é `codex/v2121-saas-super-admin-clientes-perfis-design`.
- **Remoto:** o checkout chegou sem remoto. `origin` foi configurado com a URL informada, porém o proxy recusou o acesso ao GitHub com HTTP 403; `fetch`, `pull`, `push` e a abertura do PR remoto não puderam ser concluídos.

## Entrega segura desta rodada

Esta rodada evolui somente a apresentação estática já conectada às rotas existentes. Não introduz mocks, indicadores fixos, permissões presumidas, campos sem contrato ou ações que contornem o servidor.

### Login e identidade

- O login informa explicitamente que o acesso hoje disponível usa **identidade individual por e-mail e senha**.
- A microcopy diferencia pessoa, contexto institucional e administração MNSOFT sem simular campos ainda não suportados pelo contrato.
- O CNPJ é descrito somente como identificador da instituição. Ele não é apresentado como usuário, senha ou credencial compartilhada.
- CPF/CNPJ contextual permanece pendente de implementação server-side. A interface não coleta nem transmite esses documentos enquanto não houver contrato seguro, normalização, validação, proteção LGPD e auditoria.

### Orientação de uso e design

Foi criado um padrão visual recolhível “Como usar esta tela”, responsivo e acessível com HTML nativo (`details`/`summary`), aplicado às jornadas estáticas prioritárias:

- Central administrativa SaaS;
- Clientes;
- Perfis e permissões;
- Faturamento SaaS.

As orientações explicam objetivo, sequência curta, efeito das ações sensíveis e dependência da autorização do servidor. Clientes e perfis também receberam tabelas com superfície premium e estados vazios com próximo passo claro. O login ganhou uma indicação visível do método efetivamente disponível.

## Modelo SaaS adotado como regra funcional

O desenho funcional a ser concluído em modo completo deve respeitar estas fronteiras:

1. **Super Administrador MNSOFT:** identidade global individual, sem `tenant_id`, acesso global por policies específicas, troca de contexto explícita, temporária e auditada. Bloqueios, suporte, planos, cobranças e liberações precisam produzir auditoria.
2. **Admin do Cliente:** identidade individual vinculada a exatamente um tenant no contexto ativo. Pode administrar usuários e perfis apenas desse tenant e dentro do plano. Não promove Super Admin, não altera o próprio plano nem remove bloqueio contratual.
3. **Usuário do Cliente:** CPF ou e-mail identifica a pessoa; CNPJ seleciona o contexto institucional quando necessário. CNPJ nunca autentica sozinho e nunca possui senha compartilhada.
4. **Isolamento:** toda leitura e escrita tenant-scoped deve receber o tenant do contexto autenticado e filtrá-lo no servidor. IDs enviados pelo navegador não definem nem ampliam o tenant.
5. **Entitlements:** acesso efetivo é a interseção entre plano, módulo, funcionalidade, perfil, status contratual, bloqueios e janela de teste/contrato. O menu apenas reflete essa decisão; a API continua sendo a autoridade.

## Persistência, permissões e módulos

- **Tabelas/scripts criados:** nenhum, pois o SDK está ausente e a rodada proíbe alterações de backend e banco nesse modo.
- **Tabelas reaproveitadas:** nenhuma alteração foi feita. O mapeamento definitivo deve evitar duplicar equivalentes existentes.
- **Permissões implementadas:** nenhuma policy ou permissão server-side foi criada neste modo. A UI reforça que ações dependem de autorização real.
- **Módulos controlados:** nenhum entitlement novo foi implementado. Permanecem pendentes as regras server-side para dashboard, escalas, plantões, médicos, unidades, financeiro, relatórios, ocorrências, notificações, auditoria, administração, Saúde360, Meu Dia, busca, favoritos e preferências.

## Pendências reais para `MODO COMPLETO`

1. Evoluir contratos e autenticação para CPF/e-mail e CNPJ contextual, com normalização, validação, mascaramento e testes, sem senha por CNPJ.
2. Confirmar ou implementar tenant global, vínculos de usuários, perfis tenant-scoped, planos, entitlements, bloqueios, cobranças e auditoria idempotente no PostgreSQL.
3. Aplicar filtro obrigatório por `tenant_id` nos repositórios Dapper e policies distintas para Super Admin e Admin do Cliente.
4. Implementar troca de contexto de suporte com expiração, justificativa, banner persistente e auditoria de início/fim.
5. Validar módulos e funcionalidades na API e projetar motivo de bloqueio seguro para a Web.
6. Cobrir os cenários de isolamento, promoção proibida, bloqueio, reativação, login contextual, entitlement e materialização Dapper solicitados nesta rodada.
7. Expandir o guia reutilizável às demais telas principais após validação visual em runtime.

## Validação e limitações

- `git diff --check` foi executado.
- As buscas de padrões proibidos e scripts de segurança/repositório foram executadas quando existentes; os resultados devem ser consultados no registro do agente/PR.
- Restore, builds e testes .NET não puderam ser executados porque não há SDK.
- Não foi possível iniciar a aplicação nem produzir screenshot confiável pelo mesmo motivo. A validação visual ficou limitada à revisão estática de Razor/CSS e deve ser repetida em ambiente com `net10.0`.
- O bloqueio HTTP 403 do proxy impede publicar a branch e abrir o PR remoto neste checkout.
