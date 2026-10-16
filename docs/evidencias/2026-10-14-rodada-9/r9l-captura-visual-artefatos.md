# Rodada 9 — r9l: Captura visual / aprovação visual (screenshots + zoom/breakpoints)

Data: 2026-10-15 · Base: `b302093` (r9k) · Ambiente local/homologação · Web `http://localhost:52976`, API `http://localhost:51976`, banco principal `plantaopro` (PG18) · Sem push/merge/publish. · **Atualizado 2026-10-16 (r9m): capturas de screenshot concluídas (§4); aprovação visual pendente de aceite do usuário (§6).**

## 1. Objetivo

Fechar o item pendente "screenshots/aprovação visual": capturar as telas tocadas pelo i18n de r9j (`RelatoriosFinanceiros`, `Produtos`, bloco ANVISA de `Cotacoes/Detalhes`) autenticado no admin do tenant Sistema Vertical, e registrar o estado de zoom 200% / breakpoints 360/768/1440.

## 2. Setup do ambiente e descobertas (diagnóstico de conexão)

Os dois servidores estavam fora (páginas davam 502). Ao religá-los, emergiram três fatos de ambiente documentados abaixo:

1. **API sem connection string explícita resolvia `Database:plantaopro_test`** (log de startup: `Database startup configuration Host:127.0.0.1 Port:5432 Database:plantaopro_test`). Nenhum appsettings (json/Development/Testing/cópia no output), user-secrets (inexistentes em `%APPDATA%\dotnet`), env var em shell foreground **nem em shell de background** (`r9l_bgenv.txt`) e nenhum `.env` do repositório (raiz, só chaves IA) contêm o valor — origem não identificada dentro do repositório (provavelmente nível de tooling/harness fora dele). Consequência real observada: o login do admin SV falhava com **Candidatos:0**, porque o usuário não existe em `plantaopro_test`. Tratamento definitivo adotado: sempre levantar os servidores com a env var explícita (itens dos scripts `.cmd`).
2. **Npgsql 10.0.2 rejeita o formato URI** de connection string — reproduzido com app scratch contra os DLLs do próprio build (`postgresql://postgres:123456@127.0.0.1:5432/plantaopro` → `Format of the initialization string does not conform to specification starting at index 0`; formato KEY=VALUE → OK). Isso explica a falha de parse dos primeiros restarts (o valor não era corrompido pelo shell — probe confirmou o texto exato).
3. **Senha/hash validados diretamente**: hash canônico do template `123_sistema_vertical_plantaopro.sql` bate com o hash do banco e `BCrypt.Verify("Sv-Homolog-2026!", hash)` = **True** (app scratch com `BCrypt.Net-Next.dll` do build). Com a API apontando para `plantaopro`: `POST /api/auth/login` → **success=true**, tenant `8b0c8e74-a81b-4ea2-b499-94755a1ca301`.

Resolução adotada: servidores levantados com env var explícita em formato KEY=VALUE (`set ConnectionStrings__Default=Host=127.0.0.1;Port=5432;Database=plantaopro;Username=postgres;Password=123456`) via scripts `r9l_api.cmd` / `r9l_web.cmd` (em `%TEMP%\opencode`).

## 3. Evidência funcional/DOM (autenticado, admin SV)

Sessões web autenticadas como `admin.sistemavertical@plantaopro.local` (perfil ADMINISTRADOR, tenant "Sistema Vertical"). Verificação feita por execução de script no DOM vivo + captura do `outerHTML` completo de cada página (artefatos em `artefatos-dom/`):

