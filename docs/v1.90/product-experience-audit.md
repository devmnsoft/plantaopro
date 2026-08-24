# PlantãoPro v1.90 — auditoria e consolidação da experiência

## Diagnóstico inicial

A entrada `plantaopro.css` importava **39 folhas**, das quais 19 tinham o número de uma versão no nome. Essas camadas somavam 1.121 linhas e repetiam responsabilidades de shell, formulários, feedback, clínica e páginas. A ordem de importação fazia correções de versões posteriores vencerem por posição, não por uma arquitetura explícita.

O inventário estático encontrou 384 views Razor, 36 ocorrências de `!important`, 20 views com `style` inline, 82 declarações de `min-width` e 40 declarações de `z-index`. O shell também misturava os vocabulários `.app-*` e `.pp-*`, deixando dimensões e comportamento responsivo dependentes de regras históricas. A versão `v1.63` estava escrita diretamente na sidebar.

## Causas das quebras

1. Cascata cronológica em vez de cascata por responsabilidade.
2. Tokens canônicos coexistindo com aliases de várias gerações.
3. Duas famílias de classes do shell sem contrato único.
4. Topbar sem política consistente de truncamento e redução progressiva.
5. Tabelas legadas usando largura mínima como única resposta mobile.
6. Valores de `z-index` fora da escala de tokens.
7. CSS específico de módulo misturado ao CSS transversal.

## Mapa consolidado

| Camada | Responsabilidade |
| --- | --- |
| `tokens.css` | identidade, semântica, tipografia, espaçamento, dimensões e z-index |
| `foundation.css` | normalização e elementos globais |
| `typography.css` | escala e utilitários tipográficos |
| `layout.css` | shell, canvas e grids |
| `navigation.css` | sidebar, topbar e navegação |
| `components.css` | componentes transversais e compatibilidade migrada |
| `buttons.css`, `forms.css`, `cards.css`, `tables.css` | componentes por função |
| `feedback.css`, `states.css`, `overlays.css` | loading, estados, diálogos e feedback |
| `workspaces.css` | details e áreas de trabalho |
| `clinical.css`, `financial.css`, `operations.css`, `saas.css` | linguagens de domínio dentro dos mesmos tokens |
| `responsive.css`, `accessibility.css` | breakpoints, redução progressiva e WCAG |
| `white-label.css` | sobrescritas autorizadas de marca |

## Migração da cascata

As regras úteis das folhas `v151` a `v183` foram movidas integralmente para a camada canônica correspondente antes da remoção dos arquivos e imports. Nenhuma regra foi descartada durante a consolidação. O entrypoint agora contém somente imports semânticos e previsíveis.

## Inventário de views

- **Canônicas:** shell compartilhado, autenticação, Minha Central, Meu Dia e workspaces que já usam componentes `pp-*`.
- **Precisam migrar:** CRUDs com Bootstrap estrutural, tabelas sem `data-mobile-cards` e formulários longos sem ações persistentes.
- **Legadas:** diretórios `V112`, `V114` e `V116`, mantidos porque ainda possuem controllers/rotas.
- **Não utilizadas/duplicadas:** exigem telemetria de rota e validação funcional antes de remoção; nada foi apagado nesta versão.

## Escopo entregue e continuidade

Esta consolidação cobre fundação, shell, navegação, login existente, responsividade transversal, escala de overlays e fonte central de versão. As jornadas e dados existentes foram preservados. A migração individual das 384 views permanece incremental: priorizar na v1.91 os CRUDs clínicos, financeiros e SaaS que ainda dependem de tabelas e formulários legados, seguida de remoção de aliases após telemetria.

## Gate visual

O script `scripts/ui/product-experience-audit.mjs` falha para imports CSS inexistentes, CSS versionado reintroduzido, versões hardcoded no shell, `href="#"` e `z-index` extremos. O smoke Playwright existente continua sendo a verificação navegada quando há runtime e credenciais disponíveis.
