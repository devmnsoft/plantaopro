# R8-B20 — Definição e rastreabilidade de KPIs: o BI agora EXIBE as definições (§27 linha 205 residual "Web não exibe Definicoes")

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia da **auditoria ALÉM do top-10 §29**: §27 linha 205 *residual* (renderização em tela das definições de KPI). Primeira extensão em tela do §27-205 pós-B9.
- **Data:** 2026-10-13
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica: código de Bi + B9/B11)

R8-B9 estendeu o padrão Operacional às calculadoras: cada KPI **EMITE** `Definicoes[]` (`Codigo`/`Label`/`Definicao`) + `TimeCriterion` na API. A varredura/matriz registraram o **RESIDUAL**: *"Web não exibe `Definicoes`"* (também em `R8B11EspelhoDtosApiWebContratoTests.cs:42`).

No BI especificamente, `/api/bi/resumo-executivo` **JÁ retorna** `data.definicoes` + `data.timeCriterion` (`BiServices.cs:65-66`; DTO `Models.cs:656-658`), mas a view `Bi/Index.cshtml` **nunca os renderizava** — o usuário via os números sem a fórmula/fonte/período/fuso de cada um.

## 2. Decisão e mudança (só apresentação; sem nova consulta/número/regra clínica)

- **`Bi/Index.cshtml`** (única mudança): adicionado um bloco colapsável **"Como cada indicador é calculado"** (`<details>` com card — mesmo padrão de disclosure de `Relatorios/Operacional.cshtml`). O JS, na **mesma `fetch`** que já hidrata os KPIs, popula:
  - `#bi-timecriterion` ← `d.timeCriterion`;
  - `#bi-definicoes` ← uma `<li>` por indicador (`label`:`definicao`), com escape de `&<>` (higiene contra quebra de markup).
- **Estado de indisponibilidade** (mesmo critério "Zero ≠ indisponível" da B19): no `.catch`, as definições viram *"Definições indisponíveis no momento"* e `#bi-timecriterion` fica limpo — falha não é silêncio/vazio.
- **Sucesso sem definições**: se o payload vier 200 sem `definicoes`, exibe *"Sem definições informadas pela fonte para os indicadores."*
- **Escopo:** só o **BI**. Dashboard (`api/dashboard`), Meu Dia, CommandCenter e Admin360 seguem como dívida registrada.

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Web/Views/Bi/Index.cshtml` | **Produção (única):** bloco `<details>` "Como cada indicador é calculado" + JS popula `#bi-timecriterion`/`#bi-definicoes` do payload (escapando) + estado "indisponíveis" no `.catch`. |
| `backend/PlantaoPro.Tests/R8B20BiDefinicoesContratoTests.cs` | **Novo:** 3 fatos — (1) a view renderiza as definições (markup + JS lê `d.definicoes`/`d.timeCriterion`, escapa, lista por indicador, "indisponíveis" no catch); (2) a fonte continua emitindo (`BiServices` popula `row.Definicoes`+`TimeCriterion` com `KpiDefinicao` por KPI) e o DTO carrega os campos; (3) `KpiDefinicao` tem `Codigo`/`Label`/`Definicao`. |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §27 linha 205 → residual **estreitado** (BI em tela = R8-B20); o restante (dashboard/MeuDia/CommandCenter/Admin360) segue pendente. |

## 4. Evidência (executada)

- **Build sln completa:** EXIT 0 (**compilação com êxito** — a view Razor com o bloco novo compila limpo; apenas avisos pré-existentes).
- **Alvo R8B20:** `R8B20BiDefinicoesContratoTests` = **3/3 aprovados** (`--filter FullyQualifiedName~R8B20`).
- **Gate phaseA v2:** **EXIT 0** (RAM livre 1,24 GB ao iniciar).
- **Full suite v2:** **Total de testes: 1373** (baseline 1370 + 3 novos) em TODAS as 3 rodadas. Todas as assertions de view do BI aprovadas (`R8B12`/`R8B18`/`R8B19`/`Saude360R4B9`). A **ÚNICA** falha nas 3 rodadas foi `ProvisionamentoB5Tests.P1_Finalizar_CriaMundoTodo_E_AdminAutentica` — classe de flake **conhecida**, sem ligação com a fatia:
  - **full1** = `Npgsql … 40P01: impasse detectado` (deadlock no cleanup `LimparMundoAsync` sob a carga paralela da suíte completa);
  - **full2/full3** = `CNPJ já cadastrado.` (órfão deixado pelo deadlock do full1 — o CNPJ fixo `99988877000166` não foi removido porque o `finally` do teste não completou; as execuções seguintes falham antes de chegar ao próprio cleanup → ciclo travado);
  - **isolado (estado limpo + sem concorrência) = 5× 5/5** → **transiente**.
  - Limpeza aplicada: removeu-se o mundo órfão do CNPJ espelhando a **ordem de deletes do próprio `LimparMundoAsync`** (bloco atômico; re-probe = 0 linhas).
- **Verificação byte-level:** `git status` = view Bi alterada + teste novo (+ docs selados); nenhum outro arquivo de produção tocado.

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Renderizar `Definicoes` nas demais telas** (§27-205 residual remanescente): dashboard `api/dashboard` (`Data.cs` já emite), Meu Dia, CommandCenter, Admin360 — as mesmas mudanças de apresentação aplicadas ao BI. Fazer junto com a definição/consolidação de métricas (B9/B18). O contrato `R8B20…` cobre o BI; o restante segue pendente.
2. **Drill-down §27-208:** segue proposta/pendente (B19) — agora o BI **exibe** as definições (base para linkar "ver registros" sem inventar alvo).
3. **ProvisionamentoB5 crash-idempotente (R8-B14):** este flake (deadlock → órfão → "CNPJ já cadastrado") é exatamente esse caso — reforça a dívida (limpeza não idempotente + box compartilhado), **não** a fatia.
4. **Meu Dia UTC "today" + consolidação de facade (B17):** invariado.

## 6. Fila seguinte (R8) — auditoria além do top-10

§27-205: **BI em tela entregue (R8-B20)**; as demais telas pendentes. §27-208: pendente (com base nova p/ linkar). Próximos: renderizar definições nas demais telas, ou seguir §27/§28 pela matriz, ou janela viva de homologação / push.

## 7. Gotchas aprendidos (reutilizáveis)

- **Deadlock num cleanup de teste deixa órfão que contamina as rodadas seguintes:** num teste com chave **fixa** (CNPJ) que só limpa no `finally`, um deadlock/erro no cleanup aborta o delete (rollback) → a execução seguinte falha com "já cadastrado"/duplicata **antes** de chegar ao próprio cleanup → ciclo travado. Diagnóstico: sondar a chave no banco e remover o mundo espelhando a ordem de deletes do cleanup, então rerun isolado.
- **"Falhou em todas as passes" ≠ regressão:** pode ser estado acumulado (não o primeiro flake). Checar se a mudança toca aquela área + isolar 5× em estado limpo antes de concluir.
- **"Residual" de superfície = dado já no payload:** a view do BI já recebia `definicoes`/`timeCriterion`; "exibir" foi só apresentação (sem nova query/número/regra). Verificar o que o endpoint **já devolve** antes de achar que falta trabalho no backend.
