# R52 · r52z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R51 (`7dbf1ec`, suíte 1605). Plano: `r52a0-hardening-smoke-e2e-performance-plano.md`.

## Escopo da rodada

Roadmap R52: **Hardening — smoke e2e completa + performance percebida**. Rodada sem mudança
funcional de código-fonte: entrega uma smoke e2e scriptada e **versionada** cobrindo as jornadas
principais em API+Web (incluindo as áreas novas da R51) e fixa a linha de base de performance
percebida em ambiente dev local.

## O que foi entregue

### 1. Smoke e2e versionada — `r52_smoke_e2e.ps1` (executável, nesta pasta)

Fluxo real com apps Release `--no-build` contra `plantaopro_test`:
1. API (`http://localhost:51976`) e Web (`http://localhost:51977`) subidas, espera de boot (90s/120s).
2. Login demo do gestor via form + antiforgery (padrão R49) → redirect `/ClientePortal/Index`.
3. Jornada Web de 13 verificações (cada página deve responder 200 com marcador no HTML renderizado):
   Minha Central; Administrativo360 (Vendas, Orçamentos, Pedidos de Compra, Estoque, Títulos a
   Receber, Relatórios Financeiros); Notificações/Preferências; e o pacote novo da R51 na Central de
   Ajuda — Index exibe o tópico "Operação da instituição" para o perfil `ADMINISTRADOR_CLIENTE`
   (confirma D-R51-1 ao vivo), Artigo `adm-orcamento`, Artigo `central-fatura`, Modulo
   `instituicao` (link "Abrir tela relacionada").
4. Verificações transversais (R49) na sessão autenticada: `aria-label=` ≥ 5 (mediu 26),
   `v2151-wp-c-homologacao.css` = 1, `accessibility.css` = 1, `skip-link` = 1.
5. Teardown de casa: apps parados, `PORTAS_LIVRES=True`.

**Resultado final: `R52_E2E_OK`, FALHAS=0** — 13/13 páginas OK (log da execução final em
`r52_smoke_e2e.log`, gerado ao lado do script).

### 2. Performance percebida — linha de base (dev local, loopback, amostra pós-aquecimento)

| Rota | Tempo | Observação |
|---|---|---|
| /MinhaCentral | 40 ms | padrão |
| /Administrativo360/Vendas | 84 ms | padrão |
| /Ajuda/Index | 41 ms | padrão |
| /Notificacoes/Preferencias | 35 ms | padrão |
| **MÉDIA PÁGINAS PADRÃO** | **50 ms** | orçamento < 3000 ms — atendido com folga |
| /Administrativo360/Orcamentos | ~11.600–19.600 ms | **HEAVY: payload HTML 17,2 MB** |
| /Administrativo360/Estoque | ~9.300–19.200 ms | **HEAVY: payload HTML 8,75 MB** |

O script mede com aquecimento descartado por rota; páginas com payload > 1 MB entram como HEAVY
fora da média padrão (a smoke só falha se a média das páginas padrão estourar 3 s).

### 3. Achados da hardening (o motivo de existir a rodada)

1. **404s transitórios logo após o startup**: as primeiras ondas de requests após o boot podem
   responder antes de toda a pipeline de rotas estar pronta (observado nas execuções iniciais:
   todas as páginas 404, tudo 200 em seguida). Tratado no próprio script com warm-up
   (`WARMUP_OK`) + retry por página (3 tentativas) — comportamento documentado, não bug de rota.
2. **Acentos como entidades numéricas HEX no HTML**: o Razor desta base emite acentos como
   `&#xE7;` (ex.: título renderizado `Como criar or&#xE7;amento`). Comportamento já conhecido e
   travado por `R7E1I18nFundacaoTests` ("acentos chegam como entidades numéricas"). A smoke decodifica
   entidades (hex e decimal) antes de comparar marcadores com acento — sem isso, matchers literais
   dão falso negativo (diagnosticado char a char nesta rodada).
3. **Listas densas pesam no HTML server-side**: Orçamentos (17,2 MB) e Estoque (8,75 MB) demoram
   9–20 s em dev local para renderizar. Sem mudança de infra nesta rodada → **pendência R53+**:
   paginação/virtualização (ou render progressivo) dessas duas telas; as demais áreas operacionais
   estão todas < 250 ms.

## Evidências

- Smoke e2e versionada: `docs/evidencias/2026-10-18-rodada-52/r52_smoke_e2e.ps1` + log final
  `r52_smoke_e2e.log` (`R52_E2E_OK`, FALHAS=0, `MEDIA_PADRAO=50ms`).
- Sem alteração de código-fonte nesta rodada: build/gate/suíte permanecem no estado verde da R51
  (build exit 0, gate 160/160, suíte 1605/1605).
- Ambiente restaurado: API/Web parados, `PORTAS_LIVRES=True`, postgres 5432 ativo.

## Pendências documentadas (pra frente)

- R53+: performance das listas densas (Orçamentos 17,2 MB / Estoque 8,75 MB) — paginação ou
  virtualização; reverter a linha de base desta rodada após a otimização.
- Ampliar a smoke e2e com jornada de escrita (criar → aprovar → receber) quando houver seed demo
  idempotente estável em `plantaopro_test`.

## Commits

| Commit | Conteúdo |
|---|---|
| (este) `docs (rodada 52 r52z)` | plano r52a0 + smoke versionada + log + este relatório |
