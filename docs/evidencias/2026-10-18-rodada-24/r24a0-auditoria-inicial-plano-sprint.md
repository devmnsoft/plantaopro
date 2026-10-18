# R24 · r24a0 — Auditoria inicial e plano de sprint (F-R14-D + F-R14-B)

Data: 2026-10-18 · Sessão: `opencode` · Base: R23 encerrada (`origin/main` = `beb21d1`, suíte 1529 fatos).
Fila herdada de `docs/evidencias/2026-10-16-rodada-14/r14z-relatorio-final-consolidado.md`: **F-R14-B** (total de
paginação truncado + scoping de tenant dos relatórios SaaS) e **F-R14-D** (assimetria dos 500 entre chamadas simples e
paginadas no parser BFF Web). Nesta mesma rodada é registrada também a nova diretriz de UX/texto/acesso de 30 sprints
(vide `r24a1-diretriz-ux-texto-acesso-roadmap-30-sprints.md`).

## Achados (com pontos exatos)

### F-R14-D — Assimetria dos 500 no parser BFF Web

O mapeamento dos 500 difere entre os dois parsers de `BaseWebController.cs` (Web):

- **Chamada paginada** — `ReadApiPagedResponseAsync` (L158-162): em não-sucesso usa `TryParseMessage(content)` **diretamente**,
  sem passar por `BuildApiErrorMessage`. Um 500 com mensagem da API chega ao usuário **repassado**
  ("Falha ao carregar." + mensagem do envelope). Este comportamento foi aprovado ao vivo na r14b2 ("degradação honesta com a
  devida atribuição") e está travado em contrato: `R14FaturasCompetenciaNullContratoTests.IndexDeFaturamento_FaturasEm500`
  (`Assert.Contains("carregar faturas.", html)`), idem `RelatorioSaaSDeFaturamento_EndpointEm500`.
- **Chamada simples** — `ReadApiResponsePayloadAsync` (L277-284): em não-sucesso chama
  `BuildApiErrorMessage(statusCode, parsedMessage)` (L520-530). O ramo `InternalServerError` (L528) retorna **sempre** o
  literal fixo "Ocorreu uma instabilidade interna. Tente novamente em instantes.", **descartando a mensagem da API**.
  Travado em contrato: `R16WebParserErrorContratoTests.GET_500_mantem_literal_fixo_e_descarta_mensagem_interna`
  (comentário L124 já cita "intenção dos 500 é decisão separada, fila P3 — F-R14-D") e
  `R14FaturasCompetenciaNullContratoTests.IndexDeFaturamento_ResumoEmFalha_MostraBannerDaInstabilidadeESuprimeOVazio`
  (`Assert.Contains("instabilidade interna", html)`, comentário cita F-R14-D pendente).

Ou seja: a casa versionou os dois lados da assimetria em contratos. A decisão de intenção pendente desde R16/D4 precisa
escolher UM dos dois lados como canônico.

**Decisão D-R24-1** — unificar para **repasse da mensagem da API com fallback ao literal fixo quando ela não existir**:

```csharp
HttpStatusCode.InternalServerError => apiMessage ?? "Ocorreu uma instabilidade interna. Tente novamente em instantes.",
```

Justificativa:
1. O precedente **aprovado ao vivo** (r14b2) é o repasse com atribuição — mudar o lado paginado para literal fixo quebraria
   dois fatos de contrato aprovados ao vivo; alinhar o lado simples ao paginado quebra apenas UMA asserção.
2. As mensagens de 500 desta API são redigidas pelo nosso backend (`ApiResponse.Fail("Não foi possível carregar …", 500)`),
   não são stack traces — repassá-las atende à linguagem de usuário. O risco de vazamento interno existe nos dois lados hoje
   (o paginado já repassa); permanece mitigável no handler global da API.
3. Preserva-se o literal como fallback honesto quando o corpo não traz mensagem (novo fato negativo no contrato).

Consequência de contrato: reescrever o fato R16 (`GET_500_…`) para repasse + novo fato para fallback sem mensagem; atualizar a
asserção do fato R14 `IndexDeFaturamento_ResumoEmFalha_…` para a mensagem atribuída. Restantes ramos (400/401/403/404/409/default)
já repassam `apiMessage ?? literal` — inalterados.

### F-R14-B — Total de paginação truncado ao pageSize nos relatórios SaaS

`PlantaoPro.Api/Controllers/RelatoriosSaasController.cs`:

- `ExecutarRelatorioAsync` (L136-152): executa `sql + "\nlimit @s offset @offset"` e faz `var total = items.LongCount()`
  (L142) — o total retornado é o tamanho da **página**, não do conjunto. A paginação da UI mostra 1 página sempre.
- `ObterDadosAsync` (L160-161): mesmo padrão (`items.LongCount()`) no caminho de exportação (pageSize até 500).

**Decisão D-R24-2** — contagem real via subquery no wrapper, caminho único compartilhado pelos dois endpoints:

```csharp
private static async Task<PagedResult<RelatorioSaasLinhaDto>> ExecutarPaginadoAsync(
    NpgsqlConnection cn, string sql, object parametros, int p, int s)
{
    var total = await cn.ExecuteScalarAsync<long>("select count(*)::bigint from (" + sql + ") as __contagem", parametros);
    var items = await cn.QueryAsync<RelatorioSaasLinhaDto>(sql + "\nlimit @s offset @offset",
        UnirParametros(parametros, new { s, offset = (p - 1) * s }));
    return new PagedResult<RelatorioSaasLinhaDto>(items, p, s, total);
}
```

- A query de itens permanece **idêntica** (`sql + "\nlimit @s offset @offset"`), preservando o fato R14
  `RelatorioSaaSDeFaturamento_ExecutaComCompetenciaNula_NoExatoFormatoDoEndpoint` (SQL exata + sufixo do wrapper) verde.
- A count reutiliza exatamente os mesmos parâmetros da action (`@status`/`@competencia` já tipados por cast); `order by`
  dentro de subquery sem limit é válido no PostgreSQL.
- Clampings de cada caller mantidos (lista: `Math.Clamp(pageSize,1,100)`; export: `Math.Clamp(pageSize,1,500)`), assim como
  `page = Math.Max(1, page)`; a contagem não recebe `@s`/`@offset`.
- Custo: +1 round-trip por paginação de relatório SaaS — aceitável (consultas administrativas, dados agregados pequenos).

### F-R14-B (parte 2) — Scoping de tenant em `RelatoriosSaasController.Faturamento` (e demais relatórios SaaS)

O controller inteiro é `[Authorize(Roles = AdministradorGlobal + "," + Administrador + "," + Financeiro)]` e TODAS as queries
consultam `plantaopro.clientes`/`faturas_saas` **sem filtro de tenant_id** — global por design pré-existente (R14 registrou a
pendência de decisão). Os papéis da porta (`RolesConstants`): `AdministradorGlobal`/`Administrador` são papéis-base da
plataforma; `Financeiro` (FinanceManager) é papel operacional — a visibilidade mais fina (quais papéis podem ver o backoffice
global com Nomes de clientes de OUTROS tenants) é exatamente o tema da diretriz "tenant visível apenas ao super administrador".

**Decisão D-R24-3** — nesta rodada: **manter global por design e documentar**, fixando em fato versionado o atributo
`[Authorize]` atual (regressão de autorização do backoffice é grave). O redesenho completo de visibilidade de tenant
(epic E2 da diretriz: tenant visível só para super admin; área por CPF/CNPJ/e-mail+senha) será tratado de ponta a ponta na
sua fatia própria (R38/R39 do roadmap) — evitar meio-termo que mudaria semântica do endpoint sem cobrir o resto da UI.

## Plano de fatias R24

| Fatia | Conteúdo | Fatos |
|---|---|---|
| r24b1 | D-R24-2: `ExecutarPaginadoAsync` com count real nos dois caminhos (controller SaaS); D-R24-3: fato do `[Authorize]` | fonte (sem `LongCount`; `count(*)::bigint` presente; autorizações) + banco (seed 125 faturas → total real ≠ tamanho da página; páginas 1 e 3 corretas) |
| r24b2 | D-R24-1: ramo 500 do `BuildApiErrorMessage` → repasse c/ fallback; atualização dos 2 fatos + 1 novo (fallback) | contrato parser (3 fatos no R16) + 1 fato R14 atualizado |
| r24z | Relatório consolidado + push | suíte final 1529+N, gate multiset idêntico ao pristino |

Gate de cada fatia: build real (`dotnet build backend\PlantaoPro.sln -c Release --no-incremental` → log `%TEMP%\opencode\r20b1_build.log`)
→ `r20b1_gate.ps1` (multiset 160 vs `%TEMP%\opencode\r17_pristine_build.log`); flake ⇒ isolate 5×. Suíte com base 1529 antes de r24z.

## Pontos de referência consultados

- `backend\PlantaoPro.Api\Controllers\RelatoriosSaasController.cs` L34-86 (ações), L136-168 (wrapper/export).
- `backend\PlantaoPro.Web\Controllers\BaseWebController.cs` L125-209 (parsers), L520-530 (`BuildApiErrorMessage`).
- `backend\PlantaoPro.Tests\R14FaturasCompetenciaNullContratoTests.cs` L94-105 (SQL exata + sufixo), L189-212 (fato do resumo 500).
- `backend\PlantaoPro.Tests\R16WebParserErrorContratoTests.cs` L121-133 (fato 500 literal fixo, comentário D4).
- `backend\PlantaoPro.Api\RolesConstants.cs` (papéis plataforma × tenant).
- `psql \d plantaopro.faturas_saas` / `\d plantaopro.clientes` (colunas p/ seed determinístico: `reg_status`, `valor_total`, `competencia`, `codigo` livre).
- `PagedResult<T>(IEnumerable<T> Items,int Page,int PageSize,long Total)` — `PlantaoPro.Api\Models.cs` L58 (4 argumentos; o wrapper usa 3 hoje, sem total).
