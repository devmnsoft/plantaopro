# R8-B15 — `parceiros` (B2B jsonb) × `adm360_parceiros`: validação de dualidade + contrato anti-conflusão (§29 item 7)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia "`parceiros` (B2B jsonb) × `adm360_parceiros` (master c/ flags) — propósitos distintos, sobreposição semântica" (item 7 da fila §29, ordem quebrado→alto impacto: … → B12 → B13 → B14 → **B15**)
- **Data:** 2026-10-13
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica) — duas tabelas, dois conceitos, sem sobreposição operacional

| Tabela | Origem (DDL canônico) | Modelo | Scoping | Consumo em código |
| --- | --- | --- | --- | --- |
| **`adm360_parceiros`** | `migrations/2026_09_v2191` (+flags em `v2198`) | Parceiro **por papéis operacionais**: flags `fornecedor`, `eh_hospital`, `eh_pagador`, `eh_cliente` | `tenant_id NOT NULL` | **VIVO**: CRUD em `CadastrosRepository` + view `Parceiros.cshtml` + FKs em `adm360_pedidos.fornecedor_id`, orçamentos (`hospital_id`/`responsavel_financeiro_id`), contas a pagar (`fornecedor_id`), cotações (`hospital_id`), vendas (`cliente_id`/`pagador_id`)… (~12 repositórios o JOINem/leem) |
| **`parceiros`** / **`parceiro_tenants`** | `migrations/2026_plantao_pro_white_label_b2b_launch.sql:1348+` | Parceiro **SaaS/assinatura**: `plano_id`, `parceiro_id`, `cliente_id`, `metadata jsonb` livre, `status` | `tenant_id NULL` | **DORMANT (forward-scaffolded)**: ZERO `FROM/INTO/JOIN/UPDATE … parceiros` em qualquer `.cs` de produção |

**O único "Portal do parceiro" em código é DEMO:** `ParceiroPortalApiController` delega a `CommercialDemoService.ParceiroPortalResumoAsync()`, que retorna dados **em memória** (`Task.FromResult(new PortalResumoDto{ Titulo="Portal do parceiro/revendedor", Kpis=… })`); o `ParceiroPortalController` (Web) renderiza via `IFase2OperationalFlowService.Build("PARCEIRO", …)`. Nenhum dos dois toca a tabela B2B.

**Conclusão da validação:** a "sobreposição" apontada pelo varredura (C1.2) é **apenas semântica** (o nome genérico "parceiro"). Não há duplo-mestre, duplo-write nem leitura conflitante: o domínio administrativo-360 usa **exclusivamente** `adm360_parceiros`; a schema B2B de lançamento é DDL forward-deployed ainda **sem implementação** (feature B2B/white-label por vir). Os dois modelam papéis estruturalmente distintos (papéis operacionais vs. parceiro-de-assinatura c/ plano + metadados).

## 2. Decisão e mudança (sem regra inventada, sem deleção antecipada)

