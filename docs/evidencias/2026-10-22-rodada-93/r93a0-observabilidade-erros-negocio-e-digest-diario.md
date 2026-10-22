# R93 (B5) — Observabilidade: erros de negócio centralizados + digest diário

## Objetivo

Linha 85 do plano (r66a0): "observabilidade: erros de negócio centralizados + digest diário".

Antes desta rodada, as exceções de negócio conhecidas do módulo Administrativo 360 eram
convertidas pelo `Adm360BusinessExceptionFilter` em 400/403/404 amigáveis e **paravam aí**:
nenhum destino guardava as violações. Apenas falhas técnicas (500) caíam em
`plantaopro.api_error_logs` (via `RequestLoggingMiddleware`), e o resumo de observabilidade
exibia `TotalErrosHoje` (técnico) sem qualquer visibilidade de erros de negócio. O resultado:
impossível responder "quais regras de negócio estão falhando com que frequência, por dia e por
tenant" — exatamente o dado que um ADMINISTRADOR_GLOBAL precisa para triagem e capacidade.

Entregas da rodada:
1. Tabela `plantaopro.erros_negocio` (migration **v2374**, idempotente)
   com tenant/usuário/módulo/categoria/mensagem/endereço/data;
2. Persistência **best-effort** de cada erro mapeado pelo filtro (timeout curto, falha isolada —
   nunca altera a resposta);
3. Endpoint `GET /api/observabilidade/digest-diario?dias=1..31&tenantId=<uuid>` (ADMINISTRADOR_GLOBAL):
   totais por dia × categoria + top 10 mensagens do período;
4. `TotalErrosNegocioHoje` no `GET /api/observabilidade/resumo`;
5. Contrato travado: `R93ObservabilidadeErrosNegocioContratoTests` (6 fatos novos → suíte 1780 → **1786**).

## Recon — como os erros eram tratados antes

- **Filtro** (`Adm360BusinessExceptionFilter`, registrado em `Program.cs` via `Filters.Add<T>()`,
  rotas contendo `/administrativo360`): `Administrativo360BusinessException`/`ArgumentException` → 400,
  `UnauthorizedAccessException` → 403, `KeyNotFoundException` → 404; demais tipos seguem para o
  handler global (500 + log estruturado com correlação).
- **Errata de cobertura descoberta no recon**: vários controllers do módulo capturam
  `InvalidOperationException` localmente (ex.: `Adm360CadastrosController`) e devolvem 4xx direto —
  esses erros **nunca passam pelo filtro**. Decisão: o escopo R93 centraliza o que o filtro já
  mapeia (o caminho canônico do bloco); rotas com catch local ficam para uma higienização futura.
- **Métrico de referência**: `RequestLoggingMiddleware` já grava `api_error_logs` com
  `usuario_id`/`cliente_id` das claims (`uid`/`cliente_id`) — a mesma fonte usada pela persistência nova.
- **Superfície**: não existe página Web de observabilidade (nada em `.cshtml` referencia
  `observabilidade`); o consumo é via API com papel `ADMINISTRADOR_GLOBAL` (padrão dos endpoints
  existentes do `ObservabilidadeController`). O digest entra como endpoint nesse mesmo controlador.

## Decisões (design)

- **Tabela genérica por tenant, módulo e categoria** (não um log por módulo): a categoria é 1:1
  com o status mapeado pelo filtro (`REGRA_NEGOCIO`→400, `ACESSO_NEGADO`→403, `NAO_ENCONTRADO`→404,
  trava em CHECK). `modulo varchar(80)` já admite outros módulos escrevendo no futuro sem novo DDL.
- **Dois índices**: `(reg_date DESC)` para a janela do digest (todos os tenants) e
  `(tenant_id, reg_date DESC)` para consulta por tenant — mesma lógica de liderança dos índices da R92.
- **Persistência best-effort no próprio filtro**, em `OnExceptionAsync`: insert com timeout de 2 s
  e catch total (falha vira apenas `LogWarning`). O erro de negócio já foi respondido ao cliente;
  a observabilidade não pode custar latência nem quebrar a resposta.
- **Lição 1 (DI)**: dois construtores (nu + injetado) quebraram a resolução em tempo de execução
  (`Multiple constructors accepting all given argument types`). Solução: **um único construtor com
  parâmetros opcionais** — os contratos de unidade existentes (`new Adm360BusinessExceptionFilter()`)
  continuam compilando sem edição.
- **Lição 2 (dispatch MVC)**: implementando só `IExceptionFilter`, o `DefaultFilterProvider` despacha
  pelo adapter **síncrono** (`OnException`) e o override assíncrono nunca executa — constatado de
  forma empírica (404 amigável respondido, zero linhas gravadas, zero warnings). O filtro agora
  declara também `IAsyncExceptionFilter`, o que faz o provider chamar `OnExceptionAsync`.
- **Digest determinístico**: filtro opcional `tenantId` permite auditoria por tenant sem ruído dos
  demais (o digest global enxerga todos os tenants); `TopMensagens` agrupa por mensagem exata
  (as mensagens de regra são strings estáveis guardadas pelos guards de domínio).
