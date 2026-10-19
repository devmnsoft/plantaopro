# R59 — Acessibilidade avançada (fila r53b1 §7 item 5; D-R49-4)

**Data:** 2026-10-19 · **Prévia desta rodada.** Gates executados no relatório final.

## Escopo (origem)

Fila r53b1 §7, **item 5**: "Acessibilidade avançada: trap de foco em modais, `aria-live` em
toasts, landmarks, `th scope` em tabelas densas, teclado em widgets custom (R49, D-R49-4)".
D-R49-4 declarou explicitamente que esses quatro pontos ficaram fora da passada "básica".

## Recon — estado real antes da passada

| Área | Estado encontrado | Ação |
|---|---|---|
| Trap de foco (modais BS) | `bootstrap.bundle.min.js` **v5.3.7** no layout — o plugin de modal já prende foco, move ao abrir, devolve ao fechar e aplica `role="dialog"`/`aria-modal` | travar por contrato |
| Trap de foco (overlays custom) | `core/overlay-manager.js` importa `trapFocus` de `core/focus-manager.js` (Tab wrap + Escape) | travar por contrato |
| Live regions/toasts | `pp-live-region#appLiveRegion` (`role="status"` `aria-live="polite"` `aria-atomic`) nos dois layouts; `_ToastRegion` (`aria-live="polite"` `data-toast-region`); `plantaopro-toast.js` renderiza `role=status/alert` + botão fechar com `aria-label`; `components/toast.js` insere itens `tabIndex=0` na região viva | travar por contrato |
| Landmarks | `main#mainContent` (`tabindex="-1"`) ×2 layouts + skip-link; `aside#appSidebar aria-label`; `header` topbar + `nav` breadcrumb `aria-label`; `nav` mobile `aria-label`; `footer`; `dialog#commandPalette aria-labelledby` | travar por contrato (nenhum gap) |
| Widgets custom (teclado) | Kanban: `[data-move-select]` (select nativo = teclado completo) + `[data-kanban-live]`; sortable-list: `enableAccessibleMove` (botão+select); command palette: `<dialog>` nativo + form; drawer via overlay-manager (Escape/trap) | travar por contrato |
| **`th scope`** | **791 `<th>`** em 119 tabelas (todas com `<thead>`); só **184 com `scope`** → ~607 colunas sem, em 55 arquivos. Fora de thead: 3 células no `<tfoot>` de `OperacaoPremium/Fechamentos.cshtml` (linha de totais: label `colspan=4` + valor + célula vazia) e 2 já com `scope="row"` em `MinhaAssinatura/Index.cshtml` | **correção real** |

Conclusão: o item 5 é ~90% infraestrutura já construída pelas rodadas premium (v2145+); a
correção material é o `th scope`, mais o contrato permanente que trava tudo contra regressão.

## Decisões

### D-R59-1 — `scope` em todos os cabeçalhos de tabela
Varredura determinística idempotente (script versionado `r59_scope_sweep.ps1`): todo `<th` sem
atributo `scope=` dentro de Views ganha `scope="col"`. Linha de totais do
`OperacaoPremium/Fechamentos.cshtml` corrigida à mão com semântica correta: label vira
`<th colspan="4" scope="row">`, as duas células de valor/vazia viram `<td>`. Contrato final:
**zero** `<th` sem `scope` em todo Views/**/*.cshtml.

### D-R59-2 — trap de foco travado pela infraestrutura existente
BS5.3.7 (modais bootstrap) + `overlay-manager` (overlays custom). Contrato trava: bundle
`v5.3.x` referenciado em `_Layout`; `overlay-manager.js` importa `trapFocus` e trata Escape;
`focus-manager.js` mantém `trapFocus`/`focusFirstInvalid`.

### D-R59-3 — landmarks auditados e travados
Sem gap (tabela acima). Contrato lista cada landmark obrigatório por layout/partial.

### D-R59-4 — regiões vivas e toasts travados
Contrato trava as 5 presenças/roles citadas no recon (live region ×2 layouts, região de toasts,
roles dos itens, foco + fechar nos toasts).

### D-R59-5 — teclado em widgets custom travado
Contrato trava os caminhos acessíveis existentes (select do kanban + live node,
`enableAccessibleMove`, `<dialog>` nativo do paleta). Nenhum widget custom foi encontrado sem
caminho de teclado nesta auditoria; se aparecer no futuro, o contrato D-R59-5 cobre os mesmos
componentes listados.

## Contrato novo — `R59AcessibilidadeAvancadaContratoTests` (5 fatos, scanner de fonte)

1. `Tabelas_TodosCabecalhosComScope` — zero `<th` sem `scope` em Views/**/*.cshtml.
2. `ModaisEOverlays_TrapDeFocoTravado` — fatos D-R59-2.
3. `Landmarks_CompletosNosLayouts` — fatos D-R59-3.
4. `Toasts_RegioesVivasERoles` — fatos D-R59-4.
5. `WidgetsCustom_CaminhoDeTecladoTravado` — fatos D-R59-5.

Suíte esperada: 1628 → **1633**.

## Gates (executados no relatório final)

1. Rebuild real `--no-incremental` → gate `r20b1_gate.ps1` (espera `Now=160 Base=160 DIFF=NENHUMA`).
2. Dirigidos R59 (5/5) + dirigidos R49 (não regressão).
3. Suíte integral (~1633).
4. Smoke canônica R52 contra binário final.
5. Mini-e2e R59: página densa renderizada contém `scope="col"` e zero `<th` sem scope;
   live region presente no HTML; bundle bootstrap presente.
6. Docscan dos docs desta rodada.
