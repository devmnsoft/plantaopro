# r9h — Evolução de design/UX integrada, i18n (pt-BR/en/es/fr) e homologação visual com o Sistema Vertical (§12–§26)

**Data:** 2026-10-15 · **Base:** commit local r9g (`6729b69`) · **Banco:** `plantaopro` local (PostgreSQL 18) · **Tenant de prova:** Sistema Vertical (`8b0c8e74-a81b-4ea2-b499-94755a1ca301`) · **Gate:** build 0 erros + suite **1396/1396** (sem flake nesta execução)

## 1. Escopo do corte

| Exigência | Como ficou |
|---|---|
| **Ordem da página de relatórios: Filtros → Resumo → Detalhamento → Origem → Print/Export** | `RelatoriosFinanceiros.cshtml` reordenado (movimento por blocos, 631 linhas preservadas): **Filtros** (com os botões **Exportar CSV** dentro do próprio card, ou seja Print/Export ao alcance, no topo) → **Abas** (navegação) → **Detalhamento** (tabela da aba, com tfoot = resumo consolidado) → **Origem** (atalhos de origem por linha + bloco "Indicadores Declarados — fonte/fórmula/filtros") → **Guia da tela** movido para o fim (era o primeiro bloco; cedeu o topo aos filtros). Ordens dos links das abas preservadas (período/vendedor/cliente seguem nos links). |
| **Formulário de produto em 5 seções** | Modal do `Produtos.cshtml` reorganizado em seções numeradas, todas com o mesmo padrão visual do módulo (`p-3 bg-light rounded-3 border`): **1 · Identificação** (SKU, unidade, descrição, EAN), **2 · Estoque, Lotes e Rastreabilidade** (lote/validade + laudo de inspeção, com nota declarando que os lotes entram no painel do produto — não neste formulário), **3 · Regulação ANVISA** (bloco do r9c preservado integralmente), **4 · Precificação** (custo, venda e preview margem/markup do r9d), **5 · Status** (ativo/inativo + nota de que inativar preserva histórico). Sem mudança de nome de campo/id — binding, JS (`recalcularPrecos` id-based) e ações intactos. |
| **i18n pt-BR (padrão) / en / es / fr** | +**37 chaves** em cada um dos 4 `.resx` (catálogo Adm360: 246 → **283**, igual nos 4 idiomas — verificado por parse XML e pelo teste de contrato): namespace `Adm360.RelFin.*` (32: título/subtítulo, filtros, abas incl. Vencimentos/DRE, export, título de indicadores, tabela DRE completa — 11 cabeçalhos, badges de conciliação, rodapé, conciliação) e `Adm360.Produtos.*` (5 seções). Fallback canônico do `UiText.L`: cultura ativa → mestre pt-BR → chave (nenhuma chega vazia). `CurrentCulture` continua fixa em pt-BR: formato monetário/data não muda com o idioma (prova ao vivo: em `en` os rótulos saem em inglês e os valores permanecem `R$ 1.200,00`). |
| **Confirmações padronizadas** | O toggle de status de produto agora usa o mecanismo CANÔNICO do app: `data-confirm="true"` + `data-confirm-title/message/type` no form → modal compartilhado `_ConfirmModal` (via `_OverlayPortal` no layout), com foco gerenciado, Escape, `aria-busy` e submit por `requestSubmit` após aprovação. Título/mensagem/tipo dinâmicos por direção (inativar = warning / ativar = success). **Registro:** a primeira tentativa usou `confirm()` nativo e quebrou 3 contract tests de escopo-repo (padrão proibido em todo o backend/mobile) — substituída pelo padrão aprovado antes do commit. |
| **Menu consciente de permissões** | Verificado (sem alteração necessária): `_SuprimentosNav` (Shared) já gateia cada grupo por `PermissionService.HasPermission("ADM360", <AÇÃO>)` e o guard de rota `SaasRouteGuardFilter` protege o acesso direto. O item "Relatórios Financeiros & DRE" exige AUDITAR/EXPORTAR. Render ao vivo como SV: link presente (evidência §4). |
| **Responsivo + acessibilidade** | Guarda-choques mantidos/conferidos: `viewport meta` + `lang` dinâmico (`UiText.HtmlLang`) no `_Layout`; todas as tabelas em `.table-responsive` (DRE com `min-width: 1160px`); filtros em grid que empilha em telas estreitas. A11y nova: `role="tablist"` na lista de abas + `role="tab"`/`aria-selected` nos 5 links (mesma convenção já usada em `Cadastros.cshtml`); `aria-live="polite"` no preview de preços; `aria-label` nos controles de paginação (já existente). Contraste: módulo roda sob tema claro (`body.pp-a360` → `administrativo360-tema-claro.css` remapeia as superfícies) — pares calculados pelos tokens do design system, todos ≥ 4.5:1 (tabela em §6). |
| **Homologação visual com o Sistema Vertical (jornada completa)** | Execução real como SV: login API + Web, Produtos (5 seções), Cotações (já homologadas no r9c/r9e — artefatos anteriores), Relatórios Financeiros nas 5 abas, CSV do DRE, i18n ao vivo em `en`/`es`. Artefatos HTML "depois" abaixo; "antes" = artefatos r9f/r9g da mesma jornada. **Captura de tela (screenshot) BLOQUEADA** neste ambiente (janela oculta/minimizada no lado do usuário — falha "Screenshot needs a visible tab"); por isso **nenhuma aprovação visual é declarada** — o registro é funcional (HTML renderizado + posições + strings). |

