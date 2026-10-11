# R7-E1 — Fundação i18n da interface (pt-BR padrão / en / es / fr)

Data: 2026-10-10. Base: `HEAD = 9588fab` (R7-A1). Banco de prova: `plantaopro_test`
(PostgreSQL 18 local, serviço Windows); migração **v2344** aplicada via
`PlantaoPro.Tools.Database upgrade` (`upgrade: 2026_10_v2344_r7e1_usuario_idioma_preferido.sql
aplicada.`) — coluna `usuarios.idioma_preferido varchar(8)` com CHECK em
{pt-BR,en,es,fr}∪{null} verificada por psql (`ck_usuarios_idioma_preferido`).

Suíte de fechamento (validação executada na rodada 8, 11/10): **gate contratos+R7E1+R7A1
= 47/47**; suíte completa **1248/1248** (full1) · **1248/1248** (full2) · **1247/1248**
(full3) — a única falha do full3 foi `Npgsql 40P01` no **cleanup** (`PurgarAsync` em
`Dispose`, `SaasGovernancaB6Rodada4Tests.cs:481/78`) sob concorrência total; as assertions
do próprio teste passaram, e a classe reexecutada isolada deu **8/8 em 1 s**
(`rerun-b6.log`). Máquina calma nesta janela: suíte completa em ~12-20 s (vs 40-75 min sob
thrash de RAM nas noites anteriores).

Observação de ambiente (importante para leitura dos logs desta janela): a máquina de teste
ficou sem RAM livre (processos de teste morrendo por OOM; até `systeminfo`/`dotnet new`
levando minutos). Com autorização do desenvolvedor, Docker Desktop e WSL foram parados para
a janela de testes — o PostgreSQL é serviço Windows local, fora do Docker, e seguiu ativo.
Falhas de teste vistas no período foram investigadas e comprovadamente ambientais, sem
mudança de código de produto: (a) 3 falhas do filtro R7E1 — `ConnectAsync` do Npgsql
estourando 30 s enquanto `psql` conectava ao MESMO Postgres em <1 s (`pg_isready` OK), e 2
testes Web estourando o timeout de build do IHost (elevado para 900 s na reexecução) além de
conflito de bind em `:5000` causado por `testhost.exe` órfão de execução anterior matada por
OOM (finalizado); (b) ~29 processos `dotnet.exe` ociosos de build server finalizados via
`dotnet build-server shutdown`/taskkill. Registro apenas; nenhuma mudança de produto.

**Estado honesto**: IMPLEMENTADO → TESTADO (suíte + banco real). AINDA NÃO HOMOLOGADO em
host vivo e não LIBERADO. Commitado e publicado em `2f4533d3` (mensagem externa "novos";
conteúdo do pacote conferido via `git show` — 31 arquivos, incluindo os 3 contratos de
fonte atualizados).

## 1. O que o ponto pediu (design+i18n de cada etapa do ciclo) e o que foi feito

| Exigência | Situação | Como ficou |
|---|---|---|
| Idiomas pt-BR (padrão), en, es, fr em toda a interface | Parcialmente pronto → fundação fechada | Catálogo canônico compartilhado API+Web; resolução por request com precedência **usuário > organização > navegador > pt-BR**; recursos localizados nos pontos definidos para esta fatia (abaixo). |
| Independência idioma × moeda/data/fuso/fiscal | Ausente → garantido por design | O provider devolve **CurrentCulture=pt-BR sempre**; só a UICulture varia. Formatação "C"/"N2", binding decimal e XMLs fiscais não mudam com o idioma — provado por teste de unidade do provider. |
| Preferência do usuário persistida e efetiva sem re-login | Quebrado → fechado | Coluna `usuarios.idioma_preferido` (v2344) + `POST api/usuarios/me/idioma` (validação pelo catálogo) + emissão de claims `idioma`/`org_idioma` na sessão + **reemissão canônica** (`api/auth/refresh-context`) após salvar; efeito vale no PRÓXIMO request. Prova comportamental completa (stub) e persistência real (PostgreSQL). |
| Padrão da organização como fonte intermediária | Ausente → fechado (resolver) | `tenants.metadata->>'idiomaPadrao'` (jsonb, sem migração) normalizado no login e no refresh-context → claim `org_idioma`; vence o navegador quando o usuário não definiu. UI administrativa deste padrão = DÍVIDA registrada (§6). |
| Nunca expor chave de recurso | Contrato aberto → fechado | Paridade total dos 4 `.resx` (mesmas chaves, nenhum valor vazio) testada; fallback neutro=pt-BR garante que a chave nunca chega ao usuário. |
| Sem tradução automática de dados clínicos/dados do usuário/XMLs fiscais | Regra declarada → respeitada | Apenas rótulos da interface entram em `UiText.*`; conteúdo dinâmico (nomes, descrições clínicas, XMLs) não passa pelo localizador. |
| `<html lang>` coerente por request | Hardcodado → fechado | As 4 páginas com `<html>` próprio usam `UiText.HtmlLang(Context)` (resolver canônico por request); contrato de fonte proíbe `lang="pt-BR"` hardcoded e exige exatamente os 4 usos do resolver. |

