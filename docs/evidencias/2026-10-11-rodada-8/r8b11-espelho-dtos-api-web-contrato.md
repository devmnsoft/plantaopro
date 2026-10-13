# R8-B11 — Mirror de DTOs Api↔Web ×4: contrato anti-drift + espelho de atalhos corrigido (§29 item 3)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia "Mirror de DTOs Api↔Web ×4" (item 3 da fila §29, ordem quebrado→alto impacto: … → B8 → B9 → B10 → **B11**)
- **Data:** 2026-10-13
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica)

- Matriz `r8a0-auditoria-inicial-matriz-estado.md` §29 item 3 (linha 229): **"Mirror de DTOs Api↔Web ×4"** (ManagerCommandCenter, Admin360Resumo, ProductivityPage, DashboardPremium). Varredura `r8a0-varredura-indicadores-design-redundancias.md` Área C3 (linhas 122–127): os 4 pares + "OBS: espelhamento padrão do BFF; safe a consolidar só se os VMs não ganharem campos de view."
- **Ground truth (leitura dos tipos + mappers):** NÃO há mapper campo-a-campo escrito à mão. Cada camada Web faz `ReadFromJsonAsync<TViewModel>` contra um tipo **duplicado** que vive no projeto Web — o acoplamento é **APENAS pelo nome da propriedade JSON** (sem vínculo de compilação entre a DTO Api e o VM Web). Consequência: renomear/adicionar um campo na DTO Api faz o valor cair em **silêncio** no VM Web (sem erro, sem falha).
- **Forma encontrada por par:**
  - `Administrativo360Resumo`: mirror **byte-a-byte 1:1** (`Dto(long Departamentos, long Cargos, long ColaboradoresAtivos, long ContratosVigentes)` = VM idêntico). Candidato mais forte a consolidação.
  - `ManagerCommandCenter`: mirror **aninhado** por nomes (`ManagerCommandCenterDto↔…ViewModel` + aninhados `CommandCenterSummary↔ManagerSummary`, `CoverageItem↔ManagerCoverage`) — todos os nomes batem (incl. `PlatformHealth`).
  - `ProductivityPage`: VM é **superset** (multi-endpoint; ganha estado de view `Error/ErrorKind/CanViewTeam` + coleções `Summary/QuickActions/Agenda`) e **não** espelha `TotalPages`, `Items[].SourceUpdatedAt`, `Items[].IsSnoozed` da DTO.
  - `DashboardPremium`: **divergente**. Kpis: DTO `(Titulo, Valor, Fonte, Definicao)` vs VM `(Titulo, Valor, Unidade, Tendencia)` → gaps api-only `Fonte`/`Definicao` + web-only `Unidade`/`Tendencia` (a DTO já não envia após o reshape do KPI em R8-B9). Atalhos: DTO `(Titulo, Url)` vs VM `(Texto, Url, PerfilMinimo)` → **mismatch de nome `Titulo`↔`Texto`**.

## 2. Bug corrigido nesta fatia

- **Espelho de atalhos do DashboardPremium quebrado:** a API monta `new DashboardAtalhoDto("Operação Inteligente", "/OperacaoInteligente/Index")` (campo `Titulo`), serializa `{titulo, url}`; o VM `DashboardAtalhoWebViewModel` esperava `(Texto, Url, PerfilMinimo)` → `titulo` não tinha contraparte → `Texto` ficava **vazio**. O template `Views/DashboardsPremium/Perfil.cshtml:40` renderiza `@atalho.Texto` como rótulo do botão → **todas as páginas `/DashboardsPremium/*` exibiam atalhos com rótulo VAZIO** (só a URL funcionava). Corrigido alinhando o nome (`Texto→Titulo` no VM + no template), **sem mudar o formato do wire** (a API já emitia `titulo`).

## 3. Decisões (sem regras inventadas)

1. **Deliverable = contrato anti-drift + fix do espelho quebrado, não merge físico.** Rito §29 (mapa: "comparar consumidores → canônico → migrar → regressões", sem deleção antecipada): a consolidação física (um único contrato compartilhado Api↔Web) segue o rito — aqui o **risco de deriva** vira **contrato executado**.
2. **Direção do contrato = Api→Web** (todo campo que a API pode enviar deve ser **recebível** por nome no VM). É essa direção que causa perda silenciosa de dados. Campos extras do VM (estado de view) são legítimos e não falham.
3. **Omissões intencionais documentadas** (lista curada no teste, vigiada contra crescimento silencioso pelo teste de "fantasma"): `Kpis.Fonte` (proveniência interna), `Kpis.Definicao` (dívida R8-B9: renderização de Definições pendente), `TotalPages` (derivável de Total/PageSize), `Items.SourceUpdatedAt` (frescor técnico), `Items.IsSnoozed` (estado derivado na view).
4. **Reverse drift documentada, não "consertada":** `Unidade`/`Tendencia` no card KPI web já não têm fonte na DTO (reshape do KPI em R8-B9) — decisão de conteúdo de card = pendência de produto, fora do escopo do contrato de espelho.
5. **Sem deleção/merge de tipos, sem nova coluna, sem DDL.** Mudanças restritas a 1 renomeação de propriedade + 1 binding de template + 1 novo arquivo de teste.

