# R8-B5 — Write path produtivo de `medico_especialidades` + backfill v2348

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia de bug/gap #5 (1ª da fila pós-R7-D, ordem quebrado→alto impacto)
- **Data:** 2026-10-12
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (roteiro `r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica)

Varredura `r8a0-varredura-vinculos-unidades.md`, seção A2 — Especialidades/habilitações na operação (**parcial**):

> "Não há write path produtivo em `medico_especialidades`: a única INSERT do código é em teste. Em banco real a tabela nasce vazia — lacuna operacional real (todos os médicos ficariam inelegíveis por especialidade se não populado manualmente)."

Matriz `r8a0-auditoria-inicial-matriz-estado.md`, linha "Especialidades na operação" (§17):

> "**sem caminho de escrita produtivo** (única INSERT é em teste; tabela nasce vazia em banco real) | ação: poplar/migrar + UI".

Fatos do domínio que sustentam o diagnóstico:

- A tabela M:N nasceu em **v2341** para sustentar a elegibilidade por especialidade (`MedicoElegibilidadeService.VerificarEspecialidadeCompatívelAsync`, motivo `ESPECIALIDADE`, `BusinessRulesServices.cs:187-192`) e o score de recomendação (80×20, `:335`/`:339`).
- O formulário de médico **já tem** select obrigatório de especialidade (`_MedicoForm.cshtml`, `asp-for="EspecialidadeId"`), e `MedicoService.CriarAsync/AtualizarAsync` persistiam essa escolha só em `medicos.especialidade_id` (coluna criadas em v2200 **sem FK**).
- Consequência: o consumidor (aceite de convite/substituição de escala e recomendação) lia o M:N que nenhum writer produtivo populava — médico com especialidade selecionada ficava inelegível por ela e com score 20 em vez de 80.

## 2. Decisões (sem regras inventadas)

1. **`medicos.especialidade_id` continua sendo o campo administrativo canônico** — o contrato do formulário (select único obrigatório) não muda; nada de multi-seleção inventada nesta fatia (ver dívida §5.1).
2. **Espelho replace-all para o M:N**: o writer desativa os vínculos ativos que não foram selecionados (`reg_status='I'`), **reutiliza a linha histórica** quando o par já existiu inativo (`'I'→'A'`, mesma row — `reg_date` original preservado) e só insere se o par nunca teve registro. Índice único parcial `ux_medico_especialidade_ativa` (v2341) garante no máximo um vínculo ativo por médico/especialidade; histórico sem exclusão física (padrão `reg_status` do código-base).
3. **Sem especialidade** (`Guid.Empty`, possível via API mesmo com o select obrigatório na Web) → todos os vínculos ativos do médico são desativados.
4. **Troca atômica em transação própria** na mesma conexão (`EspelharEspecialidadeAsync`). Gotcha implementativo provado por execução: o Dapper **refecha conexões que ele mesmo abriu por comando** — a conexão chegava fechada ao helper após o `ExecuteAsync` de criar/atualizar ("Connection is not open"); o helper abre/fecha apenas quando foi ele quem abriu, preservando o ciclo de vida da conexão do chamador.
5. **Backfill v2348** (o "migrar" da matriz): dos médicos já existentes com `especialidade_id` válida (não nula, não GUID vazio, médico ativo), reativa a linha histórica quando existe, senão insere. Idempotente (repetir não duplica nem desativa). **Defensivo contra órfãos**: pula médicos cujo `especialidade_id` aponta para especialidade inexistente — legítimo porque a coluna nasceu em v2200 sem FK (órfão real encontrado na suíte durante a prova).
6. **Sem mudança de contrato público**: sem novo endpoint, sem nova coluna, sem nova chave i18n (form já alimenta o campo), sem novo estado de enum — o F6 (244 chaves Adm360) não é alterado.
7. **`InativarAsync` não tocada** (decisão registrada): a inelegibilidade de médico inativo já bloqueia no nível do médico (motivo `MEDICO_INATIVO`); manter os vínculos intactos faz a reativação devolver elegibilidade imediatamente, sem re-cadastro.

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `database/migrations/2026_10_v2348_r8b5_backfill_medico_especialidades.sql` | Nova: backfill idempotente `medicos.especialidade_id → medico_especialidades` (reactiva histórico / insere faltante), com guard contra especialidade órfã. |
| `backend/PlantaoPro.Api/Data.cs` | `MedicoService`: chamada ao espelho em `CriarAsync` e `AtualizarAsync` + helper privado transacional `EspelharEspecialidadeAsync(NpgsqlConnection, Guid, Guid)`. |
| `backend/PlantaoPro.Tests/R8B5EspecialidadesMedicoTests.cs` | Nova: 7 fatos (espelho no criar + consumidor enxerga; troca mantém um único ativo com histórico preservado; volta à histórica reativa sem duplicar; remoção desativa tudo; criar sem especialidade não gera vínculo; backfill popula legado + idempotente; contrato de fonte/form/migration). Fixture padrão R7-A1 (tenant próprio, cleanup escopado nas duas pontas — médico **e** especialidade). |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | Linha "Especialidades na operação" → corrigido (write path + backfill). |

Nota de teste: o cleanup inicial escopava o M:N apenas por médico-do-tenant e falhava em FK — o writer cria o médico **sem** `tenant_id`/`cliente_id` (padrão do insert legado, Data.cs:640), então os vínculos dele escapavam do escopo. Corrigido limpando também por `especialidade_id in (especialidades do tenant)`.

## 4. Evidência (executada)

- **Alvo:** `R8B5*` + regressão `R7A1*` + `RepositoryHygieneContractTests` = **30/30 aprovados** (build28: `BUILDTESTS28_EXIT:0`, `RUNR8B5_EXIT:0`).
- **Gate phaseA v2** (contratos R7E1/R7A1/R7OB + login/refresh, 52): **EXIT=0**.
- **Full ×3 (pipeline v2):** total agora **1305** (1298 + 7 novos) — full1/full2/full3 todos **EXIT=0**, nenhuma falha, nenhum flake observado nesta rodada (RAM livre 0,71 GB no início — categoria ① não ocorreu).
- **Higiene do repositório:** contrato de termos banidos EXIT=0 na árvore final (o alvo e os 3 fulls incluem a classe; sem termos novos introduzidos).

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Uma especialidade por médico** (limitação do formulário select-único); multi-especialidade exigiria mudança de UI (checkbox) + decisão de regra — DEC, não fatia.
2. O espelho roda **indistintamente** mesmo quando o `AtualizarAsync` também muda `reg_status` (inativação via update): o vínculo fica `'A'` num médico inativo — inofensivo (bloqueio já ocorre no nível do médico) e desejável (reativação imediata); se a política mudar para "inativar cascata vínculos", é DEC.
3. Órfãos de `medicos.especialidade_id` (especialidade inexistente) são **pulados** pelo backfill e continuam inelegíveis até receberem especialidade válida via formulário; a coluna continua sem FK (v2200) — constraint poderia vir junto com a DEC de multi-especialidade.
4. `tenant_id`/`cliente_id` seguem NULL nas linhas espelhadas (mesma forma da única INSERT prévia — fixture R7A1 — e os readers não filtram por tenant); scoping de tenancy do M:N pode vir como DEC separada.
5. Base principal em v2339: o lote de deploy passa a incluir **v2340–v2348**.
6. Categoria ① de flake (40P01 sob carga da suíte inteira) permanece aberta como item de infraestrutura — **não ocorreu** neste pipeline; catálogo inalterado (4 classes).

## 6. Fila seguinte (R8)

Pela ordem quebrado → alto impacto de `r8a0` §4, com `medico_especialidades` entregue: ausência de mapeamento usuário×unidade; granularidade de glosa por conta/título; merge de paciente ausente; importações sem prévia; 5 calculadores de KPI sobrepostos; `cid_tabela` duplicada em 6 lugares. (A duplicação Usuarios/Perfis na sidebar é intencional, documentada.)
