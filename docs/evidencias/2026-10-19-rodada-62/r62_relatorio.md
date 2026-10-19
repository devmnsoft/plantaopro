# R62 — Provedor IA Drael dedicado (fila r53b1 §7 — "coloque a sua IA")

Data: 2026-10-19 · Base: `51f9cc0` (R61 fechada) · Suíte: **1639 → 1643** (+4 fatos)

## Decisão (D-R62-1)

Drael (drael.sh) entra como **provider dedicado** na camada canônica de assistentes
(`PlantaoPro.Api/Ai`), seguindo o MESMO contrato dos três provedores existentes —
nada inventado fora do padrão:

| Camada | O que entrou |
|---|---|
| `AiContracts.cs` | `AiProviderCodes.Drael = "drael"` + inclusão no conjunto `All` |
| `AiProviderAdapters.cs` | `DraelAdapter : OpenAiCompatibleAdapter` (client `AiDrael`, chave em `Ai:Providers:Drael:ApiKey`) |
| `AiGateway.cs` | modelo padrão do provedor `drael-v1`; mensagem de erro lista `groq, gemini, deepseek ou drael` |
| `AiModeloCompatibilidade.cs` | governança: exige prefixo `drael-` (bloqueante); catálogo documentado `drael-v1` (aviso não bloqueante) |
| `Program.cs` | `AddHttpClient("AiDrael")` base `https://drael.sh/v1/` (sobrescritível por config) + singleton do adapter |
| UI `AssistenteIa/Index.cshtml` | `drael` **lidera** a lista de provedores (principal e reserva) com rótulo "Drael — IA da casa"; os demais viram nomes legíveis (Groq/Gemini/DeepSeek) |
| Banco `2026_10_v2356_r62_ai_provedor_drael.sql` | estende `CHECK(provedor IN …)` em `ai_config` (principal + fallback) e em `ai_precos_modelos` com `'drael'` |

Wire format confirmado pelos docs de drael.sh (consultados em 2026-10-19):
compatível com OpenAI — `POST v1/chat/completions`, Bearer `dk-…`, modelo `drael-v1`.

Contratos preservados pelo desenho (mesma "higiene" dos outros providers):
- chave do tenant tem prioridade sobre a global do servidor;
- a chave viaja SÓ no cabeçalho `Authorization`, nunca no corpo/URL/log;
- classificação canônica de falhas (`PROVEDOR_LIMITADO` no 429, `RESPOSTA_INVALIDA`, `TRANSPORTE`, `TIMEOUT`);
- auditoria em `ai_usos` idêntica (provedor, modelo, tokens, classe de erro);
- cota mensal/orçamento/limite de testes de conexão se aplicam como aos demais;
- preço do `drael-v1` fica versionado em `ai_precos_modelos` quando publicado pela
  operação (sem preço vigente o sistema já trata como **custo incerto** — nada inventado).

Sem segredo nenhum no código-fonte: a chave vem de config/ambiente
(`Ai:Providers:Drael:ApiKey`), como os demais.

## Evidências executáveis

- Contrato próprio: `backend/PlantaoPro.Tests/R62ProvedorDraelContratoTests.cs` (4 fatos):
  1. registro em `AiProviderCodes`/`Program.cs`/UI/migration/manifest/scripts completos;
  2. wire format real sobre mock HTTP (endpoint `…/v1/chat/completions`, Bearer da
     chave do tenant, corpo com `"model":"drael-v1"`, uso/token parsing, 429→limitado,
     corpo inválido→resposta inválida);
  3. governança de modelo (bloqueante por prefixo + aviso não bloqueante);
  4. gateway ponta a ponta no banco real de teste: salvar config, executar tarefa
     (sucesso, auditoria em `ai_usos` com `provedor='drael'` e `status='SUCESSO'`),
     teste de conexão ok, modelo de concorrente rejeitado no save.
- Migration aplicada em `plantaopro_test` e `plantaopro` (dev) via psql;
  scripts completos (`scrpt_completo.sql`, `pgadmin/instalar_no_banco_atual.sql`)
  anexados com bloco `SOURCE/SOURCE-SHA256` (SHA256 `48ef52…003f`) e
  `migration-manifest.json` + `source-checksums.json` atualizados
  (integridade travada por `DatabaseGeneratorIntegrityTests`).
- Mini e2e: `r62_e2e.ps1` (BOM UTF-8) — login demo + `/AssistenteIa`:
  `ASSISTENTE_IA_OK drael_selects=5 drael_label=4 pos_drael=29434 pos_groq=29528 FALHAS=0`.

## Gates (todos verdes)

| Gate | Resultado |
|---|---|
| Build Release `--no-incremental` | exit 0 (log `%TEMP%\opencode\r20b1_build.log`) |
| Gate de casa `r20b1_gate.ps1` | `Now=160 Base=160 DIFF=NENHUMA erros=0` |
| Dirigidos R62+IA+integridade DB | 37/37 |
| Suíte integral (apps parados) | **1643/1643** (falha transitória conhecida `R19DapperBindingGuardTests.Db_CentralClientes_…` sob carga → 10/10 isolado; registro para R65/item 10) |
| Smoke R52 e2e | FALHAS=0 (PERF média 150 ms) |
| Mini e2e R62 | FALHAS=0 |

## Backlog / notas

- Preço `drael-v1`: sem tabela pública de preços por token na consulta de 2026-10-19
  (planos mensais Max/Pro); quando o valor for publicado, inserir linha em
  `ai_precos_modelos` (constraint já permite `'drael'`).
- Quirk do ambiente capturado: nesta máquina, PS 5.1 trata `null` (sem `$`) como
  cmdlet em script file — usar sempre `$null` nos scripts de evidência.
