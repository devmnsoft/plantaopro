# R4-A5 — Autenticação de teste (`TestAuth:Enabled`) e varredura anti-flake

Data: 2026-10-06 · Branch: `main` · Base: `40b18db` (R4-A4) · Banco: `plantaopro_test`

## 1. Escopo e decisão

**Problema (finding D1):** o `TestSigninController` (`/__test/signin`, `/__test/dump`) estava
disponível em **Development sem qualquer flag**. Um pool do IIS mal configurado como
Development expondo os endpoints de teste na superfície pública era um risco real: o endpoint
emite cookie de sessão autenticada com claims controlados.

**Política adotada (mais forte que o pedido):** o endpoint só existe quando
**ambiente = Testing E flag `TestAuth:Enabled` = true** (parse bool estrito, default ausente/false).
Em **Production e Development responde sempre 404 — inclusive com a flag presente**. A proteção
contra pool com ambiente errado não pode depender de configuração: ambiente errado é defeito
independente de qualquer chave.

A fonte única da guarda é o predicado estático `TestSigninController.Habilitado(ambiente, flag)`
(publico de propósito — o projeto Web não expõe `InternalsVisibleTo` ao Tests; o controller já é
publico por ser uma rota do app). Os testes invocam o mesmo predicado canônico, sem reimplementar
a política.

## 2. Implementação

| Arquivo | Mudança |
|---|---|
| `backend/PlantaoPro.Web/Controllers/TestSigninController.cs` | injeta `IConfiguration`; predicado `Habilitado`; guard nas duas ações (`signin`, `dump`) substitui `IsDevelopment() \|\| IsEnvironment("Testing")`; doc comments atualizados |
| `backend/PlantaoPro.Tests/Infrastructure/PlantaoProWebFactory.cs` | fixa `UseSetting("TestAuth:Enabled", "true")` (a suíte depende da ponta; fora da fábrica ela fica desligada) |
| `backend/PlantaoPro.Tests/TestSigninFlagGateTests.cs` | **novo**: 18 testes (matriz do predicado + prova por execução em hosts reais) |
| `backend/PlantaoPro.Tests/Administrativo360EventosImutabilidadeTests.cs` | instrumentação dos 3 cleanups silenciosos + guarda de fim de coleção (seção 4) |

Observações de implementação:

- `IConfiguration` no Web não exige using explícito: o SDK Web traz o namespace pelo implicit
  usings do .NET 10 (o namespace antigo `Microsoft.Extensions.Configuration.Abstractions` nem
  existe mais no runtime — `using` explícito dele quebra o build, CS0234).
- O `MvcSeguroTestController` (`__test/mvcfiltro`) já era Testing-only (A3); agora o conjunto
  `__test/*` tem política uniforme de superfície anônima.

## 3. Evidência de execução

### 3.1 Matriz do predicado canônico (13 casos, unitário)

| Ambiente | Flag | Resultado esperado | Observação |
|---|---|---|---|
| Production | ausente / `"false"` / `"true"` | 404 | flag **não** habilita Production |
| Development | ausente / `"true"` | 404 | flag **não** habilita Development |
| Staging | `"true"` | 404 | qualquer outro ambiente |
| Testing | ausente / `""` / `"false"` / `"banana"` | 404 | default desligado; parse estrito |
| Testing / testing | `"true"` / `"TRUE"` | habilitado | case-insensitive nos dois lados |

### 3.2 Prova por execução sobre hosts reais (mesmo `Program`, stub de API determinística)

| Host | Flag | `GET /__test/signin` | `GET /__test/dump` |
|---|---|---|---|
| Testing (fábrica da suíte) | `"true"` | **200**, corpo contém `"ok":true` e `"antiforgery"` | 200 |
| Development | ausente | **404** | **404** |
| Development | `"true"` | **404** (flag ineficaz fora do Testing) | **404** |
| Production | ausente | **404** | **404** |
| Production | `"true"` | **404** (flag ineficaz fora do Testing) | **404** |

