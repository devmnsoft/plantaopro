# R8-B24 — Definição e rastreabilidade de KPIs: ADMIN360 (Home Organização) agora EMITE e EXIBE as definições (§27 linha 205 residual, parte 5)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia da **auditoria ALÉM do top-10 §29**: §27 linha 205 *residual*. Quinta tela (após BI/B20, Dashboard/B21, Meu Dia/B22 e Command Center/B23): **Admin360 – Home Organização** (`/Administrativo360`), o painel de entrada da área administrativa.
- **Data:** 2026-10-14
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica + mapeamento da área)

Os **4 indicadores organizacionais** exibidos como cards (Departamentos/Cargos/Colaboradores ativos/Contratos vigentes) são computados pela API em `Administrativo360Service.ResumoAsync` — **um único `select` com 4 `count(*)`** (`Administrativo360Service.cs:30-34`) — devolvidos por `api/administrativo360/resumo` como `Administrativo360ResumoDto`, que a BFF Web desserializa em `Administrativo360ResumoViewModel` (dentro de `OrganizacaoIndexViewModel.Resumo`). Cada card tem um filtro claro e fiel:
- `Departamentos` = `count(*) from adm_departamentos where tenant_id=@tenant and reg_status='A'`;
- `Cargos` = `count(*) from adm_cargos where tenant_id=@tenant and reg_status='A'`;
- `ColaboradoresAtivos` = `count(*) from adm_colaboradores where tenant_id=@tenant and status='ATIVO' and reg_status='A'`;
- `ContratosVigentes` = `count(*) from adm_contratos_trabalho where tenant_id=@tenant and status='VIGENTE' and reg_status='A'`.

**Mapeamento da área Admin360 (levantamento amplo):** NÃO existe nenhum endpoint Admin360 que emitisse definições. A área tem várias telas com cards KPI (Gestao/Dashboard 14 tiles, FluxoCaixa 8 — este já com um bloco **estático** "Indicadores declarados" B4/J12 no view, várias listas com totais somados no Razor, e telas de detalhe). A **Home Organização** foi escolhida como alvo desta fatia por ser a mais limpa/bounded (um serviço, uma consulta, 4 cards simples) — análoga ao Meu Dia. O restante fica registrado em dívida (§5).

## 2. Decisão e mudança (EMITE na API + EXIBE na Web; sem nova consulta/número/regra)

- **API — emissão fiel ao SQL:**
  - `Administrativo360ResumoDto` ganha `Definicoes` + `TimeCriterion` como **`{ get; init; }` não-posicionais** (o ctor posicional de 4 colunas usado por `QuerySingleAsync` permanece inalterado);
  - `ResumoAsync` passa a `var resumo = await cn.QuerySingleAsync<…>(…); return resumo with { Definicoes = Definicoes(), TimeCriterion = TempoCritico };`. Cada texto descreve **exatamente** o filtro (`reg_status 'A'`, `status 'ATIVO'`, `status 'VIGENTE'`) — nada inventado.
- **Web — espelho + renderização:**
  - `Administrativo360ResumoViewModel` ganha `Definicoes` (`KpiDefinicaoDto` — reuso do espelho da B21) + `TimeCriterion`;
  - `Views/Administrativo360/Index.cshtml`: um bloco `<details>` **"Como cada indicador é calculado"** (server-side, codificação HTML automática do Razor) após a linha dos 4 cards, **gateado em `!Model.Erro`** (a Home renderiza os cards mesmo em erro com zeros → as definições só aparecem quando há dados). Variável hoistada no `@{ }` superior com `Array.Empty<…>()`.
