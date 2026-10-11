# R8-A0 — Varredura de evidência: §22–§26 (convênios/glosas, pacientes, importações, recuperação de falhas, notificações)

Data: 2026-11-10/11 · Método: exploração read-only do HEAD `2f4533d3` (código + migrations + testes).
Caminhos relativos a `C:\MNSOFT\plantaopro`. Classificações: pronto com evidência / parcial / quebrado / ausente após busca / não verificado.

---

## Área A — Convênios: faturamento e glosas (§22)

### A1. Faturamento a convênio end-to-end (lote, envio, retorno, recebimento, glosa, recurso)
- **ESTADO:** parcial
- **EVIDÊNCIA:**
  - Lotes de faturamento genéricos v1.16: `backend/PlantaoPro.Api/V116ConsolidacaoServices.cs:187` (`V116LoteFaturamentoService`: criar/fechar/reabrir lote, adicionar/remover item, status `ABERTO → FECHADO → ENVIADO_DEMO → RETORNO_DEMO`); tabelas `v116_faturamento_lotes`, `v116_faturamento_lote_itens`, `v116_convenio_guias` em `database/migrations/2026_v116_consolidacao_operacional_final.sql:37,101`; seed demo `database/seeds/2026_demo_v116_consolidacao_operacional.sql:15-19`.
  - Geração de conta a receber p/ atendimento com regra de convênio: `V115FaturamentoRegraService.cs:58-59` (`GerarContaAsync`, regra em `v115_regras_faturamento`); rota `POST api/v115/faturamento/gerar-conta-consulta/{id}` em `backend/PlantaoPro.Api/Controllers/V115FaturamentoController.cs:18`; item faturável → conta: `V114ProdutoService.cs:91,103-112`.
  - Cobrança simples: `v113_faturas/v113_titulos/demo_boleto` — `V113OperationalService.cs:183-184` ("Boleto demonstrativo sem valor financeiro real").
  - Alerta operacional de glosa criado junto do registro: `V115GlosaService.cs:15` (insert em `v115_alertas_operacionais`, tipo `GLOSA_ANALISAR`, rota `/FaturamentoClinico/Glosas`).
  - Repasses médicos: `V115RepasseMedicoService` (rotas `api/v115/repasses-medicos*`, `V115FaturamentoController.cs:22-26`).
- **OBS:** O "envio" de lote é apenas transição de status `ENVIADO_DEMO`/`RETORNO_DEMO` (mesma linha 187) — não existe integração externa TISS/API de convênio nem protocolo de retorno gravado para faturamento (busca `"protocolo"` + `"TISS"`: `protocolo_externo` existe apenas para cotações Adm360 — ver D2). Web: páginas demo `backend/PlantaoPro.Web/Controllers/FaturamentoClinicoController.cs:44-45`, `Saude360WebControllers.cs:302-303` ("Glosas por lote", "Lotes, guias, conferência e envio").

### A2. Itens faturáveis + pendências documentais; prevenção de duplicidade
- **ESTADO:** parcial
- **EVIDÊNCIA:**
  - Itens faturáveis = produtos: entity auditada `v114_itens_faturaveis` + outbox `ITEM_FATURAVEL_CRIADO` em `V114ProdutoService.cs:67-68`; vínculo `item_faturavel_id` em `v115_regras_faturamento` (`database/migrations/2026_v115_regras_faturamento_repasses.sql:4`; seed `database/seeds/2026_demo_v115_regras_faturamento.sql:2-3`).
  - Prevenção de cobrança duplicada: checagem app-level `GerarContaAsync` retorna 409 "Conta ja existe para esta origem." (`V115FaturamentoRegraService.cs:59`) **e** unique index no banco: `ux_v195_conta_origem_ativa on plantaopro.v113_faturas(tenant_id,origem_tipo,pedido_id) where reg_status='A'...` — `database/scrpt_completo.sql:3172` (fonte: migration v1.95, `database/migrations/2026_08_v195_financeiro_revenue_cycle.sql`).
  - Pendência documental: apenas alerta `v115_alertas_operacionais` para glosa (A1); não encontrei módulo de "pendências documentais" de faturamento (buscas: `"pendencia.*documental"`, `"documento"` em V115*).
