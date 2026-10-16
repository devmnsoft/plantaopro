# Rodada 10 — r10a0: Auditoria inicial / plano da sprint (dívidas herdadas da R8 — P3)

- **Data:** 2026-10-16 · **Base:** `6b26ec8` (ponta exata do push da rodada 9; `origin/main` == HEAD) · Ambiente local/homologação · Web `http://localhost:52976`, API `http://localhost:51976` (servidores parados; portas livres), banco principal `plantaopro` + teste `plantaopro_test` (PG18).
- **Restrição:** commits locais por slice (`main`); **push/merge/publish pendentes de autorização ao final da rodada** (padrão das rodadas anteriores; o push da R9 foi autorizado pontualmente).
- **Escopo** (herdado de `r9-relatorio-final-consolidado.md` §6 item P3): dívidas herdadas R8 — **B16/B17/B18 (residuais)**, **drill-down §27-L208** e **writer ausente de `adm360_ocorrencias`**. Fora do escopo desta rodada: **P5** (`pg_dump` 18 quebrado neste host — tooling de snapshot).

## 0. Baseline executada (gate de partida)

- **Build sln completa (Release):** EXIT 0, 35,6 s, 80 avisos pré-existentes, 0 erros.
- **Full suite (Release, `--no-build`):** TEST_EXIT 0 — **`Total de testes: 1398`** (base R9 = 1397 + 1 fact r9m-P4). Log: `C:\Users\NCELL-DEV-020\AppData\Local\Temp\opencode\r10_baseline_suite.log` (shell background `sh_144bba665001CZXDoTrWiDch5E`).
- Contrato i18n Adm360 vigente: **605 chaves ×4 idiomas** (`R7DF6Admin360I18nContratoTests.RecursosContemAs605ChavesAdm360EmTodosOsQuatroIdiomas`).

## 1. Matriz de dívidas (estado COM evidência)

### D1 — Writer da `adm360_ocorrencias` (spec **A12**) — **funcionalidade principal da sprint**

| | |
|---|---|
| **Definição** | A12 "Não conformidades/ocorrências" (`docs/administrativo360/PlantaoPro_Administrativo360_MVP_Requisitos_Prompts.md` L112): campos origem, lote/kit, local, tipo, quantidade, gravidade, evidência, ação, responsável e prazo. Ciclo **Aberta → em tratamento → resolvida → encerrada**. "Encerrar exige destino dos itens e justificativa. **Registro de ocorrência não apaga estoque**." |
| **Gap (evidência)** | Varredura do repo: `adm360_ocorrencias` aparece SOMENTE em (a) DDL `database/migrations/2026_09_v2191_administrativo360_suprimentos.sql` L18 (+ espelhos `scrpt_completo.sql`/`instalar_no_banco_atual.sql`), (b) seeds demo `database/seeds/development/141_…_suprimentos_demo.sql` L106 e `pgadmin/administrativo360_demo_completo.sql`, e (c) cleanup de testes `backend/PlantaoPro.Tests/Infrastructure/Adm360TenantCleanup.cs` L44. **Zero referência em código de produção.** O Web `Administrativo360Controller.ComprasRecebimentos.Ocorrencias()` (GET) sintetiza linhas **virtuais** a partir de `api/administrativo360/qualidade/pendentes` (tipo fixo `QUARENTENA_RECEBIMENTO`, situação sempre `ABERTA`, prazo hoje+2) — nada persiste. `BLOCO2_EXECUCAO.md` §Parcial item 2: "Ocorrências possuem persistência e massa demonstrativa, porém os comandos completos de tratamento/liberação/encerramento não entraram neste incremento." R8-B4: "`adm360_ocorrencias` não tem writer ativo em produção (só DDL)". |
| **Plano (slice `r10b1`)** | 1. `IOcorrenciasRepository` (Application/Administrativo360/Contracts.cs) + `OcorrenciasRepository : Adm360Repository` (Infrastructure) — listagem com filtro por situação, criação idempotente, transições de estado com **concursividade otimista por `versao`** (409 em conflito). Máquina: `ABERTA → EM_TRATAMENTO → RESOLVIDA → ENCERRADA` (estrita, avança 1 passo; encerrar exige `destino` + `justificativa_encerramento`). 2. `Ocorrencias360Controller` `[Route("api/administrativo360/ocorrencias")]` em `Adm360SuprimentosControllers.cs` com política **`Adm360.Qualidade`** (mesma segregação de `ADM360:LIBERAR_QUALIDADE` — estoquista puro não trata ocorrências; registro na Program.cs junto aos demais repositórios Adm360). 3. Sem movimento de estoque em nenhuma transição (invariante A12 "não apaga estoque" — testado). 4. Web: `Ocorrencias()` passa a ler a tabela real; formulário de abertura (produto/lote/local/tipo/descrição/quantidade/prazo/responsável) + botões de comando por situação (Tratar/Resolver/Encerrar); mantém link para fila de Inspeções. 5. i18n: todos os literais novos em `Adm360.Ocorrencias.*` ×4 resx; contrato 605 → 605+N + fact de completude sem literais (receita r9m-P4). 6. Testes: nova classe estilo `Administrativo360EstoqueEOrcamentosTests` (repositório direto c/ connection string) — ciclo feliz, transição inválida, encerrar sem destino/justificativa rejeitado, idempotência do abrir (sem duplicar), saldo de estoque invariante, isolamento por tenant (a tabela já está no `Adm360TenantCleanup`). |
| **Aceite** | Suíte full verde com os fatos novos; contrato i18n atualizado; ciclo A12 navegável na webview do tenant SV; zero regra inventada além da linha A12 (bloqueio imediato de saldo interno/externo fica registrado como P1 do A12, fora desta fatia — coluna `tipo`/`situacao` já suportam sem DDL). |