O host Production satisfaz o validador fail-fast de startup (A2): `ApiSettings:BaseUrl`
`http://api.test.local` (URL absoluta, sem porta dev) e `DataProtection:KeysDirectory` em
diretório temporário criado pelo próprio teste. O host Development herda o
`appsettings.Development.json` (`DemoSeed.Enabled=true`, BaseUrl de dev) — o `DemoSeed` é
consumido pela API, não pelo Web, e a BaseUrl é sobrescrita pela `UseSetting`; sobe limpo.

**Execução dirigida:** `TestSigninFlagGateTests` (18) + `Administrativo360EventosImutabilidadeTests`
(12) = **30/30 aprovados**; `MvcSeguro*` (14, consumidora da fábrica alterada) = **14/14 aprovados**.

## 4. Cleanups silenciosos — causa raiz do flake histórico e instrumentação

Os três helpers de limpeza do `Administrativo360EventosImutabilidadeTests`
(`LimparJornadaAsync`, `LimparEventosAsync`, `LimparDocumentosAsync`) terminavam em
`catch {} // best-effort`. Foi exatamente esse catch mudo a classe de falha que produziu o
órfão `status_transmissao='ENVIANDO'` no banco após execução interrompida e quebrou a rodada
seguinte por causa raiz invisível.

Instrumentação (comportamento preservado + visibilidade):

1. **Sem throw dentro do `finally`:** a limpeza continua não mascarando a asserção original do
   teste (decisão anterior mantida).
2. **Registro:** `internal static readonly ConcurrentBag<Exception> FalhasDeLimpeza` + linha em
   `Console.Error.WriteLine` (visível no log do runner quando a rodada falha).
3. **Guarda de fim de coleção:** a coleção `A360Transmissao` não tinha definição prévia (era
   implícita); criada `A360TransmissaoCollectionDefinition : ICollectionFixture<GuardaFalhasDeLimpezaColecao>`
   que, no `Dispose` (fim da coleção inteira), lança `AggregateException` listando cada falha de
   limpeza. Se algo escapou, a rodada falha **explicitamente ali**, em vez de virar flake fantasma
   na rodada seguinte. Nota: xunit 2.9.2 não possui o atributo `[CollectionFixture<T>]`
   (recurso do xunit v3); usa-se o padrão formal `CollectionDefinition`.

O escopo da instrumentação ficou restrito ao caso conhecido (A3/F4 + órfão ENVIANDO). Outros
`catch {}` de teste no resto da suíte podem existir; varredura completa fica no backlog (seção 6).

## 5. Varredura anti-flake

Três execuções consecutivas da suíte canônica integral (`dotnet test backend\PlantaoPro.Tests\...
--nologo -v q` contra `plantaopro_test`):

| Rodada | Resultado | Duração |
|---|---|---|
| 1 | **962/962 aprovados, 0 falhas** | 12 s |
| 2 | **962/962 aprovados, 0 falha** | 15 s |
| 3 | **962/962 aprovados, 0 falhas** | 10 s |

Contagem canônica da rodada: **962** (944 da base + 18 novos do R4-A5). Sem MSB4166, sem retry
necessário, nenhum flake observado nas três rodadas. Conclusão: nenhum flake reproduzível nesta
rodada; a causa raiz estrutural conhecida (limpeza silenciosa → estado residual → falha tardia)
passou a falhar de forma explícita e localizável.

## 6. Classificação e backlog

- **Status: APROVADO (por execução)** — flag default-off, indisponível em Production e
  Development provada por host real, suíte verde 3×.
- Backlog:
  1. Varredura e instrumentação dos demais `catch {}` silenciosos em outras classes de teste
     (padrão deste R4-A5 serve como modelo).
  2. Guardas de fim de coleção para outras coleções que escrevem estado global (ex.: coleções
     que tocam `adm360_documentos_recebidos`) — avaliar por coleção conforme surgirem evidências.
- Pendências ambientes (não desta máquina): validação viva do site IIS publicado continua no
  roteiro A2; homologação de IA/fiscal segue BLOQUEADA por credencial/decisão comercial (P2).
