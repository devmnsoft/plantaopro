# R8-B6 — Mesclagem controlada de pacientes (§23 — operação inteira ausente)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia de bug/gap #6 da fila pós-R7-D (ordem quebrado→alto impacto: B1→B5 → D1 → **B6**)
- **Data:** 2026-10-12
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (roteiro `r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica)

Matriz `r8a0-auditoria-inicial-matriz-estado.md`, linha "Mesclagem controlada c/ impacto + sem cross-tenant" (§23):

> "**ausente** (busca `merge/mescla/merged_into` repo-wide → 0) | ausente | operação inteira | fatia §23 | novo teste (§30)"

Varredura `r8a0-varredura-financas-pacientes-imports-falhas-notificacoes.md`, área B (§23 Pacientes): sem operação de mesclagem em nenhum ponto do código — duplicidade encontrada na busca (nome/CPF/telefone) só tinha saída manual: inativar um dos dois e perder o vínculo do histórico (ou conviver com duas contas para o mesmo CPF).

Fatos do domínio que sustentam o desenho:

- `pacientes` tem `status text ('ATIVO')` **e** `reg_status char ('A')`; os readers (ex.: `ObterAsync`) filtram `reg_status='A'`.
- **Sem FKs físicas** nas 39 tabelas que referenciam `paciente_id` (referência em nível de aplicação) — lista definitiva mapeada via `information_schema` no banco real.
- Índices únicos de CPF condicionais a `reg_status='A'` por `cliente_id` (`ux_pacientes_cliente_cpf_informado`) — inativação comum **não** libera o CPF.
- `ux_plano_saude_pacientes_principal`: no máximo um plano principal ativo por paciente — mesclar dois pacientes com principal em cada um violaria o índice se não despromovido antes.
- `paciente_historico(acao, detalhes jsonb, usuario_id)` é o veículo de trilha; `AuditAsync` cobre auditoria geral.

## 2. Decisões (sem regras inventadas)

1. **Mesclagem origem→destino (alvo)**: todos os vínculos das 39 tabelas com `paciente_id` são re-pontejados para o alvo (`TabelasComPacienteId`, constante conferida com `information_schema` vivo pelo teste de contrato). **Sem regra de merge de dados**: vínculos movidos, dados do alvo intactos.
2. **Inativação da origem = `status='INATIVO'` E `reg_status='I'`** — deliberadamente mais forte que a inativação comum (que só zera `status` e retém o CPF no índice único): a absorção libera o CPF, permitindo recriar um cadastro legítimo com o mesmo documento.
3. **Plano principal da origem é despromovido ANTES do re-pontejamento** — o principal do destino prevalece (índice único parcial).
4. **Trilha**: colunas `mescado_em/em_em/por` na origem (v2349 + índice parcial para encontrar "quem me absorveu") + linha `MESCLAR` no `paciente_historico` de **ambos** (a do alvo cita a origem; a da origem cita o destino) + `AuditAsync("pacientes", alvoId, "MESCLAR", …)`.
5. **Gates**: motivo obrigatório (400); alvo == origem (400); 404 quando qualquer um não existe; **cross-tenant 409** (`cliente_id` estritamente igual nos dois, inclusive ambos null); origem já mesclada (409); alvo já mesclado não recebe nova mesclagem (409); acesso por tenant (403).
6. **Concorrência**: `FOR UPDATE` nos dois ids em ordem determinística (crescente) + `UniqueViolation → 409`. Duas mesclas simultâneas da mesma origem → exatamente uma ganha.
7. **Contrato**: `POST api/pacientes/{alvoId}/mesclar?origem={guid}` com body `{Motivo}` (reuso de `Saude360ActionRequest`); na Web, página "Mesclar em outro" acessível do formulário de edição do paciente (entrada só existe quando há Id) — pt-BR hard-coded como as vizinhas, sem nova chave resx (F6 244 chaves Adm360 intacto).
8. **Efeito colateral aceito e documentado**: `ObterAsync` filtra `reg_status='A'`, então a origem fica invisível para leituras normais após a mescla — recuperável pela trilha (`mesclado_em` + histórico). Não foi inventado reader novo nesta fatia.
9. **Nota implementativa (executada)**: a Web não referencia o projeto API (comunicação via HTTP) — o select de destinos desserializa num tipo local `PacienteResumoDto {Id, Nome}` do mesmo contrato JSON (binding case-insensitive); e os SELECTs do serviço usam aliases (`as ClienteId` etc.) para o tuple do Dapper funcionar sob mapeamento por nome **ou** por posição.

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `database/migrations/2026_10_v2349_r8b6_mesclagem_pacientes.sql` | Nova: 3 colunas em `pacientes` (`mescado_em uuid null`, `mescado_em_em timestamptz null`, `mescado_por uuid null`) + índice parcial `ix_pacientes_mesclado_em` (origens mescladas, para o finder futuro). Idempotente. |
| `backend/PlantaoPro.Api/Saude360ClinicalService.cs` | Constante `TabelasComPacienteId` (39 tabelas) + método `MesclarPacientesAsync(alvoId, origemId, motivo)` transacional: locks em ordem determinística, gates 404/409/403, despromoção do plano principal da origem, re-pontejamento das 39 tabelas, inativação forte da origem, trilha nos dois históricos + `AuditAsync`. Retorna o alvo via `ObterAsync`. |
| `backend/PlantaoPro.Api/Controllers/Saude360ClinicalControllers.cs` | `POST api/pacientes/{id:guid}/mesclar` (`[FromQuery] Guid origem`, `[FromBody] Saude360ActionRequest`). |
| `backend/PlantaoPro.Web/Controllers/Saude360WebControllers.cs` | Ações `Mesclar` GET/POST (candidatos via `api/pacientes/buscar?tamanho=200`; POST reutiliza `ExecutarAcaoAsync(token, "api/pacientes/{alvo}/mesclar?origem={id}", motivo)`). |
| `backend/PlantaoPro.Web/Models/Saude360FormSpecificViewModels.cs` | `MesclarPacienteFormViewModel` (alvo + motivo obrigatórios) + `PacienteResumoDto` (tipo local de desserialização — ver decisão 9). |
| `backend/PlantaoPro.Web/Views/Pacientes/Mesclar.cshtml` | Nova: banner de consequências (inativação + trilha + irreversível), select de destino, motivo, rodapé auditada. |
| `backend/PlantaoPro.Web/Views/Pacientes/_Form.cshtml` | Botão "Mesclar em outro" no rodapé, apenas quando editando (`Id.HasValue`). |
| `backend/PlantaoPro.Tests/R8B6MesclagemPacienteTests.cs` | Nova: 9 fatos (happy-path completo com liberação de CPF provada; sem motivo 400 sem escrita; alvo==origem 400; cross-tenant 409 nada movido; origem já mesclada 409; alvo já mesclado 409; plano principal despromovido; concorrência um-ganha-um-409; contrato migration×serviço(reflexão)×endpoint×information_schema vivo). Fixture padrão R7-A1 + garantia de schema v2349 (advisory lock + probe de coluna + retry 40P01). |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | Linha "Mesclagem controlada" (§23) → corrigido. |

## 4. Evidência (executada)

- **Alvo:** `R8B6*` + regressão `R7A1*` + `RepositoryHygieneContractTests` = **32/32 aprovados** (build29 sln completa: `BUILD29_EXIT:0`, `RUNR8B6_EXIT:0`).
- **Alvo isolado (nomes + durações):** run com filter somente `R8B6MesclagemPacienteTests` — **9/9 aprovados** em 3,0 s (`Mescla_MoveTodosVinculos_InativaOrigem_ComTrilhaECPFLiberado [1 s]`, `Mescla_AlvoJaMesclado_409 [54 ms]`, `Mescla_PlanoPrincipalDaOrigem_Despromovido [37 ms]`, `Contrato_MigrationEServicoEEndpointCoerentesComSchemaVivo [36 ms]`, `Mescla_SemMotivo_400SemEscrita [13 ms]`, `Mescla_CrossTenant_409NadaMovido [36 ms]`, `Mescla_Concorrencia_MesmaOrigemDoisAlvos_ExatamenteUmGanha [213 ms]`, `Mescla_OrigemJaMesclada_409 [28 ms]`, `Mescla_AlvoIgualOrigem_400 [< 1 ms]`).
- **Gate phaseA v2 (pipeline):** contratos R7E1/R7A1/R7OB + login/refresh (52) — **EXIT=0**, 52/52.
- **Full ×3 (pipeline v2):** total agora **1314** (1305 + 9 novos) — full1 **1314/1314 EXIT=0**; full2 **1313/1314**, 1 falha em `SaasGovernancaB6Rodada4Tests.G4b_ReativarComConflito_Retorna409_E_AtivaOriginalIntacta` com a assinatura canônica da categoria ① (`Npgsql.PostgresException 40P01 impasse detectado` em `PurgarAsync()`, classe do catálogo; RAM livre no início do pipeline: 0,65 GB) — **isolate-rerun da classe inteira = 8/8 aprovados** (transiente confirmada antes de declarar); full3 **1314/1314 EXIT=0**.
- **Higiene do repositório:** `RepositoryHygieneContractTests` (termos banidos) aprovada no alvo e nos 3 fulls; nenhum termo novo introduzido.

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Leitura de origens mescladas**: `ObterAsync`/listagens ocultam a origem pós-mescla (`reg_status='A'`). Se a UI precisar de "pacientes mesclados", é reader novo usando `ix_pacientes_mesclado_em` — evolução simples, não inventada aqui.
2. **Inativação comum continua não liberando CPF** (dívida pré-existente da área B): inativar por `status` mantém o CPF no índice único. Se a política mudar para "inativar libera CPF", afeta o fluxo comum — DEC separada.
3. **Select de destinos limitado a 200 cadastros** (`buscar?tamanho=200`): tenants grandes vão precisar de busca por termo no próprio formulário — evolução simples.
4. Base principal em v2339: o lote de deploy passa a incluir **v2340–v2349**.
5. Categoria ① de flake (40P01 sob carga da suíte inteira): ocorreu **1 vez** em full2 (`SaasGovernancaB6Rodada4Tests`, `40P01` em `PurgarAsync`, RAM 0,65 GB) — transitividade provada por isolate-rerun 8/8. Catálogo inalterado (4 classes).

## 6. Fila seguinte (R8)

Pela ordem quebrado → alto impacto de `r8a0` §4, com B6 entregue: glosa por item (§22 parcial, "fatia §22") → importações sem prévia (§24, "fatia §24") → 5 calculadores de KPI sobrepostos → `cid_tabela` duplicada em 6 lugares. Alternativa: janela viva de homologação (`r6a1b` §7).
