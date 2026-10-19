# R59 · Relatório final consolidado — Acessibilidade avançada

**Data:** 2026-10-19 · **Base:** `c120d61` (R58) · **Fila:** r53b1 §7 item 5 (D-R49-4) · **Suíte:** 1628 → **1633**

## Entrega (resumo)

Fechamento do item "Acessibilidade avançada": **`th scope` em tabelas densas (correção material)**,
**trap de foco em modais/overlays**, **landmarks**, **`aria-live` em toasts** e **teclado em widgets
custom** — esta última frente confirmada já implementada pelos componentes premium (kanban/select,
`enableAccessibleMove`, `<dialog>` nativo) e travada por contrato contra regressão.

## Recon — o que já existia vs o que faltava

| Frente | Estado antes | Ação desta rodada |
|---|---|---|
| Trap de foco (modais BS) | Bootstrap **v5.3.7** no layout — plugin já faz trap, `role="dialog"`, `aria-modal`, devolução de foco | travar por contrato |
| Trap de foco (overlays custom) | `core/overlay-manager.js` já importa `trapFocus` de `core/focus-manager.js` (Tab wrap + Escape + foco de volta ao gatilho) | travar por contrato |
| Live regions/toasts | `pp-live-region#appLiveRegion` (`role="status"` `aria-live="polite"`) nos dois layouts; `_ToastRegion` viva; toasts com `role=status/alert` + fechar com `aria-label`; itens focáveis | travar por contrato |
| Landmarks | `main#mainContent` ×2, skip-link ×2, `aside#appSidebar aria-label`, `header` topbar, `nav` breadcrumb/mobile, `footer`, `<dialog#commandPalette aria-labelledby>` — **nenhum gap** | travar por contrato |
| Widgets custom (teclado) | Kanban com `[data-move-select]` (select nativo) + `[data-kanban-live]`; `enableAccessibleMove` (botão+select); paleta em `<dialog>` nativo; drawers via overlay-manager | travar por contrato |
| **`th scope`** | **789 `<th>` reais; só 185 com `scope`** (604 pendentes em 82 arquivos); 3 células mal-tipadas no `<tfoot>` de `OperacaoPremium/Fechamentos.cshtml` (linha de totais: label `colspan=4` + valor + célula vazia, todas como `<th>`) | **correção real** |

## Decisões

- **D-R59-1 — `scope` em todos os cabeçalhos.** Varredura determinística idempotente
  (`r59_scope_sweep.ps1`, versionada): todo `<th` sem `scope=` ganha `scope="col"` antes do
  fechamento da tag, preservando atributos (padrão `<th\b` não casa com `<thead>/<tfoot>`).
  **604 inserções em 82 arquivos (426 pares de linhas, diff puramente de scope).** A linha de
  totais do Fechamentos foi corrigida à mão com semântica correta: label `<th colspan="4" scope="row">`,
  valor e célula vazia viraram `<td>`. Contrato final: **zero** `<th` sem `scope`.
- **D-R59-2 — trap de foco travado pela infraestrutura.** Contrato trava: bundle `v5.3.x`
  referenciado em `_Layout`; `overlay-manager.js` importando/usando `trapFocus`, tratando Escape e
  devolvendo foco ao gatilho; `focus-manager.js` mantendo `trapFocus`/`focusFirstInvalid`.
- **D-R59-3 — landmarks auditados e travados.** Sem gap no recon; contrato lista cada landmark
  obrigatório por layout/partial.
- **D-R59-4 — regiões vivas e toasts travados.** Contrato trava as presenças/roles do recon
  (live region ×2 layouts, região de toasts, `role=status/alert` dos itens, foco + fechar).
- **D-R59-5 — teclado em widgets custom travado.** Nenhum widget custom foi encontrado sem caminho
  de teclado nesta auditoria; contrato trava os caminhos existentes (select do kanban + live node,
  `enableAccessibleMove`, `dialog` nativo da paleta).

## Contrato novo — `R59AcessibilidadeAvancadaContratoTests` (5 fatos, scanner de fonte)

