# Relatório BLOCO D — Template compartilhado de alertas, cookie de sessão e refatoração do controller Administrativo360

| Campo | Valor |
|---|---|
| Data | 2026-09-28 |
| Ambiente | Local dev (Windows) — API `https://localhost:51977` · Web `https://localhost:52977` |
| Build em teste | HEAD `825ac8d` + mudanças deste bloco no working tree — compilado 2026-09-28 03:00 UTC-3 (`build-20260928-030039.log`), servers no ar 03:01 (`last-run.json`) |
| Banco | PostgreSQL 18 — banco `postgres`, schema `plantaopro` |
| Conta principal | `gestor@santacasa-demo.example` (Admin. do cliente, tenant Santa Casa `d3f6584c-…-7502`) |
| Cenários automatizados | `sc_d1v2.ps1` (captura byte-a-byte Cadastros/FluxoCaixa + re-captura dos 4 snapshots) · `sc_smoke.ps1` (26 endpoints) · helpers `bc_common.ps1` (curl + psql + jar) |

Status: **COMPLETO** — os 4 itens do bloco (D1–D4) APROVADOS com verificação byte-a-byte/funcional; **P2** (pendência herdada) RESOLVIDO por diagnóstico empírico. Sem novos módulos nesta execução; nenhum fix já concluído nos blocos A–C foi reapresentado como funcionalidade nova.

---

## 1. D2/P8 — Causa raiz do cookie ~14 KB e correção (RESOLVIDO)

### Causa raiz
O principal de sessão `PlantaoPro.Auth` era fragmentado pelo CookieManager em **4 cookies** (`PlantaoPro.Auth` + `AuthC1..C3`, ~14 KB no total — acima da diretriz de ~4 KB por cookie). O payload (base64) continha duas fontes de peso:

1. **JWT da API embutido** nos claims (~2,8 KB);
2. **48 claims individuais `permission`** (uma por permissão do perfil).

### Correção (`SessionClaimsBuilder.cs` + consumidor)
- O **JWT saiu do cookie**: a cópia canônica passa a viver na sessão local (`AccountController` grava `"jwt"` no login e no `RefreshContext`) e é a fonte primária de `BaseWebController.GetJwtToken()`. A revogação/isolamento foram preservados — o fluxo da Jornada F continuou aplicando revogação só com refresh de contexto, sem novo login.
- As **48 permissões viraram UMA claim combinada `permissions`** (separador vírgula).
- Consumidor (`PermissionService.HasAccessClaim` em `AccessServices.cs`): lê a claim `permissions`; **fallback para as claims individuais `permission`** nos cookies emitidos antes do fix (tickets em voo seguem válidos até expirarem).
- Testes ajustados à nova forma de claim (`RoleCatalogAndSessionClaimsTests`, `V2156SessionSecurityTests`).

### Verificação ao vivo
| Check | Esperado | Real |
|---|---|---|
| Cookies após login | 1 único | ✅ `PlantaoPro.Auth` (valor com 3.184 B em base64), zero `AuthC*` |
| Dashboard | sem regressão | ✅ byte-idêntica ao snapshot BLOCO C (57.754 chars) |
| Recusas (auditoria) | estáveis | ✅ ACESSO_NEGADO estável em 124 (sem delta espúrio) |
| Jornada F (revogação ao vivo) | 8/8 sem re-login | ✅ 8/8 |
| Suíte | verde | ✅ 668/668 (flake conhecido reobservado/repassado — §6) |

**Trade-off documentado:** as sessões são in-memory neste ambiente dev — o restart zera o store e exige re-login para `RefreshContext` (comportamento pré-existente, não causado pelo fix). Follow-up recomendado caso o catálogo cresça: store server-side de permissões (cookie só id + tenant + hash) — registrado em `PENDENCIAS-ROADMAP.md` §3.3.

## 2. D3 — Separação do controller monolítico (CONCLUÍDO)

Split **mecânico** (zero mudança de lógica) do `Administrativo360Controller` (~2.500 linhas no HEAD) em parciais por domínio de negócio:

| Arquivo | Linhas |
|---|---:|
| `Administrativo360Controller.cs` (main — DI/helpers transversais) | 103 |
| `Administrativo360Controller.Financeiro.cs` | 655 |
| `Administrativo360Controller.EstoqueValorizacao.cs` | 373 |
| `Administrativo360Controller.Orcamentos.cs` | 339 |
| `Administrativo360Controller.Vales.cs` | 273 |
| `Administrativo360Controller.Cadastros.cs` (já existia; +136 linhas do split) | 262 |
| `Administrativo360Controller.ComprasRecebimentos.cs` | 249 |
| `Administrativo360Controller.Relatorios.cs` | 167 |
| `Administrativo360Controller.Cirurgias.cs` | 129 |
| `Administrativo360Controller.RelatoriosFinanceiros.cs` | 133 |
| `Administrativo360Controller.Vendas.cs` | 73 |

