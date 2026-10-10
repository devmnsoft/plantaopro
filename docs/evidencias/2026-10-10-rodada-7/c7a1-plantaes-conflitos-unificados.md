# R7-A1 — Plantões: conflito de horário unificado do profissional + provas de concorrência das jornadas

Data: 2026-10-10. Base: `HEAD = 3adfc55` (auditoria C7-A0 da rodada 7). Banco de prova:
`plantaopro_test` (PostgreSQL 18 local); migrações **v2341/v2342/v2343** aplicadas via
`PlantaoPro.Tools.Database upgrade` (saídas: `upgrade: 2026_10_v2341… aplicada.`, `upgrade:
2026_10_v2342… aplicada.`, `upgrade: 2026_10_v2343… aplicada.`).

Suíte de fechamento: **1219/1219** (baseline da rodada: 1204), executada 3 vezes seguidas sem
oscilação. Uma execução intermediária morreu com `StackOverflowException` no processo de teste
(sem teste nomeado no log); não se reproduziu nas 3 execuções seguintes — monitorar se recursar.

**Estado honesto**: IMPLEMENTADO → TESTADO (suíte + banco real). AINDA NÃO HOMOLOGADO em host vivo
e não LIBERADO. Commit local; push diferido por decisão da rodada.

## 1. O que o ponto pediu (Bloco 4 da matriz C7-A0) e o que foi feito

| Exigência / lacuna da auditoria | Situação | Como ficou |
|---|---|---|
| Consulta sobreposta a escala confirmada → conflito identificado | Quebrado → fechado | Criação e reagendamento de `agendamentos` consultam a política canônica de conflito (escala confirmada/solicitada/em andamento × período), respondendo **409 impeditivo** com mensagem por fonte. |
| Escala × agenda clínica também do lado do aceite | Quebrado → fechado | O aceite de convite operacional (`EscalaService.AceitarConviteAsync`) trocou sua SQL inline própria pela **mesma função canônica**; médico com agendamento clínico ativo sobreposto agora recebe 409. |
| Bloqueio/indisponibilidade do profissional na criação de consulta | Ausente → fechado | `medicos.bloqueado=true` (ou inativo) → 409; janela ativa em `medico_indisponibilidades` → 409. Mesma regra de bloqueio já usada pela elegibilidade de plantões. |
| Corrida da última vaga sem teste dedicado (c7a0 §gap-b) | Mecanismo OK, sem prova → provado | Teste de concorrência real: dois médicos, dois convites, plantão com 1 vaga, `Task.WhenAll` → exatamente uma confirmação, outra 409, `vagas_disponiveis` termina em 0 e 1 única escala confirmada. |
| Check-in repetido sem prova (c7a0 §gap-c) | Mecanismo OK, sem prova → provado | Sequencial: 2º check-in → 409, 1 linha em `agendamento_checkins`, ≤1 item de fila. Concorrente: exatamente um efetivado. |
| Dois operadores assumindo o mesmo item da fila (aceite integrada) | **Quebrado → fechado** | `chamar`/`rechamar`/`finalizar` no painel agora usam guard de estado esperado (mesmo padrão de consultas/triagens): atualização condicional ao estado lido; perdedor da corrida recebe 409. Prova concorrente incluída. |
| Distinguir impeditivo × alerta de solicitação pendente | Documentado (sem mudança de semântica) | Pontos de compromisso firme (aceite, confirmação, substituição, criação/reagendamento) **bloqueiam**; recomendações continuam marcando `PossuiConflito` sem bloquear. Solicitações (`solicitado`) seguem bloqueando como já bloqueavam entre escalas — mudar isso é decisão de produto, registrada como pendência. |

## 2. Decisão canônica: política única de conflito

- **Única implementação**: `ConflitoHorarioService.ListarFontesConflitoAsync`
  (`backend/PlantaoPro.Api/BusinessRulesServices.cs`), um único SQL `UNION ALL` sobre as três fontes
  de compromisso do profissional em `[dataInicio, dataFim)` (fim exclusivo — adjacência não conflita):
  1. escalas ativas (`solicitado/solicitada/confirmado/confirmada/em_andamento`, `reg_status='A'`);
  2. agendamentos clínicos ativos (`status not in CANCELADO/FINALIZADO`, datas não nulas);
  3. indisponibilidades registradas (`status='ATIVA'`).
