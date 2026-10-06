# WP-C — Design System de Homologação (2026-10-05)

## Ambiente e método

| Item | Valor |
|---|---|
| Baseline | `2eaf57b` (local `main` na época; `origin/main` ainda `82b8c26`). Atualização 2026-10-06: cadeia inteira publicada — `origin/main` = `7f43af2` |
| Pacote | `v2151-wp-c-homologacao.css` (novo) + `data-confirm` em 6 views ADM360 + caso ADM360 no `_ScreenGuide.cshtml` + alias de toast no `_ToastMessages.cshtml` |
| Stack dev | API `https://localhost:51977` · Web `https://localhost:52977` · ciclo canônico parar→`run-dev-build.ps1`→`run-dev-start.ps1` |
| Suíte canônica | 839/839 testes aprovados após rebuild do pacote (17s) |
| Validação | Navegador real (OpenCode desktop, pane ≈ 890px), verificação por DOM/computed style + paleta de contraste calculada (WCAG rel. luminance) |
| Emulação de viewport | zoom de `document.documentElement` no Blink para aproximar 1440/768/390px (pane fixo ≈ 890px → zoom ≈ 0,618 / 1,159 / 2,28). Método pronto; capturas PNG pendentes (ver Limitações) |

## Matriz de itens do checklist

