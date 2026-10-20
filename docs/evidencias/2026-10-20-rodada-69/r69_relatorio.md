# Rodada 69 — Auditoria WCAG AA de contraste + correções de tokens

Data: 2026-10-20 · Base: `4eb0d31` (`main == origin/main`, ponta da Rodada 68) · Plano: B2 (Design, cores, legibilidade), item R69 de `r66a0-plano-50-sprints.md`: *"auditoria WCAG AA de contraste (texto/fundo em telas densas, alertas, badges, tabelas) + correções de tokens; nunca cor de função sobreposta à cor da letra de forma ambígua."*

## 1. Recon — arquitetura efetiva de estilos

- Toda página autenticada carrega `_Layout.cshtml`: `css/vendor/bootstrap.min.css` (vendorizado, 5.3) → `bootstrap-compat.css` (só `color-scheme`) → `plantaopro.css`, que faz **@import** de `design-system/tokens.css` + 24 módulos (badges/tables/feedback/states etc.) → folhas `v2000…v2151` → `accessibility.css`. O v2151 é a camada canônica final (carregado por último, vence cascatas de mesma especificidade).
- `tokens.css` é a fonte única dos tokens `--pp-*`; o azul institucional já tinha sido ajustado em v2150/v2151 (`--pp-brand-primary` → `#1d6fe9`, texto `--pp-brand-primary-text` → `#1a5fd0`).
- Uso real de classes funcionais nos views (Select-String): `text-success/danger/warning/info` 586, `bg-*` 548, `badge bg-*` 382, `btn-outline-*` 337, `alert-*` 178, `btn-*` sólidos 76, `pp-badge--*` 45, `table-*` 45 → **os utilitários do Bootstrap são as superfícies densas dominantes**.
- Anatomia interna do vendor confirmada por leitura: `.text-*` consome `rgba(var(--bs-*-rgb))` (variável de raiz, superável); `.badge` força `color:#fff`; `.alert-*`/`.table-*` usam pares prontos emphasis/subtle (já AA); **botões sólidos e outline têm hex prontos no CSS compilado** (ex.: `--bs-btn-color:#ffc107`), exigindo regra explícita na camada de ajuste.

## 2. Método — auditoria determinística versionada

Script: `r69_audit_contraste.ps1` (nesta pasta). Pipeline:

1. **Cascade real**: as 73 folhas ativas são parseadas na ordem exata dos layouts (incluindo a expansão dos @import e os anexos por view). Walker de chaves achata `@media/@supports`; `@keyframes/@font-face` ignorados.
2. **Resolução**: mapa global de custom properties `:root` (última definição vence) + resolver `var()` com fallback, hex/rgb/rgba com `var()` nos canais (tripletes `--bs-*-rgb` do Bootstrap), fundição de alfa sobre branco.
3. **Pares**: (A) tokens canônicos × superfícies (curadoria explícita, 47 pares); (B) toda regra com `color`+`fundo` autocontidos no cascade efetivo (vencedor por par); (D) paleta funcional efetiva do Bootstrap (badge/`.text-*`/contornos com tinta extraída do vendor+v2151, last-wins); (C) blocos `<style>` e regras inline dos views.
4. **Classificação WCAG 2.1**: piso 4,5:1 para texto normal; 3,0:1 para objeto gráfico não-texto (ícones); componentes `:disabled/.disabled` excluídos (1.4.3 "apresentação visual de componentes inativos"); fundo translúcido fino (α≤0,30) classificado **CTX** (cor efetiva depende da superfície por baixo — ex.: navy da sidebar/hero) e reportado, não punido.
5. Razão calculada pela fórmula de luminância relativa WCAG; saída com resumo `FALHAS=N` e marcador `R69_CONTRAST_OK`/`R69_CONTRAST_FAILHA` (exit code).

## 3. Baseline (antes das correções)

Arquivo: `r69_audit_baseline.txt` — **FALHAS=13** (de 392 pares medidos):

| Par | Medido | Piso | Onde dói |
| --- | --- | --- | --- |
| `.text-warning` / `badge bg-warning` / `btn-outline-warning` (amarelo `#ffc107`) | 1,63:1 | 4,5 | badges, tabelas, labels de estado |
| `.text-info` / `badge bg-info` / `btn-outline-info` (ciano `#0dcaf0`) | 1,96:1 | 4,5 | idem |
| `.text-primary` sobre a página `#f4f8fb` | 4,37:1 | 4,5 | tinta fora de cartões brancos |
| `.text-success`/`.text-danger`/`.text-secondary` sobre a página | 4,24–4,39:1 | 4,5 | idem (no branco estavam no limite 4,53) |
| `--pp-text-soft` (`#64748b`) sobre a página | 4,46:1 | 4,5 | textos suaves do design system |

Itens contextuais documentados (não falha): overlays translúcidos sobre superfícies escuras garantidas por CSS no mesmo arquivo (ex.: `.nav-link-app` na sidebar navy, botão do hero `.operation-command__hero` sobre gradiente navy/teal) → **CTX=10**; ícones/superfícies de ícone avaliados como não-texto passam em 3,0 (ex.: `.empty-state-icon` 4,18–4,29); botões desabilitados (`.btn-primary:disabled` branco sobre `#a9c6f5` = 1,74) excluídos por 1.4.3.

## 4. Correções aplicadas (2 arquivos)

