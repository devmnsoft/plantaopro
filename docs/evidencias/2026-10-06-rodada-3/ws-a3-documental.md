# WS-A3 — Identidade documental por família fiscal e sequência de eventos (item 3)

> Entregue em 2026-10-06. Base de origem: `main` = `d1b3736` (WS-A2) sobre `origin/main` = `7f43af2`.
> Contrato do item 3: unicidade documental no banco após reconciliar duplicidades históricas,
> preservação dos bytes originais, idempotência de importação/vinculação concorrente, resolução
> explícita do conflito recebimento-placeholder vs. físico, `sequencia_evento` ordenada e única
> sob concorrência, e migrations incrementais com pré-condições, diagnóstico de conflitos e
> pós-condições verificáveis.

## 1. Migration incremental v2311

Arquivo: `database\migrations\2026_10_v2311_adm360_documentos_identidade_eventos.sql`
SHA-256: `5075bfe673fcf1d093b36eda354eb5f93287e2675af402da23599c2c4b85b477`

Estrutura (transação única, via wrapper com `\set ON_ERROR_STOP on`):

1. **Pré-condições** — `to_regclass` das tabelas alvo + checagem de colunas via
   `information_schema`; qualquer ausência ⇒ `RAISE EXCEPTION` nomeando a dependência.
2. **Bypass de imutabilidade em escopo transacional** —
   `set_config('plantao.bypass_imutabilidade_adm360','on',true)` (somente na transação da migração).
3. **Reconciliação de duplicidades** — DO bloco que exclui apenas cópias **byte-idênticas** ao âncora
   mais antigo de cada 5-tupla fiscal (`RAISE NOTICE` por exclusão); bytes divergentes NUNCA são
   excluídos em silêncio.
4. **Diagnóstico ANTES do índice** — DO bloco que faz `RAISE EXCEPTION` listando as 5-tuplas com
   bytes divergentes (conflito real exige decisão humana; a migração não chuta).
5. **Unicidade fiscal** — `CREATE UNIQUE INDEX IF NOT EXISTS ux_adm360_docrec_tenant_fiscal
   ON (tenant_id, emitente_cnpj, modelo, numero, serie) WHERE quarentena = false`
   (índice parcial: documentos em quarentena não participam da identidade — eles ainda estão sem
   campos fiscais confiáveis).
6. **Normalização da sequência de eventos** — DO bloco com
   `ROW_NUMBER() OVER (PARTITION BY tenant_id, documento_id ORDER BY data_evento, id)` renumerando
   de 1..N e **recalculando o hash canônico** com a fórmula verbatim da v2301
   (`encode(digest(id|tenant|tipo|seq|descricao|data|protocolo|detalhes,'sha256'),'hex')`).
7. **Unicidade de sequência** — guardado por `pg_constraint` para idempotência:
   `ADD CONSTRAINT ux_adm360_doc_eventos_tenant_doc_seq UNIQUE (tenant_id, documento_id, sequencia_evento)`.
8. **Pós-condições** — DO bloco que revalida (índice presente, zero violações residuais, hashes
   consistentes) e falha a migração caso contrário.

### Aplicação e idempotência (prova executada em `plantaopro_test`)

| Execução | Resultado |
|---|---|
| 1ª | Aplicou; normalizou **6 eventos** (drifts pré-existentes nos documentos `33caa57b…` seqs `1,2,2` e `a3600000…` seqs `1,1,1,1,1,2`) e recalculou 6 hashes canônicos; sem exclusões divergentes (o diagnóstico encontrou apenas pares de MALF quarentenizados, fora do índice parcial por construção). |
| 2ª | 100% idempotente: nenhuma alteração, zero exceções, registro em `plantaopro.schema_migrations (id, script_path, checksum)` com `ON CONFLICT DO NOTHING`. |

### Manifestos e script completo (validados)

- `database\migration-manifest.json` — entrada v2311 após v2310: `dependsOn: v2310`,
  `transactional: true`, `installRequired: true`, checksum = SHA-256 do arquivo bruto.
- `database\install-manifest.json` — nova seção order **80** "Administrativo360 Identidade Documental
  e Sequencia de Eventos WS-A3 v2.21.8".
- `database\scrpt_completo.sql` — **regenerado** pelo `scripts\generate-scrpt-completo.py`
  (nunca editado à mão); `validate-scrpt-completo.py` → `{"ok": true, "coveragePercent": 100.0}`
  (hash `a789d615ceb0f34445460b2568ffdd667ce4fa50dcf362ffb12b6f20d623ad1d`).

