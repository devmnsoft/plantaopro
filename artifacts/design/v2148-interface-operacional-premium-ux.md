# PlantãoPro v2.14.8 — interface operacional premium

## Escopo e princípios

Esta rodada refinou a experiência compartilhada do `PlantaoPro.Web` sem alterar regras de autenticação, autorização, persistência ou isolamento por tenant. Não foram adicionados dados simulados, credenciais, atalhos de segurança ou arquivos binários. A visibilidade dos links operacionais passou a consultar o serviço de permissões já registrado na aplicação; o servidor continua sendo a autoridade final.

## Inventário das telas auditadas

Todas as telas MVC usam `_Layout.cshtml` por `_ViewStart.cshtml`, exceto autenticação, que usa `_AuthLayout.cshtml`. O layout autenticado fornece barra de contexto, mensagens, cabeçalho contextual, guia “Como usar esta tela”, overlays, confirmação acessível e toasts.

| Tela | Rota principal | Diagnóstico de UX e responsividade | Ajuda, feedback e ação crítica |
|---|---|---|---|
| Login | `/Account/Login` | Composição premium existente, porém a grade de 12 colunas implícita quebrava o formulário e classes de ocultação não funcionavam sem utilitários locais. | Guia próprio, Caps Lock, sessão expirada, bloqueios, conexão e loading recuperável já existiam; estados foram corrigidos e a versão deixou de ser fixa. |
| Dashboard | `/Home/Dashboard` | Cards e filtros já usam o design system; largura e densidade agora recebem a camada responsiva v2.14.8. | Guia global do layout e mensagens compartilhadas. |
| Central Meu Dia | `/MeuDia` | Workspace real com prioridades; recebe contexto persistente e limite de conteúdo. | Guia específico pelo controller e estados reais. |
| Minha Central | `/MinhaCentral` | Cabeçalho próprio e cards operacionais; recebe contexto persistente e utilitários responsivos. | Jornada guiada, drawer de detalhe e guia do layout. |
| Central de Pendências | `/Pendencias` | Refinada na v2.14.6; foi revalidada contra a nova densidade e breakpoints. | Empty/error/loading e orientação específica já existentes. |
| Portal SaaS MNSOFT | `/AdminSaas/Dashboard` | Cockpit global já possuía mapa de módulos e guardrails; a nova barra torna o modo global persistente em todas as páginas. | Seleção de cliente é navegação real para Clientes; não foi criado seletor fictício. |
| Clientes/tenants | `/Clientes` | CRUD cru, tabela sem composição mobile e ações compactadas. Foi convertido em workspace com hero, KPIs, guardrails, tabela em cards e ações organizadas. | Suspender, reativar e cancelar usam o modal compartilhado com mensagem contextual e `aria-label`. |
| Usuários | `/Usuarios` | Estado honesto sem fonte de listagem real; recebe menu filtrado por permissão e contexto do tenant. | Empty state informa a limitação sem inventar registros. |
| Perfis | `/Perfis` | Tabela larga e cards ainda dependem da fonte existente. Recebe utilitários de grid e limite de largura. | Guia específico e navegação para permissões. |
| Permissões | `/Permissoes/Matriz` | Matriz naturalmente larga; permanece em scroll controlado, com navegação agora condicionada à permissão `SEGURANCA`. | Resultado do teste usa mensagens reais da action; autorização do servidor permanece obrigatória. |
| Escalas | `/Escalas` | Tabela extensa já possui versão em cards; foi revalidada com utilitários `d-lg-*` funcionais. | Estados vazios e drawer contextual existentes; ações indisponíveis permanecem explicitamente bloqueadas. |
| Plantões | `/Plantoes` | Lista, cards, kanban e filtros já responsivos; a camada v2.14.8 corrige grid e espaçamento em notebook. | Fluxos críticos existentes usam endpoints reais e confirmação compartilhada quando habilitados. |
| Profissionais | `/Medicos` | Formulário funcional, mas com pouca orientação e hierarquia. | Campos obrigatórios, máscara, autocomplete, ajuda de CRM/CPF/contato, feedback de salvamento e seleção de especialidade por nome. |
| Hospitais/unidades | `/Hospitais` | Formulário funcional, mas pouco contextualizado. | Ajuda para CNPJ, contato e efeito da inativação; rodapé de ação consistente. |
| Especialidades | `/Especialidades` | Formulário simples e desalinhado do padrão comum. | Formulário, validação, ajuda de campo e ações foram padronizados. |
| Financeiro | `/Financeiro` | Lista real já possui filtros, tabela e versão mobile. | Ações transacionais continuam nos endpoints existentes e no fluxo de confirmação. |
| Relatórios | `/Relatorios` | Catálogo responsivo já diferencia escopo e formatos. | Busca local, filtros, estado vazio/erro e aviso sobre dados sensíveis já presentes. |
| Notificações | `/Notificacoes` | Refinada na v2.14.6 e compatibilizada com utilitários responsivos. | Preferências e feedback permanecem nas rotas reais. |
| Auditoria | `/Auditoria` | Filtros em grid dependiam de utilitários ausentes e a tabela não tinha leitura mobile. Foi convertida em workspace com KPIs, filtros explicados, resumo ativo e tabela em cards. | Exportação preserva a query atual; status e detalhes têm rótulos acessíveis. |
| Configurações | `/Configuracoes` | Hub por domínio já evita formulário fictício; recebe contexto e grid responsivo. | Avisos de fonte indisponível e governança existentes. |
| Erro/404 | `/erro` e `/erro/{statusCode}` | Páginas genéricas e visualmente cruas. | Novos painéis oferecem retorno seguro para Minha Central e acesso à ajuda, sem expor detalhe técnico. |