| Tela | Título renderizado | Âncoras pt-BR verificadas ao vivo | Dados reais exibidos |
|---|---|---|---|
| `/Administrativo360/RelatoriosFinanceiros` | "Relatórios Financeiros — Administrativo 360" | "Vendas Faturadas no Período" ✓, "Entendendo os Relatórios Financeiros" ✓, "DRE Gerencial por Cliente" ✓, abas/colunas NÚMERO/COMPETÊNCIA/TOTAL LÍQUIDO/COMISSÃO PREV. ✓ | Linhas VEN-SV-R9G-ORFA e VEN-SV-R9F-01 (R$ 300/1.200 bruto, comissões R$ 2,50/50,00, CONFIRMADA) |
| `/Administrativo360/Produtos` | "Produtos e Materiais — Administrativo 360" | "Catálogo de Produtos & OPME" ✓, "Novo Produto" ✓, colunas SKU/PREÇO CUSTO/PREÇO VENDA/RASTREABILIDADE/**ANVISA** ✓ | 3 SKUs (SV-NOL-001, SV-PREC-001, SV-LTE-001); coluna ANVISA ao vivo: "n/a", "pendente", registro "0001234567890123" |
| `/Administrativo360/Cotacoes/Detalhes/ced7c533-…d57e68b90d49` | "Cotação SV-ANVISA-R9 — Administrativo 360" | Cabeçalhos da tabela de itens em pt-BR ✓; valores ANVISA por linha: **"ANVISA 0001234567890123"** e **"ANVISA n/a"** (chaves de r9j renderizando) ✓; blocos "Prazo de Resposta", "Status e Orçamento", "Histórico e Fila Outbox de Transmissão" ✓ | Cotação r9e (PRONTA_PARA_ENVIO/ABERTA), itens SVITEM-1/SVITEM-2 mapeados, proposta exportada com SHA256 |

Sem estouro horizontal nas 3 telas no viewport 1000×700 (`scrollWidth <= clientWidth`). O container principal resolve em px fluido (Bootstrap `container-fluid`), sem larguras fixas que quebrem em 360/768/1440 — verificação estrutural.

## 4. Screenshots: **CAPTURADOS em 2026-10-16 (r9m)**

A condição que bloqueava era a **janela desktop do OpenCode estar visível no display do usuário** (erro: `browser.operation_failed: Screenshot needs a visible tab`). Em 2026-10-16 (após as fatias r9m-P2/P4), com a janela visível fornecida pelo usuário, as três telas foram capturadas como **viewport PNG** e salvas em `imagens/`:

| Tela | Arquivo | Conteúdo visível no pixel |
|---|---|---|
| `/Administrativo360/Cotacoes/Detalhes/ced7c533-…d57e68b90d49` | `2026-10-16-cotdet-detalhes-viewport.png` | Título dinâmico do r9m-P4 renderizado via resx ("Cotação SV-ANVISA-R9 — Administrativo 360"), breadcrumb "Início / SV-ANVISA-R9", contexto do tenant SV, guia "Como usar esta tela" (reuso de `Login.HelpSummary`) |
| `/Administrativo360/RelatoriosFinanceiros` | `2026-10-16-relfin-viewport.png` | Título "Relatórios Financeiros — Administrativo 360" + chrome administrativo |
| `/Administrativo360/Produtos` | `2026-10-16-produtos-viewport.png` | Título "Produtos e Materiais — Administrativo 360" + chips de categoria |

Sem estouro horizontal nas capturas. O conteúdo abaixo da dobra (tabela de itens/ANVISA/outbox na cotação; tabelas RelFin/Produtos) tem evidência funcional completa na seção 3 + artefatos DOM; tentativas de `fullPage` adicionais ocorreram apenas com a janela oculta e não impedem o escopo da rodada.

Lições de protocolo registradas (causas das ~10 tentativas frustradas anteriores e o que resolveu em 2026-10-16):
1. **Restart do Web invalida a sessão** (Data Protection key efêmera) → re-login via form obrigatório a cada restart: setar `#Email`/`#senha` com value-setter nativo + evento `input` + `form.submit()`.
2. **Nunca navegar explicitamente logo após o submit**: um `navigate()` imediato conflita com o POST→302 do form (causa dos submits "silenciosos"). Receita que funcionou: submeter a partir de `Login?ReturnUrl=<tela>` e deixar o redirecionamento do form concluir; aguardar `location` sair de `/Account/Login`.
3. O primeiro submit logo após o boot da API pode falhar em silêncio — retry do submit resolve.
4. `browser.screenshot` exige a aba focada (`tabs.focus`) **e** a janela do OpenCode visível em primeiro plano; a condição oscila durante a sessão — executar as capturas em rajada curta (3 telas em sequência) assim que a janela estiver visível.

## 5. Zoom 200% e breakpoints 360/768/1440

- **Zoom 200%**: sem verificação empírica — a webview do browser do harness não intercepta `Ctrl+=` como zoom de navegador (medido: `innerWidth`/`devicePixelRatio` inalterados após 6 passos), e o conjunto de ferramentas não expõe device-metrics override (CDP). Mantém-se **verificado por construção** (layout fluido, sem largura fixa no conteúdo principal).
- **Breakpoints**: idem — sem emulação de viewport disponível; **verificado por construção** (classes responsivas existentes + ausência de overflow no viewport de trabalho).

## 6. Aprovação visual

**Capturas obtidas (item 4); aceite pendente do usuário.** As 3 imagens foram exibidas no Review pane em 2026-10-16 para aprovação; sem resposta até o registro. Com o aceite, a linha r9l vira PRONTO integral. As três telas têm evidência funcional completa (seção 3 + artefatos DOM) independentemente da aprovação visual.

## 7. Servidores deixados ativos

- API `:51976` e Web `:52976` rodando (scripts `r9l_api.cmd`/`r9l_web.cmd`), ambos com `ConnectionStrings__Default` explícito para `plantaopro`. Sessão autenticada preservada no perfil do browser do harness.