Rito §29 (comparar consumidores → canônico → migrar → regressões):
- **Comparar:** feito (tabela acima). **Canônico por domínio:** `adm360_parceiros` para o operacional; `parceiros`(B2B) reservado ao futuro domínio SaaS.
- **Migrar:** inexistente — não há consumo duplicado que consolidar (a B2B nem é lida ainda).
- **Sem deleção antecipada:** a schema B2B (`parceiros`/`parceiro_tenants`) **não foi removida** (forward-deployed; pode ser implementada depois — remover agora seria deleção antecipada, vedada pelo rito desta rodada).
- **Regressões/contrato:** entregue um **CONTRATO anti-conflusão** que fixa os invariantes: (1) `adm360_parceiros` é o único mestre operacional amplamente usado; (2) a tabela B2B não é consumida por código de produção; (3) as duas modelam papéis distintos nos DDL canônicos. Se no futuro alguém apontar código operacional para a tabela B2B errada (ou tratar as duas como intercambiáveis), o contrato falha e força uma decisão consciente.
- **Zero mudança em produção.**

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Tests/R8B15ParceiroDualidadeContratoTests.cs` | **Novo**: 3 fatos — (1) mestre operacional `adm360_parceiros` (CRUD em `CadastrosRepository` + ≥8 repositórios de produção o usam); (2) tabelas B2B `parceiros`/`parceiro_tenants` **não consumidas** por `.cs` de produção (regex de contexto SQL que não casa com `adm360_parceiros` nem com nomes de índice); (3) as duas modelam papéis distintos nos DDL canônicos (`v2191`/`v2198` flags operacionais × `b2b_launch` `plano_id`/`parceiro_id`/`metadata jsonb`). |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §29 item 7 (linha 233) → **R8-B15**; resumo "Estado" (§29) refletindo itens 1+2+3+4+5+6+7. |

## 4. Evidência (executada)

- **Build sln completa:** EXIT 0 (0 erros; apenas avisos pré-existentes).
- **Alvo:** `R8B15ParceiroDualidadeContratoTests` = **3/3 aprovados** (`--filter FullyQualifiedName~R8B15`).
- **Gate phaseA v2:** **52/52, EXIT 0**. (Na suíte completa a faseA teve `EXIT=-1` = crash por **OOM no startup do host Web** sob RAM livre 0,87 GB, após já ter aprovado os contratos até o teste de login — não era falha de asserção; a re-run isolada confirmou 52/52.)
- **Full suite v2 (RAM 0,87 GB):** **full1 e full2 = EXIT 0, `Total de testes: 1358`** (1355 baseline B14 + 3 novos) — runs limpas; **full3 = EXIT 1 com 1 falha** = `SaasGovernancaB6Rodada4Tests.G6_DowngradeImediato_DesativaContrato_E_RevogaSessaoAberta` (classe de flake do **catálogo ①**), **isolada e aprovada 5/5** (flaky de banco sob pressão de memória, não regressão).
- **Verificação byte-level:** `git status` = matriz + teste novo (+ doc selado); **nenhum arquivo de produção alterado**.

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Feature B2B/white-label de parceiros ainda não implementada:** as tabelas `parceiros`/`parceiro_tenants` são schema forward-scaffolded sem consumidores; o "Portal do parceiro" atual é demo em memória. Quando a feature for construída, o contrato `R8B15ParceiroDualidadeContratoTests` (fato 2) vai exigir decisão consciente (consumir a B2B vs. manter apenas o operacional) — exatamente o guard desejado.
2. **§29 item 8 (próximo na fila):** `consultas.especialidade` text + FK `especialidade_id` coexistindo.
3. **Main DB v2339 / homologação viva:** invariado — janela segue em `r6a1b` §7 (+ lote v2340–v2351 + IIS).

## 6. Fila seguinte (R8)

Com §29 item 7 entregue (validação de dualidade + contrato anti-conflusão), o próximo item da fila §29 = **item 8: `consultas.especialidade` text + FK `especialidade_id` coexistindo.** Alternativas: janela viva de homologação (`r6a1b` §7), ou o push da rodada (pendente).

## 7. Gotchas aprendidos (reutilizáveis)

- **Detetar "tabela X vs prefixo_X":** um `\bX\b` sozinho casa também em C# identifiers/strings; para provar que uma **tabela** é consumida em SQL, exija contexto: `(FROM|INTO|JOIN|UPDATE)\s+(?:schema\.)?X\b`. O `\b` antes de `parceiros` em `adm360_parceiros` NÃO casa (o `_` anterior é caractere de palavra) — então o padrão de contexto SQL já isola a tabela B2B do mestre operacional, e os nomes de índice (`ix_…_parceiros_…`) também não casam.
- **Gate que OOM-crasha na suíte não é falha de teste:** `EXIT=-1` + DUR muito baixo no início = host matado por memória; como a suíte *full* inclui os mesmos contratos, dois fulls 100% verdes já provam o gate — a re-run isolada do filtro do gate só formaliza o "52/52".
- **Forward-DLL é comum neste repositório:** DDL de módulo futuro pode existir (`create table if not exists`) sem qualquer código de produção — tratar "existe no DDL" ≠ "está em uso"; sempre varrer consumidores em `.cs` antes de concluir redundância.
