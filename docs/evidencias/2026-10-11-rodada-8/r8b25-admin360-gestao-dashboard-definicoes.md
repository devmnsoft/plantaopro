# R8-B25 — Definição e rastreabilidade de KPIs: ADMIN360 (Gestão/Dashboard) agora EMITE e EXIBE as definições (§27 linha 205 residual, parte 6)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia da **auditoria ALÉM do top-10 §29**: §27 linha 205 *residual*. Sexta tela (após BI/B20, Dashboard/B21, Meu Dia/B22, Command Center/B23 e Admin360 Home/B24): **Admin360 – Gestão/Dashboard** (`/Administrativo360/Gestao/Dashboard`), o maior painel gerencial da área administrativa.
- **Data:** 2026-10-14
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica + mapeamento)

O dashboard gerencial exibe **3 grupos / 20 valores** computados pela API em `GestaoDashboardRepository.ObterDashboardAsync` (L29-137). Nenhum endpoint Admin360 emitia definições antes. Por indicador, o SQL real:

- **Cotações (10 valores)** — `cotacoesSql`:
  - `TotalRecebidas` = `count(*)` em `adm360_cotacoes` (tenant + filtros opcionais estab/provedor/período por `capturada_em`);
  - `AguardandoRelacionamento` = `status_interno IN ('RECEBIDA','EM_RELACIONAMENTO')`;
  - `ProximasDoPrazo` = `prazo_resposta <= now() + interval '24 hours' AND status_interno NOT IN ('RESPONDIDA','CANCELADA','EXPIRADA')`;
  - `OrcamentosGerados` = `orcamento_id IS NOT NULL`;
  - `RespostasAceitas` = `status_interno = 'RESPONDIDA'`;
  - `PropostasVencedoras` / `PropostasPerdidas` = **subselect** em `adm360_cotacao_respostas` por `status_comercial_externo = 'VENCEDORA'/'PERDIDA'` (escopo **só tenant**);
  - `FalhasIntegracao` = **subselect** `status_transmissao IN ('REJEITADA_PELO_PORTAL','RESULTADO_DESCONHECIDO')` (escopo **só tenant**);
  - `TaxaSucessoPercentual` + `TempoMedioRespostaHoras` = **derivados no servidor** (taxa = vencedoras/(vencedoras+perdidas) quando há desfecho, senão respostasAceitas/totalRecebidas, senão 0; tempo médio = `AVG(EXTRACT(EPOCH FROM (updated_at - capturada_em))/3600)` das RESPONDIDAS, 0 se não houver).
- **Documentos XML (6 valores)** — `xmlSql` em `adm360_documentos_recebidos`: `Completos`=`tipo_documento='NFE_COMPLETA'`; `Resumos`=`'RESUMO'`; `Quarentenados`=`quarentena=true`; `PendentesConferencia`=`status_conferencia='PENDENTE' AND quarentena=false`; `Vinculados`=`'VINCULADO'`; `TotalRecebidos`=`count(*)` (tenant + estab + período por `data_emissao`).
- **Integrações (4 valores)** — `integracoesSql` em `adm360_portal_contas`: `ContasAtivas`=`ativo=true AND status_integracao='CONFIGURADA'`; `ConexoesBloqueadas`=`status_integracao IN ('BLOQUEADA','ERRO_AUTENTICACAO')`; `FalhasUltimas24h` = **mesmo valor** de `FalhasIntegracao` das cotações (repassado, L135); `UltimaSincronizacaoGeral`=`MAX(ultima_sincronizacao)`.

Este é o **maior** dashboard Admin360 (escolhido como próximo alvo de maior impacto após a Home). O view já trazia textos `*Desc` estáticos (UiText `Adm360.Dash.*Desc`) — aproveitados como **rótulos** dos tiles; as **definições exibidas** são emitidas pela fonte, fiéis ao SQL real (nada inventado).

## 2. Decisão e mudança (EMITE na API + EXIBE na Web; sem nova consulta/número/regra)

