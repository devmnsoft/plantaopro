# R70 — varredura responsiva 360/480/768/1024 px

**Data:** 2026-10-20 · **Base:** `7134f1b` (pós-R69) · **Ambiente:** Web `http://localhost:52976` + API `https://localhost:51977` em Release, tenant demo Santa Casa Demonstração.

## Objetivo

Varrer as telas densas do produto (forms, modais e tabelas) nas larguras **360 / 480 / 768 / 1024 px**, medir estouro horizontal e truncamento invisível de forma determinística, corrigir os defeitos na folha CSS responsável e re-varrer até limpo, com evidência reexecutável versionada.

## Metodologia (evidência executável)

- **Bateria:** `r70_sweep.mjs` (Node + Chrome headless via CDP, porta 9333, perfil efêmero). Para cada largura aplica `Emulation.setDeviceMetricsOverride` (alturas 740/850/1024/768; `mobile`+`hasTouch` ≤ 480), faz login real no app e mede **22 páginas × 4 larguras = 88 medições por perfil**.
- **Dois perfis reais:** gestor do tenant demo (`gestor@santacasa-demo.example`) e super admin global (`superadmin@plantaopro.local`, `docs/usuarios-teste.md`). O super admin cobre módulos bloqueados para o gestor (`/CommandCenter`, `/CentralEscala` — que para o gestor dão redirect/AccessDenied).
- **Critérios de medição** (JS injetado por página):
  - `docOv`: `scrollWidth - clientWidth` do documento (estouro de página);
  - `off[]`: elemento com aresta direita além do viewport **e sem ancestral com rolagem horizontal** na cadeia até a raiz — veredito `clip` = conteúdo cortado e inacessível (defeito real); veredito `scroll` = dentro de `.table-responsive`/scroller (padrão estabelecido, aceitável);
  - `trunc[]`: texto de célula/badge cortado por `ellipsis`/`nowrap`/`hidden` sem scroller horizontal na cadeia;
  - exclui off-canvas intencionais (fixed/absolute totalmente fora do viewport) e elementos zerados.
- **Por que CDP próprio:** o browser desktop do harness não expõe resize de viewport, e o emulador CSS `zoom` não responde os media queries neste Chromium (probe registrado: com `zoom = iw/360`, `matchMedia('(max-width:480px)')` permaneceu `false` e `clientWidth` inalterado). CDP `setDeviceMetricsOverride` é a fonte de verdade.
- **Páginas (22):** home, portal do cliente (landing do login), Minha Central, Pacientes (+Create), Agendamentos (+Calendário), Plantões, Triagem/Fila, Pendências, Command Center, Administrativo360 (+Orcamentos/+Vales), FaturamentoSaas, Matriz de Permissões, Relatórios, Usuários, Notificações, Escalas, Central de Escala, Auditoria — mais detalhe de paciente real quando o tenant expõe link (o demo não tinha pacientes na data).

## Baseline (antes da correção)

`r70_sweep_baseline.txt` / `.json` — perfil gestor, 21 páginas × 4 = **84 medições: FALHAS=1**.

| Largura | Página | Defeito |
|---|---|---|
| 1024 | `/Pendencias` | `<label>` + `<input name="periodTo">` (campo "Período — até" do filtro) com `right=1145px` num viewport de 1009px — **136px fora da tela, veredito clip**: conteúdo cortado, sem rolagem, inacessível |

Captura: `r70_shots/baseline/1024_Pendencias.jpg`.

**Causa raiz:** `.action-filter { grid-template-columns: repeat(4, minmax(130px,1fr)) auto auto }` (operations.css) exige ≈ 822px mínimos (4×130px + duas colunas auto de ≈ 134px cada + gaps + padding do form). Com a sidebar (~240px) e paddings do shell, um viewport de 1024px oferece ≈ 688px ao formulário → estouro. As faixas vizinhas já eram cobertas (≤ 900px colapsa para 2 colunas; ≥ ≈ 1122px o template original já cabe) — por isso o defeito só existia entre ≈ 901–1121px e a medição em 1024px o pegou exatamente.