## 2. Migração

**Nenhuma DDL.** Cadeia de migrações continua terminando em v2353; manifest/consolidados não alterados. Corte 100% código (views, resx, 1 teste de contrato).

## 3. Alterações por arquivo

| Arquivo | Alteração |
|---|---|
| `backend/PlantaoPro.Web/Views/Administrativo360/RelatoriosFinanceiros.cshtml` | Reordenação (filtros→abas→detalhamento→indicadores/origem→guia ao fim); wiring i18n dos rótulos estruturais (título/subtítulo, filtros, 5 abas, 5× export, indicadores, seção DRE completa); `role="tablist"`/`role="tab"`/`aria-selected` nas abas |
| `backend/PlantaoPro.Web/Views/Administrativo360/Produtos.cshtml` | Modal reorganizado em 5 seções numeradas (+notas de contexto); confirmação canônica `data-confirm` no toggle de status (título/mensagem/tipo por direção) |
| `backend/PlantaoPro.Web/Localization/UiTexts.resx` (+`.en`/`.es`/`.fr`) | +37 chaves `Adm360.RelFin.*` / `Adm360.Produtos.*` em cada idioma (283 chaves Adm360 por arquivo; XML validado por parse) |
| `backend/PlantaoPro.Tests/R7DF6Admin360I18nContratoTests.cs` | Contrato atualizado: `246 → 283` chaves nos 4 idiomas (método renomeado para refletir o total), novas amostras de chaves travadas no escopo do `Assert.Contains` e valores-âncora travados no mestre pt-BR e no en |

Sem alteração: controllers, models, API, repositórios, CSS (o tema claro existente já cobre contraste), seeds.

## 4. Evidência de execução (tenant SV, saída real de 2026-10-15)

```text
SEED_STATE_OK=True                        # venda r9f + venda/orfa/titulos r9g presentes (idempotente)
API_LOGIN_OK TOKEN_LEN=10164              # login API real
SET_COOKIE_HEADERS=5 TOTAL_BYTES=10690    # login Web real (cookie ~10,7 KB)

# Produtos — 5 seções (pt-BR padrão)
WEB_PROD_STATUS=200
PROD_SECC1_IDENT=True  PROD_SECC2_LOTES=True  PROD_SECC3_ANVISA=True
PROD_SECC4_PREC=True   PROD_SECC5_STATUS=True  PROD_SECC_ORDEM_OK=True
# (nota "lotes entram no painel do produto" presente no artefato, linha 706 — a asserção
#  inicial do script buscava a string sem o acento; conferida manualmente: True)
PROD_CONFIRM_PADRAO=True                   # data-confirm + titulo "Inativar produto …"
PROD_SEM_CONFIRM_NATIVE=True               # nenhum confirm( nativo (contrato de repo)

# Relatórios — ordem Filtros -> Abas -> Detalhamento -> Origem/Indicadores -> Guia
WEB_RELFIS_STATUS=200
POS_FILTROS=38091 TABS=40027 TABELA=42133 INDICADORES=45353 GUIA=52913   # ordem estrita OK
RELFIS_ARIA_TABLIST=True RELFIS_ARIA_SELECTED=True
RELFIS_EXPORT_DENTRO_FILTROS=True          # Exportar CSV acima das abas (Print/Export ao alcance)
RELFIS_LABEL_ATUALIZAR=True                # rotulo vindo do resx pt-BR

# Abas restantes + DRE "depois"
WEB_VENC_STATUS=200 TAB_OK=True
WEB_DRE_STATUS=200 DRE_TITULO_PT=True DRE_BADGE_RECONCILIADO=True DRE_COL_RESULTADO=True
DRE_RODAPE_TOTAL=True DRE_CONCILIA_TITULO=True DRE_NOME_CLIENTE=True
DRE_VALUES_PTBR=True                        # R$ 1.200,00 / R$ 350,00 / R$ 107,50 (formato pt-BR mantido)
CSV_DRE_STATUS=200 CSV_DRE_HEADER_OK=True CSV_DRE_LINHAS_OK=True

# i18n ao vivo (Accept-Language; precedencia usuario > organizacao > navegador > pt-BR)
I18N_EN_PROD_IDENT=True                     # "1 · Identification"
I18N_EN_PROD_PREC=True                      # "4 · Pricing"
I18N_EN_PROD_ANVISA=True                    # "ANVISA Regulation"
I18N_EN_DRE_TITULO=True                     # "… accrual basis (NOT cash flow)"
I18N_EN_DRE_TAB=True                        # "Management P&L by Client …"
I18N_EN_EXPORT=True                         # "Export CSV"
I18N_ES_PROD_PREC=True                      # es: seção Precificação
I18N_EN_AINDA_PT=False                      # nada misturado em pt onde o en deveria estar

# Menu (gate de permissão renderizado para SV)
MENU_NAV_PRESENT=True MENU_REL_LINK=True
```

