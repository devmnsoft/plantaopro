# R8-B28 — Command Center GLOBAL: EMITE + EXIBE definições de KPI da plataforma (s27 L205)

**Data:** 2026-10-14 · **Rodada:** 8 · **Bloco:** B28 · **Item:** §27 linha 205 (residual — fechamento)

## 1. Classificação
- **Residual §27-L205 (fechamento do conjunto de dashboards de agregação dedicada):** o **branch GLOBAL** (governança/plataforma) do Command Center — `ManagerCommandCenterService.GetGlobalAsync` — exibe métricas de plataforma (tenants ativos, clientes ativos, módulos contratados) e NÃO emitia definições. A B23 cobriu apenas o branch TENANT (`GetAsync`, 8 indicadores operacionais); seu próprio teste registrava: "o branch GLOBAL segue fora desta fatia: exige emitir as próprias definições em `GetGlobalAsync`."
- Esta fatia converte o branch global para **emissão na API** (fiel ao SQL real — contagens globais, sem tenant/período) + **exibição na Web** (no branch `isGlobal`). Fechar este item completa a uniformidade dos painéis de agregação dedicada (B20–B27).
- Sem regra inventada: cada definição descreve exatamente a contagem global que o SQL aplica.

## 2. Indicadores cobertos (3 cards de plataforma)
| Codigo | Label | base (SQL real de GetGlobalAsync) |
|---|---|---|
| ActiveTenants | Tenants Ativos | count(*) de tenants com status = 'ATIVO' (escopo global) |
| ActiveClients | Clientes Ativos | count(*) de clientes com reg_status = 'A' e status = 'ATIVO' (escopo global) |
| ContractedModules | Módulos Contratados | count(*) de tenant_modulos com status = 'ATIVO' (escopo global) |

## 3. Arquivos
| Arquivo | mudança |
|---|---|
| `backend/PlantaoPro.Api/ManagerCommandCenterService.cs` | **produção:** `GetGlobalAsync` agora anexa `with { Definicoes = DefinicoesPlataforma(), TimeCriterion = TempoCriticoPlataforma }` ao resumo; novos membros `DefinicoesPlataforma()` (3 definições fiéis ao SQL global) + const `TempoCriticoPlataforma`. Distintos do `Definicoes()` operacional da B23. |
| `backend/PlantaoPro.Web/Views/CommandCenter/Index.cshtml` | **exibição:** branch `isGlobal` ganha `<details>` "Como cada indicador da plataforma é calculado" (TimeCriterion + Label/Definicao + estado vazio), espelhando o bloco operacional da B23. |
| `backend/PlantaoPro.Tests/R8B28CommandCenterGlobalDefinicoesContratoTests.cs` | **novo:** 3 fatos source-read anti-drift (API emite definições de plataforma fiéis ao SQL · VM carrega · view renderiza no branch global). |

**Camadas/mecanismo:** o branch global devolve o MESMO `ManagerCommandCenterDto.summary` (um `CommandCenterSummary` que já carrega `Definicoes`/`TimeCriterion` init-only desde a B23). Como só UM branch roda por requisição, o único campo `ManagerSummary.Definicoes` serve os dois modos: operacional (8) no tenant, plataforma (3) no global. Sem novo tipo, consulta ou regra.

## 4. Evidência executada
- **Build** (sln `PlantaoPro.sln`, `-m:1`) EXIT 0 → assemblies recompiladas com a nova emissão do branch global.
- **Testes ~R8B28** isolados (fast-feedback): **3/3** aprovados.
- **Full suite v2** (gate phaseA + full×3 `--no-build`): gate phaseA EXIT 0 + full×3 EXIT 0, **Total de testes 1394** em cada pass (**1391** baseline + **3** novos), zero falhas (RAM livre ~0,60 GB nesta execução; nenhum flake transitório).

## 5. Status
- **IMPLEMENTADO + TESTADO** (suíte verde). Não HOMOLOGADO/LIBERADO (o portão §7 vivo segue externo).
- Sem mudança nos valores exibidos: apenas o branch global passa a emitir/exibir suas PRÓPRIAS definições (fiéis ao SQL de contagem global), separadas das operacionais do branch tenant. Nenhum assembly legado e nenhuma regra nova.
