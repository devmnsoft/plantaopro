# Rodada 19 — Smoke vivo dos endpoints tocados (r19b2)

- **Data:** 2026-10-18 · **Binário:** build Release `--no-incremental` pós-r19b1b (gate 0E/80W chaves idênticas ao pristino `r18b4_build.log`; suíte 1518/1518) · **Apps:** Api `51976` / Web `52976`, subidos com `dotnet run -c Release --no-build` + `ConnectionStrings__Default` (Host=127.0.0.1; Database=plantaopro; Search Path=PlantaoPro,public) · Logs: `%TEMP%\opencode\r19_api_out2.log` / `r19_web_out2.log`.
- **Contrato de prova:** para cada defeito corrigido, o smoke exibe o estado anterior (500) e o posterior (200 com dado materializado) no **mesmo banco operacional** e, quando possível, com a **mesma linha de produção** que falhava.

## Seeds (net-zero)

| Tabela | Id | Tenant | Uso |
|---|---|---|---|
| `ocorrencias_operacionais` | `aaaa…019` | médico (`8b0c…ca001`) | dado vivo p/ listagem no tenant médico |
| `ocorrencia_eventos` | `aaaa…029` | médico | evento da ocorrência `aaaa…019` |
| `work_items` | `aaaa…039` | médico | item p/ o drawer de histórico |
| `work_item_history` | `aaaa…049` | médico | linha CRIAR do histórico do item |
| `ocorrencia_eventos` | `aaaa…099` | Santa Casa (`d3f6…7502`) | evento na **ocorrência de produção** `d3f6584c-…-7591` (a linha que 500ava) |

## Matriz papel × rota × endpoint

### Superadmin — F7d (`/api/clientes/central` + página `/Clientes/Index`)

| Probe | Antes (r19a0) | Depois |
|---|---|---|
| `GET /api/clientes/central` (BFF server-side) | 500 — etapa 1: `função unnest(record) não existe`; etapa 2: mismatch `Decimal` vs `long` | **200** — log do Api confirma `GET /api/clientes/central … 200` |
| Página `/Clientes/Index` (role ADMINISTRADOR_GLOBAL) | banner de falha/estado vazio incorreto | renderiza 20 linhas; linha "Santa Casa Demonstração" com rótulo de **17 módulos vigentes** e totais ("3 ativos") exibidos no rodapé |

Nota: o banner "Revise os campos destacados" naquela página é artefato cosmético sempre presente (pré-existente, não regressão — verificado igual antes/depois).

### Médico — F7e `WorkItemHistoryDto` (drawer Meu Dia)

Sessão do médico logada; o drawer (`work-item-drawer.js`, base `/bff/operacao`) foi exercitado via fetch same-origin:

```
GET /bff/operacao/work-items/aaaaaaaa-…-039/historico → 200
[{"acao":"CRIAR","criadoEm":"2026-10-18T08:32:23.644316Z","id":"aaaaaaaa-…-049","usuarioId":"…ca012", …}]
```

A linha semeada (≥1 linha, o gatilho exato do mecanismo F7b) materializa com `criadoEm` em `DateTime` UTC — antes do fix, a mesma consulta 500ava pela construção do deserializer do record.

### Gestor (tenant Santa Casa Demonstração — a linha de produção que 500ava) — F7e `OcorrenciaDto`/`OcorrenciaEventoDto`

Login direto na API (`POST /api/auth/login` → JWT) + probes Bearer, sem navegador:

| Endpoint | Antes | Depois |
|---|---|---|
| `GET /api/ocorrencias` (listagem) | **500** (1+ linhas no tenant, `OcorrenciaDto` record c/ 3 parâmetros DTOFF) | **200** com a linha de produção |
| `GET /api/ocorrencias/d3f6584c-…-7591` (detalhe) | **500** | **200** (`"Atraso na transição do plantão diurno"`, status ABERTA) |
| `GET /api/ocorrencias/d3f6584c-…-7591/historico` | **500** | **200** com o evento semeado: `[{"id":"aaaa…099","tipo":"CRIACAO","descricao":"Evento seed smoke R19 para a ocorrencia de producao","criadoEm":"2026-10-18T08:42:43.91822Z"}]` |

### Observação do smoke — página de UI `/Ocorrencias/Index?id=…` (fora do escopo do binding)

A página de detalhe não pôde ser capturada no navegador nesta execução: (i) o tenant médico recebe `AccessDenied?module=CATALOGO_NAO_CONFIGURADO` (módulo não contratado — guarda de rota, comportamento esperado); (ii) o tenant Santa Casa (gestor) loga com sucesso no servidor (log Web: cookie + token na sessão + redirect p/ `ClientePortal/Index`), mas o portal desse perfil rebota p/ login logo após (reproduzido também fora do browser, via curl com jar de cookies — ou seja, **comportamento de sessão/autorização do portal demo para ADMINISTRADOR_CLIENTE**, não artefato de ferramenta); (iii) superadmin também é barrado na guarda de catálogo (`CATALOGO_NAO_CONFIGURADO`) ao abrir `/Ocorrencias`. Todos os sintomas são **redirecionamentos/guardas, não 500**; os três endpoints que alimentam exatamente aquela página foram provados 200 com dado materializado na tabela acima — a prova de nível-API é o que cobre o defeito da rodada. Registrar como observação (não P1/P2): verificar o fluxo gestor→portal na próxima rodada que tocar em sessão/portal.

### Fantasma novo p/ a fila (P3)

`OcorrenciasController` (Web) faz `RedirectToAction("Index","Pendencias", new { module="OCORRENCIAS" })`, mas `PendenciasController.Index` **não aceita** parâmetro `module` (é a Central de Ações por `ProductivityQueryViewModel`) → a URL redirecionada cai em "CONTEÚDO INDISPONÍVEL". Redirecionamento legado/orfão — somar à fila de fantasmas residuais (item 9, R22).

## Limpeza e colagem

- Cleanup executado em transação única (ordem FK: history → items → eventos → ocorrência): `DELETE 1 / 1 / 2 / 1`, `COMMIT`. Contagem pós: **0** nas quatro tabelas para ids `aaaaaaaa-%`. Banco operacional net-zero (a ocorrência de produção `d3f6…7591` permanece intacta, apenas teve seu evento semeado removido).
- Apps parados após o smoke (portas 51976/52976 livres).
- Gotchas coletados p/ o guia: aba do browser pode travar em documento `502 Bad Gateway` antigo (body vazio, `htmlLen=39`) depois de navegação atropelada — todo probe seguinte roda contra o documento vazio e finge "login sem erro"; re-navegar descarta. Fluxo web reproduzível sem navegador: GET login (token antiforgery + cookie) → POST form-encoded (curl `--data-urlencode`, jar `-c/-b`) → 302 → GETs seguintes com o mesmo jar. PS5: regex do token exige tolerância à ordem dos atributos (`name=… type=hidden value=…`).
