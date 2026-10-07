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

## 4. C11.4 — Viewports reais e interações (concluída)

**Contexto da execução**: dev no ar (Web `https://localhost:52977` / API `51977`), navegador real com sessão demo (gestor@santacasa-demo.example). Banco do app dev = **`plantaopro_test`** (user-secrets `ConnectionStrings:Default`); a v2314 já existia lá e foi aplicada ad hoc também em `postgres` (inócuo) — registrar no guia de homologação/IIS.

### 4.1 Wire-up dos botões de triagem/estorno (escopo r4c:180)

| Peça | Arquivo |
|---|---|
| Fila de triagem (GET) + abrir/reabrir + resolver | `Adm360DocumentosXmlWebController.cs` · `Views/Administrativo360/DocumentosXml/Triagens.cshtml` (nova) |
| Estorno de resposta de cotação | `Adm360CotacoesWebController.cs` · `Views/Administrativo360/Cotacoes/Detalhes.cshtml` (botão + modal) |
| DTOs da fila | `Models/Administrativo360CotacoesXmlModels.cs` (`TriagensFilaViewModel`, `TriagemDocumentoViewModel`, `UsuarioResponsavelViewModel`) |
| Acesso ao menu | `Views/Shared/_SuprimentosNav.cshtml` + botão “Fila de Triagem” no header de `DocumentosXml/Index.cshtml` |
| Foco em modais (delegado) | `wwwroot/js/plantaopro-ui.js` (`wireBootstrapModalFocus`) |
| Seed demo ADM360 (docs em quarentena, cotação c/ respostas, eventos) | `database/pgadmin/administrativo360_demo_completo.sql` |

Políticas: gate de rota inalterado (`SaasRouteGuard`, match-exato c/ fallback VER); políticas finas só na API — estornar = `Adm360.AprovarResposta`; triagens GET = `Adm360.ImportarXml`; abrir/resolver = `Adm360.VincularDocumentos`. Sem JS próprio além do Bootstrap (modais + handler de foco global já existente).

### 4.2 Validação funcional ponta a ponta (browser real)

**Teste 1 — Estorno** (cotação `a3600000-…-030`, resposta `e310fe2a-…` em `REJEITADA_PELO_PORTAL`):
- Modal abriu com foco no textarea; POST via form → toast canônico (lido em `#pp-toast-data`): *“Resposta estornada: devolvida à fila de transmissão com histórico preservado.”*
- DB: resposta voltou a `NA_FILA` (as demais 4 respostas `ACEITA_PELO_PORTAL` intocadas); evento `ESTORNO` em `adm360_eventos` (`entidade=COTACAO_RESPOSTA`, `dados` JSON c/ cotacao_id + justificativa + registrado_em); cotação segue `PRONTA_PARA_ENVIO`/`RECEBIDA`, não cancelada.

**Teste 2 — Triagens** (doc `a3600000-…-051`, chave `3526099999…0020`, motivo DESTINATARIO_NAO_AUTORIZADO):
- Abrir triagem: responsável gestor (`d3f6584c-…-7511`), prazo local (now+30min) e observação → toast *“Triagem aberta: responsável e prazo atribuídos ao documento.”*; a linha passou a exibir responsável, prazo e os botões Resolver/Reabrir.
- Resolver triagem: justificativa ≥ 10 chars → toast *“Triagem resolvida: documento liberado da quarentena (a conferência segue decisão separada).”*; linha saiu da fila (9 → 8).
- DB pós-resolver: `quarentena=false`, `motivo_quarentena` limpo, `status_conferencia=PENDENTE` **intocado** (semântica B7: resolver ≠ conferir), campos `triagem_*` preenchidos; eventos `TRIAGEM` “aberta” (`prazo_utc 15:25Z`, observação, chave) e “resolvida” (justificativa) presentes e auditáveis.

### 4.3 Bugs encontrados e corrigidos nesta etapa

1. **Interpolação Razor de switch** — `@prop switch { … }` sem parênteses interpola só a propriedade; o resto sai como texto literal (quebrava o badge NF-e). Fix: `@(prop switch { … })` (`Triagens.cshtml` L85). Validado: badge “NF-e 55”.
2. **Foco em modal** — `autofocus` nativo não funciona em elemento oculto no parse. Fix: handler delegado `shown.bs.modal` em `plantaopro-ui.js` (`wireBootstrapModalFocus`: prefere `[autofocus]`, senão 1º focusable), registrado no DOMContentLoaded; cobre `_Layout.cshtml` e `_AuthLayout.cshtml` (carregado com `asp-append-version`). Validado: `activeElement` = select de responsável ao abrir o modal.
3. **`<form>` truncado dentro de `<table>`** — modais renderizados como filhos de `<tbody>` sofrem foster-parenting do parser de tabelas HTML: o `<form>` era aberto e fechado imediatamente (elemento vazio no DOM) e os controles eram hoistados para fora dele → `requestSubmit()`/submit viravam no-op silencioso. Fix: blocos de modal movidos para **fora do `<table>`** (foreach dedicado após a tabela, dentro do card); botões das linhas mantêm `data-bs-target`. Validado: `form.querySelectorAll('input,select,textarea').length = 5` e os fluxos de 4.2 submetendo de fato. **Regra adotada daqui em diante: modais com form fora de tabelas.**

