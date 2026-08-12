# Diagnóstico visual — PlantãoPro v1.61

## Método e causa raiz

A auditoria estática cobriu o shell Razor, autenticação, navegação, páginas operacionais, formulários, feedback, drawers, CSS e JavaScript. O desalinhamento vinha sobretudo da sobreposição histórica de regras de experiência versionadas: o shell já possuía a estrutura correta, mas a folha médica ainda se identificava como v1.55 e o login dependia de regras anteriores para definir suas colunas. A v1.61 consolida essa geometria na folha médica ativa, sem `!important`, mantendo conteúdo e indicadores alimentados pelos modelos reais.

## Matriz de auditoria e correções

| Tela / área | Visual e contraste | Layout e espaçamento | Responsividade | Formulário / botão / ícone | Card / tabela / feedback | Correção aplicada |
|---|---|---|---|---|---|---|
| Layout, topbar e footer | Hierarquia dispersa entre folhas históricas | Risco de conteúdo sem largura e footer flutuante | Sidebar precisava transição explícita | Ações compactas e rotuladas | Overlays precisam permanecer acima do shell | Grid `280px/minmax(0,1fr)`, container de 1440px, main flex e footer ao final preservados e validados na folha consolidada. |
| Sidebar | Identidade e item ativo já contrastantes | Largura fixa coerente e navegação rolável | Drawer em até 991px | Busca e recolhimento possuem ícones e nomes acessíveis | Rodapé não compete com os itens | Mantido shell sticky no desktop, drawer no mobile e identificação atualizada para v1.61. |
| Login | Headline curta demais e mensagem comercial incompleta | Colunas dependiam de CSS legado; painel poderia parecer desbalanceado | Benefícios e formulário corriam risco de corte | Labels acima, toggle, Caps Lock, loading e erro inline já funcionais | Card precisava de semântica visual explícita | Grid 1.15/.85, largura máxima de 72rem, overflow controlado, `pp-auth-card`, copy definitiva e coluna única móvel. |
| Recuperar / redefinir senha | Contraste clínico herdado | Card central já limita leitura | Campo ocupa largura disponível | Ajuda, erro e ação submetível | Summary acessível | Padrão global `pp-form`/campo/erro consolidado e auditado. |
| Admin SaaS / B2B / Planos / Onboarding | Ações administrativas tinham de permanecer legíveis | Painel lateral pode comprimir cards | Grid passa a uma coluna antes de cortar | Ações usam rotas reais e tipos adequados | KPIs usam contagens do modelo; ausência usa empty state | `pp-page`, hero, KPI grid, section grid, revisão sticky só em desktop e cards flexíveis validados. |
| Dashboard / Minha Central / Meu Dia | Hierarquia variava por jornada | Conteúdo deve respeitar container global | Grids empilham e navegação móvel permanece disponível | Ações rápidas rotuladas | KPIs reais ou estados vazios | Composição `pp-page`/workspace e shell único mantida; gates verificam raízes críticas. |
| Agenda / Plantões / Escalas | Densidade operacional elevada | Filtros e ações podem disputar espaço | Tabelas requerem wrapper ou alternativa móvel | Controles alinhados e botões tipados | Drawers devem ter diálogo, foco, loading e erro | Gates estruturais preservados para tabelas responsivas e drawers; camada móvel vira painel. |
| Saúde 360 / Pacientes | Cores de status devem conservar significado clínico | Cards clínicos não podem sair do fluxo | Grids reduzem para uma coluna | Formulários usam labels e mensagens associadas | Estados vazios evitam pacientes fictícios | Tokens navy/azul/teal e padrões clínicos existentes passam pelo shell v1.61. |
| Agendamentos / Triagem / Consultas | Prioridade assistencial requer contraste | Seções longas precisam cards e espaçamento | Form grids colapsam sem overflow | Inputs/selects têm altura comum, help e erro | Tabelas e timelines preservam leitura | Grid de formulário `minmax(0,1fr)` e ações full width no mobile consolidados. |
| Pagamentos / Financeiro / Relatórios | Verde reservado ao sucesso; vermelho ao erro | KPIs e filtros devem permanecer no container | Tabelas roláveis/cards móveis | Ações financeiras não ficam soltas | Badges e empty states existentes | Shell, KPI auto-fit e wrappers responsivos validados sem dados simulados. |
| Configurações | Seções administrativas precisam hierarquia | Formulários extensos exigem agrupamento | Duas colunas viram uma | Label, helper, erro e rodapé de ações | Feedback utiliza toast/modal acessível | Classes obrigatórias são verificadas pelo gate de experiência de formulário. |
| Toasts e modais | Mensagem deve acompanhar estado/ícone | Portal evita recorte por ancestrais | Região respeita navegação móvel | Ações explícitas | `aria-live`, modal de confirmação; sem APIs nativas | Gates rejeitam `alert()` e `confirm()` e mantêm regiões acessíveis. |
| Drawers operacionais | Close e estados precisam ser visíveis | Z-index deve superar sidebar | Full-screen em telas estreitas | Foco e ações reais | Timeline/loading/erro | `_DetailDrawer` e `_WorkItemDrawer` permanecem diálogos acessíveis e são verificados por regressão. |

## Arquivos auditados

Foram revisados `Views/Shared` (layout, sidebar, topbar, user menu, footer, workspace header), autenticação, AdminSaas, B2BLaunch, Planos, Onboarding e as views de Home, MinhaCentral, MeuDia, Agenda, Plantões, Escalas, Saúde 360, Pacientes, Agendamentos, Triagem, Consultas, Pagamentos, Financeiro, Relatórios e Configurações; além de `plantaopro.css`, `design-system/*` e scripts de interface.

## Decisões

- Nenhum dado clínico, tenant, plano, contador ou notificação fictícia foi adicionado.
- Nenhum `!important`, `href="#"`, `alert()` ou `confirm()` foi introduzido.
- A validação visual automatizada cobre 360, 390, 430, 768, 1024, 1366 e 1920 px.
