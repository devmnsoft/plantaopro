# Auditoria UI — v2.00.0 Grande evolução de design e UX premium

## Resumo da evolução

A v2.00.0 consolida as três folhas incrementais das versões 1.97/1.98 em uma única fonte canônica, `v2000-premium.css`, e transforma a linguagem visual transversal do PlantãoPro. O resultado privilegia azul profundo, superfícies suaves, ciano controlado, hierarquia tipográfica, densidade operacional e feedback discreto. Nenhuma regra de negócio, banco, migration ou permissão foi alterada.

## Escopo auditado

Foram auditadas as 385 views Razor, o shell compartilhado, autenticação, dashboards/centrais, escalas, plantões, profissionais, unidades, administração e relatórios. A revisão incluiu os CSS de `wwwroot/css`, componentes compartilhados, catálogo local de ícones, scripts de interação e referências de assets nos layouts.

### Telas diretamente impactadas pelo sistema transversal

- Login, recuperação e redefinição de senha;
- Minha Central, Meu Dia e dashboards por perfil;
- Plantões, Escalas e agenda/calendário;
- Profissionais e Unidades, incluindo listagens, detalhes e formulários;
- Usuários, permissões, clientes/tenants, configurações, white label e auditoria;
- Relatórios e áreas financeira, clínica e operacional.

## Problemas encontrados

- Três folhas incrementais (`futuristic-calm-system`, `premium-clean-v1970` e `premium-operations-v1980`) eram carregadas em sequência, criando uma arquitetura de sobrescritas por versão.
- Tokens, elevação, raios e transições estavam distribuídos por camadas históricas.
- Cards, tabelas e controles possuíam diferenças de densidade entre módulos.
- Breakpoints intermediários não tinham a mesma qualidade dos extremos desktop/mobile.
- Estados de foco e movimento reduzido existiam, mas não eram uniformes em todos os elementos interativos.
- O repositório já possui componentes compartilhados para estados vazios, erro, tabelas, filtros, drawers, toasts e navegação; criar cópias aumentaria dívida técnica.
- O catálogo local SVG já evita dependência de CDN; foi preservado como família canônica.
- A busca por `href="#"`, `alert(` e `confirm(` no projeto Web não encontrou ocorrências produtivas.

## Decisões de design

1. **Uma camada canônica:** conteúdo válido das três folhas históricas foi incorporado e refinado em `v2000-premium.css`; as folhas versionadas foram removidas.
2. **Paleta semântica:** verde fica reservado a sucesso, âmbar a atenção e vermelho a risco/erro; azul profundo e teal estruturam navegação e ações.
3. **Elevação contida:** bordas frias e sombras de baixa opacidade substituem sombras pesadas.
4. **Operação legível:** tabelas ganham cabeçalhos compactos, linhas respiráveis e hover sutil; cards recebem a mesma superfície, raio e elevação.
5. **Movimento responsável:** transições entre 160–180 ms e suporte explícito a `prefers-reduced-motion`.
6. **Login institucional:** fundo profundo com textura radial discreta, painel de marca sóbrio, card claro e ação principal inequívoca.
7. **Mobile primeiro:** ações e filtros empilham, tabelas mantêm rolagem contida e controles preservam área de toque.

## Design System 2.0

### Tokens consolidados

Cores, superfícies, texto, borda, sombras, raios, espaçamento, largura da sidebar, altura da topbar, foco, transição e camadas de z-index foram centralizados em `:root`. Breakpoints cobrem mobile compacto, mobile, tablet/desktop e telas amplas.

### Componentes criados ou refinados

- **AppShell, Sidebar, Topbar e ContextBar:** navegação profunda, item ativo com marcador além de cor, rolagem independente, topbar translúcida e contexto preservado.
- **PageHeader e SectionHeader:** hierarquia tipográfica e espaçamento consistentes.
- **MetricCard, OperationalCard e QuickActionCard:** superfície, hover, borda e elevação comuns.
- **StatusBadge e RiskBadge:** forma pill e marcador visual redundante ao texto.
- **ActionBar, FilterBar e SearchBox:** controles de altura consistente e empilhamento mobile.
- **DataTable:** cabeçalho semântico, densidade operacional, hover e contêiner responsivo.
- **EmptyState, LoadingState e ErrorState:** painel tracejado, skeleton com shimmer e alertas semânticos.
- **Toast, Modal e Drawer:** elevação controlada, entrada curta e foco evidente.
- **Timeline, ProfileChip, TenantContextBanner e NotificationItem:** passam a herdar tokens globais e estados interativos uniformes.