## 2. Contrato implementado (C#)

| Camada | Arquivo | Papel |
|---|---|---|
| Lock determinístico | `backend\PlantaoPro.Infrastructure\Administrativo360\DocumentosXmlRepository.cs` (import por unidade) | `pg_advisory_xact_lock('adm360:docxml:{tenant}:{chave}')` serializa importações concorrentes da MESMA chave de acesso — vencedor único e contrato de idempotência explícito. |
| Pré-checagem fiscal | idem | Antes do INSERT, consulta a 5-tupla fiscal; se outra linha (fora de quarentena) já a ocupa ⇒ `Administrativo360BusinessException` "Identidade fiscal … já registrada" — mensagem de negócio, nunca violação técnica vazia. |
| INSERT defensivo | idem | `ON CONFLICT (tenant_id, chave_acesso) DO NOTHING` + releitura do vencedor: bytes/hash idênticos ⇒ `DuplicadoIdempotente = true`; conteúdo divergente ⇒ erro de negócio explícito ("conteúdo XML divergente"). Janela residual (commit entre pré-checagem e INSERT) coberta por `catch (NpgsqlException ex) when (ex.SqlState == "23505")` com detalhe do banco na mensagem. |
| Sequência de eventos | `Adm360Repository.cs` — `ProximaSequenciaEventoDocumentoAsync` | `COALESCE(MAX(sequencia_evento),0)+1` dentro da transação com `FOR UPDATE`-semântica da própria inserção; todo evento de documento usa o helper (import/conferência/vínculo/gate), garantindo contiguidade 1..N e satisfação do novo unique. |
| Gate de recebimento (placeholder-vs-físico) | `ComprasRepository.cs` — `ReceberAsync` | Se o documento já tem `recebimento_id`: carrega o recebimento canônico (gate `FOR UPDATE`), valida mesmo pedido (divergente ⇒ erro "outro pedido" explícito) e **reusa** — sem segundo recebimento, sem segundo vínculo. Só quando ainda sem recebimento: write-back `recebimento_id/pedido_id/titulo_pagar_id` + evento `VINCULACAO_RECEBIMENTO`. `status_conferencia` permanece inalterado (`VINCULADO` segue exclusivo do vínculo explícito — contrato G5 preservado). |
| Título único por origem | idem | Ao reusar o recebimento canônico, o novo total de parcial **acumula** no título existente (`valor_principal += Δ`, `saldo_aberto += Δ`) em vez de inserir um segundo registro — honra a restrição canônica `ux_adm360_titulos_pagar_origem` (descoberta pelo teste T5). |
| Retry serializável | idem | O corpo inteiro de `ReceberAsync` agora passa por `ExecutarComRetrySerializableAsync` (40001/40P01, máx. 4), igual aos demais repositórios ADM360 — corrigido na raiz o abort que vazava como erro técnico sob paralelismo (cause root dos flakes G5/T5/T6). |

Os bytes originais continuam preservados: o import grava `xml_bytes` do arquivo recebido
(`ImportarXmlManualCommand.XmlBytes`) e a deduplicação compara bytes antes de qualquer decisão
(contrato B1/A3 inalterado).

## 3. Verificação

### 3.1 Testes novos — `backend\PlantaoPro.Tests\Administrativo360DocumentosWsA3Tests.cs` (T1–T8)

| Teste | Cenário | Contrato verificado |
|---|---|---|
| T1 | N=8 importações **concorrentes** da mesma chave/arquivo | exatamente 1 linha + 1 evento `IMPORTACAO_MANUAL`, `Falhas = 0` nas 8 unidades, todas apontam para o MESMO `DocumentoId` (idempotência determinística via advisory lock) |
| T2 | Concorrência divergente (mesma chave, vNF 110,00 vs 220,00) | 1 vencedor persistido + exatamente 1 falha de negócio com mensagem contendo "divergente" ou "Identidade fiscal" — nunca exceção técnica vazia |
| T3 | importar → conferir → vincular recebimento | sequências `1,2,3` contíguas (tipos `IMPORTACAO_MANUAL` / `CONFIRMACAO_CONFERENCIA` / `VINCULACAO_RECEBIMENTO`) e status `VINCULADO` |
| T4 | recebimento físico do documento | write-back completo (recebimento/pedido/título) no documento, `status_conferencia` segue `CONFERIDO` (não vira VINCULADO) e evento seq 3 com descrição de "físico" |
| T5 | 2ª parcial do mesmo documento/pedido | **reuso canônico**: 1 recebimento, 2 movimentos (keys distintas), 1 título acumulado a 200,00 (5×20 + 5×20), pedido `RECEBIDO`, somente 1 evento de vínculo |
| T6 | receber em OUTRO pedido usando o mesmo documento | `Administrativo360BusinessException` "outro pedido" — erro de negócio explícito, sem violação de constraint |
| T7 | 2 chaves distintas compartilhando a 5-tupla fiscal (chars 25–33), importação sequencial | 1ª importa; 2ª falha com "Identidade fiscal" e **nenhuma** segunda linha é criada (pre-unique-index defense in depth) |
| T8 | 2 MALFs distintos (truncados, bytes diferentes) | coexistem em quarentena — o índice parcial os exclui da identidade fiscal sem falso positivo |