### 4.1 `design-system/tokens.css`
- `--pp-text-soft`: `#64748b` → **`#5c6c81`** — 5,36:1 sobre branco e 5,02:1 sobre a página (era 4,76/4,46). Mesmo matiz slate, margem confortável.

### 4.2 `design-system/v2151-wp-c-ajuste-canonical.css` — nova seção 8
Única camada que carrega por último → vence todas as cascatas para `--bs-*`. Valores escolhidos por medição (precisam passar nos **dois** piores fundos do produto, branco e página `#f4f8fb`), com unificação nos tokens canônicos quando a família coincide:

| Token `--bs-*` (e `-rgb`) | Anterior (vendor) | Novo | Branco | Página | Decisão |
| --- | --- | --- | --- | --- | --- |
| primary | `#1d6fe9`/`13,110,253` (misto) | **`#1a5fd0`**/`26,95,208` | 5,85 | 5,48 | adota `--pp-brand-primary-text` (já existia para links) |
| success | `#198754` | **`#087a4d`**/`8,122,77` | 5,38 | 5,04 | unifica com `--pp-success` |
| warning (papéis de texto/borda/badge) | `#ffc107` | **`#b45309`**/`180,83,9` | 5,02 | 4,70 | âmbar escuro; `.btn-warning` sólido **permanece amarelo com tinta preta** (14,6:1, já AA) |
| info | `#0dcaf0` | **`#0e7490`**/`14,116,144` | 5,36 | 5,02 | mantém identidade ciano (o módulo ADM360 já usa azul institucional localmente, sem conflito) |
| danger | `#dc3545` | **`#b42336`**/`180,35,54` | 6,49 | 6,08 | unifica com `--pp-danger` |
| secondary | `#6c757d` | **`#66727c`**/`102,114,124` | 4,93 | 4,61 | mínimo deslocamento |

Além do `:root`, regras explícitas para as classes com **hex prontos** no vendor: botões sólidos `.btn-success/.btn-danger/.btn-info/.btn-secondary` (fundo+borda+hover+ativo) e contornos `.btn-outline-primary/-success/-warning/-info/-danger/-secondary` (tinta/borda + hover sólido com branco ≥ 4,5). Links (`.link-*`, `.btn-link`) seguem `--bs-link-color` (já `#1a5fd0` em v2151) — cobertos pelo mesmo ajuste.

### 4.3 Decisão documentada — tokens clínicos "branco sobre sólido"
`--pp-clinical-teal/cyan/green/amber` e `--pp-color-accent` mediam 3,19–3,74:1 com branco. Recon de uso: **nenhuma** folha ativa os usa como fundo sólido com texto — apenas borda-esquerda de toast/banners, dots de KPI e fallback de barra de progresso (objeto gráfico não-texto). A auditoria os avalia com piso 3,0 (todos passam); não se alterou a identidade clínica. Se uma tela futura usá-los como badge com texto, a guarda de R74 capturará o uso.

## 5. Prova final e contrato travando o piso

- **Auditoria final**: `r69_audit_final.txt` — `RESUMO: OK=323 INFO=8 CTX=10 FALHAS=0` → **`R69_CONTRAST_OK`** (exit 0).
- **Contrato versionado**: `backend/PlantaoPro.Tests/WcagContrasteAaContratoTests.cs` — 6 fatos medem do fonte os pares efetivos (tokens + raiz `--bs-*` em ordem de cascade real + tintas de contorno) e falham abaixo de 4,5:1 (texto) / 3,0:1 (não-texto). Cresce a suíte 1653 → **1659**. A R74 estenderá com varredura de anti-padrões (cor de função sem texto).

## 6. Gates (todos verdes)

| Gate | Resultado |
| --- | --- |
| `dotnet build backend\PlantaoPro.sln -c Release --no-incremental` | 0 erros · 80 avisos (idêntico ao baseline) |
| Gate de casa (chaves normalizadas, strip `[^x00-x7F]`) | Now=160 Base=160 · **DIFF=NENHUMA** · erros=0 |
| Suíte integral Release | **1659/1659 aprovados** (1653 anteriores + 6 novos) |
| Docscan U+00C3 (`r62_docscan.ps1`) | ARQ_VERIFICADOS=7 · **DOCSCAN_BAD=0** |

## 7. Observações (fora do escopo desta rodada)

- `Views/Administrativo360/OrcamentoImprimir.cshtml` e `ValeImprimir.cshtml` linkam `~/lib/bootstrap/dist/css/bootstrap.min.css`, mas `wwwroot/lib` **não existe** (404 inofensivo: a mesma paleta do `css/vendor` entra via `_Layout`). Candidato a limpeza em rodada futura.
- Limpeza one-off dos dev DBs inchados (`plantaouro`/`plantaouro_test`) continua pendente como item lateral (era do bloat do catálogo, congelado em R68).
- Chave Gemini segue bloqueada no projeto Google (403 `API_KEY_SERVICE_BLOCKED`) — ação externa; revalidar com `scripts/ai-external-probe.ps1`.

## 8. Commits

1. `web (rodada 69 r69): tokens e paleta bootstrap com piso de contraste wcag aa` — `tokens.css` + `v2151-wp-c-ajuste-canonical.css`
2. `tests,docs (rodada 69 r69): contrato de contraste aa e auditoria da rodada com evidencias` — `WcagContrasteAaContratoTests.cs` + esta pasta de evidências

Push: `origin main` (confirmado no fechamento).
