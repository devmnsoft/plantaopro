# R65 — Consolidação de flakes (fila r53b1 §7 item 10) · fechamento da fila

Data: 2026-10-19 · Base: `08520f2` (R64 fechada) · Suíte: **1648 → 1650** (+2 fatos)

Esta rodada fecha a fila r53b1 §7 (itens 1–10 concluídos: R52…R58, R59, R60, R61,
R62, R63, R64 e esta R65).

## Catálogo consolidado de flakes

Todas as observações documentadas desde a R8 foram reunidas aqui com assinatura,
histórico e dispisição. Critério de casa mantido: flake ≠ regressão — falha
isolada 5× verde antes de classificar.

### Família ① — impasse 40P01/40001 em purge concorrente (teardown/Dispose)

Assinatura: `Npgsql.PostgresException 40P01: impasse detectado` (ou 40001) em
`PurgarAsync()` chamado de `Dispose()`, com coleções de teste correndo em paralelo
sobre `plantaopro_test` sob carga. O corpo do teste aprova; o erro é de banco no
teardown. Método surfante diferente a cada run.

Observações históricas: r8b10 (`R7OB…O5`), r8b22 (`O2`), r8b23 (`O1`), r8b25
(`O2`), r8b13/r8b20/r9-dre/r9-financeiro (`ProvisionamentoB5Tests.P2/P3`),
r9k-b14 (endureceu idempotência pós-crash), r10b1 (`B5 P3`), r11 (`R7OB O3`),
r12 (`B5 P1`), r14 (`SaasGovernancaB6 G3/G4b`), r15 (`G5`), r61 (`R7O O3`,
isolate 5/5), reincidente sob carga nas suítes das R63/R64 desta sessão.

**Endurecido nesta rodada:** toda purge do projeto agora roda pelo retry único
compartilhado `TestInfraRetry.ExecutarComRetryImpasseAsync` (novos em
`backend/PlantaoPro.Tests/TestInfraRetryImpasse.cs`) — promoção do helper privado
que já existia na SaasComercialB4MatrizTests desde a r9m-P2, que era a única das
8 classes purgadoras protegida. Migradas: CobrancaB6, OnboardingC7,
ProvisionamentoB5, R23CaixaJustificativaEItemPedido, R7OB, R8B4, SaasGovernancaB6,
e SaasComercialB4 (agora usa o compartilhado, privado removido). Semântica: só
`40P01`/`40001`, um único retry após 250 ms, e propaga na segunda falha — purges
são idempotentes por construção (IDs fixos), então o retry nunca mascara assert.
Travado por contrato (F1: exatamente UMA definição no projeto + as 8 classes
roteiam por ele).

### Família ② — corrida de snapshot em R19 `Db_CentralClientes`

Assinatura: recorrente **somente sob carga** (suítes corridas logo após outras
suítes/apps no mesmo banco); passa sempre isolado. Mecanismo: o teste executa a
SQL real da fonte em duas statements (`QueryMultipleAsync`: itens + totais) sem
snapshot compartilhado — um purge concorrente de outra coleção entre as duas
deixava o somatório menor que a página lida e o assert de integridade caía.

**Endurecido nesta rodada:** as duas statements rodam numa transação
`REPEATABLE READ` explícita (mesmo snapshot) — a SQL da fonte continua intacta,
o que mudou foi só o harness de execução. Travado por contrato (F2).

### Família ③ — flake transiente de suíte anônimo (execuções consecutivas)

Observações: R59 (execução 2, nome não capturado) e R60 (execução 1, nome não
capturado); padrão idêntico à família ① surfando em método não registrado.

**Regra de processo adotada (a partir desta rodada):** toda execução full da
suíte que terminar com falhas isoladas grava o **nome completo do teste falho**
no arquivo de resumo da rodada ANTES de re-executar — os dois históricos acima
ficam registrados como "nome não capturado" e são considerados membros da
família ① até prova em contrário. A partir de agora não há mais resumo sem nome.

### Classe meta — auto-referência dos scanners de repo

Os scanners (`EvolucaoProdutoVendavelContractTests` / `HomologacaoCrudsAcoesJornadasContractTests`)
proíbem literais `alert(` / `confirm(` em QUALQUER `.cs`, inclusive em contratos
que os citam para travá-los; o contrato F1 desta rodada caiu no mesmo espelho ao
contar definições que o próprio arquivo cita. Padrões de tratamento (R63 + R65):
construir o literal por concatenação na fonte ou excluir o próprio arquivo da
varredura contratual com comentário explicando o porquê.

## Mudanças desta rodada

| Arquivo | Mudança |
|---|---|
| `backend/PlantaoPro.Tests/TestInfraRetryImpasse.cs` | novo — infra compartilhada de retry 40P01/40001 (família ①) |
| 8 classes purgadoras | corpos de `PurgarAsync` roteados pelo compartilhado; `SaasComercialB4MatrizTests` teve o helper privado removido |
| `backend/PlantaoPro.Tests/R19DapperBindingGuardTests.cs` | `Db_CentralClientes` em snapshot único REPEATABLE READ (família ②); comentário "NÃO"→"não" (docscan limpo) |
| `backend/PlantaoPro.Tests/R65ConsolidacaoFlakesContratoTests.cs` | novo — 2 fatos travando as duas endurecimentas contra regressão |

Nenhum código de produto foi tocado: as duas mudanças são de infra de teste, e
a SQL guardada pelo R19 continua extraída e executada da fonte, byte a byte.

## Gates (todos verdes)

| Gate | Resultado |
|---|---|
| Build Release `--no-incremental` | exit 0 (log `%TEMP%\opencode\r20b1_build.log`) |
| Gate de casa `r20b1_gate.ps1` | `Now=160 Base=160 DIFF=NENHUMA erros=0` |
| Suíte integral | **1650/1650** limpa (`r65_suite2.log`); a execução anterior falhou apenas no novo contrato F1 por auto-referência (correção registrada acima), re-verificado isolado 2/2 e na full final |
| Escopo tocado (R19 ×10 + B4 ×6 + R7OB ×6 + B5 ×8 + R65 ×2 + scanners) | 42/42 na repassada |
| Docscan arquivos mudados | bad=0 |

## Efeito colateral esperado na observação futura

Com a família ① endurecida na única camada onde ela aparecia (teardown de
purge), a previsão honesta é que execuções full consecutivas passem a zerar
falhas com frequência bem maior; se qualquer 40P01 escapar por caminho fora de
`PurgarAsync` (ex.: seeds inserindo em concorrência), ele aparece SEMPRE com o
nome do teste (regra ③) e entra neste catálogo com o histórico.