- **API — emissão fiel ao SQL + detalhe de camada:** o DTO de dashboard (`DashboardAdm360Dto`) vive em **Application** (que só referencia **Domain**), mas o record `KpiDefinicao` vive em **Api**. Para respeitar as camadas (evitar referência reversa Application→Api), a emissão usa um **record de resposta da camada Api** — `DashboardAdm360Resposta(Cotacoes, DocumentosXml, Integracoes, GeradoEm, Definicoes, TimeCriterion)` — montado no `Adm360GestaoController.ObterDashboard` (mesmo padrão da B22: definições no controller Api). O controller devolvia `Ok(dto)` puro → agora `Ok(resposta)`. Os **20 textos** descrevem exatamente cada filtro/fórmula; os 3 subselects (vencedoras/perdidas/falhas) marcam explicitamente **"escopo apenas tenant (NÃO aplica os filtros de estabelecimento/provedor/período do painel)"** e as duas taxas marcam "derivado no servidor".
- **Web — espelho + renderização:**
  - `DashboardAdm360ViewModel` (em `Models/Administrativo360CotacoesXmlModels.cs`) ganha `Definicoes` (`KpiDefinicaoDto` — reuso do espelho da B21) + `TimeCriterion` como `{ get; init; }` (ctor posicional de 4 cols inalterado);
  - `Views/Administrativo360/Gestao/Dashboard.cshtml`: bloco `<details>` **"Como cada indicador é calculado (fórmula · fonte · período · fuso)"** ao final (após os 3 grupos), **gateado em `!Model.Erro`**. Variável hoistada no `@{ }` superior com `Array.Empty<…>()`. O BFF `Adm360GestaoWebController.Dashboard()` deserializa o novo campo automaticamente (STJ por nome) — **sem mudança no controller Web**.

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Api/Controllers/Adm360GestaoController.cs` | **Produção:** novo record `DashboardAdm360Resposta`; `ObterDashboard` monta a resposta com `Definicoes()`/`TempoCritico` (20 definições + critério, fiéis ao SQL). |
| `backend/PlantaoPro.Web/Models/Administrativo360CotacoesXmlModels.cs` | **Produção:** `DashboardAdm360ViewModel` + `Definicoes`/`TimeCriterion` (espelho). |
| `backend/PlantaoPro.Web/Views/Administrativo360/Gestao/Dashboard.cshtml` | **Produção:** bloco `<details>` "Como cada indicador é calculado" (gateado em `!Erro`). |
| `backend/PlantaoPro.Tests/R8B25GestaoDashboardDefinicoesContratoTests.cs` | **Novo:** 3 fatos source-read — API emite (fiel ao SQL: `'VENCEDORA'`, `prazo_resposta <= now()`, `EXTRACT(EPOCH`, `status_integracao 'CONFIGURADA'`) · VM Web carrega · view renderiza. |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §27 linha 205 → + Admin360 Gestão/Dashboard (B25); residual = FluxoCaixa estático + listas somadas no Razor + telas de detalhe + view global CC; suíte 1388. |
| `docs/evidencias/2026-10-11-rodada-8/r8b24-admin360-org-definicoes.md` | Higiene: "baixa fidelidade" → "sem fórmula fiel ao SQL" (o termo "fidelidade" é proibido pelo teste de higiene do repositório). |

## 4. Evidência (executada)

- **Build sln completa:** EXIT 0 (compila o novo record da Api + o espelho Web + a view Razor). Reconfirmada após o refinamento dos textos.
- **Alvo R8B25:** `R8B25GestaoDashboardDefinicoesContratoTests` = **3/3 aprovados** (`--filter FullyQualifiedName~R8B25`) — antes E depois do refinamento dos textos (build + rerun confirmaram).
- **Gate phaseA v2:** EXIT 0.
- **Full suite v2:** **Total de testes: 1388** (baseline 1385 + 3 novos) em cada uma das 3 rodadas, **sem kill por OOM** (RAM 0,79 GB livre ao iniciar; passes de ~0,4–0,5 min completaram os 1388). Ruído observado, todo transiente/pré-existente e confirmado:
  - `RepositoryHygieneContractTests.Repositorio_NaoDeveConterTermosDoProdutoIncorretoEmArquivosVersionaveis` — **falha determinística nas 3 passes**. Causa: termo proibido `"fidelidade"` presente numa linha do doc `r8b24-admin360-org-definicoes.md` (acrescentada no scoping pós-baseline 1385, sem nova execução da higiene). **Corrigido** (reword de 1 frase) → teste isolado **1/1 PASS** (também re-confirmado após o refinamento do controller).
  - `R7OBOnboardingPreferenciasIdiomaTests.O2` (full1) e `SaasGovernancaB6Rodada4Tests.G6`/`.G1b` (full2) — 40P01/teardown sob concorrência, classe flake **conhecida** (catálogo) sob RAM baixa; cada uma falhou em 1 de 3 passes independentes e **isolada = 5× 5/5** → transiente.
- **Verificação byte-level:** `git status` = 1 file Api + 2 files Web + 1 teste novo + 3 docs (matriz + doc novo + doc B24-higiene); nenhum outro arquivo de produção tocado.

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Demais telas Admin360 c/ cards KPI** (§27-205 residual remanescente):
   - **FluxoCaixa (8 tiles)** — já possui bloco **estático** "Indicadores declarados" (B4/J12) no view (`FluxoCaixa.cshtml` L248-264); decisão pendente: manter estático vs. converter para emissão do backend para uniformizar.
   - **Listas c/ totais somados no Razor** (TitulosPagar 4, TitulosReceber 3, Estoque 3, ContasFinanceiras 2, ComissoesPendentes 1) — a agregação vive **na view**; definir fielmente exigiria descrever a soma em C#/Razor, não o SQL.
   - **Telas de detalhe** (Venda/Título/TítuloPagar/Orçamento) — valores gravados na entidade (não KPI de agregação); menor prioridade.
2. **View GLOBAL do Command Center** (`GetGlobalAsync` — métricas de plataforma): segue pendente (menor; visão de governança).
3. **Drill-down §27-208:** segue proposta/pendente (B19) — agora **seis** telas (BI + Dashboard + Meu Dia + Command Center + Admin360 Home + Admin360 Gestão/Dashboard) exibem definições (base para linkar "ver registros").
4. **Nota honesta sobre tempo/filtros (documentada no `TimeCriterion` exibido):** o dashboard mistura escopos — a maioria das métricas aplica os filtros do painel (estab/provedor/período via `capturada_em`/`data_emissao`, janelas UTC), mas os 3 subselects (vencedoras/perdidas/falhas) são **só tenant** e a taxa de sucesso e o tempo médio são **derivados no servidor**. Tudo isso está explícito no critério de tempo + nas definições individuais — sem regra inventada.

## 6. Fila seguinte (R8) — auditoria além do top-10

§27-205: **BI (B20) + Dashboard (B21) + Meu Dia (B22) + Command Center (B23) + Admin360 Home (B24) + Admin360 Gestão/Dashboard (B25) entregues**; residual = **FluxoCaixa estático** + **listas somadas no Razor** + **telas de detalhe** + **view global do CC**. Próximos: **FluxoCaixa** (alinhar estático→emissão) ou **view global do CC**, ou outra área §27/§28 pela matriz, ou janela viva de homologação / push.

## 7. Gotchas aprendidos (reutilizáveis)

- **DTO da camada Application não enxerga tipos do projeto Api:** Application só referencia Domain, mas `KpiDefinicao` mora em Api. Solução limpa: um **record de resposta na camada Api** que empacota o DTO de Aplicação + os campos novos (evita referência circular Application→Api). Mesma lógica da B22 (definições no controller Api). Como o controller Api devolve `Ok(dto)` puro (não envelopado) e a Web lê via `ReadApiResponse<T>` (STJ por nome), basta o JSON carregar `definicoes`/`timeCriterion` — o espelho Web os absorve sem mudar o BFF.
- **Termo de higiene que "quebra" a suíte de forma determinística:** `Repositorio_NaoDeveConterTermosDoProdutoIncorretoEmArquivosVersionaveis` faz `Assert.DoesNotContain(termo, conteúdo, OrdinalIgnoreCase)` de vários termos legados (incluindo **"fidelidade"**) em **todos** os arquivos versionaveis — inclusive `.md`. Uma palavra num doc novo derruba a suíte inteira nas 3 passes. Ao escrever evidências/docs, evitar esses termos legados (ex.: "fidelidade" → "fiel"/"aderente"); localize com `grep -ri fidelidade`.
- **Subselects com escopo diferente do painel:** num dashboard com filtros opcionais, subqueries que filtram **só por `tenant_id`** (ignorando estab/provedor/período) são fáceis de passar despercebido na leitura — vale explicitar na definição ("escopo apenas tenant; NÃO aplica os filtros do painel") para rastreabilidade honesta (§27-205).