- **OBS:** Não há teste dedicado da constraint `ux_v195_conta_origem_ativa`; os testes da área são contract tests estáticos: `backend/PlantaoPro.Tests/V115RegrasFaturamentoContractTests.cs:23,35,41`.

### A3. Faturamento parcial; protocolo/retorno de integração externa registrado
- **ESTADO:** parcial
- **EVIDÊNCIA:**
  - Outbox com status gravado: `v113_outbox_eventos` (`status PENDING/PROCESSED/ERROR` + coluna `erro`) — inserção em `V115FaturamentoRegraService.cs:60`, `V114ProdutoService.cs:157`; consulta/ação manual em `V113OperationalService.cs:186-188` + `v113_outbox_logs`; rotas `api/v113/outbox*` em `Controllers/V112HomologationController.cs:105-118`.
  - Protocolo externo persistido **apenas** no Adm360 (cotações): `adm360_cotacao_envios.protocolo_externo` — `database/scrpt_completo.sql:6272`; gravação em `backend/PlantaoPro.Infrastructure/Administrativo360/CotacoesRepository.cs:1126-1133`.
- **OBS:** Nenhum evento de faturamento de convênio é efetivamente processado por worker — "detalhe" do outbox literalmente diz "Ação ... registrada sem envio externo de produção." (`V113OperationalService.cs:187`). Sem protocolo de retorno no ciclo de convênios.

### A4. Glosa afetando parte de item/lote; recurso com motivo/documentos/prazo/resultado
- **ESTADO:** parcial
- **EVIDÊNCIA:**
  - Registro de glosa: `POST api/v115/glosas` (`V115FaturamentoController.cs:28`) → `V115GlosaService.cs:15`: validações (motivo obrigatório, valor ≤ elegível, tenant), campos `conta_receber_id`, `titulo_id`, `convenio_id`, `motivo`, `valor_glosado`, `prazo_recurso`, status `ABERTA`; tabela `v115_regras_glosa`, colunas `valor_recuperado`, `resolucao` + `ck_v195_glosa_positiva` (`scrpt_completo.sql:3183-3184` / migration v1.95:30-32).
  - Recurso/resolução: `POST glosas/{id}/resolver` → `ResolverAsync` grava `status='RESOLVIDA'`, `resolucao=@motivo` (`V115GlosaService.cs:16`); variação Saude360: `POST api/clinica-financeiro/glosas/{id}/recorrer` exige justificativa — `Controllers/Saude360ClinicalControllers.cs:259`, `Saude360ClinicalService.cs:808`; segunda tabela `convenio_glosas` (`scrpt_completo.sql:3978-4034`, migração `database/migrations/2026_10_v2315_saude360_b8_financeiro_canonico.sql`).
  - Impacto no saldo: `glosa_reconhecida` abatida no saldo das contas (`V115FaturamentoRegraService.cs:40`).
- **OBS:** Granularidade da glosa é **por conta/título**, não por item de lote (não existe glosa por `v116_faturamento_lote_itens`). Prazo de recurso persistido e gera prioridade ALTA se < 2 dias. **Documentos** anexados ao recurso: ausentes (busca `"anexo"|"documento"` em `V115GlosaService.cs`/`convenio_glosas` → nada). Teste funcional: só regra de justificativa em `PlantaoPro.Tests/ClinicalCommercialRulesTests.cs:28`.

### A5. Recebimento parcial mantém saldo + vínculo com itens
- **ESTADO:** pronto com evidência (para conta a receber clínica)
- **EVIDÊNCIA:** `V115FaturamentoRegraService.ReceberAsync` (`cs:44-50`): `SELECT ... FOR UPDATE` na fatura, soma `RECEBIMENTO`/`ESTORNO` da conta, rejeita excedente (422), grava linha em `v115_recebimentos` com `conta_receber_id` (vínculo), status da fatura `PARCIAL`/`PAGA`, histórico em `v115_financeiro_historico` com saldo anterior/novo; estorno parcial com vínculo `recebimento_origem_id` e teto (`cs:52-55`); saldo derivado no read model (`cs:40`). DB: `v115_recebimentos` + `ck_v195_recebimento_positivo`/`tipo in ('RECEBIMENTO','ESTORNO')` (`scrpt_completo.sql:3159-3180` / `database/migrations/2026_08_v195_financeiro_revenue_cycle.sql:11-32`). Também `v116_recebimentos_parciais` (demo, `V116ConsolidacaoServices.cs:192`).
- **OBS:** "Itens" aqui são contas/faturas, não itens de lote de convênio.

