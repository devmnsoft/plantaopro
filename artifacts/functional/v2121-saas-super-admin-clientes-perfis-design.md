# v2.12.1 — SaaS, Super Admin, clientes, perfis e design guiado

## Modo e diagnóstico

- **Modo usado:** MODO DESIGN ESTÁTICO.
- **SDK:** `dotnet`, `dotnet --info` e `dotnet --list-sdks` indisponíveis (`command not found`).
- **Git:** repositório local íntegro, branch `codex/v2121-saas-super-admin-clientes-perfis-design` criada a partir de `work`.
- **Remoto:** o checkout chegou sem remoto. `origin` foi configurado com a URL informada na tarefa, mas o proxy recusou a conexão ao GitHub com HTTP 403; `fetch`, `pull` e `push` não puderam ser concluídos.
- Em cumprimento à restrição da rodada, não foram alterados C#, banco, migrations, contratos, projetos, solution, `TargetFramework` ou `LangVersion`.

## Entrega segura desta rodada

- O login ganhou linguagem explícita de identidade individual e três contextos visuais: profissional/gestor, instituição e administração MNSOFT.
- O contexto institucional esclarece que o CNPJ identifica a organização, mas não substitui a identidade da pessoa nem autoriza senha compartilhada.
- A ajuda recolhível orienta o acesso atual por e-mail e informa como solicitar CPF/contexto corporativo sem simular uma função ainda não suportada pelo contrato existente.
- O componente reutilizável de introdução passou a identificar suas etapas como **Como usar esta tela**, levando orientação curta e recolhível a todas as páginas que já o consomem.
- Uma camada CSS v2.12.1 adiciona acabamento premium aos contextos do login e ao painel de orientação, com foco visível herdado, responsividade, redução de conteúdo secundário no mobile e suporte a cores forçadas.

## Modelo SaaS adotado para implementação completa

### Escopos

1. **Super Admin MNSOFT:** identidade global, sem `tenant_id`, com troca de contexto explícita, auditável e temporária. Pode administrar tenants, planos, módulos, cobranças e bloqueios, sem transformar a sessão de suporte em autoria do usuário do cliente.
2. **Admin do Cliente:** identidade vinculada a exatamente um tenant em cada contexto ativo. Administra usuários e perfis somente dentro dos módulos e limites contratados. Não altera plano, inadimplência, permissões globais ou o próprio escopo.
3. **Usuário do Cliente:** identidade individual com perfil mínimo e permissões efetivas calculadas pela interseção entre perfil, plano, módulo, funcionalidade e situação contratual.

### Isolamento e autorização

- Toda leitura e escrita de domínio deve receber `tenant_id` do contexto autenticado, nunca do corpo livre da requisição.
- O escopo global deve ser concedido somente à função interna de Super Admin e registrado em auditoria.
- O backend deve negar o acesso quando qualquer camada aplicável estiver bloqueada, mesmo quando o menu também esconder o recurso.
- A permissão efetiva deve considerar plano, módulos habilitados, recurso liberado, bloqueio manual, inadimplência, segurança, teste e expiração.
- Bloqueios, desbloqueios, concessões temporárias, mudança de plano e suporte em contexto devem produzir auditoria com ator, tenant, motivo e instante.

## Login CPF, CNPJ e e-mail

- **Pessoa física:** CPF normalizado ou e-mail + senha individual.
- **Contexto corporativo:** CNPJ normalizado identifica o tenant, seguido de CPF/e-mail e senha da pessoa.
- **Super Admin:** e-mail administrativo MNSOFT + senha individual e MFA quando o suporte existente for confirmado.
- CPF/CNPJ devem ser normalizados no servidor, validados pelo padrão do projeto e mascarados fora de operações estritamente necessárias.
- CNPJ jamais será credencial ou login compartilhado; senha não será persistida em texto puro nem registrada em log.
- A tela atual continua enviando apenas o contrato existente de e-mail e senha. Campos funcionais de CPF/CNPJ dependem de evolução coordenada do contrato e não foram simulados sem SDK.

## Estrutura de dados e scripts

Nenhuma tabela ou migration foi criada neste modo. O diagnóstico encontrou estruturas SaaS e referências a `tenant_id` já existentes; no modo completo elas devem ser inventariadas antes de qualquer script para impedir duplicidade. A evolução deverá reaproveitar equivalentes existentes para tenants/clientes, usuários, perfis, permissões, planos, módulos, cobranças, bloqueios e auditoria, usando SQL idempotente e parametrizado.

## Permissões e módulos

O desenho funcional cobre dashboard, escalas, plantões, médicos, unidades, financeiro, relatórios, ocorrências, notificações, auditoria, administração, Saúde360, Central Meu Dia, busca, favoritos e preferências. A implementação server-side e os testes de interseção de permissões permanecem pendentes porque exigem alterações C#, contratos e banco.

## Telas e orientação

- **Login:** hierarquia e microcopy SaaS, explicação dos contextos e orientação de segurança.
- **Telas com `_PageIntroduction`:** título padronizado “Como usar esta tela”, mantendo conteúdo curto, recolhível e acessível.
- Dashboard global, clientes, área administrativa do cliente, usuários, perfis, planos, módulos, cobranças, bloqueios e auditoria não receberam fluxos falsos ou dados fixos. Sua implementação real permanece condicionada ao modo completo.

## Testes e verificações

- Diagnóstico de Git, remoto e SDK executado.
- Busca de arquitetura/autorização executada nos três projetos solicitados.
- `git diff --check` executado sem erros.
- Build e testes .NET não executáveis porque o SDK não existe no ambiente.
- Homologação visual por screenshot não executável porque a aplicação Razor requer o runtime ausente.

## Limitações reais restantes

1. Evoluir contratos e autenticação para CPF/e-mail + CNPJ como contexto, com validação, normalização e LGPD.
2. Validar e completar o isolamento server-side por tenant em todos os repositórios/serviços.
3. Implementar políticas de Super Admin, Admin do Cliente e permissão efetiva por entitlement.
4. Implementar gestão real de clientes, perfis, planos, módulos, cobranças, bloqueios e auditorias.
5. Criar scripts idempotentes somente após inventário completo do schema existente.
6. Adicionar os testes de integração e autorização definidos no escopo e executar restore/build/test com .NET 10.
7. Executar smoke visual responsivo e capturar screenshots quando houver runtime.
8. Publicar a branch e abrir o PR quando o proxy permitir acesso ao GitHub e houver autenticação do GitHub CLI.
