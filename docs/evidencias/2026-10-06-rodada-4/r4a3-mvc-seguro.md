# R4-A3 — MVC seguro: destino local validado no filtro de ModelState (sem open redirect)

Data: 2026-10-06 · Base: `c84afc5` + `cdd517b` (D1) + `5166bb6` (R4-A2) · Suíte canônica antes desta rodada: 930/930.

## 1. Requisito e decisão de design

Quando qualquer campo falha no binding (decimal ambíguo "1.234", valor fora de `numeric(18,4)`, campo ausente), a ação **não executa** e **não chama a API/banco** — sem o filtro, MVC clássico liga o decimal como `default(0)` e o fluxo financeiro prossegue em silêncio (bug M2.5). O destino do retorno **não é confiado ao header `Referer` cru** (open redirect):

| Tipo de chamada | Comportamento em ModelState inválido |
|---|---|
| BFF (`/bff/*`) | **JSON 400** com envelope canônico `{status, reason:"DADOS_INVALIDOS", message}` — consumidores de API nunca recebem redirect HTML nem `problem+json` |
| GET | Redirect para a **própria URL** da requisição (`PathAndQuery` preservado): filtros/seletores do usuário permanecem na URL; a ação não executou |
| POST form (MVC) | Redirect para o `Referer` **apenas se local** (mesmo scheme + host + porta da requisição). Referer ausente, URL não absoluta ou origem externa → **400 direto** |

Mensagem humana em `TempData["Error"]` (mesmo storage que as views leem). Sem aumento de timeout; nada de ÷100; nenhum valor digitado é "corrigido" — só rejeitado ou aceito.

**Preservação dos valores digitados:** entregue em nível de mecanismo (GET preserva query string; POST usa PRG segura). A **reidratação campo-a-campo por view** depende das views individuais e fica registrada como backlog (§6) — não foi prometida como universal nesta rodada.

## 2. Implementado

1. `backend/PlantaoPro.Web/Services/Mvc/ModelStateInvalidoFiltro.cs` — reescrito: três ramos acima; `DestinoLocalValidado(HttpRequest)` estática e coberta por unidade.
2. `backend/PlantaoPro.Web/Services/Security/BffContracts.cs` — constante `RazaoDadosInvalidos = "DADOS_INVALIDOS"` (envelope canônico já existia via `RespondAsync`).
3. `backend/PlantaoPro.Web/Controllers/MvcSeguroTestController.cs` — novo: **exclusivo de Testing** (a ação responde 404 fora dele): `__test/mvcfiltro/get`, `__test/mvcfiltro/post` (antiforgery) e `MvcSeguroTestBffController` em `/bff/mvcfiltro` (padrão da casa dos BFFs: `[ApiController]`, `ControllerBase`, `[AutoValidateAntiforgeryToken]`). Um decimal financeiro ambíguo ("1.234") liga por query/form nos três ramos; a ação não toca API nem banco.
4. `backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs` — entrada de catálogo `MvcSeguroTest → ADM360` (sem isso a guarda redireciona para `AccessDenied` antes do filtro; o módulo bate com os claims de teste `modules=ADM360`).
5. `backend/PlantaoPro.Web/Program.cs` — `ApiBehaviorOptions.SuppressModelStateInvalidFilter = true`: o `[ApiController]` instala um filtro próprio de ModelState inválido (`problem+json`) que atendia **antes** do filtro único da casa e quebrava o contrato do BFF. Agora há uma única fonte de verdade para ModelState inválido.
6. `backend/PlantaoPro.Tests/MvcSeguroDestinoFiltroTests.cs` — novo: 14 testes (7 unidade contra `DestinoLocalValidado` + 6 integração via fábrica Web + 1 theory expandida).
7. `backend/PlantaoPro.Tests/Administrativo360GateMonetarioTests.cs` — Referer dos gate tests atualizado para origem real do transporte do TestServer (§3).
8. `backend/PlantaoPro.Tests/Administrativo360EventosImutabilidadeTests.cs` — cleanup `LimparJornadaAsync` agora remove `adm360_cotacao_envios` antes da resposta (§4.2).

## 3. Descobertas empíricas da runtime net10 (cada uma isolada por diagnóstico dentro do próprio teste xUnit)