---

## Área B — Pacientes: identificação e duplicidades (§23)

### B1. Busca por combinações de dados (nome+cpf/doc+nascimento)
- **ESTADO:** parcial
- **EVIDÊNCIA:** `GET api/pacientes/buscar?termo=&pagina=&tamanho=` — `backend/PlantaoPro.Api/Controllers/Saude360ClinicalControllers.cs:294`; filtro é **um único termo livre** casando `nome | cpf | telefone | email` via `ilike` (`Saude360ClinicalService.cs:635`). Lookup de apoio: `Saude360SupportControllers.cs:16`.
- **OBS:** Não existe endpoint com parâmetros combinados nome+cpf+data_nascimento (buscas: `"data_nascimento"` nos controllers → apenas coluna persistida; `"combinac"`, `"busca-avancada"` → nada).

### B2. Aviso de duplicidade na criação (nome semelhante)
- **ESTADO:** parcial
- **EVIDÊNCIA:** CPF **exato** já cadastrado bloqueia criação/atualização: contagem por CPF normalizado → `Saude360ClinicalService.cs:153` (criar) e `:215` (atualizar); unique index condicional `ux_pacientes_cliente_cpf_informado on pacientes(cliente_id, regexp_replace(cpf,'[^0-9]',''))` — `database/migrations/2026_plantao_pro_saude360_base_clinica.sql:27`.
- **OBS:** Detecção de **nome semelhante/fuzzy** ou "aviso" (vs bloqueio): **ausente** (buscas: `"similare"`, `"fuzzy"`, `"trigram"`, `"suggest"` em Api + Web → só sugestões de plantão/escala, nada de paciente).

### B3. Correção de cadastro com histórico; mesclagem controlada
- **ESTADO:** histórico pronto com evidência; mesclagem ausente
- **EVIDÊNCIA:**
  - Histórico: tabela `paciente_historico(id, ..., paciente_id, acao, detalhes jsonb, usuario_id)` — `backend/sql/20260610_saude360_base_clinica_idempotente.sql:87,211-212`; inserção em toda atualização de paciente (`Saude360ClinicalService.cs:752`, acoes CRIAR/ATUALIZAR/INATIVAR/REATIVAR via `AcaoAsync`); CRUD completo em `PacientesController` (`Saude360ClinicalControllers.cs:282-295`).
  - Mesclagem: **nenhuma** tabela/endpoint — buscas em backend+database: `"paciente_merg"`, `"merged_into"`, `"merge"`, `"mescla"` (case-insens.) → zero ocorrências relacionadas a paciente.
- **OBS:** Item global: **parcial**.

### B4. Uso de nome social vs civil definido (coluna nome_social)
- **ESTADO:** parcial
- **EVIDÊNCIA:** Coluna existe: `pacientes.nome_social` — `database/migrations/2026_plantao_pro_saude360_base_clinica.sql:10,18`; persistida no insert (`Saude360ClinicalService.cs:682`, campo `@nomeSocial`); exposta no request DTO `PacienteRequest`.
- **OBS:** Regra de **uso** (quando exibir nome social vs civil em etiquetas/documentos): não encontrada (busca `"NomeSocial"` fora do insert/select → apenas passagem de dados).

### B5. Mesclagem preserva vínculos; impede cross-tenant
- **ESTADO:** ausente após busca
- **EVIDÊNCIA:** não há operação de merge para analisar (ver B3). Isolamento cross-tenant geral existe (ex.: testes `Saude360TenantScopeP2ContractTests.cs:65`, `Administrativo360IsolamentoBidirecionalQ7Tests`), mas específico de merge não se aplica.

---