Todos os componentes suportam normal, hover, `focus-visible`, disabled, loading/`aria-busy`, empty, error e reorganização mobile por estilos transversais. Estados continuam dependentes dos dados reais entregues pelas views; nenhum mock foi adicionado.

## Arquivos alterados

- `backend/PlantaoPro.Web/wwwroot/css/design-system/v2000-premium.css` — fonte canônica do Design System 2.0.
- `backend/PlantaoPro.Web/Views/Shared/_Layout.cshtml` — carregamento único do tema em páginas autenticadas.
- `backend/PlantaoPro.Web/Views/Shared/_AuthLayout.cshtml` — carregamento único do tema de autenticação.
- Removidos os três CSS incrementais históricos das versões anteriores.
- `artifacts/ui-audit/v2000-grande-evolucao-design-ux-premium.md` — esta evidência.

## Checklist responsivo

| Viewport | Resultado da revisão CSS |
|---|---|
| 360 px | Conteúdo com padding compacto, ações em largura total e login sem moldura externa |
| 390 px | Área de toque mínima e conteúdo sem largura fixa |
| 768 px | Cards/filtros reorganizados e tabelas com overflow localizado |
| 1024 px | Sidebar offcanvas e topbar reduzida sem colisão de ações |
| 1366 px | Shell expandido e densidade operacional equilibrada |
| 1440 px | Container central com espaçamento ampliado |
| 1920 px | Conteúdo limitado a 94 rem para evitar linhas excessivamente longas |

- [x] Sem largura fixa no conteúdo principal.
- [x] Sidebar preserva comportamento offcanvas existente.
- [x] Tabelas possuem overflow no próprio componente.
- [x] Topbar reduz ações secundárias progressivamente.
- [x] Login ocupa a viewport no mobile.
- [x] Ações e filtros empilham abaixo de 768 px.

## Checklist de acessibilidade

- [x] Skip link e região viva existentes preservados.
- [x] Foco visível global com contraste e espessura perceptíveis.
- [x] Labels visíveis do login preservados.
- [x] Status combina texto, cor e marcador de forma.
- [x] Controles desabilitados comunicam indisponibilidade e cursor.
- [x] `prefers-reduced-motion` desativa movimentos não essenciais.
- [x] Áreas de toque móveis possuem pelo menos 44 px nos controles principais.
- [x] Navegação, modal, drawer e busca mantêm os atributos ARIA existentes.

## Evidência visual e limitações

Não foi possível gerar captura do aplicativo no contêiner porque o runtime `dotnet` e um navegador headless não estão instalados. A evidência reproduzível fica nos tokens, breakpoints e componentes canônicos acima. A validação visual final deve ser executada em ambiente com SDK .NET e navegador nos viewports listados.

Também não foi possível consultar PRs abertas ou CI do GitHub: o checkout não possui remote configurado e o GitHub CLI não possui autenticação. O commit local de partida é `938a1d4`, imediatamente após o merge da PR #394; o histórico local contém os merges de design #392 e #393.

## Comandos executados

```bash
git status --short --branch
git log --oneline --decorate -8
gh pr list --repo devmnsoft/plantaopro --state open --search 'design in:title'
gh run list --repo devmnsoft/plantaopro --branch main --limit 5
rg -n 'href="#"|alert\(|confirm\(' backend/PlantaoPro.Web --glob '!wwwroot/lib/**'
python3 scripts/repository-security-check.py
python3 scripts/check-csharp10-compatibility.py
python3 scripts/validate-scrpt-completo.py
dotnet restore backend/PlantaoPro.sln
dotnet build backend/PlantaoPro.sln -c Release --no-restore
dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build
git diff --check
```

Os três validadores Python passaram. Os comandos .NET não iniciaram por ausência do executável no ambiente, não por falha identificada no código.
