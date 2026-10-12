# R8-B3 — Política de conflito de SALA em agendamentos clínicos

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia de bug/policy #3
- **Data:** 2026-10-11
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (roteiro `r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica)

Matriz de estado `r8a0-auditoria-inicial-matriz-estado.md`, linha "Recurso compartilhado sem reservas incompatíveis (sala/guichê/setor)":

> tabelas prontas (`painel_chamada_fila(setor,sala,guiche)`, `agendamentos.sala_id`); política de conflito **somente por profissional** (`ConflitoHorarioService.ListarFontesConflitoAsync`); nenhum índice/constraint anti-double-booking por sala — **parcial (tabelas) / ausente (regra)** — dois agendamentos simultâneos na mesma sala eram possíveis.

Classificação: **ausente (regra)** com infraestrutura pronta — o encontro não era inventado; a coluna `agendamentos.sala_id` já existia (sem FK, migration base clínica) e a UI/API já aceitava `SalaId` na criação/atualização de agendamentos, mas nenhum ponto de decisão a considerava.

## 2. Decisões (sem regras inventadas)

1. **Extender a política canônica** (R7-A1) em vez de inventar um novo ponto de decisão: novo método `ConflitoHorarioService.ListarFontesConflitoSalaAsync` / `ExisteConflitoSalaAsync`, no mesmo serviço, mesmo intervalo `[inicio, fim)` fim exclusivo, mesma convenção "adjacência não é conflito".
2. **Comparação apenas quando ambos declaram sala** — sem sala declarada (`NULL`) não há conflito de sala (não há como comparar o que não foi declarado). Cobre as duas direções (novo sem sala / existente sem sala), porque a consulta ancora no `sala_id` do lado que declara.
3. **Predicado de "ativo" espelhado do ramo AGENDAMENTO da política unificada**: `reg_status='A'` e `upper(status) not in ('CANCELADO','FINALIZADO')`. Nada de whitelist nova ou status inventado.
4. **Prioridade de mensagem**: se houver conflito do profissional E de sala, a mensagem do profissional prevalece (precedência existente preservada); a de sala só aparece quando o profissional está livre.
5. **Revalidação no reagendamento cobre mudança APENAS de sala** (período inalterado) — antes do corte, mudar só a `SalaId` não entrava no bloco de revalidação. Sala efetiva pós-update = `coalesce(request.SalaId, sala atual)` — mesma semântica do `BuildUpdate` (`sala_id=coalesce(@SalaId,sala_id)`).
6. **Escopo = agendamentos** (o encontro documentado fala exatamente disso). Outros referenciadores de sala ficam como dívida (§5). Nenhum número, preço ou prazo foi adicionado; nenhuma regra clínica foi inventada.

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Api/BusinessRulesServices.cs` | `ConflitoHorarioService`: novos `ListarFontesConflitoSalaAsync(Guid salaId, DateTime di, DateTime df, Guid? agendamentoIgnoradoId)` e `ExisteConflitoSalaAsync(...)` (consulta única sobre `plantaopro.agendamentos`, âncora `sala_id`). |
| `backend/PlantaoPro.Api/Saude360ClinicalService.cs` | `VerificarConflitoAgendamentoAsync` ganha parâmetro `Guid? salaId` e aplica a checagem de sala quando o profissional está livre; criação (`CriarAsync`) passa `request.SalaId`; atualização (`AtualizarAsync`) estende o gatilho com `\|\| request.SalaId.HasValue`, lê a sala atual do registro, computa a sala efetiva e revalida (checagem completa quando período/profissional mudaram; somente sala quando mudou apenas a sala). |
| `backend/PlantaoPro.Tests/R8B3AgendamentoConflitoSalaTests.cs` | Novo: 8 fatos cobrindo criação (mesma sobreposta 409, adjacência OK, salas diferentes OK, NULL nas duas direções OK, cancelado não bloqueia OK), reagendamento (só sala 409 preservando registro; mover período na mesma 409) e o predicato de ativo. |

Sem migração (nenhuma mudança de schema — a coluna já existia). Sem mudança de contrato público. Sem i18n novo (mensagem de erro segue o padrão pt-BR literal dos conflitos existentes do R7-A1).

## 4. Evidência (executada)

- **Alvo:** `R8B3*` = **8/8 aprovados** (build26, `BUILDTESTS26_EXIT:0`).
- **Gate phaseA v2** (contratos R7E1/R7A1/R7OB + login/refresh, 52): EXIT=0 — inclui a regressão direta do `ConflitoHorarioService` modificado.
- **Full ×3 (pipeline v2):** total agora **1290** (1282 + 8 novos).
  - `full3`: **1290/1290 verde**, EXIT=0.
  - `full1`: 1 falha — `SaasGovernancaB6Rodada4Tests.G4b_...` → `Npgsql.PostgresException 40P01` em `PurgarAsync` (classe não relacionada à fatia).
  - `full2`: 2 falhas — `R7OBOnboardingPreferenciasIdiomaTests.O3_...` e `SaasComercialB4MatrizTests.B2_...` → mesmo `40P01` em `PurgarAsync`.
- **Prova de flake (categoria ①):** assinatura exata do catálogo — `40P01: impasse detectado` sob carga paralela da suíte inteira, testes diferentes a cada run, sempre nos purges de classes não relacionadas; RAM livre ~1,9 GB no início. Isolate-rerun das três classes: **8/8 + 5/5 + 12/12 = 25/25 aprovados** (isolate-r8b3.log). `R7OBOnboardingPreferenciasIdiomaTests` entra no catálogo como 4ª classe observada da categoria ①.
- **Higiene do repositório:** contrato `Repositorio_NaoDeveConterTermosDoProdutoIncorretoEmArquivosVersionaveis` passou após renomear um teste cujo nome continha um dos substratos proibidos (análise case-insensitive). Varredura adicional dos termos proibidos nos 3 arquivos alterados: limpo.

## 5. Dívida registrada (fora do escopo desta fatia)

1. `painel_chamada_fila` referencia setor/sala/guichê como filas de senha, sem reserva temporal — sem política de conflito para essas linhas (natureza diferente: fila, não agenda horária).
2. Escalas/plantões não reservam sala (conflito de profissional é ortogonal ao de sala) — nada a fazer enquanto plantão não tiver sala declarada.
3. Não há constraint excludente de banco em `agendamentos(sala_id, período)` — a política vive na camada de serviço (mesma escolha do R7-A1). Um constraint exigiria `btree_gist` + validação prévia de dados (a base principal em v2339 pode conter sobreposições históricas legítimas) — decisão adiada, não esquecida.
4. A criação de agendamento não valida existência/status de sala (`clinica_salas_atendimento`) — o `sala_id` é apenas âncora de conflito; validação de referência é outro encontro (não incluído aqui para manter a fatia mínima).
5. Categoria ① de flake (40P01 em carga) continua aberta como item de infraestrutura de teste; agora com 4 classes catalogadas.

## 6. Fila seguinte (R8)

Próximo por ordem quebrado → alto impacto: **cancelamento de compra recebida** (ver matriz §4 de `r8a0`), depois os demais achados §17–§30.