- **Escopo Web**: fora da rodada. O BFF consome o mesmo serviço via API, então falhas de negócio do
  módulo já chegam à centralização pelo lado API; erros lançados diretamente em controllers Web
  seguem pendência (abaixo).

## Mudança (commit r93a)

| Arquivo | Papel |
|---|---|
| `database/migrations/2026_10_v2374_r93_erros_negocio.sql` | Tabela + CHECK de categoria + 2 índices (idempotente, padrão v2372/v2373) |
| `database/migration-manifest.json` | Entrada v2374 (hash LF real, dependsOn v2373, active, transactional) — manifest **144** migrações |
| `backend/PlantaoPro.Api/Adm360BusinessExceptionFilter.cs` | Mapeamento extraído para `Mapear()`; `IAsyncExceptionFilter`; persistência best-effort; ctor único opcional |
| `backend/PlantaoPro.Api/Controllers/ObservabilidadeController.cs` | `digest-diario` (+clamps 1..31, tenant opcional) e `TotalErrosNegocioHoje` no resumo |
| `backend/PlantaoPro.Tests/R93ObservabilidadeErrosNegocioContratoTests.cs` | 6 fatos (DDL, manifest, auto-cura, e2e 404→linha, agregação do digest, wiring do resumo) |

Detalhes do contrato:
- **Fato 4 (e2e)** usa `GET /api/administrativo360/fiscal/notas/{guid-falso}`: rota fiscal **sem
  catch no controller** (documentada no controller justamente como "erros de negócio viram 400/403/404
  via Adm360BusinessExceptionFilter") e `ObterNotaAsync` lança `KeyNotFoundException` — cenário
  determinístico, sem escrita/clean-up. A linha esperada carrega `tenant_id` = claim `cliente_id` do
  gestor, `usuario_id` = claim `uid`, categoria `NAO_ENCONTRADO` e endpoint real.
- **Auto-cura** (padrão R55/R89/R92): reaplicação idempotente da migration sob advisory lock
  `hashtext('r93_erros_negocio')` + retry de impasse `40P01`, **memoizada por processo** — na primeira
  suíte completa da rodada, a DDL inicial de auto-cura concorreu com classes em paralelo e duas suítes
  alheias caíram em vítima de impasse (corrida CNPJ do provisionamento e limpeza financeira Saude360,
  ambas a ~12–14 s da execução). Com a memoização (uma única DDL por processo) a reexecução ficou verde.
- **Fato 5** isola a agregação num tenant dedicado (`3fa85f64-…fa93`, criado e removido pelo teste):
  dias=1 → 3 erros (2 REGRA_NEGOCIO + 1 NAO_ENCONTRADO) e top de mensagens = a mensagem duplicada;
  dias=7 → 4 erros em 2 dias (a linha anterior ao período cai fora do clamp inferior da janela).

## Evidência (banco de teste, pós-suíte)

- Conteúdo real de `plantaopro.erros_negocio` após as execuções da suíte (linhas do Fato 4; as do
  Fato 5 são limpas pelo próprio teste):

```text
categoria      | total | primeira            | ultima
NAO_ENCONTRADO |     3 | 2026-10-22 22:07:00 | 2026-10-22 22:10:44
```

```text
endpoint                                              | categoria      | msg
/api/administrativo360/fiscal/notas/075b5ce9-…        | NAO_ENCONTRADO | Pré-nota não encontrada.
/api/administrativo360/fiscal/notas/313c16ad-…        | NAO_ENCONTRADO | Pré-nota não encontrada.
/api/administrativo360/fiscal/notas/39a3a89e-…        | NAO_ENCONTRADO | Pré-nota não encontrada.
```

  Um registro por execução da suíte = exatamente a taxa esperada (o Fato 4 dispara 404 uma vez por run).
- Digest validado nos formatos `dias=1` e `dias=7` com `PorDia`/`Categorias`/`TopMensagens`
  (saída JSON do Fato 5: `TotalErrosNegocio=3`, top = `"Saldo insuficiente para reservar o lote."` ×2).

## Gates

- **A/B Release** (`r87abuild.ps1 -Tag r93`): base-unicos=**76** / nova-unicos=**76** → **DIFF=NENHUMA**.
- **Suíte completa (pré-doc)**: `dotnet test … --no-build -c Release` → **Aprovado! 1786/1786**
  (1780 canônicos + 6 fatos novos da R93).
- **docscan** (`r62_docscan.ps1`): `ARQ_VERIFICADOS=6`, `DOCSCAN_BAD=0`.
- **Suíte completa (pós-doc)**: **Aprovado! 1786/1786** — higiene `RepositoryHygieneContractTests` **8/8**.
- **Inventário** (`r75_gap.ps1`): N/A — nenhuma página/artigo novo (endpoint API apenas).

## Pendências

- Rotas adm360 com catch local de `InvalidOperationException` (ex.: cadastros/parceiros) ainda
  respondem 4xx sem passar pela centralização — higienizar os catches quando a rota do módulo for tocada.
- Erros de negócio lançados diretamente em controllers do BFF Web (fora do escopo API) não são
  centralizados ainda.
- Retenção/particionamento de `erros_negocio`: fora de escopo na escala atual (dezenas de linhas/dia
  no banco de teste); reavaliar sob carga (R94 pode validar o digest como observável sob pressão).