## Área C — Central de importações (§24)

### C1. Importadores existentes
- **ESTADO:** parcial (2 importadores reais; nenhum para clientes/pacientes em planilha)
- **EVIDÊNCIA:**
  1. **XML NF-e/NFS-e (Adm360)**: `backend/PlantaoPro.Api/Controllers/Adm360DocumentosXmlController.cs` (route `api/administrativo360/xml`: `importar-manual`:71, `conferir`:89, `manifestar`:99, `vincular-recebimento`:109, `sincronizar-dfe`:128, `triagens`:138-166) + `DocumentosXmlRepository.cs` (`ImportarXmlAsync`, linhas ~245-325).
  2. **CID (CSV manual ou URL)**: `Controllers/Saude360ClinicalControllers.cs:168-170` (`importacoes`, `importar-csv`, `importar-url`) + `Saude360ClinicalService.cs:404-456` (tabela `cid_importacoes`).
  3. Sincronização DFe/WS Adm360 (`ExecutarSincronizacaoDfeAsync` — mesmo controller).
- **OBS:** Busca `"IFormFile"`, `"xlsx"`, `"Excel"`, `"OpenXml"` em Api → CSV/XML apenas nos itens acima; demais "csv" são **exportação**.

### C2. Fluxo padronizado: selecionar → validar → prévia → confirmar → processar → resultado
- **ESTADO:** parcial
- **EVIDÊNCIA (XML Adm360):** selecionar (upload base64) → validar (teto 2 MiB / 2M chars `Adm360DocumentosXmlController.cs:78-80`; parsing multi-doc; destinatário autorizado, mod divergente → quarentena `DocumentosXmlRepository.cs:281-321`) → processar → resultado consultável (listagem `GET` por status/quarentena `cs:30-40` + triagens abertas `cs:138`). **CID:** upload → parse → gravar direto.
- **OBS:** Etapas **"prévia"** e **"confirmar"** (duas etapas) não existem para nenhum importador (busca `"previa"`, `"preview"` em contexto de import → nada; o XML grava imediatamente no `importar-manual`).

### C3. Erros por linha/por item exibidos; política de sucesso parcial
- **ESTADO:** pronto com evidência (XML); parcial (CID)
- **EVIDÊNCIA:** XML: resultado **por unidade de documento** com erro individual (`ImportarXmlDocumentoResultado(..., un.MotivoQuarentena, ex.Message)` — `DocumentosXmlRepository.cs:290-325`) + resumo agregado `Total/Importados/EmQuarentena/DuplicadosIgnorados/Falhas` (`cs:325`; log do controller `cs:83-85`); quarentena com motivo (`DESTINATARIO_NAO_AUTORIZADO`, `XML_MALFORMADO`...). CID: apenas totais `Inseridos/Atualizados/Erros` (`Saude360ClinicalService.cs:456`), sem detalhe por linha.
- **OBS:** Política de sucesso parcial implementada no XML (partes importadas, partes em quarentena, partes falhas coexistem).

### C4. Idempotência de reimportação — testes?
- **ESTADO:** pronto com evidência (XML); CID usa upsert com contadores, sem teste específico
- **EVIDÊNCIA:**
  - Constraint `UNIQUE(tenant_id, chave_acesso)` em `adm360_documentos_recebidos` — `database/migrations/2026_09_v2196_administrativo360_cotacoes_xml_dashboard.sql:232`; dedup intra-arquivo (`vistosNoArquivo`, `DocumentosXmlRepository.cs:282-300,316`).
  - Testes: `Administrativo360DocumentosXmlA3Tests.cs` → `G2_DuplicadoIntraArquivo_SegundaOcorrenciaIdempotente` (:298), `G3_Malformado_ChaveSinteticaDeterministica_Idempotente` (:467), `G5_Conferir_Idempotente_QuarentenaBloqueia` (:556); além disso `Administrativo360DocumentosXmlB1Tests.cs` / `WsA3Tests.cs`.
- **OBS:** CID: `total_atualizados` sugere upsert por código (`Saude360ClinicalService.cs:454`), mas sem teste de reimportação nomeado.

