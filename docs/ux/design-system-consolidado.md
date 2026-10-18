# Design system PlantãoPro — documento consolidado (R53)

Data: 2026-10-18 · Rodada: R53 (consolidação final da diretriz r24a1). Este é o documento canônico e
vivo do estado de design do produto após as rodadas R24–R53. Os fragmentos anteriores deste diretório
(`design-system.md`, `design-system-premium.md`, etc.) permanecem como histórico; em caso de conflito,
este documento vence.

Como atualizar: qualquer mudança de design que vire **regra** (não apenas ajuste pontual) deve vir
acompanhada de (1) atualização deste arquivo e (2) contrato permanente em `PlantaoPro.Tests` — a casa
trava regra por teste, não por convenção.

## 1. Camadas de CSS e ordem canônica

CSS versionado em blocos `vNNNN-nome.css` sob `wwwroot/css/design-system/`. A ordem de carregamento é
**parte do contrato** (o contrato R49 trava a posição da fundação de acessibilidade nos dois layouts):

`_Layout.cshtml` (autenticado) termina assim:

```
… v2144-responsive-safe-actions → v2145-onboarding-help → v2146-pending-notifications → kanban
→ v2148-operational-premium → pages/administrativo360-tema-claro
→ v2150-bloco-c-canonical → v2151-wp-c-homologacao
→ accessibility.css   <!-- R49: fundacao de acessibilidade -->
```

`_AuthLayout.cshtml` (login/registro/recuperação) usa um subconjunto próprio mas com o **mesmo
sufixo canônico** `v2150 → v2151 → accessibility.css` (paridade garantida na R49).

Regras da casa:

- Mudança global nova = **bloco novo versionado** carregado depois dos existentes; não se edita o
  passado para corrigir presente.
- Não existe `tokens.css` global carregado: os tokens são **duplicados nos blocos versionados**
  (padrão histórico da casa, documentado em R49/D-R49-5). Ao criar bloco novo, copie os tokens que
  usa do bloco mais recente.
- `accessibility.css` fica por último e carrega fallbacks próprios nos `var()`: skip-link,
  `:focus-visible`, `prefers-reduced-motion`, fundação de foco/movimento.
- Todo `<link>` usa `asp-append-version="true"`.

## 2. Tokens e contraste (E4 + revisitado R49)

Contraste medido sobre os hex reais (R49 — fato 4 do contrato trava ≥ 4,5:1 nos pares críticos):

| Token | Razão de contraste |
|---|---|
| text-primary | 16,91 : 1 |
| text-secondary | 7,02 : 1 |
| text-muted | 5,59 : 1 |
| text-soft | 4,76 : 1 |
| success | 5,38 : 1 |
| warning | 6,85 : 1 |
| danger | 6,49 : 1 |
| info | 6,48 : 1 |
| azul de marca (v2151 `#1d6fe9`) | 4,66 : 1 (AA) |

Decisões travadas:

- **D-R26-1** (R26): em fundo escuro, texto usa branco 50% (`text-white-50`), nunca `text-muted` —
  varredura de 214 trocas em 38 views Adm360; scanner de adjacência DOM permanente (`R26ContrasteZonasEscurasContratoTests`).
- Azul de marca: `#1f73f1` do tokens falhava em AA; o valor efetivo AA `#1d6fe9` é carregado por
  último no `v2151-wp-c-homologacao.css` (nome do arquivo é técnico, o que vale é o hex final).

## 3. Componentes

- `_PageHeader` — título + descrição curta + ícone; toda tela operacional começa com ele.
- **Guia da tela** (`details.pp-screen-guide`) — padrão E3, presente nas 29 páginas Adm360 (R35–R37)
  e no portal (R35). Estrutura fixa: *para que serve · como utilizar (passos curtos) · efeito da ação
  · próxima ação*. Exemplo canônico: a própria Central de Ajuda (R51).
- `pp-guided-journey` / `_GuidedJourney` — jornada guiada por perfil (primeiros passos, implantação);
  botão "Fazer tour" com `data-tour-start="help-center"` já aponta para a Central de Ajuda.
- Estados (R44): vazio/carregamento/erro em linguagem simples; campos rotulados com "Não informado";
  **dump JSON cru em view é proibido** (contrato contra `JsonSerializer.Serialize(Model?.Data)` em
  `.cshtml`). Erros de API renderizam a mensagem repassada com fallback (D-R24-1).
- Partials de base SaaS (histórico): `_KpiCard`, `_StatusBadge`, `_EmptyState`, `_ActionBar`,
  `_FilterBar`, `_ConfirmModal`, `_ToastMessages`, `_TenantContextBadge` (zona super-admin),
  `_ModuleLockedNotice`, `_PlanLimitReachedNotice`, `_UpgradeCta`.
- Confirmação de ação destrutiva sempre por modal; toasts para sucesso/erro leve.

## 4. Acessibilidade mínima (R49) e pendências