- `ExisteConflitoAsync` (assinatura existente) delega a essa função — todos os chamadores antigos
  (aceite de plantão, confirmação, substituição, recomendações, elegibilidade) passam a ver agenda
  clínica + indisponibilidades, o que era exatamente a unificação pedida.
- Chamadores novos: criação/reagendamento de agendamento (`Saude360ClinicalService`) e o aceite de
  convite operacional (`Data.cs`), que perdeu sua query inline duplicada.
- 409 impeditivo com mensagem por fonte; a mensagem de agendamento×agendamento preserva a âncora
  `"Já existe agendamento para o médico"` fixada por contrato
  (`Saude360BaseClinicaContractTests`).
- Teste de contrato garante a ausência de implementação paralela: `Saude360ClinicalService.cs`
  não pode conter `plantaopro.escalas` (usa só a função canônica).

## 3. Problemas latentes encontrados durante a implementação (divergência código↔banco)

A auditoria C7-A0 classificou "SQL/tipos" como pronto; esta investigação mais profunda encontrou
três divergências reais que quebravam fluxos operacionais em tempo de execução (nenhuma era coberta
por teste comportamental — consistentemente com o fato de o aceite de convite nunca ter sido
executado por nenhum teste antes desta rodada):

| Migration | Estrutura ausente | Código que a referencia | Falha latente |
|---|---|---|---|
| `v2341` | `medicos.bloqueado`; tabela `medico_especialidades` (+ índice único parcial `ux_medico_especialidade_ativa`) | `EscalaService` (validação de médico no aceite), `MedicoElegibilidadeService.VerificarEspecialidadeCompatívelAsync` | 500 (coluna/tabela inexistentes) em todo aceite/confirmação de escala |
| `v2342` | colunas `escala_id/status_anterior/status_novo/justificativa/usuario_id/reg_date` em `historico_escala` (+ índice) | `EscalaService.AddHistoricoAsync`; leitura no Portal Profissional (`ConfirmadoEm`) | 500 no aceite (insert) e na listagem do portal profissional |
| `v2343` | `data_resposta/motivo_recusa/reg_update/reg_date` em `plantao_convites` | aceite/recusa/cancelamento de convite; seletores no portal e no app (`MobileController`) | 500 no aceite/recusa de convite e nos seletores de convites |

Migrações criadas em `database/migrations/`, registradas em `migration-manifest.json`
(checksum SHA-256 bruto, `transactional`, `installRequired:false` — mesmo padrão de v2339/v2340) e
aplicadas a `plantaopro_test`. **Debt herdada**: o banco principal `plantaopro` (prod/dev IIS)
ainda carece do upgrade acumulado v2339→v2343 — manter na fila externa de homologação.

## 4. Alterações por arquivo

| Arquivo | Alteração | Validação |
|---|---|---|
| `backend/PlantaoPro.Api/BusinessRulesServices.cs` | `ConflitoFonteDto` + `ListarFontesConflitoAsync` (fonte única unificada); `ExisteConflitoAsync` delega | Testes de conflito (6) + contrato de política única |
| `backend/PlantaoPro.Api/Data.cs` | `AceitarConviteAsync`: SQL inline de conflito substituída pela chamada canônica; mensagem 409 acionável | Teste de corrida da última vaga + teste de unificação reversa (aceite × agenda clínica) |
| `backend/PlantaoPro.Api/Saude360ClinicalService.cs` | Injeção de `ConflitoHorarioService`; `CriarAsync(agendamentos)` usa política unificada (bloqueio + fontes); `AtualizarAsync` revalida conflito em reagendamento/troca de médico (ignorando a si); guard de estado esperado em `painel` (chamar/rechamar/finalizar) para reserva concorrente segura; helper `VerificarConflitoAgendamentoAsync` | Testes comportamentais de criação (6), reagendamento (1), check-in (2), fila (1) |
| `backend/PlantaoPro.Tests/R7A1PlantoesConflitosUnificadosTests.cs` | **Novo** — 15 testes (10 comportamentais + 5 de contrato) | Suíte |
| `backend/PlantaoPro.Tests/Saude360JornadaSFixesRegressionTests.cs` | Construção do serviço adaptada ao novo parâmetro de injeção | Suíte |
| `backend/PlantaoPro.Tests/Saude360TenantScopeP2ContractTests.cs` | Idem | Suíte |
| `backend/PlantaoPro.Tests/V2166CoverageJourneyContractTests.cs` | Contrato do aceite passa a fixar a chamada canônica (a âncora antiga apontava a SQL inline removida) | Suíte |
| `database/migrations/2026_10_v2341_r7a1_conflitos_unificados_profissional.sql` | **Nova** — `medicos.bloqueado` + `medico_especialidades` | Aplicada a `plantaopro_test`; teste de contrato |
| `database/migrations/2026_10_v2342_r7a1_historico_escala_colunas.sql` | **Nova** — colunas de `historico_escala` | Idem |
| `database/migrations/2026_10_v2343_r7a1_convite_ciclo_vida.sql` | **Nova** — colunas de ciclo de vida de `plantao_convites` | Idem |
| `database/migration-manifest.json` | Entradas v2341/v2342/v2343 (checksums verificados contra os bytes dos arquivos) | `Tools.Database upgrade` limpo |

