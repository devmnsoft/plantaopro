# Rodada 23 — Auditoria inicial e plano de sprint (r23a0)

- **Data:** 2026-10-18 · **Base:** `ec3d424` (r22z, ponta de `origin/main`) · **Escopo:** fila herdada das rodadas anteriores — **FecharCaixa F14** (achado r16z/R17) + **F-R15-C** (achado R15, herança R16–R22) · Diretriz: "continuar pelas próximas 10 sprints (R19→R28) e avançar também o design do sistema". Push ao fim da rodada segue autorização genérica registrada em r19z.

## Achados sob análise

### F14 (P3) — "modal de FecharCaixa não tem campo justificativa; API exige quando diferença ≠ 0"

**Auditoria executada — FALSO POSITIVO / já implementado desde antes da observação:**

1. **View:** `backend\PlantaoPro.Web\Views\Administrativo360\FechamentoCaixa.cshtml` — o form do modal `asp-action="FecharCaixa"` (L198) contém o campo `textarea id="justificativaFechamento" name="justificativa"` rotulado "Justificativa de Divergência", com placeholder orientando que é obrigatório quando há diferença (L238-240). O commit mais recente do arquivo é `8669951` (r16b2) e `git show 8669951:…FechamentoCaixa.cshtml` **já contém o campo** — ou seja, o achado r16z foi contemporâneo do commit que o descrevia como ausente; a observação estava defasada da própria superfície.
2. **BFF:** `Administrativo360Controller.Financeiro.cs` L517 — assinatura `FecharCaixa(Guid contaFinanceiraId, DateOnly dataInicio, DateOnly dataFim, decimal saldoConferido, string token, string? justificativa)`; L531 repassa `Justificativa = justificativa` no payload POST `api/administrativo360/caixa/fechar`. Encadeamento completo e vivo.
3. **Domain:** `CaixaRegras.ValidarFechamento` (`PlantaoPro.Domain\Administrativo360\FinanceiroVendas.cs` L192-197) — exige justificativa **apenas quando** `|conferido − calculado| > 0` e o texto está em branco; diferença zero dispensa. O 400 observado na smoke R16 era o comportamento correto da regra com campo vazio (o smoke r16b2 homologou "fechar caixa com diferença ZERO").

**Decisão D-R23-1:** fechar sem mudança de código de produto; travar a cadeia de ponta a ponta com fatos permanentes (evita que alguém "ajuste" o campo fora do form ou troque a semântica da regra sem alarme):
- fato unitário puro do domain (diferença ≠ 0 × vazio/branco ⇒ ArgumentException c/ mensagem; diferença ≠ 0 × preenchido ⇒ passa; diferença 0 × vazio ⇒ passa);
- fato de fonte: slice do form `FecharCaixa` contém `name="justificativa"` E o trecho da ação BFF contém bind `string? justificativa` + encaminhamento `Justificativa = justificativa`.

### F-R15-C (P3) — form de Recebimentos não preenche `pedidoItemId` ao selecionar pedido

**Causa raiz confirmada:**
1. `Recebimentos.cshtml` L172: `<input type="hidden" name="pedidoItemId" value="@Guid.Empty">` — o form tem seletor de **pedido** (`select name="pedidoId"` L109) mas **nenhum seletor de item**; o campo de item fica `00000000-…` até submissão manual.
2. Superfície: `PedidoResumo` (Application Contracts L20) e espelho `PedidoCompraResumoViewModel` (Web Models L1080) **não expõem itens** — impossível ao client saber os ids de item do pedido selecionado.
3. API: `ComprasRepository.ReceberInternoAsync` L336-348 faz lookup `i.id = @id AND i.pedido_id = @PedidoId` em `adm360_pedido_itens` — GUID vazio ⇒ `row is null` ⇒ 400 "Quantidade excede o saldo autorizado do pedido." (exatamente o modo de falha do achado).
4. O BFF `ConfirmarRecebimento` (`Administrativo360Controller.ComprasRecebimentos.cs` L164) guarda pedidoId/documento/quantidade/localId mas **não** pedidoItemId — o GUID vazio atravessa até a API.

**Decisão D-R23-2:** mesmo padrão do r22b2 (extensão de superfície, não gambiarra de form):
- Application: novo `record PedidoItemResumo(Id, Produto, Sku, Quantidade, QuantidadeRecebida)` + parâmetro `Itens` em `PedidoResumo` (único site de construção: `ComprasRepository.ListarAsync` L54 — verificado por grep); `ListarAsync` ganha segunda query por lote `ANY(@ids)` (padrão `adm360_produtos … id = ANY(@ids)` já usado no próprio arquivo L72) agrupada por pedido; classe intermediária `PedidoItemRow` (não há timestamptz, mas o agrupamento exige o `pedido_id`).
- Web: espelho grafia idêntica `PedidoItemResumoViewModel` + `Itens` em `PedidoCompraResumoViewModel` (ADR-0003; entrada do dicionário do contrato R20 já existe — `"api/administrativo360/compras"` L63 — e cobre recursivamente os props novos sem mudança de teste).
- View: o hidden input é substituído por um `<select name="pedidoItemId" required>` com `<optgroup data-pedido="@ped.Id">` por pedido e opções `Sku — Produto (saldo N)`; JS no padrão da casa (função global + `change` no select de pedido) filtra os optgroups e auto-seleciona quando resta exatamente 1 item visível; o botão por-linha "Conferir Entrada" (L80) passa a chamar o filtro após setar `.value` (setAttribute direto não dispara `change`). Literais pt-BR crus — zona pré-conversão i18n (consistente com as telas de Suprimentos).
- BFF: guarda L164 ganha `|| pedidoItemId == Guid.Empty` usando a key existente `Adm360.Compras.ErrReceiptRequiredFields` — **zero novas keys resx** (contador R7DF6 653 intacto).
- Scanner: o conjunto de campos do form não muda (mesmo `name="pedidoItemId"`, agora num select) — superfície **83/212/76 intacta**.
- Fatos: (a) reflexivo — paridade de props Web↔Api nos dois records (mini-contrato ADR-0003); (b) de fonte — view tem select `name="pedidoItemId"` com optgroups `data-pedido` e o hidden de GUID vazio sumiu; BFF guarda `pedidoItemId == Guid.Empty`; (c) de banco — seed parceiro/produto/local (padrão R8B4, tenant Santa Casa), `CriarAsync`+`AprovarAsync` ⇒ `ListarAsync` expõe o item c/ quantidade e saldo zero ⇒ `ReceberAsync(2)` ⇒ `QuantidadeRecebida` atualiza para 2.

## Plano de fatias

| Fatia | Conteúdo |
|---|---|
| **r23b1** | F14: fatos permanentes (domain unit + fonte) — sem código de produto (falso positivo) |
| **r23b2** | F-R15-C: extensão DTO/repo/espelho + view seletor de item + guarda BFF + 3 fatos |
| **r23z** | Relatório consolidado + tentativa de desbloqueio da smoke UI (quirk sessão portal gestor, aberta desde R19) + push |

## Protocolo de casa (mantido)

- Commits `<seções> (rodada 23 r23xx)` sem acentos; evidência versionada aqui neste diretório; push simples `git push origin main` (gitconfig local wincred).
- Gate **com rebuild real** `--no-incremental` antes do compare (lição r22b2: script `r20b1_gate.ps1` só compara logs — build precede sempre).
- Suíte 1524 + fatos novos; flake de banco e2e sob carga ⇒ isolate 5× (protocolo). C# 10; sem novas keys resx Adm360.
