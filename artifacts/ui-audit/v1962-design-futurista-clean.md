# Auditoria de UI — PlantãoPro v1.96.2

## Escopo e conceito

Esta rodada evolui a camada v1.96.1 sem criar uma identidade paralela e sem alterar regras de negócio. O conceito aplicado é **SaaS B2B premium para saúde**: canvas branco-gelo, superfícies brancas, grafite e azul-petróleo para estrutura, slate para texto secundário e teal apenas para foco e ações. Gradientes, transparência e sombras foram reduzidos a sinais discretos de profundidade.

## Telas alteradas

- **Login:** microcopy mais direta e comercial; card, formulário, foco, segurança e composição responsiva recebem os tokens consolidados.
- **Shell autenticado:** topbar ganha contexto operacional mais sóbrio; sidebar, cabeçalhos e conteúdo recebem ritmo, canvas e superfícies unificados pela folha global.
- **Dashboards:** grids, KPIs, métricas, cards e títulos de `DashboardsPremium`, `SaasDashboard`, `ClinicaDashboard`, `AdminSaas`, `Saude360`, `CentralAtendimento` e `CentralEscala` são refinados sem inventar métricas.
- **Jornadas operacionais e clínicas:** tabelas, filtros, formulários, modais e cards usados por `Consultas`, `Triagem`, `Plantoes`, `Agendamentos`, `MedicoArea`, `HospitalArea` e `Financeiro` passam a compartilhar contraste, espaçamento e profundidade.
- **Agenda:** o hero histórico foi aproximado da paleta petróleo e seus cards passaram a usar as sombras e bordas semânticas globais.

## Antes/depois conceitual

| Área | Antes | Depois |
| --- | --- | --- |
| Superfícies | Branco e sombras definidos em várias camadas | Canvas gelo e superfície elevada semânticos, com sombra curta consistente |
| Dados | Espaçamento e alinhamento variavam por módulo | Células centralizadas verticalmente, ações sem quebra e ritmo uniforme |
| Cabeçalhos | Boa identidade, mas leitura distribuída | Largura de leitura controlada e hierarquia tipográfica mais objetiva |
| Formulários | Controles de alturas distintas | Altura confortável, radius menor e foco teal inequívoco |
| Mobile | Tabelas rolavam, mas podiam comprimir conteúdo | Largura mínima legível com rolagem horizontal e KPIs em 2/1 colunas |
| Operação | Azul com maior presença visual | Azul-petróleo estrutural, teal reservado a interação e estado |

## Tokens aplicados

- Canvas: `--fc-canvas` (`#f3f7f8`).
- Superfície elevada/muted: `--fc-surface-raised` e `--fc-surface-muted`.
- Raios: `--fc-radius-sm`, `--fc-radius` e `--fc-radius-lg`.
- Elevação: `--fc-shadow-xs`, `--fc-shadow-sm` e `--fc-shadow-lg`.
- Ritmo responsivo: `--fc-space-page`.
- Leitura: `--fc-reading-width`.
- Ponte legada: `--pp-app-canvas`, `--pp-surface`, `--pp-muted`, `--pp-border` e `--pp-shadow` apontam para tokens `--fc-*`.

## Componentes refinados

- Cards, KPIs, métricas, grids de dashboard e quick actions.
- Tabelas responsivas e ações/status dentro de células.
- Toolbars, filtros, campos, selects, modais, offcanvas e drawers.
- Cabeçalhos de página/workspace e textos introdutórios.
- Empty states, cards premium, formulários Saúde 360 e next actions por herança global.
- Login, topbar e painéis de operação.

## Decisões de UX

1. Preservar dados e fluxos existentes; a alteração é exclusivamente de apresentação e microcopy.
2. Reservar teal para foco, seleção e ação, reduzindo ruído cromático.
3. Limitar largura de descrições para varredura rápida e previsível.
4. Manter tabelas como tabelas no mobile, com rolagem explícita, em vez de ocultar campos relevantes.
5. Escalonar KPIs de múltiplas colunas para duas no tablet/mobile e uma em 360 px.
6. Usar fallbacks nos tokens para manter telas históricas funcionais durante a consolidação gradual.

## Checklist de responsividade

- [x] 360 px: KPIs em coluna única, login sem overflow e ações sem compressão.
- [x] 768 px: grids de métricas em duas colunas e tabelas com rolagem horizontal.
- [x] 1366 px: conteúdo com respiro fluido, cabeçalhos legíveis e densidade executiva.
- [x] Sidebar e topbar preservam os breakpoints e controles mobile existentes.
- [x] Cards e lanes usam `min-width: 0` onde conteúdo poderia forçar overflow.

## Checklist de acessibilidade

- [x] Foco visível teal em links, botões, controles e regiões tabuláveis.
- [x] Contraste sóbrio entre texto grafite/slate e superfícies claras.
- [x] Estados semânticos mantêm verde, âmbar e vermelho controlados.
- [x] Login mantém labels, ajuda, autocomplete, região de erro e botão explícito.
- [x] Animações continuam respeitando `prefers-reduced-motion`.
- [x] Não foram adicionados `alert()`, `confirm()`, HTML inseguro, CDN ou links vazios.

## Pendências reais fora deste PR

- A migração das folhas históricas minificadas para módulos semânticos deve ocorrer por jornada, com regressão visual dedicada; removê-las integralmente nesta rodada aumentaria o risco funcional.
- A varredura textual ainda encontra ocorrências legadas das expressões proibidas; elas estão documentadas pela validação e não foram introduzidas nesta rodada.
- Testes visuais autenticados de todos os perfis exigem massa/credenciais de ambiente e permanecem recomendados para homologação.
- Eventual falha de segurança vinculada a `backend/PlantaoPro.Api/appsettings.json` é preexistente à v1.96.2 e deve ser tratada em PR próprio de configuração.