### C5. Processamento longo com progresso + resultado consultável
- **ESTADO:** parcial
- **EVIDÊNCIA:** Resultado consultável existe (listagens `documentos_recebidos`, `triagens`, `cid_importacoes.status='PROCESSADA'` `Saude360ClinicalService.cs:454`). **Sem** job assíncrono, coluna de progresso ou fila/outbox para importações (busca `"progresso"`, `"percentual"` em import → nada; tudo síncrono dentro do request, limitado a 2 MiB/2M chars `Adm360DocumentosXmlController.cs:79-80`).

---

## Área D — Recuperação de falhas e reprocessamento (§25)

### D1. Operações assíncronas com estado de processamento persistido
- **ESTADO:** pronto com evidência (escopo Adm360 + outbox demo)
- **EVIDÊNCIA:**
  - `adm360_cotacao_envios` — máquina de estados `INICIADA/ENVIANDO/EXPORTADA_MANUALMENTE/CONFIGURACAO_PENDENTE/ACEITA_PELO_PORTAL/REJEITADA_PELO_PORTAL/RESULTADO_DESCONHECIDO/INTERRUPTA`, `UNIQUE(tenant_id,resposta_id,tentativa)` — `database/scrpt_completo.sql:6256-6283` (fonte: `database/migrations/2026_09_v2304_administrativo360_transmissao_tentativas.sql`).
  - Outbox: `v113_outbox_eventos` (`status` + `erro`) e `v113_outbox_logs` — `V113OperationalService.cs:186-188`; `v116_integracao_provedores` (`OUTBOX_OPERACIONAL`, `V116ConsolidacaoServices.cs:22,195`).

### D2. Tentativas + último erro persistidos; correlação com origem
- **ESTADO:** pronto com evidência
- **EVIDÊNCIA:** contador `tentativas` na resposta (`CotacoesRepository.cs:1079`), linha por tentativa com `canal`, `mensagem`, `protocolo_externo`, `iniciado_em/finalizado_em` (`scrpt_completo.sql:6260-6276`; insert `CotacoesRepository.cs:1069-1075`); correlação por `resposta_id` (FK `adm360_cotacao_respostas.id`) e, no outbox, `payload_ref` (`V115FaturamentoRegraService.cs:60`). Comentário de design no migration (`scrpt_completo.sql:6233-6246`).
- **OBS:** Para o outbox clínico (v113), `erro` é preenchido manualmente; falha real de conector só no Adm360.

### D3. Reprocessamento autorizado (endpoint/botão)
- **ESTADO:** pronto com evidência
- **EVIDÊNCIA:**
  - `POST api/administrativo360/... respostas/{respostaId}/transmitir?confirmarRetransmissaoDesconhecido=` com policy `Adm360.TransmitirResposta` — `Controllers/Adm360CotacoesController.cs:165-170`.
  - Outbox: `POST api/v113/outbox/{id}/retry|process|error` — `Controllers/V112HomologationController.cs:108-118`.
  - `POST jobs/{id}/reprocessar` — `Controllers/OperacoesController.cs:33`; `POST notificacoes-operacionais/reprocessar` — `Controllers/V116ConsolidacaoController.cs:44`.

### D4. Detecção de processamento travado/interrupido
- **ESTADO:** pronto com evidência
- **EVIDÊNCIA:** `Adm360TransmissaoRecovery.ReconciliarEnviandoAncoradosAsync` (limiar de idade 10 min; `ENVIANDO → RESULTADO_DESCONHECIDO`, tentativas `INICIADA → INTERRUPTA`) — `backend/PlantaoPro.Infrastructure/Administrativo360/Adm360TransmissaoRecovery.cs:17-56`; execução no boot — `backend/PlantaoPro.Api/Program.cs:338`; **scanner periódico** `Adm360TransmissaoRecoveryHostedService` (BackgroundService, default 300 s, config `Adm360:TransmissaoRecovery:IntervaloSegundos`) — `Adm360TransmissaoRecoveryHostedService.cs:35-62`.

