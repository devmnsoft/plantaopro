# R72 — Anexos de arquivos (PDF/Word/imagem), menu do Administrativo 360 fixo, formulários e veredito SEFAZ

- Data: 2026-10-20 · Plano: `docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md`
- Commits: `web (rodada 72 r72a)` · `db api web (rodada 72 r72b)` · `tests docs (rodada 72 r72c)`
- Base do início da rodada: `main == origin/main == 784b5e2` · suíte 1671 aprovados · build 81 avisos / 0 erros.

## 1. Pedidos atendidos nesta rodada

| # | Pedido | Resultado | Onde |
|---|--------|-----------|------|
| 1 | Menu do Administrativo 360 "fica embaixo das areas quando ele desce" | Corrigido (sticky + z-index canônico) | §2.1 |
| 2 | Design de formulários "esta horrivel ainda" | Uplift canônico global no design system + varredura de markup planejada | §2.2 |
| 3 | Upload de arquivo no cadastro de funcionários | Pronto (bloco de anexos FUNCIONARIO) | §3 |
| 4 | Upload de arquivo no cadastro de empresas (parceiros) | Pronto (bloco de anexos EMPRESA) | §3 |
| 5 | Anexar nota fiscal como PDF/Word/imagem | Pronto (bloco de anexos NOTA_FISCAL nos detalhes da pré-nota) | §3 |
| 6 | Itens "Sistema Vertical" | Confirmação do que já existia × o que esta rodada criou | §4 |
| 7 | NF para emissão: já tem integração com certificado digital/SEFAZ? | **Não** — pré-emissão interna + importação/conferência de XML prontas; emissão DF-e/SEFAZ agendada na R85 | §5 |

## 2. r72a — interface (somente web)

### 2.1 Menu do módulo fixo ao rolar

- Causa raiz: `.adm360-nav-wrapper` (renderizado no topo de todas as páginas do módulo via `_SuprimentosNav.cshtml`) não tinha nenhuma regra CSS própria — sem posicionamento nem contexto de empilhamento. Ao rolar, os cards das areas seguintes (ordem DOM posterior) cobriam o menu e os dropdowns abertos desapareciam atrás deles.
- Correção em `wwwroot/css/pages/administrativo360-tema-claro.css` (arquivo que já vale apenas sob `body.pp-a360`): `position: sticky` sob o topbar (`top: calc(var(--pp-topbar-height, 5.25rem) + .5rem)`), `z-index: var(--pp-z-sticky)` (1010 — abaixo do topbar 1020, acima das areas padrão), `isolation: isolate` (contexto próprio de empilhamento para as areas nunca voltarem a cobrir o menu) e `.dropdown-menu { z-index: var(--pp-z-dropdown) }` (1030). Todos os valores são tokens canônicos de `tokens.css`; o fundo opaco já vem do card interno remapeado pelo tema claro.

### 2.2 Polimento canônico de formulários

`wwwroot/css/design-system/forms.css` entra via `@import` na entrada global do design system (`plantaopro.css`), então vale em todo o sistema sem tocar nos ~177 arquivos com formulários. Regras novas da rodada:

1. Estrela automática no label de campo obrigatório via `:has([required])` — escopada aos marcadores premium (`.pp-form-field` / `.pp-form-label`) para **não** duplicar as estrelas manuais ("Nome *") usadas pelos módulos legados;
2. Input de arquivo (upload) com altura mínima, borda tracejada e hover consistentes — antes ficava como controle nativo solto, destoando do resto do formulário (relevante exatamente pelos uploads novos desta rodada);
3. Label vira vermelha quando o grupo tem classe de erro de validação ou `aria-invalid="true"` (cor + texto: o canal de estado nunca é só cor);
4. Foco visível padronizado em checkbox/radio.

Próxima etapa (rodadas seguintes): varredura de arquivo a arquivo do markup (scanner da rodada mapeou 177 arquivos com formulários; ~539 campos sem label explícito e ~729 sem erro inline — referência `r72_gap_scan.txt`).

## 3. r72b — Anexos de arquivos (banco + API + web + views)

