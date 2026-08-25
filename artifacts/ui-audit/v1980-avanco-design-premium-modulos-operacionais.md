# Auditoria visual — avanço premium dos módulos operacionais v1.98.0

## Objetivo e método

Auditoria estrutural das views Razor, layouts compartilhados, componentes e estilos usados nas jornadas autenticada e de acesso. A revisão priorizou consistência, hierarquia, semântica dos ícones, estados sem dados, responsividade e acessibilidade, sem alterar regras de negócio, autenticação ou persistência.

## Telas avaliadas

| Área | Views e estruturas avaliadas | Diagnóstico principal |
| --- | --- | --- |
| Login | `Account/Login`, `_AuthLayout` | Estrutura comercial e labels já existiam; faltava unificar o acabamento com a camada operacional e reforçar o erro, o foco e a adaptação mobile. |
| Dashboard | `Home/Dashboard` | Já consome indicadores, plantões, pagamentos e notificações reais e possui estados vazios; precisava de maior contraste hierárquico entre contexto, métricas, riscos e ações. |
| Navegação | `_AppSidebar`, `_AppTopbar`, `_MobileNavigation`, `_Layout` | Grupos operacionais estavam claros, mas os cadastros essenciais não eram alcançáveis diretamente no grupo de gestão e a topbar acumulava contexto em larguras intermediárias. |
| Escalas e plantões | `Escalas/Index`, `Escalas/Details`, `Plantoes/Index`, `Plantoes/Details`, `CentralEscala/*` | Há filtros, status e ações reais; tabelas e cards móveis precisavam compartilhar superfícies, densidade e foco. |
| Profissionais | `Medicos/Index`, `Medicos/Create`, `Medicos/Edit`, `Medicos/Details` | Fluxo completo existente, porém com acesso pouco evidente na navegação principal. |
| Unidades | `Hospitais/Index`, `Hospitais/Create`, `Hospitais/Edit`, `Hospitais/Details` | Cadastro real existente; ícone e nomenclatura precisavam refletir o contexto de unidade. |
| Usuários e permissões | `Usuarios/*`, `Permissoes/Matriz`, `Permissoes/TestarAcesso` | Módulos reais existentes, mas ausentes da navegação principal de gestão. |
| Relatórios | `Relatorios/Index`, `Relatorios/Cobertura`, `Relatorios/ProdutividadeMedica`, `Relatorios/Sla`, `Relatorios/Saas` | Conteúdo especializado existente; precisava herdar a mesma linguagem visual de tabelas, filtros e blocos. |
| Configurações e white label | `Configuracoes/*`, `Parametrizacoes/*`, `Parametrizacoes/WhiteLabel` | Jornadas existentes e separadas por contexto; preservadas sem mudança funcional. |
| Administração | `AdminSaas/*`, `Auditoria/*`, `Observabilidade/*`, `FeatureFlags/Dashboard` | Alta densidade informacional; passa a herdar tokens, superfícies, tabelas e controles uniformes. |

## Principais problemas visuais encontrados

- A identidade anterior estava distribuída entre muitas folhas de estilo, com acabamento nem sempre uniforme entre módulos antigos e novos.
- Cadastros de profissionais, unidades, usuários e permissões existiam, mas não formavam um grupo operacional navegável na sidebar.
- Algumas superfícies tinham raio, borda e sombra diferentes, reforçando a sensação de telas montadas em momentos distintos.
- Em notebooks, o conjunto de contexto do tenant, plano, busca, ajuda, upgrade, notificações e usuário podia competir por espaço na topbar.
- Tabelas herdadas ainda se aproximavam do visual padrão de scaffolding e precisavam de cabeçalho, hover, densidade e recorte consistentes.
- O login possuía boa base semântica, mas precisava de tratamento mais sóbrio para fundo, painel, erro e leitura em celular.

## Decisões de design

1. **Camada incremental e segura:** criada uma folha v1.98.0 carregada por último nos dois layouts. Assim, todos os módulos reais recebem o padrão sem duplicar marcação nem interferir em regras de negócio.
2. **Paleta calma:** canvas frio, superfícies brancas, azul profundo institucional, ciano discreto e verde saúde apenas como apoio. Sombras são leves e bordas finas.
3. **Hierarquia operacional:** heróis, seções, métricas, cards de ação e cards operacionais compartilham borda, raio e elevação; métricas usam números tabulares.
4. **CRUD legível:** controles têm foco ciano visível; tabelas usam cabeçalhos compactos, linhas frias e hover suave; badges permanecem textuais para não depender apenas de cor.
5. **Navegação funcional:** profissionais, unidades, usuários e permissões entram no grupo Gestão com ícones coerentes e acesso condicionado às permissões já existentes.
6. **Responsividade por redução de ruído:** contexto secundário da topbar é ocultado progressivamente; filtros, ações e métricas passam para uma coluna; cards substituem tabelas onde as views já oferecem essa alternativa.
7. **Movimento responsável:** a camada respeita `prefers-reduced-motion` e mantém reforço de bordas em modo de cores forçadas.

## Componentes criados ou refinados

- Tokens de canvas, superfície, texto, borda, marca, destaque, radius e sombra.
- Cabeçalho contextual e subtítulo.
- Cards de métrica, ação, operação e perfil.
- Painéis de filtros e controles de formulário.
- Tabelas, badges e empty states.
- Botões primários e destrutivos.
- Sidebar, item ativo e topbar responsiva.
- Card e painel narrativo do login.
- Mensagem de erro de autenticação.
- Superfície de modal e tratamento de foco.
- Suporte a movimento reduzido e cores forçadas.

Os componentes compartilhados já existentes — alertas, chips/status, loading, modais, toasts, blocos de resumo, timeline, avatar/perfil e estados vazios — foram preservados e passam a receber os mesmos tokens e superfícies por meio da camada global.

## Telas alteradas

- Todas as telas autenticadas que usam `_Layout`, incluindo dashboard, escalas, plantões, profissionais, unidades, usuários, permissões, relatórios, configurações, white label e administração.
- Login e demais telas que usam `_AuthLayout`.
- Sidebar principal, com atalhos de gestão alinhados aos módulos reais.

## Validações executadas

- `python3 scripts/repository-security-check.py` — aprovado.
- `python3 scripts/check-csharp10-compatibility.py` — aprovado.
- `python3 scripts/validate-scrpt-completo.py` — aprovado, cobertura reportada em 100%.
- `dotnet restore backend/PlantaoPro.sln` — não executado: SDK `dotnet` indisponível no ambiente.
- Build e testes .NET — não executados pelo mesmo bloqueio de ambiente.
- `git diff --check` — aprovado após a implementação.
- Busca de padrões de segredo solicitada — executada; os achados seguros/negativos são documentados no resumo da PR.

## Riscos e pendências

- Executar restore, build e testes em um runner com .NET SDK antes do merge.
- A validação visual automatizada depende de iniciar a aplicação, o que requer o runtime .NET ausente neste ambiente; recomenda-se conferir os breakpoints de 1440, 1024, 768 e 390 px no CI de interface ou ambiente de homologação.
- A folha v1.98.0 é deliberadamente incremental. Regras antigas continuam carregadas para compatibilidade e podem ser consolidadas em uma revisão futura após regressão visual completa.
- Consulta de PRs abertas e estado remoto do CI não pôde ser concluída porque o checkout não possui remote configurado nem autenticação do GitHub CLI.
