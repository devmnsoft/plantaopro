# R8-B18 — Cards KPI manuais × partials `_KpiCard`/`_CoverageCard`: validação + contrato anti-drift (§29 item 10)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia "Cards KPI manuais × partials `_KpiCard`/`_CoverageCard` subutilizados" (item 10 da fila §29, ordem quebrado→alto impacto: … → B15 → B16 → B17 → **B18**). Este é o **último item do top-10 §29**.
- **Data:** 2026-10-13
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica: o design system de views + os 4 locais apontados)

O varredura (C7.1) classificou como "cards de KPI escritos à mão … embora existam partials `_KpiCard.cshtml` (usado só em `Clientes/Inteligencia.cshtml:13`) e `_CoverageCard.cshtml`". A validação por fonte canônica mostra que o snapshot está **desatualizado** e que os 4 locais **não são duplicatas exatas** do parcial:

**(a) O design system DE KPI existe e é estabelecido** (não "subutilizado"):
- `_KpiCard.cshtml` (Shared) = tile KPI **server-side**: `<div class="kpi-card kpi-@semantic">` com `kpi-title`/`kpi-value`/`kpi-subtitle`/`kpi-icon`(bi icon)/`kpi-variation` opcional. Modelo `KpiCardViewModel(title, value, subtitle, icon, variation, semanticColor)`.
- `_CoverageCard.cshtml` (Shared) = métrica de **cobertura**: `<article class="coverage-card coverage-card--@tone">` com label/value/context + link opcional "Ver detalhes". Modelo `CoverageCardViewModel`.
- Ambos os modelos vivem em `DesignSystemViewModels.cs`.
- Uso real: `_KpiCard` consumido por **7 views** (`Clientes/Inteligencia`, `Coordenacao/Index`, `Marketplace/Index`, `WhiteLabel/Preview`, `WhiteLabel/Index`, `Permissoes/TestarAcesso`, `Permissoes/Matriz`); `_CoverageCard` por `CentralEscala/Index`. Ou seja: **o componente canônico já é o padrão estabelecido**, refutando "só em Inteligencia".

**(b) Os 4 locais apontados são 4 RENDERIZAÇÕES DIFERENTES**, nenhuma roteada pelo `_KpiCard`:

| Local | Padrão real | Por que não é swap para `_KpiCard` |
| --- | --- | --- |
| `Bi/Index.cshtml` | Bootstrap `.card`/`.card-body` com `<h3 id="kpi-*">-</h3>` + **JS `fetch('/api/bi/resumo-executivo')`** que preenche os valores em runtime | hidratação client-side; sem icon/semântica/subtítulo |
| `Operacoes/Index.cshtml` | `<div id="ops">` vazio + **JS `fetch('/api/operacoes/dashboard')`** montando cards Bootstrap a partir de um mapa chave→valor dinâmico | sem markup server-side nenhum (loop sobre chaves arbitrárias) |
| `MeuDia/Index.cshtml` | `<section class="day-kpis"><article><span>…</span><strong>…</strong></article>` | strip próprio, sem card chrome/icon |
| `CommandCenter/Index.cshtml` | `<section class="cc-kpis"><article class=critical><span>…</span><strong>…</strong><small>…</small></article>` | strip próprio distinto |

**Conclusão:** a "redundância" é **CONCEPTUAL** — várias formas de exibir um número de métrica — e **não** uma cópia byte-idêntica removível por substituição (ao contrário dos stubs de relatório do B12, que eram idênticos). O canônico estabelecido é o par de parciais do design system (`_KpiCard` para KPI server-side; `_CoverageCard` para cobertura com drill-down). Unificar os 4 páginas no `_KpiCard` exigiria trocar markup/classe/DOM em 4 páginas vivas e converter as duas JS-hidratadas para server-side: um **refator de design** com risco de regressão visual.

## 2. Decisão e mudança (sem regra inventada, sem deleção antecipada)

