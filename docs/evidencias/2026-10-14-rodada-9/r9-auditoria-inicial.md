# Auditoria inicial — Rodada 9 (Sistema Vertical, produtos/lotes/ANVISA, cotações, financeiro e DRE)

Data: 2026-10-14 · Ambiente: local/homologação · Legenda de status: **PRONTO** · **PARCIAL** · **QUEBRADO** · **AUSENTE(após busca)** · **NÃO VERIFICADO** — sempre com evidência.

## 0. Estado do repositório
- Branch `main` = `4ab9770`, `origin/main` em sincronia (AHEAD 0); árvore limpa.
- Baseline de suíte: **1394** testes verdes ×3 (pipeline v2: `final-suite2.ps1` = gate phaseA + full×3 `--no-build`).
- Banco principal `plantaopro` disponível (Postgres 18, `postgresql://postgres:123456@127.0.0.1:5432/plantaopro`, schema `plantaopro`), espelho `plantaopro_test`.

## 1. Método
Leitura somente-leitura dos componentes nomeados + rastreio de consumidores (API/serviços/modelos Web/views/migrations/testes) + consultas diretas ao banco para idempotência e catálogos reais (permissoes/modulos/plano). Nenhuma alteração nesta fase.

## 2. Classificação por domínio

| # | Domínio | Status | Evidência-chave |
|---|---------|--------|-----------------|
| 3.1 | Produto canônico + lotes + validade | **PRONTO** (fechado em r9b) | estrutura multi-lote + UI de lotes vinculados na tela do produto com regras de estoque/origem/validade; evidência `r5-produtos-lotes.md` |
| 3.2 | Precificação + custo histórico | **PRONTO** (fechado em r9d) | `preco_venda` padrão + margem/markup recalculados no servidor c/ estados ausente/não calculável/negativo; evidência `r7-precificacao.md` |
| 3.3 | ANVISA (cadastro/rastreio) | **PRONTO** (fechado em r9c) | cadastro regulatório em texto (zeros à esquerda), tri-estável (informado/não aplicável/pendente), origem manual, vigência regulatória ≠ validade de lote; evidência `r6-produtos-anvisa.md` |
| 3.4 | Cotações/propostas (itens) | **PRONTO** (fechado em r9e) | item liga produto+preço(snapshot) + ANVISA por item (seleção/linha/detalhe/comparativo) preservada no snapshot da versão enviada; evidência `r8-cotacoes-anvisa.md` |
| 3.5 | Parceiros com função de cliente | **PRONTO** | `adm360_parceiros.eh_cliente/eh_pagador/eh_hospital` |
| 3.6 | Vendas/títulos/parcelas/baixas | **PRONTO** | `adm360_vendas` + títulos por parcela (`data_vencimento`,`saldo_aberto`) + `adm360_recebimentos` |
| 3.7 | Relatórios (Hospital→Cliente) | **PRONTO** (corrigido em r9f) | contrato Hospital→Cliente corrigido (filtro/colunas/fallback "Cliente não identificado"/cross-tenant 400) + vencimentos por parcela sem multiplicar totais; evidência `r9-financeiro-cliente.md` |
| 3.8 | DRE gerencial | **PRONTO** (construído em r9g) | DRE por cliente em competência, consolidado conciliado = clientes + não atribuídas, origem e limitações sinalizadas; evidência `r9-dre-gerencial.md` |
| 3.9 | Autenticação/seed/permissões | **PRONTO** | template idempotente 122_* + BCrypt cost 11 + mapa política→permissão |
| 3.10 | Migrations/manifesto | **PRONTO** | manifesto de upgrade completo até v2353; install-manifest v2.21.9 cobre v2312-v2353 + gap/convergência; instalação limpa validada em banco scratch (evidência `r9i-instalacao-consolidada.md`) |
| 3.11 | Estado do banco | **PRONTO** | banco vivo; sem tenant "Vertical"/"Sistema" pré-existente |