| Sintoma | Causa raiz | Correção |
|---|---|---|
| Referer local consistente rejeitado (null) em unidade | `HostString.Value` **inclui a porta** nesta runtime (`"localhost:443"`), enquanto `Uri.Host` devolve só o nome | comparar com `request.Host.Host ?? request.Host.Value` |
| Mesma rejeição nos testes TestServer | requisições chegam com **scheme `http`** no TestServer mesmo com `BaseAddress https://localhost/` (diag: `scheme uri=[https] req=[http]`) | referer de integração usa `http://localhost/...`; unitário cobre a semântica `https` |
| Porta ausente em `HttpRequest` | `HttpRequest.Port` removida na net10 | `request.Host.Port ?? (https ? 443 : 80)` — mesma normalização de `Uri.Port` |
| Controller com rota absoluta no nível de ação (`[Route("bff/mvcfiltro")]` sob `[Route("__test/...")]` no controller) respondia **404 vazio** | comportamento não reproduzido no restante do app; forma combinada controller+ação absoluta não casava | controller BFF dedicado seguindo o padrão exato da casa (`bff/agenda`, `bff/operacao`) — funciona |
| BFF retornava `application/problem+json` em vez do envelope canônico | convenção do `[ApiController]` (`ModelStateInvalidFilter`) roda antes dos filtros registrados nas options | `SuppressModelStateInvalidFilter = true` (comportamento preservado: o filtro da casa intercepta primeiro em todos os controllers) |
| Integrações falhavam com 302 para `Account/AccessDenied?module=...` | `SaasRouteGuardFilter` bloqueia controller fora do catálogo antes do filtro | entrada `MvcSeguroTest → ADM360` no catálogo (comentário no código explica o escopo Testing) |
| `ExecuteAsync`/`Port` inexistentes em compile | APIs removidas na net10 | `IActionResult.ExecuteResultAsync(ActionContext)`; porta via `Host.Port` (registrado no guia da rodada) |

## 4. Evidências por execução

### 4.1 Testes

- **14 novos aprovados** (`MvcSeguroDestinoUnitTests` + `MvcSeguroDestinoIntegracaoTests`), cenários:
  - POST decimal ambíguo **sem Referer** → 400 direto, stub da API com **zero chamadas**;
  - POST **Referer externo** → 400 (não vira open redirect);
  - POST **Referer local** → 302 para a origem (destino preservado), zero chamadas de API;
  - GET decimal ambíguo → 302 para a **própria URL com query preservada**; GET válido → ação executa (200);
  - BFF decimal ambíguo → **400 JSON**, `reason:DADOS_INVALIDOS`, `status:400`, content-type `application/json` (não `problem+json`), sem `Location`;
  - unidade: sem referer / relativo-ou-inválido (theory ×2) / externo / **porta diferente** / **scheme diferente** / host em maiúsculas (aceita, case-insensitive) / local válido com path+query.
- **Gate monetário (`Administrativo360GateMonetarioTests`) mantido verde**: `1.234` (ambíguo) e valor > `numeric(18,4)` não chegam ao sistema com destino local (PRG) e caem em 400 sem destino válido.
- **Suíte canônica completa: 944/944 aprovados** (base 930 + 14 novos) — `dotnet test backend\PlantaoPro.Tests\PlantaoPro.Tests.csproj --nologo -v q` (~34 s).

### 4.2 Causa raiz colateral corrigida (infra de testes, observação p/ A5)

A primeira suíte completa da rodada foi **truncada** (nó MSBuild morto, MSB4166) no meio de um teste ADM360 e deixou órfão em `plantaopro_test`: 1 resposta `ENVIANDO` (+ cotacao/orçamento/itens/envio). `ConectorComFalhaTecnica_...` passou a falhar determinístico (`Expected: 0, Actual: 1` de `ENVIANDO` no tenant). Diagnóstico: `LimparJornadaAsync` **não** remove `adm360_cotacao_envios` (FK sobre a resposta) — a `DELETE` da resposta falhava e o `catch {}` silencioso abortava o resto da limpeza. Correção: remover `envios` antes das exportações/respostas no cleanup + limpeza manual do órfão (verificado `ENVIANDO = 0` após). Suíte seguinte: 944/944.

## 5. Status

- **Técnico: implementado e testado por execução** (unidade + integração TestServer + suíte canônica). Aceite funcional do usuário: pendente de revisão nas telas reais (a reidratação de campos depende das views — §6).
- Não promovedor de bug zero: cobertura se limita aos ramos exercitados (decimal financeiro em 3 superfícies); outros tipos de binding seguem o mesmo caminho do filtro mas sem teste dedicado cada um.

## 6. Backlog decorrente

1. **Reidratação campo-a-campo por view** após erro (preservar selects/textos digitados nas views reais; mecanismo disponível: `TempData["Error"]` + re-passagem de valores) — item de UX a validar na homologação, por view financeira principal.
2. `LimparJornadaAsync` ainda engole erros de cleanup em `catch {}` silencioso — tornar visível (log/warn) em A5 junto com a varredura de flake.
3. `TestSigninController` segue desativado apenas por ambiente (sem flag) — alvo do A5 (`TestAuth:Enabled`, default false, provar indisponível em Production **e** Development sem flag).
4. Forma combinada `[Route]` absoluto de ação sob `[Route]` do controller respondendo 404 na net10 não foi explicada em fonte (trabalhada por padrão da casa) — registrar no playbook de net10.