Feature completa: antes da R72 o único upload do sistema era a Central XML (arquivos `.xml` apenas); funcionários, empresas e notas fiscais não recebiam documentos.

- **Banco** — `database/migrations/2026_10_v2362_r72_anexos_arquivos.sql` (idempotente): tabela `plantaopro.anexos_arquivos` com `tenant_id NOT NULL`, `categoria CHECK (FUNCIONARIO/EMPRESA/NOTA_FISCAL/OUTRO)`, `entidade_id uuid null`, `nome_original`, `extensao`, `tipo_mime`, `conteudo text` (base64 — convenção da casa, mesmo padrão de `v115_recurso_documentos`), `sha256 char(64)`, `tamanho int`, `observacao`, `created_at`/`created_by` e `reg_status` (exclusão macia `'I'`). Seção 211 appendada nos dois scripts completos com `SOURCE-SHA256`; manifesto e checksums atualizados.
- **API** — `Adm360AnexosController` (`api/administrativo360/anexos`): listar por categoria/entidade, criar, baixar (`{id}/download`) e remover (`{id}/remover`), todos com policy `Adm360.Ver`. Validações: whitelist `{pdf, doc, docx, jpg, jpeg, png, webp, gif, xml}`, máx 8 MiB, conteúdo em base64 com verificação de integridade por sha256 gravado. DI registrada no `Program.cs` no padrão vizinho (`GetConn(sp)`).
- **Proxy web** — `Adm360AnexosWebController` (`Administrativo360/Anexos`: Lista/Upload/Remover/Baixar): o web não faz multipart contra a API — `IFormFile` vira base64 e segue por JSON (mesmo padrão da Central XML). Download devolve bytes com `Content-Disposition` reconstituído do header da API.
- **UI** — partial `_AnexosBloco.cshtml` + JS global `window.PPAnexos` (fetch para lista/envio/remoção; download via `<a href>`; CSRF pelo hidden antiforgery incluído no FormData):
  - `Colaboradores.cshtml`: botão clip por linha abre modal `modalAnexosColab` (categoria FUNCIONARIO);
  - `Parceiros.cshtml`: botão clip por linha abre modal `modalAnexosParceiro` (categoria EMPRESA);
  - `Fiscal/Detalhes.cshtml`: bloco inline dentro de `@if (Model.Nota is not null)` (categoria NOTA_FISCAL) — parcial referenciado por virtual path absoluto, pois não havia precedente de relative path entre pastas no repo.
  - Remoção com confirmação em duas etapas no próprio botão (troca de texto/estado legível) — evita diálogo nativo, proibido pelo gate de binding R16.
- **Decisões registradas**: granularidade de permissão mais fina que `Adm360.Ver` fica para rodada futura; sem ETag no download (conteúdo sempre lido do banco, simples de auditar); Onboarding/NovoCliente fora do escopo (a entidade só existe após a criação); textos das views adm360 seguem o estilo do módulo (strings locais hardcoded, sem resx), como os vizinhos.

## 4. O que já existia (itens "Sistema Vertical")

- Gestão Organizacional: já OK.
- Produtos com lote, validade, anvisa, preço de venda e margem: PRONTOS (migrations v2352/v2353 + `Produtos.cshtml`).
- Cotações com Anvisa: PRONTO (`Cotacoes/Detalhes.cshtml`, bloco Anvisa).
- Relatórios Financeiros/DRE: PRONTOS (`RelatoriosFinanceiros.cshtml` — abas vendas/comissões/margem/vencimentos/dre com filtro por cliente).

## 5. Veredito SEFAZ / emissão fiscal

- **Não existe emissão com certificado digital hoje.** O conector de transmissão é contrato sem implementação (`IFiscalTransmissor`, `FiscalPreEmissoesContracts.cs` L139-178).
- O que está pronto: pré-emissão interna (rascunho → pronta para emissão), importação/conferência de XMLs recebidos (Central XML) e telas de DF-e bloqueadas pela ausência de certificado A1.
- Emissão DF-e nacional/SEFAZ com certificado digital está agendada na **R85** do plano de 50 sprints.

