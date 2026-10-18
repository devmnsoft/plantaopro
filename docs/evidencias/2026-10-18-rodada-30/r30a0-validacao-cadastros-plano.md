# R30 · r30a0 — E5 formulários parte 1: auditoria + validação (cadastros)

Data: 2026-10-18 · Sessão: `opencode` · Base: R29 (`3d011fd`, suíte 1546).

## Auditoria (scan `%TEMP%\opencode\r30_scan_formas.ps1`)

59 views com `<form` em Adm360 · 342 inputs · `required` em 164 · `maxlength` em 43 · `pattern` em 3 ·
handler JS de submit (~checkValidity/reportValidity) em ~6 views. Tamanhos de coluna levantados direto do
banco (`plantaopro`, psql) e usados como teto dos `maxlength`.

**Resultado da auditoria — os cadastros detalhados JÁ são válidos** (Parceiros, Produtos/detalhe, Locais,
Cargos, Colaboradores, Departamentos têm required + maxlength coerentes + type=email). Os buracos reais:

| # | Local | Problema → correção (D-R30-1) |
|---|---|---|
| 1 | `Cadastros.cshtml` L286 (modal parceiro, nome) | sem teto → `maxlength="160"` (coluna 160) |
| 2 | idem L290 (modal parceiro, documento CNPJ/CPF) | sem teto/formato → `maxlength="20" pattern="[0-9./ -]*"` (coluna 20; aceita formatado e puro) |
| 3 | idem L321 (modal produto, sku) | sem teto → `maxlength="40"` (coluna 40) |
| 4 | idem L325 (modal produto, nome) | sem teto → `maxlength="180"` (coluna 180) |
| 5 | idem L331 (modal produto, unidade) | sem teto → `maxlength="12"` (coluna 12) |
| 6 | idem L335 (modal produto, preço custo) | number sem piso → `min="0"` (custo negativo é impossível no domínio) |
| 7 | idem L339 (modal produto, código de barras) | sem teto/formato → `maxlength="80" pattern="[0-9 -]*"` (EAN/GTIN) |
| 8 | idem L371 (modal local, código) | sem teto → `maxlength="30"` (coluna 30) |
| 9 | idem L375 (modal local, nome) | sem teto → `maxlength="120"` (coluna 120) |
| 10 | `Produtos.cshtml` L355 (sku detalhe) | **sobreteto**: `maxlength="60"` > coluna 40 → usuário digita o que o banco rejeita → `maxlength="40"` |
| 11 | idem L371 (nome detalhe) | sobreteto: `maxlength="200"` > coluna 180 → `maxlength="180"` |

## Decisão D-R30-1 (regra de validação da casa para E5)

- Validação **nativa do HTML5** (`required`, `maxlength`, `pattern`, `min`, `type=email`) — sem framework
  novo; casa já tem o padrão checkValidity/reportValidity em poucos lugares e não há motivo para divergir.
- `maxlength` = **tamanho exato da coluna** (fonte: information_schema do banco dev) — nem mais (o banco
  vai rejeitar e vira 500 de banco) nem menos (trunca dado legítimo).
- `pattern` só quando o formato é objetivamente fechado (documento CNPJ/CPF, EAN/GTIN); sempre
  full-match implícito do HTML5; campo opcional segue aceitando vazio (pattern só valida valor presente).
- O servidor continua autoridade (BFF→API via TempData + `_AlertasAcao`); o lado cliente é prevenção UX.
- Sobretetos (maxlength > coluna) são BUG, não estilo: o usuário passa a errar no banco.

## Fato permanente (r30b1)

`R30ValidacaoFormulariosCadastrosContratoTests`: por arquivo, regex confirma os atributos nos campos
(nome+maxlength exato, documento+pattern, sku/nome/unidade/preco/codigoBarras/local, e os dois sobretetos
do Produtos.cshtml sumidos de seus ids). Não trava strings — trava **contrato de atributo**.

## Plano de fatias R30

| Fatia | Conteúdo | Fatos |
|---|---|---|
| r30b1 | Os 11 pontos (Cadastros.cshtml modais + Produtos.cshtml) + 1 fato | 2 fatos |
| r30z | Consolidado + suíte (base 1546) + push | — |

Próxima: R31 = E5 parte 2 (formulários operacionais — recebimentos, movimentações, faturamento/financeiro —
mesma régua D-R30-1, tamanhos de coluna levantados na hora).