## 3. Evidências por domínio

### 3.1 Produto + lotes + validade — **PRONTO** (inicialmente PARCIAL; fechado em r9b)
- Tabela produto: `adm360_produtos(id, tenant_id, sku, nome, unidade, codigo_barras, controla_lote, exige_inspecao, preco_custo, ativo)` — `CadastrosRepository.cs:151-162` (SELECT) e `:196-197` (INSERT). Registro `Produto360` em `Contracts.cs:664`.
- **Estrutura multi-lote já existe** (não colapsar num único campo): tabela `adm360_lotes(id, tenant_id, produto_id, codigo, validade, custo_unitario)` criada por *upsert* `ON CONFLICT (tenant_id,produto_id,codigo)` em `ComprasRepository.cs:368-372`; registro `Lote360(Id,ProdutoId,Codigo,Validade,Fabricacao)` em `Contracts.cs:666`; consulta `ListarLotesAsync(tenantId, produtoId)` em `Contracts.cs:696` e `CadastrosRepository.cs:402/444`.
- Saldos por lote+local+condição: `adm360_saldos` (`EstoqueRepository.cs:32-33`,`:86-89`); registro `SaldoEstoque` por `LoteId` em `Contracts.cs:16`. Movimentações com origem: `adm360_movimentos(…origem_tipo,origem_id,…)` (`EstoqueRepository.cs:113-116`).
- **Gaps (§5):** sem ANVISA no produto; sem `preco_venda`/margem/markup; cadastro de lote hoje ocorre pelo fluxo de compras/recebimento — a tela do produto ainda não lista/registra lotes vinculados de forma explícita (view `Views/Administrativo360/Produtos.cshtml` existe, sem seção de lotes confirmada). Regras a garantir: lote não cria estoque; entrada exige movimentação+origem; alteração de lote/validade com movimentações é procedimento controlado.
- **Fechamento (r9b, `8b83e10`):** tela de produtos com lotes vinculados (registro sem estoque automático); gaps de ANVISA e precificação atendidos nos slices próprios (r9c/r9d). Evidência: `r5-produtos-lotes.md`.

### 3.2 Precificação + custo histórico — **PRONTO** (inicialmente PARCIAL; fechado em r9d)
- Custo de referência: `preco_custo` (produto) `CadastrosRepository.cs:154/196/258`. Custo histórico por lote: `custo_unitario` (`ComprasRepository.cs:368`).
- `RelatorioMargemItem`/`RelatorioMargemAsync` (`Adm360FinanceiroRelatoriosRepository.cs:84-128`) é **relatório de margem de contribuição da VENDA** (receita−custo−comissão), **não** o cálculo de precificação do produto.
- **Gap (§7):** ausente `preco_venda` padrão e os campos derivados (lucro bruto unitário, margem sobre venda %, markup %). Precisam ser calculados **no servidor** com as fórmulas do §7, “Não calculável” quando denominador 0, margem negativa evidente, custo ausente ≠ zero confirmado, sem reescrever cotações/vendas passadas, decimal + política de arredondamento.
- **Fechamento (r9d, `27e1d8d`):** `preco_venda` padrão + margem/markup recalculados no servidor com todos os estados do §7. Evidência: `r7-precificacao.md`.

### 3.3 ANVISA — **PRONTO** (inicialmente AUSENTE; construído em r9c)
- `grep -i anvisa` em `backend/**/*.cs` = 0; em `database/**/*.sql` = 0. Ocorrências incidentes apenas: `Views/Administrativo360/Cotacoes/Detalhes.cshtml:285` (placeholder “autorizado pela ANVISA”) e `Views/Administrativo360/Recebimentos.cshtml:167` (rótulo “Data de Validade (ANVISA)”).
- **A construir (§6):** registro/notificação como **texto** (preserva zeros à esquerda), tipo de identificação, aplicabilidade (informado/não aplicável/pendente), fabricante/apresentação quando necessário, origem+data da conferência, flag “informação manual”, não simular consulta à ANVISA, não confundir validade do lote com vigência regulatória, justificativa para dispensados.
- **Fechamento (r9c, `d0fcbdc`):** cadastro regulatório ANVISA em produtos com todos os itens do §6 (texto c/ zeros à esquerda, tri-estável, manual, vigência regulatória). Evidência: `r6-produtos-anvisa.md`.