| # | Item do checklist | Estado | Evidência |
|---|---|---|---|
| 1 | Fundo claro + superfícies brancas | PASS | body base `#f4f8fb` computado em todas as telas validadas; `.modal-content` com classes `bg-dark text-light border-secondary` resolve para fundo `rgb(255,255,255)` / tinta `rgb(16,33,58)` (J11-WP7) |
| 2 | Azul consistente (sem teal residual) | PASS | context-bar e jornada guiada sem teal; tokens v2151 ativos: `--pp-brand-primary #1d6fe9`, hover `#1a63d8`, active `#1549ab`, `--bs-primary-rgb 29,111,233`, soft `#eaf4ff` |
| 3 | Cores semânticas reservadas a estados | PASS+FIX | ciano `#0dcaf0` usado como tinta decorativa falhava AA (~1,9:1); remapeado para `--pp-info` `#176ba0` dentro de `body.pp-a360 main` (texto, botões outline-info) — ver tabela de contraste |
| 4 | Contraste AA de links pequenos | PASS+FIX | rescan após correção: todas as tintas de link ≥ 5,39:1 sobre branco/base (antes: ciano 1,83–1,96, off-white 1,01, azul-marcas 4,37) |
| 5 | Guia "Como usar" (caso ADM360) | PASS | `.pp-screen-guide__content` renderiza texto do caso Administrativo 360 corretamente |
| 6 | Validação junto ao campo (HTML5 + Bootstrap) | PASS | comportamento nativo confirmado nas jornadas: submit bloqueado antes da confirmação quando há campo required vazio (ex.: lote no ConfirmarRecebimento) |
| 7 | Confirmação em ações críticas (`data-confirm`) | PASS | 9/9 alvos validados de ponta a ponta (abrir diálogo + cancelar = zero mudança de estado) — tabela abaixo |
| 8 | Foco visível | PASS | CSSOM: cobertura ampla `:where(a, button, input, select, textarea, [tabindex]):focus-visible` + anéis específicos (`.btn`, dropdowns, tabs); escopo fora-ADM360 espelhado na seção 5 do v2151 |
| 9 | Dobra inicial em viewports estreitos | PASS (estrutura) | regras ≤991px/≤575px presentes no v2151 (seção 3); verificação visual por capturas pendente (Limitações) |
| 10 | Toast sem renderização de JSON cru (finding #7 WP-B) | PASS+FIX | `_ToastMessages.cshtml` emite `<script type="application/json" id="pp-toast-data">` — validado ao vivo na tela de login (tag `SCRIPT`, `renderedAsText=false`, JSON válido); UI do cartão capturada em `wp-c-toast-login-sessao-encerrada.png`; **fix adicional**: 15 ações financeiras/comerciais gravavam `TempData["SuccessMessage"/"ErrorMessage"]`, que o partial não consumia — nenhum toast era exibido nesses fluxos. Alias adicionado no consumidor (chaves canônicas primeiro); unificação dos pontos de escrita vai para o backlog pós-MVP |

## Diálogos de confirmação (item 7) — 9/9 validados

| # | Tela (ADM360) | Ação | Botão | Resultado |
|---|---|---|---|---|
| 1 | PedidosCompra | AprovarPedidoCompra | success | Abriu + cancelou, zero mudança de estado (via PC-37) |
| 2 | Recebimentos | ConfirmarRecebimento | warning | Sobre `#modalNovoRecebimento`; validação nativa (required: pedidoId/documento/localId/quantidade/lote) precede a confirmação |
| 3 | Inspecoes | DecidirInspecao | warning | Sobre `#modalInspecao` (campos aprovada/reprovada são number; destino padrão ALMOXARIFADO_LIBERADO) |
| 4 | Inventarios | AprovarInventario | success | Abriu + cancelou, zero mudança de estado |
| 5 | Inventarios | CancelarInventario | danger | Abriu + cancelou, zero mudança de estado |
| 6 | TituloPagarDetalhes (título `…91`) | AprovarTituloPagar | warning | Abriu + confirmou — ver mudança de estado registrada abaixo |
| 7 | DocumentosXml/Detalhes | VincularRecebimento | success | Required PedidoId/LocalId ok; cancelou |
| 8 | DocumentosXml/Detalhes | Manifestar | warning | Abriu + cancelou |
| 9 | DocumentosXml/Detalhes | Conferir | success | Abriu + cancelou |

## Contraste — antes/depois das correções do pacote

| Tinta | Uso | Antes (cor/contraste) | Depois (cor/contraste) |
|---|---|---|---|
| Links de conteúdo (×37–41) | Cards e listas | `#1a5fd0` 5,85:1 sobre branco ✓ | idem ✓ |
| Passos da jornada guiada (×4) | `.pp-guided-journey__steps a` | `#1d6fe9` 4,37:1 sobre base `#f4f8fb` ✗ (< 4,5) | `#1a5fd0` 5,48:1 sobre base ✓ (regra com `!important` para vencer a regra genérica de links do v2150 — 12 `:not()`, especificidade 0-13-1) |
| Breadcrumb "XML Recebidos" | `.text-info` | `#0dcaf0` 1,83:1 ✗ | `#176ba0` 5,39:1 ✓ |
| Botão "Dashboard 360" | `.btn-outline-info` | `#0dcaf0` 1,96:1 ✗ | `#176ba0` 5,76:1 sobre branco ✓ (hover sólido `#176ba0` + branco = 5,76 ✓) |
| Botão "Baixar XML" | `.btn-outline-light` | `#f8f9fa` 1,01:1 ✗ (quase invisível) | `#405a80` 7,02:1 sobre branco ✓ (hover sólido com branco) |
| Ícones/códigos `text-info` (17 pts Cadastros) | Acentos decorativos | `#0dcaf0` ~1,9:1 ✗ | `#176ba0` ~5,8:1 ✓ (contra-escopo único no tema claro estendido para todo o conteúdo `main`) |

Verificação de regressão: scan completo de `main a, .card a, a.badge` nas páginas Detalhes (NF-e) e Cadastros após reload — **nenhuma tinta de link abaixo de 4,5:1** sobre branco nem sobre a base `#f4f8fb`.

## Mudanças de estado feitas durante a validação (DB `plantaopro_test`)

| Mudança | Objeto | Justificativa |
|---|---|---|
| Criação (Rascunho) | Pedido `PC-37` (R$ 50,00; fornecedor `d2ec5f24…`; produto `0d96cc88…`) | Alvo real para validar diálogo 1 sem afetar pedido demo |
| Aprovação | Título a pagar `a3610000-…91` → `APROVADO` (badge confirmado na tela) | Evidência ao vivo do pipeline de toast do fluxo financeiro (diálogo 6, confirmado de verdade) |

Demais diálogos (8) validados com **cancelar** — zero efeito de estado.

## Capturas e screenshots

- Método: emulação de viewport via `document.documentElement.style.zoom` no pane do navegador desktop (≈ 800px físicos): zoom **0,556** → ≈ 1440px CSS · zoom **1,042** → ≈ 768px · zoom **2,051** → ≈ 390px (larga real medida pós-zoom: 1439/768/390).
- Capturas salvas neste diretório:

| Arquivo | Conteúdo |
|---|---|
| `cadastros-1440.png` | ADM360 Cadastros: contexto institucional, jornada guiada, guia "Como usar", breadcrumbs, botões outline-info/light, tabela |
| `cadastros-768.png` | Cadastros em viewport de tablet: compactação ≤991px, barra inferior |
| `cadastros-390.png` | Cadastros em viewport mobile: jornada oculta (≤575px), contexto truncado |
| `detalhes-nfe-1440.png` | Detalhes NF-e: jornada com passo ativo, breadcrumb `text-info`, botões Voltar/Baixar XML/Manifestar, status cards |
| `detalhes-nfe-768.png` | Detalhes NF-e em tablet |
| `detalhes-nfe-390.png` | Detalhes NF-e em mobile |
| `wp-c-toast-login-sessao-encerrada.png` | Toast "Sessão encerrada com sucesso." renderizado como cartão na tela de login (pipeline `_ToastMessages` → `#pp-toast-data` → `PlantaoProToast.show`) |

- **Descoberta por captura visual**: o link do breadcrumb da topbar ("Minha Central") era teal `#076773` (`v2132-premium-global.css:21`, `--pp-v2132-brand-strong`) — fora do escopo dos rescans em `main`. Passava AA (~6,6:1), mas quebrava a consistência de azul; corrigido na seção 7 do v2151 (mesmo seletor, carga posterior) → `rgb(26,95,208)` revalidado por computed style nas duas páginas capturadas.

## Limitações e riscos registrados

1. **Emulação de viewport por zoom** (não resize físico da janela): os breakpoints responsivos foram exercitados pela largura CSS resultante (1439/768/390), o que cobre as regras de media query do pacote; não substitui um device real para teste de toque/rolagem fina.
2. **Alias SuccessMessage/ErrorMessage**: correção no consumidor (`_ToastMessages.cshtml`) cobre 15 pontos de escrita (`Administrativo360Controller.Financeiro.cs` ×12, `Vendas.cs` ×1, `AssinaturasController.cs` ×2, `CommercialDemoWebController.cs` ×1); a unificação dos controllers fica no backlog pós-MVP para não expandir o diff do pacote de design.
3. **Contraste fora de escopo `body.pp-a360 main` / topbar**: os rescans cobrem o módulo Administrativo 360 (foco do WP-C) e, a partir desta rodada, o breadcrumb da topbar; outros utilitários decorativos fora desses escopos (portais internos) herdam os tokens globais de link já AA.
4. **Telas de autenticação** usam `_AuthLayout`, que não carrega o design system v2150/v2151 (escopo próprio de layout); o pipeline de toast funciona nelas (`_ToastMessages` incluído) — evidência na captura `wp-c-toast-login-sessao-encerrada.png`.
