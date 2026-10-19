# R61 — Auditoria sistemática cadastro×operação (E7) + identidade de telas, mensagens e forms

**Data:** 2026-10-19 · **Fila r53b1 §7 item 7** ("Auditoria sistemática cadastro×operação — o E7 não
executado como epic autônomo"). **Plano prévio:** `r61a0-identidade-cadastro-operacao-plano.md`.

## Achado central da auditoria (por que as telas "estavam quebradas")

A identidade da topbar (h1 + breadcrumb) vem de `_AppTopbar` → `BreadcrumbService.ResolveTrail(controller,
action, ViewData["Title"], ...)`. Para páginas fora dos 12 lances canônicos do `FeatureCatalogService`, a
folha cai em `Humanize(action)` — **"Index"/"Details"/"Create"/"Edit" em inglês** como h1 e breadcrumb — e o
`<title>` fica "PlantãoPro". Varredura determinística sobre as 416 views (exceto Shared/partials) mediu:

- **65 páginas** sem `ViewData["Title"]` nem entrada no catálogo (gap do E7);
- 174 views com h1 inline adicional sob `_Layout` (topbar também emite h1) → backlog D-R61-B1;
- **72 inputs sem nome acessível** em ~25 views (sem `aria-label`/`aria-labelledby`, sem `label[for]`,
  sem label envolvente na janela de 220 chars, sem `asp-for`);
- vestígios de conotação de homologação em mensagem/asset: copy "sandbox" em
  `FaturamentoSaas/Details`, cabeçalho "Ocorrências da Homologação" em `OperacaoAssistida/Ocorrencias`
  (drift: o próprio `ViewData["Title"]` da página já dizia "Ocorrências da Operação Assistida"), e asset
  `v2151-wp-c-homologacao.css` (nome de work-package legado; conteúdo = ajuste fino da linha canônica AA).
- `@page` em Views: 0 (contrato de repo mantido); views de impressão com `Layout=null` têm `<title>` próprio.

## Decisões

### D-R61-1 — toda página tem identidade pt-BR garantida (61 pares)
As 61 views do gap (excluídas: 2 impressões `Layout=null`, `V112/OperationalPage` e `Pendencias/Index`,
que o controlador intitula) agora aportam `ViewData["Title"] = "<título pt-BR>";` no topo da própria view —
o mesmo mecanismo das 327 views já conformes e que a topbar lê (código top-level da view executa antes da
topbar no layout; verificado empiricamente via e2e). Títulos curtos em linguagem de domínio (o
`BreadcrumbService` acrescenta o prefixo de área quando existe, ex.: Início / Atendimento / Agenda do dia).
**Fonte única das inserções:** `r61a1_titles.ps1` (idempotente, gravado nesta pasta). A abordagem original
do plano (mapeamento central + filtro global) foi descartada por motivo técnico registrado abaixo.

**Nota técnica (por que filtro não servia):** `ActionExecutingContext.ViewData` é somente leitura,
`ResultExecutingContext` não expõe ViewData, e o `ViewResultExecutor` recria/assume o ViewData após os
filtros globais — a escrita em filtro global seria sobrescrita ou invisível. O ponto correto é o que a base
já usa: a view (ou o controlador) aporta no próprio fluxo.

Tabela completa: contrato `R61IdentidadeCadastroOperacaoContratoTests.ParesTravados` (61 entradas com
título exato). Amostra verificada no ar pelo e2e: `/Convenios/Index`→"Convênios",
`/Agendamentos/AgendaDia`→"Agenda do dia", `/PainelChamada/Guiches`→"Guichês",
`/PainelChamada/Tv`→"Tela TV", `/MinhaAssinatura/Faturas`→"Faturas da assinatura".

Refinamento do e2e (achado real): as ações `Agendamentos/AgendaDia|AgendaMedico|Calendario` renderizam a
view compartilhada `AgendaPremium.cshtml` (`ViewData["Title"] = Model.Titulo`) — títulos vindos do
controller: "Agenda do dia", "Agenda por médico", **"Calendário clínico"** (sincronizado com wrapper e
contrato; antes o h1 saía pela folha da trilha, hoje sai do Title garantido).

### D-R61-2 — mensagens sem conotação de homologação/sandbox
- `FaturamentoSaas/Details`: "apenas abre a página do sandbox … webhook assinado" → "apenas abre a página
  de pagamento … notificação assinada do provedor";
- `OperacaoAssistida/Ocorrencias`: cabeçalho "Ocorrências da Homologação" → "Ocorrências da Operação
  Assistida" (alinhado ao Title da página); texto de confirmação "ocorrência da homologação assistida" →
  "ocorrência da operação assistida";
- asset renomeado `git mv v2151-wp-c-homologacao.css → v2151-wp-c-ajuste-canonical.css` (+ refs nos dois
  layouts + comentário de cabeçalho + ref no contrato R49 + check na smoke R52);
- whitelist preservada: código SEFAZ `HOMOLOGACAO` nos seletores de ambiente (valor funcional, não é cópia)
  e "Não Homologada".

### D-R61-3 — todo input tem nome acessível
Os 72 inputs órfãos ganham `aria-label` pt-BR (prefiro o placeholder quando já adequado; senão nome
derivado do campo). Sem mudança visual; leitores de tela anunciam todos os campos. Fonte idempotente:
`r61_forms_aria.ps1` (regras iguais às do scanner do contrato; reescrita UTF-8 sem BOM, compatível com a
base). Exemplo: `Fiscal/Nova` OrigemId → `aria-label="Código do documento de origem"` (compatível com o
rótulo travado no contrato R60).

### Backlog registrado (fora desta rodada, com motivo)
- **D-R61-B1** — 174 h1 inline duplicados sob `_Layout`: demover para h2 exige revisar os 55 seletores `h1`
  nus do design system arquivo a arquivo (`.agenda-hero h1`, `.pp-page-hero h1,h2`, etc.); a topbar agora
  exibe o h1 correto em todas as telas, mitigando o sintoma visível.
- Wrappers finos que o controller ignora (ex.: `Views/Agendamentos/AgendaDia.cshtml` quando a ação renderiza
  `View("AgendaPremium")`) recebem título como fallback latente; identidade viva desses 3 lances vem do
  controller (ver nota em D-R61-1).

## Contrato novo — `R61IdentidadeCadastroOperacaoContratoTests` (3 fatos, scanner de fonte)
1. `Paginas_TodasComIdentidadePortuguesa` — (a) cada um dos 61 pares traz o título exato no arquivo;
   (b) nenhum título com jargão de ação em inglês/TODO; (c) varredura global: zero outra view de página
   sem Title nem isenção (catálogo via `FeatureCatalogService`, impressões, V112, Pendencias).
2. `Mensagens_SemConotacaoDeHomologacao` — zero "sandbox" em views; layouts sem asset "homologacao";
   `Ocorrencias.cshtml` com 2×"Ocorrências da Operação Assistida" e zero "Homologação".
3. `Formularios_TodosInputsComNomeAcessivel` — zero inputs órfãos pela regra idêntica ao recon.

Contratos atualizados por consequência direta: `R49AcessibilidadeContratoTests` (nome do asset ×3 pontos).
Suíte: 1636 → **1639**.

## Gates
| Gate | Resultado |
|---|---|
| Rebuild `dotnet build backend\PlantaoPro.sln -c Release --no-incremental` | exit 0 |
| Gate `r20b1_gate.ps1` (log `%TEMP%\opencode\r20b1_build.log`) | Now=Base, DIFF=NENHUMA, erros=0 |
| Dirigidos R61×3 + R59×5 + R60×3 | 11/11 |
| Suíte integral | **1639/1639** (execução canônica pós-mudanças; uma falha transiente R7O O3 em execução intermediária — flake conhecido, passou isolado 5/5) |
| Smoke R52 canônica | FALHAS=0 |
| Mini-e2e `r61_identidade_e2e.ps1` | FALHAS=0 (identidades ≥4 verificadas no ar; homologação ausente; asset renomeado presente/ausente conforme esperado) |
| Docscan repo (acentos/canal) | bad=0 |

## Arquivos desta rodada
- 61 views com `ViewData["Title"]` novo (gap do E7)
- 30 views com `aria-label` em inputs (D-R61-3)
- `backend/PlantaoPro.Web/Views/FaturamentoSaas/Details.cshtml` (copy sandbox)
- `backend/PlantaoPro.Web/Views/OperacaoAssistida/Ocorrencias.cshtml` (termo de domínio)
- `wwwroot/css/design-system/v2151-wp-c-ajuste-canonical.css` (renomeado; 2 layouts referenciam)
- `backend/PlantaoPro.Tests/R61IdentidadeCadastroOperacaoContratoTests.cs` (novo, 3 fatos)
- `backend/PlantaoPro.Tests/R49AcessibilidadeContratoTests.cs` (asset renomeado)
- `docs/evidencias/2026-10-18-rodada-52/r52_smoke_e2e.ps1` (nome do asset no check V2151)
- Esta pasta: plano, scripts idempotentes (r61a1_titles.ps1, r61_forms_aria.ps1, r61_identidade_e2e.ps1)