### 3.4 Cotações/propostas (itens) — **PRONTO** (inicialmente PARCIAL; fechado em r9e)
- Item: `CotacaoItemDetalheDto(Id, NumeroItem, CodigoExterno, DescricaoExterna, FabricanteExterno, ModeloExterno, QuantidadeSolicitada, UnidadeSolicitada, ProdutoId?, ProdutoNome?, UnidadeInterna, FatorConversao, QuantidadeConvertida, PrecoUnitarioOfertado, Desconto, PrecoTotalOfertado, MaterialOfertado?, JustificativaSubstituicao?, StatusRelacionamento, MotivoNaoAtendimento?)` — `CotacoesXmlContracts.cs:109-130`. Detalhes com `RevisaoExterna`, `StatusInterno/Externo`, `OrcamentoId` (`:141-168`). Comandos `RelacionarItemCotacaoCommand` (`:206-216`) e `CapturarCotacaoItemCommand` (`:188-196`).
- Já há vínculo de produto + **snapshot de preço ofertado**.
- **Gaps (§8):** sem campo de ANVISA no item; sem snapshot de lote/validade no item; sem distinção explícita “cadastro × fornecedor” da informação regulatória; precisa preservar snapshot na versão enviada (mudança posterior no produto não altera proposta enviada).
- **Fechamento (r9e, `684ab79`):** ANVISA em itens de cotação (seleção/linha/detalhe/comparativo) com snapshot congelado por versão enviada. Evidência: `r8-cotacoes-anvisa.md`.

### 3.5 Parceiros → cliente comercial — PRONTO
- `adm360_parceiros` com flags `eh_hospital/eh_pagador/eh_cliente` (`CadastrosRepository.cs:19-29` SELECT, `:113-115` UPDATE); `SalvarParceiroAsync(..., ehCliente)` (`Contracts.cs:686`); filtro `ehCliente` em `ListarParceirosAsync` (`CadastrosRepository.cs:12`). Modelo canônico de cliente comercial pronto para o §9.

### 3.6 Vendas/títulos/parcelas/baixas — PRONTO
- Venda: `adm360_vendas(numero, competencia, cliente_id, pagador_id, total_liquido, total_custo, comissao_prevista, valorizacao_id, situacao)` (`Adm360FinanceiroRelatoriosRepository.cs:14-21, 88-105`). Cliente e pagador já são campos distintos.
- Títulos por **parcela**: `documentO, competencia, data_emissao, data_vencimento, parcela, total_parcelas, saldo_aberto, situacao` (`ComprasRepository.cs:431,436`; `ContasReceberRepository.cs:24-57,74-163`; `ContasPagarRepository.cs:25-186`). Baixas/recebimentos: `adm360_recebimentos`.
- Cálculo de vencido já existe: `data_vencimento < hoje && saldo_aberto > 0` (`ContasReceberRepository.cs:57`; `CaixaRepository.cs:307`).

