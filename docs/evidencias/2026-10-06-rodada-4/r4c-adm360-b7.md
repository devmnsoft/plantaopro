# R4-B7 — ADM360: cancelamento de cotação, estorno de resposta, triagem de quarentena e finalização honesta

Data: 2026-10-06 · Branch: `main` · Base: `0cd03ab` (R4-B6) · Banco: `plantaopro_test`

## 1. Escopo e decisões

O item B7 pede: jornada cotação→estorno completa; divergência com responsável+prazo; evitar
duplicidade; estados honestos de exportação.

Decisões adotadas nesta rodada:

1. **Cancelar cotação** — motivo obrigatório (validado fora da transação); idempotente quando já
   está `CANCELADA`; bloqueado com resposta em trânsito (`ENVIANDO`); evento `CANCELAMENTO` com
   chave idempotente `"cancelamento:cotacao:{id:N}"` (uma decisão, um registro).
2. **Estornar resposta** — justificativa obrigatória; bloqueado com `ENVIANDO`; devolve a resposta
   a `NA_FILA` preservando protocolo/tentativas/enviado_em e apendando o histórico
   `| Estorno (B7): {justificativa}` (truncado a 2000); se a cotação estava `RESPONDIDA`, volta a
   `PRONTA_PARA_ENVIO`. Evento `ESTORNO` **sem** chave (estornos podem se repetir).
3. **Anti-duplicidade na aprovação** — `SELECT ... FOR UPDATE` da resposta existente antes de
   criar: se existe, **reusa** (novo snapshot/status/attempt), sem segunda linha. Evento de
   criação com chave `"aprovacao:resposta:{id:N}"`; reuso sem chave + flag `reuso=true` no payload.
4. **Ordem de lock única** — cotação → resposta em todos os quatro fluxos de escrita
   (aprovar, cancelar, estornar, transmitir). Elimina a classe de deadlock entre esses fluxos.
5. **Finalização honesta** — a transação final só atualiza a resposta enquanto ela ainda estiver
   `ENVIANDO` (guard na WHERE). Se outro escritor (estorno/recovery/cancelamento) conciliou o
   estado durante a chamada externa, linhas afetadas = 0: nenhum campo é sobrescrito, o envio
   registra a nota `Finalizacao honesta (B7)...` com o estado real relido, a cotação **não** vira
   `RESPONDIDA`, e o evento `RETORNO_EXTERNO` passa a carregar `estado_resposta_conciliado`.
6. **Expiração lazy do prazo** — helper marca `EXPIRADA` por leitura de uma cotação específica
   (não varredura global nem job), dentro da mesma transação que decide a operação; mensagem de
   domínio ancorada: `O prazo de resposta da cotação expirou em {dd/MM/yyyy HH:mm:ss} UTC.`
7. **Triagem de documentos em quarentena** — listagem ordenada por prazo (menor primeiro,
   `NULLS LAST`) com flag derivada `Vencida` e `NomeArquivo=null` quando ausente; abrir exige
   quarentena + prazo futuro (UTC) + usuário do próprio tenant; resolver exige quarentena **e**
   responsável atribuído; eventos `TRIAGEM` (entidade `DOCUMENTO_XML`) sem chave.
8. **Orçamento com revisão** — gate de fase (`AGUARDANDO_APROVACAO` ou `EM_ORCAMENTO`); revisão
   nova = `MAX(revisao)+1` → sufixo `"-V{n}"` no número (primeiro orçamento mantém revisão 1);
   nova revisão muda a fase para `EM_ORCAMENTO` e a aprovação segue pela transição
   `EM_ORCAMENTO -> PRONTA_PARA_ENVIO` (matriz atualizada no domínio).
9. **Recovery de transmissão** — `Adm360TransmissaoRecoveryHostedService` (BackgroundService):
   skip em Testing / intervalo ≤ 0 / connection string vazia; config
   `Adm360:TransmissaoRecovery:IntervaloSegundos` (default 300, parse manual — quirk .NET 10).
10. **API** — endpoints novos (`cancelar`, `estornar`, triagens list/abrir/resolver) +
    `Adm360BusinessExceptionFilter` (`BusinessException`/`ArgumentException` → 400,
    `KeyNotFound` → 404). Web: banner de cancelamento em `Detalhes.cshtml`.

## 2. Implementação