## 6. Gates desta rodada

| Gate | Resultado | Referência |
|------|-----------|------------|
| Build A/B Release `--no-incremental` | BASE=81 / NOVA=81 avisos, 0 erros, **DIFF_AVISOS=NENHUMA** | `r72_build_base.txt`, `r72_build_nova.txt` |
| Suíte integral | **1681/1681 aprovados** (baseline 1671 + 10 novos), apps parados nas portas 51976/51977/52976 antes | `r72_tests_suites_completa.txt` |
| Docscan (`r62_docscan.ps1`) | ARQ_VERIFICADOS=23, BAD=4 — **zero ocorrências introduzidas pela R72** (verificado por diff das linhas adicionadas + varredura integral dos arquivos novos: 0). Os 4 flags são conteúdo legado prévio em arquivos tocados: seções antigas dos 2 scripts completos de banco, texto de desligar em `Colaboradores.cshtml:380` e comentário de vendor em `administrativo360-tema-claro.css:123` — mesmo critério de aceito da R71c (BAD=2 nos mesmos dois scripts) | saída no fim deste diretório |
| Higiene | `RepositoryHygieneContractTests` (8 fatos) verde — inclusive reexecutada após a criação deste documento (lição R8B25: docs novos também entram no scan) | suíte integral |

## 7. Arquivos tocados

- **r72a**: `forms.css` · `administrativo360-tema-claro.css`
- **r72b**: `database/migrations/2026_10_v2362_r72_anexos_arquivos.sql` (novo) · `migration-manifest.json` · `source-checksums.json` · `scrpt_completo.sql` · `scrpt_completo.sha256` · `pgadmin/instalar_no_banco_atual.sql` · `AnexosArquivosContracts.cs` (novo) · `AnexosArquivosRepository.cs` (novo) · `Adm360AnexosController.cs` (novo) · `Api/Program.cs` (DI) · `Adm360AnexosWebController.cs` (novo) · `AnexosBlocoViewModel.cs` (novo) · `_AnexosBloco.cshtml` (novo) · `Colaboradores.cshtml` · `Parceiros.cshtml` · `Fiscal/Detalhes.cshtml`
- **r72c**: `R72AnexosArquivosContratoTests.cs` (novo, 10 fatos) · `R16WebParserErrorContratoTests.cs` (whitelist JS_DRIVEN "Remover") · `docs/evidencias/2026-10-20-rodada-72/*`

## 8. Lições para rodadas seguintes

1. Dapper assíncrono na Infrastructure usa `new CommandDefinition(sql, param, cancellationToken: ct)` — chamadas diretas com parâmetro `cancellationToken:` nomeado não compilam na versão do repositório.
2. Razor: não usar aspas duplas aninhadas dentro de atributo de tag helper (`<partial model="@(new X { P = \"y\" })">` quebra no source generator desta base) — pré-montar a VM num bloco `@{ }` no topo da view.
3. O projeto Web compila C# 10: membros `required` indisponíveis — usar `init` com valor padrão.
4. `SendApiAsync<object, JsonElement>` é ambíguo (anotação `TResponse?` com struct) — preferir um DTO record de referência para deserializar a resposta da criação.
5. `git stash pop` com `autocrlf=true` renormaliza EOL da árvore de trabalho (LF→CRLF) e invalida SHA256 calculados antes do ciclo — recalcular hashes da migração/scripts e atualizar os 4 registros (source-checksums, manifesto, comentários SOURCE-SHA256 dos scripts completos, `scrpt_completo.sha256`) no fim da rodada.
6. `appsettings.json` carrega credencial local (banco de teste) na árvore de trabalho — reverte-la antes do commit (contrato: sem segredos em appsettings versionado).
7. Gate R16: ação `[HttpPost]` com parâmetro escalar não-nulável e sem `<form>` estático precisa ser classificada na whitelist `AcoesJsDrivenSemFormEstatico` (JS_DRIVEN) mantendo um literal de URL completo em quotes na view — padrão aplicado ao "Remover" dos anexos (mesma mecânica do "AnaliseIa", R21).
