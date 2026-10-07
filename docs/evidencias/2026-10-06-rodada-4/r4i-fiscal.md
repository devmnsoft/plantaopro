# R4-F · Fiscal — pré-emissão de NF-e (F1 dados+regras / F2 Web MVC)

- **Data**: 2026-10-07 (UTC) · **Rodada**: 4 · **Bloco**: Fiscal (d1-baseline §9 + §3.11)
- **Base da árvore**: `c84afc5` (consolidação p/ homologação).
- **Commits do bloco até aqui**:
  | Commit | Prefixo | Conteúdo |
  |---|---|---|
  | `48a600c` | R4-F1 | Fiscal pré-emissão: migração v2321 (parametros + pré-notas + itens), Domain/Application/Infrastructure, evento `EMISSAO_FISCAL`, suíte F1 57/57 (completa 1106/1106) |
  | `(este commit)` | R4-F2 | Web MVC do Fiscal: controlador + VMs + 4 views, nav/gate, credencial A33 no appsettings.Development + este documento |

---

## 1. Escopo e referências canônicas

Escopo aprovado nesta rodada: **F1 + F2 em 2 commits**; o F1 já foi commitado em `48a600c`, o F2 fecha com este commit. Fontes de verdade: `docs/evidencias/2026-10-06-rodada-4/d1-baseline-diagnostico.md` (§3.11, L127–131; item 9, L158).

Regras aplicadas ao desenho:

- **A29** — MVP = **pré-documento interno** + conferência de referências; **sem emissão autorizada**. P1 = `EmitirDocumentoFiscal` real (validar/assinar/transmitir/consultar/cancelar + XML/protocolo).
- **A30** — registrar parâmetros/pendências; **sem cálculo automático de tributos** (nenhuma coluna/campo de imposto).
- **A33** — **nunca** senha/token/certificado no banco: só o **nome da referência do segredo** (`certificado_referencia`, regex `^[A-Za-z0-9._\-]{1,120}$` aplicada na Web e na regra).
- **L33/H19** — nunca exibir "NF-e autorizada" sem evidência externa válida; **sem botões que sempre retornam sucesso**: todo avanço de situação ou devolve o motivo real de bloqueio ou grava o que realmente aconteceu (`ENVIANDO`, nunca `AUTORIZADA`).
- **L376** — **sem CFOP universal**: três campos por operação (`cfop_venda` / `cfop_remessa` / `cfop_retorno`) nos parâmetros e no formulário.
- **Decisão comercial ausente = pendência P2 explícita**: operação/UF/provedor/ambiente não definidos → status `PENDENTE_DE_CONFIGURACAO`; a homologação fiscal externa segue **BLOQUEADA até credenciais** fora desta máquina.

## 2. R4-F1 (`48a600c`) — dados e regras (resumo)

- **Migração `v2321`** (`database/migrations/2026_10_v2321_adm360_fiscal_parametros_pre_emitidas.sql`, aplicada em `plantaopro_test`):
  - `plantaopro.adm360_parametros_fiscais` (1 linha/tenant; `certificado_referencia` = nome do segredo, A33; `cfop_venda/remessa/retorno`, L376; `status` derivado);
  - `plantaopro.adm360_notas_pre_emitidas` (+ `ix_adm360_notas_pre_filtro`) — situação `RASCUNHO → PRONTA_PARA_EMISSAO → ENVIANDO → … → CANCELADA (terminal)`; invariante `CANCELADA` exige momento + motivo; `chave_acesso_externa` só por retorno externo (H19, 44 dígitos);
  - `plantaopro.adm360_nota_pre_emitida_itens` (+ `ix_adm360_notas_pre_itens_nota`).
- **Domain** (`PlantaoPro.Domain/Administrativo360/FiscalPreEmissoes.cs`): `NotaPreEmitidaRegras` — matriz de transições, `ValidarOrigem` (A29: origem não-MANUAL exige GUID do documento), `ValidarCancelamento`, `ChaveAcessoValida` (H19) e **`MotivoBloqueioEmissao(ParametrosFiscaisSnapshot?)`** — o bloqueio de "Emitir" é derivado dos parâmetros (null → "ainda não cadastrados"; `BLOQUEADO` → bloqueio externo declarado; credencial indisponível no ambiente → motivo correspondente; tudo ok → null = sem bloqueio). Nenhum caminho retorna sucesso fictício.
- **Application** (`FiscalPreEmissoesContracts.cs`): commands/DTOs do fluxo; **Infrastructure**: repositórios Dapper com isolamento por tenant e evento imutável/idempotente **`EMISSAO_FISCAL`** na trilha `adm360_eventos`.
- **Gate F1**: suíte F1 57/57; completa **1106/1106**.

