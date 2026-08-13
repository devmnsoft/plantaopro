# v1.64 — guia visual interno do design system

## Princípios

- **Clínico e confiável:** navy para hierarquia e confiança; azul para ação; teal para saúde.
- **Sem cor decorativa de status:** verde, âmbar e vermelho ficam reservados a sucesso, atenção e erro.
- **Densidade confortável:** conteúdo limitado por `pp-content-container`, gaps fluidos e cards sem mínimos frágeis.
- **Progressive enhancement:** HTML e validação server-side continuam úteis sem JavaScript.

## Cores

| Papel | Token/classe de referência | Uso |
|---|---|---|
| Navy | `--pp-v161-navy` (`#0b2443`) | headings e texto de maior hierarquia |
| Ação | `--pp-v161-blue` (`#1769aa`) | ação primária e foco |
| Saúde | `--pp-v161-teal` (`#087f8c`) | contexto assistencial |
| Canvas | `--pp-v161-canvas` (`#f5f8fb`) | fundo clínico |
| Linha | `--pp-v161-line` (`#dce5ee`) | divisores e bordas |
| Secundário | `--pp-v161-muted` (`#526579`) | apoio com contraste legível |

Sempre verificar contraste AA no browser; não inferir conformidade apenas pelo token.

## Tipografia

- Hero público: `clamp(2.4rem, 4.8vw, 4.35rem)`, máximo de 15 caracteres aproximados por linha (`15ch`).
- Hero autenticado: `clamp(1.65rem, 3vw, 2.35rem)`.
- Login: headline `clamp(2rem, 3.6vw, 3.45rem)` e título de card até `2rem`.
- Título de card: cerca de `1.25rem`; body com `line-height` próximo de `1.6`.
- Labels usam `pp-form-label`; helper usa `form-help`; erro usa `pp-form-error`.

## Componentes

### Botões

Use `.btn.btn-primary` para a ação principal e outline para secundárias. Todo `<button>` declara `type`; botão somente com ícone declara `aria-label`. Ações de hero ficam em `.pp-hero-actions` e ocupam a largura no mobile.

### Cards e KPIs

- `.pp-card`: superfície base.
- `.pp-action-card`: card navegável, ação no rodapé.
- `.pp-data-card`: informação sem ação obrigatória.
- `.pp-kpi-card` dentro de `.pp-kpi-grid`: KPI com altura coerente, valor forte e contexto real.
- `.pp-mobile-card`: alternativa semântica a tabela em telas estreitas.

Não preencher KPIs ausentes com números artificiais; usar empty state ou linguagem explícita de indisponibilidade.

### Formulários

Formulário usa `.pp-form`; cada controle fica em `.pp-form-field`, com label acima, `.pp-form-control`, helper e `.pp-form-error`. Use `.pp-form-grid` para duas colunas que colapsam abaixo de 768px, `asp-validation-summary` no topo e `.pp-form-actions` no fim.

### Tabelas

Use `.pp-data-table` dentro de `.table-responsive`. Para fluxos densos, ofereça `.pp-mobile-card`. Cabeçalhos devem ser claros e ações agrupadas; scroll horizontal é contenção, não substituto automático da apresentação mobile.

### Drawers e modais

`pp-detail-drawer` e `_WorkItemDrawer` usam `role="dialog"`, `aria-modal`, título/descrição associados, close nomeado e retorno de foco. Drawer aberto deve ficar acima da sidebar e ocupar `100dvh` abaixo de 600px. Confirmações usam `_ConfirmModal`, nunca `confirm()`.

### Toasts e banners

Toasts são publicados em região `aria-live="polite"` com ícone, título e mensagem. Erros críticos podem usar anúncio assertivo. No mobile, nenhuma mensagem pode ficar atrás da navegação fixa.

### Empty states

Use `_EmptyState` com título objetivo, motivo verdadeiro e próxima ação disponível. Não invente usuário, paciente, plano, valor, status ou contador.

## Breakpoints operacionais

- `≤430px`: celulares estreitos e menu de usuário compacto.
- `≤767px`: forms em uma coluna, heros/actions empilhados, drawer de página.
- `≤991px`: sidebar vira drawer; conteúdo ganha espaço inferior para navegação.
- `≤1199px`: Admin SaaS e grids laterais viram uma coluna.
- `≥1200px`: composição desktop com lateral e conteúdo principal.

A homologação oficial cobre 360×800, 390×844, 430×932, 768×1024, 1024×768, 1366×768 e 1920×1080.
