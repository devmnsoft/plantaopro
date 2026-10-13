# R8-B16 — `consultas.especialidade` text × FK `especialidade_id`: validação de canonicidade + contrato anti-conflusão (§29 item 8)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia "`consultas.especialidade` text + FK `especialidade_id` coexistindo" (item 8 da fila §29, ordem quebrado→alto impacto: … → B13 → B14 → B15 → **B16**)
- **Data:** 2026-10-13
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica: DDL em banco + todos os consumidores em código)

A varredura C1.3 apontou "consultas: especialidade text + especialidade_id coexistindo". A validação por fonte canônica (`information_schema.columns` em `plantaopro_test` + varredura de **todos os 89 usos** de `especialidade_id` no backend) mostra que a "coexistência" está de fato distribuída entre **duas tabelas** — e que o par live (text + id) vive em `agendamentos`, não em `consultas`:

| Tabela | Colunas `especialidade*` (schema live) | Consumo em código |
| --- | --- | --- |
| **`agendamentos`** | **AS DUAS**: `especialidade` TEXT + `especialidade_id` uuid | **VIVO por TEXTO**: escrita da clínica popula `especialidade` (text); exibição lê `a.especialidade` (primário) e resolve `a.especialidade_id` só como fallback |
| **`consultas`** | SÓ `especialidade_id` uuid (NÃO tem coluna text) | **DORMANT**: nenhum INSERT/UPDATE/SELECT da clínica referencia nem o texto nem o id; tabela vazia no DB de teste |
| **`especialidades`** (mestre) | `id`, `nome`, … | mestre normalizado — **referenciado por id no domínio plantões/médicos**, não na clínica |

**Consumidores (fontes em código):**
- **Escrita** — `Saude360ClinicalService.cs`: o `INSERT` do agendamento grava `…data_inicio,data_fim,tipo,**especialidade**,status,…` (TEXTO); o `UPDATE` grava `especialidade=coalesce(nullif(@Especialidade,''),especialidade)` (TEXTO). O `INSERT`/`UPDATE` da **consulta** não materializa especialidade (nem texto nem id). O serviço **inteiro** NÃO contém uma única referência a `especialidade_id`.
- **Exibição** — `CentralAtendimentoService.cs`: mostra `coalesce(a.especialidade, e.nome, 'Especialidade não definida')` → **texto primário**; o nome-por-id (`e.nome`) vem de `left join … on e.id=a.especialidade_id` (fallback defensivo). Como o id nunca é populado na clínica, o fallback praticamente nunca acerta — o texto é o que efetivamente dirige exibição e filtro.

**Canonicidade por subdomínio:**
- **CLÍNICA (agendamentos/consultas):** a representação operacional/canônica é o **texto livre** `agendamentos.especialidade`. As colunas `especialidade_id` de ambas são **DORMANTES** (forward-schema): nem populadas nem lidas como fonte primária.
- **PLANTÕES/MÉDICOS:** a representação **por id** É canônica e operacional (`plantoes.especialidade_id`, `medicos.especialidade_id`, `medico_especialidades`, `medico_disponibilidades`, `solicitacoes_plantao`) — **todos os demais 88 usos** de `especialidade_id` vivem ali, **não na clínica**.

**Conclusão:** não há duplo-mestre nem leitura conflitante dentro da clínica (o id clínico dorme; o texto manda). Os dois subdomínios usam representações diferentes (clínica=texto livre; plantões/médicos=id→mestre) — confundir os dois é exatamente o risco que o contrato guarda.

## 2. Decisão e mudança (sem regra inventada, sem deleção antecipada)

