# Rodada 9 — r9j: i18n completude (RelatoriosFinanceiros + Produtos + bloco ANVISA do Detalhe da Cotação)

Data: 2026-10-15 · Base: `604437e` (r9i) · Ambiente local/homologação · Sem push/merge/publish.

## 1. Objetivo e escopo

Fechar o item PARCIAL "i18n" da fila: eliminar os literais estáticos pt-BR nos três arquivos restantes do escopo do módulo Administrativo 360:

| Arquivo | Escopo desta fatia | Resultado |
|---|---|---|
| `Views/Administrativo360/RelatoriosFinanceiros.cshtml` | 100% dos literais estáticos (cards, tabelas, badges, conciliação DRE, bloco de indicadores, guia final) | **PRONTO** — 108 chaves novas |
| `Views/Administrativo360/Produtos.cshtml` | 100% dos literais estáticos (título/header/guia de regras, filtros, painel de lotes, tabela, badges, confirmações, paginação, modal completo, JS de prévia de precificação) | **PRONTO** — 99 chaves novas |
| `Views/Administrativo360/Cotacoes/Detalhes.cshtml` | apenas o **bloco ANVISA** (badges por item L188-205, select de catálogo + ajuda L275-288, JS `atualizarAnvisaInfo` L355-371) | **PRONTO** — 12 chaves novas |

Total: **+219 chaves** (`Adm360.RelFin.*` 108, `Adm360.Produtos.*` 99, `Adm360.CotDet.*` 12) em cada um dos 4 `.resx` → contrato Adm360 **283 → 502 chaves × 4 idiomas** (mestre pt-BR + en/es/fr), mesmas contagens nos 4 arquivos.

## 2. Convenções mantidas (sem alteração de pipeline)

- Lookup simples `UiText.L(key)` com fallback pt-BR mestre; nenhum argumento de formatação; strings dinâmicas compostas por fragmentos no Razor (ex.: `@UiText.L("...DreConcPeriodoIntro") <strong>@conc.VendasNoPeriodo</strong> @UiText.L("...DreConcPeriodoVendas")`).
- Conteúdo longo com `<code>`/`<strong>`: valor guardado no resx com markup escapado (`&lt;code&gt;`) e renderizado via `@Html.Raw(UiText.L(...))` (corpo dos indicadores, regras do guia de produtos, aviso do lote).
- Strings injetadas em JS: `'@UiText.L("...")'` — o Razor HTML-encodeia o valor (`'`→`&#39;`), o que mantém o literal JS simples válido mesmo com apóstrofos (fr).
- Todos os `<data>` novos com `xml:space="preserve"` (fragmentos com espaço inicial/final, ex.: `Adm360.Produtos.AnvisaVigPrefix` = ` · Vigência regulatória: `, `Adm360.Produtos.ConfContinuar` = ` Deseja continuar?`).
- Mantidos literais propositais: marca `ANVISA ` (prefixo), tokens universais `n/a`, `R$`, `—`, `·`, exemplos de formato (`LT-2026-014`, `0.00`, EAN `...`), códigos de máquina (APROPRIADA/ESTORNADA/CONFIRMADA/NAO_APLICAVEL etc.), comentários HTML/Razor (não renderizados).
- Reuso de chaves existentes: `Clinical.ButtonCancel`, `Clinical.ButtonSave` (botões do modal), `Implantacao.CloseAria` (fechar modal), `Adm360.ButtonFiltrar`, `Adm360.ButtonLimpar`.

## 3. Decisões tomadas durante a fatia (com verificação prévia)

- `Adm360.RelFin.DreThReceitaLiquida` existe com valor `"(=) Receita Líquida"` (prefixo conferido no resx) → o cabeçalho de margem `"Receita Líquida"` (sem prefixo) ganhou chave própria `Adm360.RelFin.ThReceitaLiquida`.
- Literal residual em chrome já keyizado detectado e keyizado: `title="Consolidado = clientes + Não atribuída, ..."` → `Adm360.RelFin.DreConciliadoTitle`.
- Bloco de select do catálogo coberto integralmente (L275-288): `OptSelecioneProduto` e `LblIdentificadorManual` adicionais ao desenho original.
- Correção tipográfica mínima em texto novo (não regra clínica/comercial): "Margen %" → "Margem %" no corpo de `IndMargemFormula`.
- Provas de colisão: probe verificou que nenhuma das 219 chaves colide com nomes existentes; valores vazios rejeitados por assert.

## 4. Método de aplicação (reproduzível)

Gerador Python (temp dir): catálogo de (chave, pt, en, es, fr) + mapa de substituições exatas por view.
- Injeção nos 4 resx antes do único `</root>` (sem BOM nos 4 arquivos — preservado); escape XML `&<>`; idempotente em re-execução.
- Substituições com **assert exact-once** (falha barulhenta com contexto); substituições múltiplas declaradas (RA): `bate — delta zero` ×2, `placeholder="Opcional"` ×3, `Anterior` ×2, `Próxima` ×2 — contagens confirmadas pelo log do gerador.
- Validações pós-run: `xml.etree.ElementTree.fromstring` OK nos 4 resx; sem `<value>` vazio; contagem Adm360 = 502 em cada arquivo; auditoria de sufixo `>` nos pares old/new (detectou 4 fechamentos de tag perdidos em atributos `title=` — corrigidos e reverificados antes do build).

## 5. Evidências

### 5.1 Gerador (saída real)
```
NEW_KEYS 219
TOTAL_ADM360 {'UiTexts.resx': 502, 'UiTexts.en.resx': 502, 'UiTexts.es.resx': 502, 'UiTexts.fr.resx': 502}
XML_OK + SUBSTITUICOES_OK   (35 substituicoes RelFin + 75 Produtos + 11 CotDet, todas 1x ou RA com conteu declarado)
```

