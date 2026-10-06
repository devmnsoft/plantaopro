# R4-B8 — Saúde 360: financeiro canônico em `clinica_*` (baixa, estorno, caixa) e pré-autorização na finalização

Data: 2026-10-06 · Branch: `main` · Base: `a97a8cd` (R4-B7) · Banco: `plantaopro_test`

## 1. Escopo e decisões

O item B8 pede: fonte canônica do financeiro (com matriz de responsabilidades documentada),
baixa de recebimento (jornada paciente→caixa com parcelas), estorno, fechar-caixa e
pré-autorização (bloqueio operacional explícito, sem decisão médica automática).

Decisões adotadas nesta rodada:

1. **Fonte canônica** — para a jornada clínica (consulta → conta a receber → recebimento →
   caixa/lançamentos/fechamento → estorno), `clinica_*` é a fonte canônica do financeiro.
   As camadas v115/v116 permanecem módulos de **consolidação** com origem própria (faturamento
   em lote, relatórios, integração) e **não recebem escritas cruzadas** desta jornada — sem
   módulos paralelos. A responsabilidade por operação está documentada no cabeçalho da
   migração v2315, no cabeçalho da classe `Saude360FinanceiroService` e nesta evidência
   (§2, matriz origem×operação).
2. **Baixa de recebimento** (`BaixarRecebimentoAsync`) — transação única: lock da conta
   (`FOR UPDATE`) → guards (404 inexistente; 409 já quitada/pendente ≤ 0; 409 cancelada;
   400 excede o saldo pendente com valor formatado; 400 falta conta/valor/forma/data) →
   caixa: o informado é validado (se não estiver `ABERTO`, 409) ou, quando ausente, liga-se
   ao mais recente `ABERTO` do tenant — se não existe nenhum aberto, a baixa **segue
   confirmada** com `CaixaVinculado=false` e mensagem honesta ("Nenhum caixa aberto...") em
   vez de falhar ou inventar caixa. Grava `clinica_recebimentos` CONFIRMADO (paciente da
   conta; comprovante = documento alternativo) + atualiza a conta com **predicado do valor
   pendente esperado** (≠1 linha → 409 "Conflito: a conta foi alterada por outra ação
   simultânea...") + lançamentos ENTRADA/totais/`saldo_final` do caixa + histórico
   `BAIXA_RECEBIMENTO` + auditoria.
3. **Estorno** (`EstornarRecebimentoAsync`) — transação única sobre o recebimento: só estado
   CONFIRMADO (senão 409 "O recebimento já foi estornado ou está em outro estado."); grava
   status ESTORNADO + `estornado_em/por` + `justificativa_estorno` (motivo truncado a
   2000) + linha em `clinica_estornos` + **reversão da conta** (`greatest(...,0)` como piso
   em zero; RECEBIDO reabre como `VENCIDA` se vencida, senão `ABERTA`) + caixa **só se
   aberta** (fechado → `CaixaRevertida=false` + aviso "concilie manualmente", total não é
   reaberto) + lançamento SAÍDA + histórico ESTORNO. Motivo obrigatório (400 "A ação exige
   motivo ou justificativa.").
4. **Fechar-caixa** (`FecharCaixaAsync`) — transação única: lock; já FECHADO → 200
   idempotente ("O caixa já estava fechado." + `JaviaFechado=true`); senão
   `saldo = round(saldo_inicial + entradas − saídas, 2)` gravado com predicado ABERTO
   (conflito → 409) + `saldo_informado=@saldo, diferenca=0` + `clinica_fechamentos_caixa`
   com observação honesta **"sem contagem física nesta versão"** (não há conferência
   física nesta entrega — o valor informado é o computado, não o contado) + histórico
   `FECHAMENTO_CAIXA`.
5. **Pré-autorização** (gate em `FinalizarAsync`) — após as pendências impeditivas e antes
   de qualquer escrita, **somente quando a finalização gera faturamento por CONVENIO ou
   PLANO_SAUDE**: consulta `bool_or` sobre `convenio_autorizacoes` ativas do tenant ligadas
   por `consulta_id`, senão por `agendamento_id`, senão por `paciente_id` — prioridade
   PENDENTE > NEGADA > livre. PENDENTE/NEGADA → 409 com mensagem canônica ("aguarde a
   decisão (aprovar/negar)..."/"revise a necessidade ou registre nova autorização...");
   APROVADA ou sem registros → segue. **Sem auto-aprovação**: o sistema nunca decide pela
   médica/o médico; PARTICULAR/CORTESIA/ISENTO não passam pelo gate. O gate roda no
   snapshot da transação (early return antes das escritas — nada é gravado ao bloquear).
   `PendenciasAsync` passa a exibir alerta correspondente (não impeditivo) via
   `SituacaoPreAutorizacaoAsync` (novo método em `IConsultaRepository`).
6. **Valores operacionais canônicos da conta** — o insert de `FinalizarAsync` agora preenche
   `valor_total` **e** `valor_pendente` com o líquido (bruto − desconto + coparticipação);
   backfill idempotente na v2315 para linhas legadas zeradas (atualizou 0 linhas no vivo —
   seeds já preenchiam).
7. **Pagamentos parciais** — removido o índice único legado
   `ux_recebimento_conta_confirmado` (cliente×conta WHERE CONFIRMADO), que impedia a 2ª
   parcela. A proteção contra excesso passa a ser **transacional**: lock + validação
   `valor ≤ pendente` + predicado do pendente esperado (corrida → 409 + rollback total).
   Índices novos: `ix_clinica_recebimentos_cliente_conta_status`,
   `ix_clinica_lancamentos_cliente_caixa`, `ix_clinica_estornos_recebimento`,
   `ix_convenio_autorizacoes_cliente_consulta`.
8. **Wiring** — `ClinicaFinanceiroController.Receber/Estornar/FecharCaixa` passam a chamar os
   métodos canônicos (mesmas URLs/shapes/roles/atributos; `HttpContext.RequestAborted`;
   resposta recomposta via `ObterAsync` do registro afetado; `cancelar` segue genérico).
   DI: `AddScoped<Saude360FinanceiroService>()`.

### Matriz origem × operação (fonte canônica)

| Operação | Escritor canônico | Camada | Escopo tenant |
|---|---|---|---|
| Conta a receber (origem CONSULTA) | `ConsultaApplicationService.FinalizarAsync` | clínica | `cliente_id=@tenant`, `tenant_id=NULL` |
| Conta a receber (origem MANUAL/DEMO) | CRUD `Saude360ClinicalService` | clínica | idem |
| Recebimento + baixa (conta/caixa/lançamento/histórico) | `Saude360FinanceiroService.BaixarRecebimentoAsync` | clínica | idem (leituras `coalesce(tenant_id, cliente_id)`) |
| Estorno (recebimento/estornos/conta/caixa/histórico) | `Saude360FinanceiroService.EstornarRecebimentoAsync` | clínica | idem |
| Caixa: abrir/fechar, `clinica_fechamentos_caixa` | `Saude360FinanceiroService.FecharCaixaAsync` (abrir via CRUD existente) | clínica | idem |
| Consolidação em lote, relatórios, exportação | v115/v116 (tabelas próprias) | consolidação | idem — **sem cross-writes** desta jornada |
| Pré-autorização (leitura p/ gate/alerta) | `convenio_autorizacoes` (CRUD existente) | convênios | idem |

## 2. Implementação

| Ponto | Arquivo | Mudança |
|---|---|---|
| B8-schema-1 | `database/migrations/2026_10_v2315_saude360_b8_financeiro_canonico.sql`, `database/migration-manifest.json` | backfill de valores operacionais + drop do único legado + 4 índices (aplicada/verificada em test; checksum `dd5a5e4d…`) |
| B8-schema-2 | `database/migrations/2026_10_v2316_saude360_b8_fila_atendimento_legado.sql`, `database/migration-manifest.json` | **novo** — cria a tabela legada `fila_atendimento` que nenhuma DDL do repo criava (root cause §3.2; checksum `cc22f083…`) |
| B8-serviço | `backend/PlantaoPro.Api/Saude360FinanceiroService.cs` | **novo** — Baixar/Estornar/FecharCaixa canônicos + header de responsabilidades |
| B8-api | `backend/PlantaoPro.Api/Controllers/Saude360ClinicalControllers.cs` | `ClinicaFinanceiroController` repontado p/ o serviço (3 endpoints; `cancelar` intacto) |
| B8-clínica | `backend/PlantaoPro.Api/Clinical/ConsultaApplicationService.cs` | gate de pré-autorização em `FinalizarAsync`; insert de conta unificado (`valor_total/valor_pendente`); alerta em `PendenciasAsync`; `SituacaoPreAutorizacaoAsync` na interface + `ConsultaRepository` |
| B8-di | `backend/PlantaoPro.Api/Program.cs` | `AddScoped<Saude360FinanceiroService>()` |
| B8-tests | `backend/PlantaoPro.Tests/Saude360R4B8FinanceiroCanonicidadeTests.cs` | **novo** — 15 fatos T01–T15 (tenant GUID próprio por teste; cleanup escopado em `finally`; fakes `B8FakeUser`/`B8FakeAudit`) |

## 3. Defeitos fechados nesta rodada

### 3.1 v2315 aplicada e verificada em `plantaopro_test`

Backfills executados (0 linhas alteradas no vivo — seeds legadas já preenchiam), índice único
`ux_recebimento_conta_confirmado` derrubado, 4 índices criados, linha em `schema_migrations`
com checksum `dd5a5e4d32b8ab1d9b23b9b4845dfbfd1e0772f2e6634db433f118c0f1f155d2`. Quirk do
runner confirmado: nomes de índice **sem** qualificação de schema (`CREATE INDEX` não
admite; o `set search_path` posiciona), tabelas continuam qualificadas. Warning pré-existente
do runner inalterado: `IDENTITY_SCHEMA_INCOMPLETE MISSING_ADMIN_ROLE_LINK count=1` (defeito
de dado catalogado desde o B6).

### 3.2 `fila_atendimento`: UPDATE em tabela que nenhuma DDL do repo cria (v2316)

Root cause: `FinalizarAsync` (introduzido em `5406e514`, prontuário/faturamento v1.27.0)
faz, dentro da transação de finalização, `update plantaopro.fila_atendimento …` — mas
nenhuma migração/SQL do repositório cria essa tabela (verificado por varredura completa de
DDL e histórico git; a documentação da rodada 3 a lista entre as tabelas principais do
módulo). Sem ela, **toda** finalização via workspace falhava com `42P01` nesses bancos —
jornada consulta→conta→caixa interrompida antes mesmo do B8 existir. Nunca apareceu porque
nenhum teste integrativo exercitava `FinalizarAsync` (os testes de finalização pré-existentes
usam o caminho genérico `AcaoAsync("consultas","finalizar")`). Os fatos T11–T15 pegaram o
defeito na primeira execução. Correção: **v2316** — `CREATE TABLE IF NOT EXISTS` idempotente
(colunas exatas que o código usa + convenções do módulo) + índice de apoio, preservando o
código (nenhum escritor insere nesta versão; projeção legada de fila — o UPDATE apenas
marcaria `FINALIZADO` onde houvesse linhas). Aplicada em test; em `plantaopro` entra na
janela A2/F3.

### 3.3 Normalização de valores operacionais da conta

Contas geradas por finalização gravavam só `valor_bruto/desconto/coparticipacao/
valor_liquido` com `valor_pendente=0` — o par operacional que a baixa consome
(`valor_total/valor_pendente`) ficava zerado. Correção dupla: insert unificado em
`FinalizarAsync` + backfill idempotente na v2315.

## 4. Evidência de execução

### 4.1 Matriz T01–T15 (suíte viva contra `plantaopro_test`)

| Fato | Cenário | Observado |
|---|---|---|
| `B8/T01` | baixa parcial 50/100 c/ caixa aberto → conta paga 50/pendente 50/ABERTA; recebimento CONFIRMADO ligado ao caixa (paciente da conta); caixa entradas+50/saldo 150; 1 lançamento ENTRADA; 1 histórico BAIXA_RECEBIMENTO | ✅ passa |
| `B8/T02` | baixa integral → RECEBIDO quitada; 2ª baixa → 409 "já está quitada"; exatamente 1 recebimento CONFIRMADO | ✅ passa |
| `B8/T03` | validações: 404 inexistente/fora-do-tenant; 400 excede pendente (msg c/ valor formatado); 400 falta forma+data; 409 cancelada; 403 sem organização | ✅ passa |
| `B8/T04` | baixa sem caixa aberto → sucesso `CaixaVinculado=false` (caixa NULL no recebimento, 0 lançamentos) + mensagem honesta; conta paga | ✅ passa |
| `B8/T05` | corrida: duas baixas simultâneas na mesma conta → exatamente 1×200 + 1×409; conta RECEBIDO/pendente 0; 1 recebimento (lock FOR UPDATE + predicado do pendente esperado) | ✅ passa |
| `B8/T06` | estorno da parcial → recebimento ESTORNADO c/ estornado_em+justificativa; linha `clinica_estornos` 50; conta revertida (pago 0/pendente 100/ABERTA); caixa revertido (entradas 0/saldo 100); lançamentos [ENTRADA, SAÍDA]; 2º estorno → 409 "já foi estornado" | ✅ passa |
| `B8/T07` | 2 parcelas CONFIRMADAS na mesma conta (índice único legado fora do caminho); estorno sem motivo → 400; c/ motivo → conta pendente 70/pago 30 | ✅ passa |
| `B8/T08` | estorno de quitada vencida → conta reabre `VENCIDA`; quitada futura → reabre `ABERTA` | ✅ passa |
| `B8/T09` | baixa com caixa explicitamente FECHADO → 409 "não está aberto"; estorno pós-fechamento → `CaixaRevertida=false` + aviso "concilie manualmente"; total de entradas do caixa congelado (50), status FECHADO | ✅ passa |
| `B8/T10` | fechar-caixa → FECHADO c/ saldo computado 150, `saldo_informado=150/diferenca=0`, fechado_em; linha `clinica_fechamentos_caixa` (informado 150/diferenca 0/"sem contagem física"); 2º fechamento → 200 idempotente `JaviaFechado=true` | ✅ passa |
| `B8/T11` | convênio + autorização PENDENTE → 409 "Pré-autorização pendente…aprovar/negar"; consulta intacta (EM_ATENDIMENTO v1), 0 contas gravadas (rollback total) | ✅ passa |
| `B8/T12` | convênio + NEGADA → 409 "Pré-autorização negada…"; aprova → mesma chamada 200; conta EM_ANALISE origem CONSULTA total=pendente=líquido=200; consulta FINALIZADA v2 | ✅ passa |
| `B8/T13` | convênio sem autorização → finaliza (gate livre); líquido 185 = 200−20+5 com total=pendente=185 (normalização §3.3); fila FINALIZADO; jornada fim-a-fim: baixa 185 da conta gerada → RECEBIDO | ✅ passa |
| `B8/T14` | PARTICULAR c/ autorização PENDENTE → finaliza (gate só convênio/plano); conta ABERTA 150; CORTESIA sem justificativa → 400; CORTESIA c/ justificativa → finaliza sem financeiro (0 contas) mesmo com autorizações pendentes | ✅ passa |
| `B8/T15` | `PendenciasAsync` c/ PENDENTE → alerta "Pré-autorização pendente para este atendimento…" sem impedir (Impeditivas vazia, PodeFinalizar=true) | ✅ passa |

Metodologia: harness no estilo dos S-fixes (fakes `ICurrentUserService`/`IAuditService`,
`ConfigurationBuilder` in-memory c/ `TestDatabase.ConnectionString`); tenant GUID próprio por
teste (escritores usam `cliente_id=@tenant`, `tenant_id=NULL` — convenção canônica);
seeds mínimos pelas colunas reais (verificado via `information_schema`); limpeza escopada
por `cliente_id=@tenant` em `finally` (12 tabelas, filhos primeiro). A primeira execução da
classe falhou em 5/15 **por um seed** (`medico_id` não existe em `atendimentos_fila`) —
corrigido; foi essa execução que expôs o root cause §3.2 (tabela `fila_atendimento`
ausente).

### 4.2 Suíte canônica integral

Seis execuções consecutivas de `dotnet test backend\PlantaoPro.Tests\PlantaoPro.Tests.csproj
--nologo -v q` contra `plantaopro_test` (requisito ×3 atendido em dobro):

| Rodada | Resultado |
|---|---|
| 1 | **1011/1011 aprovado**, 0 falhas |
| 2 | 1010/1011 — 1 falha: `Administrativo360CotacoesXmlDashboardTests.Aceite17_PersistenciaDuravel_PreservaRegistrosEFila` (flake, §5.1) |
| 3 | **1011/1011 aprovado**, 0 falhas |
| 4 | 1010/1011 — mesma falha Aceite17 (flake, §5.1) |
| 5 | **1011/1011 aprovado**, 0 falhas |
| 6 | **1011/1011 aprovado**, 0 falhas |

Baseline no B7: 996. Diferença **+15** = exatamente os novos fatos T01–T15.

### 4.3 Build

Solução completa: 0 erros. API: 12 warnings de estilo/nullabilidade, dos quais 4 em arquivos
tocados pelo B8 (3× CS8604 no fallback de reconstituição do controller + 1× CS8629 no
serviço) — cosméticos, documentados em §5. Os demais são pré-existentes e inalterados.

## 5. Classificação e limitações

**Classificação B8:**

| Item | Classificação |
|---|---|
| Jornada financeira canônica (baixa/estorno/caixa) + pré-autorização + v2315/v2316 + 15 fatos | **APROVADO** — evidência de execução completa (15/15 + suíte 1011 em 4/6 rodadas integrais), pronto tecnicamente |
| Aceite funcional da jornada no IIS vivo (UI → caixa real) | pendente de homologação (P0–P7); caminho genérico `AcaoAsync("consultas","finalizar")` não leva o gate — a finalização da UI do workspace usa `/finalizar` → `FinalizarAsync` (gated); se o negócio exigir o gate também no genérico, requer decisão explícita |
| Aplicação de v2315/v2316 (e v2312–v2314) no `plantaopro` (prod local) | **PENDENTE JANELA A2/F3** |

**Limitações honestas (backlog):**

1. **Flake ADM360 sob carga (novamente observado):** `Aceite17_PersistenciaDuravel_PreservaRegistrosEFila`
   falhou em 2/6 execuções integrais e 0/6 isolado. Mecanismo: o teste compara **dois
   snapshots não sincronizados** de `ListarCotacoesAsync` (que executa uma escrita —
   `MarcarExpiradasPrazoExcedidoAsync` — antes do SELECT) contra escritores paralelos da
   coleção `A360Transmissao`; qualquer inserção/expiração entre os dois reads muda o
   count/ordem. Fora do escopo B8 (ADM360, seed `TenantSantaCasa`). Correção proposta:
   snapshot único ou transação SERIALIZABLE com retry delimitado no teste. Junto do flake
   B6 §5.5, continua sob observação.
2. **`fila_atendimento` segue sem escritor** (v2316 a materializou p/ a jornada; quem — se
   houver — alimenta a projeção de fila é decisão futura de produto/operacional).
3. **4 warnings de nullabilidade nos arquivos B8** (§4.3) — cosméticos; tratar na próxima
   oportunidade de toque.
4. **Auditoria DateOnly completa (Npgsql 10)** segue aberta desde o B6 (cast `::timestamp`
   nas leituras novas; risco concentra-se em outros módulos).
5. **Sem promessa de ausência absoluta de bugs.** A suíte cobre os cenários acima contra
   `plantaopro_test`; caminhos sem teste vivo continuam sujeitos a defeito latente.

**Fora do escopo B8 (estado geral da entrega, sem mudança nesta rodada):** B9
(Plantões/Meu Dia/BI), B10 (IA — segue BLOQUEADO por credencial real), C11 (design),
emissão fiscal (segue BLOQUEADA por decisão comercial), IIS vivo P0–P7. Push do
repositório continua condicionado a decisão explícita (este commit deixa o HEAD 9 commits
à frente de `origin/main` = `c84afc5`).