## 3. R4-F2 — Web MVC

### 3.1 Arquitetura (decisões registradas)

- **Só Web MVC**: nenhum endpoint/policy novo na API, nenhum seed. O `PlantaoPro.Web.csproj` ganha `ProjectReference` para Application + Infrastructure e as páginas constroem os repositórios **por request**: `IConfiguration.GetConnectionString("Default")` + `ICurrentUserService` (scoped; `TenantId`/`UserId` obrigatórios — GETs fazem dupla checagem `ErroSemSessao`/`ErroSemConexao`; POSTs falham via `TempData["Error"]` → toast canônico).
- **Guard de rota**: `SaasRouteGuardFilter` ganhou `"Adm360FiscalWeb" => "ADM360"`; as ações têm nomes próprios e o guard resolve para `ADM360.VER` (mesma política fina do restante do ADM360 — sem policy nova).
- **Ação `Emitir` adicionada além da lista aprovada de ações** (documentado aqui, como combinado): justifica um botão cuja única função é devolver o **motivo real** de bloqueio quando os parâmetros não permitem (L33/H19) — sem ela a situação PRONTA ficaria sem saída visível e o bloqueio só apareceria como toast pós-clique.
- Views invocadas por path explícito (`View("~/Views/Administrativo360/Fiscal/*.cshtml", …)`); antiforgery em todos os POSTs; moeda exibida em pt-BR `N2`.
- **Nav** (`_SuprimentosNav.cshtml`): gate `podeFiscal = HasPermission("ADM360","VER")` incluído em `temAcessoAdm360`; grupo **Fiscal** entre Financeiro e Integrações (confirmado no DOM renderizado).
- **Credencial "disponível no ambiente" (A33)**: a checagem usa a chave `Fiscal:Credenciais:{referencia}` ser não vazia na `IConfiguration`. Em `appsettings.Development.json` entra neste commit `Fiscal:Credenciais:nfe-santacasa-demo` (valor dev — o segredo de verdade viverá no ambiente de produção, nunca no banco).

### 3.2 Rotas e ações

| Rota (`/Administrativo360/Fiscal/...`) | Ação | Função |
|---|---|---|
| `Configurar` GET | `Configurar` | Parâmetros + pendências P2 explícitas |
| `SalvarConfiguracao` POST | `SalvarConfiguracao` | Upsert dos parâmetros (referência de segredo validada) |
| `Notas` GET | `Notas` | Lista c/ filtro por situação |
| `Nova` GET / `SalvarNota` POST | `Nova` / `SalvarNota` | Criação em RASCUNHO c/ conferência de origem (A29) |
| `Detalhes/{id}` GET | `Detalhes` | Nota + evidência H19 + painel de emissão |
| `MarcarPronta` POST | `MarcarPronta` | RASCUNHO → PRONTA |
| `Emitir` POST | `Emitir` | PRONTA → ENVIANDO (só sem motivo real de bloqueio) |
| `Reabrir` POST | `Reabrir` | PRONTA/ENVIANDO/REJEITADA → PRONTA |
| `CancelarNota` POST | `CancelarNota` | Cancelamento interno c/ motivo (modal `[autofocus]`) |

### 3.3 Matriz de botões × situação (domínio + observado no E2E)

| Situação | Marcar como Pronta | Emitir (transmitir) | Reabrir (rótulo vivo) | Cancelar Pré-Nota |
|---|---|---|---|---|
| `RASCUNHO` | ✓ | — (oculto) | — | ✓ |
| `PRONTA_PARA_EMISSAO` | — | ✓ | "Reabrir em Rascunho" | ✓ |
| `ENVIANDO` | — | — | "Retornar para Pronta" | ✓ |
| `AUTORIZADA` / `CANCELADA` | — | — | — | — (+ texto "Situação terminal no MVP") |

No E2E cada linha exibiu exatamente estes controles (ver §4).

### 3.4 Regras refletidas na UI

- **A33**: campo "Credencial de emissão" aceita apenas o **nome** do segredo (regex na view-model/controller); nada de valor de certificado/senha em tela nem no banco.
- **A30**: nenhuma tela calcula tributo; os parâmetros apenas registram o que o cliente definiu.
- **L376**: três campos de CFOP por operação no formulário (`cfopVenda` / `cfopRemessa` / `cfopRetorno`) — nenhum "CFOP único".
- **P2**: enquanto houver campo obrigatório ausente, o status permanece `PENDENTE_DE_CONFIGURACAO` e o alerta lista explicitamente o que falta (UF, Município, Regime, Operação, Provedor, Ambiente, CFOP por operação, Credencial).