Entregue (travado por `R49AcessibilidadeContratoTests`, 5 fatos):

- Todo campo tem nome acessível: auditoria final inputs+selects+textareas = **882**, buttons =
  **670**, VIOLATIONS = **0**. Regra de nome acessível inclui **label envolvente** (janela de 90
  chars). 215 `aria-label` inseridos automaticamente + 8 manuais + 12 botões só-ícone nomeados.
- Sem mudança de `id`/layout: nomes entram por `aria-label`.
- Layouts: `lang` correto, `skip-link`, `:focus-visible`, `prefers-reduced-motion` (fundação em
  `accessibility.css`, D-R49-5).
- Contraste dos pares críticos ≥ 4,5:1 (seção 2).

Pendências documentadas (D-R49-4 — fora do escopo "básico"): trap de foco em modais, `aria-live` em
toasts, audit de landmarks, tabelas densas (`th scope`/summary), teclado em widgets custom.

## 5. Formulários (E5, régua D-R30-1)

- Validação **HTML5 nativa** (sem framework novo): `required`, `maxlength`, `min`, `pattern`.
- `maxlength` = tamanho **exato da coluna** (fonte: `information_schema`); teto maior que a coluna
  é bug (corrigido na R30).
- `pattern` só para formato fechado (CNPJ/CPF/sku/EAN/uuid); valores monetários com `min 0`.
- O servidor continua sendo a autoridade final de validação; a validação de view é experiência.
- Contratos: `R30ValidacaoFormulariosCadastrosContratoTests`, `R31ValidacaoFormulariosOperacionaisContratoTests`.

## 6. Listas e busca: nome na tela, id no banco (E6)

- Selects exibem **nome** e salvam **id**; campo livre de id em UI é exceção auditada (KEEPs:
  `ProdutoId` manual em Cotações, `EntidadeInternaId`, `VendedorId` — R33).
- Lookups específicos por ação reutilizam **endpoints já existentes** (D-E6-1/D-E6-2: zero rota nova,
  zero mudança de contrato BFF/API, ADR-0003 intacto).
- GUID nunca cru em copy: fragmentos `[..8]` banidos (R50 aplica em Convites; inventário completo dos
  fragmentos remanescentes é pendência registrada).
- Contratos: `R32LookupsCampoDeIdContratoTests`, `R33E6LookupsReservaEPedidoContratoTests`,
  `R50CopyContratoTests`.

## 7. Navegação e menus (R43/R46)

- Rótulos de menu vêm do resx (`Nav.*`); **menu e título da página usam a mesma chave** (D-R43-1 —
  nunca divergem).
- Item novo só aparece sob permissão (`Can("…") && !isGlobalAdmin`) (D-R43-2).
- Mapa único de ícone por item (R46: 20 trocas de deduplicação + 5 pares residuais **declarados como
  exceção no contrato**, D-R46-1); ícone inválido falha em teste, não em runtime (D-R46-2).

## 8. Linguagem simples, tom de voz e glossário (E1/E8/R50)

- Jargão técnico não aparece em copy renderizada: BFF, scoping, seed, endpoint, GUID, outbox etc.
  saíram em quatro ondas (R27 portal → R28 Adm360-1 → R29 demais módulos → R34 valores resx ×4
  culturas). Anti-regressão por token nas 4 culturas (`R34LinguagemSimplesResxContratoTests`).
- Vestígios de homologação/demo removidos (R25); **whitelist de domínio** permanece travada:
  SEFAZ `HOMOLOGACAO/PRODUCAO`, "Não Homologada", Operação Assistida, marketing comercial,
  "demonstrativo financeiro", nomes técnicos em código.
- **Glossário canônico (D-R50-1)**: fallback → padrão/reserva · token → link/sessão · endpoint/tier
  → plano · invoice/billing → fatura/assinatura. O banimento vale **só em copy**: nomes de código
  (`FallbackProvedor`, `Endpoint = "/api/..."`) e chaves (`Nav.Fallback*`) permanecem.
- "Dashboard" mantido como termo consolidado de negócio; renomeação para "Painel" segue pendente de
  decisão de produto (decidir junto da revisão de menus).
- Tratamento: "você"; frases curtas com verbo; sem exclamações na copy nova. Auditoria de tom R53:
  restam 31 `!` em strings literais de Views (~19 arquivos) e 4 valores de toast em cada cultura de
  resx ("…com sucesso!") — pendência de passada de tom (inventário no r53b1).
- i18n: 4 culturas (`UiTexts.resx` neutro = pt-BR + `en`/`es`/`fr`); o contrato `R7DF6` trava as
  **653 chaves** `Adm360.*` (valores são livres, chaves não mudam); namespaces novos (`Nav.*` etc.)
  entram fora dessas 653.
- Detalhe de plataforma documentado: o Razor desta base emite acentos como **entidades numéricas HEX**
  (`&#xE7;`) — travado por `R7E1I18nFundacaoTests`; ferramentas de verificação devem decodificar
  antes de comparar texto acentuado (aprendizado registrado na R52).

