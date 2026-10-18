# R28 · r28a0 — E1 linguagem simples, zona Adm360 parte 1 (suprimentos/compras/fiscal) (inventário file:line + decisões)

Data: 2026-10-18 · Sessão: `opencode` · Base: R27 (`b47951b`, suíte 1542).

## Método

Dois scans em `Administrativo360` (66 views): tokens técnicos amplos (`%TEMP%\opencode\r28_scan_jargao2.ps1`:
outbox/hash/sincronização/token/uuid/etc.) + grep dedicado de `UUID` (9 ocorrências) e `score` (0).
Cada ocorrência renderizada foi classificada abaixo; comentários HTML/Razor e código JS/C# entram na whitelist.

## Classificação REESCREVER (texto visível ao usuário → linguagem simples)

| # | Local | Texto atual → novo |
|---|---|---|
| 1 | `OrcamentoNovo.cshtml` L76 (placeholder) | "UUID do Vendedor (opcional)" → "Código do vendedor (opcional)" |
| 2 | `ValeNovo.cshtml` L133 (cabeçalho de tabela) | "Reserva Vinculada (UUID)" → "Reserva vinculada" |
| 3 | idem L161/L236 (placeholders, 2×) | "UUID da reserva ativa (opcional)" → "Código da reserva ativa (opcional)" |
| 4 | `Cotacoes\Relacionamento.cshtml` L157 (label) | "ID Interno UUID (Opcional)" → "Código interno (opcional)" |
| 5 | `DocumentosXml\Detalhes.cshtml` L231 (alerta do modal) | "A operação é idempotente e auditada." → "Se você repetir a operação, nada é duplicado, e tudo fica registrado para auditoria." |
| 6 | idem L234 (label) | "ID do Pedido de Compra (UUID)" → "Pedido de compra" |
| 7 | idem L238 (label) | "ID do Local de Estoque (UUID)" → "Local de estoque" |
| 8 | `Gestao\Monitor.cshtml` L157 (título do card) | "Fila e Transmissões Outbox" → "Envios para os fornecedores" |
| 9 | idem L153 (comentário HTML) + L179 (empty state) | comentário sem "Outbox"; "Nenhuma transmissão outbox registrada." → "Nenhum envio para os fornecedores registrado ainda." |
| 10 | `TituloDetalhes.cshtml` L164 (nota) | "Recebimento registrado manualmente pelo operador interno. Sem confirmação bancária automática por API." → "Recebimento registrado manualmente pela sua equipe. Sem confirmação automática com o banco." |
| 11 | `Fiscal\Configurar.cshtml` L133 (ajuda do campo) | "…O valor fica no ambiente da API (<code>Fiscal:Credenciais:<nome></code> no pool da API) — nunca no banco, nunca no Web." → "Só o nome da referência (A33). O valor do certificado fica em configuração separada, nunca no banco de dados nem nas telas." |
| 12 | `Fiscal\Nova.cshtml` L45 (label) | "Identificador do documento de origem (GUID)" → "Código do documento de origem" |
| 13 | `DocumentosXml\Importar.cshtml` L31 (lista de regras) | "(incluindo BOM)… download idêntico ao enviado, com hash SHA-256 verificado" → "Os bytes originais do arquivo são preservados: o download é idêntico ao enviado, com integridade verificada (SHA-256)." |

Decisões de conteúdo:
- **SHA-256 mantido** no item 13: informação de integridade útil ao auditor; só a redação muda ("hash verificado" → "integridade verificada").
- **Placeholders com formato** (`00000000-0000-…`) permanecem: mostram o formato esperado sem nomear a tecnologia.
- **E6 futuro**: esses campos ainda exigem o usuário colar o código na mão; a conversão para busca por nome
  (select) fica registrada na fila do epic E6 (R32/R33) quando as view-models ganharem lookups.

## Whitelist (código/comentário — NÃO renderiza, manter)

- `Cotacoes\Detalhes.cshtml` L275 (comentário Razor `@* *@`) e L334 (comentário HTML) — menções a UUID na doc do s8.
- `Gestao\Monitor.cshtml`: colunas "Cotação / Identificador", "Protocolo Externo", "Tentativas" — cabeçalhos de
  tabela operacionais compactos, sem jargão de arquitetura.
- Expressões `@Guid.NewGuid()` em inputs hidden `idempotencyKey` (TituloDetalhes L113/L241, ValorizacaoPrevia L212).
- URLs `/api/...` em href (Gestao\Dashboard) e tokens fiscais SEFAZ/DIF (domínio fiscal, whitelisted desde R25).

## Fato permanente (r28b1)

`R28LinguagemSimplesAdm360ContratoTests`: marcadores anti-regressão dos literais antigos (inclui regra estrutural
`DoesNotContain("outbox", OrdinalIgnoreCase)` no Monitor) + presença dos textos novos nos 8 arquivos reescritos.

## Plano de fatias R28

| Fatia | Conteúdo | Fatos |
|---|---|---|
| r28b1 | Os 13 pontos (8 views) | 2 fatos (anti-regressão + presença dos novos textos) |
| r28z | Consolidado + suíte (base 1542) + push | — |

Próxima fatia E1 (R29): demais módulos fora de Adm360 — Financeiro, MinhaAssinatura, Escalas, Pagamentos,
AdminSaas, Comercial, Lgpd, Observabilidade, V114 restantes (inventário parcial já mapeado no resíduo do scan
"pela API" da R27).
