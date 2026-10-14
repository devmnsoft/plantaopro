# R8-B27 — Admin360 Fluxo de Caixa: EMITE + EXIBE definições de KPI (s27 L205)

**Data:** 2026-10-14 · **Rodada:** 8 · **Bloco:** B27 · **Item:** §27 linha 205 (residual)

## 1. Classificação
- **Residual §27-L205:** o painel **Fluxo de Caixa** (Admin360) era o único dashboard de agregação dedicada ainda com as definições dos indicadores em um **bloco estático** no `.cshtml`. Esta fatia converte para **emissão na API** (fiel ao SQL real) + **exibição na Web**, completando a uniformidade dos painéis (mesmo padrão da B20–B25).
- Sem regra clínica/comercial inventada: as definições espelham exatamente o que `CaixaRepository.FluxoCaixaAsync` calcula hoje.

## 2. Indicadores cobertos (8 tiles + travamento de estorno)
| Codigo | Label | base (SQL real de CaixaRepository.FluxoCaixaAsync) |
|---|---|---|
| SaldoAtualContas | Saldo Atual das Contas | soma dos saldos atuais derivados por conta (inicial + soma movimentos ENTRADA - SAIDA em `adm360_movimentos_financeiros`); estado atual |
| SaldoAberturaPeriodo | Saldo Abertura Período | soma saldos iniciais (DataSaldoInicial <= início) + soma movimentos com data_movimento < início |
| TotalEntradasRealizadas | Entradas Realizadas | soma valor movimentos tipo 'ENTRADA' em [início, fim] (inclusivo) |
| TotalSaidasRealizadas | Saídas Realizadas | soma valor movimentos tipo 'SAIDA' em [início, fim] (inclusivo) |
| SaldoFechamentoPeriodo | Saldo Fechamento Período | Abertura + Entradas Realizadas - Saídas Realizadas (fórmula no servidor, não gravada) |
| TotalEntradasPrevistas | Entradas Previstas (AR) | soma saldo_aberto de títulos a receber situacao IN ('ABERTO','PARCIAL') venc. [início, fim] |
| TotalSaidasPrevistas | Saídas Previstas (AP) | soma saldo_aberto de títulos a pagar situacao IN ('PENDENTE_APROVACAO','APROVADO','PARCIAL') venc. [início, fim] |
| TotalVencidos | Vencidos em Aberto (AR+AP) | soma saldo_aberto com data_vencimento < hoje (independente do período) |
| Estorno | Tratamento de estorno | regra preservada do bloco original (movimento compensatório SAIDA, título volta a ABERTO/PARCIAL, comissões ESTORNADA; bloqueado em fechamento fechado) |

## 3. Arquivos
| Arquivo | mudança |
|---|---|
| `backend/PlantaoPro.Api/Controllers/Adm360CaixaController.cs` | **produção:** novo record `FluxoCaixaResposta` (mesmos campos de topo de `FluxoCaixaDto` + `IReadOnlyList<KpiDefinicao> Definicoes` + `string TimeCriterion`); ação `Fluxo` agora retorna o envelope com `Definicoes()` fiéis ao SQL + `TempoCritico`. |
| `backend/PlantaoPro.Web/Models/Administrativo360Models.cs` | **espelho:** `FluxoCaixaViewModel` ganha corpo init-only `{ IReadOnlyList<KpiDefinicaoDto>? Definicoes; string TimeCriterion }` (a Web não referencia o projeto Api; STJ mapeia por nome). |
| `backend/PlantaoPro.Web/Views/Administrativo360/FluxoCaixa.cshtml` | **exibição:** bloco estático substituído por `<details>` "Como cada indicador é calculado" (TimeCriterion + Label/Definicao/Codigo + estado vazio). |
| `backend/PlantaoPro.Tests/R8B27FluxoCaixaDefinicoesContratoTests.cs` | **novo:** 3 fatos source-read anti-drift (API emite fiel ao SQL · VM carrega Definicoes+TimeCriterion · view renderiza). |

**Camadas:** o DTO vive em Application (que só referencia Domain); a emissão usa um record da **CAMADA API** (`FluxoCaixaResposta`) porque `KpiDefinicao` está no projeto Api — mesmo padrão da B25 (`DashboardAdm360Resposta`). A resposta mantém os campos de topo de `FluxoCaixaDto` para a BFF, que já lia `/caixa/fluxo` diretamente em `FluxoCaixaViewModel`, mapear 1:1 por nome (incluindo os 2 novos).

## 4. Evidência executada
- **Build** (sln `PlantaoPro.sln`, `-m:1`) EXIT 0 → assemblies recompiladas com a nova emissão.
- **Testes ~R8B27** isolados (fast-feedback): **3/3** aprovados.
- **Full suite v2** (gate phaseA + full×3 `--no-build`): gate phaseA EXIT 0 + full×3 EXIT 0, **Total de testes 1391** em cada pass (**1388** baseline + **3** novos), zero falhas (RAM livre ~0,70 GB nesta execução; nenhum flake transitório).

## 5. Status
- **IMPLEMENTADO + TESTADO** (suíte verde). Não HOMOLOGADO/LIBERADO (o portão §7 vivo segue externo).
- Sem mudança nos VALORES exibidos: apenas a origem das definições passa a ser a API (fiel ao SQL) em vez do texto estático; nenhum assembly legado e nenhuma regra de negócio alterada (o bloco de estorno original foi preservado como item emitido).