## 9. Acesso e isolamento (E2)

- **D-R38-1**: instituição opera por **CNPJ**; pessoa é identificada por **e-mail global único +
  senha** com vínculo explícito ao cliente; CPF opcional. Login aceita CPF/CNPJ/e-mail como
  identificador (`LoginViewModel` consolidada).
- **D-R38-2 / D-R24-3**: "tenant" é linguagem interna/operador da plataforma; a instituição vê
  "instituição"/"escopo de acesso".
- **Tenant visível apenas ao super-admin** (R39, contrato `R39TenantSoParaSuperAdminContratoTests`):
  `AdminSaas` é restrito aos papéis de plataforma (`AdministradorGlobal`, `Suporte`, `Auditor`) e
  nenhum `.cshtml` fora de `AdminSaas/` e da whitelist de 25 keeps pode conter "tenant".
- Perfis do catálogo (`RoleCatalog`): prioridade define o papel primário da sessão
  (`ADMINISTRADOR_CLIENTE`=700 > `ADMINISTRADOR`=650 > …); o papel primário decide redirecionamento
  pós-login e filtro contextual (ex.: Central de Ajuda).

## 10. Central de Ajuda (R51)

- `AjudaController` filtra tópicos/artigos pelo papel primário da sessão; a família administrador de
  tenant lê um único manual (D-R51-1: `ADMINISTRADOR` → `ADMINISTRADOR_CLIENTE`).
- Tópicos: operação da instituição (orçamento, compras, estoque, títulos, relatórios), Minha Central
  e avisos (fatura vencida R48, notificações), privacidade LGPD + tópicos operacionais históricos.
- Cada artigo: resumo curto + passos em imperativo + **link direto para a tela real** (rotas
  verificadas no controller antes de escrever). Contrato: `R51AjudaContratoTests`.
- Pendências: artigos para `DIRETOR`/`RECEPCAO`/`PARCEIRO`; tour guiado consumindo os artigos.

## 11. Performance percebida (linha de base R52)

- Média das páginas padrão (dev local, loopback, pós-aquecimento): **~50 ms**; orçamento registrado:
  < 3000 ms por página.
- Listas densas são a exceção conhecida: `/Administrativo360/Orcamentos` renderiza **17,2 MB** de HTML
  (~12–20 s) e `/Estoque` **8,75 MB** (~9–19 s) — pendência de paginação/virtualização registrada na
  R52, a reverter esta linha de base quando for atendida.
- Smoke e2e versionada e reproduzível:
  `docs/evidencias/2026-10-18-rodada-52/r52_smoke_e2e.ps1` (13 verificações + perf + a11y).

## 12. Índice de contratos que travam o design

| Área | Contrato(s) |
|---|---|
| Contraste/adiacência escura | `R26ContrasteZonasEscurasContratoTests`, `R49AcessibilidadeContratoTests` (fato 4) |
| Formulários | `R30ValidacaoFormulariosCadastrosContratoTests`, `R31ValidacaoFormulariosOperacionaisContratoTests` |
| Lookups nome×id | `R32LookupsCampoDeIdContratoTests`, `R33E6LookupsReservaEPedidoContratoTests` |
| Linguagem/jargão | `R25TextosSemHomologacaoContratoTests`, `R27/R28/R29LinguagemSimples*`, `R34LinguagemSimplesResxContratoTests`, `R50CopyContratoTests` |
| Instruções por página | `R35/R36/R37InstrucoesPorPaginaLoteNContratoTests` |
| Acesso/tenant | `R39TenantSoParaSuperAdminContratoTests`, `V2155TenantPermissionBoundaryTests`, `R38*` |
| Navegação/menus/ícones | `R43NavigacaoMenusContratoTests`, `R46*` (unicidade de ícone) |
| Estados sem JSON cru | fato R44 (contra `JsonSerializer.Serialize` em views) |
| Responsividade | fato R45 (tabela sempre dentro de `.table-responsive`, keeps de impressão declarados) |
| SaaS/faturamento | contratos R40 (limites), R47 (faturas unificadas/cobrar), R48 (vencimento/adiplência) |
| Ajuda central | `R51AjudaContratoTests` |
| i18n/chaves | `R7DF6Admin360I18nContratoTests` (653), `R7E1I18nFundacaoTests` (entidades hex) |
| Higiene do repositório | `RepositoryHygieneContractTests` (termos vetados, `href="#"`, segredos) |
| Consolidação E4/E5/E6/E8 | `R42ScannersPermanentesContratoTests` (um ponto de entrada por regra) |

Guarda-mão de build: rebuild real + gate multiset de warnings vs `r17_pristine_build.log` (160 chaves)
deve sair `DIFF=NENHUMA` em toda rodada que toca código. Suíte de referência na consolidação:
**1605 testes**.