**Verificação:** build limpo (0 erros) · suíte 668/668 pós-split · smoke 26/26 endpoints · dashboard byte-idêntica (57.754 chars) — rotas/atributos/acções intactos provados por comportamento, não só compilação.

## 3. D4 — Foco de teclado na mensagem de contexto do login

A mensagem exibida quando a chegada traz `?reason=…` (sessão expirada, logout etc.) agora é alcançável por teclado/leitor de tela, sem alterar a identidade visual:

- `Views/Account/Login.cshtml`: `tabindex="-1"` no div `.pp-auth-context-message` (foco programático, sem entrar na ordem de tabulação).
- `wwwroot/js/auth-login.js`: `else if (contextMessage)` com `window.queueMicrotask(() => contextMessage.focus())` — mesmo padrão já usado para o `errorSummary`, executado somente quando não há resumo de erro preenchido (prioridade do erro mantida), com comentário documentando a chegada via `?reason=`.

## 4. D1 — Template compartilhado de alertas `_AlertasAcao` (byte-idêntico)

### Contexto
As 25 views de Administrativo360 duplicavam inline o bloco de alertas success/error, em **duas variantes**: *elevada* (`border-0 shadow-sm` + ícones `-fill`) em 7 views (Cadastros, Coleta, Inspecoes, Inventarios, Movimentacoes, PedidosCompra, Recebimentos) e *padrão* nas 18 restantes. A refatoração consolidou tudo num único partial `_AlertasAcao.cshtml`, controlado pela flag `ViewData["AlertaElevado"] = true;` nas 7 elevadas — preservando exatamente a identidade de cada variante.

### Diagnóstico byte-a-byte da primeira conversão
A conversão inicial (tag em indentação 4, chaves fechadas em linha própria) produziu em **todas as 25 views** dois deltas sistemáticos, explicados pelas regras de whitespace do Razor:

| Delta | Causa raiz |
|---|---|
| **+4 espaços** no topo (12sp vs 8sp antes do `<div class="alert…`) | os espaços **antes** da tag `<partial>` (indentação da view) são emitidos literalmente e somaram aos 8 espaços internos do parcial |
| **+1 LF** no fundo (3 LFs vs 2 antes do `<!-- Header -->`) | quebra da code-line `    }` truncada + newline **pós-tag** `<partial/>` emitido + terminação do parcial — efeito líquido: 1 LF a mais que no bloco inline original |

Obs.: a variante **padrão** tinha os mesmos deltas de topo/fundo — apenas o interior dos blocos era idêntico; a primeira verificação por regex a partir do `<div` tinha mascarado isso.

### Iterações do fix
1. **v2** — tag `<partial>` em **coluna 0** nas 25 views + chaves coladas `</div>}` no parcial + guarda `ViewData.ContainsKey("AlertaElevado") && …` (segura para as 18 views que não definem a chave). Resolveu topo e fundo, mas revelou um terceiro caso: **alertas empilhados** (success + error simultâneos — o caso real do FluxoCaixa) perderam o LF entre blocos (delta −1). Com as duas chaves coladas o trecho entre blocos fica **código puro** e o Razor truncou todos os seus newlines; no código original havia um newline de markup após `</div>` que sobrevivia.
2. **v3 (forma final)** — parcial **100% código**: zero nós de texto de markup; os blocos são construídos como strings C# com os bytes exatos (classes, ícones, indentações 8/12 espaços) e emitidos via `@Html.Raw(string.Join("\n", partes))`. As mensagens passam por `System.Text.Encodings.Web.HtmlEncoder.Default` — o mesmo encoder default do Razor — então a codificação (`é → &#xE9;` etc.) fica idêntica. Sem texto de markup no arquivo, não existe comportamento de whitespace contextual que possa divergir.

### Evidência final (sessão fresca, sequência determinística reproduzível)
Sequência executada (`sc_d1v2.ps1`): psql lê o parceiro (`ativo=f`) → GET `Cadastros?aba=parceiros` → POST `AlternarStatusParceiro` com o **valor atual** (endpoint **param-wins**, confirmado no código: `PATCH …/parceiros/{id}/status?ativo={ativo}` — não é flip; o estado permanece estável entre capturas) → GET; em seguida GET `FluxoCaixa` → POST `CriarContaFinanceira` vazio (erro validado, sem dado novo) → GET. O SuccessMessage do toggle aparecer também no FluxoCaixa ("leak") é comportamento determinístico pré-existente dessa sequência e se reproduz **simétrico** nos baselines OLD e nas capturas novas.

