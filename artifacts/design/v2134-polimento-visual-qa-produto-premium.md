# PlantãoPro v2.13.4 — polimento visual e QA premium

## Escopo e pré-validação

A rodada foi preparada na branch `codex/v2134-polimento-visual-qa-produto-premium`. A árvore inicial estava limpa e sem remoto Git configurado; o `origin` foi adicionado a partir da URL fornecida, mas o acesso de rede foi recusado pelo proxy. O SDK .NET não está instalado (`dotnet: command not found`). Por isso, o trabalho ficou estritamente em Razor, CSS, JavaScript e documentação: não houve alteração de C#, autenticação server-side, serviços, banco, migrations, solução ou projetos.

Foram inventariadas as árvores de `Views`, `Pages` e `wwwroot` do Web/API e localizados os fluxos de login, dashboards, usuários, perfis, clientes, médicos, hospitais, escalas, plantões, financeiro, relatórios, configurações, auditoria e notificações. Os contratos existentes de autorização, tenant, antiforgery e POST real foram preservados.

## Matriz de QA visual

| Tela | Módulo | Perfil permitido | Normal | Vazio | Carregando | Erro | Formulário | Mensagens | Responsividade | Ajuda contextual | Problema visual | Correção feita |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Login | Acesso | Não autenticado | Identidade PlantãoPro/MNSOFT e benefícios | Não aplicável | Botão com spinner e texto transitório | Resumo seguro, focável e `aria-live` | Labels, autocomplete, ajuda, senha visível/oculta e Caps Lock | Sem expor motivo sensível | Shell único no mobile; campos e texto sem corte | “Como acessar com segurança” | Copy divergente, acabamento sem redução explícita de movimento e versão anterior | Copy comercial alinhada, v2.13.4, animação discreta com `prefers-reduced-motion` |
| Shell / topbar / sidebar | Navegação | Todos os autenticados conforme roles | Contexto, breadcrumb, busca e módulo ativo | Não aplicável | Estados dos conteúdos internos | Toasts e páginas de erro existentes | Não aplicável | Região viva e toasts globais | Menu mobile, sidebar recolhível e ações condensadas | Guia global “Como usar esta tela” | Áreas globais misturadas à gestão do tenant | Grupo “MNSOFT global” exclusivo do Super Admin; transições, foco e espaçamento mobile refinados |
| Minha Central / Meu Dia | Produtividade | Todo perfil autenticado conforme autorização | Prioridades pessoais e operacionais | Componentes de estado existentes | Componentes de carregamento existentes | Feedback global existente | Ações vêm dos fluxos reais | Toast após operações | Cards fluidos e navegação mobile | Guia agora específico | Ajuda genérica não distinguia jornada pessoal | Finalidade, público, ação e impacto descritos explicitamente |
| Dashboard | Gestão / operação | Conforme controller e roles | Indicadores do contexto atual | Estados existentes | Skeletons/estados existentes | Estado de erro existente | Filtros reais quando disponíveis | Feedback global | Grid adaptativo | Guia agora específico | Ajuda genérica não orientava período/contexto | Orientação para conferir tenant, período e fonte do indicador |
| Usuários / perfis / permissões | Gestão do cliente | Administradores autorizados | Listagem e ações reais | Empty state existente | Padrões globais | Validação/erro existentes | IDs técnicos não são solicitados nas telas auditadas | Toast/modal existentes | Tabelas com rolagem e ações móveis | Guia específico | Hierarquia visual e foco variavam entre CRUDs | Foco, hover, ações desabilitadas e formulários móveis padronizados globalmente |
| Clientes / visão SaaS / auditoria | SaaS global | Super Admin MNSOFT | Contexto global explícito | Estados existentes | Padrões globais | Feedback seguro | Relacionamentos por seletores já existentes | Trilhas preservadas | Grupo próprio na sidebar | Guia específico | Cliente aparecia junto à gestão para administradores não globais; auditoria sem acesso direto no menu | Clientes, Visão SaaS e Auditoria agrupados e exibidos apenas para `ADMINISTRADOR_GLOBAL` |
| Médicos / hospitais / especialidades | Cadastros operacionais | Gestores e operadores autorizados | Listagens, badges e ações reais | Empty states existentes | Padrões globais | Erros inline/globais | Labels, validação e seletores existentes | Toast/modal existentes | Tabelas roláveis; botões empilhados no mobile | Guia específico | Feedback tátil/visual desigual | Hover discreto, foco WCAG, tabelas e ações móveis refinados |
| Escalas / plantões | Operação médica | Coordenação/operação autorizada | Cobertura, status e filtros | Ilustrações/empty state existentes | Skeleton/progresso existente | Estado de erro existente | Datas, horários e relacionamentos existentes | Confirmações próprias preservadas | Colunas importantes e cards móveis existentes | Guia específico | Densidade alta em notebook/mobile | Rolagem estável, cabeçalhos legíveis e microinterações sem alterar ações |
| Financeiro / relatórios | Financeiro e gestão | Roles financeiras/gestão | Valores, períodos e filtros reais | Estados existentes | Padrões globais | Mensagens existentes | Campos formatados existentes | Feedback global | Tabelas fluidas | Guia específico | Risco de perder contexto/competência | Ajuda reforça competência, impacto e LGPD; acabamento de tabela padronizado |
| Notificações | Acompanhamento | Destinatário autenticado no tenant | Avisos e destinos internos seguros | Empty state existente | Estado assíncrono existente | Erro seguro existente | Não aplicável | Região viva/toast | Drawer e central responsivos | Guia agora específico | Ajuda não explicava o efeito de “marcar como lida” | Guia esclarece que leitura não executa a ação operacional |
| Proposta comercial | Comercial | Conforme rota existente | Conteúdo real do model | Não aplicável | Não aplicável | Padrão global | Não aplicável | Não aplicável | Impressão com CSS dedicado | Guia global do layout | Handler inline `onclick` | Evento movido para JavaScript local, sem ação fictícia |

