# R8-B2 — Notificações legado com dedupe em retries (achado E3)

Data: 2026-10-11 · Base: `9ceddf6` (R8-B1). Segundo corte dos bugs R8 (ordem
quebrado → ausente-alto-impacto, §4 de `r8a0-auditoria-inicial-matriz-estado.md`).

## Problema (achado E3 da varredura de notificações)

O caminho **legado** de notificações (`NotificacaoService.CriarNotificacaoAsync` →
`plantaopro.notificacoes`, usado por escala/financeiro/convites/fechamentos em ~15
métodos de `Data.cs`, `BusinessRulesServices.cs` e `FechamentoOperacionalService.cs`)
inseria **sem dedupe/unique**: cada chamada repetida do mesmo evento (retry após timeout,
duplo POST, reprocessamento) criava uma notificação duplicada para o usuário. O canal
in-app "operacional" (Operation360 `NotificationService.cs:97-98`) já resolvia isso com
chave única `(tenant_id, usuario_id_dedupe, origem_tipo, origem_id, tipo_evento)` +
`on conflict do nothing` + `inserted==0 → false`; o legado simplesmente não tinha esse mecanismo.

## Solução

| Peça | O que mudou | Evidência |
|---|---|---|
| `database/migrations/2026_10_v2346_r8b2_notificacoes_legado_dedupe.sql` (nova) | colunas `origem_tipo text` / `origem_id uuid` + unique index parcial `ux_notificacao_legado_origem (usuario_id, origem_tipo, origem_id, titulo) where reg_status='A' and origem_id is not null` | teste estático de contrato |
| `Data.cs` — `NotificacaoService.CriarNotificacaoAsync` | `private Task` → `internal Task<bool>` (+ parâmetros opcionais `origemTipo`/`origemId`); insert passa a trazer as origens e `on conflict (...) where reg_status='A' and origem_id is not null do nothing`; retorna `false` quando dedupado | 4 testes comportamentais |
| 21 call sites | todos declararam origem (mapa abaixo); 2 inserts passaram a gerar o Guid em C# para expor o id da entidade (contestação de pagamento, divergência de fechamento) | build 0 erros + suíte |

### Mapa de origens declaradas

| Dominio | Call sites | origem_tipo | origem_id |
|---|---|---|---|
| Escala (aceite/convite/7 transições de status) | `Data.cs:1447,1513,1584,1606,1632,1670,1687,1702` | `escala` | id da escala |
| Pagamento (gerado/confirmado/registrado/estornado/cancelado) | `Data.cs:1859,1905,1931,2005,2033` | `pagamento` | id do pagamento |
| Contestação (aberta/resolvida) | `Data.cs:1957,1985` | `contestacao` | id da **contestação** (não do pagamento) |
| Convite por recomendação | `BusinessRulesServices.cs:374` | `convite` | id do convite |
| Fechamento (criado/financeiro/transição genérica) | `FechamentoOperacionalService.cs:109,172,176` | `fechamento` | id do fechamento |
| Divergência (aberta/resolvida) | `FechamentoOperacionalService.cs:153,163` | `divergencia` | id da **divergência** (não do fechamento) |

## Decisões registradas

1. **Convenção copiada do repositório, não inventada**: mesma forma do Operation360
   (origem_tipo/origem_id + `on conflict do nothing` + `inserted==0 → false`).
2. **Dedupe PARCIAL e intencional**: só linhas COM origem declarada entram na chave.
   Chamadas sem origem (e linhas históricas) mantêm o comportamento anterior — sem
   inventar dedupe para eventos sem entidade identificável.
3. **`titulo` assume o papel de `tipo_evento`** do Operation360: distingue eventos
   diferentes do mesmo registro (ex.: "Escala confirmada" → depois "Escala cancelada"
   coexistem para a mesma escala); repetir o MESMO evento do MESMO registro para o MESMO
   usuário vira no-op.
4. **Origem fina para entidades de segundo nível**: contestação e divergência usam o
   próprio id (e não o do pagamento/fechamento pai) — permite segunda contestação/
   divergência legítimas sobre o mesmo pai, cada uma notificando uma vez.
5. **Caso-limite aceito (documentado)**: evento idêntico repetido no MESMO registro
   colapsa numa única notificação para sempre (ex.: pagamento → pago → estornado → pago
   de novo: apenas o primeiro "Pagamento confirmado" notifica). É exatamente o
   comportamento da convenção adotada (dedupe permanente por tipo de evento) — registrado
   como dívida observável, não como regra nova.
6. **`private` → `internal`** (via `InternalsVisibleTo("PlantaoPro.Tests")` existente):
   permite teste comportamental direto sem ampliar a superfície pública da API.

## Validação

- Builds Web+Tests `-m:1 -nodeReuse:false`: **0 erros**.
- Run direcionado: **R8B2 5/5** (~0.5 s) — retry idempotente (false + 1 linha); sequência
  confirmada→cancelada coexistem; mesmo evento em registros distintos coexistem; sem
  origem mantém uma-por-chamada; contrato do arquivo v2346.
- Pipeline v2: gate `phaseA` **52/52**; full ×3 = **1282 / 1281+1 / 1281+1**. As falhas
  isoladas dos full2/full3 foram `Npgsql 40P01 (impasse)` em `SaasComercialB4MatrizTests`
  (testes distintos em cada run; RAM livre ~1,2 GB no start) — categoria ① do catálogo de
  flakes; **provadas transientes** por rerun isolado da classe: **12/12 aprovados**
  (a classe passou no full1).
- Baseline da suíte sobe de 1277 para **1282** (+5 fatos deste corte).

**Estado honesto**: IMPLEMENTADO → TESTADO. AINDA NÃO HOMOLOGADO em host vivo e não LIBERADO
(o upgrade v2346 precisa ir junto no deploy do banco). Commit local; push diferido por
decisão da rodada.