### D5. Transiente vs permanente; timeout externo → resultado desconhecido consultado antes de retry
- **ESTADO:** pronto com evidência
- **EVIDÊNCIA:** Timeout 30 s ou exceção do conector → `RESULTADO_DESCONHECIDO` com mensagem honesta ("O retorno do envio não pôde ser confirmado") — `CotacoesRepository.cs:1092-1111`; guard anti-sobreescrita só se ainda `ENVIANDO` (`cs:1126-1133`); retransmissão exige confirmação explícita: `CotacaoRegras.ValidarRetransmissaoPermitida(statusAtual, command.ConfirmarRetransmissaoDeDesconhecido)` (`cs:1063`) e bloqueio se já `ACEITA_PELO_PORTAL` citando o protocolo (`cs:1060-1061`). Testes: `Administrativo360CotacoesTransmissaoB5Tests.cs:353 FalhaDeConector_ResultadoDesconhecidoExigeConfirmacaoExplicitaParaRetransmitir`; `:497 AceitaPeloPortal_BloqueiaRetransmissaoCitandoProtocoloENaoCriaTentativa`.
- **OBS:** Fora do Adm360, o outbox v113 classifica apenas PENDING/PROCESSED/ERROR manualmente.

### D6. Restart da aplicação não perde estado
- **ESTADO:** pronto com evidência (Adm360); parcial (outbox clínico)
- **EVIDÊNCIA:** Recovery de boot concilia o que ficou preso (`Program.cs:338`; `Adm360TransmissaoRecovery.cs:6-13` "nunca apagadas nem inventadas"); teste de interrupção: `Administrativo360CotacoesTransmissaoB5Tests.cs:413 EnvioInterrompido_ReconciliacaoMarcaDesconhecidoETentativaInterrupta_RecentePermanece`.
- **OBS:** Para `v113_outbox_eventos`/`v116_integracao_provedores` **não há worker que retoma PENDING no boot** — ações manuais via endpoints (D3); eventos PENDING permanecem PENDING após restart (estado preservado, sem retomada automática).

---

## Área E — Notificações e comunicação contextual (§26)

### E1. Eventos de notificação + canais
- **ESTADO:** parcial (somente in-app de fato)
- **EVIDÊNCIA:**
  - In-app legado: `NotificacaoService.CriarNotificacaoAsync` insere em `plantaopro.notificacoes` (só flag `lida`) — `Data.cs:2158`; eventos de escala/financeiro/convites em `Data.cs:1447,1513,1584,1606,1632,1670,1687,1702,1859,1905,1931,1957,1985,2005,2033`; `BusinessRulesServices.cs:374`.
  - In-app "operacional": `Operation360/Notifications/NotificationService.cs` (tabela `notifications` + `notification_recipients`, realtime via `IOperationRealtimePublisher` — :119-123) + `AlertRuleService.cs` disparando eventos `PLANTAO_SEM_PROFISSIONAL`, `CHECKIN_PENDENTE`, `PLANTAO_AGUARDANDO_CONFIRMACAO`, `OCORRENCIA_PLANTAO`, `FECHAMENTO_FINANCEIRO_PENDENTE`, `PLATAFORMA_ALERTA` (:133-251).
  - v1.16 operacional: `V116NotificacaoOperacionalService` (`V116ConsolidacaoServices.cs:195`).
  - **Email/WhatsApp: sem envio real.** Buscas `"smtp"`, `"MailKit"`, `"SendGrid"`, `"EmailSender"`, `"whatsapp"` (código) → coluna de preferência existe (`notification_preferences(..., in_app, email, push, whatsapp)` — `NotificationService.cs:80-87`; legacy `usuario_notificacao_preferencias(in_app,email)` — `PremiumServices.cs:45-65`), mas **nenhum provedor/enviador** encontrado em todo o `backend`.

### E2. Estados criada/enviada/entregue/lida persistidos
- **ESTADO:** parcial
- **EVIDÊNCIA:** criada (insert) / lida (`notification_read_states` com `on conflict do nothing` — `NotificationService.cs:59`; legacy `lida boolean` — `Data.cs:2158,2198`); ações `RESOLVIDA`/`ARQUIVADA` em `notification_actions` (`NotificationService.cs:69-75`); v116: `PENDENTE → LIDA`. Estados **enviada/entregue**: ausentes (coerente com ausência de canal externo).