`Tabelas_TodosCabecalhosComScope` · `ModaisEOverlays_TrapDeFocoTravado` ·
`Landmarks_CompletosNosLayouts` · `Toasts_RegioesVivasERoles` · `WidgetsCustom_CaminhoDeTecladoTravado`

## Mudanças na base

- 82 views (`Views/**/*.cshtml`) — `scope="col"` (604 inserções) via script versionado.
- `Views/OperacaoPremium/Fechamentos.cshtml` — linha de totais resemantizada (1× `th scope="row"` + 2× `td`).
- `R59AcessibilidadeAvancadaContratoTests.cs` — novo contrato (5 fatos).
- `R28LinguagemSimplesAdm360ContratoTests.cs` — literal travado atualizado
  (`<th>Reserva vinculada</th>` → `<th scope="col">Reserva vinculada</th>`), regressão intencional da
  varredura; mesmo tratamento dado pela R57 a copy tratada.

## Incidente recuperado (varredura v1)

A primeira versão do script usava grupo nomeado lido como `$m.Groups.attrs.Value` dentro do
scriptblock do `[regex]::Replace` (PS 5.1): o acesso retornou `$null` em todos os matches e a
substituição reescreveu cada tag como `<th scope="col">` **descartando os demais atributos**
(inclusive absorvendo `<thead class=...>`). Detectado inspecionando o diff amostrado antes de
qualquer build definitivo. Recuperação: `git checkout` dos 98 arquivos afetados, script v2 sem
grupos (opera sobre `$m.Value` inteiro + `\b` para ignorar `thead/tfoot`), teste seco nos casos
difíceis (thead c/ classe, th c/ style, th plano, colspan) e re-execução. **Lição:** em scriptblock
de substituição regex no PS 5.1, preferir `$m.Value` a grupos nomeados e sempre conferir um diff
amostra antes de declarar varredura mecânica como completa.

## Gates (todos verdes)

| Gate | Resultado |
|---|---|
| Rebuild real `dotnet build -c Release --no-incremental` | exit 0, 0 erros (log: `%TEMP%\opencode\r20b1_build.log`) |
| Gate `r20b1_gate.ps1` | **Now=160 Base=160 DIFF=NENHUMA** |
| Dirigidos R59 + R49 (não-regressão) | **10/10** |
| Suíte integral | **1633/1633** (execução 3; histórico honesto em `r59_suite_resumo.txt`: falha 1 = literal R28 corrigido, falha 2 = flake transiente entre execuções consecutivas → fila item 10) |
| Smoke canônica R52 (binário final) | **FALHAS=0 / R52_E2E_OK** (registro em `r59_smoke_e2e_log.txt`) |
| Mini-e2e R59 (`r59_a11y_e2e.ps1`) | **14/14 OK**: página densa Estoque com 8 `<th>` todos `scope="col"`, zero `<th` sem scope renderizado, `thead/tfoot` limpos, bundle BS, live region, landmarks aside/header/footer/main; layout auth com skip-link + main + live region (saída em `r59_a11y_e2e_saida.txt`) |
| Docscan da rodada | **bad=0** (`r59_docscan.txt`) |

## Fila restante (r53b1 §7)

Itens **6** (inventário GUIDs/fragmentos), **7** (auditoria cadastro×operação), **8** (login
CPF/CNPJ/e-mail guiado), **9** (smoke de escrita c/ seed demo idempotente), **10** (consolidação
de flakes — agora inclui o flake transiente de suíte observado nesta rodada).

## Arquivos desta evidência

`r59a0-acessibilidade-avancada-plano.md` · `r59_scope_sweep.ps1` (varredura canônica, idempotente) ·
`r59_verify_scope.ps1` (verificação independente do estado final) · `r59_a11y_e2e.ps1` +
`r59_a11y_e2e_saida.txt` · `r59_smoke_e2e_log.txt` · `r59_suite_resumo.txt` · `r59_docscan.txt` ·
este relatório.