## 4. Homologação E2E (browser real, sessão demo gestor)

Ambiente: Web `https://localhost:52977` (http `52976`) · API `http://localhost:51976` · banco `plantaopro_test` via env `ConnectionStrings__Default` · v2321 aplicada · credencial `nfe-santacasa-demo` presente. Todos os textos abaixo são **verbatim** capturados no DOM (`#pp-toast-data` / alertas / cards).

1. **Configurar (não cadastrado)** — heading: *"Configuração Fiscal de Emissão / Parâmetros do cliente para emissão de NF-e. A decisão comercial ausente vira pendência P2 explícita; a credencial entra apenas como NOME de segredo — nunca o valor (A33)."* Alerta P2 verbatim: *"Ainda não cadastrado — pendência P2 registrada. Defina: UF de emissão Município Regime fiscal Operação fiscal Provedor de emissão Ambiente (homologação/produção) CFOP por operação Credencial de emissão (nome do segredo) Enquanto houver pendência, o status permanece PENDENTE_DE_CONFIGURACAO e o botão \"Emitir\" devolve o motivo real — nenhum sucesso fictício (L33/H19)."*
2. **Nova MANUAL** — destinatário "Hospital São Rafael LTDA" (CNPJ 12.345.678/0001-95), 1 item 2 × 149,90 → toast success: *"Pré-nota criada em rascunho com conferência de referências."* → badge **Rascunho**; matriz conforme §3.3 (apenas MarcarPronta + Cancelar; Emitir oculto).
3. **MarcarPronta** → toast: *"Pré-nota marcada como pronta para emissão."* → badge **"Pronta p/ emissão"**; Emitir + Reabrir aparecem.
4. **Emitir SEM parâmetros** → toast error (bloqueio real, L33/H19): *"Emissão bloqueada — Parâmetros fiscais ainda não cadastrados para este cliente."* + alerta inline amarelo no card de emissão: *"Emissão bloqueada: Parâmetros fiscais ainda não cadastrados para este cliente. O botão "Emitir" devolve este motivo real — nenhum sucesso fictício (L33/H19). [Resolver na Configuração Fiscal]"*. Nota segue PRONTA.
5. **SalvarConfiguracao** — SP / São Paulo / SIMPLES_NACIONAL / VENDA / CFOPs 5101·5911·4101 / SEFAZ_DIRETO / HOMOLOGACAO / `nfe-santacasa-demo` / responsável "Gestor Homologação R4" → toast: *"Parâmetros fiscais salvos. Status atual: CONFIGURADO."* + badge **"STATUS ATUAL: CONFIGURADO"**.
6. **Detalhes após configurar** → badge verde **"Parâmetros CONFIGURADO"** + alerta verde: *"Nenhum bloqueio real: parâmetros completos e credencial disponível no ambiente. A transmissão registra ENVIANDO; autorização/rejeição dependem do retorno externo (conector = P1)."* Card H19 continua integralmente "—" (chave de acesso / emitida em / rejeição / cancelada) — nada de evidência externa inventada.
7. **Emitir** → toast success: *"Pré-nota transmitida (ENVIANDO). A autorização/rejeição só é registrada com o retorno externo (conector = P1)."* → badge **Enviando**; botões "Retornar para Pronta" + Cancelar (Emitir oculto).
8. **Reabrir** → toast: *"Transmissão retornada: a pré-nota voltou a PRONTA_PARA_EMISSAO."*
9. **CancelarNota** (motivo preenchido no modal com foco automático) → toast: *"Pré-nota cancelada."* → badge **Cancelada**; card H19 registra *"Cancelada em 07/10/2026 19:46"* + o motivo; texto terminal: *"Situação terminal no MVP — alterações posteriores exigem o conector externo (P1)."*; nenhum botão de transição.
10. **Bônus A29 (conferência de origem)** — Nova com origem VENDA + GUID inexistente → toast error: *"Documento de origem 'VENDA' não encontrado para este cliente."* (não há nota fantasma; o POST volta para Nova).
11. **Lista Notas** — linha: `NPE-00000060 | MANUAL | Hospital São Rafael LTDA | R$ 299,80 | — | Cancelada | 07/10/2026 19:28 | Detalhes` (moeda pt-BR; link funcional).

## 5. Bugs encontrados e corrigidos nesta rodada