### 3.7 Relatórios — Hospital→Cliente — **PRONTO** (inicialmente QUEBRADO; corrigido em r9f)
- `RelatorioVendasAsync`: `Adm360FinanceiroRelatoriosRepository.cs:17` → `COALESCE(c.nome, hosp.nome,'Hospital') AS Hospital`, juntando **ambos** `adm360_parceiros c ON c.id=v.cliente_id` e `hospitais hosp ON hosp.id=v.cliente_id`; parâmetro `hospitalId` filtra `v.cliente_id=@hospitalId` (assinatura `:11`).
- `RelatorioMargemAsync`: mesmo alias em `:91` (join `hosp` em `:100`), mapeia `r.hospital` em `:120`.
- Exportação CSV mantém cabeçalho “Hospital”: `Administrativo360Controller.RelatoriosFinanceiros.cs:115`.
- **A corrigir (§9):** renomear dimensão para Cliente (filtro por cliente comercial, colunas/agrupamento/totais, busca por nome, impressão/exportação com mesmos filtros), manter **Pagador** separado, fallback “Cliente não identificado”, compatibilidade para registros históricos vinculados a hospital (sem remover tabelas/FK clínicas), rejeitar cliente de outro tenant.
- **Fechamento (r9f, `9c347ff`):** dimensão corrigida para Cliente com todos os itens do §9 + vencimentos por parcela (sem multiplicar totais). Evidência: `r9-financeiro-cliente.md`.

### 3.8 DRE gerencial — **PRONTO** (inicialmente AUSENTE; construído em r9g)
- `grep -i DRE` em `backend/**/*.cs` e `database/**/*.sql` = 0. Não há relatório/endpoint de DRE.
- **A construir (§11):** DRE por cliente em competência (receita, deduções identificadas, receita líquida, custo dos produtos/serviços realizado, resultado bruto, despesas atribuídas + “Não atribuída”, resultado gerencial do período); distinguível de fluxo de caixa; consolidado reconciliado = clientes + não atribuídas; acesso aos registros de origem; fontes incompletas sinalizadas como limitação.
- **Fechamento (r9g, `6729b69`):** DRE gerencial por CLIENTE em competência + conciliação, origem e limitações, implementado com todos os itens do §11. Evidência: `r9-dre-gerencial.md`.

### 3.9 Autenticação/seed/permissões — PRONTO
- Hash: `PasswordHashService.Hash = BCrypt.Net.BCrypt.HashPassword(pw, 11)` → `$2a$11$…`; verificação `BCrypt.Verify` (`backend/PlantaoPro.Api/Security/PasswordHashService.cs:10-29`). Pacote `BCrypt.Net-Next 4.0.3`.
- Template idempotente: `database/seeds/development/122_usuarios_homologacao_plantaopro.sql` cria `planos` (plano `HOMOLOGACAO_360`), `tenants`, `clientes`, `perfis`, `usuarios` (upsert por e-mail normalizado **preservando senha existente**), `usuarios_perfis`, `tenant_modulos`, `perfil_permissoes` (catálogo operacional, nunca SaaS global) + validação fail-closed.
- Mapa política→permissão: `Program.cs:136` (`Adm360.Ver`→`ADM360:VER`, `Adm360.Financeiro`→`ADM360:FINANCEIRO`, `Adm360.Vendas`→`ADM360:VENDAS`, `Adm360.CotacaoConsultar`→`ADM360:COTACAO_CONSULTAR`, `Adm360.MapearProdutos`→`ADM360:MAPEAR_PRODUTOS`, `Adm360.Exportar`→`ADM360:EXPORTAR`, …).
- Gating de menu: `_AppSidebar.cshtml:72` `Can("ADM360")`; sub-nav `_SuprimentosNav.cshtml:18-39` `PermissionService.HasPermission("ADM360", …)`.
- DB: plano `HOMOLOGACAO_360` (`8b0c8e74-a81b-4ea2-b499-94755a1ca003`, ATIVO) existe; catálogo `modulos_sistema` contém `ADM360` e ações; **nenhum** tenant `VERTICAL`/`SISTEMA` pré-existente.

