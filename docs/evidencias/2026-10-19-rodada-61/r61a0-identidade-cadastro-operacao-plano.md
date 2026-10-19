# R61 — r61a0 — Auditoria sistemática cadastro×operação (E7) + design de identidade e mensagens

**Data:** 2026-10-19 · **Prévia desta rodada.** Gates no relatório final.
**Fila:** r53b1 §7 **item 7** — "Auditoria sistemática cadastro×operação (o E7 não executado como epic autônomo)".
Diretrizes do usuário desta sessão (aplicadas como critérios): verificar o design (pontos quebrados),
ver todos os forms, nenhuma mensagem com conotação de homologação, IA integrada explicitamente
(tratada na R62 como provider dedicado) e diferencial de mercado.

## Recon — a cadeia real de identidade das telas

O h1 da topbar e o breadcrumb **não** vêm do conteúdo da view: `_AppTopbar` (renderizado ANTES do
RenderBody) consulta `BreadcrumbService.ResolveTrail(controller, action, ViewData["Title"], ...)`, que:
1. se o par (controller, action) existe no `FeatureCatalogService` → trilha rica do catálogo (12 lances canônicos);
2. senão → `Início [> área] > Humanize(action)` — e o h1 = `ViewData["Title"] ?? última-folha`.

Consequência auditada (varreduras determinísticas sobre as 416 views, excluindo Shared/partials):
- **65 páginas sem `ViewData["Title"]` nem entrada no catálogo** → topbar exibe "Index"/"Details"/
  "Create"/"Edit" em inglês como h1 e folha de breadcrumb (o "ponto quebrado" sistêmico). Amostra:
  `/Convenios/Index` → h1 "Index"; `/Pacientes/Details` → h1 "Details"; `/ClinicaUnidades/Create` → "Create".
- **174 views com h1 inline** sob `_Layout` (topbar também emite h1) → hierarquia duplicada; registrado em
  backlog de design (demover h1→h2 exige revisar seletores de tag nua do design system, 55 ocorrências).
- CSS do design system com 55 seletores `h1`; maioria escopada por classe (`.pp-page-hero h1, .pp-page-hero h2`).
- **Vestígios de homologação em mensagem visível**: `FaturamentoSaas/Details.cshtml:128` cita "sandbox";
  `OperacaoAssistida/Ocorrencias.cshtml:6` cabeçalho "Ocorrências da Homologação" (o `ViewData["Title"]` da mesma
  página já diz "Ocorrências da Operação Assistida" — drift identidade); asset
  `wwwroot/css/design-system/v2151-wp-c-homologacao.css` referenciado nos dois layouts (nome de work-package
  legado, conteúdo = ajuste fino da linha canônica/contraste AA).
- **Forms**: 72 inputs sem nome acessível (sem `aria-label`/`aria-labelledby`, sem `label[for]`, sem label
  envolvente, sem `asp-for`) em ~25 views — scanner com janela de 220 chars para label envoltor.
- `@page` em Views: 0 (contrato de repo mantido). Views de impressão (`Layout = null`) têm `<title>` próprio.

## Decisões

### D-R61-1 — identidade única cadastro×operação: mapa central de páginas fora do catálogo
Nova `Services/PageIdentityMap.cs` (par controller/ação → título pt-BR + rótulo curto de trilha), consumida por
`PageIdentityFilter` (IActionFilter global registrado no `AddControllersWithViews`, antes das ações). O filtro só
escreve se a view/controlador ainda não aportou `ViewData["Title"]` — precedência: view > controlador > mapa >
catálogo > `Humanize(action)`. É a extensão natural da arquitetura existente (catálogo + serviço de trilha), uma
fonte única auditável em vez de 65 edições espalhadas. Cobre os 61 pares do gap (excluídas 2 views de impressão
`Layout=null`, `V112/OperationalPage` e `Pendencias/Index`, que o controlador já intitula). Sem jargão: nenhum
título usa nomes de ação em inglês (travado por contrato).

### D-R61-2 — mensagens sem conotação de homologação/sandbox
- `FaturamentoSaas/Details`: "apenas abre a página do sandbox" → "apenas abre a página de pagamento";
- `OperacaoAssistida/Ocorrencias`: cabeçalho "Ocorrências da Homologação" → "Ocorrências da Operação Assistida"
  (alinhado ao próprio Title da página);
- asset renomeado para `v2151-wp-c-ajuste-canonical.css` (+ refs nos dois layouts + comentário de cabeçalho);
- whitelist de domínio preservada: ambiente SEFAZ `HOMOLOGACAO` (código funcional) e "Não Homologada".

### D-R61-3 — todo input tem nome acessível
Os 72 inputs órfãos ganham `aria-label` pt-BR (preferindo o texto do placeholder quando já existe e é adequado;
senão, nome derivado do campo). Sem mudança visual; leitores de tela passam a anunciar todos os campos.

### Backlog registrado (fora desta rodada, com motivo)
Demover os h1 inline duplicados sob `_Layout` (174) para h2 — exige revisar os 55 seletores `h1` nus do design
system arquivo por arquivo; a topbar agora exibe o h1 correto em todas as telas, mitigando o sintoma.

## Contrato novo — `R61IdentidadeCadastroOperacaoContratoTests` (3 fatos, scanner de fonte)
1. `Paginas_TodasComIdentidadePortuguesa` — zero gap: toda view de página satisfaz (tem `ViewData["Title"]`) OU
   (está no catálogo canônico) OU (está no PageIdentityMap) OU (isenta documentada: impressão/V112/Pendencias);
   títulos do mapa sem palavras de ação em inglês.
2. `Mensagens_SemConotacaoDeHomologacao` — zero "sandbox" em cshtml; layouts sem referência a asset
   "homologacao"; cabeçalho de OperacaoAssistida/Ocorrencias alinhado ao Title.
3. `Formularios_TodosInputsComNomeAcessivel` — zero inputs órfãos pela regra do recon.

Suíte esperada: 1636 → **1639**.

## Gates (no relatório final)
Rebuild `--no-incremental` → gate `r20b1_gate.ps1`; dirigidos R61+R59+R60; suíte integral; smoke R52; mini-e2e R61
(topbar com h1 pt-BR em páginas antes quebradas, sandbox/homologação ausentes nas páginas cobertas); docscan.