- **Nota de arquitetura:** Admin360 **não tem** classes `Adm360*WebService` — os controllers Web (`BaseWebController`) chamam a API diretamente (`CreateApiClient()` + `ReadApiResponse<T>(client, "api/administrativo360/…")`); o espelho VM vive em `Models/Administrativo360Models.cs`.

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Api/Administrativo360Contracts.cs` | **Produção:** `Administrativo360ResumoDto` + `Definicoes`/`TimeCriterion` (`{ get; init; }`). |
| `backend/PlantaoPro.Api/Administrativo360Service.cs` | **Produção:** `ResumoAsync` anexa as definições + `Definicoes()`/const fiéis ao SQL. |
| `backend/PlantaoPro.Web/Models/Administrativo360Models.cs` | **Produção:** `Administrativo360ResumoViewModel` + `Definicoes`/`TimeCriterion` (espelho). |
| `backend/PlantaoPro.Web/Views/Administrativo360/Index.cshtml` | **Produção:** bloco `<details>` "Como cada indicador é calculado" (gateado em `!Erro`). |
| `backend/PlantaoPro.Tests/R8B24Admin360OrgDefinicoesContratoTests.cs` | **Novo:** 3 fatos source-read — API emite (fiel ao SQL) · VM Web carrega · view renderiza. |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §27 linha 205 → + Admin360 Home (B24); residual = demais dashboards Admin360 + view global CC. |

## 4. Evidência (executada)

- **Build sln completa:** EXIT 0 (compilação com êxito — ambos os projetos e a view Razor do Admin360 Home compilam limpo).
- **Alvo R8B24:** `R8B24Admin360OrgDefinicoesContratoTests` = **3/3 aprovados** (`--filter FullyQualifiedName~R8B24`).
- **Gate phaseA v2:** EXIT 0.
- **Full suite v2:** **Total de testes: 1385** (baseline 1382 + 3 novos) nas 3 rodadas. **full1 + full3 LIMPOS** (EXIT 0); full2 teve **1** falha — `SaasComercialB4MatrizTests.B4_Downgrade_UsoAcimaDoDestino_409` — com `Npgsql 40P01: impasse detectado` (**deadlock**), classe flake **conhecida** (catálogo ①), sem ligação com a fatia → **isolado = 5× 5/5** (transiente).
- **Sem kill por OOM** nesta rodada (box menos carregado; RAM 0,84 GB livre; passes de ~0,5 min completaram os 1385). Assinatura ambiental consistente das rodadas anteriores (deadlock aleatório em teste de escrita sob concorrência).
- **Verificação byte-level:** `git status` = 2 arquivos Api + 2 arquivos Web alterados + teste novo + docs; nenhum outro arquivo de produção tocado.

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Demais dashboards Admin360 com cards KPI** (§27-205 residual remanescente), priorizados pelo levantamento:
   - **Gestao/Dashboard (14 tiles)** — maior impacto; métricas derivadas (taxa de sucesso, tempo médio de resposta, falhas 24h) exigiriam definir cada fórmula fiel ao SQL (`GestaoDashboardRepository.ObterDashboardAsync`); já tem textos `*Desc` estáticos no view (UiText) a aproveitar.
   - **FluxoCaixa (8 tiles)** — já possui bloco **estático** "Indicadores declarados" (B4/J12) no view (`FluxoCaixa.cshtml` L248-264); decisão pendente: manter estático vs. converter para emissão do backend para uniformizar.
   - **Listas com totais somados no Razor** (TitulosPagar 4, TitulosReceber 3, Estoque 3, ContasFinanceiras 2, ComissoesPendentes 1) — o total é agregado **na view** sobre a listagem (não há agregação dedicada na API); definir fielmente exigiria descrever a soma em C#/Razor, não o SQL.
   - **Telas de detalhe** (Venda/Título/TítuloPagar/Orçamento) — cards de valores gravados na entidade (não são KPIs de agregação); menor prioridade.
2. **View GLOBAL do Command Center** (`GetGlobalAsync` — métricas de plataforma): segue pendente (menor; visão de governança).
3. **Drill-down §27-208:** segue proposta/pendente (B19) — agora **cinco** telas (BI + Dashboard + Meu Dia + Command Center + Admin360 Home) exibem definições (base para linkar "ver registros").
4. **Caveat honesto (documentado, sem mudança):** o resumo da Home é **estado-atual** (sem janela de data; fuso irrelevante pois o filtro é só `tenant_id` + status/reg_status). Os 4 cards e os 4 tiles de "áreas" repetem as mesmas contagens (os tiles são atalhos de navegação). Nenhuma regra inventada.

## 6. Fila seguinte (R8) — auditoria além do top-10

§27-205: **BI (B20) + Dashboard (B21) + Meu Dia (B22) + Command Center (B23) + Admin360 Home (B24) entregues**; residual = **demais dashboards Admin360** (Gestao/Dashboard, FluxoCaixa estático, listas somadas no Razor, detalhes) + **view global do CC**. Próximos: **Gestao/Dashboard** (maior impacto, 14 tiles) ou **FluxoCaixa** (alinhar estático→emissão), ou outra área §27/§28 pela matriz, ou janela viva de homologação / push.

## 7. Gotchas aprendidos (reutilizáveis)

- **Record DTO consumido pelo Dapper + campos novos:** `{ get; init; }` não-posicionais mantêm o ctor posicional que o Dapper resolve por colunas inalterado; popula-se com `with { … }`. (Regra B22/B23/B24.)
- **View que SEMPRe renderiza cards (mesmo em erro):** a Home Admin360 mostra os 4 cards com zeros quando há erro (diferente de Meu Dia/CC que têm ramo `else`). Gatear o bloco de definições em `!Model.Erro` evita exibir "como é calculado" ao lado de valores zerados por falha.
- **Área sem camada `*WebService`:** Admin360 chama a API direto pelos controllers Web; o espelho do DTO vive em `Models/Administrativo360Models.cs` (não num arquivo de services). Localizar o mirror = buscar o tipo usado em `ReadApiResponse<T>`.
- **Levantamento de área ampla primeiro:** para uma área grande (várias sub-telas), mapear TODOS os pontos que exibem KPI cards + sua origem (SQL vs. soma no Razor vs. valor gravado) ANTES de fatiar — evita escolher alvo sem fórmula fiel ao SQL para espelhar (ex.: total somado no Razor, cuja contagem vive apenas na view).