Rito §29 (comparar consumidores → canônico → migrar → regressões):
- **Comparar:** feito (tabelas acima). **Canônico:** o par de parciais do design system; `_KpiCard` já é o padrão estabelecido (~7 views).
- **Migrar:** **adiado** — unificar os 4 renderings distintos seria mudança de direção visual (decisão de produto), e as páginas JS-hidratadas mudariam de modelo de renderização. Fora do escopo do rito desta rodada (consolidação byte-equivalente não se aplica por não serem idênticos).
- **Regressões/contrato:** entregue **CONTRATO anti-drift** `R8B18KpiCardsParciaisContratoTests` (3 fatos): (1) os parciais canônicos existem em `Shared` (com modelos em `DesignSystemViewModels.cs`) e o `_KpiCard` é efetivamente estabelecido (≥5 views o usam); (2) `Bi` e `Operacoes` hidratam KPI por `fetch(...)` e **não** usam `_KpiCard`; (3) `MeuDia` (`day-kpis`) e `CommandCenter` (`cc-kpis`) usam strips próprios e **não** usam `_KpiCard`. Se uma dessas páginas vier a adotar o parcial (ou o parcial deixar de existir), o contrato falha e força decisão consciente.
- **Zero mudança em produção.**

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Tests/R8B18KpiCardsParciaisContratoTests.cs` | **Novo**: 3 fatos — (1) parciais canônicos existem + `_KpiCard` estabelecido em ≥5 views; (2) Bi/Operacoes por JS, sem `_KpiCard`; (3) Meu Dia/CommandCenter com strips próprios, sem `_KpiCard`. |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §29 item 10 (linha 236) → **R8-B18**; resumo "Estado" (§29) refletindo itens 1–10 + nota de que o **top-10 do §29 ficou completo**. |

## 4. Evidência (executada)

- **Build sln completa:** EXIT 0 (`BUILD_R8B18_EXIT:0`; apenas avisos pré-existentes).
- **Alvo:** `R8B18KpiCardsParciaisContratoTests` = **3/3 aprovados** (`--filter FullyQualifiedName~R8B18`).
- **Gate phaseA v2:** **52/52, EXIT 0** (RAM livre 0,49 GB ao iniciar).
- **Full suite v2:** **`Total de testes: 1367`** (1364 baseline B17 + 3 novos) em **todas** as 3 rodadas — **full1, full2 e full3 todos limpos** (nenhuma falha nesta rodada, mesmo sob RAM baixa).
- **Verificação byte-level:** `git status` = matriz + teste novo (+ doc selado); **nenhum arquivo de produção alterado**.

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Unificação opcional dos 4 renderings de KPI** em `_KpiCard`/componente único: refator de design (markup/DOM de Bi/MeuDia/CommandCenter/Operacoes + converter Bi/Operacoes de JS-hidratação para server-side). Fazer junto com a definição das métricas (dívida "Meu Dia/CommandCenter/Admin360 definitions" + drill-down, §27 linhas 206/208). Contrato `R8B18…` exige decisão consciente se uma dessas páginas adotar o parcial.
2. **Snapshot de varredura desatualizado (auditoria):** o C7.1 dizia "`_KpiCard` usado só em `Clientes/Inteligencia`"; na prática está em ~7 views. Observação de auditoria, não dívida técnica.
3. **Main DB v2339 / homologação viva:** invariado — janela segue em `r6a1b` §7 (+ lote v2340–v2351 + IIS).

## 6. Fila seguinte (R8) — §29 top-10 COMPLETO

Com o §29 item 10 entregue, **os 10 candidatos do top-10 do §29 estão todos endreçados** (B9–B18, mais B10–B13). Próximos passos naturais: **janela viva de homologação (`r6a1b` §7)** (inclui upgrade do banco principal + IIS), ou o **push da rodada** (pendente, `main` ahead do `origin/main`). Candidatos fora do top-10, se houver, seguem o rito do §29.

## 7. Gotchas aprendidos (reutilizáveis)

- **"Partial subutilizado" pode ser snapshot defasado:** confirmar o uso REAL atual antes de concluir (aqui `_KpiCard` já estava em ~7 views, não só em 1). Enumerar os consumidores atuais > confiar no texto da varredura.
- **"Redundância de KPI" ≠ cópia exata:** comparar a estrutura (classe raiz / DOM / modelo de renderização server vs. client-JS) dos locais apontados contra o parcial. Só há consolidação segura (byte-equivalente, à la B12) quando são idênticos; senão é refator de design (decisão de produto).
- **Contar arquivos, não nomes de arquivo:** várias views se chamam `Index.cshtml`; contar por caminho (não `GetFileName()`) senão o `Distinct()` colapsa nomes iguais e distorce a contagem de uso do parcial.
