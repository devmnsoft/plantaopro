# R8-B19 — Zero ≠ informação indisponível: falha mascarada como zero em Bi/MeuDia (§27 linha 206)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia da **auditoria ALÉM do top-10 §29**: §27 linha 206 ("Zero ≠ informação indisponível" — falha mascarada como zero em Bi/MeuDia) + linha 208 (drill-down). Primeira fatia além do top-10.
- **Data:** 2026-10-13
- **Status:** IMPLEMENTADO + TESTADO (fix do Bi por testes executados). Meu Dia = **existente+comprovada** (sem mudança). Drill-down (§27-208) = **proposta/pendente** (decisão de design). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica: o código de Bi + Meu Dia, não o snapshot da varredura)

A linha 206 da matriz dizia: "distinguido em Operacional/Home Dashboard/Pagamentos/_EmptyState; **Bi/Index e MeuDia exibem 0 em falha de API** (`??0`/`toFixed` sem catch)". A validação por fonte canônica **separa os dois casos** — um era verdade, o outro estava desatualizado:

**Bi/Index.cshtml — REAL bug (corrigido nesta fatia).** KPIs hidratadas por JS: `fetch('/api/bi/resumo-executivo').then(r=>r.json()).then(x=>{const d=x.data||{}; …d.<campo> ?? 0…}).` **sem guarda de sucesso HTTP e sem `.catch`.** Consequência: qualquer falha — 403 (guard de módulo não contratado), 404, 5xx, rede fora, corpo não-JSON — fazia a cadeia cair em `x.data` nulo/ausente e o `??0` renderizava `0`/`R$ 0,00`/`0%`, **mascarando "indisponível" como zero** (violação direta da regra "Zero ≠ informação indisponível"). O endpoint é o da API (o `BiController` Web só expõe actions de view); o fix é corretamente **client-side**, porque o masking acontece no cliente.

**MeuDia/Index.cshtml — snapshot DESATUALIZADO (já distingue; sem mudança).** Não é JS-hidratado: os KPIs vêm do model server-side (`@Model.Summary.Active/Critical/Today/Overdue`). O `ProductivityWebService.GetAsync` **mapeia cada classe de falha para um `ErrorKind`+`Error` distintos** — 401 `NAO_AUTENTICADO`, 403 `SEM_PERMISSAO`, 5xx `FALHA_SERVIDOR`, outro status, corpo nulo/JSON inválido `RESPOSTA_INVALIDA`, cancelamento `CANCELADO`, timeout `TIMEOUT`, rede `TRANSPORTE`. A view, quando `Model.Error` está preenchido, **oculta o bloco `day-kpis`** e mostra um painel de erro ("Não foi possível montar seu dia" + mensagem + *Tentar novamente*). Ou seja: Meu Dia **já não confunde zero com indisponível**; a menção a "`??0`/`toFixed` sem catch" no Meu Dia era defasada.

Referências in-repo espelhadas no fix do Bi: `V112/OperationalPage.cshtml` (`if (!response.ok) throw new Error(...)`) + `Operacoes/Index.cshtml` (`.catch` → `alert-warning` "Dashboard indisponível…").

## 2. Decisão e mudança (sem regra inventada, sem regra clínica)

Regra aplicada: **"Zero ≠ informação indisponível"** — uma falha na consulta exibe estado de indisponibilidade (não zero); um **zero real** (API 200 com valor 0) continua exibindo 0.

- **Bi/Index.cshtml** (única mudança de produção):
  - Guarda de sucesso HTTP: `.then(r=>{if(!r.ok){throw new Error('bi-resumo-'+r.status);}return r.json();})` — 403/404/5xx deixam de cair no `json()`/`??0`.
  - `.catch` devolve **todos os KPIs ao placeholder `-`** (o MESMO usado na inicialização server-side dos cards — claramente **não** zero) + `text-muted` + revela um `alert-warning` honesto (`#bi-status`: "Indicadores indisponíveis no momento: não foi possível consultar o serviço BI neste instante.").
  - Caminho de **sucesso preserva `??0`** (zero real segue 0) e esconde o banner (suporta retry).
  - Markup: adicionado `<div id="bi-status" class="alert alert-warning d-none mb-3" role="alert"></div>`.
