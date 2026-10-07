# R4-B9 — Saúde 360: plantões até encerrado/contestação, Meu Dia real, BI por competência/fuso/escopo

Data: 2026-10-07 · Branch: `main` · Base: `628d311` (R4-B8) · Banco: `plantaopro_test`

## 1. Escopo e decisões

O item B9 do plano de homologação pede: cadeia de plantão completa até `encerrado`
(terminal alcançável via API) e até **contestação**; Meu Dia **real e explicável**;
indicadores respeitando **filtros/fuso/escopo**, diferenciando filtrado de acumulado.

Decisões adotadas nesta rodada:

1. **Encerramento** (`PlantaoService.EncerrarAsync` + rota `POST /plantoes/{id}/encerrar`)
   — guarda de estado em ordem: 404 "Plantão não encontrado" (sem ponto, âncora
   original), 409 "Somente plantão realizado pode ser encerrado.", 409 "Conclua o
   fechamento financeiro antes de encerrar o plantão.", idempotência 200 "O plantão já
   estava encerrado.". Estado terminal alcançável pela API com fechamento anterior.
2. **Contestação de pagamento** (`ContestarAsync`/`ResolverAsync`) — passa a gravar
   `situacao_anterior` em `pagamento_contestacoes` (v2317), permitindo resolução honesta
   (aprovar/estornar/cancelar) e diagnóstico de legado sem situação anterior (409 "Contestação
   sem situação anterior registrada; resolva por manutenção de dados."). Pagamento PAGO
   permanece PAGO na contestação (apenas anota a contestação, sem mudar valor/versão).
   Duplicada: a guarda de status só aceita pendente/aprovado/pago; a unicidade de uma
   contestação ABERTA por pagamento é garantida pela constraint única do banco
   (23505 → 409 "Já existe contestação aberta para este pagamento.").
3. **Estorno** (`EstornarPagamentoAsync` + `POST /pagamentos/{id}/estorno`) — só registrado
   (409 "Somente pagamento registrado pode ser estornado."), motivo ≥ 10 caracteres
   (400), bloqueio por contestação aberta ("Resolva a contestação aberta antes de estornar
   o pagamento."), reversão pago→aprovado zerando dados do recebimento, ok idempotente com
   `(id,"aprovado",0m,null,"estorno")`.
4. **Meu Dia real** (`GetAgendaAsync` + `day-item-reason` na view) — itens com razão
   explícita (PriorityReason); divergência de fechamento bloqueia com motivo canônico
   ("Fechamento com divergência aberta; resolva antes de prosseguir.").
5. **BI por competência/fuso/escopo** — competência fixada em `yyyy-MM`; fuso validado via
   `pg_timezone_names` (400 "Fuso horário invalido.") com default
   `current_setting('TimeZone')`; escopo global (admin) vs tenant; **acumulado vs período
   diferidos explicitamente** (`EscalasConfirmadas` acumulado × `EscalasConfirmadasPeriodo`
   da competência) e 'contestado' **fora** dos buckets de pendentes/confirmados.
6. **Dashboards premium + API absoluta/escopo** — scoping `(@tid is null or tenant_id=@tid or
   cliente_id=@tid)` nos dashboards premium; `DashboardService.GetAsync` com absoluto
   global e delta por tenant; notificações não lidas por usuário.
7. **Relatórios por `data_negocio`** — competência por primeira data real do registro
   (`data_inicio > data_prevista > data_pagamento > aberto_em > reg_date`), extraída via
   `to_jsonb` com guarda de formato, fronteiras em dia (DateOnly), sem interpretação de fuso
   nas bordas do período. **Dimensão ausente na tabela ⇒ resultado vazio** (comportamento
   documentado, não acidental): `pagamentos` não tem `hospital_id/especialidade_id/
   convenio_id`, então esses filtros retornam vazio para PAGAMENTOS_MEDICOS.

## 2. Migrações (correções de bugs latentes de defasagem schema object-catalog)

O layout de schema `database/schema/030_operacao_plantoes.sql` deixou quatro tabelas com
definição de **catálogo genérico** (id/nome/descricao) em vez da instância usada pelo
código — bug latente de produção, não apenas de teste:

| Migração | Conteúdo |
|---|---|
| v2317 | `situacao_anterior varchar(32)` em `pagamento_contestacoes` (resolução honesta) |
| v2318 | `plantao_historico` era catálogo genérico → layout de instância |
| v2319 | `notificacoes` era catálogo genérico → instância (`usuario_id, titulo, mensagem, tipo, lida, created_by, updated_by, reg_date, reg_update, reg_status`) + `idx_notificacoes_usuario_lida`; linhas de catálogo intactas (usuario_id null → filtradas) |
| v2320 | auditoria em `plantoes`: `updated_by uuid`, `reg_update timestamptz`, `conflito_detectado boolean not null default false` |

Checksums no manifest: v2318 `8a0f882b…1074`, v2319 `baca5925…706a`, v2320 `d9eec352…503f7`.
**Sem v2319, todo fluxo que cria notificação quebrava com 42703** (escala aceitar/recusar/
confirmar/cancelar/substituir/realizar/não-comparecimento, pagamento gerar/confirmar/
registrar/contestar, dashboard) — B8 não exercitava esses caminhos. `historico_escala` tem a
mesma defasagem → backlog (próxima migração livre: v2321).

## 3. Bugs reais corrigidos durante esta rodada (fora do escopo planejado, evidenciados pelos testes)

1. **`TotalClientesAtivos` sempre 0** — a BI filtrava `c.status='ativo'`, mas
   `ck_clientes_status_saas` só aceita MAIÚSCULAS (`TESTE/ATIVO/SUSPENSO/CANCELADO/INATIVO`
   e os dados usam `'ATIVO'`). Corrigido para `upper(c.status)='ATIVO'` em
   `BiServices.cs` (bug real de produção).
2. **Reports: `String.Replace` aplicado ao fragmento errado** — em `ReportQueryService.Sql`
   o `.Replace("@code", …)` da concatenação valia **somente para o último fragmento**
   (chamada de método tem precedência sobre `+`), então `@code` chegava intacto ao banco e
   o PostgreSQL interpretava o `@` como operador de prefixo → 42703 coluna "code". `code`
   virou parâmetro Dapper real.
3. **Seeds de `date` via DateTime perdiam um dia** — session TZ do banco é
   `America/Sao_Paulo`: `DateTime` meia-noite UTC enviado a coluna `date` via timestamptz
   fazia o cast `::date` retroceder um dia (repro controlada confirmou
   `dn=2026-10-01` com seed de `2026-10-02`). Seeds passam a enviar data nativa
   (`yyyy-MM-dd` + cast `::date`). Lição documentada: para colunas `date`, seeds devem
   enviar o dia como string, nunca `DateTime` UTC.
4. **Status de catálogo em seeds** — `clientes.status` e `assinaturas.status` exigem
   MAIÚSCULAS pelas constraints `*_saas`; seeds ajustados (`ATIVO`/`ATIVA`).
5. **T10 (duplicata de contestação) redesenhado** — chamar `ContestarAsync` duas vezes nunca
   atinge o 23505: a segunda cai na guarda de status (apos contestar, status sai de
   pendente/aprovado/pago). O teste agora seeda a linha ABERTA direto na tabela com o
   pagamento ainda `pendente`, de modo que a guarda de status passe e a constraint única
   responda.

## 4. Âncoras e contratos preservados

- Mensagens exatas de encerrar/contestar/resolver/estorno mantidas idênticas (lista completa
  na seção de contratos do plano D1); 404 de encerrar **sem ponto final**.
- `ck_v2158_*` respeitados nos seeds (`origem_pagamento='MANUAL'`, 'pago' exige
  `valor_pago=valor_aprovado>0`); suite B8/V186/V2158 seguem verdes sobre o código modificado.
- T34 ancora: rotas `{id:guid}/encerrar|realizar|estorno`, `EncerrarAsync`, mensagens
  canônicas em `Data.cs`, `pg_timezone_names`+`valor_contratado` em `BiServices.cs`,
  `@inicioDia::date`/guarda `[0-9]` em `ReportServices.cs`, `day-item-reason` (Meu Dia) e
  `bi-periodo-fuso`/`escalasConfirmadasPeriodo` (BI), presença dos arquivos v2317–v2320 e dos
  checksums no manifest.
- `DateOnlyDapperTypeHandler` é idempotente e chamado APENAS em `Program.cs`; testes que
  constroem serviços diretamente precisam de `DapperTypeHandlerRegistrar.RegistrarTodos()`
  (static ctor da suite) — sem isso: `NotSupportedException: The member inicioDia of type
  System.DateOnly cannot be used as a parameter value`.

## 5. Resultado

| Execução | Resultado |
|---|---|
| Build `backend\PlantaoPro.sln` | 0 erros |
| Classe B9 (`Saude360R4B9PlantoesMeuDiaBiTests`, 32 fatos) | **32/32 verde** |
| Suíte completa — rodada 1 | **1043/1043 verde** |
| Suíte completa — rodada 2 | **1043/1043 verde** |
| Suíte completa — rodada 3 | **1043/1043 verde** |

Baseline anterior (B8): 1011 testes; delta +32 = suite B9. O flake conhecido
`Administrativo360CotacoesXmlDashboardTests.Aceite17…` **não caiu** nas três rodadas.
Upgrade aplicado em `plantaopro_test`: v2317–v2320 (`IDENTITY_SCHEMA_INCOMPLETE
MISSING_ADMIN_ROLE_LINK count=1` = backlog conhecido, pré-existente).

## 6. Desvios documentados (não são bugs desta entrega)

- Filtro pedido em dimensão ausente na tabela de origem ⇒ relatório vazio (documentado em
  §1.7; âncoras negativas no T30 da suite).
- `pagamentos` sem `hospital_id/especialidade_id/convenio_id` (layout V2158) — dimensões
  inexistentes para relatórios financeiros de plantão.
- `bi-periodo-fuso` default segue o fuso do servidor (`current_setting('TimeZone')`).
- 'contestado' não entra em `PagamentosPendentes` nem `PagamentosConfirmados` (estado
  distinto por design de disputa).
- `historico_escala` com a mesma defasagem de catálogo → próxima migração (v2321).
