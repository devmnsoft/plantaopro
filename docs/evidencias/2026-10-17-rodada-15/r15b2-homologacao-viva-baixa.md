# R15b2 — Homologação viva: ciclo de ocorrência com efeito físico BAIXA

**Rodada 15 · fatia r15b2 · base `8db207f` (r15b1) · banco dev `plantaopro`, tenant SV**

Objetivo: smoke vivo ponta a ponta do contrato novo do r15b1 no browser real, contra o banco
de homologação — criar o pré-requisito (parceiro FORNECEDOR → pedido → recebimento que gera
saldo LIBERADO), abrir uma ocorrência, tratar, resolver (sem liberação) e **encerrar com
`efeitoFisico=BAIXA`**, provando no banco a perna única `BAIXA_OCORRENCIA` e o saldo final.

Ciclo autocontido e aprovado pelo usuário: os resíduos permanecem em entidades de smoke do
próprio tenant SV (documentadas na seção 4); nenhum grant/banco fora da homologação foi tocado.

## 1. Ambiente

| Item | Detalhe |
|---|---|
| Api | `dotnet run --no-build -c Release` + override `ConnectionStrings__Default` → `Database=plantaopro` (user-secrets aponta `plantaopro_test`; override via env na subida). Log de subida com `Database:plantaopro`. |
| Web | `dotnet run --no-build -c Release` (launchSettings 52976, Development) |
| Browser | Login SV (`admin.sistemavertical@plantaopro.local`) em aba dedicada |
| psql | `127.0.0.1:5432/plantaopro` (user `postgres`), bats em `%TEMP%\opencode\r15b2_*.bat` |

**Baseline ao iniciar** (`r15b2_base.out`): 3 produtos, 2 lotes (LT-2026-001/002),
**0 locais, 0 saldos, 0 movimentos** no tenant SV.

## 2. Passo a passo executado (com IDs reais)

| # | Ação | Entrada / resultado |
|---|---|---|
| 1 | Criar local interno | `DEP-A-15 — Depo A - smoke R15` → id `7dcb4748-7340-4ed6-9299-36b77eb98264` (persistiu antes do crash intermediário — conferido no banco) |
| 2 | Criar parceiro Fornecedor | `Distribuidora R15 (smoke)` → id `12343a45-0835-47ec-86d9-9fe332282da9` (o submit interrompido mid-flight **persistiu** — conferido no banco antes de reenviar) |
| 3 | Criar pedido de compra | `CriarPedidoCompra` (fornecedor acima, `SV-LTE-001 Seroma de Teste p/ Lote` `3a002ffc-…`, qtd 10 @ 100,00, frete 0) → **PC-1807** `4b2b0820-38c2-4d3a-8a30-723f964cf420` RASCUNHO; item `35f461ed-ef39-42b8-aff0-e2bb6395b784` |
| 4 | Aprovar pedido | `AprovarPedidoCompra` → APROVADO (estado congelado) |
| 5 | Confirmar recebimento | `ConfirmarRecebimento` (pedidoId PC-1807, documento `NF-15-0001`, pedidoItemId `35f461ed-…` — **setado manualmente**, ver G2.4, lote `L15-001` validade 17/10/2028, local DEP-A-15, qtd 10) → recebimento `7669be40-e4dc-447d-b436-e7def39583b8`; PC-1807 → RECEBIDO; saldo **LIBERADO 10** criado |
| 6 | Criar ocorrência | `CriarOcorrencia` (produto/lote `e4ab0f01-b1ce-4dbc-9e49-ba37f34b4ea1`/local DEP-A-15, tipo `QUEBRA`, qtd 3) → ocorrência `3877c269-277b-4070-9f79-5403276efd1a` ABERTA v1; **bloqueio imediato aplicado**: LIBERADO 7 / BLOQUEADO 3 |
| 7 | Tratar | `TratarOcorrencia` v1→v2 → EM_TRATAMENTO |
| 8 | Resolver | `ResolverOcorrencia` v2→v3 → RESOLVIDA; **saldo conferido sem liberação**: BLOQUEADO segue 3 (comportamento canônico r15b1: resolver não toca saldo) |
| 9 | Encerrar c/ BAIXA | `EncerrarOcorrencia` (destino `Descarte assistido`, efeitoFisico **BAIXA**, justificativa `Quebra confirmada no descarte assistido (smoke R15b2).`) v3→v4 → ENCERRADA |

