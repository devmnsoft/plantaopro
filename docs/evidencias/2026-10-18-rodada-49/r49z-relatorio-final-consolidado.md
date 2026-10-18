# R49 · r49z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R48 (`33c4c33`, suíte 1594). Plano: `r49a0-acessibilidade-basica-plano.md`.

## Escopo da rodada

Roadmap R49 (diretriz UX r24a1): **acessibilidade básica (aria/labels/foco) + contraste revisitado** — sem
mudança de ids/layout, com contrato permanente travando o resultado.

## O que foi entregue

### 1. Nomes acessíveis em todo formulário e botão só-ícone

| Passada | Volume | Como |
|---|---|---|
| Automática (`r49_fix1.ps1`) | **215 inserções** em 57 arquivos (117 inputs, 91 selects, 7 textareas) | dicionário curado pt-BR por atributo `name`; inserção antes do `>` final da tag; fallback humanizado camelCase |
| Manuais | **8** | 5× `Parametrizacoes\*.cshtml` → `aria-label="@item.Key"` (confere o label visível da linha); `Usuarios\Form.cshtml` select disabled → "Cliente"; `MinhaCentral\Index.cshtml` → "Prazo" e "Responsável" |
| Botões só-ícone (`r49_fixbtn.ps1`) | **12** | "Buscar" ×4, "Editar conta/local/parceiro/produto", "Remover item" ×4 |

Regra de nome acessível do contrato: campo tem `id` \| `asp-for` \| `aria-label` \| `aria-labelledby` \|
`placeholder` \| `title` **ou está envolto em `<label>`** (associação implícita; janela de 90 chars antes do
início da tag). Checkboxes de `Notificacoes\Preferencias.cshtml` permanecem sem atributo de propósito: estão
envoltos em `<label class="switch-row">`.