### 4.4 Métricas de viewport (medidas reais no browser)

Método: janela real 1000×700 (layout efetivo 985px) + iframes offscreen 1440/1024/768/390 carregando as views novas/modificadas (`Triagens`, `Cotacoes/Detalhes`).

| Viewport | overflowX (scrollW − innerW) | `--pp-focus-ring` (computed) | Ordem de tabulação | Alvos < 44px relevantes |
|---|---|---|---|---|
| 1440 | −15 (zero) | `#1d6fe9` ✓ | skip-link → brand → shell → nav → conteúdo ✓ | só chrome legado |
| 1024 | −15 (zero) | `#1d6fe9` ✓ | idem ✓ | só chrome legado |
| 768 | −15 (zero) | `#1d6fe9` ✓ | idem ✓ | chrome legado |
| 390 | −15 (zero; scrollW 375) | `#1d6fe9` ✓ | idem ✓ | 2 itens de chrome legado \* |
| janela real (985) | −15 (zero) | `#1d6fe9` ✓ | skip-link primeiro ✓ | chrome legado |

\* em 390 os únicos alvos pequenos restantes são chrome pré-existente (botão “Fechar ajuda” 42px e link de usuário 16px), não as views novas; os botões de ação das linhas atingem ≥ 44px pela regra `min-height:44px` do breakpoint pequeno já existente em `app-shell.css`.

Pontos verificados nas duas views em todos os viewports:
- `--pp-z-topbar` = **1020** no computed de `:root`; consumidor vivo encontrado: `nav.mobile-navigation.d-lg-none` (z 1020) — alert v2132 (migrado p/ o mesmo token em C11.1) e sidebar/modais sem colisão de camada.
- Shell em 390: sidebar `position:fixed` transladada p/ **−336px** (off-canvas, não cobre conteúdo), `mobileMenuToggle` visível (flex), `sidebarCollapse`/`sidebarToggle` ocultos, `main` em left 0px com largura 375 — sem sobreposição nem scroll horizontal.
- Contagem de elementos focáveis estável entre viewports (180 na Triagens / 160 nos Detalhes) — nenhum controle “some” em telas menores.

### 4.5 Caveats documentados (decisão: registrar, não forçar agora)

- **`datetime-local` → UTC**: o form envia horário local sem offset; o binder (`.UtcDateTime`) grava o instante correto em UTC (validado: 12:25 local BRT → `15:25Z` na DB e no evento). A coluna Prazo, porém, exibe o relógio UTC do servidor (Dapper/Npgsql retorna `DateTimeKind.Utc`) → o usuário vê o prazo 3h à frente do valor digitado. Ordenação e vencimento usam o instante absoluto (incorruptíveis); só o rótulo fica em UTC. **Backlog**: exibir prazo no fuso do cliente.
- **Toque coarse-pointer**: não existe regra `@media (pointer:coarse)` no CSS do app; os alvos pequenos remanescentes seguem o padrão legado global (btn-sm ~31–42px em desktop). As views novas respeitam o padrão existente e ganham ≥ 44px no breakpoint pequeno. **Backlog**: regra coarse-pointer global.
- Banco dev `plantaopro_test` e v2314 aplicada ad hoc em `postgres`: anotar no guia de homologação/IIS.

### 4.6 Gate

Mudanças do commit: C# (controllers/models) + Razor + JS + seed → gate completo: **build verde + suíte xunit re-executada antes do commit**. Resultado final: **1049/1049**. Obs.: na primeira execução completa o teste `Aceite12_XmlRepetido_NaoDuplica` falhou isoladamente (1048/1); passou na execução isolada e na re-execução completa — flake de corrida paralela sobre o banco compartilhado `plantaopro_test` (o teste escolhe dinamicamente o “1º documento fora da quarentena” por `data_emissao DESC` enquanto outras classes fazem import/delete em paralelo; mesma família do flake Aceite17 já registrado no backlog). Sem mudança de código em resposta (infra de teste, fora do escopo C11).

Arquivos: `Adm360CotacoesWebController.cs`, `Adm360DocumentosXmlWebController.cs`, `Administrativo360CotacoesXmlModels.cs`, `Cotacoes/Detalhes.cshtml`, `DocumentosXml/Index.cshtml`, `DocumentosXml/Triagens.cshtml` (novo), `_SuprimentosNav.cshtml`, `plantaopro-ui.js`, `administrativo360_demo_completo.sql` + este documento.
