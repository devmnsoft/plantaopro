# R27 · r27z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R26 (`e880524`, suíte 1540).

## Escopo da rodada

Epic **E1** da diretriz (r24a1), fatia zona portal: reescrever jargão técnico visível ao usuário em
linguagem simples.

## Commits

| Commit | Conteúdo |
|---|---|
| `6be27b8` | r27b1: 19 pontos em 7 views (Plantoes, Agendamentos, FaturamentoClinico, DashboardsPremium/Perfil, OperacaoPremium/Cobertura, V114/Produto) + `R27LinguagemSimplesPortalContratoTests` (2 fatos) + r27a0 |

## Destaques da reescrita

- **Botão "Atualizar BFF"** → "Atualizar cobertura" (arquitetura BFF era o único jargão de arquitetura
  exposto em rótulo de ação em toda a UI).
- **URL de endpoint crua** `Fonte: <code>/api/...</code>` removida de 2 locais (`AgendaPremium`, template
  `V114\Produto.cshtml` — que renderiza as páginas de produto de faturamento; bônus E2: retira "tenant").
- Padrão "**retornado pela API / não informado pela API**" → "quando for gerado no sistema / não informado"
  em ~12 strings de tooltip/tabela/vazio — o usuário passa a ler estado de dados, não infraestrutura.
- Perfil: "KPIs carregados da API /api/dashboards/... respeitando tenant..." → "Indicadores do seu perfil,
  plano e módulos contratados."

## Whitelists versionadas (r27a0)

Código, não texto renderizado: `Guid.Empty` em Razor, propriedade `Endpoint = "/api/..."` dos componentes de
busca, `fetch()` em JS (Bi), atributos `data-lookup-endpoint`, comentários Razor `@* *@` (incl. o comentário
drill-down R10 do Perfil que menciona reg_status).

## Gates e suíte

- Rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Suíte completa final: **Total 1542** (= base 1540 + 2 fatos), **0 falhas**, 1m49s.
- Contrato `PosRcUxFinalQaContractTests` revisado: substrings travadas ("Fonte:", "Somente indicadores reais
  são exibidos.") permanecem satisfeitas — 6/6 nas classes afetadas.

## Recon já iniciado para a R28 (E1 Adm360 parte 1 — suprimentos/compras)

Scan amplo de tokens técnicos em `Administrativo360` (script `%TEMP%\opencode\r28_scan_jargao2.ps1`):
- **UUID exposto como label/placeholder** (alvos claros E1+E6): `OrcamentoNovo` L76 "UUID do Vendedor
  (opcional)", `ValeNovo` L133/L161/L236 "UUID da reserva ativa", `Relacionamento` L157 "ID Interno UUID
  (Opcional)", `DocumentosXml\Detalhes` L234/L238 "ID do Pedido de Compra (UUID)" / "ID do Local de Estoque (UUID)".
- **Outbox** em texto: `Gestao\Monitor` L157 "Fila e Transmissões Outbox" + L179 "Nenhuma transmissão outbox
  registrada."
- **"API" em texto de ajuda**: `TituloDetalhes` L164 "Sem confirmação bancária automática por API.",
  `Fiscal\Configurar` L133 (pool da API / nunca no banco — reescrever mantendo a advertência).
- **BOM/hash**: `DocumentosXml\Importar` L31 "(incluindo BOM)... hash SHA-256 verificado" — decidir grau de
  tecnicidade aceitável (integridade de arquivo tem valor informativo; simplificar a redação).
- Whitelist: `is not null` em Razor, URLs `/api/...` em href, comentários de rodada, tokens de sincronização
  DF-e/Sefaz (domínio fiscal).

## Estado de casa

- Apps PARADOS, portas livres. `origin/main` sincronizada neste push.
- Fila: **R28 = E1 Adm360 parte 1** (suprimentos/compras: UUIDs, outbox, textos fiscais); roadmap r24a1 segue.