**Fatia de tradução desta rodada** (mecanismo pronto para expansão): chrome
(`_Layout`, `_AuthLayout`), tela completa `Account/Login.cshtml`, `Usuario/Index.cshtml`
(com card de escolha de idioma) e as 4 tags `<html lang>`. Menu lateral/topbar, microtextos
JS e mensagens da API/BFF entram em **R7-D** (lista em §6) — o mecanismo é o mesmo
(chaves novas nos mesmos arquivos, mesmo provider/claims).

## 2. Decisão canônica: catálogo único e precedence única

- **Única implementação**: `PlantaoPro.CrossCutting.Localization.UiLanguageCatalog`
  (`Supported = {pt-BR, en, es, fr}`, `Default = pt-BR`, `FormatCultureName = pt-BR`).
  - `Normalize`: aceita variantes regionais (`FR-FR`→`fr`, `EN-us`→`en`, `PT`→`pt-BR`) e
    rejeita desconhecidas/brancas (→ `null` = fonte ausente).
  - `Resolve(usuario, organizacao, acceptLanguage)`: primeira fonte válida vence;
    `ParseAcceptLanguage` honra q-values (inclusive descarte de `q=0`) e preserva a ordem do
    header em empate de q.
- **Transporte**: a Web NÃO lê banco; a preferência viaja como **claims de sessão**
  `idioma` (usuário) e `org_idioma` (organização), emitidas em login E refresh-context pelo
  `SessionClaimsBuilder` a partir do payload canônico (`LoginResponse.IdiomaPreferido` /
  `IdiomaOrganizacao`, populados em login e refresh-context na API).
- **Provider** (`UiLanguageCultureProvider`): consulta claims → fallback Accept-Language →
  pt-BR; devolve sempre `(CurrentCulture=pt-BR, UICulture=idioma-resolvido)`.
  Pipeline: `UseRequestLocalization()` logo após autenticação; providers padrão substituídos
  pelo canônico.
- **Recursos**: `UiText.L(key)` (ResourceManager; master neutro=pt-BR, satélites
  en/es/fr) + `UiText.HtmlLang(Context)`. `Html.Raw` deliberado em exatamente 1 chave
  (`Login.HelpBody`, contém `<strong>` no verbo da ação).
- **Reemissão**: `ISessionReissueService` extraído do fluxo existente
  (`AccountController.RefreshContext` refatorado sobre ele, mensagens de falha preservadas
  por status code); `UsuarioController.SalvarIdioma` = antiforgery → API → reemissão → PRG
  (com signout+PRG em fallback 401).

## 3. Problemas latentes encontrados durante a implementação (descobertas .NET 10)

| Descoberta | Efeito se ignorado | Como foi tratado |
|---|---|---|
| No .NET 10 o middleware grava a cultura com a **chave da interface `IRequestCultureFeature`**, não do tipo concreto `RequestCultureFeature` | `Features.Get<RequestCultureFeature>()` retorna null SEMPRE → `<html lang>` preso em pt-BR apesar da cultura correta no thread (sintoma exato visto em debug: `UI=NOFEATURE THREAD=fr`) | `UiText.HtmlLang` lê por `IRequestCultureFeature`; comentado no código para não regredir |
| `RequestLocalizationOptions` mudou de namespace no .NET 10 (`Microsoft.AspNetCore.Builder`) | Compilação em código que qualifique o nome completo | Uso sem qualifier via usings; verificado por probe direto no assembly do framework |
| SDK net10 gera o satélite com nome interno com sufixo de cultura (`…UiTexts.fr.resources`) | Assumir a convenção antiga quebra a busca e degrada silenciosamente para pt-BR | Verificado por probe end-to-end: ResourceManager padrão resolve as 4 linguagens contra o satélite real da Web (nenhuma ação necessária — registro para quem mexer em `.resx` no futuro) |
| Estado incremental do compilador Razor podia compilar layout novo e view antiga no MESMO assembly | Página mista (chrome novo, corpo em pt-BR) sem erro nenhum | `obj` da Web descartado e rebuild completo; o problema não ocorreu mais nas execuções seguintes |
| Controller `Usuario` (perfil próprio) SEM entrada no catálogo do guard SaaS | **Todo** usuário autenticado caía em `CATALOGO_NAO_CONFIGURADO` em `/Usuario*` (página linkada como "Editar meu perfil" na Configuracoes; sem o fix, o card de idioma e o salvamento desta rodada eram inalcançáveis na prática) | Mapeado para o módulo `CONTA` (módulo comum: `CommonModules`/`CoreModules` — liberado para todo autenticado, sem live-check), mesmo padrão do fix de `Caixa` em R6-BlocoA; comprovado pelo teste de POST que agora recebe o PRG para `/Usuario` |