| Comparação (captura nova vs baseline pré-conversão) | Veredito |
|---|---|
| Página inteira Cadastros (elevada, alert success) | ✅ IDEM módulo tokens antiforgery (delta 0; 10 linhas apenas de token) |
| Janela `<!-- Feedback messages -->..<!-- Header -->` (Cadastros) | ✅ **byte-exata (358 chars)** |
| Página inteira FluxoCaixa (padrão, success+error empilhados) | ✅ IDEM módulo tokens antiforgery (delta 0; 1 linha de token) — LF entre blocos preservado |
| Sufixo após último alerta + ausência de vazamento de texto interno | ✅ OK / OK nas duas páginas |
| 4 snapshots BLOCO C re-capturados (Gestao Dashboard, Cotações, DocumentosXml, Financeiro) | ✅ 3 byte-exatos + dashboard IDEM (3 tokens) — nenhuma das 4 telas mapeia para as 25 views convertidas |
| Estado do parceiro pós-run | ✅ `f → f` (estável) |

### Idiom final documentado (regra de manutenção)
- A tag `<partial name="_AlertasAcao" />` fica em **coluna 0** — qualquer indentação anterior é emitida literalmente no HTML.
- O parcial **não contém nós de texto de markup** (todo o HTML vive em strings C#) e o arquivo termina **sem newline final**.
- A variante é controlada exclusivamente por `ViewData["AlertaElevado"]`, lida com guarda `ContainsKey`.

## 5. P2 — Anomalia `POST /Account/*` → 405: resolvida por diagnóstico empírico

- **Síntoma original (09-26):** `POST /Account/Login` e `POST /Account/RefreshContext` → 405 `Allow: GET`, enquanto os GETs e o `POST /` funcionavam.
- **Reprodução neste bloco (build fresco):** os 4 POSTs — `/Account/Login`, `/`, `/Account/RefreshContext`, `/Account/Logout` — retornam **302 corretos**, com revogação real no logout (Set-Cookie expired). O 405 **não se reproduziu**.
- **Causa raiz do falso sintoma:** bug nos scripts de teste — PowerShell 5.1 descarta argumento string vazio ao chamar executável nativo; o helper passava `--data ''` e o curl engolia o próximo flag, corrompendo a requisição (lição registrada no relatório BLOCO C §9; os helpers omitiram `--data` quando o body é vazio).
- **Conclusão:** roteamento do produto coerente; item fechado com evidência empírica. Cobertura contratual contínua já existe na matriz BLOCO C: M1 (POST do form de login) e M13 (logout GET+POST).

## 6. Infraestrutura de verificação (ciclo desta execução)

- Ordem limpa usada: **stop → build → test → start** (`scripts/local/run-dev-*.ps1`); o start dá timeout falso no foreground (pipe herdado) → validação por `last-run.json` + readiness (health da API / form da Web).
- Suíte: **668 testes**; o flake conhecido `IsolamentoCadastros_ValidacoesDeNegocio_DuplicidadeEBloqueios` foi reobservado (667/668 na corrida completa → 1/1 isolado → **668/668** na reexecução imediata) — padrão de timing/concorrência, sem relação com as mudanças do bloco (documentado em `PENDENCIAS-ROADMAP.md` §3.3).
- Smoke: **26/26** endpoints 200 com conteúdo + confirmação da identidade do dashboard BLOCO C.

## 7. Arquivos alterados neste bloco

| Item | Arquivos |
|---|---|
| D1 | `Views/Administrativo360/_AlertasAcao.cshtml` (novo) + 25 views de Administrativo360 (tag em coluna 0; as 7 elevadas mantêm a linha de flag) |
| D2/P8 | `Security/SessionClaimsBuilder.cs` · `Services/Security/AccessServices.cs` · `Tests/RoleCatalogAndSessionClaimsTests.cs` · `Tests/V2156SessionSecurityTests.cs` |
| D3 | `Controllers/Administrativo360Controller.cs` (main esbeltido) + 10 parciais (9 novos + `…Controller.Cadastros.cs`) |
| D4 | `Views/Account/Login.cshtml` · `wwwroot/js/auth-login.js` |
| Evidências/documentação | `artifacts/homologacao/evidencias-html/` (4 snapshots BLOCO C, commitados) · `artifacts/homologacao/PENDENCIAS-ROADMAP.md` (P2/P8 → RESOLVIDOS) · este relatório |

## 8. Vereditos consolidados

| Item | Status |
|---|---|
| D1 — template `_AlertasAcao` compartilhado | ✅ APROVADO — identidade byte-idêntica nas duas variantes (elevada e padrão), incluindo alertas empilhados |
| D2 — causa raiz do cookie ~14 KB | ✅ RESOLVIDO — JWT em sessão local + claim única `permissions`; cookie único (3.184 B); revogação/isolamento preservados (Jornada F 8/8) |
| D3 — split do controller monolítico | ✅ CONCLUÍDO — 11 arquivos, zero mudança de lógica, 668/668 + smoke 26/26 |
| D4 — foco da mensagem de contexto no login | ✅ APROVADO — teclado/leitor de tela atendidos sem mudança visual |
| P2 (pendência herdada) | ✅ RESOLVIDO por diagnóstico empírico (falso sintoma de scripts de teste) |
| Roadmap | atualizado: P2 e P8 → RESOLVIDOS; flake e trade-offs documentados |