Auditoria final: **inputs+selects+textareas=882, buttons=670, VIOLATIONS=0** (espelhada permanentemente no
contrato C# — ver item 4).

### 2. Contraste revisitado (computado, não anotado)

Fato novo do recon: o **azul AA já existia** — `v2151-wp-c-homologacao.css` reescreve
`--pp-brand-primary` para `#1d6fe9` (~4,66:1 sobre branco), mas o `tokens.css` canônico tem `#1f73f1`
(4,39:1 = FAIL) e `_AuthLayout` nem carregava o v2150/v2151 (parava no v2148). Correção: **v2150+v2151
adicionados ao `_AuthLayout`** (paridade com `_Layout`, que os carregava por último). Pares críticos medidos
pelo contrato a partir dos hex reais dos arquivos (todos ≥ 4,5:1 sobre branco):

| Par | Razão |
|---|---|
| text-primary | 16,91 |
| text-secondary | 7,02 |
| text-muted | 5,59 |
| text-soft | 4,76 |
| success | 5,38 |
| warning | 6,85 |
| danger | 6,49 |
| info | 6,48 |
| brand-primary (efetivo, v2151 `#1d6fe9`) | 4,66 |

### 3. Fundação de foco/movimento efetivamente carregada (**D-R49-5**, decisão nova da rodada)

A smoke Web revelou que `accessibility.css` (skip-link CSS, `:focus-visible` 3px,
`prefers-reduced-motion`) existia em disco **mas não era referenciado por nenhum layout** — os skip-links dos
dois layouts renderizavam sem estilo. Correção: `accessibility.css` linkado nos dois layouts **após o v2151**
(com comentário R49) + fallbacks próprios nos `var()`s (`--pp-z-skip,1100`, `--pp-focus-ring,#1d6fe9`, etc.)
por causa de `tokens.css` também não ser carregado globalmente (padrão da casa: tokens duplicados nos blocos
versionados). O fato 3 do contrato agora **trava essa carga** (posição após v2148 nos dois layouts).

### 4. Contrato permanente — `R49AcessibilidadeContratoTests` (5 fatos)

1. `Formularios_TodoCampoTemNomeAcessivel` — scanner de fonte sobre todos os cshtml (regra do item 1).
2. `BotoesSoIcone_TemAriaLabelOuTitle` — botões só-ícone precisam de `aria-label` ou `title`.
3. `Layouts_TemLangESkipLinkEMainEEAzulAAPorUltimo` — `lang`, skip-link, `#mainContent`, v2151 após v2148
   nos dois layouts, v2150 antes do v2151 no `_Layout`, **accessibility.css após v2148 nos dois** (D-R49-5).
4. `Contraste_ParesCriticosMedemQuatroCincoOuMais` — hex parseado dos arquivos reais; WCAG relativo ≥ 4,5:1.
5. `Fundacao_FocoVisivelMovimentoReduzidoESkipLinkCss` — `:focus-visible`, `outline`,
   `prefers-reduced-motion:reduce`, `.skip-link` presentes na fundação.

### 5. Bugs curados na passada de correção (registro de fatos)

- **Parser de tags (`Get-TagText`)**: contagem de profundidade `<`/`>` sem noção de aspas. Duas quebras:
  (a) expressão Razor com operador `>` dentro de valor de atributo — a inserção caiu **dentro** do `@(...)`
  em `Cotacoes\Detalhes.cshtml` L294 e quebrou o build (CS1003/CS0103/CS0019); (b) tags terminadas em `/>` —
  95 ocorrências de ` / aria-label="..."` (compilavam, inconsistentes). Correção por cirurgia de string
  (`r49_fix3.ps1`): expressão restaurada para `@(item.FatorConversao > 0 ? ...)` com `aria-label` antes do
  `value=`, e normalização global das 95.
- **Encoding**: PowerShell 5.1 lê `.ps1` sem BOM como Windows-1252 — o dicionário acentuado do `r49_fix1.ps1`
  chegou duplamente codificado nos 57 arquivos ("SituaÃ§Ã£o"). Reparo pontual só dentro dos spans
  `aria-label="..."` (10 pares mapeados por char codes); verificação final: **0 pares duplicados restantes**.
- **Quirk do canal/harness**: regex verificado como correto (char codes do padrão e do texto conferidos um a
  um) retornou 0 matches sem erro contra texto que continha o alvo — bisseção mostrou comportamento
  inconsistente de prefixos. Contorno adotado: string surgery (`IndexOf`/`Substring`), sem regex, em todos os
  scripts de correção R49. Registrado como observação; root cause não isolada nesta rodada.
- **Corrupção de palavras do canal** (D-SESSÃO-1, recorrente): confirmação sempre por char codes
  (`plantaopro` = 112,108,97,110,116,97,111,112,114,111; "ABERTA" = 65,66,69,82,84,65).
- **Contrato novo falhou 3x antes de compilar**: ternário direto em interpolação (CS8361 ×2 — envolvido em
  parênteses via variável intermediária), local function sem tipo no parâmetro (`double Lum(hex)` → `string
  hex`), e regra de label envolvente comparando **um caractere** com a palavra (`janela[lt+1].ToString() ==
  "label"` → `StartsWith("<label")`). Este último estava mascarando os 4 checkboxes de Preferencias que a
  auditoria PS (implementação correta) aceitava.

## Evidências

- Build real `dotnet build backend\PlantaoPro.sln -c Release --no-incremental` → **exit 0**; gate de casa
  multiset vs `r17_pristine_build.log` → **160/160, DIFF=NENHUMA**.
- Classes dirigidas (R49 + higiene + R39 tenant): verde. Suíte integral: **1599/1599** (1594 base R48 + 5
  fatos novos). As execuções 2 e 3 caíram em **um flake da família conhecida**
  (`R19DapperBindingGuardTests.Db_CentralClientes_SqlCompletaDaFonte_…`, "totais inferior a página lida";
  na 3ª os apps API+Web estavam vivos no mesmo banco); protocolo imutável aplicado (isolate ×5 →
  **5/5 verde**) e 4ª execução integral, ambiente limpo, fechou **verde 1599/1599** —
  `%TEMP%\opencode\r49_suite.log`, `r49_suite2..4.log`, `r49_flake_1..5.log`.
- Smoke Web (API 51976 + Web 51977, banco `plantaopro_test`): login demo
  `gestor@santacasa-demo.example` via form + antiforgery (`WebRequestSession`; o GET do token precisa usar a
  mesma sessão do POST, senão 400) → redirect `/ClientePortal/Index`; página renderizada com
  **28 `aria-label`**, v2151 presente, skip-link presente; após a correção D-R49-5, re-smoke confirmou
  `accessibility.css` presente no HTML público de login **e** na página autenticada. Apps parados ao fim,
  portas livres.
- Whitelists acumuladas intactas (25 views R39, termos de higiene, guias RelFin, keeps R45/R46 etc.) —
  `RepositoryHygieneContractTests` + `R39TenantSoParaSuperAdminContratoTests` verdes.

## Escopo "básico" explicito (D-R49-4) — pendências documentadas

Nesta rodada **não entram** (deixadas para R52+): trap de foco em modais, `aria-live` em toasts (a região
`#appLiveRegion` já existe nos layouts — falta a auditoria de uso), landmark audit, tabelas densas
(cabecalhos `th scope`, colunas agrupadas), navegação por teclado completa em widgets custom.

## Commits

| Commit | Conteúdo |
|---|---|
| (a seguir) `web,tests (rodada 49 r49b1)` | passada de nomes acessíveis (215+8+12) + paridade v2151 no auth + fundação css carregada (D-R49-5) + contrato (5 fatos) + correções parser/encoding |
| (este) `docs (rodada 49 r49z)` | este relatório |
