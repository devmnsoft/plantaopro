# Rodada 16 — r16b1 - Fix F-R14-C do parser Web + contrato de binding (12 fatos)

**Data:** 2026-10-17 · **Base:** `5e31c78` (r16a0) · **Escopo:** topo da fila R16 — implementação do plano r16a0 (fix em um ponto no parser + limpeza D2) com os fatos de contrato, mais 4 bugs reais F-R15-A encontrados pela varredura refinada do D6 durante a execução. Sem DDL, sem resx, sem novas literais de UI.

## 1. Achados novos da fatia (fora do plano r16a0)

| # | Achado | Evidência |
|---|--------|-----------|
| F9 | **O scan refinado do D6 (por-form, só escalares NÃO-nuláveis implícitos) encontrou 4 binds mortos reais** — a mesma família do typo `efetivoFisico` da R15, todos em fluxo financeiro vivo do Adm360, todos dead-on-arrival (parâmetro não-nulável sem campo correspondente ⇒ `[Required]` implícito NRT ⇒ `ModelStateInvalidoFiltro` redireciona antes da ação, e a view não renderiza TempData ⇒ botão que "não faz nada"): (a) **PagarTitulo** — hidden `name="tituloId"` vs parâmetro `Guid id`, E input `name="valor"` vs parâmetro `decimal valorPago`; (b) **FecharCaixa** — select `name="contaId"` vs parâmetro `Guid contaFinanceiraId`; (c) **ReabrirCaixa** — hidden `name="id"` vs parâmetro `Guid fechamentoId`. Fix = rename do atributo `name` nas views (IDs e JS intocados — verificado que nenhum script lê o `name` antigo). | `Views/Administrativo360/TituloPagarDetalhes.cshtml` L287/L314; `Views/Administrativo360/FechamentoCaixa.cshtml` L158/L209; medidor `%TEMP%\opencode\r16b1_fieldscan_v6.ps1` (antes: 10+1 violações; depois: 0) |
| F10 | **Correção ao F4 do r16a0:** `ListPageViewModel.InfoMessage` **não é renderizado por nenhum layout de lista** (grep `InfoMessage` nas Views: só `ConvitesEquipe/Index` e `Onboarding/Index`, que usam viewmodels próprios). O "banner azul Sucesso" das listas genéricas nunca existiu de fato — o campo era morto. A limpeza D2 segue válida e vira literal `null`. | grep `InfoMessage` em `PlantaoPro.Web/Views/**/*.cshtml`; `Views/Especialidades/Index.cshtml` (renderiza só Items/erros de item) |
| F11 | **Baseline versionado do source-scan (D6):** família `Administrativo360Controller*.cs` × views `Views/Administrativo360/**` — **54 formações action×form / 141 parâmetros escalares verificados / 0 violações** após os 4 renames. Regras finais: só escalares não-nuláveis sem source attribute (string/Guid/int/long/double/float/decimal/bool/DateOnly/DateTime, incl. arrays); isenta parâmetros c/ default, `CancellationToken` e atributos `FromRoute/FromHeader/FromForm/FromQuery`; isenção por rota via `asp-route-<nome>` na tag do form. 6 actions sem form estaticamente mapeável (JS monta a URL no modal Inativar*/cancelamentos) ficam em whitelist nomeada que o teste trava: qualquer action nova fora do mapeamento falha o teste de propósito. Antivacuidade: ≥40 pares e ≥120 parâmetros por execução. | `R16ScanFormBindingFonteTests` (novo); medidor PS idem `%TEMP%\opencode\r16b1_fieldscan_v6.ps1` |
| F12 | **Gotchas do .NET 10 no harness de controller sem host** (precisaram ser sondados contra o runtime pack 10.0.13 — o repo roda net10.0 com LangVersion travado em C# 10, sem raw strings): (a) `ITempDataProvider` saiu do `Microsoft.AspNetCore.Mvc` para `Microsoft.AspNetCore.Mvc.ViewFeatures` (e a interface mudou para `LoadTempData(HttpContext)`); (b) o caminho atual de `Controller.TempData` resolve **`ITempDataDictionaryFactory` → `ITempDataDictionary`** (Load/Save/Keep/Peek sobre `IDictionary<string, object?>`) via `RequestServices`; (c) `HttpContext.Session` agora **lança** `InvalidOperationException("Session has not been configured…")` se a **`ISessionFeature`** (feature da request, ns `Microsoft.AspNetCore.Http.Features`) não existir — `IHttpSessionFeature`/`HttpSessionFeature` deixaram de existir; (d) `ISession` foi simplificado: `IsAvailable/Id/Keys/LoadAsync/CommitAsync/TryGetValue(string, out byte[]?)/Set(string, byte[])/Remove/Clear` (sem Start/Expires). O harness do b1 implementa tudo em memória e fica como receita de casa para tests futuros de parser/BFF sem host. | `R16WebParserErrorContratoTests.cs` §harness (classes `SessaoFeatureSonda`/`SessaoMemoria`/`DicioTempDataFactory`/`DicioTempData`) |

## 2. Changes aplicadas

| Arquivo | Change |
|---|---|
| `backend/PlantaoPro.Web/Controllers/BaseWebController.cs` | L302 (ramo 2xx de `ReadApiResponsePayloadAsync`): `TryParseMessage(content)` → **`null`** + comentário do achado F-R14-C/D1 citando o precedente `ReadApiPagedResponseAsync`. Único ponto: cobre `ReadApiResponse`/`SendApiAsync`/`SendApiWithoutResponseAsync`/`DeleteApiAsync`. |
| `backend/PlantaoPro.Web/Controllers/WebListControllerExtensions.cs` | `RenderList`/`RenderPaged`: `InfoMessage: result.StatusCode == OK ? result.Error : null` → `InfoMessage: null` literal + comentário D2 (campo morto, F10). |
| `backend/PlantaoPro.Web/Views/Administrativo360/TituloPagarDetalhes.cshtml` | 2 renames de binding (F9a): hidden `tituloId` → `id`; input `valor` → `valorPago` (ids `valorPago`/label intactos). |
| `backend/PlantaoPro.Web/Views/Administrativo360/FechamentoCaixa.cshtml` | 2 renames de binding (F9b/F9c): select `contaId` → `contaFinanceiraId` (modal FecharCaixa); hidden `id` → `fechamentoId` (modal ReabrirCaixa). |
| `backend/PlantaoPro.Tests/R16WebParserErrorContratoTests.cs` (novo) | Os 12 fatos do §3, com harness mínimo de `BaseWebController` sem host (F12) e source-scan versionado (F11). |

## 3. Fatos implementados (contrato permanente)

Classe `R16WebParserErrorContratoTests` — harness: subclass mínimo do `BaseWebController` + `StubApiHandler` próprio por fato + `ISessionFeature`/`ITempDataDictionaryFactory` em memória (F12). Sem host, sem coleção, sem estado compartilhado.

1. `GET_2xx_envelope_sucesso_error_nulo_e_dados_deserializados` — envelope padrão `"Sucesso"` ⇒ `Error == null`, `Data` ok (o leak morto).
2. `GET_2xx_mensagem_custom_significativa_tambem_eh_descartada_do_error` — `"Check-in registrado."` ⇒ `null` (descarte uniforme, não só o literal).
3. `GET_2xx_array_puro_error_nulo_regressao_caminho_adm360` — array cru ⇒ `null`, itens preenchidos.
4. `GET_2xx_objeto_puro_com_title_nao_vaza_para_error` — DTO sem envelope c/ `title` ⇒ `null` (bônus F1: `TryParseMessage` olhava `title`).
5. `GET_2xx_envelope_sem_sucesso_preserva_mensagem_do_envelope` — 200 c/ `success:false` ⇒ `Error` = msg do envelope (preservado).
6. `GET_404_repassa_mensagem_da_api` — não-2xx inalterado.
7. `GET_500_mantem_literal_fixo_e_descarta_mensagem_interna` — D4/F-R14-D segue P3 (premissa refinada do F6b).
8. `POST_2xx_sucesso_error_nulo_escrita_passa_pelo_mesmo_parser` — caminho de escrita coberto (201).
9. `POST_falha_de_comunicacao_escreve_tempdata_e_retorna_erro_de_comunicacao` — assimetria D3 preservada (catch de escrita escreve).
10. `GET_falha_de_comunicacao_retorna_erro_sem_escrever_tempdata` — leitura não escreve TempData (D3, outra face).
11. `RenderList_2xx_info_message_nula_e_lista_preenchida` — D2 visível de fora: model da view com `InfoMessage`/`ErrorMessage` nulos e itens ok.

Classe `R16ScanFormBindingFonteTests`:

12. `acoes_post_adm360_parametros_escalares_implicitos_existem_no_form_que_as_aponta` — o F-R15-A vira contrato: todo escalar não-nulável implícito de `[HttpPost]` Adm360 deve existir como `name="…"` ou `asp-route-…` DO FORM que aponta para a ação (mesma mecânica que matou os 4 bugs F9); trava whitelist das 6 actions JS-driven e antivacuidade ≥40 pares / ≥120 parâmetros (baseline 54/141).

## 4. Gate

- Build Release completa `--no-incremental`: **0 erros, 80 avisos = idêntico ao baseline r16a0** (diff linha-a-linha dos avisos confirma zero novos; os 4 avisos transientes do desenvolvimento do harness — CS8767/CS8602/CS8604 no `R16WebParserErrorContratoTests.cs` — foram eliminados, não silenciados).
- Suíte full `dotnet test backend\PlantaoPro.Tests -c Release --no-build`: **1475/1475 aprovados, 0 falhas**, 34 s (baseline r16a0: 1463 + 12 novos) — executada duas vezes (antes e depois da limpeza de nullabilidade), ambas verdes. I18n 653 intacto (fatos R7DF6 dentro da suíte verde — D7).
- Família ① (40P01): não apareceu nas execuções do gate (regra: isolar 5× antes de tratar como regressão — não acionou).
- Run isolado prévio dos 12 fatos: 12/12.

## 5. Divergências do plano r16a0 (documentadas)

- **9 fatos planejados → 12 implementados** (+POST 2xx sucesso, +face GET do catch, +source-scan ganhou classe própria).
- **4 renames de view além das 2 edições previstas** (F9) — dentro do espírito do item ("adesão preventiva ao padrão F-R15-A"), mas achados de produção, não do plano.
- **D2 reformulado pelo F10:** o banner azul nunca renderizava; a motivação passa a ser "literal nulo + campo morto documentado".
- Escopo do scan definido como família `Administrativo360Controller*.cs` com views `Views/Administrativo360/**` (a família `Adm360*WebController*` usa paths explícitos `~/Views/Administrativo360/<pasta>/…` por ação — extensão do scanner fica para fila R17 junto com os fantasmas do catálogo).

## 6. Restante da rodada

- **r16b2 (opcional):** smoke vivo de tela atingível (Minha Assinatura / Minha Agenda) só se acessível ao login SV com subscription ativa; senão encerra nos fatos (classe já homologada ao vivo na R14). O F9 pode ganhar smoke vivo se houver acesso ao Financeiro Adm360 no tenant SV (Pagar Título / Fechar Caixa).
- **r16z:** consolidado + fila R17 (itens 2–12 do r15z; F-R14-C baixado; F-R14-D premissa refinada; extensão do scanner para `Adm360*WebController*`).
- Push da rodada (inclui r16a0) só com autorização explícita ao fim.