### 3.10 Migrations/manifesto — PRONTO (fechado em r9i, 2026-10-15)
- Convenção real: `database/migrations/2026_10_vNNNN_<desc>.sql` encadeados em `migration-manifest.json` (checksum SHA-256, `dependsOn`).
- Drift original: manifesto lê até **v2345**; arquivos existem até **v2351** (migrações R8 não anexadas). `install-manifest.json` (v2.21.8) encerrava em ordem 80 (≈v2311).
- **Fechamento (r9i):** upgrade manifest verificado completo até v2353 (checksums conferem); install-manifest elevado a **v2.21.9** com 42 migrações v2312-v2353 + 2 arquivos de canonização de gap/convergência (`database/schema/450_v2219_*`, `460_v2219_*`); instalação limpa validada de ponta a ponta em banco scratch (128 fontes, `IDENTITY_SCHEMA_READY`); `scrpt_completo.sql` regenerado e validado (cobertura 100%). Evidência completa em `r9i-instalacao-consolidada.md`.

### 3.11 Estado do banco (consultas)
- `tenants=3976`, `usuarios=1470`, `saas_tenants=0`, `modulos_sistema=1142`, `tenant_modulos=23`, `perfis=26`, `adm360_parceiros=5765`.
- Sem tenant “Sistema Vertical”/“VERTICAL” → criação idempotente segura.

## 4. Bloco A (gate de estabilização)
- Build/suíte verde em baseline (1394×3). Mecanismo de autenticação validado. Banco disponível. Isolamento de tenant coberto por tenant sintético de isolamento (seed 122).
- Sem defeito bloqueante confirmado até aqui além do §3.7 (bug de contrato, não bloqueador de build/login). Gate revalidado (build+auth+isolation) antes do trabalho dependente.

## 5. Plano de execução (ordem do §1)
1. **r9a — Bloco A + §4 Sistema Vertical:** validar gate; criar/reutilizar organização+tenant+admin `admin.sistemavertical@plantaopro.local` (senha temporária por BCrypt cost 11 real), perfil admin do próprio cliente, módulo ADM360 + permissões não-SaaS, reutilizando plano `HOMOLOGACAO_360`, seed idempotente sem duplicar/reescrever senha; evidência de login+abertura do Administrativo 360.
2. **r9b — §5 Produtos/Lotes/Validade:** concluir cadastro (identificação/estoque), UI de lotes vinculados na tela do produto, regras de estoque/origem/validade.
3. **r9c — §6 ANVISA:** campos de cadastro + aplicabilidade + origem manual + rastreabilidade.
4. **r9d — §7 Precificação:** `preco_venda` + margem/markup recalculados no servidor + estados (ausente/não calculável/negativo).
5. **r9e — §8 Cotações com ANVISA por item:** snapshot preservado por versão.
6. **r9f — §9/§10 Financeiro por Cliente + Vencimentos:** Hospital→Cliente + relatórios com competência/vencimento/liquidação por parcela, sem multiplicar totais.
7. **r9g — §11 DRE gerencial por cliente:** reconstrução + reconciliação + limitações.
8. **r9h — §12 Design + i18n (pt/en/es/fr)** nos fluxos ajustados, preservando Gestão Organizacional.
9. Cada slice: build + testes (suíte) + migrações incrementais + evidência; commits locais, sem push/merge automático.

## 6. Registro de fechamentos da fila pendente (atualizado por slice)

