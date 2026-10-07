# R4-C11 · Design system — escala de camadas, breadcrumbs, contraste/tokens

- **Data**: 2026-10-07 (UTC) · **Rodada**: 4 · **Bloco**: C11 (Design), item 8 do d1-baseline
- **Base da árvore**: `c84afc5` (consolidação p/ homologação), tree limpa ao iniciar a C11.
- **Commits do bloco até aqui**:
  | Commit | Prefixo | Conteúdo |
  |---|---|---|
  | `11ea69f` | R4-C11a | Escala canônica de z-index `--pp-z-*` em `tokens.css` |
  | `aa29693` | R4-C11b | 9 referências ad-hoc migradas p/ `var(--pp-z-*)` |
  | `481f1a9` | R4-C11c | `IBreadcrumbService` (resolver) + mapa `AreaPrefixes` + 5 testes + DI |
  | `8640eeb` | R4-C11d | Topbar renderiza o trilho canônico; `_PageContextHeader`/`_WorkspaceHeader` perdem o crumb próprio |
  | `717f7df` | R4-C11e | 18 `<nav>` hand-rolled removidos (+ aportes `BreadcrumbLabel`/`BreadcrumbLeaf`); órfãos `_Breadcrumb.cshtml`/`_FriendlyBreadcrumb.cshtml` excluídos; chave de área corrigida `V112Web` (teste de guarda) |
  | `717f7df+1` | R4-C11f | Este item: C11.3 contraste/tokens + correção do indicador de foco |

---

## 1. C11.1 — Escala de camadas (concluída)

Escala única de z-index (`--pp-z-skip`, `--pp-z-topbar`, `--pp-z-drawer`, `--pp-z-overlay`, `--pp-z-dropdown`, `--pp-z-tour`) em `tokens.css`; camadas v2xxx deixaram de declarar valores mágicos.

Mapeamento adotado (regra: `z<1000` = stacks locais legítimos, mantidos):

| Padrão legado | Token |
|---|---|
| help-trigger (dropdowns) | `--pp-z-dropdown` |
| help-backdrop | `--pp-z-overlay` |
| help-drawer | `--pp-z-drawer` |
| tour (3 camadas) | `--pp-z-tour` / calc(+1/+2) |
| skip-link + assisted-context | `--pp-z-skip` |
| alert v2132 (1050) | `--pp-z-topbar` |

Invariante registrada: não re-introduzir `--pp-z-*` nem cores de marca em `:root` de camadas v2xxx intermediárias (fonte única = `tokens.css`; camada canônica final `v2150-bloco-c-canonical.css` documenta o mesmo valor por redundância intencional).

## 2. C11.2 — Breadcrumbs (concluída)

**Decisão de produto registrada**: breadcrumb **canônico único na topbar persistente**; nenhum controller popula crumb; as views só aportam rótulos via `ViewData`.

Resolver (`BreadcrumbService.ResolveTrail`):
- `HomeUrl = "/Home/Dashboard"`; raiz sempre `[Início(link)]`.
- Catálogo com feature conhecida → trilha rica; sem feature → `Início + AreaPrefixes + atual`.
- `AreaPrefixes` conservador (match exato, `OrdinalIgnoreCase`): `Administrativo360→["Administrativo 360"]`, `CentralEscala→["Central de Cobertura"]`, **`V112Web→["Homologação"]** (guardado por teste — correção da chave `V112`).
- Folha: `dynamicLeaf ?? breadcrumbLabel ?? pageTitle ?? Humanize(action)`; dedup contra o último nó do prefixo.
- **Invariante**: o trilho nunca expõe controller bruto (6 testes no resolver).

Suíte após C11.2: **1049/1049**.

## 3. C11.3 — Contraste e tokens (concluída neste commit)

### 3.1 Método

Contraste calculado por script (luminância relativa WCAG 2.x, fórmula exata) sobre os pares reais de renderização: branco `#ffffff`, canvas `#f4f8fb`, superfície sutil `#f8fafc` e os softs de cada semântica. Critérios: texto ≥ 4,5:1 (AA); UI não-texto / indicador de foco ≥ 3:1 (WCAG 1.4.11).

### 3.2 Situação antes das correções

**Aprovados (não alterados)** — mínimo do conjunto:

| Família | Mínimo medido |
|---|---|
| Tokens canônicos de texto (`--pp-text-*`) sobre branco/canvas | **4,55:1** |
| Semânticas canônicas sobre branco e softs | **4,87:1** |
| Invertidos (branco sobre navy/sidebar) | **14:1+** |
| 27 tokens ink/muted das camadas v2xxx (v2000→v2148) | **4,51:1** (`fc-muted #60748a` sobre canvas) — todos ≥ 4,5 |
| Links de conteúdo | **5,85:1** — já resolvidos pela camada v2151 (`--pp-brand-primary-text #1a5fd0` → `--bs-link-color`) |
| Semânticas vivas danger/info sobre softs | `#c33b46` 4,52:1 · `#176ba0` 5,09:1 |

**Falhas reais encontradas e corrigidas neste commit:**

