# Rodada 16 — r16a0 - Auditoria inicial e plano de sprint

**Data:** 2026-10-17 · **Base:** `f9c1128` (`origin/main` = ponta do push da Rodada 15) · **Escopo:** topo da fila R15z — **P2 F-R14-C**: uniformizar a exposição de erro dos parsers Web (leak da `message` do envelope em 2xx vazando como `Error`), com adesão ao padrão F-R15-A (nomes de campo × `[Required]` implícito) como fato de contrato preventivo. Fora de escopo (seguem na fila): F-R14-D (intenção dos 500), F-R14-B (paginação/scoping), fantasmas do catálogo, drill-downs, menu, demais P3/P5.

## 1. Estado atual (fatos com evidência)

| # | Fato | Evidência |
|---|------|-----------|
| F1 | **O leak, fixado por fonte:** no caminho de sucesso (2xx + envelope ok), o parser devolve `Error = TryParseMessage(content)` — e `TryParseMessage` lê a propriedade `message` do body primeiro (depois `mensagem`/`error`/`detail`/`title`). Com o envelope padrão da API, todo GET/POST bem-sucedido devolve `Error = "Sucesso"`. É a única linha que produz o leak; o caminho `!IsSuccessStatusCode` e o envelope-fracasso já se comportam certo. | `backend/PlantaoPro.Web/Controllers/BaseWebController.cs` L302 (return do 2xx), L210–258 (`TryParseMessage`), L269–314 (`ReadApiResponsePayloadAsync`) |
| F2 | **Amplitude da superfície:** `ApiResponse.Ok(data, message="Sucesso")` é o padrão de casa (~90 arquivos de API retornam envelope); os endpoints suspeitos confirmam shape `StatusCode(result.StatusCode, result)` em 2xx com `message` presente — inclusive mensagens 2xx significativas que ninguém lê como info: `"Plantão confirmado"`, `"Check-in registrado."`, `"Solicitação cancelada; os horários registrados foram preservados."`. | `backend/PlantaoPro.Api/Models.cs` L4; `Controllers/MedicoAreaController.cs` (toda a classe); `Controllers/SelfServiceSaasController.cs` L396–443; `ProfessionalPortalService.cs` L99/L119/L177 |
| F3 | **Consumidores que EXISTEM lendo `.Error` em 2xx como erro — atingíveis hoje:** Minha Assinatura (`Index`/`Uso`/`Limites`/`Faturas`: `model.ErrorMessage ??= catalog.Error ?? requests.Error`, `ErrorMessage = result.Error ?? …`), Minha Agenda (viewmodels diretos `result.Error` em agenda/detalhe/pagamentos/presenças), Saude360 (prontuário/timeline), Central Atendimento L23, Convites Equipe L58, Onboarding L68, Conferência Execução L38, Dashboards Premium L37. A classe foi já reproduzida AO VIVO na R14 no banner de Faturamento SaaS (fix local D6). | grep `\.(Error|erro)\b` em `PlantaoPro.Web/Controllers/**` (censo: 34 arquivos, ~140 usos — tabela de classificação abaixo) |
| F4 | **Consumidor protegido da mensagem 2xx:** `WebListControllerExtensions.RenderList/RenderPaged` usa `result.Error` em 2xx como **InfoMessage** (`InfoMessage: status==OK ? result.Error : null`) — banner azul "Sucesso" nas listas genéricas; é o único lugar onde a mensagem 2xx é lida propositalmente (o restante do uso de `InfoMessage` no Web vem de literais fixos ou estático). O mesmo helper já guarda o slot de ERRO por status (`errorMessage = status==OK ? null : result.Error`). | `Controllers/WebListControllerExtensions.cs` L28/L33, L56/L61; grep `InfoMessage` no Web |
| F5 | **Consumidores imunes (classificados, ~90% dos usos):** guarda por `StatusCode` (Adm360: `if (resp.StatusCode is OK/Created)… else TempData["Error"] = resp.Error ?? fallback`), por flag `Success` (`SendApiWithoutResponseAsync`: LGPD, Comercial, Hospitais, Médicos, Especialidades, Plantões), por `Data is null` (V1420 `Acao`, MinhaAssinatura L113) ou o endpoint é DTO puro sem envelope (toda a família Adm360 — `Ok(list)` cru; em 2xx o `TryParseMessage` retorna nulo hoje e pós-fix também). FaturamentoSaas já tem a imunização local da D6 (R14). | Amostra lida: `Administrativo360Controller.{ComprasRecebimentos,Cadastros,Financeiro}.cs`, `MinhaAssinaturaController.cs`, `LgpdController.cs`, `HospitaisController.cs`, `V1420OperationalControllers.cs` L29 |
| F6 | **Assimetrias documentadas (mudam NESTA rodada apenas se barato e sem risco):** (a) catch de comunicação: `ReadApiResponseAsync` NÃO escreve `TempData`; `SendApiAsync`/`DeleteApiAsync` ESCRIVEM — mantido de propósito, pois existem consumidores que descarteiam a tupla (`var (_, _, status) = await SendApiAsync…`) e dependem do `TempData` do parser na falha; (b) `BuildApiErrorMessage(500)` DESCARDA a `apiMessage` mesmo no caminho paginado (`ReadApiPagedResponseAsync` L162/L169 passam a mensagem mas o switch de 500 retorna literal fixo) — **refina a premissa do F-R14-D** (a assimetria simples×paginada existe nos 4xx, não nos 500); (c) `ReadApiPagedResponseAsync` já devolve `Error=null` em 2xx (precedente canônico do alvo do fix). | `BaseWebController.cs` L85–89 vs L110–113/L136–140; L158–170; L173; L515–525 |
| F7 | **F-R15-A medido (adesão):** scan automatizado de parâmetros escalares de actions `[HttpPost]` da família Administrativo360 × nomes de campo das views homônimas → **0 mismatch real** hoje (5 hits falsos = overloads GET/POST com parâmetro complexo `model`/`form`, que bindam por propriedade sem prefixo). O typo `efetivoFisico` da R15 só não existe mais porque o contrato web-bff o pegou; sem teste permanente a invariante depende de sorte. | Script `%TEMP%\opencode\r16a0_fieldscan.ps1` (reexecutável; saída: `CirurgiaNova(model)`, `NovaDespesa(model,idempotencyKey)`, `OrcamentoNovo(form)`, `OrcamentoEditar(form)`, `ValeNovo(model)` — todos complexos) |
| F8 | **Baseline da base `f9c1128`:** build Release `EXIT 0` (0 erros); suíte full **1463/1463 aprovados, 0 falhas** em 34 s — flake família ① não apareceu (regra: isolar 5× antes de tratar como regressão). | `%TEMP%\opencode\r16a0_build.log`, `r16a0_suite.log` |