1. **Required implícito por Nullable enable** — `NovaNotaItemLinha.Descricao` era `string` (não-nulo): com `<Nullable>enable</Nullable>` o model binding trata string não-nula como required, e as linhas vazias de itens (seleção fixa de 8 linhas) quebravam o POST com *"The Descricao field is required."* Fix: `Descricao` virou `string?` (semântica declarada: "linhas sem descrição são ignoradas") + filtro no controller (`!string.IsNullOrWhiteSpace(i.Descricao)` + `i.Descricao!.Trim()`). Validado no E2E passo 2.
2. **Moeda em cultura invariante na tabela de itens** — célula mostrava `149.9`/`299.8` (ponto decimal) enquanto o card de resumo mostrava `R$ 299,80`. Fix: preços/totais em pt-BR `N2` e quantidades `0.###` pt-BR nas tabelas de `Detalhes` (alinhado à convenção "moeda exibição N2 pt-BR"). Validado: `2 | 149,90 | 299,80`.
3. **Build incremental Razor desatualizado** — a cadeia de alertas do card de emissão (if/else-if/else, L142–164 de `Detalhes.cshtml`) existia no source e no assembly (marcadores conferidos por byte-scan UTF-16 do `PlantaoPro.Web.dll`) mas **não renderizava** em nenhuma das três ramas — impossível num if/else completo, portanto o código compilado era de uma iteração anterior do arquivo (cache incremental do Razor ficou dessincronizado após uma build intermediária falha durante o F2). Resolução: rebuild **`--no-incremental`** → todas as ramas passaram a renderizar (bloqueio antes do Configurar, verde depois, secundário fora de PRONTA). **Regra de build adotada**: se um Razor parar de refletir o source atual, confirmar com rebuild não-incremental antes de depurar a lógica da view.
4. **Teste F1 não-hermético exposto pelo próprio F2** — `Parametros_isolamento_total_entre_tenants` contava a **tabela inteira** de `adm360_parametros_fiscais` (esperando 1 linha), mas `LimparAsync` só limpa os tenants do teste (`TenantA`/`TenantB`). Com qualquer parâmetro salvo por outro tenant — exatamente o que a nova UI do F2 faz em dev (linha do tenant demo registrada no E2E acima) — a contação virava 2 e o teste falhava de forma determinística. Fix (1 linha): contagem escopada `where tenant_id in (@a, @b)`; a asserção de isolamento (`ObterAsync(TenantB) is null`) ficou intacta.

## 6. Gate

- **Build Web**: `dotnet build PlantaoPro.Web.csproj -c Debug --no-incremental` → **0 erros**; 20 avisos, todos backlog conhecido (família CS86xx, CS0108 em `CotacoesRepository`, CS8321 em `NotasPreEmitidasRepository`, CS8603/CS8629 etc.) — **nenhum em arquivo do Fiscal F2**.
- **Suíte xUnit**: na primeira re-execução antes do commit, 1 falha determinística em teste F1 (item 5.4, acima) — corrigida; re-execução completa após o fix: **1106/1106**.
- **Arquivos deste commit**: `PlantaoPro.Web.csproj` (refs Application/Infrastructure) · `Services/Security/SaasRouteGuardFilter.cs` (mapa ADM360) · `Views/Shared/_SuprimentosNav.cshtml` (gate + grupo Fiscal) · `appsettings.Development.json` (credencial A33) · novos: `Controllers/Adm360FiscalWebController.cs` · `Models/Administrativo360FiscalViewModels.cs` · `Views/Administrativo360/Fiscal/{Configurar,Notas,Nova,Detalhes}.cshtml` · teste F1 hermeticizado (`Administrativo360R4F1FiscalPreEmissoesTests.cs`, 1 asserção) · este documento. **Sem push.**

## 7. Pendências e bloqueios (fora desta máquina)

- **P2 (registrado)**: decisão comercial operação/UF/provedor do cliente — enquanto não vier, status `PENDENTE_DE_CONFIGURACAO`; a tela deixa isso explícito em vez de fingir configuração.
- **Homologação fiscal externa BLOQUEADA até credenciais** reais (SEFAZ) disponíveis no ambiente.
- **P1**: conector externo real de NF-e (validar/assinar/transmitir/consultar/cancelar + XML/protocolo); até lá `ENVIANDO` é o teto do ciclo interno e os estados `AUTORIZADA`/`REJEITADA` só existem via retorno externo.
- Upgrade do banco principal `plantaopro` p/ v2321 (hoje só `plantaopro_test` tem as tabelas fiscais) e guia IIS de produção (credencial `Fiscal:Credenciais:{ref}` no environment/secrets do host).