| Ponto | Arquivo | Mudança |
|---|---|---|
| B7-schema | `database/migrations/2026_10_v2314_adm360_b7_cotacao_cancelamento_estorno_triagem.sql`, `database/migration-manifest.json` | migração aditiva idempotente (cancelamento, estorno, triagem) + entry (veja §3.1) |
| B7-repo | `backend/PlantaoPro.Infrastructure/Administrativo360/CotacoesRepository.cs` | `CancelarCotacaoAsync`, `EstornarRespostaAsync`, anti-duplicidade em `AprovarRespostaAsync` (L772+), revisão em `GerarOrcamentoCirurgicoAsync` (gate L658–663), `TransmitirRespostaAsync` (ordem de lock L1028–1038, expiração lazy L1031, guards CANCELADA/prazo L1043/L1047, `já foi concluída` L1061, finalização honesta L1116–1200, evento L1195–1198), `MarcarExpiradasPrazoExcedidoAsync` (L263) |
| B7-triagem | `backend/PlantaoPro.Infrastructure/Administrativo360/DocumentosXmlRepository.cs` | listar/abrir/resolver triagem com guards (L1028+) |
| B7-eventos | `backend/PlantaoPro.Infrastructure/Administrativo360/Adm360EventService.cs` | tipos `Cancelamento`, `Estorno`, `Triagem` + entidade/documentação das colunas |
| B7-contratos | `backend/PlantaoPro.Application/Administrativo360/CotacoesXmlContracts.cs` | comandos/DTOs novos (`CancelarCotacaoCommand`, `EstornarRespostaCommand`, `AbrirTriagemDocumentoCommand`, `ResolverTriagemDocumentoCommand`, `TriagemDocumentoDto`, campo `Revisar` no orçamento) |
| B7-dominio | `backend/PlantaoPro.Domain/Administrativo360/CotacoesXmlDashboard.cs` | matriz de transições (inclui `EM_ORCAMENTO -> PRONTA_PARA_ENVIO`), mensagens ancoradas |
| B7-recovery | `backend/PlantaoPro.Infrastructure/Administrativo360/Adm360TransmissaoRecoveryHostedService.cs` | **novo** — reconciliação cíclica de envios `ENVIANDO` órfãos |
| B7-api | `backend/PlantaoPro.Api/Controllers/Adm360CotacoesController.cs`, `Adm360DocumentosXmlController.cs`, `Program.cs` | endpoints novos + filtro de exceção + registro do hosted service |
| B7-web | `backend/PlantaoPro.Web/Views/Administrativo360/Cotacoes/Detalhes.cshtml`, `Models/Administrativo360CotacoesXmlModels.cs` | banner de cancelamento + modelo |
| B7-tests | `backend/PlantaoPro.Tests/Administrativo360R4B7EstornoCancelamentoTriagemTests.cs` | **novo** — 18 fatos T01–T18 (coleção `A360Transmissao`, stubs de conector `StubPortaL`/`StubGate`/`StubConcorrenciaHonesto`, fixtures XML CT-e 57 com chave única derivada, cleanup escopado c/ GUC de bypass de imutabilidade) |
| B7-regressões | `Administrativo360EventosImutabilidadeTests.cs`, `Administrativo360CotacoesXmlDashboardTests.cs` | testes pré-existentes atualizados ao contrato estendido pelo B7 (veja §3.2/§3.3) |

## 3. Defeitos fechados nesta rodada

### 3.1 v2314 aplicada e verificada em `plantaopro_test`

Checksum `4b56d6d862e403482d9473d75f1502abe730d613ac00f4635ec3158252210409`. Warning pré-existente
do runner inalterado: `IDENTITY_SCHEMA_INCOMPLETE MISSING_ADMIN_ROLE_LINK count=1` (defeito de dado
catalogado no B6, não de schema). **Aplicação em `plantaopro` (prod local) pendente para a janela
A2/F3** — junto de v2312/v2313.

### 3.2 Defeito de implementação pego pelo T17: cast inválido no caminho de finalização honesta

`TransmitirRespostaAsync` usava `QuerySingleOrDefaultAsync<object>` num SELECT de coluna única e
fazia cast `(string?)` no resultado — Dapper devolve **`DapperRow`** (a linha), não o escalar →
`InvalidCastException` exatamente no caminho de 0 linhas afetadas (outro escritor conciliou a
resposta durante a chamada externa — o caso que o B7 existe para cobrir). O teste T17 (concorrência
honesta com UPDATE direto sobre a linha) pegou o defeito na primeira execução da classe. Correção:
`QuerySingleOrDefaultAsync<string?>` (forma escalar do Dapper).

### 3.3 Contrato de eventos estendido: hashes recomputáveis atualizados (evolução documentada)

O B7 adicionou campos ao payload canônico de dois eventos imutáveis:

