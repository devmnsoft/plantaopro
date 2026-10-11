# R8-A0 — Varredura de evidência: §17–§18 (vínculos profissionais, credenciamento, elegibilidade, multi-unidade, recurso compartilhado)

Data: 2026-10-11 · Método: exploração read-only do HEAD `2f4533d3`. Caminhos relativos a `C:\MNSOFT\plantaopro\`.
Legenda: ESTADO ∈ {pronto com evidência, parcial, quebrado, ausente após busca, não verificado}.

---

## A1 — Tabela de vínculo profissional×tenant×unidade — **parcial**

**NÃO existe** tabela `medico_vinculos` / `profissional_vinculo` / `usuario_vinculo` (busca literal dos termos em todo o repo: 0 matches fora de comentários). O "vínculo" é materializado na própria tabela `medicos`:
- DDL legado: `database/scrpt_completo.sql:759-768` (`medicos` com `tenant_id`, `status`, `dados jsonb`) e `database/schema/030_operacao_plantoes.sql:25-34`.
- DDL histórico original: `database/PlantaoPro_PostgreSQL_Completo.sql:10` — inclui `usuario_id uuid references usuarios(id)` (identidade × profissional), `crm`, `uf_crm`, `cpf`.
- Tenant-scoping: `database/migrations/2026_09_v2200_reconciliar_colunas_medicos_compatibilidade.sql:16-37` adiciona `cliente_id`, `crm`, `uf_crm` etc.
- Bloqueio: `medicos.bloqueado boolean not null default false` em `database/migrations/2026_10_v2341_r7a1_conflitos_unificados_profissional.sql:7-8`.
- Checagem efetiva de "vínculo" no aceite de convite: `backend/PlantaoPro.Api/Data.cs:1488-1491` — `select exists(... from plantaopro.medicos where id=... and cliente_id=@clienteId and reg_status='A' and coalesce(bloqueado,false)=false and usuario_id=@userId and crm is not null and uf_crm is not null)` com mensagem **"Seu vínculo ou elegibilidade profissional mudou."**
- Criação de médico por admin **sem** `usuario_id`/`cliente_id`: `backend/PlantaoPro.Api/Data.cs:640`; seed dev liga usuário (`DevelopmentSeed.cs:237`).

OBS: Status disponível = apenas `ATIVO`/`reg_status='A'|'I'` + flag `bloqueado`. **Não existe** enum de status de vínculo "ativo/suspensa/encerrada", nem coluna `validade`, nem `responsavel_verificacao` (busca em todo repo: 0 matches). Sem coluna de unidade na `medicos` — o vínculo é profissional×tenant (via `cliente_id`); unidade só entra pelo plantão/hospital.

## A2 — Especialidades/habilitações na operação — **parcial**

- Criação: `database/migrations/2026_10_v2341_r7a1_conflitos_unificados_profissional.sql:10-23` — tabela `medico_especialidades` (id, tenant_id, cliente_id, medico_id, especialidade_id, reg_status) + índice único parcial `ux_medico_especialidade_ativa`. **Correção ao enunciado: criada em v2341, não v2342** (v2342 = colunas de `historico_escala`). Manifest: `migration-manifest.json:1211-1212`.
- Uso na elegibilidade: `BusinessRulesServices.cs:187-192` (`VerificarEspecialidadeCompatívelAsync`) → motivo bloqueante `"ESPECIALIDADE"` (:215).
- Convites/recomendações: score 80×20 por compatibilidade — `BusinessRulesServices.cs:308,312-313,254`; `ConvidarRecomendadosAsync` (:336-388) valida só `reg_status='A'` + `usuario_id` não nulo (:358-362) — elegibilidade completa só no aceite.
- Teste: `R7A1PlantoesConflitosUnificadosTests.MigracaoV2341_MaterializaBloqueadoEVinculoEspecialidadeReferenciadosPeloCodigo` (:413).

OBS — **Não há caminho de escrita produtivo** em `medico_especialidades`: única INSERT no código é o teste (`R7A1…Tests.cs:57`); nenhum seed, controller ou service insere/atualiza. Em banco real a tabela nasce vazia — lacuna operacional real (tornaria todo médico inelegível por especialidade se não populada manualmente).

## A3 — Suspensão do profissional — **parcial**

- Bloqueio de **novos** compromissos: sim, em todos os pontos de decisão — `VerificarStatusMedicoAsync` (`BusinessRulesServices.cs:202-206`) → motivo `MEDICO_INATIVO` ("Médico inativo, bloqueado ou suspenso.", :214); aplicado em aceite/confirmação/substituição e no aceite de convite (`Data.cs:1488-1491`); criação de consulta clínica (`Fase4OperationalServices.cs:417-421` e `Saude360ClinicalService.CriarAsync(agendamentos)` → 409 quando `bloqueado=true` — teste `CriarAgendamento_MedicoBloqueado_409`, `R7A1…Tests.cs:183`).
- **Sinalização de escalas FUTURAS existentes ao suspender: ausente após busca.** Buscas: `suspens|SUSPENSO|suspensa` (56 matches — todos em tenant/cliente/módulo/assinatura, nenhum em escala futura de médico); `sinalizad|SINALIZADO|alerta.*escala.*futur|escala.*suspenso` (3 matches irrelevantes).

OBS: Não existe endpoint/service que escreve `bloqueado=true` em `medicos` (busca `set bloqueado|m.bloqueado` → apenas leituras + seed de teste). A única suspensão efetiva por código é `InativarAsync` (`Data.cs:687-695`, `reg_status='I'`, auditada). Ou seja: "bloqueado" existe na regra, mas não tem origem administrativa no produto.

## A4 — Documentos do profissional com vencimento (CRM etc.) — **ausente após busca**

- Buscas: `responsavel_verificacao`, `medico_documentos`, `profissional_documentos`, `documento_vencimento` → **0 matches** no repo. `vencimento|data_vencimento` → só financeiro.
- O que existe: `medicos.crm varchar(20)` e `medicos.uf_crm char(2)` como campos de cadastro simples (`v2200:17-18`), exigidos como não-nulos no aceite de convite (`Data.cs:1489-1490`) — **sem vencimento, sem status, sem alerta/bloqueio**.
- Placeholder honesto: `MedicoElegibilityService.VerificarDocumentacaoAsync` retorna sempre `null` (`BusinessRulesServices.cs:207`) → motivo não-bloqueante fixo `DOCUMENTACAO_NAO_VERIFICADA` sempre presente (:220).
- Tabela genérica `documentos` existe mas é outra coisa (`database/20260525_evolucao_saas_premium.sql:56`); `paciente_documentos` é do paciente.
- Design v2.11.0 afirma explicitamente que status de credenciamento, documentação obrigatória e transições administrativas "continuam pendentes de implementação": `artifacts/functional/v2110-portal-profissional-credenciamento-disponibilidade-design.md:38-41`.

## A5 — Histórico/audit trail de aprovação-suspensão de vínculo — **parcial**

- Histórico de transições de escala com autor e data: `plantaopro.historico_escala` (`escala_id, status_anterior, status_novo, justificativa, usuario_id, reg_date`) — criada em `database/migrations/2026_10_v2342_r7a1_historico_escala_colunas.sql:7-16` (manifest :1222); escrita por `EscalaService.AddHistoricoAsync` (`Data.cs:1446,1512,1583,1605,1631,1668-1669`); leitura no portal profissional (`ProfessionalPortalService.cs:84`).
- Auditoria genérica (`IAuditService`): `ACEITAR_CONVITE` (`Data.cs:1517`), `CONVIDAR_RECOMENDADO` (`BusinessRulesServices.cs:375`), `EFETIVAR` substituição (`Fase4OperationalServices.cs:434`), inativação de médico (`Data.cs:692`).
- Teste de contrato das colunas: `MigracaoV2342_MaterializaColunasDoHistoricoDeEscalaUsadasPorCodigoEPortal` (:422).

OBS: Não há histórico específico de **aprovação/suspensão do próprio vínculo**. A trilha cobre escalas e inativação de cadastro, não ciclo de vida de credenciamento.

## A6 — Revalidação de elegibilidade no momento da ação — **pronto com evidência**

| Ação | file:line |
|---|---|
| Núcleo da regra | `MedicoElegibilidadeService.VerificarElegibilidadeParaPlantaoAsync` — `BusinessRulesServices.cs:153-172`; motivos em `ObterMotivosInelegibilidadeAsync` :209-222 (status, especialidade, conflito, indisponibilidade, limite semanal) |
| Aceite de plantão | `Data.cs:1396-1398` + conflito :1406-1415 |
| Aceite de convite operacional | `Data.cs:1485-1503` — idoneidade do vínculo (:1488-1491), política unificada de conflito (:1499-1500), revalidação de motivos (:1502-1503) dentro da transação c/ `pg_advisory_xact_lock` por médico (:1468) e `FOR UPDATE` do convite/plantão |
| Confirmação de escala | `Data.cs:1567-1569` |
| Substituição | `Data.cs:1643-1645` + conflito :1650-1652 |
| Efetivação de cobertura (Fase4) | `Fase4OperationalServices.cs:417-421` (Serializable tx + advisory lock :403,406) |
| Criação/reagendamento de agenda clínica | `Saude360ClinicalService` via `ConflitoHorarioService` (doc c7a1 §4; testes `CriarAgendamento_*` ×4 e `AtualizarAgendamento_MoverParaSobreEscalaConfirmada_409EPreservaHorarioOriginal` :110-255) |

Testes: `R7A1PlantoesConflitosUnificadosTests` (15 testes, :110-439), âncoras em `V2157OperationalCycleContractTests.cs:24`, `V2166CoverageJourneyContractTests`. Política única de conflito: `ConflitoHorarioService.ListarFontesConflitoAsync` (`BusinessRulesServices.cs:61-90`).

OBS — Convite **emitido** (`ConvidarRecomendadosAsync`) não roda a elegibilidade completa (só reg_status/usuario_id); a recomposição ocorre no aceite. "solicitado bloqueia" = decisão de produto pendente (c7a1 §7).

## A7 — Unidades compartilhadas / acesso multi-unidade (§18) — **parcial**

- Modelo multi-unidade no tenant: existe. Unidades = `hospitais` (per tenant) e `clinica_unidades_atendimento` / `clinica_salas_atendimento(unidade_id)` / `clinica_setores_atendimento(unidade_id)` — `database/migrations/2026_plantao_pro_saude360_base_clinica.sql:71-73`; escrita real de unidades (rota R5-E13, teste `OnboardingE13UnidadesContractTests`).
- Contexto de tenant na sessão: claims `uid/sub, role(s), access_scope, context_mode, cliente_id, tenant_id, permissions, module...` (`SessionClaimsBuilder.cs:70-93,117-126`; JWT da API `Data.cs:553-559`). **Não há nenhuma claim de unidade/hospital.**
- Enforcement server-side de "unidade" = guarda de **tenant** sobre a entidade: `TenantGuardService.PodeAcessarHospitalAsync` → `PodeAcessarEntidadePorClienteAsync(usuarioId, hospitalId, "hospitais","id")` (`TenantServices.cs:95-98`) — hospital pertence ao `cliente_id` da sessão (global admin passa sempre). Usado em `Fase4OperationalController.cs:111,123-124,141-142` (mensagem "Você não possui vínculo com a unidade desta solicitação."), `ExecutionConferenceService.cs:39,132,196`, `OperationalReportService.cs:36` (teste `ManagerUnitDecisionJourneyTests.cs:24`).
- Propagação do contexto de unidade nas telas operacionais: via parâmetro de query/rota filtrado server-side — `UnitPortalServices.cs:20-50`, `ConsultasWorkspaceController.cs:15`, `CentralEscalaController.cs:53-63`, `ConferenciaExecucaoController.cs:14-36`.

OBS — Um usuário opera em **todas** as unidades do tenant: não há mapeamento usuário×unidade (busca `usuario_unidade|user_unidade|unidade_usuario|usuario_hospital` → 0 matches). A restrição granular multi-unidade por pessoa está ausente; a mensagem de "vínculo com a unidade" é sustentada apenas pela guarda de tenant.

## A8 — Recurso compartilhado (sala/guichê/setor) + conflitos de reserva — **parcial** (tabelas prontas; regra de conflito entre reservas incompatíveis na mesma sala: ausente após busca)

- Tabelas: `clinica_salas_atendimento`, `clinica_setores_atendimento`, `clinica_unidades_atendimento` (`base_clinica.sql:71-73`); `painel_chamada_setores/salas/guiches`, `painel_chamada_fila(setor_id, sala_id, guiche_id, senha, ...)` (`base_clinica.sql:44-45`; índices `modulos_clinicos.sql:208-229`); `agendamentos(unidade_id, sala_id, ...)` (`base_clinica.sql:48`); contrato de existência: `Saude360BaseClinicaContractTests.cs:11`.
- Regras de conflito: **apenas por profissional**, nunca por sala — `ConflitoHorarioService.ListarFontesConflitoAsync` cobre escalas + agendamentos + indisponibilidades pelo `medico_id` (`BusinessRulesServices.cs:61-90`).
- Ausência de trava de reserva exclusiva de sala: índices de `agendamentos` (`ix_agendamentos_periodo on (cliente_id, medico_id, data_inicio, data_fim)` — `base_clinica.sql:105`) — **nenhum** índice/constraint com `sala_id` nem único parcial anti-double-booking. Buscas `unique.*sala|conflito.*sala|sala.*conflito` → 0 regras.
- A única "reserva concorrente" tratada com prova é a de **item de fila do painel de chamada** (R7-A1; teste `Chamar_FilaConcorrente_DoisOperadores_SomenteUmaReservaESemHistoricoDuplicado` :384) — não é reserva temporal de sala/guichê.
- Mapas internos registram a limitação: `docs/evidencias/2026-10-06-rodada-3/mapas/mapa-saude360.md:13,36`.

---

## ÁREA B — Termo "credenciamento" — **ausente após busca** (como domínio profissional); **parcial** (como integração fiscal)

Buscas `credenciamento|credenciad|Credencia` (repo inteiro) + variações em `database/` e `.cshtml`:
1. **Domínio profissional/CRM**: 0 tabelas, 0 endpoints, 0 colunas. Único rastro: descrição de capacidade em `database/migrations/2026_10_v2339_r6a_granularidade_saude360_pacote_capacidades.sql:71` ("credenciamento e guias de convenios"). Design v2.11.0 documenta que painel administrativo de credenciamento nunca foi implementado; nada posterior cria essa entidade (Web: 0 matches).
2. **Integrações fiscais/externas**: "credenciais" como segredos — `CobrancaService.cs:45-72`, `FiscalPreEmissoes.cs:172-185`, `Administrativo360/PortalConnectors.cs:17,73`; teste `Aceite11_SemCredenciais_IntegracaoFicaBloqueadaENaoRetornaSucessoFicticio` (`Administrativo360CotacoesXmlDashboardTests.cs:384`).
3. `credenciais` no sentido de login: seeds/reset — não confundir com credenciamento profissional.

**Resumo:** o que existe hoje de "elegibilidade de profissional" é o conjunto R7-A1 (v2341-v2343 + `MedicoElegibilityService`/`ConflitoHorarioService`) — status/inatividade/bloqueio + especialidade + conflito + disponibilidade + limite semanal. O "credenciamento" formal (documentos com vencimento, aprovação/suspensão de vínculo com responsável, restrições por unidade) continua essencialmente ausente.