### 5.2 Amostra de traduções (verificada nos 4 arquivos)
| Chave | pt-BR | en | es | fr |
|---|---|---|---|---|
| `Adm360.RelFin.VendasCardTitle` | Vendas Faturadas no Período | Billed Sales in Period | Ventas Facturadas en el Período | Ventes facturées sur la période |
| `Adm360.Produtos.PageTitle` | Produtos e Materiais — Administrativo 360 | Products and Materials — Administrative 360 | Productos y Materiales — Administrativo 360 | Produits et Matériaux — Administratif 360 |
| `Adm360.Produtos.AnvisaVigPrefix` | ` · Vigência regulatória: ` | ` · Regulatory validity: ` | ` · Vigencia regulatoria: ` | ` · Validité réglementaire : ` |
| `Adm360.CotDet.SelectAjuda` | Selecione no catálogo ou informe o identificador… congelada no snapshot da versão enviada. | Select from the catalog or provide the product identifier… frozen in the snapshot of the submitted version. | Seleccione en el catálogo… | Sélectionnez dans le catalogue… |
| `Adm360.RelFin.IndVendasFiltros` | `<code>competencia &gt;= início AND competencia &lt;= fim</code> …` (Html.Raw) | `<code>competencia &gt;= start AND competencia &lt;= end</code> …` | idem (inicio/fin) | `<code>competencia &gt;= début AND competencia &lt;= fin</code> …` |

Verificação cruzada de cultura: chave `AnvisaVigPrefix` extraída de cada arquivo confirmou idioma correto + espaços preservados (pt/en/es/fr nas linhas 1-4 do dump `r9j_4langs`).

### 5.3 Varredura de literais residuais (scan de não-ASCII + grep ASCII em 3 views)
- **RelatoriosFinanceiros.cshtml**: restam apenas comentários HTML (não renderizados), em-dash `—` como token universal e o separador `—` na linha de totais (intencional). Nenhum literal de UI.
- **Produtos.cshtml**: restam comentários, tokens `—`/`·`, números/formatos (`N2` pt-BR mantido como política de formatação, não de texto), e os cabeçalhos das seções já keyizados. `Novo Produto`/`Editar Produto`/`Processando...`/`margem negativa`/`MARGEM NEGATIVA` ausentes (grep).
- **Cotacoes/Detalhes.cshtml**: bloco ANVISA 100% keyizado; todos os literais restantes estão **fora do escopo desta fatia** (dívida registrada abaixo).

### 5.4 Contrato (testes)
- `R7DF6Admin360I18nContratoTests.cs`: teste de contagem renomeado `...As283Chaves...` → `RecursosContemAs502ChavesAdm360EmTodosOsQuatroIdiomas` (`Assert.Equal(502,...)` nos 4 resx) + 24 chaves amostra r9j travadas por `Contains` + valores-âncora novos (pt-BR: `Vendas Faturadas no Período`, `&lt;strong&gt;SKU Único:...`, ` · Vigência regulatória: `; amostras en/es/fr).
- Novo fato view-level `ViewsAdm360_R9jRelFinProdutosEAnvisaDoCotDetPorRecursosSemLiterals`: Contains de 20 chaves RelFin + 15 Produtos (+ reusos `Implantacao.CloseAria`, `Clinical.ButtonCancel/Save`) + 12 CotDet; DoesNotContain de 8/8/8 literais característicos por view; estados de máquina preservados (`value="NAO_APLICAVEL"`, `.ToString("N2", ... pt-BR)`, `pp-badge--*`, hidden `aba`).

### 5.5 Checks executadas
| Check | Resultado |
|---|---|
| `dotnet build PlantaoPro.sln -c Release` | **PASSOU** — 0 erros (80 warnings pré-existentes, nenhum novo) |
| Suíte completa (`--no-build`) | 1397 total (baseline 1396 + 1 novo) → **1396 aprovados / 1 falha transitória** (abaixo) |
| Retry isolado `SaasComercialB4MatrizTests` | **PASSOU 12/12** (~1 s) — deadlock 40P01 em `PurgarAsync()` (Dispose) é flake de concorrentes no banco, irrelevante a esta fatia (sem mudança em C#/DB) |
| Parse XML dos 4 resx | PASSOU (ET.fromstring ×4) |
| Contagem Adm360 ×4 arquivos | PASSOU (502 em cada) |

Resultado efetivo da suíte: **1397/1397**.

## 6. Migrations

Nenhuma — mudança somente em recursos localizados + 3 views Razor + 1 teste.

## 7. Dívida registrada (fora do escopo r9j, mantida)

Literais pt-BR remanescentes em `Cotacoes/Detalhes.cshtml` **fora do bloco ANVISA** (escopo registrado desde o plano da rodada): título/breadcrumb dinâmicos (L3-4), header e cancelamento (L16-53), guia do documento (L61-65, L83), cabeçalhos de tabela de itens (L157-162, L174), opção "Não Atendido" (L237/L270), labels de conversão/preço/justificativa (L292-313), anexos (L394-408), fila outbox + mapa de próximas ações (L425-475), estorno (L546-555), painel IA (L574-579). Nenhuma destas linhas foi alterada por r9j além do bloco ANVISA.

Dívidas herdadas de R8/R9 permanecem registradas (B14 flake → r9k; screenshots/aprovação visual → r9l; B16/B17/B18; drill-down §27-L208; writer `adm360_ocorrencias`).
