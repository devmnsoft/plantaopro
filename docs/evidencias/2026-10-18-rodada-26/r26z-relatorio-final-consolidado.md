# R26 · r26z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R25 (`b28e9b5`, suíte 1538).

## Escopo da rodada

Epic **E4** da diretriz (r24a1): "uma área escura não pode ter letra escura, se não confunde e não dá pra ler".

## Commits

| Commit | Conteúdo |
|---|---|
| `16c4d70` | r26b1: 214 trocas de `text-muted`→`text-white-50` + 2 remoções cirúrgicas em 38 views Adm360 + fato-scanner global `R26ContrasteZonasEscurasContratoTests` + plano r26a0 atualizado com resultado real |

## Execução (decisão D-R26-1 aplicada)

- O recon do scanner PS subestimou o escopo (19 pontos): o port C# fiel ao algoritmo expôs que
  `text-muted` sobre zona escura é **padrão de casa** nas views Adm360 — 192 linhas violadoras em 35 views.
  Total aplicado: **214 correções em 38 arquivos** (lote scriptado por file:line + 22 manuais iniciais).
- Regra de correção: fundo escuro → `text-white-50` (branco 50%, ≈ `#adb5bd` sobre preto — mesmo grau de
  clareza do muted em fundo claro, desenhado para escuro). Duas exceções documentadas:
  - `ComissoesPendentes` L79 tinha `text-light` E `text-muted` no MESMO elemento (conflito de ordem de CSS) → removido o muted;
  - `ContasFinanceiras` L190: `alert-secondary` é superfície CLARA dentro de card escuro → removido o muted
    (texto padrão escuro é o correto sobre cinza claro). A regra "superfície clara própria" foi validada em
    controle negativo do fato e preservada pelo scanner.
- Auditoria do diff: TODA linha alterada é exclusivamente a troca/remoção da classe — conteúdo intacto
  (acentuação verificada spot-check).

## Fato permanente

`R26ContrasteZonasEscurasContratoTests` (2 fatos): scanner de adjacência DOM (pilha de tags; gatilho =
`text-muted` cujo próprio class ou ancestor aberto tem classe escura, exceto superfície clara própria)
sobre TODAS as views + controles positivo/negativo do próprio scanner. A partir de agora qualquer novo
`text-muted` em zona escura em qualquer view futura falha a suíte — a regra está contratada, não só corrigida.

## Gates e suíte

- Rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Suíte completa final: **Total 1540** (= base 1538 + 2 fatos E4), **0 falhas**, 1m15s.

## Recon já iniciado para a R27 (E1 — linguagem simples, zona portal)

Scan de jargão técnico em views do portal (script `%TEMP%\opencode\r27_scan_jargao.ps1`) — ocorrências por zona:
Saude360 11, FaturamentoClinico 9, Agendamentos 6, DashboardsPremium 6, Plantoes 4, Bi 2, OperacaoPremium 3, AssistenteIa 1.
Classificação preliminar (detalhamento file:line vai no r27a0):

- **Botão "Atualizar BFF"** (`OperacaoPremium\Cobertura.cshtml` L13) — jargão de arquitetura visível ao usuário; alvo claro.
- **`Fonte: <code>@Model.Endpoint</code>`** (`Agendamentos\AgendaPremium.cshtml` L10) — URL de endpoint crua exibida na página.
- **Strings "retornado pela API"/"Não informado pela API"** (FaturamentoClinico/Index, Agendamentos/AgendaPremium, Plantoes/Details)
  — candidatas a "os dados do sistema..." (linguagem simples).
- **`DashboardsPremium\Perfil.cshtml` L9**: "KPIs carregados da API /api/dashboards/... respeitando tenant, perfil, plano e módulos"
  — trecho técnico (e 3× a palavra "tenant" neste arquivo — sobrepõe-se ao E2/R39; tratar aqui a linguagem e o tenant jargão junto).
- **Whitelist (código, não texto)**: `Endpoint = "/api/lookups/..."` em Razor (Saude360 Formulario/Modulo), `fetch('/api/bi/...')`
  em JS (Bi/Index), `Guid.Empty` em expressão C# (Plantoes/Details).

## Estado de casa

- Apps PARADOS, portas livres. `origin/main` sincronizada neste push.
- Fila: **R27 = E1 zona portal** (r27a0 inventário file:line + reescritas resx/literals + fato anti-jargão); roadmap r24a1 segue.