### D2 — Residual **B17** (Meu Dia: consolidação de facade + caveat UTC)

| | |
|---|---|
| **Definição** | R8-B17 validou: `MeuDiaService`/`MeuDiaRepository` são **facade fina sobre o único serviço canônico** `IProductivityActionService`; "Consolidação (apagar) é possível e de baixo risco, mas **adiada pelo rito**". Caveat registrado e invariado desde então: janela "hoje" usa `UtcNow`/`now()` no fuso do servidor. Contrato `R8B17MeuDiaFacadeContratoTests` 3/3. |
| **Plano (slice `r10b2`)** | Executar a consolidação adiada: apontar `MeuDiaController` diretamente para `IProductivityActionService` (rotas/respostas invariantes), remover `MeuDiaService`/`MeuDiaRepository`, e **reescrever os 3 fatos do contrato** para afirmar a nova topologia (controller → canônico único; rotas preservadas). Caveat UTC segue como caveat honesto documentado (sem mudança de comportamento nesta rodada). |
| **Aceite** | Suíte full verde; topologia única provada por contrato; respostas das rotas de Meu Dia byte-equivalentes nos testes existentes. |

### D3 — Drill-down **§27-L208** (cards Bi/Premium sem link "abrir registros")

| | |
|---|---|
| **Definição** | Matriz R8 (`r8a0…matriz-estado.md` L208, §27 "Indicadores com definição e rastreabilidade"): "Abrir registros que compõem o total — Operacional ('Ver registros'/'Abrir origem'), CommandCenter ('Ver detalhe'), MeuDia (ação primária); **cards Bi/Premium sem link** \| parcial \| estender padrão". R8-B19 registrou como pendente "depende da definição das métricas … sem definição = inventar alvo". **Fato novo pós-R8:** as definições EMITE+EXIBE já foram fechadas em BI (B20), Dashboard (B21), Meu Dia (B22), CommandCenter tenant (B23) e global (B28) e Fluxo de Caixa Admin360 (B27) — o pré-requisito se firmou parcialmente, abrindo caminho para alvos honestos. |
| **Plano (slice `r10b3`)** | Recon das views Bi/Premium + serviços (`BiService`/`DashboardPremiumService`); estender o padrão existente de "Ver registros"/"Ver detalhe" **somente** para KPIs cujo conjunto de registros possui rota de listagem já existente e filtrável pela dimensão do KPI (período/unidade/especialidade conforme o filtro que a página já aceita); KPI sem rota-alvo existente permanece sem link, com contrato que força decisão consciente caso alguém adicione link sem alvo. Sem rota inventada. |
| **Aceite** | Links presentes onde houver alvo real (testes de contrato + navegação); zero KPI com link para rota inexistente; suíte full verde. |

### D4 — Residual **B16** (especialidade clínica text × id)

| | |
|---|---|
| **Definição** | R8-B16 validou canonicidade: clínica opera por **texto livre** `agendamentos.especialidade`; ids clínicos dormem (forward-schema); id→`especialidades` é canônico só em plantões/médicos. Contrato anti-conflusão 3/3. Residual: "normalização opcional texto→id = **decisão de modelo de dados** fora do rito" + coluna órfã `consultas.especialidade_id` (DDL-only). |
| **Plano (slice `r10b4`, com D5)** | **Decisão formal registrada**: texto livre permanece representação operacional da clínica até decisão explícita de normalização (que exigiria backfill nome→id + picker de UI + backfill de `agendamentos.especialidade_id` — escopo próprio de rodada futura); o contrato `R8B16…` segue como guarda (qualquer mudança falha o contrato e força decisão consciente). Coluna órfã mantida (forward-schema). |
| **Aceite** | Decisão versionada em doc + painel; contrato inalterado e verde. |

