# Rodada 16 — r16b2 - Smoke vivo dos fluxos financeiros Adm360 + F9d (`Ativa`→`Ativo`)

**Data:** 2026-10-17 · **Base:** `5ad0de9` (r16b1) · **Escopo:** homologação ao vivo dos 4 binds do r16b1 (Pagar Título, Fechar Caixa, Reabrir Caixa) no tenant SV, com prova de efeito físico no banco dev; durante o smoke, descoberta e correção do **F9d** — 5º caso da família F-R15-A, em nível de deserialização JSON (não de form binding). Sem DDL, sem resx (D7 segue: 653 chaves intactas).

## 1. Ambiente e seed

| Item | Valor |
|---|---|
| Apps | Api `http://localhost:51976` · Web `http://localhost:52976` (Release, `--no-build`, binário do gate r16b1 → trocado pós-F9d, §3) |
| Login | SV `admin.sistemavertical@plantaopro.local` → user `8b0c8e74-a81b-4ea2-b499-94755a1ca310` · tenant `8b0c8e74-a81b-4ea2-b499-94755a1ca301` |
| Seed (falta de pré-requisito) | **0 contas financeiras no tenant SV** — select "Selecione uma conta..." vazio não era bug, era ausência de dado. Inserido direto no banco dev: `adm360_contas_financeiras` id `405dbf51-a38a-4063-949a-4d97e18ae112` "Cofre Smoke R16", tipo `BANCO`, `saldo_inicial` 500,00, `ativo`=true |
| Alvo Pagar Título | `TP-SV-R9G-01` (`e9a00011-9f0f-4a11-8f0f-000000000011`), R$ 40,00, `DESPESA_MANUAL`, Aprovado, vencido |

## 2. Achados novos da fatia

| # | Achado | Evidência |
|---|--------|-----------|
| F9d | **O select de conta do Pagar Título nunca renderizou nenhuma opção, em nenhum tenant** — 5º caso da família F-R15-A, mas em camada diferente dos F9 (binding de form): o DTO da API é `ContaFinanceiraDto(bool Ativo)` → JSON `"ativo"`, e o ViewModel Web esperava o parâmetro **`Ativa`**. System.Text.Json com `PropertyNameCaseInsensitive=true` ignora só caixa, não grafia: `ativo ≠ ativa` ⇒ a propriedade **nunca populava ⇒ sempre `false`** ⇒ todo consumidor `.Where(c => c.Ativa)` zerava. **5 consumers / 8 referências em 5 views**: select Pagar Título (`TituloPagarDetalhes` L304), select Receber/Pagar Título (`TituloDetalhes` L119), select Fechar Caixa (`FechamentoCaixa` L211), `FluxoCaixa` L149, e os KPIs/badges de `ContasFinanceiras` (saldo total sempre R$ 0,00, contador "0 ativas", badge Ativa/Inativa invertido). Diagnóstico ao vivo: chamada direta à API (Bearer) devolvia a conta com `"ativo": true` enquanto a view renderizava vazio; o filtro `filtroConta` da mesma tela de fechamento (que **não** filtra por Ativa) exibia a conta — diferencial que fechou o raciocínio. Convenção dos irmãos já era `Ativo` (`Departamento360ViewModel`, `Cargo360ViewModel`). Fix = rename do parâmetro `Ativa` → `Ativo` + 8 refs nas views (nenhum uso em `.cs` fora do record). | chamada direta `POST /api/auth/login` + `GET /api/administrativo360/caixa/contas` (devolveu a conta) × render da view (vazio); `Application/Administrativo360/Contracts.cs` L347 (`bool Ativo`) vs `Web/Models/Administrativo360Models.cs` L593 (era `bool Ativa`); 6 call-sites idênticos `ReadApiResponse<List<ContaFinanceiraViewModel>>(client, "api/administrativo360/caixa/contas")` confirmam fonte única |
| F14 | **Lacuna latente do Fechar Caixa:** quando há diferença entre saldo conferido e calculado, a API exige `justificativa` (400 "A justificativa é obrigatória."), mas **o modal FecharCaixa não tem campo `justificativa`** (action aceita `string? justificativa`; form tem só conta/datas/saldo/token) ⇒ com diferença ≠ 0 o usuário fica sem caminho para concluir. Reprodutível no smoke (tentativa 1, período 09-17..10-17, conferido 460 × calculado −40 ⇒ diferença 500). Fora do escopo do fix desta fatia — vai para fila R17. | log Web: `POST …/caixa/fechar Status:400` c/ body da regra de negócio; inventário do form (inputs+textarea) sem nome `justificativa` |

## 3. Smoke vivo (sequência e efeito físico)

Executado no browser (tab de trabalho), binário do r16b1 → após o F9d, rebuild e **restart dos dois apps** (sessão in-memory do Web invalidada ⇒ re-login) → binário final da suíte 1476.