- **r9j — i18n completude (2026-10-15): PRONTO** nos 3 arquivos restantes do escopo: `RelatoriosFinanceiros.cshtml` (108 chaves), `Produtos.cshtml` (99) e bloco ANVISA de `Cotacoes/Detalhes.cshtml` (12). Contrato Adm360 **283 → 502 chaves × 4 idiomas** (paridade verificada por parse XML e teste de contrato); suíte efetiva 1397/1397 (1 flake de deadlock 40P01 em `SaasComercialB4MatrizTests.Dispose`, retry isolado 12/12). Evidência: `r9j-i18n-completude.md`. Dívida registrada remanescente: literais fora do bloco ANVISA em `Cotacoes/Detalhes.cshtml` (guia/status/outbox/estorno/IA).
- **r9k — B14 flake / ProvisionamentoB5 (2026-10-15): PRONTO**. Crash-idempotência da semente: `SementarAsync` varre os 7 CNPJs dos testes via `LimparPorCnpjAsync` antes da purga/insert + varredura final de solicitações órfãs (arquivo de teste único, +23 linhas). Prova controlada: com 3 órfãos plantados, o P1 **falha** no código HEAD exatamente com `"CNPJ já cadastrado."` (reprodução R8-B14 §5) e **passa** com a correção (órfãos zerados pós-execução); classe isolada 11/11. Suíte efetiva 1397/1397 (1 flake ambiental 40P01 em `SaasComercialB4MatrizTests.B3_DecidirDowngrade…`, retry isolado 12/12 — mesma família, método diferente, fator RAM baixa). Evidência: `r9k-b14-flake-provisionamento.md`. Candidato registrado: mesma receita para `SaasComercialB4MatrizTests` (**fechado em r9m-P2**).
- **r9m-P2 — flake B4 matriz / SaasComercialB4MatrizTests (2026-10-15): PRONTO**. Fechamento do candidato de r9k §6: pré-limpeza por chave natural na semente (`delete ... usuarios where lower(email_normalizado)='b4-usuario@exemplo.test'`, único fixture com índice único parcial — `tenants.codigo` sem índice único) + helper `ExecutarComRetryImpasseAsync` (retry único após 250 ms em `PostgresException` 40P01/40001) envolvendo semente e purga; arquivo de teste único, ≈+30/-11 linhas, sem mudança em produção/DB. Classe isolada **12/12**; suíte completa **1398/1398** sem flake na execução de fechamento. Evidência: `r9m-p2-b4matriz-flake.md`.
- **r9m-P4 — i18n completude Cotacoes/Detalhes fora do bloco ANVISA (2026-10-15): PRONTO**. Dívida de r9j §7 fechada: **103 chaves novas** `Adm360.CotDet.*` × 4 idiomas (contrato Adm360 **502 → 605**), 102 substituições exatas na view (guia/status/itens/modal/anexos/outbox/estorno/IA; strings dinâmicas por fragmentos; reuso de `Shift.Back`, `Clinical.ButtonCancel`, `Implantacao.CloseAria`, `Login.HelpSummary`, `Adm360.Cotacoes.ThResponseDeadline/StatusExpirada`); bloco ANVISA inalterado; residuais com acento = 7 comentários não renderizados; fact novo `ViewsAdm360_R9mDetalhesCotacaoCompletudeSemLiterals` (suíte 1397 → 1398). Suíte completa **1398/1398**. Evidência: `r9m-p4-cotdet-i18n.md`.
- **r9l — Screenshots / aprovação visual (2026-10-15): screenshots BLOQUEADA (ambiente)** — `browser.screenshot` exige a janela desktop do OpenCode visível ao usuário (~10 tentativas; erro exato e protocolo de retry documentados). Evidência funcional completa e independente da aprovação visual: as 3 telas de r9j verificadas ao vivo via DOM com sessão autenticada do admin SV (âncoras pt-BR, coluna/valores ANVISA renderizando, dados reais do tenant) + artefatos de `outerHTML` em `artefatos-dom/`. Zoom 200% e breakpoints 360/768/1440: verificados por construção apenas (a webview não expõe zoom de navegador nem device-metrics). **Aprovação visual não declarada** (requer pixels + aceite do usuário). Descobertas de ambiente registradas: API sem connection string explícita resolvia `plantaopro_test` (origem na árvore de processos de background); Npgsql 10.0.2 rejeita o formato URI (KEY=VALUE funciona); hash/senha do admin SV validados por BCrypt direto contra o build. Evidência: `r9l-captura-visual-artefatos.md`.