### D5 — Residual **B18** (cards KPI manuais × parciais do design system)

| | |
|---|---|
| **Definição** | R8-B18 validou: `_KpiCard` (×~7 views) e `_CoverageCard` são componentes estabelecidos; os 4 locais apontados (Bi/Operações hidratados por JS; MeuDia `day-kpis`; CommandCenter `cc-kpis`) são **renderizações distintas**, redundância conceitual e não cópia — unificar = **refator de design fora do rito**. Contrato anti-drift 3/3. |
| **Plano (slice `r10b4`, com D4)** | **Decisão formal registrada**: unificação de tiles KPI fica registrada como refator de design candidato a rodada futura (requer definição de alvo visual + paridade de comportamento JS); contrato `R8B18…` segue como guarda. |
| **Aceite** | Decisão versionada em doc + painel; contrato inalterado e verde. |

## 2. Fila ordenada da sprint

Ordem por valor > risco (contratos/validação antes de mudança de topologia):

1. **`r10b1` — D1 writer A12** (nova funcionalidade, escopo delimitado pela linha A12).
2. **`r10b2` — D2 consolidação B17** (baixo risco, topologia única).
3. **`r10b3` — D3 drill-down §27-L208** (recon dentro do slice; alvo honesto ou registro).
4. **`r10b4` — D4+D5 decisões B16/B18** (documento de decisão + painel).
5. **Relatório final consolidado** da rodada + painel atualizado.

Cada slice termina com: evidência executada (testes isolados + suíte full), doc `r10bx-*.md` nesta pasta e linha no §3. Push somente quando autorizado.

## 3. Registro de fechamentos da fila (atualizado por slice)

| Slice | Escopo | Resultado | Evidência |
|---|---|---|---|
| `r10b1` | D1 writer A12 `adm360_ocorrencias` | FECHADO — máquina estrita ABERTA→EM_TRATAMENTO→RESOLVIDA→ENCERRADA, encerrar exige destino+justificativa, zero toque em estoque; API `Adm360.Qualidade`; Web lê tabela real + 4 comandos; i18n 605→**650**; **24/24** da fatia; full 1416 com 1 flake catálogo ① (`ProvisionamentoB5Tests.P3`) isolado 5× 11/11 | `r10b1-writer-adm360-ocorrencias-a12.md` |
| `r10b2` | D2 consolidação B17 facade Meu Dia | FECHADO — `MeuDiaServices.cs` deletado, `MeuDiaController` direto sobre `IProductivityActionService`, janela UTC +1d/PageSize 25/CLINICO relocada p/ helpers privados com caveat documentado; contrato R8B17 reescrito (4 fatos); **9/9** da fatia; **full 1417/1417 limpo** | `r10b2-consolidacao-facade-meu-dia-b17.md` |
| `r10b3` | D3 drill-down §27-L208 cards Bi/Premium | FECHADO — recon por fonte canônica (SQL dos endpoints); 5 links BI (Clientes ATIVO / Assinaturas ATIVA / FaturamentoSaas VENCIDA estáticos + Escalas período e Cobertura via JS) e 2 Premium (Plantões/Escalas); sem link onde a rota não é honesta (Valor pago por data_pagamento, Médicos global, Agendamentos por período) com caveats documentados; contrato anti-inventário 4 fatos; **17/17** da fatia; **full 1421/1421 limpo** | `r10b3-drilldown-bi-premium-l208.md` |

## 4. Riscos e gotchas da rodada

- **Flakes de família 40P01/40001 (deadlock sob RAM baixa)** — catálogo ①; receita de mitigação já entregue em r9k-P2 (pré-limpeza natural-key + retry único após 250 ms) disponível para outras classes; assinatura "método diferente a cada run + erro de DB (não Assert)" identifica flake.
- **Build lock:** matar listeners Api/Web antes de build Release (servidores já parados ao iniciar a rodada).
- **Contrato i18n Adm360:** qualquer literal novo em view ⇒ chave em 4 resx + contar chaves no contrato + fact de completude (receita r9m-P4). Proibições de repo: `confirm(`, `alert(`, `href="#"`, `@page` em view compartilhada, segredos em appsettings.
- **`pg_dump` 18 quebrado neste host (P5)** — snapshots de estado via consultas `psql` (`-At`), não dump.
- **Suíte = 1398:** todo número citado nestes docs parte dessa base.