Resultado: **8/8 VERDE** (filtro `FullyQualifiedName~Administrativo360DocumentosWsA3Tests`).

### 3.2 Suíte completa (PostgreSQL real `plantaopro_test`)

`dotnet test backend\PlantaoPro.Tests\PlantaoPro.Tests.csproj --nologo`

| Rodada | Total | Aprovados | Observação |
|---|---|---|---|
| base (WS-A2) | 902 | 902 | referência |
| WS-A3 run 1 | 910 | 909 | flake 40001 no gate (antes do fix de retry) — reproduziu a causa raiz histórica do A3/G5 |
| WS-A3 run 2 | 910 | 908 | 2 flakes 40001 (T5/T6) confirmando a mesma raiz |
| WS-A3 run 3 (pós-fix) | **910** | **910** | log `ws-a3-suite-run3.log` |
| WS-A3 run 4 (pós-fix) | **910** | **910** | log `ws-a3-suite-run4.log` — confirma determinismo; `Aceite12_XmlRepetido_NaoDuplica` e A3/G5 verdes nas duas rodadas |

As correções aplicadas durante a execução dos testes (todas por causa raiz, nenhuma por ajuste de
asserção para passar):
1. Purge de testes zerava `recebimento_id/pedido_id/titulo_pagar_id` do documento antes de excluir
   as origens (o write-back criou FKs novas — afeta também o helper do A3Tests/G5).
2. Acúmulo do título existente no reuso canônico (novidade funcional do próprio item 3).
3. `ReceberAsync` sob `ExecutarComRetrySerializableAsync` (padrão dos demais repositórios).

## 4. Limitações (declaradas)

- Banco validado: apenas `plantaopro_test` local (migração aplicada 2× aqui); ambientes externos
  recebem v2311 no upgrade com o mesmo comportamento (transacional, idempotente, com diagnóstico).
- Documentos em **quarentena** ficam fora do índice de unicidade fiscal por projeto: a identidade é
  plena quando conferidos (os campos fiscais só são confiáveis após parsing válido).
- O diagnóstico da v2311 lista conflitos de bytes divergentes, mas **não resolve sozinho**: nesses
  casos a migração aborta e exige decisão humana (corrigir/descartar as linhas e reexecutar).
- Flakes não invalidam contratos, mas este registro não promete ausência absoluta de bugs; as duas
  rodadas canônicas verdes (run 3/4) e o histórico de cause-root estão acima.
- IA sem chave real: inferência real continua não homologada externamente.

## 5. Status do item 3

| Critério do item 3 | Estado |
|---|---|
| Identidade documental por família fiscal | ✅ índice parcial único `(tenant, emitente, modelo, numero, serie)` + defesa em código (pré-checagem + 23505) |
| Unicidade no banco após reconciliar duplicidades | ✅ reconcile byte-idênticas + diagnóstico abortante para divergentes |
| Preservação dos bytes originais | ✅ dedupe por `xml_hash`/bytes; nenhum teste altera payload |
| Idempotência de importação/vinculação concorrente | ✅ T1 (8 paralelas, 1 linha), T2 (divergente → 1 vencedor + erro), advisory lock |
| Placeholder vs. recebido físico resolvido explicitamente | ✅ reuso canônico do recebimento (T4/T5/T6) com trilha auditável |
| `sequencia_evento` ordenado e único sob concorrência | ✅ renumeração na migração + helper `MAX+1` em tx + unique constraint (T3/T4/T5) |
| Migrations incrementais com pré/pós-condições verificáveis | ✅ v2311 (aplicada 2×, idempotente) + manifestos + `scrpt_completo.sql` 100% |

**Prontidão técnica: APROVADO.** Aceite funcional e liberação seguem o item 11; sem push automático.