## Validação por perfil

- **Super Admin MNSOFT:** o menu apresenta um agrupamento global separado com Visão SaaS, Clientes e Auditoria. A indicação de tenant/contexto, a orientação para confirmar o tenant e o banner de acesso assistido existentes permanecem intactos.
- **Administrador do cliente:** mantém Usuários, Perfis, Permissões e Configurações quando autorizado, mas não recebe o agrupamento global MNSOFT. O escopo visual continua sendo a instituição vinculada à sessão.
- **Usuário operacional:** os módulos continuam condicionados às roles existentes; nenhuma ação administrativa foi promovida por CSS ou JavaScript. A ajuda orienta a conferir contexto e impacto.
- **Médico/profissional:** Minha Central e Meu Dia receberam ajuda direcionada à agenda, prioridades e próximos plantões, sem adicionar excesso administrativo.

## Melhorias aplicadas

### Login

- Copy comercial consolidada em “Acesse sua central PlantãoPro” e descrição curta de escalas, plantões, equipes e financeiro.
- POST tradicional, antiforgery, validação, proteção contra envio duplicado, timeout de recuperação, Caps Lock, mostrar/ocultar senha e recuperação de senha foram preservados.
- O campo permanece **E-mail**, pois a inspeção disponível não comprova suporte seguro do backend a CPF/CNPJ; a interface não promete um identificador que a autenticação possa rejeitar.
- Entrada visual curta e respeitosa, integralmente desativada para usuários com redução de movimento.

### Navegação, formulários, tabelas e mensagens

