# Rodada 71 — Semantica de cores: estado sempre com icone + texto

## Objetivo

Nenhum status no produto comunica por cor sozinha: todo ponto que exibe um estado
de negocio exibe icone + texto, e o pareamento `estado -> tom (cor) -> icone` vive
em exatamente um lugar, aplicado em todos os pontos onde havia divergencia.

## Artefatos unicos (travados por contrato)

| Artefato | Papel |
| --- | --- |
| `backend/PlantaoPro.Web/Localization/EstadoVisual.cs` | mapa canonico: estado normalizado -> (tom, icone); 155 entradas; fallback neutro `secondary` + `bi-question-circle` |
| `v2151` secao 9 (marker `R71-MAPA-ESTADOS`) | tom -> cor: tokens `--pp-tone-<tom>-ink` / `--pp-tone-<tom>-soft` e regras `.pp-status--<tom>` |
| `Views/Shared/_StatusBadge.cshtml` e `_StatusBadgeComRotulo.cshtml` | componentes canonicos: sempre `<i class="bi ...">` + `<span data-status-label>` + `role="status"` + classe de slug `.status-<slug>` |
| JS `agenda-operacional.js` / `notification-center.js` | subconjunto exato do mapa C# (`MAPA_R71` / `TONOS_R71`): mesmo tom E mesmo icone, fallback identico |

## Tons (paleta funcional R69, todos passam AA sobre o fundo soft)

| Tom | Tinta | Fundo |
| --- | --- | --- |
| success | #087a4d | #e8f7f0 |
| warning | #b45309 | #fff4d8 |
| info | #0e7490 | #e8f2ff |
| danger | #b42336 | #ffeaed |
| secondary | #66727c | #e8eef5 |

## Metodologia de verificacao

1. **Auditoria deterministica** (`r71_audit_mapa.ps1`, PowerShell 5.1, sem
   dependencias), dois passes sobre todo o repo:
   - **PASS A (mapa)**: bloco a bloco em `wwwroot/css` (92 arquivos, vendor fora) —
     seletor com token `status-` + declaracao `color`/`background*` com valor bruto
     fora dos tons canonicos (`var(--pp-tone-*-ink|soft)`, `currentColor`, `inherit`,
     `transparent`, tipografia neutra `--pp-text*`) = falha.
   - **PASS B (cor-sola)**: tag a tag em `Views/**/*.cshtml` (485 arquivos) —
     elemento de familia (badge, chip, pill, dot, indicator, text-bg-*) cujo conteudo
     inline nao tem texto legivel nem icone, sem `aria-label`/`title` = falha.
     Regra icon-first: `<i>` como primeiro filho so passa se ha texto apos o `</i>`.
2. **Contrato em C#** (`R71SemanticaDeCoresContratoTests`, 6 fatos) espelha os dois
   passes e trava os artefatos: tons/regras no v2151; integridade do mapa (>=70
   slugs, sem duplicatas, tons validos, icones `bi-*`, fallback) + cobertura de
   **todos** os valores dos guards `CHECK (status IN (...))` do banco; paridade dos
   JS (todo par estado->tom->icone dos clients existe no mapa C# com tom e icone
   iguais); componentes emitem icone + texto + `data-status-label`.

## Resultado

| Medida | Antes | Depois |
| --- | --- | --- |
| Auditoria FALHAS (MAPA + COR_SOLA) | 28 (28 + 0) | **0** (0 + 0) |
| Suite | 1661 aprovados | **1667 aprovados** (+6 da R71), 0 falhas |
| Gate build Release | Now=Base=160, DIFF=NENHUMA | idem (0 erros) |
| Cobertura guards CHECK do banco | — | 39 valores distintos, nenhum fora do mapa |

## O que mudou

- **CSS (10 folhas)**: cor de estado bruta substituida pelos tokens
  `--pp-tone-*`. `v2000-premium`, `financial` (hero pago/cancelado/recusado),
  `custom`, `professional-portal` (shape mantida, cor fora), `minha-central`
  (forca de azul removida), `plantao-premium` (border-left confirmado/conflito/
  aberto), `v2070-notifications` (grupo `.priority-badge,.status-badge` quebrado),
  `v2148` (is-success/warning/danger/muted), `saas` (--source/--unknown/
  --unavailable), `v2145` (pill base/next-step). Regras que so definiam forma
  (border, radius, padding, font) foram mantidas.
- **JS**: `agenda-operacional.js` — o drawer atualiza `[data-status-label]` (o
  defeito era `badge.textContent = ...` destruindo o `<i>`) e sincroniza
  tom+icone via `MAPA_R71` (27 pares, subconjunto do C#); `notification-center.js`
  — o span de status passa a nascer com `pp-status pp-status--<tom>` + `<i>` +
  texto, via `TONOS_R71` (10 pares; `nao-lida`=info, `devolvido`=warning,
  `lida`/`arquivada`=secondary — idêntico ao C#).
- **Views (12 arquivos)**: spans crus `status-badge` / `pp-status-badge` /
  `pp-payment-status` convertidos para os partials canonicos:
  ConferenciaExecucao/Index, OperacaoPremium/Fechamentos (x2), MinhaAgenda
  (Index, Agenda, Presencas, Detalhe, PlantoesDisponiveis, MeusPagamentos x3
  rotulados), Auditoria/Index, Clientes/Index, Configuracoes/Index,
  Financeiro/Details (hero + estado compacto via `EstadoVisual.Interpretar`).
- **Mapa**: duas entradas apuradas pelo contrato durante a rodada: `conflito`
  (danger / oitavangulo — conflito de agenda) e `desligado` (secondary / risca —
  colaborador desligado, familia de inativo/desativado).

## Whitelist do PASS B (fundamentacao)

`pp-timeline-dot`, `pp-login-connection__dot`, `pp-session-health__dot`,
`save-indicator`, `bg-opacity-25`: marcadores decorativos dentro de containers
`role="status"` cujo texto vizinho comunica o estado (ex.: "Conexao estavel",
"Salvo"). O ponto em si nao carrega informacao independente, entao nao exige
icone proprio; se o texto sumir, o container inteiro falha no PASS B.

## Follow-ups (fora do escopo desta rodada)

- `text-bg-*` (Bootstrap): 51 usos, todos com texto visivel — passam no PASS B;
  migracao para os tons canonicos pode virar rodada futura de polimento.
- `plantao-premium.css` / `custom.css`: camadas legadas servidas apenas por SW
  precache (nenhuma referencia ativa de pagina); foram limpas mesmo assim para o
  mapa ser verdadeiramente unico. Nenhum novo asset em `wwwroot` (o service
  worker continua precachendo os mesmos caminhos).
- 404s de assets detectados na varredura R70 (documentados na rodada 70).

## Evidencias

- `r71_audit_antes.txt` — FALHAS=28 (mesmo estado de codigo, antes da limpeza)
- `r71_audit_depois.txt` — FALHAS=0
- `r71_probe_guard.ps1` — cobertura dos guards CHECK do banco
- Suite final: `Aprovado! 0 falhas, 1667 aprovados, 1667 total`
- Gate build Release: `Now=160 Base=160 DIFF=NENHUMA erros=0`
