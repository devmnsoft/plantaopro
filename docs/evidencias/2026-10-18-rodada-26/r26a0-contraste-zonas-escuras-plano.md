# R26 · r26a0 — Epic E4: contraste em zonas escuras (plano e decisão)

Data: 2026-10-18 · Sessão: `opencode` · Base: R25 (`b28e9b5`, suíte 1538).

## Recon

Scanner de adjacência DOM sobre TODAS as views (pilha de tags; gatilho = elemento `text-muted` cujo próprio
class ou qualquer ancestor aberto tem classe escura `bg-black|bg-dark|table-black|pp-hero__bg|operational-card`).
Script guardado em `%TEMP%\opencode\r26_scan_contraste.ps1`. A contagem bruta de ~200 `text-muted` em views com
contexto escuro era ilusória: o scan estruturado encontrou **apenas 19 violações reais** (8 arquivos, todos Adm360):

- `Cotacoes\Detalhes.cshtml` 9 (ajudas de formulário, estados vazios de lista, metadados de anexo/exportação, modal de estorno)
- `DocumentosXml\Detalhes.cshtml` 3 (evento vazio, protocolo, data)
- `ValorizacaoPrevia.cshtml` 2 (label + valor do custo)
- `Gestao\Dashboard.cshtml` 1 (badge), `ContasFinanceiras.cshtml` 1 (alerta), `ComissoesPendentes.cshtml` 1 (card vazio),
  `OrcamentoNovo.cshtml` 1 (ajuda de lote), `FluxoCaixa.cshtml` 1 ("até" entre datas)

## Decisão D-R26-1 — Regra de correção

1. **Fundo escuro** → `text-muted` vira **`text-white-50`** (classe Bootstrap nativa: branco 50%, ≈ `#adb5bd` sobre
   preto — a mesma família de clareza que o muted em fundo claro, desenhada para escuro; mantém hierarquia visual de
   "texto secundário" sem perder legibilidade).
2. **Elemento já com cor correta** (`ComissoesPendentes`: tinha `text-light` E `text-muted` conflitando por ordem de
   CSS) → remover `text-muted`.
3. **Fundo claro** (`ContasFinanceiras` L190: `alert-secondary` é cinza-claro) → remover `text-muted` (texto padrão
   escuro do alerta tem contraste bom sobre o cinza; `text-white-50` aí seria o erro simétrico).

Aplicado nos 19 pontos (r26b1); scanner pós-fix: **TOTAL=0**.

## Fato permanente

`R26ContrasteZonasEscurasContratoTests`: port C# do mesmo algoritmo (controles positivo/negativo + varredura global das
views). Qualquer novo `text-muted` dentro de zona escura em qualquer view futura falha a suíte — a regra da diretriz
("área escura não pode ter letra escura") passa a ser contratada, não só corrigida. Aproximação de parser documentada
no cabeçalho do fato (ângulos em expressões Razor; tags falsas de generics entram sem classe e nunca ativam o gatilho).

## Resultado (atualizado pós-r26b1)

O recon com o scanner PS (19 pontos) subestimou o escopo: o port para C# do fato — mais fiel ao
algoritmo — expôs que `text-muted` em zona escura é **padrão de casa** nas views Adm360: a varredura
final apontou 192 linhas violadoras em 35 views, somando **214 trocas/2 remoções em 38 arquivos**
(manuais + lote). A auditoria do diff confirmou que cada linha alterada é exclusivamente a troca/remoção
da classe (sem tocar conteúdo). A regra de superfícies claras próprias (`alert-*` claros dentro de card
escuro) foi validada em controle negativo do fato e preservada nas 2 remoções manuais (alert claro).

## Plano de fatias R26

| Fatia | Conteúdo | Fatos |
|---|---|---|
| r26b1 | Correções em lote (38 views) + fato-scanner global | 2 fatos (controles + varredura global) |
| r26z | Consolidado + suíte (base 1538) + push | — |