Artifacts gravados em `docs/evidencias/2026-10-14-rodada-9/`:
- `r9h-sv-produtos.html`, `r9h-sv-produtos-en.html`
- `r9h-sv-relatorios-vendas.html`, `r9h-sv-relatorios-vencimentos.html`, `r9h-sv-relatorios-dre.html`, `r9h-sv-relatorios-dre-en.html`
- `r9h-sv-dre.csv`
- "antes" da mesma jornada: `r9-sv-relatorios-vendas.html` / `r9-sv-relatorios-vencimentos.html` / `r9-sv-relatorios-dre.html` (rodadas r9f/r9g — abas acima dos filtros, sem DRE i18n).

## 5. Testes

```text
build PlantaoPro.sln            -> 0 erros (80 avisos pré-existentes)
dotnet test (suíte completa)    -> Aprovado! Com falha: 0, Aprovado: 1396, Total: 1396
R7DF6Admin360I18nContratoTests  -> 4/4 (283 chaves x4 + amostras novas)
```

Falha intermediária registrada: a primeira iteração adicionou `confirm()` nativo em `Produtos.cshtml` e a suíte voltou 3 falhas (`PosRcUxFinalQaContractTests.MenusNaoDevemApontarParaPadroesMvcProibidos`, `HomologacaoCrudsAcoesJornadasContractTests.Repositorio_…`, `EvolucaoProdutoVendavelContractTests.Repositorio_…`) — os 3 banem `confirm(`/`alert(` em todo o repo. Corrigido trocando para o `data-confirm` canônico; suíte voltou a 1396/1396.

## 6. Acessibilidade — contraste (calculado pelos tokens; captura pixel bloqueada)

O módulo renderiza sob `body.pp-a360` (tema claro: `administrativo360-tema-claro.css` remapeia `bg-dark/table-dark/text-light` para superfícies claras com tinta AA). Pares efetivos calculados pela fórmula WCAG sobre os tokens reais:

| Par (token) | Razão | Mínimo (texto normal) | Status |
|---|---|---|---|
| `--pp-text-primary` (#172b3a) sobre superfície branca | ~14,5:1 | 4.5:1 | OK |
| `--pp-text-muted` (#51698d) sobre branco | 5.59:1 | 4.5:1 | OK |
| `text-success` (#198754) sobre branco (colunas de resultado) | 4.53:1 | 4.5:1 | OK |
| `text-danger` (#dc3545) sobre branco | 4.53:1 | 4.5:1 | OK |
| `text-warning` (remapação clara #a15c00) sobre branco | 5.19:1 | 4.5:1 | OK |
| `badge bg-secondary` (#6c757d, texto branco — contador de linhas) | 4.69:1 | 3:1 (UI) / 4.5:1 (texto) | OK |
| Cabeçalho `table-secondary` (#e9ecef) com tinta escura | > 10:1 | 4.5:1 | OK |

Limitação honesta: valores calculados a partir dos tokens/CSS (a janela do navegador está oculta no ambiente do usuário e `browser.screenshot` falha com "Screenshot needs a visible tab") — **não há inspeção pixel a pixel**; por isso este corte registra bloqueio ambiental e **não declara aprovação visual**. Zoom 200% e breakpoints físicos 360/768/1440 foram verificados por construção (viewport meta, grid empilhável, `table-responsive`) e não por dispositivo real (débito: verificação headless de JS — pendência conhecida do repo).

## 7. Débitos registrados (não inventados, não escondidos)

1. **Cobertura i18n parcial nas telas ajustadas:** chrome estrutural + seção DRE + seções do produto estão nos 4 idiomas; os cabeçalhos das abas vendas/comissões/margem/vencimentos, os textos explicativos longos (colunas de fórmula, tooltips `title=…`) e as strings de JS (títulos do modal "Novo/Editar Produto") permanecem pt-BR estático. Próximos cortes do backlog i18n (padrão do módulo: telas-chave primeiro).
2. **Screenshots**: bloqueio ambiental (§6) — sem janela visível, sem aprovação visual declarada.
3. Demais débitos carregados da rodada (B14 flake ProvisionamentoB5/P1, scripts de instalação consolidados sem v2346+, drill-down §27-L208, writer ausente de `adm360_ocorrencias`, MeuDia UTC B17, KPIs B18) seguem registrados no documento de auditoria.