- Separação visual e condicional do espaço global MNSOFT para impedir que a navegação de tenant sugira acesso a áreas globais.
- Foco visível com contraste forte, hover apenas em dispositivos que suportam hover e transições de 160 ms.
- Botões desabilitados comunicam indisponibilidade sem parecer ativos; nenhuma regra altera autorização ou habilita comandos.
- Form actions passam a ocupar a largura disponível no mobile, modais ganham margem segura e regiões de toast não excedem o viewport.
- Tabelas têm cabeçalhos estáveis, rolagem com gutter e destaque de linha discreto.
- Mensagens existentes (toast, erro, atenção, informação e confirmação modal) foram preservadas; nenhuma API nativa `alert()`/`confirm()` foi introduzida.
- A ação de impressão da proposta deixou de usar `onclick` inline e agora usa um listener local, compatível com uma política de conteúdo mais restritiva.

## Ajuda contextual

Todas as páginas autenticadas continuam recebendo `_ScreenGuide` pelo layout. Nesta rodada foram adicionadas orientações específicas para Minha Central/Meu Dia, Dashboard e Notificações, sempre cobrindo finalidade, público, ação, cuidado, contexto do tenant e impacto. Os módulos de usuários, perfis, clientes, médicos, hospitais, especialidades, escalas, plantões, financeiro, relatórios, configurações e auditoria já tinham conteúdo específico e foram revalidados estaticamente.

## Responsividade e acessibilidade

A inspeção estática cobriu desktop grande, notebook, tablet, mobile, sidebar, formulário longo, tabela, modal e toast. O novo acabamento evita overflow em containers, empilha ações de formulário em telas estreitas, limita toasts ao viewport e mantém foco visível. A folha inclui comportamento de redução de movimento e regras de impressão.

## Screenshots

O repositório contém Playwright instalado, mas screenshots novos não foram gerados: sem o SDK .NET não é possível iniciar a aplicação Razor, autenticar com dados reais ou capturar as rotas solicitadas de modo confiável. Reutilizar HTML isolado ou dados inventados violaria o requisito de não usar mocks/fakes. As capturas históricas em `artifacts/screenshots` não foram apresentadas como evidência desta rodada.

## Comandos e resultados

- `git status --short --branch`: árvore inicial limpa na branch `work`.
- `git remote -v || true`: nenhum remoto inicialmente; `origin` adicionado depois a partir da URL fornecida.
- `dotnet --info || true` / `dotnet --list-sdks || true`: indisponível (`command not found`).
- Inventário com `find ... Views/Pages/wwwroot` e busca por módulos com `rg`: concluídos.
- Buscas de inconsistências e padrões proibidos: concluídas; `href="#"`, `alert()`, `confirm()` e `onclick="` não permanecem nas telas. Ocorrências de `style="` são históricas e incluem valores Razor dinâmicos para progresso/white-label, fora das telas tocadas.
- `python3 scripts/check-premium-ui.py`: aprovado.
- `python3 scripts/check-layout-regression.py`: aprovado.
- `python3 scripts/check-feedback-ui.py`: aprovado.
- `python3 scripts/check-ui-assets.py`: aprovado.
- `python3 scripts/check-form-experience.py`: aprovado.
- `python3 scripts/check-operational-ux.py`: aprovado.
- `python3 scripts/check-saas-ui.py`: aprovado.
- `python3 scripts/repository-security-check.py`: aprovado.
- `python3 scripts/check-csharp10-compatibility.py`: aprovado.
- `python3 scripts/validate-scrpt-completo.py`: aprovado, 100% de cobertura.
- `git diff --check`: aprovado.

## Limitações restantes

- Restore, builds Debug/Release, testes .NET, execução autenticada e QA visual por screenshot permanecem bloqueados pela ausência do SDK.
- Achados C#/SQL históricos da busca ampla não foram alterados, conforme a proibição expressa de tocar backend sem SDK.
- `git pull --rebase origin main` e `git push` foram tentados após configurar `origin`, mas o proxy recusou a conexão HTTP com código 403. A criação do PR também ficou bloqueada porque o GitHub CLI não possui autenticação neste ambiente.