## 2. Decisões de design

- **D1 — Semântica canônica do parser: `Error` não-nulo ⟺ a chamada NÃO deu certo.** No caminho 2xx+sucesso a `message` do envelope é descartada (é chapa da API, não erro); em envelope-fracasso (!success), 4xx/5xx, desserialização ruim e falha de comunicação, `Error` mantém o valor de hoje. Implementação em **um ponto**: o return do ramo 2xx de `ReadApiResponsePayloadAsync` passa a devolver `null` no lugar de `TryParseMessage(content)` (+ comentário do achado). Cobre `ReadApiResponse`/`SendApiAsync`/`SendApiWithoutResponseAsync`/`DeleteApiAsync` de uma vez; `ReadApiPagedResponseAsync` já está em conformidade (sem change) e vira o precedente citado no comentário.
- **D2 — Zero mudanças em consumidores de erro** (o censo F3/F5 prova que nenhum consome a mensagem 2xx como erro de forma útil — as telas atingidas exibem hoje justamente o defeito). Única limpeza: `RenderList`/`RenderPaged` passam a `InfoMessage: null` literal (morte silenciosa pós-D1) com comentário apontando a D1 — os banners azuis "Sucesso" somem, que é exatamente o ruído da classe do bug da R14.
- **D3 — Assimetria de catch preservada** (F6a): parsers continuam escrevendo `TempData["Error"]` apenas nas falhas (não-2xx/exception) e nunca em 2xx; consumidores que descarteiam a tupla seguem cobertos.
- **D4 — `BuildApiErrorMessage` intocado nesta rodada** (fila P3 item #5, agora com a premissa refinada do F6b: decidir a intenção dos 500 é decisão separada de produto/suporte).
- **D5 — Imunização local da D6 (R14) mantida** no `FaturamentoSaasController` (já correta; o censo mostra o padrão de escolha por status valendo como referencial de casa para ações futuras).
- **D6 — Contrato F-R15-A permanente (novos fatos de source-scan):** parâmetros escalares de actions `[HttpPost]` da família Administrativo360 devem aparecer como nome de campo na view homônima (comparação case-insensitive; ignora parâmetros complexos e `CancellationToken`; isenta o antiforgery). Baseline do scan: 0 violações — o teste trava a invariante contra a próxima digitação.
- **D7 — Sem novas literais de UI** ⇒ sem bump de resx (contrato i18n de 653 chaves segue intacto — verificado no b1 pelo teste R7DF6).

## 3. Plano de fatos (contrato da fatia r16b1) — novo arquivo `R16WebParserErrorContratoTests.cs`

1. **GET 2xx envelope padrão** `{"success":true,"message":"Sucesso","data":…}` ⇒ `Error == null` e `Data` deserializado (o leak morto).
2. **GET 2xx mensagem custom significativa** (`"Check-in registrado."`) ⇒ `Error == null` (descarte uniforme, não só o literal "Sucesso").
3. **GET 2xx array puro** ⇒ `Error == null` (guarda de regressão do caminho comum Adm360).
4. **GET 2xx objeto puro c/ propriedade `title`/`message`** (DTO sem envelope) ⇒ `Error == null` — hoje vazava o título (bônus do F1: `TryParseMessage` olhava `title`).
5. **2xx envelope com `success:false`** ⇒ `Error` continua a mensagem do envelope (comportamento existente preservado).
6. **Não-2xx inalterados:** 404 c/ mensagem ⇒ passa a mensagem da API; 500 ⇒ literal fixo de instabilidade (prova de que o D4 segurou a linha).
7. **Exception de comunicação (handler estourado)** ⇒ `Error == "Falha de comunicação com a API."`; variante POST ⇒ `TempData["Error"]` escrito (assimetria D3 preservada).
8. **web-bff `RenderList`:** 2xx ⇒ `InfoMessage == null` e `ErrorMessage == null` (limpeza D2 visível de fora).
9. **Source-scan F-R15-A (D6):** 0 parâmetros escalares de `[HttpPost]` Adm360 sem campo correspondente na view homônima (mesma heurística do `r16a0_fieldscan.ps1`, versionada).

Gate previsto: 1463 + 9 = **1472** (contagem exata fechada no b1; i18n segue 653).

## 4. Plano de fatias

- **r16b1:** implementação mínima (L302 + comentário; 2 linhas de `InfoMessage` em `WebListControllerExtensions`) + os 9 fatos acima + gate full. Sem DDL, sem resx.
- **r16b2 (opcional, depender do b1):** smoke vivo breve de UMA tela atingível com subscription ativa no tenant SV (ex.: Minha Assinatura ou Minha Agenda) para fotografar o banner "Sucesso" indo embora — somente se o módulo for acessível ao login SV; senão o veredicto encerra nos fatos web-bff (classe já homologada ao vivo na R14).
- **r16z:** consolidado + fila R17 (itens 2–12 do r15z, com F-R14-C baixado e a premissa do F-R14-D refinada).

## 5. Arquivos previstos (r16b1)

| Arquivo | Change |
|---|---|
| `backend/PlantaoPro.Web/Controllers/BaseWebController.cs` | L302 → `null` no Error do 2xx + comentário do achado F-R14-C/D1 |
| `backend/PlantaoPro.Web/Controllers/WebListControllerExtensions.cs` | `InfoMessage: null` (L33/L61) + comentário D2 |
| `backend/PlantaoPro.Tests/R16WebParserErrorContratoTests.cs` (novo) | os 9 fatos do §3 (parser via subclass mínimo de `BaseWebController` + `HttpMessageHandler` stub; web-bff para `RenderList`; source-scan para F-R15-A) |

## 6. Riscos / fora de escopo

- **Risco aceito:** banners azuis "Sucesso" em listas genéricas somem (cosmético; era ruído/defeito). Nenhuma mensagem 2xx significativa era exibida em slot útil hoje (censo).
- **Fora de escopo:** F-R14-D/B, fantasmas do catálogo, drill-downs, menu, Meu Dia UTC, tile B18, especialidade, pg_dump (fila R17).