- **MeuDia:** nenhuma mudança — já distingue falha de dado (existente+comprovada).
- **Drill-down (§27 linha 208):** **proposta/pendente** — estender "abrir registros que compõem o total" aos cards Bi/Premium depende da **definição das métricas** (residual de R8-B9 "Web não exibe `Definicoes`" + Meu Dia/CommandCenter/Admin360 fora do §29-1, registrado em B18). Fazer agora exigiria escolher para qual conjunto de registros cada link aponta **sem a definição** = inventar regra. Mantido pendente, coberto pelo contrato anti-drift.

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Web/Views/Bi/Index.cshtml` | **Produção (única):** guard `if(!r.ok) throw` + `.catch` devolvendo KPIs ao placeholder `-` + banner honesto `#bi-status`; sucesso preserva `??0`. |
| `backend/PlantaoPro.Tests/R8B19BiMeuDiaEstadoDeErroContratoTests.cs` | **Novo:** 3 fatos anti-drift — (1) Bi guarda falha de API + devolve KPIs ao placeholder (não zero) + banner + preserva `??0`; (2) Meu Dia segue server-rendered com estado de erro (sem hidratação JS que zere); (3) `ProductivityWebService` mapeia cada classe de falha para erro honesto. |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §27 linha 206 → **corrigido (R8-B19)** + Meu Dia **existente+comprovada**; linha 208 (drill-down) → nota R8-B19 (mantém-se proposta/pendente, depende das definições). |

## 4. Evidência (executada)

- **Build sln completa:** EXIT 0 (0 erros; apenas avisos pré-existentes).
- **Alvo R8B19:** `R8B19BiMeuDiaEstadoDeErroContratoTests` = **3/3 aprovados** (`--filter FullyQualifiedName~R8B19`).
- **Gate phaseA v2:** **EXIT 0** (RAM livre 0,48 GB ao iniciar).
- **Full suite v2:** **Total de testes: 1370** em TODAS as 3 rodadas (baseline 1367 + 3 novos). **full2 e full3 limpos**; **full1 teve 1 falha** `Administrativo360CotacoesXmlDashboardTests.Aceite17_PersistenciaDuravel_PreservaRegistrosEFila` (`Assert.Equal` Expected 3370 / Actual 3371 — contagem off-by-one de estado de banco). **Isolado e reexecutado 5× = 5/5** → **transiente** (o mesmo binário `--no-build` passou em full2/full3 e isolado; nenhuma relação com a fatia — mudança apenas na view Bi + um contrato source-read).
- **Verificação byte-level:** `git status` = view Bi alterada + teste novo (+ docs selados); nenhum outro arquivo de produção tocado.

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Drill-down §27 linha 208** (abrir registros que compõem o total nos cards Bi/Premium): proposta/pendente — depende da definição das métricas (R8-B9 residual + B18). Contrato `R8B19…` força decisão consciente.
2. **Verificação JS-comportamental em browser headless:** o contrato prova a estrutura (guarda/`.catch`/banner) por leitura de fonte; o comportamento real do `fetch` sobre o DOM exigiria browser headless (fora da infra de teste atual — xUnit/TestServer não executa JS). Mesma restrição dos demais itens client-side da rodada.
3. **Meu Dia UTC "today" window caveat + consolidação de facade** (dívida B17): invariado.

## 6. Fila seguinte (R8) — auditoria além do top-10

Top-10 §29 completo (B9–B18). Em andamento a auditoria além do top-10: **§27-206 entregue** (Bi fix + Meu Dia comprovado); **§27-208 (drill-down) registrado como pendente**. Próximos: continuar a fila §27/§28 além do top-10 pela matriz, ou janela viva de homologação (`r6a1b` §7), ou push (local `main` ahead do `origin/main`).

## 7. Gotchas aprendidos (reutilizáveis)

- **"Falha ≠ zero" começa no transporte:** um `.catch` sozinho **não basta** se o endpoint responde HTTP-erro com corpo JSON válido (o `json()` não lança e o `??0` zera). É preciso **guardar `r.ok` ANTES de parsear** (padrão `V112/OperationalPage`): `if(!r.ok) throw`.
- **Snapshot de varredura pode misturar dois casos opostos numa linha:** aqui "Bi e MeuDia exibem 0 em falha" — um era verdade (Bi, JS-hidratado), o outro defasado (MeuDia, server-rendered c/ estado de erro). Validar **caso a caso na fonte** antes de agir.
- **Off-by-one em teste de persistência = flake de estado de banco:** `Expected N / Actual N+1` num teste "PersistênciaDurável" que passa nas rodadas seguintes (mesmo binário) é estado acumulado/tempo, não regressão — isolar e rerun (5×) antes de declarar transiente.