## 3. Evidência final no banco (`r15b2_final.out`)

**Ocorrência**: `ENCERRADA` v4 · destino `Descarte assistido` · justificativa registrada.

**Saldos** (lote L15-001 @ DEP-A-15): `LIBERADO 7` / `BLOQUEADO 0` — exato: 10 recebidas − 3 baixadas = 7.

**Movimentos** (histórico completo do lote, na ordem):

| Tipo | Condição | Qtd | Idempotency key | Motivo |
|---|---|---|---|---|
| ENTRADA | LIBERADO | +10 | `REC-6181c185…:35f461ed-…` | (recebimento) |
| BLOQUEIO_OCORRENCIA | LIBERADO | −3 | `3877c269-…:BLQ:7dcb4748-…:SAIDA_LIBERADO` | Bloqueio imediato da ocorrencia QUEBRA - … (ocorrencia 3877c269-…) |
| BLOQUEIO_OCORRENCIA | BLOQUEADO | +3 | `3877c269-…:BLQ:7dcb4748-…:ENTRADA_BLOQUEADO` | (idem) |
| **BAIXA_OCORRENCIA** | BLOQUEADO | −3 | `3877c269-…:BXA:7dcb4748-…:BAIXA_BLOQUEADO` | Baixa fisica do saldo bloqueado da ocorrencia (ocorrencia 3877c269-…) |

Conformidade com o design r15b1 (D1–D8): par de bloqueio simétrico na abertura, **perna única**
de baixa com a chave idempotência canônica `{ocorrenciaId}:BXA:{localId}:BAIXA_BLOQUEADO` e o
motivo canônico, soma física do ciclo ≠ 0 apenas pela perna autorizada, RESOLVIDA sem efeito
de saldo. **Smoke vivo: PASS.**

## 4. Resíduo documentado (autocontido, homologação)

No tenant SV: local `DEP-A-15`, parceiro `Distribuidora R15 (smoke)`, pedido PC-1807
(RECEBIDO), recebimento NF-15-0001 (lote L15-001), saldo residual **LIBERADO 7** de
Seroma de Teste p/ Lote em DEP-A-15, título a pagar gerado pelo recebimento (padrão do módulo)
e ocorrência ENCERRADA. Nenhum write fora desta cadeia; sem DDL, sem grant, sem schema alterado.

## 5. Achados e gotchas desta fatia (com prova)

- **G2.1 — shells de fundo morrem com a interrupção da rodada**: ambos os apps caíram junto da
  interrupção de ferramenta anterior (connection refused + arquivos `.out` dos shells sumidos);
  relançados com o mesmo padrão de subida (Api com override de connection string). O 502 nas abas
  antigas era estado de página morta pós-crash, não proxy.
- **G2.2 — restart do Web invalida a sessão in-memory**: nova aba caiu em `/Account/Login`
  após o relance do Web; re-login resolveu (coerente com sessão server-side volátil).
- **G2.3 — o `data-confirm` custom intercepta `requestSubmit()`**: a 1ª tentativa de aprovar o
  pedido só abriu o modal (pedido seguiu RASCUNHO no banco, sem alerta visível porque a view não
  renderiza `TempData`). Corrigido clicando no botão de confirmação do modal a cada ação — padrão
  que se manteve em receber/tratar/encerrar.
- **G2.4 — `pedidoItemId` do form de recebimento não é auto-preenchido** pela JS ao selecionar o
  pedido (ficava `00000000-…`); preenchido manualmente a partir de `adm360_pedido_itens`. Vale
  para fila R16 (P2) — candidato a correção no view script.
- **G2.5 — escape `%%` em .bat confirmado mais uma vez**: query `ilike '%x%'` com `%` simples
  retornou 0 linhas; duplicado, ok.

## 6. Estado e próximos passos

- Commit desta fatia (documento apenas — o código é do r15b1).
- **r15z**: relatório consolidado da rodada + fila R16. Push só com autorização explícita ao fim da rodada.