## 4. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Web/Models/SaasEvolutionWebViewModels.cs` | `DashboardAtalhoWebViewModel.Texto` → **`Titulo`** (alinha com `DashboardAtalhoDto.Titulo`). |
| `backend/PlantaoPro.Web/Views/DashboardsPremium/Perfil.cshtml` | `@atalho.Texto` → **`@atalho.Titulo`** (rótulo do botão de atalho). |
| `backend/PlantaoPro.Tests/R8B11EspelhoDtosApiWebContratoTests.cs` | **Novo**: 3 fatos — (1) contrato recursivo Api→Web nos 4 pares (nenhum campo da DTO fica sem contraparte de nome, além das omissões intencionais); (2) guarda anti-fantasma da lista intencional; (3) round-trip JSON do DashboardPremium provando que o `Titulo`+`Url` do atalho fluem (regressão do bug). |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §29 item 3 (linha 229) → **R8-B11**; resumo "Estado" (§29) refletindo itens 1+2+3. |

## 5. Evidência (executada)

- **Build sln completa:** EXIT 0 (0 erros, 73 avisos pré-existentes).
- **Alvo:** `R8B11EspelhoDtosApiWebContratoTests` = **3/3 aprovados** (run isolado, `--filter FullyQualifiedName~R8B11`).
- **Gate phaseA v2 (pipeline):** **52 testes, EXIT=0**.
- **Full ×3 (pipeline v2):** total agora **1346** (1343 baseline + 3 novos). full1 **1346/1346 EXIT=0**; full2 **1346/1346 EXIT=0**; full3 **1346/1346 EXIT=0**. **Sem flake esta rodada** (catálogo ① inalterado).
- **Verificação byte-level:** VM `DashboardAtalhoWebViewModel` expõe `public string Titulo`; template `Perfil.cshtml` usa `@atalho.Titulo`; teste contém os 3 `[Fact]`, o walker recursivo e a lista intencional com exatamente os 5 caminhos detectados. Árvore limpa: só os 3 arquivos acima alterados/criados (+ matriz).

## 6. Dívida registrada (fora do escopo desta fatia)

1. **Consolidação física do espelho BFF (§29 item 3, rito):** esta fatia contratou a correspondência por nome + corrigiu o espelho quebrado; a dedup física (um único contrato compartilhado, ou VM derivado da DTO) segue o rito (comparar → canônico → migrar → regressões), sem merge antecipado.
2. **Card KPI sem fonte para `Unidade`/`Tendencia`** (pós-R8-B9): os campos continuam vazios porque a DTO `DashboardKpiDto(Titulo, Valor, Fonte, Definicao)` não os envia. Decisão de produto: manter/remover esses campos no card. Fora do contrato de espelho.
3. **Renderização de Definições de KPI em tela** (dívida R8-B9): `Kpis.Definicao` existe na DTO mas o VM web não exibe — segue pendente (listada na lista intencional deste contrato).
4. **Main DB em v2339 / homologação viva:** invariado — janela segue em `r6a1b` §7 (+ lote v2340–v2351 + IIS).

## 7. Fila seguinte (R8)

Com §29 item 3 entregue (contrato anti-drift + espelho de atalhos corrigido), o próximo item da fila §29 = **item 4: Placeholder "Sem dados no período" duplicado em 5 views + `BiController` 8 ações/4 views (views-fantasma)**. Alternativas: janela viva de homologação (`r6a1b` §7), ou o push da rodada (pendente).

## 8. Gotchas aprendidos (reutilizáveis)

- **Espelhamento BFF por nome JSON:** quando o Web faz `ReadFromJsonAsync<TViewModel>` contra um tipo duplicado, a correspondência é **só por nome** — não existe vínculo de compilação; renomear/adicionar campo num lado quebra o outro **em silêncio**. Um contrato recursivo por nome (reflexão, seguindo coleções/objetos) é a forma de monitorar.
- **Dois tipos `DashboardChartItem`** coexistem: `PlantaoPro.Api.Models.DashboardChartItem` (classe, usada pelos serviços Api) e `PlantaoPro.Web.Models.DashboardChartItem` (record, usado pelo dashboard overview web). Ao referenciar ambos num projeto que importa os dois namespaces, use **alias** (`using X = PlantaoPro.Api.Models.DashboardChartItem;`).
- **Caminho de dados do espelho:** a rota pode ter 2 saltos — `DTO Api →(JSON)→ ApiViewModel →(cópia manual campo-a-campo no controller)→ PageViewModel → View`. O ponto de quebra silenciosa é o 1º (JSON por nome); o 2º é cópia manual (risco próprio de esquecimento).