### E3. Dedupe em retries
- **ESTADO:** parcial
- **EVIDÊNCIA:** Operation360 tem dedupe: `insert into notifications ... on conflict(tenant_id,usuario_id_dedupe,origem_tipo,origem_id,tipo_evento) do nothing` e `if (inserted == 0) return false;` — `NotificationService.cs:97-98`; read-states idempotentes. AlertRuleService consulta estado atual antes de disparar (`AlertRuleService.cs:139-159`).
- **OBS:** O caminho legado (`Data.cs:2158`) inseria **sem** dedupe/unique — cada chamada criava nova notificação. **Corrigido em R8-B2** (v2346: coluna `origem_tipo`/`origem_id` + unique index parcial + `on conflict do nothing`; os 21 call sites declararam origem). Doc `r8b2-notificacoes-legado-dedupe.md`.

### E4. Preferências por usuário (não anulam avisos obrigatórios)
- **ESTADO:** parcial
- **EVIDÊNCIA:** Preferências per-user per categoria/tipoEvento: `GET/POST` em `Controllers/NotificacoesController.cs:95-102` + `NotificationService.cs:77-89` (`on conflict do update`), e legacy `PremiumController.cs:33-53` / `PremiumServices.cs:45-65`. `in_app` é fixado a `true` no upsert (`:87`), e o dispatch respeita `ativo and in_app` (`:94`).
- **OBS:** Não existe classificação de categorias/avisos "obrigatórios" imunes à preferência (busca `"obrigat"|"critical.*preference"` → nada); flags `email/push/whatsapp` são gravadas mas nunca lidas para envio.

### E5. Templates respeitando idioma/tenant/identidade visual
- **ESTADO:** ausente após busca (tabelas mortas existem)
- **EVIDÊNCIA:** Títulos/mensagens são strings literais em PT-BR embutidas no código (ex.: `Data.cs:1584` "Sua escala foi confirmada"; `AlertRuleService.cs:133` "Plantão sem cobertura"). Tabelas `tenant_parametros_notificacoes` (`database/migrations/2026_plantao_pro_white_label_b2b_launch.sql:2808-2832`) e `notificacoes_avancadas` (`:3127`) existem no schema, mas **não são referenciadas por nenhum código** (grep em backend → 0 matches).
- **Termos buscados:** `"template"` (contexto notificação), `"idioma"|"language"` em `Operation360/Notifications`, `"branding"` em dispatch → nenhum motor de template ligado a notificações. (I18n existe só para UI: `CrossCutting/Localization/UiLanguageCatalog.cs`.)

### E6. Alteração de escala gera aviso rastreável
- **ESTADO:** pronto com evidência
- **EVIDÊNCIA:** `EscalaService` dispara notificação **na mesma transação** da mudança de escala: confirmada `Data.cs:1584`, recusada `:1606`, cancelada `:1632`, substituída `:1670`, realizada `:1687`, não comparecimento `:1702`, aceite `:1447`, compromisso confirmado `:1513`; rastreabilidade adicional via `IAuditService` injetado no serviço (`Data.cs:1261`) e auditoria de leitura de notificação (`:2205,2216`). Complemento proativo: `AlertRuleService.cs:133,159,179`.
- **OBS:** O "aviso" é in-app (sem canal externo — ver E1).

---

## Notas transversais de metodologia
- Projetos analisados: `PlantaoPro.Api` (Data.cs + services V113-V116 + Controllers), `PlantaoPro.Infrastructure/Administrativo360`, `PlantaoPro.Web/Controllers`, `PlantaoPro.Tests`, `database/migrations/*.sql` (+ `scrpt_completo.sql` como visão consolidada com SOURCE citada por seção).
- Padrões de teste desta base: predomínio de **contract tests** (verificam arquivos/rotas/schema — ex.: `V115RegrasFaturamentoContractTests.cs:23-41`) e poucos testes comportamentais com banco real (Adm360 B1/A3/B5 são os mais fortes). Itens marcados "pronto com evidência" citam o teste quando existe; onde o "teste" é de contrato estático, consta em OBS.