## 5. Classificação dos testes novos (`R7A1PlantoesConflitosUnificadosTests`)

Comportamentais (banco real, seeds isolados por tenant efêmero, limpeza em `finally`):

1. `CriarAgendamento_SobrepostoAEscalaConfirmada_409ComMensagemDeEscala`
2. `CriarAgendamento_SobrepostoAAgendamentoExistente_409PreservaAncoraDeMensagem`
3. `CriarAgendamento_SobrepostoAIndisponibilidadeAtiva_409`
4. `CriarAgendamento_MedicoBloqueado_409`
5. `CriarAgendamento_SemConflito_PersisteComStatusAgendado` (controle positivo)
6. `AtualizarAgendamento_MoverParaSobreEscalaConfirmada_409EPreservaHorarioOriginal`
7. `AceitarConvite_UltimaVaga_DoisConcorrentes_SomenteUmaConfirmacao` (**corrida real**)
8. `AceitarConvite_MedicoComAgendamentoClinicoSobreposto_409PelaPoliticaUnificada`
9. `Checkin_Repetido_SegundaTentativaRetorna409EMantemUmRegistro`
10. `Checkin_Concorrente_DoisOperadores_SomenteUmEfetivadoESemDuplicarFila` (**corrida real**)
11. `Chamar_FilaConcorrente_DoisOperadores_SomenteUmaReservaESemHistoricoDuplicado` (**corrida real**)

Contratos de fonte:

12. `MigracaoV2341_MaterializaBloqueadoEVinculoEspecialidadeReferenciadosPeloCodigo`
13. `MigracaoV2342_MaterializaColunasDoHistoricoDeEscalaUsadasPorCodigoEPortal`
14. `MigracaoV2343_MaterializaColunasDoCicloDeVidaDoConviteOperacional`
15. `ConflitoCanônico_UnificaEscalasAgendaEIndisponibilidadeSemImplementacaoParalela`

## 6. Evidências de execução

- `dotnet build backend/PlantaoPro.sln` → **0 erro / 0 aviso**.
- `dotnet test` (suíte completa, `plantaopro_test`) → **1219/1219 aprovado**, 3 execuções seguidas.
- Migrações aplicadas e estrutura confirmada via `psql` (`BLOQUEADO_OK`, `TBL_OK:7`,
  `IDX_OK:ux_medico_especialidade_ativa`; colunas de `historico_escala` e `plantao_convites` presentes).
- Nota: o `StackOverflowException` isolado (1 execução, sem nome de teste no log) e o histórico da
  rodada anterior com 1 falha isolada não reproduzida indicam possível instabilidade ocasional do
  processo de testes; nenhuma assinatura de falha persistente identificada até aqui.

## 7. Pendências e próxima fatia

- **R7-E1 (i18n)**: as mensagens novas de 409 desta fatia são pt-BR hardcoded — entram no escopo
  de extração de strings quando a fundação i18n existir (pt-BR/en/es/fr, sem chave exposta).
- Decisão de produto pendente: solicitar (`solicitado`) segue **bloqueando** criação de consulta e
  aceite (coerente com o tratamento existente entre escalas); se o produto quiser apenas "alerta
  não impeditivo" para solicitações pendentes, é mudança de semântica registrada para deliberar.
- Upgrade do banco principal `plantaopro` (v2339→v2343) + IIS segue na fila externa.
- Homologação viva segue etapa separada (roteiro `r6a1b` §7), agora incluindo o ciclo completo
  convite → aceite → escala com as estruturas materializadas.
- Push do commit local diferido por política da rodada (aguarda sinal do usuário ou viaja com a
  fatia seguinte).