- `APROVACAO` → `+ reuso` (booleano; `false` na criação original, `true` no reuso pós-estorno);
- `RETORNO_EXTERNO` → `+ estado_resposta_conciliado` (`null` no caminho normal; estado real quando
  outro escritor conciliou durante a chamada).

Dois fatos de eventos recomputam o `sha256_hash` a partir de um payload fixo em código
(`JornadaCompletaManual...` e `ConectorComFalhaTecnica...`) e falharam com o hash novo — esperado,
é a evolução do contrato. Os testes passaram a reconstruir o payload completo na mesma ordem de
declaração (System.Text.Json: ordem dos membros do tipo anônimo define a chave JSON), e o primeiro
deles agora lê o novo campo direto do JSON armazenado em vez de presumir `null`. Nenhum evento
histórico foi reescrito (imutabilidade preservada — P0001 verificado nos testes).

### 3.4 Envelhecimento do seed demonstrativo x expiração lazy (Aceite06/Aceite09)

O script demo (`database/pgadmin/administrativo360_demo_completo.sql`) semeia prazos relativos ao
momento da aplicação (`v_now + 48/72h`). Em 2026-10-06 esses prazos já haviam vencido; a expiração
lazy do B7 passou a expor o estado real (`EXPIRADA` na leitura) e os dois testes — um filtrando
cotação `EM_RELACIONAMENTO` pendente, outro esperando o guard de resposta concluída — falharam.
**Decisão: o comportamento permanece** (é a intenção do B7 — prazo vencido é estado visível, não
fantasma); os testes declaram o próprio pré-requisito com `RenovarSeedOperavelAsync` (renova
prazo+status da linha específica pelo identificador; a Cotação 3, EXPIRADA por construção do seed,
não é tocada). Backlog (§5.1): renovação centralizada do seed — a linha v2 da Cotação 4
(`+36h`) envelhecerá da mesma forma em execuções futuras.

## 4. Evidência de execução

### 4.1 Matriz T01–T18 (suíte viva contra `plantaopro_test`)

| Fato | Cenário | Observado |
|---|---|---|
| `B7/T01` | aceitar → estornar → reaproveitar a MESMA resposta → retransmitir (2 tentativas) → RESPONDIDA; auditoria de eventos (APROVACAO c/ chave + reuso sem chave, ESTORNO, 2× RETORNO_EXTERNO) | ✅ passa |
| `B7/T02` | estorno bloqueado durante ENVIANDO (transitório, gate liberando para RESULTADO_DESCONHECIDO) e liberado após a conciliação | ✅ passa |
| `B7/T03` | motivo do cancelamento e justificativa do estorno validados ANTES de tocar no banco | ✅ passa |
| `B7/T04` | cancelar cotação RECEBIDA: CANCELADA + motivo + momento; repetir é idempotente (1 evento com chave, sem sobrescrever) | ✅ passa |
| `B7/T05` | cancelamento bloqueado em situação terminal (RESPONDIDA) | ✅ passa |
| `B7/T06` | cancelamento bloqueado com resposta ENVIANDO (transitório) e liberado após a conciliação | ✅ passa |
| `B7/T07` | prazo excedido marca EXPIRADA na leitura (lazy) e a operação é idempotente | ✅ passa |
| `B7/T08` | transmissão bloqueada em cotação CANCELADA antes de abrir qualquer envio | ✅ passa |
| `B7/T09` | transmissão bloqueia em prazo excedido (EXPIRADA marcada) ANTES de abrir envio; resposta intacta (0 envios) | ✅ passa |
| `B7/T10` | anti-duplicidade sequencial na aprovação — mesmo ID, 1 linha, evento de criação com chave + reuso sem chave | ✅ passa |
| `B7/T11` | segunda aprovação após aceite bloqueada pela situação terminal (RESPONDIDA), sem duplicar resposta | ✅ passa |
| `B7/T12` | estorno isolado de resposta ACEITA — preserva protocolo/tentativas/enviado_em + apêndice de histórico | ✅ passa |
| `B7/T13` | ciclo completo de triagem — abrir → listar → reabrir → vencida → resolver (sai da quarentena, conferência intacta, 3 eventos) | ✅ passa |
| `B7/T14` | guards negativos da triagem — resolver sem responsável, responsável de outro tenant, prazo no passado, fora de quarentena | ✅ passa |
| `B7/T15` | lista de triagens ordena por prazo (menor primeiro) e deriva a flag de vencida | ✅ passa |
| `B7/T16` | revisão de orçamento gera -V2 + EM_ORCAMENTO; aprovação segue pela transição EM_ORCAMENTO→PRONTA_PARA_ENVIO; revisão fora de fase bloqueia | ✅ passa |
| `B7/T17` | finalização honesta — resposta conciliada por outro escritor durante a transmissão não é sobrescrita; envio loga o retorno real + cotação não vira RESPONDIDA | ✅ passa (pegou o defeito §3.2 na 1ª rodada) |
| `B7/T18` (unit) | matriz de transições — EM_ORCAMENTO→PRONTA_PARA_ENVIO liberada; terminais e saltos inválidos bloqueiam | ✅ passa |

