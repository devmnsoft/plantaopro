# R50 · r50z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R49 (`f255d3f`, suíte 1599). Plano: `r50a0-passada-final-copy-plano.md`.

## Escopo da rodada

Roadmap R50: **passada final de copy (tom de voz único, glossário)** — última varredura sistemática de
texto visível antes da Ajuda central (R51), com glossário canônico registrado e contrato permanente.

## O que foi entregue

### 1. Glossário canônico (D-R50-1)

| Termo | Uso no produto | Banido em texto visível |
|---|---|---|
| modelo de reserva / padrão | config de IA, logos, valores por defeito | "fallback" |
| link (de redefinição) / sessão | autenticação | "token" |
| plano (do cliente) | recursos contratados | "endpoint", "tier" |
| fatura / assinatura | SaaS | "invoice", "billing" em frases |
| organização / profissional | tenant/perfil quando o claim não existe | "tenant" fora da zona super-admin (já R39) |

Decisões registradas: nomes de **código** (`name="FallbackProvedor"`, `Endpoint = "/api/..."`, chaves
`Nav.FallbackTenant`) permanecem — jargão banido é em *copy*. A palavra **"Dashboard"** foi mantida como
termo de negócio consolidado; a mudança global para "Painel" ficou pendente para R51+ junto da revisão de
menus. Traduções en/es/fr sem jargão equivalente identificado nesta passada (fora de escopo).

### 2. Correções aplicadas (13 pontos, 6 arquivos)

| Arquivo | Mudança |
|---|---|
| `AssistenteIa\Index.cshtml` | 5 usos de "fallback" → "Modelo de reserva" / "Provedor de reserva (opcional)" / "Modelo de reserva (opcional)" / placeholder "padrão vigente do provedor" / form-text limpo |
| `WhiteLabel\Index.cshtml` + `Preview.cshtml` | KPI `"Fallback"` → `"Padrão"`; help "usa o fallback PlantãoPro" → "usa o padrão PlantãoPro" (2 arquivos) |
| `Convites\Index.cshtml` | `title="Reenvio não está contratado neste endpoint."` → "O reenvio de convites não está disponível no seu plano."; removido `<dd>@item.PlantaoId.ToString()[..8]</dd>` (fragmento de GUID cru sob "Plantão" — E6; redundante com a ação "Abrir plantão") |
| `UiTexts.resx` (pt-BR) | `Auth.ResetTokenInvalid`: "Token inválido ou expirado." → "O link de redefinição é inválido ou expirou."; guia RelFin "(fallback de nome)" → "(resolução alternativa de nome)" |

Nenhum nome de chave/chave contada mudou (R7DF6: 653 chaves Adm360 intactas); nenhum teste previa os
valores antigos (verificado por grep antes das edições).

### 3. Contrato permanente — `R50CopyContratoTests` (3 fatos)

1. `JargaoTecnico_NaoApareceEmTextoVisivelDasViews` — 7 frases-jargão (frases, não palavras, para não
   bater em identificadores de código) ausentes em todos os cshtml.
2. `ResxPtBr_SemJargaoDeSessaoNosValores` — nenhum `<value>` do `UiTexts.resx` pt-BR contém "fallback"
   ou "token" (case-insensitive, word boundary), com guarda de sanidade do volume de valores.
3. `Convites_NaoExibeFragmentoDeIdCru` — a lista de convites não volta a exibir `PlantaoId.ToString()[..8]`.

## Evidências

- Build real `dotnet build backend\PlantaoPro.sln -c Release --no-incremental` → **exit 0**; gate de casa
  multiset vs `r17_pristine_build.log` → **160/160, DIFF=NENHUMA**.
- Classes dirigidas (R50 + R49 + higiene + R7DF6 i18n + R39 tenant): **25/25 verde**.
- Suíte integral: **1602/1602** — execução em background `%TEMP%\opencode\r50_suite.log` saiu **exit 0**
  (resumo do xunit cortado no redirect, confirmado pelo exit code da `dotnet test`). Re-execução em
  foreground atingiu **1 caso da família de flake conhecida**
  (`R19DapperBindingGuardTests.Db_CentralClientes_SqlCompletaDaFonte_…`); protocolo imutável aplicado:
  isolate ×5 → **5/5 verde** (`r50_flake_1..5.log`).

## Pendências documentadas (pra frente)

- Renomeação global "Dashboard" → "Painel" (se aprovada) com a revisão de menus/navegação (R51+).
- Revisão de tom por zona (exclusões de "!", tratamento de usuário) — auditoria ampla fica para R51/R53.
- Demais exibições de id fragmentado fora de `Convites` (inventário E6) — pendência conhecida, sem achado
  novo nesta rodada além do corrigido.

## Commits

| Commit | Conteúdo |
|---|---|
| (a seguir) `web,tests (rodada 50 r50b1)` | 13 correções de copy + contrato R50 (3 fatos) |
| (este) `docs (rodada 50 r50z)` | este relatório |