## Correção

`backend/PlantaoPro.Web/wwwroot/css/design-system/operations.css` — folha do próprio componente, onde já viviam os breakpoints ≤ 900 e ≤ 575:

```css
@media(min-width:901px) and (max-width:1299px){.action-filter{grid-template-columns:repeat(3,minmax(0,1fr))}.action-filter .btn{justify-self:start}}
```

Na faixa, o formulário quebra em 3 colunas fluidas (tracks `minmax(0,1fr)` nunca estouram); acima de 1299px o layout original permanece intacto.

**Lição de cascata (documentada):** a primeira tentativa foi na camada canônica `v2151`, mas o `_Layout` renderiza `@section Styles` **depois** do v2151 (linha 69 vs 66) e a página de Pendências carrega `operations.css` nessa section — mesma especificidade, source order vence para a folha da página. Ajustes de componente carregados por section precisam viver na própria folha do componente. O rascunho no v2151 foi removido.

## Final (depois da correção)

| Perfil | Arquivo | Resultado |
|---|---|---|
| Gestor demo | `r70_sweep_final.txt` / `.json` | **FALHAS=0**, medicoes=88 |
| Super admin | `r70_sweep_final-super.txt` / `.json` | **FALHAS=0**, medicoes=88 |

Todos os 4 widths limpos nos dois perfis. Modais e drawers: fluidos por construção (`pp-confirm-modal` `min(34rem,100%)`, modais Bootstrap marginados, drawer tela cheia ≤ 600px) — verificado estático, nenhum estouro registrado na bateria.

## Achados (fora do escopo R70, registrados para rodadas futuras)

1. **404 preexistente:** `/CentralEscala/Substituicoes` não resolve para ninguém — ação sem template de rota num controller com routing por atributos (`[Route("CentralEscala")]`), enquanto `CentralEscala/Index.cshtml` linka para ela. Bug de navegação independente da responsividade.
2. Gestor demo sem permissão do módulo ESCALAS (`/CentralEscala` → AccessDenied `module=ESCALAS`) — comportamento esperado; o layout da página de acesso negado foi medido e está ok nas 4 larguras.
3. `.manager-table table { min-width: 940px }` num container `overflow:hidden`: o tenant demo sem dados renderiza estado vazio (sem tabela); com dados, a tabela vive dentro de `.table-responsive` (rola, não corta) — verificação estática ok.
4. `OrcamentoImprimir.cshtml` / `ValeImprimir.cshtml` linkam `~/lib/bootstrap/dist/css/bootstrap.min.css` inexistente (404 inofensivo, paleta entra via `_Layout`) — herdado de R69, telas de impressão, candidato a limpeza.

## Gates (todos verdes)

| Gate | Resultado |
|---|---|
| `dotnet build backend\PlantaoPro.sln -c Release --no-incremental` | 0 erros; avisos Now=Base=160, comparação chave-a-chave **DIFF=NENHUMA** |
| Suíte integral `dotnet test -c Release` | **1661/1661 aprovados** (1659 pré-R70 + 2 fatos novos do contrato R70) |
| Docscan `r62_docscan.ps1` | ARQ_VERIFICADOS=9, **DOCSCAN_BAD=0** |

Apps derrubados após os gates (portas 51976/51977/52976 livres).

## Arquivos desta pasta

- `r70_sweep.mjs` — bateria reexecutável (tags: `baseline`, `final`, `final-super`);
- `r70_sweep_baseline.{txt,json}` — varridura anterior;
- `r70_sweep_final.{txt,json}`, `r70_sweep_final-super.{txt,json}` — varriduras finais canônicas;
- `r70_shots/baseline/1024_Pendencias.jpg` — captura do defeito pré-correção;
- `r70_relatorio.md` — este relatório.
- Reexecução: com os apps subidos (Web 52976 http / API 51977 https), `node r70_sweep.mjs <tag>` (credenciais via env `R70_EMAIL`/`R70_SENHA`, padrão = gestor demo).