Metodologia: os três stubs de conector exercitam os três modos de vida externa — sucesso fixo,
sucesso adiado por válvula (20 s) p/ testar o estado transitório ENVIANDO, e concorrente honesto
(UPDATE direto na linha durante a chamada). Isolação por sufixo GUID (conta `CONTAB7-*`, cotação
`B7T-*`), coleção `A360Transmissao` serializando com as demais classes que escrevem `ENVIANDO`,
cleanup escopado em `finally` (filhos primeiro; `session_replication_role='REPLICA'` só para os
eventos imutáveis via GUC de bypass ADM360).

### 4.2 Suíte canônica integral

Três execuções consecutivas de `dotnet test backend\PlantaoPro.Tests\PlantaoPro.Tests.csproj
--nologo -v q` contra `plantaopro_test`:

| Rodada | Resultado | Duração |
|---|---|---|
| 1 | **996/996 aprovado**, 0 falhas | ~37 s |
| 2 | **996/996 aprovado**, 0 falhas | ~23 s |
| 3 | **996/996 aprovado**, 0 falhas | ~24 s |

Baseline no B6: 978/978. Diferença **+18** = exatamente os novos fatos T01–T18.
(Primeira tentativa da suíte com B7: 4 falhas em 996 — as regressões §3.3/§3.4; fechadas antes
da contagem acima.)

### 4.3 Build

Solução completa: 0 erros (warnings de estilo pré-existentes inalterados; 2 warnings novos no
arquivo de testes resolvidos no mesmo turno).

## 5. Classificação e limitações

**Classificação B7:**

| Item | Classificação |
|---|---|
| Cadeia cotação→estorno + triagem + finalização honesta + v2314 + 18 fatos | **APROVADO** — evidência de execução completa (18/18 + 996×3), pronto tecnicamente |
| Aceite funcional contra portais OPMENEXO/INPART reais | pendente de homologação (stubs aqui; conector real exige credenciais — sem credencial, homologação externa não é declarada) |
| Aplicação de v2314 no `plantaopro` (prod local) | **PENDENTE JANELA A2/F3** (junto de v2312/v2313 — banco prod ainda sem as tabelas ADM360) |

**Limitações honestas (backlog):**

1. **Renovação centralizada do seed demo.** Esta rodada renova apenas as linhas usadas pelos
   Aceite06/09. A Cotação 4-v2 (prazo `+36h`) e outras linhas operáveis envelhecerão; qualquer
   leitura futura do B7 vai marcá-las EXPIRADA — correto como comportamento, indesejável como
   fixture. Prever refresh idempotente (script pgadmin com UPDATE de linhas `DADOS_DE_TESTE` não
   terminais, ou garantia de seed nos testes).
2. **Auditoria DateOnly completa (Npgsql 10)** segue aberta desde o B6 (item 1 lá); os fluxos B7
   usam `timestamptz`/cast `::timestamp` nas leituras novas, o risco concentra-se em outros módulos.
3. **Botões JS da triagem/estorno no Web** (banner aplicado; ações de triagem ainda por wirear na
   view) entram com o C11 (design/teclado/toque/foco).
4. **Recovery cíclico** é exercitado indiretamente (skip em Testing + reconciliação por tick);
   o cenário "boot encontra órfão ENVIANDO do processo anterior" ainda depende de homologação viva
   IIS (P0–P7).
5. **Asimetria AGENDADO** na decisão de solicitações (B6 §5.2) continua em aberto.
6. **Flake catalogado no B6 §5.5** continua sob observação (não repetiu nas 6 execuções desta rodada).
7. **Sem promessa de ausência absoluta de bugs.** A suíte cobre os cenários acima contra
   `plantaopro_test`; caminhos sem teste vivo continuam sujeitos a defeito latente.

**Fora do escopo B7 (estado geral da entrega, sem mudança nesta rodada):** B8 (Saúde 360),
B9 (Plantões/Meu Dia/BI), B10 (IA — segue BLOQUEADO por credencial real), C11 (design), emissão
fiscal (segue BLOQUEADA por decisão comercial). Push do repositório continua condicionado a
decisão explícita (HEAD `0cd03ab`, 8 commits à frente de `origin/main` = `c84afc5` após este commit).