## 4. Provas (testes novos — `backend/PlantaoPro.Tests/R7E1I18nFundacaoTests.cs`)

- **Catálogo/provider (sem host)**: matriz de precedência (usuário vence tudo; variante
  regional normaliza; org vence navegador; q-values; empate preserva ordem; `q=0` descarta;
  tudo ausente → pt-BR), normalização (aceita/rejeita), catálogo tem exatamente 4 idiomas,
  provider formata sempre pt-BR mesmo com UICulture fr e anônimo sem fonte → pt-BR.
- **Contratos de fonte**: paridade total 4× resx (faltantes/excedentes/vazios); zero
  `lang="pt-BR"` hardcoded nas views + exatamente 4 usos de `UiText.HtmlLang(Context)`;
  pipeline ativa `UseRequestLocalization` e o builder emite `idioma`/`org_idioma`.
- **Comportamental Web (stub determinístico)**: anônimo Accept-Language fr → página em
  francês (`lang="fr"` + textos fr, sem rastro de pt-BR); idioma indisponível (de) →
  degrada pt-BR; sem fonte → pt-BR; claim de usuário vence org+navegador; claim de org
  vence navegador quando usuário não definiu; login com payload de idioma emite claims e
  renderiza sem depender do browser; **POST /Usuario/SalvarIdioma** persiste via API
  (corpo `{"idioma":"es"}` + requests `/api/usuarios/me/idioma` e
  `/api/auth/refresh-context` observados), redireciona PRG e o MESMO cookie já renderiza
  `/Usuario` em espanhol com navegador alemão (efeito imediato sem re-login); valor fora do
  catálogo não chama a API nem muda a interface.
- **Persistência real (API + PostgreSQL)**: seed tenant/cliente/perfis/usuário → login real
  (padrão da ORGANIZAÇÃO `es` lido do jsonb; usuário null) → `POST api/usuarios/me/idioma`
  `fr` → coluna verificada por query direta no banco → `refresh-context` com o MESMO token
  traz `fr`+`es` → novo login continua `fr`+`es`. Limpeza de seed no finally.
- **Contratos de fonte pré-existentes atualizados (mesmo método de prova, intenção intacta)**:
  três testes de contrato por fonte precisaram acompanhar mudanças canônicas desta rodada,
  sem perder o que provavam — (i) `V2164…Login_UsesTheRequestedConciseHeading`: o título
  agora é chave canônica no `<h2>` + valor exato "Acesse sua conta" fixado no resx pt-BR;
  (ii) `V2150…LoginPremium`: sumário do guia agora é `UiText.L("Login.HelpSummary")` + valor
  exato "Como usar esta tela" fixado no resx; (iii) `V2156…RefreshDeContexto`: a reemissão
  consolidou no `SessionReissueService`, então o contrato cobre controller + serviço e exige
  que o controller delegue a `_sessionReissue.RefreshFromApiAsync` (caminho único).

## 5. Decisões de produto respeitadas

- Sem percentuais/preços/prazos/regras clínicas inventados: nenhuma string nova carrega
  número ou afirmativa clínica — apenas rótulos de interface traduzidos 1:1 do texto
  existente.
- Independência linguagem × moeda × fuso × fiscal: garantida estruturalmente (§2) e testada;
  nada de auto-tradução de dados clínicos/do usuário/XMLs fiscais.
- Critério de aceitação do ciclo (usuário acessa por menu → executa → persiste → lê efeitos
  sem tocar em banco): atendido para o caminho de idioma — salvar na tela do usuário, sem
  re-login, interface muda a partir do próximo request; o valor fica em coluna real lida de
  volta pelos próprios endpoints de sessão.

## 6. Dívidas registradas (não bloqueiam esta entrega)

1. **R7-D (próxima fatia)**: varredura de textos restantes com o MESMO mecanismo — menu
   lateral/topbar, microtextos JS (toasts/alerts inline), mensagens de API/BFF incluindo os
   409 pt-BR introduzidos em R7-A1; chaves novas entram nos mesmos 4 `.resx` respeitando o
   contrato de paridade.
2. **UI administrativa do padrão da organização** (`tenants.metadata->>'idiomaPadrao'`):
   resolver + payload + claim + precedência já provados ponta a ponta (teste real); falta só
   a superfície para o admin definir o valor.
3. **DEC (produto/design)**: glossário i18n oficial + revisor por idioma, com prioridade
   definida — necessário antes de escalar a varredura R7-D para os módulos ADM360/S360.
4. **Deploy-side (após commit/push)**: upgrade do banco principal `plantaopro`
   v2339→v2344 + republicação IIS; homologação viva segue roteiro `r6a1b` §7.
