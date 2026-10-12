# R8-B7 — Glosa por item de lote + recurso rastreável com documentos (§22 — fatia §22)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia de bug/gap "glosa por item" da fila pós-R7-D (ordem quebrado→alto impacto: B1→B5 → D1 → B6 → **B7**)
- **Data:** 2026-10-12
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (roteiro `r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica)

Matriz `r8a0-auditoria-inicial-matriz-estado.md`, linha "Glosa parcial + recurso rastreável → reconciliável" (§22):

> "abatida no saldo (`glosa_reconhecida`); prazo gera prioridade ALTA <2 dias; 2 tabelas … | **parcial** | **documentos anexos ao recurso ausentes; glosa por conta/título, não por item de lote** | fatia §22 | teste: lote parcial+glosado reconciliável (§30)"

Varredura `r8a0-varredura-financas-pacientes-imports-falhas-notificacoes.md` (área A): a glosa v115 era registrada apenas por conta/título; o recurso não tinha trilha própria; e não havia ligação com o item do lote v116.

**Achado novo desta scoping (mesmo padrão de dead column do R8-B5):** a coluna `v113_faturas.glosa_reconhecida` era **lida** na fórmula do saldo (`V115FaturamentoRegraService.cs` — saldo = valor − descontos + acréscimos − glosa_reconhecida − recebido + estornado) mas **NINGUÉM escrevia nela** — ou seja, registrar glosa não afetava o saldo nem depois. Escrever nela ao registrar (e devolvê-la ao resolver) é **completar o design já declarado pela leitura**, não inventar regra. Confirmação no banco: `valor_recuperado` (coluna existente, grafia com `p`) também estava nunca escrita, apesar de o reader (`ListarAsync`) já fazer `coalesce(valor_recuperado,0)`.

Fatos de domínio sustentando o desenho:

- `v113_faturas.glosa_reconhecida numeric` (existente) e `v115_regras_glosa.valor_recuperado` (existente, ck `ck_v195_glosa_positiva`) — sem writer.
- `v116_faturamento_lote_itens`: `AdicionarItemAsync(loteId)` recebia o id do lote e o **descartava** (`AdicionarItemAsync(Guid id, …) => CriarAsync("…lote_itens", …)` sem passar `id`); a tabela nem tinha coluna `lote_id`.
- Saude360 (`AcaoAsync("convenioGlosas", id, "recorrer")`) exige justificativa (`Saude360ClinicalService.cs`) e grava status `EM_RECURSO` + audit, **sem** persistir justificativa nem documentos fora do audit; `convenio_glosa_recursos` existe sem writer em código — jornada clínica distinta (decisão v2315 mantém origem própria), não reutilizada aqui.
- Web: página Glosas = placeholder `Views/V114/Produto.cshtml` (sem forms).

## 2. Decisões (sem regras inventadas)

1. **Glosa referencia item de lote**: `V115GlosaRequest` ganha `LoteItemId Guid?` (opcional — back-compat). Quando informada, valida existência no tenant em `v116_faturamento_lote_itens` (`coalesce(reg_status,'ATIVO')<>'EXCLUIDO'`) → 404 se alheia/inexistente. Persistida em `lote_item_id`.
2. **Saldo reconciliável (writer)**: Registrar glosa → transação que (a) insere a glosa, (b) `v113_faturas.glosa_reconhecida += valor_glosado`. Resolver c/ `ValorRecuperado` → `greatest(0, glosa_reconhecida − valor_recuperado)` + grava `valor_recuperado`. Piso em zero impede saldo negativo por excesso de devolução.
3. **Recurso (V115) rastreável**: `POST api/v115/glosas/{id}/recorrer` body `{Justificativa, Documentos:[{NomeArquivo, TipoMime, ConteudoBase64}]}`. Justificativa obrigatória (wording idêntico à regra Saude360: "Recurso de glosa exige justificativa.") → 400 sem ela. Status `EM_RECURSO` = **reuso do vocabulário já canônico** na variante Saude360 (`StatusForAction`). 1 recurso por glosa (modelo por colunas); docs vão para nova tabela `v115_recurso_documentos` na mesma transação; audit `RECORRER_GLOSA`.
4. **Race-safety do recurso**: pré-checagem `status='ABERTA'` + UPDATE com predicado `status='ABERTA'`; `affected==0` → 409. Dois recursos concorrentes → exatamente um efetivado (prova em teste).
5. **Resolver com valor recuperado**: novo record `V115ResolucaoGlosaRequest(string? Motivo, decimal? ValorRecuperado)` muda o binding de `glosas/{id}/resolver`; `V115ContestacaoRequest` (compartilhado c/ repasse) **intacta**. `valor_recuperado` fora de `[0, valor_glosado]` → 422.
6. **V116 itens rastreáveis**: `AdicionarItemAsync` persiste `lote_id` (dedicado insert c/ `returning`); novo `ListarItensAsync(loteId)` + `GET api/v116/faturamento/lotes/{id:guid}/itens`. Helpers do base `V116BaseService` ampliados p/ `protected` (visibilidade só) para reutilização pelo sub-service — sem mudar o comportamento dos demais sub-serviços.
7. **Back-compat**: glosa sem `LoteItemId` continua funcionando (campos cauda nullable no `GlosaDto`); resolver sem `ValorRecuperado` segue só mudando status (como antes).
8. **Web inalterado**: página Glosas segue placeholder (dívida UI registrada); **sem novas chaves i18n** → F6 (244 chaves Adm360 pt-BR) intacto.
9. **Nota implementativa (executada)**: sob transação (extended protocol do Npgsql), parâmetro nomeado **não referenciado** no SQL dispara `42P08 could not determine data type` — o helper `ScopeWith` injetava `isGlobal` sempre (inocuo no fluxo antigo batchado) → removido do escopo deste serviço. E parâmetro usado **só** em `is not null`/booleano com valor nulo também não tem tipo inferível → a prioridade do alerta passou a ser computada em C# (`ALTA` se prazo ≤ 2 dias, regra já existente), deixando `@prazoRecurso` só na atribuição de coluna. Grafia da coluna é `valor_recuperado` (com `p`), conferida por hex no banco.

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `database/migrations/2026_10_v2350_r8b7_glosa_item_recurso_documentos.sql` | Nova: `v115_regras_glosa` + `lote_item_id uuid null`, `recurso_justificativa text null`, `recurso_em timestamptz null` (+ índice parcial `ix_v115_glosa_lote_item` where `reg_status='A'`); `v116_faturamento_lote_itens` + `lote_id uuid null` (+ índice `(tenant_id, lote_id)`); nova tabela `v115_recurso_documentos` (+ índice em `glosa_id`). Idempotente. |
| `backend/PlantaoPro.Api/V115GlosaService.cs` | Reescrito: `RegistrarAsync` (valida item + transação insert glosa / abate `glosa_reconhecida` / alerta c/ prioridade em C#), `ResolverAsync` (novo binding `V115ResolucaoGlosaRequest`, valida `[0,glosado]` 422, devolve ao saldo c/ piso 0, grava `valor_recuperado`), `RecorrerAsync` (justificativa 400, `ABERTA→EM_RECURSO` race-safe 409, docs na mesma tx), `DocumentosAsync` (GET round-trip base64), `ListarAsync` estendido (+`loteItemId/recursoJustificativa/recursoEm`), record `V115GlosaRequest` +`LoteItemId`. |
| `backend/PlantaoPro.Api/FinancialContracts.cs` | `GlosaDto` +3 campos cauda (mapeamento posicional Dapper preservado); 4 records novos: `V115RecursoDocumentoRequest`, `V115RecursoRequest`, `V115ResolucaoGlosaRequest`, `V115RecursoDocumentoDto`. |
| `backend/PlantaoPro.Api/V116ConsolidacaoServices.cs` | Helpers do base + `V116Contexto` → `protected`; `AdicionarItemAsync` persiste `lote_id` (insert dedicado c/ `returning` + evento); `ListarItensAsync(loteId)` novo. |
| `backend/PlantaoPro.Api/Controllers/V115FaturamentoController.cs` | `resolver` rebinda `V115ResolucaoGlosaRequest`; novos `POST glosas/{id:guid}/recorrer` e `GET glosas/{id:guid}/documentos`. |
| `backend/PlantaoPro.Api/Controllers/V116ConsolidacaoController.cs` | Novo `GET faturamento/lotes/{id:guid}/itens`. |
| `backend/PlantaoPro.Tests/R8B7GlosaItemRecursoTests.cs` | Nova: 14 fatos (item persiste+abate saldo; sem item back-compat; item outro tenant 404; excede elegível 422; recorrer persiste just+docs c/ round-trip; sem justificativa 400; glosa não aberta 409; concorrência dois recursos um-ganha-um-sem-doc-duplicado; resolver vr total/partial devolve ao saldo; vr>glosado 422; **jornada §30: lote parcial+glosado reconciliável exato**; contrato migration×serviço×endpoint). Fixture padrão R7-A1 + garantia de schema v2350 (advisory lock + probe de coluna + retry 40P01). |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | Linha "Glosa parcial + recurso rastreável" (§22) → corrigido (residual = UI Web, dívida registrada). |

## 4. Evidência (executada)

- **Alvo:** `R8B7*` + regressão `V115*` + `V116*` + `RepositoryHygieneContractTests` = **32/32 aprovados** (build sln completa: `BUILD30_EXIT:0`, `RUNR8B7_EXIT:0`).
- **Gate phaseA v2 (pipeline):** contratos R7E1/R7A1/R7OB + login/refresh — **EXIT=0, 52/52**.
- **Full ×3 (pipeline v2):** total agora **1328** (1314 + 14 novos) — full1 **1328/1328 EXIT=0**; full2 **1328/1328 EXIT=0**; full3 **1328/1328 EXIT=0**. Sem flake nos três fulls (RAM livre no início: 1,08 GB; catálogo de flakes ① inalterado, nada acionado).
- **Higiene do repositório:** `RepositoryHygieneContractTests` aprovada no alvo e nos 3 fulls; nenhum termo novo banido introduzido.
- **Verificação byte-level dos arquivos novos/mudados:** tokens de schema presentes (`lote_item_id`, `recurso_justificativa`, `recurso_em`, `glosa_reconhecida`, `valor_recuperado`) e typos recorrentes ausentes (`MescadoEm`, `recuero`, `plantaoUro/plantaouro`, e a grafia errada `valor_recuerado` sem o `p`); grafia crítica conferida por hex contra o banco (`valor_recuperado` len=16). Rotas novas confirmadas no controller.
- **Nota de debugging (registrada):** duas causas de falha encontradas e corrigidas antes do verde — (a) `42P08` por parâmetro `@prazoRecurso` usado em `is not null` nulo sob extended protocol (resolvido computando prioridade em C#); (b) `42703` por grafia `valor_recuerado` (correto: `valor_recuperado`). Ambas isoladas por teste diagnóstico temporário (removido depois).

## 5. Dívida registrada (fora do escopo desta fatia)

1. **UI Web de glosa/recurso (forms)**: a página Glosas segue placeholder `Produto.cshtml` — a API já expõe `POST /recorrer` e `GET /documentos`, mas não há tela para anexar documento nem preencher justificativa. Dívida UI (próxima fatia), sem novas chaves i18n por enquanto.
2. **Leitura de glosas com origem mesclada / item de lote removido**: `lote_item_id` é referência lógica (sem FK); se o item for removido/excluído, a glosa permanece como histórico — evolução simples se a UI pedir "ver item da glosa".
3. Base principal em v2339: o lote de deploy passa a incluir **v2340–v2350**.

## 6. Fila seguinte (R8)

Pela ordem quebrado→alto impacto de `r8a0` §4, com B7 entregue: importações sem prévia (§24, "fatia §24") → 5 calculadores de KPI sobrepostos → `cid_tabela` duplicada em 6 lugares. Alternativa: janela viva de homologação (`r6a1b` §7).
