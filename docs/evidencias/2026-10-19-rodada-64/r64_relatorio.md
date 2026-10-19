# R64 — Smoke de escrita com seed demo idempotente (fila r53b1 §7 item 9)

Data: 2026-10-19 · Base: `d20af4b` (R63 fechada) · Suíte: **1647 → 1648** (+1 fato)

## Decisão (D-R64-1)

O item 9 dizia: *"Smoke de escrita (criar→aprovar→receber) quando existir seed demo
idempotente em plantaopro_test"*. Escolhido o fluxo real de **Pedidos de Compra**
do Administrativo 360 — o único que materializa exatamente esses três verbos na
UI (o artigo de ajuda `adm-compras` já documenta o passo a passo) — e criado o
fixture que faltava:

| Artefato | Papel |
|---|---|
| `database/seeds/2026_r64_smoke_escrita_compras_demo.sql` | 3 linhas fixas (UUIDs determinísticos) para o tenant do demo Santa Casa: fornecedor `Fornecedor SMOKE R64 (teste)`, produto SKU `SMK64` (sem lote/inspeção obrigatória → recebimento direto ao estoque) e local interno `SMK64ALM`. Upsert puro por PK (`ON CONFLICT (id) DO UPDATE`) — reaplicável sem duplicar, sem apagar nada, sem tocar em outra linha |
| `docs/evidencias/2026-10-19-rodada-64/r64_smoke_escrita.ps1` | Smoke ponta a ponta pelo **Web real** (forms + antiforgery): aplica o seed 2× seguidas (prova de idempotência), login do demo, CRIAR pedido → APROVAR → RECEBER, e confere o estado final direto no banco |
| `backend/PlantaoPro.Tests/R64SmokeEscritaContratoTests.cs` | 1 fato travando o contrato do fixture: UUIDs fixos, exatamente 3 upserts, ausência de DELETE/TRUNCATE/DROP/ALTER, e os marcadores estáveis (`SMK64`, `SMK64ALM`, `fornecedor = true`, `exige_inspecao = false`, `INTERNO`) |

O fluxo exercitado é o mesmo do uso real: criação em rascunho via form
(`CriarPedidoCompra`), aprovação com congelamento de preços e Idempotency-Key
(`AprovarPedidoCompra`), recebimento cego com lote e destino
(`ConfirmarRecebimento`). IDs de fornecedor/produto/local/item são lidos do
próprio HTML das telas (os `<select>` e optgroups que o usuário vê) — nada de
chamar o serviço por fora da UI.

## Provas (execução verde, repetida 2× para provar reexecução)

```
SEED_IDEMPOTENTE fornecedor=1 produto=1 local=1        (seed aplicado 2x seguidas)
LOGIN_OK
CRIAR_OK rascunho=True
APROVAR_OK pedido=d672ede0-d44c-4dbc-9214-b10125e92c9c
RECEBER_OK documento=NF-SMOKE-R64-261019105525 recebidoTotal=True
DB situacao=RECEBIDO lotes=1 movimentosEntrada(total/cx/liberadoTodas)=4.0000/2/true titulosPagarFornecedor=2
FALHAS=0
R64_E2E_OK
```

Ciclo completo 2× (a 1ª execução válida correu antes do ajuste final do regex de
formatação do total numérico — ver histórico da rodada). No banco, após os 2
ciclos: pedido final `RECEBIDO`, lote `LOTE-R64-SMOKE` único (upsert do recebimento
evita duplicar lote entre execuções), movimentos de entrada integralmente
`LIBERADO` (produto sem inspeção, como o seed define) e uma obrigação em Contas
a Pagar por recebimento — efeito colateral financeiro esperado e documentado.

Honestidade de estado: cada execução deixa linhas identificáveis e acréscimo
(`NF-SMOKE-R64-*`, pedidos SMOKE `RECEBIDO`); uma re-execução abortada no meio
pode deixar 1 rascunho SMOKE pendente — inofensivo e reconhecível na lista
filtrada por fornecedor. O banco alvo é `plantaopro_test` (padrão dos gates); o
seed também foi aplicado em `plantaopro` (dev) com o mesmo resultado 1/1/1.

## Gates (todos verdes)

| Gate | Resultado |
|---|---|
| Build Release `--no-incremental` | exit 0 (log `%TEMP%\opencode\r20b1_build.log`) |
| Gate de casa `r20b1_gate.ps1` | `Now=160 Base=160 DIFF=NENHUMA erros=0` |
| Suíte integral (apps parados) | **1648/1648** limpa, primeira execução |
| Smoke canônico R52 (apps acesos) | FALHAS=0 (`R52_E2E_OK`, media 116 ms) |
| Smoke de escrita R64 × 2 ciclos | FALHAS=0 nos dois (`R64_E2E_OK`) |
| Docscan arquivos mudados | bad=0 |

## Notas de aprendizado (acumuladas)

- PS 5.1: atribuição multi-linha de string com continuação precisa de crachá
  (`` ` ``) no fim da linha anterior — sem ele a 2ª linha vira expressão nova e
  `'&previsao='` cai num cast para Int32. Padrão adotado no smoke: linhas
  completas + `+=`.
- xUnit desta base: `Assert.Equal(3, n, "msg")` não existe para numéricos e
  `Assert.Contains(sub, str, "msg")` resolve para o overload de enumeráveis —
  usar `Assert.True(x == 3, "msg")` / `Assert.False(low.Contains(...), "msg")`.
- Caminhos em `.ps1` ancorados no `$MyInvocation.MyCommand.Path`: contar as
  subidas — a pasta de evidência fica **3 níveis** abaixo da raiz do repo
  (`docs/evidencias/<rodada>/`), ou seja, 4 `Split-Path -Parent`.
