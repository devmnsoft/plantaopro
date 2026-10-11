# R7-D-F3 — Central de Escala / jornada de cobertura em recursos localizados

Data: 2026-10-11 · Base: `8cc4a54` (R7-D-F2). Fatia 3 do corte R7-D (escopo em
`r7d0-varredura-design-textos-i18n.md`, seção 6).

## Problema (achado R7-D0, jornadas 2/4)

A jornada operacional inteira (Central de Escala + lista de Plantões) seguia com pt-BR
hardcoded:

- `CentralEscalaController.cs` — 16 mensagens fixas (falha de carga, filtros inválidos,
  decisão de substituição validada/sucesso/conflicto/permissão/não-encontrada/desconhecida,
  convite validado/falha/sucesso/imprevisto, plantão não encontrado/falha de carga) +
  a mensagem do payload de convite;
- `Views/CentralEscala/Index.cshtml` — hero, "Como usar esta página" (9 textos),
  formulário de filtros (situações + busca), 5 cards de indicadores, alternador de
  visualização, mapa de cobertura, fila prioritária, linha do tempo (+ estado vazio),
  atalhos rápidos — ~51 literais;
- `Views/CentralEscala/Plantao.cshtml` — título, voltar, campos e ações;
- `Views/Plantoes/Index.cshtml` — page-intro, banner de falha, jornada MVP, KPI strip,
  barra de filtros completa (status/cobertura/valores/ordenação/direção), viewbar
  list/kanban/calendário, tabela, cards mobile, lanes do kanban (+ estado vazio por
  lane), progressbar e estado vazio geral.

## Solução

| Peça | O que mudou | Evidência |
|---|---|---|
| `UiTexts.resx` (+ `.en/.es/.fr`) | **+165 chaves** (`Coverage.*` para a central/mensagens, `Shift.*` para plantão/turno/lista) em paridade total nos 4 arquivos (352 chaves por arquivo, 0 duplicatas) | teste de paridade R7-E1 + contrato novo |
| `CentralEscalaController.cs` | todas as 17 strings via `UiText.L("Coverage.…")`/`Shift.…`; `using PlantaoPro.Web.Localization` adicionado | contrato `CentralEscalaController_Mensagens_…` |
| `CentralEscala/Index.cshtml` | hero/banners/filtros/cards/alternador/mapa/fila/timeline/atalhos via recursos; contagens interpoladas com `string.Format(InvariantCulture, …)`; **guardas, rotas e parciais intactos** | contrato `CentralEscalaIndex_UseRecursos…` |
| `CentralEscala/Plantao.cshtml` | título/voltar/campos/ações via recursos | contrato `PlantaoEDetalheUsemRecursos…` |
| `Plantoes/Index.cshtml` | toda a tela (intro→jornada→KPIs→filtros→viewbar→tabela→mobile→kanban→estado vazio) via recursos | idem |
| Testes `R7DF3CentralEscalaI18nContratoTests.cs` (novo) | 4 fatos de contrato de fonte: uso das chaves + ausência dos literais nos 4 arquivos, chaves presentes nos 4 resx | `[números]` |

## Decisões registradas

1. **Reuso deliberado de chaves existentes** para evitar sinônimos paralelos:
   `Shift.New` ("Novo plantão", já usado no botão do hero), `Coverage.ApplyFilters`,
   `Coverage.ModeList/ModeCalendar`, `Shift.NoCoverage` (KPI "Sem cobertura" da lista é o
   mesmo conceito do badge de risco), `Shift.CoverageComplete`, `Coverage.ConflictsCard`.
   Registro único de cada frase no catálogo.
2. **Chaves com placeholders usam `string.Format(InvariantCulture, …)`** — formato de
   cultura sempre pt-BR para exibição, mas a montagem de `{0}`/`{1}` não depende de
   cultura (mesmo padrão de `Auth.ErrorAuthFailedWithRef`).
3. **`data-detail-kind="Plantão"` não foi alterado** — valor de máquina consumido pelo
   JS de detalhe (`v2119-shift-operations.js`); trocar exigiria sincronizar o script e
   não é rótulo exibido. Registrado como verba de follow-up caso o JS venha a exibi-lo.
4. **`title="Ação indisponível"` do `_EmptyState.cshtml`** (botões desabilitados) segue
   hardcoded — parcial compartilhado por 40+ jornadas fora do escopo desta fatia;
   oportunidade documentada para o corte de estados vazios (F4).
5. **Mensagens dinâmicas da API** (`result.Error`, `error ??`) continuam passando direto
   quando presentes — mesmo padrão da F2 (dívida do lado API, não inventar texto).
6. O anexo R7-D0 apontava só `Plantoes/Index.cshtml:79,99`; a leitura completa revelou
   que a tela inteira era literal. A fatia expandiu-se para **completar a tela**
   (jornada inteira localizável ou fica meio pt-BR) — ~165 chaves em vez de ~25,
   mesmo padrão mecânico, sem mudança de comportamento.

## Validação

- Build Web (Razor) + Tests: **0 erros** nos dois (`build16.log`, `BUILDWEB16_EXIT:0`,
  `BUILDTESTS16_EXIT:0`).
- Testes alvo (R7DF1 + R7DF2 + R7DF3 + R7E1 + V2150 + V2156 + V2164 + V2165): **72/72 aprovados em 3 s**
  (`build16.log`) — primeiro verde, sem retrabalho de contrato nesta fatia.
- Gate da rodada + suíte completa ×3 (10:42 UTC-3): gate **52/52** · full1 **1264/1264** ·
  full2 **1263/1264** · full3 **1264/1264** (`suite-final2-summary.txt`, `v2-full*.log`).
  Falha única do full2: `Administrativo360DocumentosXmlB1Tests.Divergente_MesmaChave_OutroConteudo_FalhaComMensagemDeErro`
  (`Assert.Equal(1, r2.Falhas)` → 0, `Administrativo360DocumentosXmlB1Tests.cs:356`) — **não** é a assinatura
  40P01 de cleanup; full1/full3 passaram com o MESMO binário e **reexecução isolada da classe:
  6/6 em 1 s** (`rerun-b1.log`) → terceira ocorrência distinta de flake de banco compartilhado
  sob concorrência de coleções, catalogada na matriz R8-A0. Fora do escopo desta fatia
  (corte Web/i18n; zero contato com o repositório de documentos XML) e não bloqueia o TESTADO.

**Estado honesto**: IMPLEMENTADO → TESTADO. AINDA NÃO HOMOLOGADO em host vivo e não LIBERADO.
Commit local desta fatia; push diferido por decisão da rodada.