1. **Colisão de tipo no token de foco (a mais grave)**. `--pp-focus` era cor canônica em `tokens.css`, mas `v2000-premium.css` o redefinia como **sombra** (`0 0 0 3px rgb(15 155 168 / 24%)`). Consumidores que o usavam como cor (`accessibility.css`, `charts.css`, `forms.css` via color-mix) passavam a produzir valor inválido em *computed-time* → **outline inexistente**; o único indicador visível era o halo teal a 24% de alfa (~1,2:1). Reindefinições mortas na cascade: `v2132` (`#64c8d1`) e `v2134` (sombra vestindo nome de cor).
   Correção: separação de papéis na fonte única —
   - `--pp-focus-ring: var(--pp-brand-primary)` (cor; resolve vivo em `#1d6fe9` pela camada v2151 → **4,66:1** sobre branco, passa nos 3:1 de foco e até no 4,5 de texto);
   - novo `--pp-focus-shadow: 0 0 0 .2rem color-mix(in srgb, var(--pp-brand-primary) 30%, transparent)` (halo **decorativo**, segue a marca automaticamente);
   - 8 consumidores migrados (`accessibility.css`, `charts.css`, `forms.css`, `v2000` ×2, `v2126` ×2, `v2134`); definições conflitantes removidas (`v2000:394`, `v2132:13`, `v2134:2`); `outline:3px solid #16849f` literal (v2134) → `var(--pp-focus-ring)`.
   Antes/depois:

   | | Antes (vivo) | Depois (vivo) |
   |---|---|---|
   | Indicador primário (outline) | **inexistente** (valor inválido) | outline sólido 3px marca → 4,66:1 |
   | Reforço (halo) | box-shadow teal 24% (~1,2:1), cor fora da identidade | color-mix da marca 30% (decorativo) |

2. **Semânticas vivas × softs (badges)**: `v2000` redefinía `--pp-success #16845b` (sobre `#e8f7f0` = **4,23:1**) e `--pp-warning #a86408` (sobre `#fff4d8` = **4,27:1**) — abaixo de 4,5 para texto pequeno de badge/caption (combinação real: `badges.css`, `components.css`, `premium-operations.css`). Alinhadas aos pares já validados com AA em `tokens.css`: `#087a4d` (**4,87:1**) e `#805000` (**6,26:1**). Matiz praticamente idêntico; `danger`/`info` mantidos (já passam).

3. **`.pp-badge--primary`**: azul de marca vivo `#1d6fe9` sobre `#e8f2ff` = **4,13:1**. Passou a preferir `--pp-brand-primary-text` (fallback encadeado) → **5,17:1**.

4. **Tema claro A360**: `.nav-link.active.bg-info { color:#fff !important }` sobre o ciano `#0dcaf0` (não redefinido em lugar nenhum) = **1,96:1** — caso real no tab “Produtos” (`Cadastros.cshtml:42`, que tem `active bg-info text-dark`). `.bg-info` saiu do grupo de “branco explícito”; segue a convenção do próprio Bootstrap (tinta escura sobre ciano).

**Limites conhecidos (documentados, não forçados — identidade visual é baseline aprovada):**

- Botões/badges com fundo de marca: branco sobre `#1d6fe9` vivo = 4,66:1 (✓; ajuste já feito pela própria v2151); sobre o valor-base de tokens `#1f73f1` seria 4,39:1 (< 4,5 para texto pequeno, ≥ 3:1 como UI não-texto) — mesma classe do próprio Bootstrap (`#0d6efd` ≈ 4,14:1). A cor de marca não foi alterada.
- Bordas decorativas de contêiner: `border-default` 1,46:1 / `border-strong` 2,52:1 — bordas não-carregam-informação não exigem 3:1 (WCAG 1.4.11 cobre controles/foco).
- Acentos decorativos da camada v2139: âmbar `#f59e0b` 2,15:1, verde `#16a34a` 3,30:1 — uso ornamental/gráfico, nunca texto funcional.
- Ícone decorativo só em `Administrativo360/Index.cshtml:47` (`badge bg-info bg-opacity-25 text-info p-3`) — ícone de card, não texto.

### 3.3 Gate

CSS puro → **build verde** (gate acordado para mudanças puramente CSS). Arquivos alterados neste commit: `tokens.css`, `accessibility.css`, `charts.css`, `forms.css`, `badges.css`, `v2000-premium.css`, `v2126-form-feedback.css`, `v2132-premium-global.css`, `v2134-premium-qa.css`, `administrativo360-tema-claro.css` + este documento.

Pós-edit, verificado por grep: `--pp-focus-ring` definido apenas em `tokens.css` (canon) e `v2150` (redundância idêntica documentada); zero consumidores do `var(--pp-focus)` nu ou dos antigos valores-sombra.

## 4. C11.4 — Viewports reais e interações (pendente)

Requer app rodando (fora desta máquina em momento de commit):
- Viewports 1440 / 1024 / 768 / 390 reais — validar alert v2132 × sidebar e a escala `--pp-z-topbar`.
- Backlog registrado: botões JS de triagem/estorno.
- View fiscal ADM360 (r4c:180): teclado, toque e foco.