| Passo | Ação | Resultado / prova |
|---|---|---|
| S1 | **Pagar Título** (valida F9a: `id` + `valorPago`) — detalhe `TP-SV-R9G-01`, select conta (populado só após F9d), valor 40,00, 17/10/2026, PIX, "Confirmar Pagamento" | Redireção 302 + **DB**: `adm360_titulos_pagar.situacao` = `PAGO`, `valor_pago` 40,00, `saldo_aberto` 0; linha `adm360_titulo_pagamentos` id `3b1760b0-…77d` (conta Cofre, PIX, key de idempotência `f811b633…c070`, `pago_por` = user SV); movimento `77637211-…24b0` tipo `SAIDA` 40,00 "Pagamento manual título TP-SV-R9G-01" vinculado via `movimento_financeiro_id` |
| S2 | **Fechar Caixa** (valida F9b: `contaFinanceiraId`) — tentativa 1 com período 09-17..10-17 ⇒ 400 de regra de negócio (F14, diferença 500) — **o POST chegou e foi processado pela API**, o que já valida o bind; tentativa 2 com período 17-10..17-10 (abertura 500 − saída 40 = calculado 460 = conferido, diferença 0) | 200 + linha `adm360_caixa_fechamentos` id `e15ced2f-…58eb`: `situacao` `FECHADO`, `saldo_abertura` 500, `saldo_calculado` 460, `saldo_conferido` 460, `diferenca` 0, `fechado_por` user SV, `fechado_em` 11:15:39 |
| S3 | **Reabrir Caixa** (valida F9c: `fechamentoId`) — modal Reabrir, motivo preenchido no textarea (campo `name="motivo"`), "Confirmar Reabertura" | 200 + mesma linha `e15ced2f-…58eb` → `situacao` `REABERTO`, `motivo_reabertura` gravado, `reaberto_por` = user SV, `reaberto_em` 11:16:11 |
| S4 | **Tela de KPIs** (visibilidade do F9d corrigido) — `/Administrativo360/ContasFinanceiras` | "Saldo Total em Contas Ativas **R$ 460,00**" (antes do fix: sempre 0), badge **"Ativa"** na linha, contador "1 contas" |
| S5 | **Classe do leak F-R14-C** — `/MinhaAssinatura` | Renderiza limpo, **sem o texto "Sucesso" vazando como alerta** (procura exata na página: ausente). Detalhe honesto: a API respondeu 404 "Organização sem cliente vinculado para a assinatura." — o tenant SV não tem cliente de assinatura, então o caminho **2xx** ao vivo não passou dessa tela nesta rodada; a classe segue coberta pelo 13 fatos do parser (r16b1) + homologação de tela da R14. O 404 foi tratado corretamente (mensagem real na Error, página sem ruído) |

Apps **parados** ao fim da fatia (portas 51976/52976 liberadas por kill — o build `--no-incremental` do gate exige). Estado deixado no banco dev: conta "Cofre Smoke R16" ativa (saldo 460,00), título `TP-SV-R9G-01` pago, fechamento `e15ced2f-…` em `REABERTO`.

## 4. Changes aplicadas

| Arquivo | Change |
|---|---|
| `backend/PlantaoPro.Web/Models/Administrativo360Models.cs` | `ContaFinanceiraViewModel`: parâmetro `bool Ativa` → **`bool Ativo`** + comentário F9d (casamento com a chave `"ativo"` do DTO). |
| `backend/PlantaoPro.Web/Views/Administrativo360/ContasFinanceiras.cshtml` | 4 refs `.Ativa` → `.Ativo` (L5 saldo total, L66 contador ativas, L117/L132 badges/ações). |
| `backend/PlantaoPro.Web/Views/Administrativo360/TituloPagarDetalhes.cshtml` | 1 ref (select do pagamento, L304). |
| `backend/PlantaoPro.Web/Views/Administrativo360/TituloDetalhes.cshtml` | 1 ref (select de recebimento/pagamento, L119). |
| `backend/PlantaoPro.Web/Views/Administrativo360/FechamentoCaixa.cshtml` | 1 ref (select Fechar Caixa, L211). |
| `backend/PlantaoPro.Web/Views/Administrativo360/FluxoCaixa.cshtml` | 1 ref (L149). |
| `backend/PlantaoPro.Tests/R16WebParserErrorContratoTests.cs` | Fato 13 (§5) — contrato sobe de 12 para 13. |

## 5. Fato adicionado (contrato permanente)

13. `GET_2xx_contas_financeiras_ativo_da_api_popula_vm_regressao_f9d` — stub devolve o array crú exatamente como a API serializa (`"ativo": true` + demais chaves do `ContaFinanceiraDto`); `ReadApiResponse<List<ContaFinanceiraViewModel>>` no parser real ⇒ `Error == null` e **`Ativo == true`** (com `Nome`/`SaldoAtual` conferidos). Trava a regressão do mismatch: se alguém renomear de volta para `Ativa` (ou mudar o DTO sem atualizar o VM), o fato falha.

## 6. Gate

- Build Release completa `--no-incremental`: **0 erros CS**; avisos em **71 linhas únicas (80 com repetições) = idêntico ao baseline r16a0** (`Compare-Object` dos sets linha-a-linha: vazio; logs `%TEMP%\opencode\r16b2_warn_now.txt` vs `r16b1_warn_base.txt`).
- Run isolado da família R16: **13/13 aprovados** (12 do r16b1 + fato 13).
- Suíte full `dotnet test backend\PlantaoPro.Tests -c Release --no-build`: **1476/1476 aprovados, 0 falhas**, 36 s (baseline 1463 + 12 r16b1 + 1 r16b2). I18n 653 intacto (fato dentro da suíte verde — D7).
- Família ① (40P01): não acionou.

## 7. Restante da rodada

- **r16z:** consolidado final + fila R17 atualizada (acrescentar **F14** — campo `justificativa` ausente no modal FecharCaixa — junto com a extensão do scanner para `Adm360*WebController*` e os fantasmas do catálogo).
- Push da rodada (r16a0+r16b1+r16b2+r16z) **somente com autorização explícita** ao fim da rodada.
