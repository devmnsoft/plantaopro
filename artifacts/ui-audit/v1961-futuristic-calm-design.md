# PlantãoPro v1.96.1 — design futurista sóbrio e calmo

## Conceito

A camada **Clinical Calm Future** posiciona o PlantãoPro como uma central SaaS de saúde conectada: tecnológica sem estética de game, executiva sem frieza e clínica sem aparência hospitalar genérica. A linguagem usa luz ambiente discreta, superfícies quase opacas, bordas finas e profundidade curta. Nenhuma regra de negócio ou fonte de dados foi alterada.

## Paleta e tokens

A fonte de verdade está em `design-system/futuristic-calm-system.css`:

- `--fc-navy` e `--fc-graphite`: navegação e estrutura;
- `--fc-ink` e `--fc-muted`: hierarquia de conteúdo;
- `--fc-surface`, `--fc-surface-solid` e `--fc-border`: vidro discreto e separação;
- `--fc-cyan`: tecnologia, foco e ação controlada;
- `--fc-success`, `--fc-warning`, `--fc-danger` e `--fc-info`: estados semânticos;
- `--fc-focus`, `--fc-shadow` e `--fc-radius`: foco, profundidade e geometria consistentes.

Os estilos históricos receberam somente aliases/bridges, evitando duplicar componentes existentes. A nova folha é carregada por último nos layouts autenticado e de autenticação.

## Telas e áreas evoluídas

- Layout global: canvas, sidebar, módulo ativo, tenant, busca, topbar, contexto conectado, menu de usuário e conteúdo.
- Login: composição em duas áreas, proposta de valor, benefícios de operação/jornada/governança, formulário claro e sinal de segurança.
- Dashboards Premium, SaaS, Clínica e Admin SaaS: cards, métricas, grids, cabeçalhos e hierarquia passam a compartilhar a camada global.
- Saúde 360, Triagem, Consultas, Plantões e Agendamentos: filtros, ações, tabelas, badges, empty states e superfícies operacionais são harmonizados por componentes reutilizáveis.
- Workboards/timelines: drawers, timeline e estados contextuais usam a mesma profundidade e cor semântica.

## Componentes alterados ou criados

- Criado `futuristic-calm-system.css`, responsável por tokens, canvas, superfícies, cards de KPI/módulo, painéis de filtro, tabelas responsivas, botões, badges, command palette, login, modais, drawers, timeline, alerts e breakpoints.
- Page Header, Page Context Header e Workspace Header adotam a superfície contextual comum.
- Action Toolbar e Quick Actions ganharam semântica operacional e agrupamento explícito.
- Empty State ganhou região semântica e associação acessível de título; Error State, Status Badge e dados de toast ganharam hooks visuais consistentes.
- Command Palette ganhou título e contexto visível; topbar expõe estado de operação conectada.

## Decisões de UX

- O destaque ativo fica restrito a contraste, borda e marcador teal, sem glow neon.
- KPIs mantêm o dado principal forte e oferecem leitura por meio dos rótulos já existentes.
- Hover desloca apenas dois pixels e nunca é necessário para compreender ou operar um controle.
- Filtros são tratados como painéis operacionais; tabelas preservam densidade confortável e rolagem horizontal quando necessária.
- Conteúdo real e ações existentes foram preservados: não foram criadas imagens, dados, endpoints ou ações decorativas.

## Responsividade

- **360 px:** login vira fluxo vertical, reduz narrativa secundária, menus preservam controles e tabelas rolam no próprio contêiner.
- **768 px:** cards empilham segundo os grids existentes, toolbars permitem quebra e ações usam largura disponível.
- **1366 px:** sidebar e topbar mantêm contexto sem competir com o workspace.
- **Telas largas:** o conteúdo é limitado a 1680 px para evitar linhas e tabelas excessivamente dispersas.
- Dialog usa `calc(100vw - 2rem)` e drawers usam `min(32rem, 100vw)`/`100dvh`.

## Acessibilidade

- Foco visível global com outline e halo controlado; estados disabled têm cursor e contraste próprios.
- Cores de status são acompanhadas por texto; ícones continuam complementares.
- Empty state usa `role=status` e título associado; erros permanecem regiões de alerta.
- Command Palette preserva `dialog`, busca rotulada, resultados vivos e fechamento por teclado.
- `prefers-reduced-motion` reduz animações e transições; `prefers-contrast: more` reforça bordas e navegação.
- Contraste principal usa navy/slate sobre branco suave, evitando ciano claro como texto essencial.

## Como validar visualmente

1. Executar a aplicação e entrar com um usuário autorizado.
2. Conferir Login e, após autenticação, sidebar/topbar nos viewports 360, 768, 1366 e acima de 1680 px.
3. Visitar Dashboard Premium, SaaS Dashboard, Clínica Dashboard, Admin SaaS, Saúde 360, Triagem, Consultas, Plantões e Agendamentos.
4. Abrir busca com `Ctrl+K`, navegar por teclado, abrir menu do usuário, filtros, confirmação e detail/quick-action drawers.
5. Simular empty/error/toast e estados success/warning/danger; habilitar redução de movimento e alto contraste no sistema operacional.

Não foi produzido print automatizado porque a validação depende de autenticação e dados reais do tenant; não são adicionados dados fake para contornar esse requisito.

## Próxima sprint visual

- Fazer auditoria visual autenticada por perfil/tenant com snapshots de regressão nos quatro viewports.
- Consolidar variações históricas de cards e badges após medir uso, removendo CSS legado apenas em uma mudança dedicada.
- Validar contraste com ferramenta automatizada sobre dados e estados reais e revisar gráficos/canvas que não herdam CSS.