## Alterações realizadas

### AppShell e contexto

- `_ContextBar.cshtml` deixou de ser placeholder e agora mostra usuário, perfil, tenant, modo global e acesso assistido.
- Super Admin recebe identidade visual “Contexto global MNSOFT”, indicação de auditoria e acesso real à lista de clientes.
- Quando há acesso assistido, o estado é visível e existe ação clara para encerrá-lo.
- O menu lateral passou a consultar `IPermissionService.HasPermission` para links de operação, Saúde 360, financeiro e gestão. Isso reduz atalhos indevidos para usuários comuns, sem substituir `[Authorize]` ou policies.
- Topbar, sidebar e conteúdo foram compactados para notebook e mantêm menu offcanvas/mobile já existente.

### Login

- A versão exibida agora vem de `AssemblyInformationalVersionAttribute` pelo partial compartilhado.
- Foi corrigida a grade do formulário, que criava doze colunas implícitas por conflito entre `.pp-form` e `.pp-form-field`.
- Estados `.d-none`, validação vazia, aviso de Caps Lock, spinner e demora ficam realmente ocultos até serem necessários.
- A composição foi validada em viewport desktop 1280×720 e mobile 390×844.
- O POST real, timeout recuperável, liberação do botão em erro/offline e mensagens de sessão/bloqueio continuam no fluxo existente; nenhum bypass ou login simulado foi criado.

### Componentes, formulários e tabelas

- Criada a camada `v2148-operational-premium.css`, carregada por último no AppShell e também no layout de autenticação.
- Consolidado um subconjunto autocontido de utilitários realmente usados pelas views: display responsivo, grid, espaçamento, alinhamento e conteúdo apenas visual.
- Adicionados padrões de barra de contexto, KPI operacional, painel, barra de filtros, ações de formulário, badges e página de erro.
- Clientes e Auditoria usam `data-mobile-cards`, captions, `data-label` e ações tocáveis.
- Os formulários de profissionais, unidades e especialidades receberam hierarquia, ajuda curta, autocomplete/inputmode, obrigatoriedade explícita e rodapé de salvamento consistente.

## Mensagens, modais e ícones

- As ações críticas tocadas continuam usando `data-confirm` e o modal acessível compartilhado; não foram introduzidos `alert()`, `confirm()` ou `prompt()`.
- Login, erro, empty state, toast e validação mantêm regiões `role="status"`/`role="alert"` e `aria-live` adequadas.
- Foram reutilizados `app-icon` e os SVGs já versionados. Nenhum PNG, JPG, WebP, ICO ou outro binário foi adicionado.

## Comandos executados e resultados

- `pwd`, `git status --short --branch`, `git remote -v`: repositório local confirmado; quatro arquivos preexistentes estavam modificados e foram preservados.
- `dotnet --info`, `dotnet --list-sdks`: SDKs 8, 9 e 10 disponíveis; build executado com .NET 10.
- Buscas `rg` de layout, navegação, login, tenant, permissões, IDs manuais, diálogos nativos, overflow e dimensões fixas: usadas para priorizar componentes reais.
- `git fetch origin --prune` e rebase sobre `origin/main`: concluídos com autostash; alterações locais preexistentes reaplicadas.
- `dotnet build backend/PlantaoPro.Web/PlantaoPro.Web.csproj -c Debug --no-restore`: concluído; apenas seis warnings de nulabilidade preexistentes.
- `dotnet clean backend/PlantaoPro.sln` e `dotnet restore backend/PlantaoPro.sln`: concluídos sem erros.
- `dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj -c Debug`: concluído com oito warnings preexistentes e zero erros.
- `dotnet build backend/PlantaoPro.sln -c Debug --no-restore`: concluído com nove warnings preexistentes e zero erros.
- `dotnet build backend/PlantaoPro.sln -c Release --no-restore`: concluído com dezessete warnings preexistentes e zero erros.
- `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build`: 319 de 327 testes aprovados. As oito falhas são contratos preexistentes fora deste diff: três detectam a senha literal presente nos `appsettings` locais já modificados pelo usuário, duas detectam padrão proibido em `FinanceiroPagamentoDetailsV2104Tests.cs`, e as demais apontam hash SQL desatualizado, texto esperado em Relatórios e validação de período da assinatura SaaS.
- Testes focados em `V148PremiumTemplateContractTests` e `Core_registration_models_reject_invalid_identifiers_and_statuses`: três de três aprovados, cobrindo links do menu, AppShell canônico e validação dos cadastros centrais tocados.
- QA visual local: login inspecionado em 1280×720 e 390×844; versão dinâmica, estados ocultos e composição responsiva confirmados.

## Limitações reais

- A inspeção visual autenticada de Clientes, Auditoria e AppShell exige uma sessão real e API/tenant válidos; não foram criadas credenciais ou dados falsos para contornar essa restrição.
- A action de Perfis já retorna uma coleção estática no código preexistente. Esta rodada não a ampliou nem a apresentou como fonte real; sua substituição requer integração de backend própria.
- Alguns avisos de nulabilidade já existem no projeto Web e não pertencem ao escopo visual.
- A suíte global permanece com oito falhas anteriores à entrega e fora do diff desta versão. Corrigi-las exigiria alterar configuração local do usuário e artefatos funcionais/contratuais não relacionados ao refinamento de interface.
- O `git diff --check` do working tree inclui espaço em branco no arquivo `UnitPortalV2100Tests.cs`, que já estava modificado pelo usuário antes desta rodada. A validação final do commit deve ser feita apenas nos arquivos desta entrega para não reescrever trabalho alheio.
