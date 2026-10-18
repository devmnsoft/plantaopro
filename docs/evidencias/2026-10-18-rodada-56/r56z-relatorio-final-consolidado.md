# R56 · r56z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R55 (`24199a4`, suíte 1614).
Plano: `r56a0-copy-dashboard-para-painel-plano.md`.

## Escopo da rodada

Fila r53b1 §7, item 2: **decisão de produto "Dashboard"→"Painel"** junto da revisão de menus —
decisão pendente registrada na D-R50-1 ("termo mantido por ser estabelecido; mudança global fica
para a revisão de menus"). Esta rodada materializa a decisão: **pt-BR passa a usar "Painel"** como
termo canônico para telas de visão consolidada.

Fato que fecha o raciocínio: as traduções idiomáticas já usavam os equivalentes — es
"Panel de control"/"Panel Gerencial", fr "Tableau de bord"/"Tableau de Bord de Gestion", en
"Dashboard" (idiomático em inglês). Só o pt-BR mantinha o empréstimo inglês.

## O que foi entregue

### 1. Copy pt-BR renomeada (19 pontos, resx + views)
- `UiTexts.resx` (4 valores): `Nav.Dashboard` "Dashboard"→**"Painel"** (sidebar/tela principal);
  `Adm360.Dash.PageTitle/Heading/CapDashboardTitle` → **"Painel Gerencial…"**. en/es/fr inalterados.
- Badges de seção (4 telas): ClientePortal, ParceiroPortal, Demo, FeatureFlags → `>Painel<`.
- `DashboardsPremium/Perfil` (Title+eyebrow+h1), `Inteligencia/Dashboard` (Title+h1),
  `SaasDashboard/Index` (Title+h1), `Administrativo360/Gestao/Monitor` (botão voltar),
  `_SuprimentosNav` ("Painel 360"), `ClinicaDashboard/FluxoAtendimento` ("Painel clínico"),
  mock white label (Index/Preview) → "Painel".
- Strings visíveis em C# (5 arquivos): `B2BCommercialOpsWebControllers` ("Painel executivo B2B"),
  `DashboardsPremiumController` (erro amigável), `SaasIntelligenceService` e
  `Saude360ClinicalService` (mensagens da API), `V1420OperationalControllers` ("Painel executivo"),
  `Saude360WebControllers` (título do módulo clínico + link rápido "Painel").

### 2. v2355 — nomes de exibição no catálogo `modulos_sistema`
O tenant via os nomes em Minha Assinatura/contratação/gestão de permissões (o canary e2e encontrou,
na primeira rodada de execução, a capacidade "Dashboard Clinico" exibida ao lado das permissões
`DASHBOARD.*`). Renomeados só os `nome`; códigos estáveis:
- `DASHBOARD`: 'Dashboard' → **'Painel'**
- `CLINICA_DASHBOARD`: 'Dashboard Clinico' → **'Painel Clinico'** (capacidade SAUDE360; ASCII,
  convenção da tabela)

Migration idempotente + sincronização dos dois scripts canônicos (marcador SOURCE/SOURCE-SHA256,
seção 126; SHA256 `43a6f366…1715b7`) + `source-checksums.json` + `migration-manifest.json`
(`dependsOn: [v2354]`, `installRequired: false`). Convenção R55.

### 3. Contrato permanente `R56PainelCopyContratoTests` (7 fatos) + asserção R12 atualizada
- resx pt-BR sem valor visível com "Dashboard" (sanity >500 valores);
- valores exatos de `Nav.Dashboard` nos 4 locales (pt-BR "Painel"; en/es/fr intactos);
- títulos Adm360 exatos "Painel Gerencial…";
- views sem frases antigas fora da zona comercial + exatamente 4 badges "Painel" + sidebar ligada à
  mesma chave `UiText.L("Nav.Dashboard")`;
- v2355 com os upserts exatos nos 3 artefatos (migration, scrpt_completo, pgadmin);
- fato de banco auto-curado (advisory lock + reaplicação + retry 40P01): `nome='Painel'` p/
  `DASHBOARD` e `nome='Painel Clinico'` p/ `CLINICA_DASHBOARD`.
- `R12GuardBiPremiumCatalogTests` : a asserção de h1 passou a travar a nova copy
  ("Painel Administrador Cliente") — mudança de copy, não de comportamento.

Suíte base 1614 → **1621**.

### 4. Prova e2e `r56_painel_e2e.ps1` (reproduzível)
Texto visível (tags/scripts/comentários removidos, entidades decodificadas) sem a palavra
"dashboard": HOME (positivo "Painel" na sidebar), Gestao/Dashboard (positivos "Painel Gerencial" +
subheading), Estoque (card de resumo) e Minha Assinatura/Módulos (códigos `MODULO.ACAO` removidos
antes da assert; canaries RENOMEADO_OK do módulo e da capacidade). Saída versionada em
`r56_painel_e2e_saida.txt`. O arquivo exige UTF-8 **com BOM** (PS 5.1 corrói acentos sem BOM).

## Decisões registradas (D-R56)

| ID | Decisão |
|---|---|
| D-R56-1 | pt-BR: "Painel" é o termo canônico para telas de visão consolidada; en/es/fr mantêm traduções idiomáticas. Nomes de código invariáveis (rotas, controllers, ícones, classes CSS, chaves resx, códigos de papel/capacidade/módulo) — continuidade de D-R50-1. |
| D-R56-2 | Zonas preservadas: ComercialDemoWeb (`<h2>Dashboards</h2>`, zona comercial pública whitelista), breadcrumb da área compat V112 (contrato R4C11), catálogo comercial `'DASHBOARD_GESTOR'`, permissões exibidas como código (`DASHBOARD.VER`…), seeds históricos, URLs/APIs, comentários. |
| D-R56-3 | v2355 renomeia apenas nomes de EXIBIÇÃO (módulo DASHBOARD + capacidade CLINICA_DASHBOARD) em `modulos_sistema`; códigos e grants intactos; ASCII conforme convenção da tabela. |
| D-R56-4 | Contrato R56 (7 fatos) + e2e de texto visível + smoke R52 canônica como regressão geral. |

## Gates (todos verdes)
- Rebuild Release `--no-incremental`: exit 0; gate `r20b1_gate.ps1` **Now=160 Base=160 DIFF=NENHUMA**
  (revalidado após cada rebuild).
- Testes dirigidos (R56 + DatabaseGeneratorIntegrity + R7DF6 + R7DF1 + R4C11 + R50 + R55 + R46): 34/34
  na primeira passada; após o ajuste do v2355, 9/9 no par crítico.
- Suíte integral: **1621/1621** (exit 0, ~23–26 s).
- Smoke R52 (script canônico inalterado): `R52_E2E_OK FALHAS=0` (rodada duas vezes; última contra o
  binário final).
- E2E R56: `R56_E2E_OK FALHAS=0` com `ASSINATURA_MODULO_DASHBOARD_RENOMEADO_OK` e
  `ASSINATURA_CAPACIDADE_CLINICA_DASHBOARD_RENOMEADA_OK`.
- Teardown: `PORTAS_LIVRES=True`.
- Docscan dos docs da rodada: bad=0 (abaixo).

## Incidentes e aprendizados (rodada)
1. **Scripts canônicos truncados**: uma substituição PS5.1 falhou (`LastIndexOf` com exceção →
   `$start=$null` → `Substring(0,0)`) e sobrescreveu `scrpt_completo.sql` e
   `pgadmin/instalar_no_banco_atual.sql` com só o bloco novo. Recuperação: `git checkout --` (R55 já
   commitado, Seção 125 intacta) + re-append idempotente; verificação posterior com contagem de
   ocorrências e hash embarcado. Lição: substituição em arquivo grande exige verificação de
   integridade (tamanho/markers) no mesmo comando.
2. **PS 5.1**: `.ps1` sem BOM é lido como ANSI — termos acentuados nas asserts correm (necessário
   UTF-8 c/ BOM); `$name:`/`$home` dentro de interpolação são armadilhas conhecidas (usar `${name}`;
   evitar `$home`).
3. **Canary e2e rendeu**: a primeira execução falhou por causa real — a capacidade 'Dashboard
   Clinico' aparecia para o tenant em Minha Assinatura e fora do escopo original (só o módulo
   DASHBOARD) → estendeu-se o v2355 antes de fechar, e o contrato passou a travar as duas linhas.

## Próximos itens da fila r53b1 §7
3. Passada de tom (uniformização de frases de ajuda/vazio).
4. Artigos de ajuda DIRETOR/RECEPCAO/PARCEIRO.
5. Acessibilidade avançada.