Rito §29 (comparar consumidores → canônico → migrar → regressões):
- **Comparar:** feito (tabela acima). **Canônico por subdomínio:** texto livre para a clínica; id→`especialidades` para plantões/médicos.
- **Migrar:** inexistente nesta rodada — "migrar a clínica para o id" equivaleria a normalizar **texto-livre → mestre** (resolver nome→`especialidades.id`, backfill + picker de UI), uma **decisão de modelo de dados** fora do escopo do rito (e sem regra canônica que a exija). As colunas `especialidade_id` clínicas **não foram removidas** (forward-schema; pode-se adotar o id depois).
- **Regressões/contrato:** entregue um **CONTRATO anti-conflusão/drift** `R8B16EspecialidadeClinicaContratoTests` (3 fatos): (1) a escrita da clínica popula o **texto** e **nunca** o id (o serviço inteiro não referencia `especialidade_id`); (2) a exibição da clínica trata o **texto como primário** (id só via fallback); (3) o **id** de especialidade é canônico no domínio **plantões/médicos** (join `plantoes→especialidades` por id), separando-o do campo textual da clínica. Se alguém apontar a escrita clínica para o id, ou inverter a prioridade de exibição, o contrato falha e força decisão consciente.
- **Zero mudança em produção.**

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Tests/R8B16EspecialidadeClinicaContratoTests.cs` | **Novo**: 3 fatos — (1) escrita da clínica popula texto do agendamento e nunca o id (+ escrita da consulta sem especialidade); (2) exibição da clínica usa texto como primário, id só fallback; (3) id canônico no domínio plantões/médicos (`Data.cs`). |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §29 item 8 (linha 234) → **R8-B16**; resumo "Estado" (§29) refletindo itens 1–8. |

## 4. Evidência (executada)

- **Build sln completa:** EXIT 0 (`BUILD_R8B16_EXIT:0`; apenas avisos pré-existentes).
- **Alvo:** `R8B16EspecialidadeClinicaContratoTests` = **3/3 aprovados** (`--filter FullyQualifiedName~R8B16`).
- **Gate phaseA v2:** **52/52, EXIT 0** (RAM livre 1,20 GB ao iniciar — sem OOM nesta rodada).
- **Full suite v2:** **`Total de testes: 1361`** (1358 baseline B15 + 3 novos) em **todas** as 3 rodadas. **full1** = 1 falha `SaasGovernancaB6Rodada4Tests.G1b_PrefisGlobais_SomenteFonteUnicaCarregaLiteralEmCodigo`; **full2** = 1 falha `SaasComercialB4MatrizTests.B3_DecidirDowngrade_EmSolicitacaoDeCancelamento_409`; **full3** = limpo 1361/1361. As duas falhas são **`Npgsql 40P01: impasse detectado`** (deadlock) no **`PurgarAsync()`** (cleanup em `Dispose`) de cada classe de flake do **catálogo ①** sob pressão de memória/concorrência da suíte — **isoladas e aprovadas 5/5 cada** sob memória normal (não regressão).
- **Verificação byte-level:** `git status` = matriz + teste novo (+ doc selado); **nenhum arquivo de produção alterado**.

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Normalização opcional da especialidade clínica:** se o produto quiser unificar em `especialidades.id` (como os plantões), a clínica precisaria resolver nome-texto→id (backfill + picker de UI) e começar a gravar `agendamentos.especialidade_id`; a exibição já tolera os dois (coalesce texto→nome-id). Até lá, texto é o campo operacional — o contrato `R8B16…` (fatos 1/2) exigirá decisão consciente se isso mudar.
2. **Coluna órfã `consultas.especialidade_id`:** DDL-only, sem coluna text correspondente e sem consumo; mantém-se (forward-schema) até haver decisão de modelo da entidade `consultas`.
3. **§29 item 9 (próximo na fila):** `MeuDiaService` facade sobre `ProductivityActionService` (compatibilidade intencional).
4. **Main DB v2339 / homologação viva:** invariado — janela segue em `r6a1b` §7 (+ lote v2340–v2351 + IIS).

## 6. Fila seguinte (R8)

Com §29 item 8 entregue (validação de canonicidade + contrato anti-conflusão/drift), o próximo item da fila §29 = **item 9: `MeuDiaService` facade sobre `ProductivityActionService` (compatibilidade intencional).** Alternativas: janela viva de homologação (`r6a1b` §7), ou o push da rodada (pendente).

## 7. Gotchas aprendidos (reutilizáveis)

- **"Coexistência text+id" pode estar em tabela diferente da apontada:** a varredura citou `consultas`, mas o par live (text + id) vive em `agendamentos`; `consultas` só tem o id (orfanado). Sempre confirmar em `information_schema.columns` qual tabela carrega quais colunas ANTES de concluir — o nome da tabela no mapa pode ser impreciso.
- **Contagem total de `especialidade_id` revela o domínio canônico:** enumerar TODAS as ocorrências (89) mostrou que só UMA está na clínica (o fallback join em `CentralAtendimentoService`) e o resto (88) é plantões/médicos — prova de que o id é canônico no outro domínio. Enumeração completa > amostragem.
- **Deadlock 40P01 em `PurgarAsync()`/`Dispose` = flake de catálogo ①, não asserção:** classes que purgam estado via FK no cleanup podem dead-lockar sob RAM baixa; a assinatura "método diferente a cada run" + erro de **DB** (não `Assert`) confirma flaky de concorrência — isolar (5×) sob memória normal prova não-regressão.
- **Filtro de teste por prefixo de classe:** `FullyQualifiedName~R8B16` casa no nome da classe — padrão reutilizável (`build-r8bNN.bat` + log).
